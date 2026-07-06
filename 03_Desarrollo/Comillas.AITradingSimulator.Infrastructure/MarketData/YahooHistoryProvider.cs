using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Comillas.AITradingSimulator.Infrastructure.MarketData;

/// <summary>
/// Histórico de precios desde Yahoo Finance (<c>/v8/finance/chart</c> con
/// <c>range</c>/<c>interval</c>). Reutiliza el HttpClient "YahooFinance".
/// </summary>
public sealed class YahooHistoryProvider : IMarketHistoryProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<YahooHistoryProvider> _logger;

    public YahooHistoryProvider(IHttpClientFactory httpFactory, ILogger<YahooHistoryProvider> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    // Rango de la UI -> (range, interval) de Yahoo.
    private static readonly IReadOnlyDictionary<string, (string Range, string Interval)> RangeMap =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
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
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ChartResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Yahoo Finance: respuesta vacía o no deserializable.");

        var result = payload.Chart?.Result?.FirstOrDefault();
        var timestamps = result?.Timestamp;
        var quote = result?.Indicators?.Quote?.FirstOrDefault();
        var closes = quote?.Close;
        var volumes = quote?.Volume;   // array de volumen en paralelo al de cierre
        if (timestamps is null || closes is null)
            return [];

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
            points.Add(new PricePoint(ts, (decimal)close.Value, volume));
        }

        _logger.LogDebug("Yahoo histórico {Symbol} {Range}: {Count} puntos.", yahooSymbol, range, points.Count);
        return points;
    }

    /// <summary>Mapea símbolos internos al formato de Yahoo (cripto con guion).</summary>
    private static string NormalizeSymbol(string symbol) => symbol.ToUpperInvariant() switch
    {
        "BTCUSD" => "BTC-USD",
        "ETHUSD" => "ETH-USD",
        _ => symbol,
    };

    // DTOs para deserializar /v8/finance/chart (arrays de histórico).
    private sealed record ChartResponse(
        [property: JsonPropertyName("chart")] ChartBody? Chart);

    private sealed record ChartBody(
        [property: JsonPropertyName("result")] List<ChartResult>? Result);

    private sealed record ChartResult(
        [property: JsonPropertyName("timestamp")] List<long>? Timestamp,
        [property: JsonPropertyName("indicators")] ChartIndicators? Indicators);

    private sealed record ChartIndicators(
        [property: JsonPropertyName("quote")] List<ChartQuote>? Quote);

    private sealed record ChartQuote(
        [property: JsonPropertyName("close")] List<double?>? Close,
        [property: JsonPropertyName("volume")] List<long?>? Volume);
}
