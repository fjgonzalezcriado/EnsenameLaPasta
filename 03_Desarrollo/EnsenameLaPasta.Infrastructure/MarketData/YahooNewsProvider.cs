using System.Net.Http.Json;
using System.Text.Json.Serialization;
using EnsenameLaPasta.Application.Common.Dtos;
using EnsenameLaPasta.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace EnsenameLaPasta.Infrastructure.MarketData;

/// <summary>
/// Noticias por símbolo vía Yahoo Finance (HV-055), reutilizando el mismo endpoint de búsqueda
/// que <see cref="YahooInstrumentSearchProvider"/> (<c>/v1/finance/search</c>), que además de
/// "quotes" devuelve un array "news" cuando se pide con <c>newsCount &gt; 0</c>. No requiere
/// API key ni cliente HTTP adicional (comparte el HttpClient "YahooFinance").
/// Degrada sin lanzar: cobertura pobre para símbolos poco seguidos (small caps, listados
/// europeos) es esperable, no un error — mismo principio que <c>SsaPriceForecaster</c>.
/// </summary>
public sealed class YahooNewsProvider(IHttpClientFactory httpFactory, ILogger<YahooNewsProvider> logger) : INewsProvider
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<YahooNewsProvider> _logger = logger;

    public async Task<SymbolNews> GetNewsAsync(string symbol, int max = 8, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return new SymbolNews(symbol, false, [], "Símbolo vacío.");

        if (max < 1) max = 8;

        try
        {
            var client = _httpFactory.CreateClient(YahooFinanceProvider.HttpClientName);

            // Sin lang/region: fijarlos a es-ES/ES recorta la cobertura a solo fuentes en español
            // y deja sin nada a la mayoría de small/mid caps de la watchlist (MBOT, NUCL.SW…).
            // Sale sobre todo en inglés, pero con muchas más noticias por símbolo.
            var response = await client.GetAsync(
                $"/v1/finance/search?q={Uri.EscapeDataString(symbol)}&quotesCount=0&newsCount={max}",
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<NewsSearchResponse>(cancellationToken: cancellationToken);
            var raw = payload?.News ?? [];

            var items = raw
                .Where(n => !string.IsNullOrWhiteSpace(n.Title) && !string.IsNullOrWhiteSpace(n.Link))
                .Select(n => new NewsItem(
                    n.Title!,
                    n.Publisher,
                    n.Link!,
                    n.ProviderPublishTime is > 0
                        ? DateTimeOffset.FromUnixTimeSeconds(n.ProviderPublishTime.Value).UtcDateTime
                        : null,
                    n.RelatedTickers ?? []))
                .OrderByDescending(n => n.PublishedAtUtc)
                .Take(max)
                .ToList();

            if (items.Count == 0)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("Yahoo noticias {Symbol}: sin cobertura.", symbol);
                return new SymbolNews(symbol, false, [], "Sin noticias para este símbolo.");
            }

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Yahoo noticias {Symbol}: {Count} titulares.", symbol, items.Count);
            return new SymbolNews(symbol, true, items, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Noticias Yahoo {Symbol} fallaron.", symbol);
            return new SymbolNews(symbol, false, [], "No se pudieron cargar noticias.");
        }
    }

    // DTOs internos para deserializar el array "news" de /v1/finance/search.
    private sealed record NewsSearchResponse(
        [property: JsonPropertyName("news")] List<NewsSearchItem>? News);

    private sealed record NewsSearchItem(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("publisher")] string? Publisher,
        [property: JsonPropertyName("link")] string? Link,
        [property: JsonPropertyName("providerPublishTime")] long? ProviderPublishTime,
        [property: JsonPropertyName("relatedTickers")] List<string>? RelatedTickers);
}
