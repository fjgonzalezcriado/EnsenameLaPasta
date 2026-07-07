using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Infrastructure.MarketData;

/// <summary>
/// Búsqueda de instrumentos vía Alpha Vantage (<c>SYMBOL_SEARCH</c>): devuelve símbolos con la
/// convención de Alpha Vantage. Reutiliza el HttpClient "AlphaVantage".
/// </summary>
public sealed class AlphaVantageInstrumentSearchProvider : IInstrumentSearchProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IOptionsMonitor<AlphaVantageOptions> _options;
    private readonly ILogger<AlphaVantageInstrumentSearchProvider> _logger;

    public AlphaVantageInstrumentSearchProvider(
        IHttpClientFactory httpFactory,
        IOptionsMonitor<AlphaVantageOptions> options,
        ILogger<AlphaVantageInstrumentSearchProvider> logger)
    {
        _httpFactory = httpFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InstrumentSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var apiKey = _options.CurrentValue.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Alpha Vantage symbol_search: falta la API key; sin resultados.");
            return [];
        }

        var client = _httpFactory.CreateClient(AlphaVantageProvider.HttpClientName);
        var response = await client.GetAsync(
            $"/query?function=SYMBOL_SEARCH&keywords={Uri.EscapeDataString(query)}&apikey={Uri.EscapeDataString(apiKey)}",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<SymbolSearchResponse>(cancellationToken: cancellationToken);
        if (payload is null)
            return [];

        var apiMessage = payload.Note ?? payload.Information ?? payload.ErrorMessage;
        if (!string.IsNullOrWhiteSpace(apiMessage))
        {
            _logger.LogWarning("Alpha Vantage symbol_search para '{Query}': {Message}", query, apiMessage);
            return [];
        }

        var matches = payload.BestMatches ?? [];
        return matches
            .Where(m => !string.IsNullOrWhiteSpace(m.Symbol))
            .Select(m => new InstrumentSearchResult(
                m.Symbol!,
                m.Name ?? string.Empty,
                m.Region ?? string.Empty,
                m.Type ?? string.Empty))
            .ToList();
    }

    private sealed record SymbolSearchResponse(
        [property: JsonPropertyName("bestMatches")] List<SymbolMatch>? BestMatches,
        [property: JsonPropertyName("Note")] string? Note,
        [property: JsonPropertyName("Information")] string? Information,
        [property: JsonPropertyName("Error Message")] string? ErrorMessage);

    private sealed record SymbolMatch(
        [property: JsonPropertyName("1. symbol")] string? Symbol,
        [property: JsonPropertyName("2. name")] string? Name,
        [property: JsonPropertyName("3. type")] string? Type,
        [property: JsonPropertyName("4. region")] string? Region);
}
