using System.Net;
using System.Text;

namespace Comillas.AITradingSimulator.Tests.Helpers;

/// <summary>
/// HttpMessageHandler de test que responde con una función configurable.
/// Permite simular respuestas HTTP determinísticas sin red.
/// </summary>
public sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public List<HttpRequestMessage> ReceivedRequests { get; } = new();

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    /// <summary>Atajo: responde siempre con el JSON dado y el status indicado (default 200).</summary>
    public static MockHttpMessageHandler Json(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        return new MockHttpMessageHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }

    /// <summary>Atajo: responde siempre con el status dado y un body vacío.</summary>
    public static MockHttpMessageHandler Status(HttpStatusCode status)
    {
        return new MockHttpMessageHandler(_ => new HttpResponseMessage(status));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ReceivedRequests.Add(request);
        return Task.FromResult(_responder(request));
    }
}
