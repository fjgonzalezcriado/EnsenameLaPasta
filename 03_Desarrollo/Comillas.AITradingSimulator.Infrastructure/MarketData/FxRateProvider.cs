using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Infrastructure.MarketData;

/// <summary>
/// Tipos de cambio vía Yahoo Finance (símbolo <c>{FROM}{TO}=X</c>, p.ej. <c>USDEUR=X</c>),
/// con caché en memoria por par de divisas y TTL configurable. Degrada a último valor
/// conocido o a 1 si la fuente falla (no rompe el dashboard).
/// </summary>
public sealed class FxRateProvider : IFxRateProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IOptionsMonitor<FxOptions> _options;
    private readonly TimeProvider _time;
    private readonly ILogger<FxRateProvider> _logger;

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    public FxRateProvider(
        IHttpClientFactory httpFactory,
        IOptionsMonitor<FxOptions> options,
        TimeProvider time,
        ILogger<FxRateProvider> logger)
    {
        _httpFactory = httpFactory;
        _options = options;
        _time = time;
        _logger = logger;
    }

    public async Task<decimal> GetRateAsync(string from, string to, CancellationToken cancellationToken = default)
    {
        var f = (from ?? string.Empty).Trim().ToUpperInvariant();
        var t = (to ?? string.Empty).Trim().ToUpperInvariant();

        if (f.Length == 0 || t.Length == 0 || f == t)
            return 1m;

        var key = f + t;
        var ttl = TimeSpan.FromMinutes(Math.Max(1, _options.CurrentValue.CacheMinutes));
        var now = _time.GetUtcNow();

        if (_cache.TryGetValue(key, out var cached) && now - cached.At < ttl)
            return cached.Rate;

        try
        {
            var rate = await FetchRateAsync(f, t, cancellationToken);
            _cache[key] = new CacheEntry(rate, now);
            return rate;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Degradación: último valor conocido (aunque esté vencido) o 1.
            _logger.LogWarning(ex, "FX {From}->{To}: fallo al obtener el tipo; se usa fallback.", f, t);
            return _cache.TryGetValue(key, out var stale) ? stale.Rate : 1m;
        }
    }

    private async Task<decimal> FetchRateAsync(string from, string to, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient(YahooFinanceProvider.HttpClientName);
        var symbol = $"{from}{to}=X";

        var response = await client.GetAsync(
            $"/v8/finance/chart/{Uri.EscapeDataString(symbol)}?interval=1d&range=1d", ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<YahooChartResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Yahoo FX: respuesta vacía o no deserializable.");

        var price = payload.Chart?.Result?.FirstOrDefault()?.Meta?.RegularMarketPrice;
        if (price is null or <= 0)
            throw new InvalidOperationException($"Yahoo FX: tipo inválido para '{symbol}'.");

        return (decimal)price.Value;
    }

    private readonly record struct CacheEntry(decimal Rate, DateTimeOffset At);

    private sealed record YahooChartResponse(
        [property: JsonPropertyName("chart")] YahooChart? Chart);
    private sealed record YahooChart(
        [property: JsonPropertyName("result")] List<YahooChartResult>? Result);
    private sealed record YahooChartResult(
        [property: JsonPropertyName("meta")] YahooChartMeta? Meta);
    private sealed record YahooChartMeta(
        [property: JsonPropertyName("regularMarketPrice")] double? RegularMarketPrice);
}
