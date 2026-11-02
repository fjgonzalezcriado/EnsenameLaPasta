using System.Net;
using EnsenameLaPasta.Application.Common.Options;
using EnsenameLaPasta.Infrastructure.MarketData;
using EnsenameLaPasta.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnsenameLaPasta.Tests.Infrastructure;

public class OnvistaNewsProviderTests
{
    private static readonly Uri BaseUrl = new("https://www.onvista.de");

    private static OnvistaOptions MappedOptions() => new()
    {
        SymbolMap = new Dictionary<string, OnvistaSymbolMapping>
        {
            ["HY9H.F"] = new OnvistaSymbolMapping { EntityType = "STOCK", EntityValue = 12952618 }
        }
    };

    private static OnvistaNewsProvider NewProvider(MockHttpMessageHandler handler, OnvistaOptions? options = null)
    {
        var factory = new TestHttpClientFactory(handler, BaseUrl);
        return new OnvistaNewsProvider(factory, Options.Create(options ?? MappedOptions()), NullLogger<OnvistaNewsProvider>.Instance);
    }

    // Forma real del feed /news/feed/rss.xml (RSS 2.0, pubDate en RFC822).
    private const string ValidRss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
          <channel>
            <item>
              <title><![CDATA[SK Hynix: Langfristvertraege stuetzen das Ziel von 1.200 Euro]]></title>
              <link>https://www.onvista.de/news/1</link>
              <pubDate>Wed, 28 Oct 2026 15:50:46 +0100</pubDate>
            </item>
            <item>
              <title><![CDATA[SK Hynix: Die niedrige Bewertung macht die Aktie kaufenswert]]></title>
              <link>https://www.onvista.de/news/2</link>
              <pubDate>Mon, 26 Oct 2026 16:00:32 +0100</pubDate>
            </item>
          </channel>
        </rss>
        """;

    [Fact]
    public async Task GetNewsAsync_RespuestaValida_MapeaYOrdenaPorFechaDesc()
    {
        var provider = NewProvider(MockHttpMessageHandler.Xml(ValidRss));

        var items = await provider.GetNewsAsync("HY9H.F");

        Assert.Equal(2, items.Count);
        Assert.Equal("SK Hynix: Langfristvertraege stuetzen das Ziel von 1.200 Euro", items[0].Title);
        Assert.Equal("https://www.onvista.de/news/1", items[0].Link);
        Assert.Equal("onvista", items[0].Source);
        Assert.Equal(new DateTime(2026, 10, 28, 14, 50, 46, DateTimeKind.Utc), items[0].PublishedAtUtc);
    }

    [Fact]
    public async Task GetNewsAsync_SimboloSinMapeo_DevuelveVacioSinLlamar()
    {
        var handler = MockHttpMessageHandler.Xml(ValidRss);
        var provider = NewProvider(handler);

        var items = await provider.GetNewsAsync("AAPL");

        Assert.Empty(items);
        Assert.Empty(handler.ReceivedRequests);
    }

    [Fact]
    public async Task GetNewsAsync_SimboloVacio_DevuelveVacioSinLlamar()
    {
        var handler = MockHttpMessageHandler.Xml(ValidRss);
        var provider = NewProvider(handler);

        var items = await provider.GetNewsAsync("   ");

        Assert.Empty(items);
        Assert.Empty(handler.ReceivedRequests);
    }

    [Fact]
    public async Task GetNewsAsync_Http500_DegradaSinLanzar()
    {
        var provider = NewProvider(MockHttpMessageHandler.Status(HttpStatusCode.InternalServerError));

        var items = await provider.GetNewsAsync("HY9H.F");

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetNewsAsync_XmlMalformado_DegradaSinLanzar()
    {
        var provider = NewProvider(MockHttpMessageHandler.Xml("<rss><channel><item><title>Sin cerrar"));

        var items = await provider.GetNewsAsync("HY9H.F");

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetNewsAsync_FeedVacio_DevuelveVacio()
    {
        const string empty = """<?xml version="1.0"?><rss version="2.0"><channel></channel></rss>""";
        var provider = NewProvider(MockHttpMessageHandler.Xml(empty));

        var items = await provider.GetNewsAsync("HY9H.F");

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetNewsAsync_ItemSinTituloOLink_SeFiltra()
    {
        const string xml = """
            <rss version="2.0"><channel>
              <item><link>https://x.com/1</link><pubDate>Wed, 28 Oct 2026 10:00:00 +0100</pubDate></item>
              <item><title>Sin link</title><pubDate>Wed, 28 Oct 2026 10:00:00 +0100</pubDate></item>
              <item><title>Valida</title><link>https://x.com/2</link><pubDate>Wed, 28 Oct 2026 10:00:00 +0100</pubDate></item>
            </channel></rss>
            """;
        var provider = NewProvider(MockHttpMessageHandler.Xml(xml));

        var items = await provider.GetNewsAsync("HY9H.F");

        Assert.Single(items);
        Assert.Equal("Valida", items[0].Title);
    }

    [Fact]
    public async Task GetNewsAsync_RespetaElMaxIndicado()
    {
        const string xml = """
            <rss version="2.0"><channel>
              <item><title>Uno</title><link>https://x.com/1</link><pubDate>Wed, 28 Oct 2026 12:00:00 +0100</pubDate></item>
              <item><title>Dos</title><link>https://x.com/2</link><pubDate>Wed, 28 Oct 2026 11:00:00 +0100</pubDate></item>
              <item><title>Tres</title><link>https://x.com/3</link><pubDate>Wed, 28 Oct 2026 10:00:00 +0100</pubDate></item>
            </channel></rss>
            """;
        var provider = NewProvider(MockHttpMessageHandler.Xml(xml));

        var items = await provider.GetNewsAsync("HY9H.F", max: 2);

        Assert.Equal(2, items.Count);
        Assert.Equal("Uno", items[0].Title);
        Assert.Equal("Dos", items[1].Title);
    }

    [Fact]
    public async Task GetNewsAsync_LlamaAlEndpointEsperadoConEntityTypeYValue()
    {
        var handler = MockHttpMessageHandler.Xml(ValidRss);
        var provider = NewProvider(handler);

        await provider.GetNewsAsync("HY9H.F");

        var request = Assert.Single(handler.ReceivedRequests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Contains("/news/feed/rss.xml", request.RequestUri!.AbsolutePath);
        Assert.Contains("entityType=STOCK", request.RequestUri.Query);
        Assert.Contains("entityValue=12952618", request.RequestUri.Query);
    }
}
