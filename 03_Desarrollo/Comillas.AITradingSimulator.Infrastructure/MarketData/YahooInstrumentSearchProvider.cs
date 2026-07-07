using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Comillas.AITradingSimulator.Infrastructure.MarketData;

/// <summary>
/// Búsqueda de instrumentos vía Yahoo Finance (<c>/v1/finance/search</c>).
/// Soporta búsqueda por nombre/descripción, ISIN y ticker/core.
/// (WKN no está soportado por Yahoo: devuelve 0 resultados.)
/// Reutiliza el HttpClient "YahooFinance".
/// </summary>
public sealed class YahooInstrumentSearchProvider(IHttpClientFactory httpFactory, ILogger<YahooInstrumentSearchProvider> logger) : IInstrumentSearchProvider
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<YahooInstrumentSearchProvider> _logger = logger;

    public async Task<IReadOnlyList<InstrumentSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var client = _httpFactory.CreateClient(YahooFinanceProvider.HttpClientName);

        var response = await client.GetAsync(
            $"/v1/finance/search?q={Uri.EscapeDataString(query)}&quotesCount=15&newsCount=0",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Yahoo Finance: respuesta de búsqueda vacía o no deserializable.");

        var quotes = payload.Quotes ?? [];
        var results = quotes
            .Where(q => !string.IsNullOrWhiteSpace(q.Symbol))
            .Select(q => new InstrumentSearchResult(
                q.Symbol!,
                FirstNonEmpty(q.LongName, q.ShortName) ?? q.Symbol!,
                FirstNonEmpty(q.ExchDisp, q.Exchange) ?? string.Empty,
                FirstNonEmpty(q.TypeDisp, q.QuoteType) ?? string.Empty))
            .ToList();

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Yahoo búsqueda '{Query}': {Count} resultados.", query, results.Count);
        return results;
    }

    private static string? FirstNonEmpty(string? a, string? b)
        => !string.IsNullOrWhiteSpace(a) ? a : (!string.IsNullOrWhiteSpace(b) ? b : null);

    // DTOs para deserializar /v1/finance/search.
    private sealed record SearchResponse(
        [property: JsonPropertyName("quotes")] List<SearchQuote>? Quotes);

    private sealed record SearchQuote(
        [property: JsonPropertyName("symbol")] string? Symbol,
        [property: JsonPropertyName("shortname")] string? ShortName,
        [property: JsonPropertyName("longname")] string? LongName,
        [property: JsonPropertyName("exchange")] string? Exchange,
        [property: JsonPropertyName("exchDisp")] string? ExchDisp,
        [property: JsonPropertyName("quoteType")] string? QuoteType,
        [property: JsonPropertyName("typeDisp")] string? TypeDisp);
}
