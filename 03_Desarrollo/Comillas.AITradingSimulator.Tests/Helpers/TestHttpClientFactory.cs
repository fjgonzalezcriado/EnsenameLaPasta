namespace Comillas.AITradingSimulator.Tests.Helpers;

/// <summary>
/// IHttpClientFactory de test que devuelve siempre el mismo HttpClient envolviendo
/// un HttpMessageHandler determinístico. Suficiente para tests de providers.
/// </summary>
public sealed class TestHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _client;

    public TestHttpClientFactory(HttpMessageHandler handler, Uri baseUrl)
    {
        _client = new HttpClient(handler) { BaseAddress = baseUrl };
    }

    public HttpClient CreateClient(string name) => _client;
}
