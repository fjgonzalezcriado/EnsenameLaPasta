# Azure Functions - Configuracion y Patrones

> Skill: azure-config | Version: 3.1.0

Worker aislado (.NET 10) con triggers comunes.

---

## Program.cs (Isolated Worker)

```csharp
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddDbContext<AppDbContext>(o =>
            o.UseSqlServer(context.Configuration.GetConnectionString("Default")));
    })
    .Build();
await host.RunAsync();
```

## Timer Trigger

```csharp
[Function("ProcesarPendientes")]
public async Task Run(
    [TimerTrigger("0 */5 8-20 * * 1-5")] TimerInfo timer,
    CancellationToken ct)
{
    _logger.LogInformation("Timer ejecutado: {Time}", DateTime.UtcNow);
    await _service.ProcesarAsync(ct);
}
```

## HTTP Trigger

```csharp
[Function("GetBecas")]
public async Task<HttpResponseData> GetAll(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "becas")] HttpRequestData req,
    CancellationToken ct)
{
    var becas = await _service.GetAllAsync(ct);
    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(becas, ct);
    return response;
}
```

## Service Bus Trigger

```csharp
[Function("ProcesarMensaje")]
public async Task Run(
    [ServiceBusTrigger("cola", Connection = "ServiceBus")] ServiceBusReceivedMessage message,
    ServiceBusMessageActions actions, CancellationToken ct)
{
    try
    {
        var payload = message.Body.ToObjectFromJson<MiMensaje>();
        await _service.ProcesarAsync(payload, ct);
        await actions.CompleteMessageAsync(message, ct);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error procesando {MessageId}", message.MessageId);
        await actions.DeadLetterMessageAsync(message, cancellationToken: ct);
    }
}
```

---

*Pattern v3.1.0*
