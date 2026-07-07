using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Comillas.AITradingSimulator.Infrastructure.MarketData;

/// <summary>
/// Clasifica la dirección del próximo periodo (sube/baja) con ML.NET (regresión logística SDCA)
/// sobre features técnicas del histórico: retorno, momentum, cruce de medias, precio vs media,
/// RSI, volatilidad y volumen relativo (HV-044). Entrena al vuelo con split cronológico 80/20 y
/// reporta accuracy/AUC del hold-out (honestidad: la dirección de precio ronda el azar).
/// </summary>
public sealed class MlDirectionClassifier : IDirectionClassifier
{
    private const int Lookback = 14;      // RSI 14 marca el mínimo de historia por muestra
    private const int MinPoints = 40;     // mínimo para tener muestras de entrenamiento suficientes
    public const int FeatureCount = 7;

    private readonly IMarketHistoryProvider _history;
    private readonly ILogger<MlDirectionClassifier> _logger;

    public MlDirectionClassifier(IMarketHistoryProvider history, ILogger<MlDirectionClassifier> logger)
    {
        _history = history;
        _logger = logger;
    }

    public async Task<DirectionSignal> ClassifyAsync(string symbol, string range, CancellationToken cancellationToken = default)
    {
        var points = await _history.GetHistoryAsync(symbol, range, cancellationToken);
        var series = points.Where(p => p.Price > 0).ToArray();
        var closes = series.Select(p => (double)p.Price).ToArray();
        var volumes = series.Select(p => (double)p.Volume).ToArray();

        DirectionSignal Insufficient(string msg) =>
            new(symbol, range, false, "Baja", 0.5, "Mantener", 0, 0, 0, FeatureCount, msg);

        if (closes.Length < MinPoints)
            return Insufficient($"Histórico insuficiente para la clasificación (se necesitan ≥ {MinPoints} puntos).");

        try
        {
            var samples = new List<FeatureRow>();
            // Muestras etiquetadas: i en [Lookback, n-2] (necesita c[i+1] para la etiqueta).
            for (var i = Lookback; i <= closes.Length - 2; i++)
                samples.Add(new FeatureRow { Features = BuildFeatures(closes, volumes, i), Label = closes[i + 1] > closes[i] });

            if (samples.Count < 20)
                return Insufficient("Muestras de entrenamiento insuficientes.");

            // Split cronológico 80/20 (no aleatorio: en series temporales no se mezcla el futuro).
            var cut = (int)(samples.Count * 0.8);
            var trainRows = samples.Take(cut).ToList();
            var testRows = samples.Skip(cut).ToList();

            // SDCA necesita ambas clases en entrenamiento.
            if (trainRows.All(r => r.Label) || trainRows.None(r => r.Label))
                return Insufficient("Sin variación de dirección suficiente para entrenar.");

            var ml = new MLContext(seed: 0);
            var trainData = ml.Data.LoadFromEnumerable(trainRows);
            var pipeline = ml.Transforms.NormalizeMinMax("Features")
                .Append(ml.BinaryClassification.Trainers.SdcaLogisticRegression(
                    labelColumnName: nameof(FeatureRow.Label), featureColumnName: nameof(FeatureRow.Features)));
            var model = pipeline.Fit(trainData);

            // Evaluación honesta en el hold-out.
            double accuracy = 0, auc = 0;
            if (testRows.Count > 0)
            {
                var metrics = ml.BinaryClassification.Evaluate(model.Transform(ml.Data.LoadFromEnumerable(testRows)),
                    labelColumnName: nameof(FeatureRow.Label));
                accuracy = double.IsNaN(metrics.Accuracy) ? 0 : metrics.Accuracy;
                auc = double.IsNaN(metrics.AreaUnderRocCurve) ? 0 : metrics.AreaUnderRocCurve;
            }

            // Predicción para el último dato (features en n-1, sin etiqueta futura todavía).
            var engine = ml.Model.CreatePredictionEngine<FeatureRow, DirectionPrediction>(model);
            var latest = new FeatureRow { Features = BuildFeatures(closes, volumes, closes.Length - 1) };
            var pred = engine.Predict(latest);

            var pUp = Math.Clamp(pred.Probability, 0f, 1f);
            var direction = pUp >= 0.5f ? "Sube" : "Baja";
            var signal = pUp >= 0.55f ? "Comprar" : pUp <= 0.45f ? "Vender" : "Mantener";

            _logger.LogDebug("Clasificación {Symbol} {Range}: {Dir} P(sube)={P:F2} acc={Acc:F2} auc={Auc:F2} (n={N}).",
                symbol, range, direction, pUp, accuracy, auc, trainRows.Count);

            return new DirectionSignal(symbol, range, true, direction, pUp, signal, accuracy, auc,
                trainRows.Count, FeatureCount, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Clasificación {Symbol} {Range} falló: {Error}", symbol, range, ex.Message);
            return Insufficient("No se pudo calcular la señal.");
        }
    }

    // 7 features técnicas en el índice i (requiere i >= Lookback).
    private static float[] BuildFeatures(double[] c, double[] v, int i)
    {
        var ret1 = Change(c[i - 1], c[i]);
        var ret5 = Change(c[i - 5], c[i]);
        var sma5 = Mean(c, i - 4, i);
        var sma10 = Mean(c, i - 9, i);
        var maRatio = sma10 != 0 ? sma5 / sma10 - 1 : 0;
        var priceVsSma = sma10 != 0 ? c[i] / sma10 - 1 : 0;
        var rsi = Rsi(c, i, Lookback) / 100.0;                 // 0..1
        var vol = Volatility(c, i, 10);
        var avgVol = Mean(v, i - 9, i);
        var volRatio = avgVol > 0 ? v[i] / avgVol - 1 : 0;
        return new[] { (float)ret1, (float)ret5, (float)maRatio, (float)priceVsSma, (float)rsi, (float)vol, (float)volRatio };
    }

    private static double Change(double from, double to) => from != 0 ? (to - from) / from : 0;

    private static double Mean(double[] a, int lo, int hi)
    {
        double sum = 0; var n = 0;
        for (var k = lo; k <= hi; k++) { sum += a[k]; n++; }
        return n > 0 ? sum / n : 0;
    }

    private static double Volatility(double[] c, int i, int window)
    {
        var rets = new List<double>(window);
        for (var k = i - window + 1; k <= i; k++)
            if (k >= 1) rets.Add(Change(c[k - 1], c[k]));
        if (rets.Count < 2) return 0;
        var mean = rets.Average();
        return Math.Sqrt(rets.Sum(r => (r - mean) * (r - mean)) / rets.Count);
    }

    private static double Rsi(double[] c, int i, int period)
    {
        double gain = 0, loss = 0;
        for (var k = i - period + 1; k <= i; k++)
        {
            if (k < 1) continue;
            var d = c[k] - c[k - 1];
            if (d >= 0) gain += d; else loss += -d;
        }
        var avgGain = gain / period;
        var avgLoss = loss / period;
        if (avgLoss == 0) return avgGain == 0 ? 50.0 : 100.0;
        var rs = avgGain / avgLoss;
        return 100.0 - 100.0 / (1.0 + rs);
    }

    private sealed class FeatureRow
    {
        [VectorType(FeatureCount)]
        public float[] Features { get; set; } = new float[FeatureCount];
        public bool Label { get; set; }
    }

    private sealed class DirectionPrediction
    {
        [ColumnName("PredictedLabel")] public bool PredictedLabel { get; set; }
        public float Probability { get; set; }
        public float Score { get; set; }
    }
}

internal static class EnumerableGuards
{
    public static bool None<T>(this IEnumerable<T> source, Func<T, bool> predicate) => !source.Any(predicate);
}
