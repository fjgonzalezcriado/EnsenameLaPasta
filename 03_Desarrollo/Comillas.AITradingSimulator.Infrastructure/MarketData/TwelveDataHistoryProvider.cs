using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Infrastructure.MarketData;

/// <summary>
/// Histórico de precios+volumen desde Twelve Data (<c>/time_series</c>). Alternativa a
/// <see cref="YahooHistoryProvider"/>; se registra cuando el proveedor activo es "TwelveData".
/// </summary>
public sealed class TwelveDataHistoryProvider(
    IHttpClientFactory httpFactory,
    IOptionsMonitor<TwelveDataOptions> options,
    TimeProvider time,
    ILogger<TwelveDataHistoryProvider> logger) : IMarketHistoryProvider
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly IOptionsMonitor<TwelveDataOptions> _options = options;
    private readonly TimeProvider _time = time;
    private readonly ILogger<TwelveDataHistoryProvider> _logger = logger;

    // Rango de la UI -> (interval, outputsize) de Twelve Data. YTD se calcula aparte.
    private static readonly IReadOnlyDictionary<string, (string Interval, int OutputSize)> RangeMap =
        new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
        {
            ["1D"] = ("5min", 100),
            ["5D"] = ("30min", 200),
            ["1M"] = ("1day", 30),
            ["3M"] = ("1day", 90),
            ["6M"] = ("1day", 180),
            ["1A"] = ("1day", 366),
            ["3A"] = ("1week", 160),
            ["5A"] = ("1week", 260),
        };

    private static readonly string[] DateTimeFormats =
        { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd" };

    public async Task<IReadOnlyList<PricePoint>> GetHistoryAsync(string symbol, string range, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol es obligatorio.", nameof(symbol));

        string interval;
        int outputSize;
        if (string.Equals(range, "YTD", StringComparison.OrdinalIgnoreCase))
        {
            var now = _time.GetUtcNow().UtcDateTime;
            interval = "1day";
            outputSize = (now - new DateTime(now.Year, 1, 1)).Days + 5;
        }
        else if (RangeMap.TryGetValue(range, out var r))
        {
            interval = r.Interval;
            outputSize = r.OutputSize;
        }
        else
        {
            throw new ArgumentException($"Rango no soportado: '{range}'.", nameof(range));
        }

        var apiKey = _options.CurrentValue.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "Twelve Data: falta la API key (MarketData:TwelveData:ApiKey).");

        var tdSymbol = NormalizeSymbol(symbol);
        var client = _httpFactory.CreateClient(TwelveDataProvider.HttpClientName);

        var response = await client.GetAsync(
            $"/time_series?symbol={Uri.EscapeDataString(tdSymbol)}&interval={interval}&outputsize={outputSize}&timezone=UTC&apikey={Uri.EscapeDataString(apiKey)}",
            cancellationToken);

        // Símbolo no válido para Twelve Data (p.ej. convención Yahoo) → 404. No es un fallo
        // del programa: degradamos a "sin datos" para no romper el gráfico.
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Twelve Data histórico {Symbol} {Range}: HTTP {Code}; sin datos.",
                tdSymbol, range, (int)response.StatusCode);
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<TimeSeriesResponse>(cancellationToken: cancellationToken);
        if (payload is null || string.Equals(payload.Status, "error", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Twelve Data histórico {Symbol} {Range}: {Message}; sin datos.",
                tdSymbol, range, payload?.Message ?? "respuesta no deserializable");
            return [];
        }

        if (payload.Values is null || payload.Values.Count == 0)
            return [];

        var points = new List<PricePoint>(payload.Values.Count);
        foreach (var v in payload.Values)
        {
            if (string.IsNullOrWhiteSpace(v.Datetime) || string.IsNullOrWhiteSpace(v.Close)) continue;
            if (!DateTime.TryParseExact(v.Datetime, DateTimeFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var ts))
                continue;
            if (!decimal.TryParse(v.Close, NumberStyles.Any, CultureInfo.InvariantCulture, out var close) || close <= 0)
                continue;
            decimal volume = 0m;
            if (!string.IsNullOrWhiteSpace(v.Volume))
                decimal.TryParse(v.Volume, NumberStyles.Any, CultureInfo.InvariantCulture, out volume);
            // OHLC para velas japonesas (HV-041); si falta un componente, cae al cierre.
            static decimal ParseOr(string? s, decimal fallback)
                => (!string.IsNullOrWhiteSpace(s) && decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) && d > 0) ? d : fallback;
            points.Add(new PricePoint(DateTime.SpecifyKind(ts, DateTimeKind.Utc), close, volume,
                Open: ParseOr(v.Open, close), High: ParseOr(v.High, close), Low: ParseOr(v.Low, close)));
        }

        // Twelve Data devuelve los valores del más reciente al más antiguo; ordenar ascendente.
        points.Reverse();

        _logger.LogDebug("Twelve Data histórico {Symbol} {Range}: {Count} puntos.", tdSymbol, range, points.Count);
        return points;
    }

    /// <summary>Mapea símbolos internos (estilo Yahoo) al formato de Twelve Data (cripto con barra).</summary>
    private static string NormalizeSymbol(string symbol) => symbol.ToUpperInvariant() switch
    {
        "BTCUSD" or "BTC-USD" => "BTC/USD",
        "ETHUSD" or "ETH-USD" => "ETH/USD",
        _ => symbol,
    };

    // DTOs para deserializar /time_series (valores numéricos como string).
    private sealed record TimeSeriesResponse(
        [property: JsonPropertyName("values")] List<TimeSeriesValue>? Values,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("code")] int? Code,
        [property: JsonPropertyName("message")] string? Message);

    private sealed record TimeSeriesValue(
        [property: JsonPropertyName("datetime")] string? Datetime,
        [property: JsonPropertyName("close")] string? Close,
        [property: JsonPropertyName("volume")] string? Volume,
        [property: JsonPropertyName("open")] string? Open = null,
        [property: JsonPropertyName("high")] string? High = null,
        [property: JsonPropertyName("low")] string? Low = null);
}
