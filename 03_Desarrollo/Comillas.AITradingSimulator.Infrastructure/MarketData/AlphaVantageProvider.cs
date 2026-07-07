using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Infrastructure.MarketData;

/// <summary>
/// Proveedor de datos en vivo vía Alpha Vantage (<c>GLOBAL_QUOTE</c>): precio y volumen en una
/// llamada. Tercer proveedor (Fase 3), seleccionable con <see cref="MarketDataOptions.ProviderType"/>
/// == "AlphaVantage". No informa la divisa (GLOBAL_QUOTE no la incluye) → se deja vacía.
/// </summary>
public sealed class AlphaVantageProvider(
    IHttpClientFactory httpFactory,
    IOptionsMonitor<AlphaVantageOptions> options,
    ILogger<AlphaVantageProvider> logger) : IMarketDataProvider
{
    public const string HttpClientName = "AlphaVantage";

    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly IOptionsMonitor<AlphaVantageOptions> _options = options;
    private readonly ILogger<AlphaVantageProvider> _logger = logger;

    public async Task<MarketQuote> GetLatestAsync(string symbol, DateTime timestampUtc, CancellationToken cancellationToken = default)
    {
        var apiKey = _options.CurrentValue.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "Alpha Vantage: falta la API key (MarketData:AlphaVantage:ApiKey). Regístrate en alphavantage.co para obtener una.");

        var client = _httpFactory.CreateClient(HttpClientName);

        // Alpha Vantage devuelve HTTP 200 también en errores/límite de cuota: el detalle va en
        // el cuerpo JSON (Note / Information / Error Message). Lo parseamos igualmente.
        var response = await client.GetAsync(
            $"/query?function=GLOBAL_QUOTE&symbol={Uri.EscapeDataString(symbol)}&apikey={Uri.EscapeDataString(apiKey)}",
            cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<GlobalQuoteResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException($"Alpha Vantage: respuesta vacía para '{symbol}' (HTTP {(int)response.StatusCode}).");

        // Límite de cuota (plan gratuito: 25/día) o error → mensaje claro (no un genérico).
        var apiMessage = payload.Note ?? payload.Information ?? payload.ErrorMessage;
        if (!string.IsNullOrWhiteSpace(apiMessage))
            throw new InvalidOperationException($"Alpha Vantage '{symbol}': {apiMessage}");

        var quote = payload.GlobalQuote;
        if (quote?.Price is null
            || !decimal.TryParse(quote.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var price)
            || price <= 0)
            throw new InvalidOperationException($"Alpha Vantage: precio inválido o símbolo no encontrado para '{symbol}'.");

        decimal volume = 0m;
        if (!string.IsNullOrWhiteSpace(quote.Volume))
            decimal.TryParse(quote.Volume, NumberStyles.Any, CultureInfo.InvariantCulture, out volume);

        _logger.LogDebug("Alpha Vantage: {Symbol} = {Price} (vol {Volume})", symbol, price, volume);

        // GLOBAL_QUOTE no trae divisa → vacía (el generador no pisa la divisa si viene vacía).
        return new MarketQuote(MarketTick.Create(symbol, price, volume, timestampUtc), string.Empty);
    }

    // DTO para GLOBAL_QUOTE (claves con espacios/números; valores numéricos como string).
    private sealed record GlobalQuoteResponse(
        [property: JsonPropertyName("Global Quote")] GlobalQuoteData? GlobalQuote,
        [property: JsonPropertyName("Note")] string? Note,
        [property: JsonPropertyName("Information")] string? Information,
        [property: JsonPropertyName("Error Message")] string? ErrorMessage);

    private sealed record GlobalQuoteData(
        [property: JsonPropertyName("05. price")] string? Price,
        [property: JsonPropertyName("06. volume")] string? Volume);
}
