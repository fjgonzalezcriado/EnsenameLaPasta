using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;

namespace Comillas.AITradingSimulator.Infrastructure.MarketData;

/// <summary>
/// Clasifica la dirección del próximo periodo (sube/baja) con ML.NET sobre features técnicas del
/// histórico (HV-044, ampliado en HV-045). Entrena DOS modelos — regresión logística SDCA y
/// árboles con boosting FastTree — con split cronológico 80/20 y se queda con el mejor por AUC en
/// el hold-out (reporta accuracy/AUC y el modelo elegido). Honestidad: la dirección de precio
/// ronda el azar; es un indicador, no asesoramiento.
/// </summary>
public sealed class MlDirectionClassifier(IMarketHistoryProvider history, ILogger<MlDirectionClassifier> logger) : IDirectionClassifier
{
    private const int Lookback = 26;      // warmup del EMA26 (MACD) marca el mínimo por muestra
    private const int MinPoints = 60;     // mínimo para tener muestras de entrenamiento suficientes
    public const int FeatureCount = 12;

    private readonly IMarketHistoryProvider _history = history;
    private readonly ILogger<MlDirectionClassifier> _logger = logger;

    public async Task<DirectionSignal> ClassifyAsync(string symbol, string range, CancellationToken cancellationToken = default)
    {
        var points = await _history.GetHistoryAsync(symbol, range, cancellationToken);
        var series = points.Where(p => p.Price > 0).ToArray();
        var closes = series.Select(p => (double)p.Price).ToArray();
        var volumes = series.Select(p => (double)p.Volume).ToArray();

        DirectionSignal Insufficient(string msg) =>
            new(symbol, range, false, "Baja", 0.5, "Mantener", 0, 0, 0, FeatureCount, "-", msg);

        if (closes.Length < MinPoints)
            return Insufficient($"Histórico insuficiente para la clasificación (se necesitan ≥ {MinPoints} puntos).");

        try
        {
            // Indicadores acumulativos precomputados (EMAs para MACD).
            var ema12 = Ema(closes, 12);
            var ema26 = Ema(closes, 26);
            var macd = new double[closes.Length];
            for (var k = 0; k < closes.Length; k++) macd[k] = ema12[k] - ema26[k];
            var signal = Ema(macd, 9);

            var samples = new List<FeatureRow>();
            for (var i = Lookback; i <= closes.Length - 2; i++)
                samples.Add(new FeatureRow { Features = BuildFeatures(closes, volumes, macd, signal, i), Label = closes[i + 1] > closes[i] });

            if (samples.Count < 20)
                return Insufficient("Muestras de entrenamiento insuficientes.");

            var cut = (int)(samples.Count * 0.8);
            var trainRows = samples.Take(cut).ToList();
            var testRows = samples.Skip(cut).ToList();

            if (trainRows.All(r => r.Label) || trainRows.None(r => r.Label))
                return Insufficient("Sin variación de dirección suficiente para entrenar.");

            var ml = new MLContext(seed: 0);
            var trainData = ml.Data.LoadFromEnumerable(trainRows);
            var testData = ml.Data.LoadFromEnumerable(testRows);
            var norm = ml.Transforms.NormalizeMinMax("Features");

            // Dos candidatos; nos quedamos con el de mayor AUC (o accuracy si empatan) en hold-out.
            var candidates = new (string Name, IEstimator<ITransformer> Pipe)[]
            {
                ("SDCA", norm.Append(ml.BinaryClassification.Trainers.SdcaLogisticRegression(
                    labelColumnName: nameof(FeatureRow.Label), featureColumnName: nameof(FeatureRow.Features)))),
                ("FastTree", norm.Append(ml.BinaryClassification.Trainers.FastTree(new FastTreeBinaryTrainer.Options
                {
                    LabelColumnName = nameof(FeatureRow.Label),
                    FeatureColumnName = nameof(FeatureRow.Features),
                    NumberOfThreads = 1,          // determinismo
                    NumberOfTrees = 50,
                    NumberOfLeaves = 10,
                    MinimumExampleCountPerLeaf = 5,
                }))),
            };

            (string Name, ITransformer Model, double Acc, double Auc)? best = null;
            foreach (var (Name, Pipe) in candidates)
            {
                var model = Pipe.Fit(trainData);
                var m = ml.BinaryClassification.Evaluate(model.Transform(testData), labelColumnName: nameof(FeatureRow.Label));
                var acc = double.IsNaN(m.Accuracy) ? 0 : m.Accuracy;
                var auc = double.IsNaN(m.AreaUnderRocCurve) ? 0 : m.AreaUnderRocCurve;
                if (best is null || auc > best.Value.Auc || (auc == best.Value.Auc && acc > best.Value.Acc))
                    best = (Name, model, acc, auc);
            }

            var winner = best!.Value;
            var engine = ml.Model.CreatePredictionEngine<FeatureRow, DirectionPrediction>(winner.Model);
            var latest = new FeatureRow { Features = BuildFeatures(closes, volumes, macd, signal, closes.Length - 1) };
            var pred = engine.Predict(latest);

            var pUp = Math.Clamp(pred.Probability, 0f, 1f);
            var direction = pUp >= 0.5f ? "Sube" : "Baja";
            var sig = pUp >= 0.55f ? "Comprar" : pUp <= 0.45f ? "Vender" : "Mantener";

            _logger.LogDebug("Clasificación {Symbol} {Range}: {Dir} P(sube)={P:F2} modelo {Model} acc={Acc:F2} auc={Auc:F2} (n={N}).",
                symbol, range, direction, pUp, winner.Name, winner.Acc, winner.Auc, trainRows.Count);

            return new DirectionSignal(symbol, range, true, direction, pUp, sig, winner.Acc, winner.Auc,
                trainRows.Count, FeatureCount, winner.Name, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Clasificación {Symbol} {Range} falló: {Error}", symbol, range, ex.Message);
            return Insufficient("No se pudo calcular la señal.");
        }
    }

    // 12 features técnicas en el índice i (requiere i >= Lookback).
    private static float[] BuildFeatures(double[] c, double[] v, double[] macd, double[] signal, int i)
    {
        var ret1 = Change(c[i - 1], c[i]);
        var ret5 = Change(c[i - 5], c[i]);
        var ret10 = Change(c[i - 10], c[i]);
        var sma5 = Mean(c, i - 4, i);
        var sma10 = Mean(c, i - 9, i);
        var sma20 = Mean(c, i - 19, i);
        var maRatio = sma10 != 0 ? sma5 / sma10 - 1 : 0;
        var priceVsSma10 = sma10 != 0 ? c[i] / sma10 - 1 : 0;
        var priceVsSma20 = sma20 != 0 ? c[i] / sma20 - 1 : 0;
        var rsi = Rsi(c, i, 14) / 100.0;                          // 0..1
        var vol = Volatility(c, i, 10);
        var avgVol = Mean(v, i - 9, i);
        var volRatio = avgVol > 0 ? v[i] / avgVol - 1 : 0;
        var macdHist = c[i] != 0 ? (macd[i] - signal[i]) / c[i] : 0;
        var pctB = BollingerPctB(c, i, 20);                       // ~0..1 (posición en las bandas)
        var stochK = StochasticK(c, i, 14);                       // 0..1
        return
        [
            (float)ret1, (float)ret5, (float)ret10, (float)maRatio, (float)priceVsSma10, (float)priceVsSma20,
            (float)rsi, (float)vol, (float)volRatio, (float)macdHist, (float)pctB, (float)stochK
        ];
    }

    private static double Change(double from, double to) => from != 0 ? (to - from) / from : 0;

    private static double Mean(double[] a, int lo, int hi)
    {
        double sum = 0; var n = 0;
        for (var k = lo; k <= hi; k++) { if (k < 0) continue; sum += a[k]; n++; }
        return n > 0 ? sum / n : 0;
    }

    // Media móvil exponencial de toda la serie (para MACD).
    private static double[] Ema(double[] a, int period)
    {
        var ema = new double[a.Length];
        if (a.Length == 0) return ema;
        var alpha = 2.0 / (period + 1);
        ema[0] = a[0];
        for (var k = 1; k < a.Length; k++) ema[k] = alpha * a[k] + (1 - alpha) * ema[k - 1];
        return ema;
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

    // %B de Bollinger(window, 2σ): posición del precio dentro de las bandas (0=inferior, 1=superior).
    private static double BollingerPctB(double[] c, int i, int window)
    {
        var mean = Mean(c, i - window + 1, i);
        double sq = 0; var n = 0;
        for (var k = i - window + 1; k <= i; k++) { if (k < 0) continue; sq += (c[k] - mean) * (c[k] - mean); n++; }
        if (n == 0) return 0.5;
        var sd = Math.Sqrt(sq / n);
        if (sd == 0) return 0.5;
        var lower = mean - 2 * sd; var upper = mean + 2 * sd;
        return (c[i] - lower) / (upper - lower);
    }

    // %K del estocástico(window): posición del cierre en el rango máx-mín de la ventana (0..1).
    private static double StochasticK(double[] c, int i, int window)
    {
        double min = double.MaxValue, max = double.MinValue;
        for (var k = i - window + 1; k <= i; k++)
        {
            if (k < 0) continue;
            if (c[k] < min) min = c[k];
            if (c[k] > max) max = c[k];
        }
        return max > min ? (c[i] - min) / (max - min) : 0.5;
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
