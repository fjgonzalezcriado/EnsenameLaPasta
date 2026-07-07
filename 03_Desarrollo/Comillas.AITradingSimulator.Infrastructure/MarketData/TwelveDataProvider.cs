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
/// Proveedor de datos en vivo vía Twelve Data (endpoint <c>/quote</c>): precio de
/// cierre, volumen y divisa en una sola llamada. Alternativa a Yahoo, seleccionable
/// con <see cref="MarketDataOptions.ProviderType"/> == "TwelveData".
/// </summary>
public sealed class TwelveDataProvider(
    IHttpClientFactory httpFactory,
    IOptionsMonitor<TwelveDataOptions> options,
    ILogger<TwelveDataProvider> logger) : IMarketDataProvider
{
    public const string HttpClientName = "TwelveData";

    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly IOptionsMonitor<TwelveDataOptions> _options = options;
    private readonly ILogger<TwelveDataProvider> _logger = logger;

    public async Task<MarketQuote> GetLatestAsync(string symbol, DateTime timestampUtc, CancellationToken cancellationToken = default)
    {
        var apiKey = _options.CurrentValue.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "Twelve Data: falta la API key (MarketData:TwelveData:ApiKey). Regístrate en twelvedata.com para obtener una.");

        var client = _httpFactory.CreateClient(HttpClientName);

        // /quote devuelve precio (close), volumen y divisa en una llamada.
        var response = await client.GetAsync(
            $"/quote?symbol={Uri.EscapeDataString(symbol)}&apikey={Uri.EscapeDataString(apiKey)}",
            cancellationToken);

        // Twelve Data devuelve el error como JSON { status, code, message } tanto con 200
        // como con 4xx (p.ej. símbolo no disponible en el plan). Lo parseamos igualmente y
        // lanzamos un mensaje claro (no un "404 Not Found" genérico con stack).
        var quote = await response.Content.ReadFromJsonAsync<TwelveQuote>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException($"Twelve Data: respuesta vacía para '{symbol}' (HTTP {(int)response.StatusCode}).");

        if (string.Equals(quote.Status, "error", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Twelve Data '{symbol}': {quote.Message}");

        if (string.IsNullOrWhiteSpace(quote.Close)
            || !decimal.TryParse(quote.Close, NumberStyles.Any, CultureInfo.InvariantCulture, out var price)
            || price <= 0)
            throw new InvalidOperationException($"Twelve Data: precio inválido para '{symbol}'.");

        decimal volume = 0m;
        if (!string.IsNullOrWhiteSpace(quote.Volume))
            decimal.TryParse(quote.Volume, NumberStyles.Any, CultureInfo.InvariantCulture, out volume);

        _logger.LogDebug("Twelve Data: {Symbol} = {Price} {Currency} (vol {Volume})", symbol, price, quote.Currency, volume);

        return new MarketQuote(MarketTick.Create(symbol, price, volume, timestampUtc), quote.Currency);
    }

    // DTO para deserializar /quote (los valores numéricos llegan como string).
    private sealed record TwelveQuote(
        [property: JsonPropertyName("close")] string? Close,
        [property: JsonPropertyName("volume")] string? Volume,
        [property: JsonPropertyName("currency")] string? Currency,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("code")] int? Code,
        [property: JsonPropertyName("message")] string? Message);
}
