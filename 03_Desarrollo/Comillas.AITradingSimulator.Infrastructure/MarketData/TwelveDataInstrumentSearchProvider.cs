using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Infrastructure.MarketData;

/// <summary>
/// Búsqueda de instrumentos vía Twelve Data (<c>/symbol_search</c>): devuelve símbolos
/// con la convención de Twelve Data (los que su feed/histórico aceptan). Reutiliza el
/// HttpClient "TwelveData".
/// </summary>
public sealed class TwelveDataInstrumentSearchProvider : IInstrumentSearchProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IOptionsMonitor<TwelveDataOptions> _options;
    private readonly ILogger<TwelveDataInstrumentSearchProvider> _logger;

    public TwelveDataInstrumentSearchProvider(
        IHttpClientFactory httpFactory,
        IOptionsMonitor<TwelveDataOptions> options,
        ILogger<TwelveDataInstrumentSearchProvider> logger)
    {
        _httpFactory = httpFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InstrumentSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var client = _httpFactory.CreateClient(TwelveDataProvider.HttpClientName);
        var apiKey = _options.CurrentValue.ApiKey;
        var keyParam = string.IsNullOrWhiteSpace(apiKey) ? string.Empty : $"&apikey={Uri.EscapeDataString(apiKey)}";

        var response = await client.GetAsync(
            $"/symbol_search?symbol={Uri.EscapeDataString(query)}&outputsize=20{keyParam}",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<SymbolSearchResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Twelve Data: respuesta de búsqueda vacía o no deserializable.");

        if (string.Equals(payload.Status, "error", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Twelve Data symbol_search error para '{Query}': {Message}", query, payload.Message);
            return [];
        }

        var data = payload.Data ?? [];
        return data
            .Where(d => !string.IsNullOrWhiteSpace(d.Symbol))
            .Select(d => new InstrumentSearchResult(
                d.Symbol!,
                d.InstrumentName ?? string.Empty,
                d.Exchange ?? string.Empty,
                d.InstrumentType ?? string.Empty))
            .ToList();
    }

    private sealed record SymbolSearchResponse(
        [property: JsonPropertyName("data")] List<SymbolSearchItem>? Data,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("message")] string? Message);

    private sealed record SymbolSearchItem(
        [property: JsonPropertyName("symbol")] string? Symbol,
        [property: JsonPropertyName("instrument_name")] string? InstrumentName,
        [property: JsonPropertyName("exchange")] string? Exchange,
        [property: JsonPropertyName("instrument_type")] string? InstrumentType);
}
