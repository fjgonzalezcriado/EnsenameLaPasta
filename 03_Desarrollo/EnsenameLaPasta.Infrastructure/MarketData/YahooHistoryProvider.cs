using System.Net.Http.Json;
using System.Text.Json.Serialization;
using EnsenameLaPasta.Application.Common.Dtos;
using EnsenameLaPasta.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace EnsenameLaPasta.Infrastructure.MarketData;

/// <summary>
/// Histórico de precios desde Yahoo Finance (<c>/v8/finance/chart</c> con
/// <c>range</c>/<c>interval</c>). Reutiliza el HttpClient "YahooFinance".
/// </summary>
public sealed class YahooHistoryProvider(IHttpClientFactory httpFactory, ILogger<YahooHistoryProvider> logger)
    : IMarketHistoryProvider, IPreviousCloseProvider
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<YahooHistoryProvider> _logger = logger;

    // Rango de la UI -> (range, interval) de Yahoo.
    private static readonly Dictionary<string, (string Range, string Interval)> RangeMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["1D"] = ("1d", "5m"),
            ["5D"] = ("5d", "30m"),
            ["1M"] = ("1mo", "1d"),
            ["3M"] = ("3mo", "1d"),
            ["6M"] = ("6mo", "1d"),
            ["YTD"] = ("ytd", "1d"),
            ["1A"] = ("1y", "1d"),
            ["3A"] = ("3y", "1wk"),
            ["5A"] = ("5y", "1wk"),
        };

    public async Task<IReadOnlyList<PricePoint>> GetHistoryAsync(string symbol, string range, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol es obligatorio.", nameof(symbol));
        if (!RangeMap.TryGetValue(range, out var r))
            throw new ArgumentException($"Rango no soportado: '{range}'.", nameof(range));

        var yahooSymbol = NormalizeSymbol(symbol);
        var client = _httpFactory.CreateClient(YahooFinanceProvider.HttpClientName);

        var response = await client.GetAsync(
            $"/v8/finance/chart/{Uri.EscapeDataString(yahooSymbol)}?range={r.Range}&interval={r.Interval}",
            cancellationToken);

        // Símbolo no válido → 404. Degradamos a "sin datos" en vez de romper el gráfico.
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Yahoo histórico {Symbol} {Range}: HTTP {Code}; sin datos.",
                yahooSymbol, range, (int)response.StatusCode);
            return [];
        }

        var payload = await response.Content.ReadFromJsonAsync<ChartResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Yahoo Finance: respuesta vacía o no deserializable.");

        var result = payload.Chart?.Result?.FirstOrDefault();
        var timestamps = result?.Timestamp;
        var quote = result?.Indicators?.Quote?.FirstOrDefault();
        var closes = quote?.Close;
        var volumes = quote?.Volume;   // array de volumen en paralelo al de cierre
        var opens = quote?.Open;       // OHLC en paralelo (para velas japonesas, HV-041)
        var highs = quote?.High;
        var lows = quote?.Low;
        if (timestamps is null || closes is null)
            return [];

        // Toma el valor del array paralelo si existe y no es nulo; si no, cae al cierre.
        static decimal PickOr(List<double?>? arr, int i, decimal fallback)
            => (arr is not null && i < arr.Count && arr[i].HasValue) ? (decimal)arr[i]!.Value : fallback;

        var count = Math.Min(timestamps.Count, closes.Count);
        var points = new List<PricePoint>(count);
        for (var i = 0; i < count; i++)
        {
            var close = closes[i];
            if (close is null or <= 0) continue; // Yahoo intercala nulos (sesiones sin cierre)
            var ts = DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).UtcDateTime;
            var volume = (volumes is not null && i < volumes.Count && volumes[i].HasValue)
                ? (decimal)volumes[i]!.Value
                : 0m;
            var c = (decimal)close.Value;
            points.Add(new PricePoint(ts, c, volume,
                Open: PickOr(opens, i, c), High: PickOr(highs, i, c), Low: PickOr(lows, i, c)));
        }

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Yahoo histórico {Symbol} {Range}: {Count} puntos.", yahooSymbol, range, points.Count);
        return points;
    }

    /// <summary>
    /// Cierre del día de negociación anterior (HV-058), vía <c>meta.previousClose</c> del mismo
    /// endpoint <c>/v8/finance/chart</c> que usa <see cref="GetHistoryAsync"/>. Mismo
    /// range/interval que su llamada para "1D" (<c>1d</c>/<c>5m</c>): con <c>interval=1d</c> ese
    /// campo no viene en meta (Yahoo lo renombra a <c>chartPreviousClose</c> en ese caso) — se
    /// comprueban ambos nombres por si algún símbolo solo trae uno. Hace su propia petición (no
    /// reutiliza la de GetHistoryAsync: esta clase es singleton sin estado por símbolo, y
    /// cachear un único valor pisaría el de otros símbolos). Degrada a null sin lanzar: sin este
    /// dato, el consumidor cae a su comportamiento anterior.
    /// </summary>
    public async Task<decimal?> GetPreviousCloseAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return null;

        try
        {
            var yahooSymbol = NormalizeSymbol(symbol);
            var client = _httpFactory.CreateClient(YahooFinanceProvider.HttpClientName);
            var response = await client.GetAsync(
                $"/v8/finance/chart/{Uri.EscapeDataString(yahooSymbol)}?range=1d&interval=5m",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var payload = await response.Content.ReadFromJsonAsync<ChartResponse>(cancellationToken: cancellationToken);
            var meta = payload?.Chart?.Result?.FirstOrDefault()?.Meta;
            var previousClose = meta?.PreviousClose ?? meta?.ChartPreviousClose;
            return previousClose is > 0 ? (decimal)previousClose.Value : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Yahoo previousClose {Symbol} falló.", symbol);
            return null;
        }
    }

    /// <summary>Mapea símbolos internos al formato de Yahoo (cripto con guion).</summary>
    private static string NormalizeSymbol(string symbol) => symbol.ToUpperInvariant() switch
    {
        "BTCUSD" => "BTC-USD",
        "ETHUSD" => "ETH-USD",
        _ => symbol,
    };

    // DTOs para deserializar /v8/finance/chart (arrays de histórico + meta.previousClose).
    private sealed record ChartResponse(
        [property: JsonPropertyName("chart")] ChartBody? Chart);

    private sealed record ChartBody(
        [property: JsonPropertyName("result")] List<ChartResult>? Result);

    private sealed record ChartResult(
        [property: JsonPropertyName("timestamp")] List<long>? Timestamp,
        [property: JsonPropertyName("indicators")] ChartIndicators? Indicators,
        [property: JsonPropertyName("meta")] ChartMeta? Meta = null);

    private sealed record ChartMeta(
        [property: JsonPropertyName("previousClose")] double? PreviousClose,
        [property: JsonPropertyName("chartPreviousClose")] double? ChartPreviousClose = null);

    private sealed record ChartIndicators(
        [property: JsonPropertyName("quote")] List<ChartQuote>? Quote);

    private sealed record ChartQuote(
        [property: JsonPropertyName("close")] List<double?>? Close,
        [property: JsonPropertyName("volume")] List<long?>? Volume,
        [property: JsonPropertyName("open")] List<double?>? Open = null,
        [property: JsonPropertyName("high")] List<double?>? High = null,
        [property: JsonPropertyName("low")] List<double?>? Low = null);
}
