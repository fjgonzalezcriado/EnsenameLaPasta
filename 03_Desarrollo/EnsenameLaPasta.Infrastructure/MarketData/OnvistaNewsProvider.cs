using System.Globalization;
using System.Xml.Linq;
using EnsenameLaPasta.Application.Common.Dtos;
using EnsenameLaPasta.Application.Common.Interfaces;
using EnsenameLaPasta.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnsenameLaPasta.Infrastructure.MarketData;

/// <summary>
/// Noticias por símbolo vía el feed RSS filtrado de onvista.de (HV-057), complemento de
/// <see cref="YahooNewsProvider"/> (HV-055). onvista no tiene API pública: el feed
/// <c>/news/feed/rss.xml?entityType=..&amp;entityValue=..</c> es RSS 2.0 estándar, verificado en
/// vivo para DAX (INDEX/20735) y SK Hynix GDR / HY9H.F (STOCK/12952618).
/// Solo cubre símbolos presentes en <see cref="OnvistaOptions.SymbolMap"/> (el ID de onvista no
/// se puede resolver desde el ticker). Degrada sin lanzar: sin mapeo, HTTP no-2xx, XML inválido
/// o feed vacío devuelven lista vacía — el agregador (<see cref="INewsAggregatorService"/>)
/// decide el mensaje combinado, mismo principio que <c>YahooNewsProvider</c>.
/// </summary>
public sealed class OnvistaNewsProvider(
    IHttpClientFactory httpFactory,
    IOptions<OnvistaOptions> options,
    ILogger<OnvistaNewsProvider> logger) : IOnvistaNewsProvider
{
    public const string HttpClientName = "Onvista";
    private const string Source = "onvista";

    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly OnvistaOptions _options = options.Value;
    private readonly ILogger<OnvistaNewsProvider> _logger = logger;

    public async Task<IReadOnlyList<NewsItem>> GetNewsAsync(string symbol, int max = 8, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return [];

        if (!_options.SymbolMap.TryGetValue(symbol, out var mapping))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("onvista noticias {Symbol}: sin mapeo configurado.", symbol);
            return [];
        }

        if (max < 1) max = 8;

        try
        {
            var client = _httpFactory.CreateClient(HttpClientName);
            var response = await client.GetAsync(
                $"/news/feed/rss.xml?entityType={Uri.EscapeDataString(mapping.EntityType)}&entityValue={mapping.EntityValue}",
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var xml = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = XDocument.Parse(xml);

            var items = doc.Descendants("item")
                .Select(item => new
                {
                    Title = (string?)item.Element("title"),
                    Link = (string?)item.Element("link"),
                    PubDate = (string?)item.Element("pubDate"),
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Title) && !string.IsNullOrWhiteSpace(x.Link))
                .Select(x => new NewsItem(
                    x.Title!,
                    Source,
                    x.Link!,
                    ParsePubDate(x.PubDate),
                    [],
                    Source))
                .OrderByDescending(n => n.PublishedAtUtc)
                .Take(max)
                .ToList();

            if (items.Count == 0 && _logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("onvista noticias {Symbol}: feed sin items.", symbol);

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Noticias onvista {Symbol} fallaron.", symbol);
            return [];
        }
    }

    // RFC822 ("Mon, 02 Nov 2026 08:45:49 +0100"), formato estándar de pubDate en RSS 2.0.
    private static DateTime? ParsePubDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.UtcDateTime
            : null;
    }
}
