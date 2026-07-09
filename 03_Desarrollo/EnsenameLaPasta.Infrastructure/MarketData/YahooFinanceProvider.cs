using System.Net.Http.Json;
using System.Text.Json.Serialization;
using EnsenameLaPasta.Application.Common.Dtos;
using EnsenameLaPasta.Application.Common.Interfaces;
using EnsenameLaPasta.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EnsenameLaPasta.Infrastructure.MarketData;

public sealed class YahooFinanceProvider(IHttpClientFactory httpFactory, ILogger<YahooFinanceProvider> logger) : IMarketDataProvider
{
    public const string HttpClientName = "YahooFinance";

    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<YahooFinanceProvider> _logger = logger;

    public async Task<MarketQuote> GetLatestAsync(string symbol, DateTime timestampUtc, CancellationToken cancellationToken = default)
    {
        var client = _httpFactory.CreateClient(HttpClientName);

        // Endpoint /v8/finance/chart: sirve precio + volumen sin autenticación.
        // (El antiguo /v7/finance/quote pasó a devolver 401 — requiere cookie + crumb.)
        var response = await client.GetAsync(
            $"/v8/finance/chart/{Uri.EscapeDataString(symbol)}?interval=1d&range=1d",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<YahooChartResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Yahoo Finance: respuesta vacía o no deserializable.");

        var meta = payload.Chart?.Result?.FirstOrDefault()?.Meta
            ?? throw new InvalidOperationException($"Yahoo Finance: sin cotización para '{symbol}'.");

        if (meta.RegularMarketPrice is null or <= 0)
            throw new InvalidOperationException($"Yahoo Finance: precio inválido para '{symbol}'.");

        var price = (decimal)meta.RegularMarketPrice.Value;
        var volume = (decimal)(meta.RegularMarketVolume ?? 0);

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Yahoo Finance: {Symbol} = {Price} {Currency} (vol {Volume})", symbol, price, meta.Currency, volume);

        return new MarketQuote(MarketTick.Create(symbol, price, volume, timestampUtc), meta.Currency);
    }

    // DTOs internos para deserializar la respuesta del endpoint /v8/finance/chart.
    private sealed record YahooChartResponse(
        [property: JsonPropertyName("chart")] YahooChart? Chart);

    private sealed record YahooChart(
        [property: JsonPropertyName("result")] List<YahooChartResult>? Result,
        [property: JsonPropertyName("error")] object? Error);

    private sealed record YahooChartResult(
        [property: JsonPropertyName("meta")] YahooChartMeta? Meta);

    private sealed record YahooChartMeta(
        [property: JsonPropertyName("symbol")] string? Symbol,
        [property: JsonPropertyName("regularMarketPrice")] double? RegularMarketPrice,
        [property: JsonPropertyName("regularMarketVolume")] long? RegularMarketVolume,
        [property: JsonPropertyName("currency")] string? Currency);
}
