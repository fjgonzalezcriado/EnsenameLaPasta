using System.Net;
using System.Text;

namespace EnsenameLaPasta.Tests.Helpers;

/// <summary>
/// HttpMessageHandler de test que responde con una función configurable.
/// Permite simular respuestas HTTP determinísticas sin red.
/// </summary>
public sealed class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder = responder;

    public List<HttpRequestMessage> ReceivedRequests { get; } = [];

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
