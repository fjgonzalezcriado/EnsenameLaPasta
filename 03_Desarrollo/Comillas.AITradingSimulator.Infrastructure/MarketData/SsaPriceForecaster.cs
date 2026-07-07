using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

namespace Comillas.AITradingSimulator.Infrastructure.MarketData;

/// <summary>
/// Pronóstico de precio con ML.NET usando SSA (Singular Spectrum Analysis) sobre la serie de
/// cierres del histórico del símbolo (HV-043). Entrena al vuelo con la propia serie (no requiere
/// datos de entrenamiento persistidos) y devuelve el pronóstico con banda de confianza y una
/// señal (Alcista/Bajista/Neutral) derivada del cambio esperado.
/// </summary>
public sealed class SsaPriceForecaster(IMarketHistoryProvider history, ILogger<SsaPriceForecaster> logger) : IPriceForecaster
{
    // Nº mínimo de puntos para que SSA sea razonable; umbral (%) para clasificar la señal.
    private const int MinPoints = 12;
    private const decimal SignalThresholdPct = 0.5m;

    private readonly IMarketHistoryProvider _history = history;
    private readonly ILogger<SsaPriceForecaster> _logger = logger;

    public async Task<PriceForecast> ForecastAsync(string symbol, string range, int horizon, CancellationToken cancellationToken = default)
    {
        if (horizon < 1) horizon = 10;

        var points = await _history.GetHistoryAsync(symbol, range, cancellationToken);
        var closes = points.Where(p => p.Price > 0).Select(p => (float)p.Price).ToArray();
        var last = closes.Length > 0 ? (decimal)closes[^1] : 0m;

        if (closes.Length < MinPoints)
            return new PriceForecast(symbol, range, false, "Insuficiente", last, last, 0m, [],
                $"Histórico insuficiente para el pronóstico (se necesitan ≥ {MinPoints} puntos).");

        try
        {
            var mlContext = new MLContext(seed: 0);
            // Ventana SSA: ~1/3 de la serie, acotada, garantizando trainSize > 2·windowSize.
            var window = Math.Max(2, Math.Min(closes.Length / 3, 30));

            var pipeline = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: nameof(SsaOutput.Forecast),
                inputColumnName: nameof(SsaInput.Value),
                windowSize: window,
                seriesLength: closes.Length,
                trainSize: closes.Length,
                horizon: horizon,
                confidenceLowerBoundColumn: nameof(SsaOutput.LowerBound),
                confidenceUpperBoundColumn: nameof(SsaOutput.UpperBound),
                confidenceLevel: 0.95f);

            var dataView = mlContext.Data.LoadFromEnumerable(closes.Select(v => new SsaInput { Value = v }));
            var model = pipeline.Fit(dataView);
            using var engine = model.CreateTimeSeriesEngine<SsaInput, SsaOutput>(mlContext);
            var prediction = engine.Predict();

            var forecast = prediction.Forecast ?? [];
            if (forecast.Length == 0)
                return new PriceForecast(symbol, range, false, "Insuficiente", last, last, 0m, [],
                    "El modelo no devolvió pronóstico.");

            var lower = prediction.LowerBound ?? [];
            var upper = prediction.UpperBound ?? [];
            var pts = new List<ForecastPoint>(forecast.Length);
            for (var i = 0; i < forecast.Length; i++)
            {
                var v = (decimal)forecast[i];
                var lo = i < lower.Length ? (decimal)lower[i] : v;
                var up = i < upper.Length ? (decimal)upper[i] : v;
                pts.Add(new ForecastPoint(v, Math.Min(lo, up), Math.Max(lo, up)));
            }

            var forecastEnd = pts[^1].Value;
            var changePct = last != 0m ? (forecastEnd - last) / last * 100m : 0m;
            var signal = changePct >= SignalThresholdPct ? "Alcista"
                       : changePct <= -SignalThresholdPct ? "Bajista"
                       : "Neutral";

            _logger.LogDebug("SSA {Symbol} {Range}: {H} puntos, señal {Signal} ({Pct}%).",
                symbol, range, pts.Count, signal, changePct);

            return new PriceForecast(symbol, range, true, signal, last, forecastEnd, changePct, pts, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Pronóstico SSA {Symbol} {Range} falló: {Error}", symbol, range, ex.Message);
            return new PriceForecast(symbol, range, false, "Insuficiente", last, last, 0m, [],
                "No se pudo calcular el pronóstico.");
        }
    }

    // Tipos de entrada/salida de ML.NET (por reflexión sobre los nombres de columna).
    private sealed class SsaInput
    {
        public float Value { get; set; }
    }

    private sealed class SsaOutput
    {
        public float[] Forecast { get; set; } = [];
        public float[] LowerBound { get; set; } = [];
        public float[] UpperBound { get; set; } = [];
    }
}
