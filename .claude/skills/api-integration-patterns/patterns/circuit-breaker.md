# Pattern: Circuit Breaker

> **Pattern**: Circuit Breaker
> **Problema**: Llamadas repetidas a servicio caído
> **Solución**: Polly Circuit Breaker con estados Open/Half-Open/Closed

---

## 🎯 Problema

Sin circuit breaker, las llamadas a un servicio caído:
- **Agotan recursos** (threads, connections)
- **Aumentan latencia** (esperar timeout cada vez)
- **Cascada de fallos** (servicios dependientes también fallan)

```csharp
// ❌ Sin circuit breaker - cada llamada espera timeout (30s)
for (int i = 0; i < 100; i++)
{
    try
    {
        await _httpClient.GetAsync("https://servicio-caido.com");
        // Espera 30s timeout cada vez = 50 minutos total!
    }
    catch (HttpRequestException)
    {
        // Log error, pero sigue intentando...
    }
}
```

---

## ✅ Solución: Circuit Breaker Pattern

### Estados del Circuit Breaker

```
┌──────────────────────────────────────────────────────────────┐
│                    CLOSED (Normal)                            │
│  Llamadas pasan normalmente                                   │
│  Se cuenta nº de fallos consecutivos                          │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     │ N fallos consecutivos
                     ▼
┌──────────────────────────────────────────────────────────────┐
│                    OPEN (Circuito abierto)                    │
│  ⚠️ NO se hacen llamadas al servicio                          │
│  Retorna error inmediato (fail-fast)                          │
│  Espera T segundos antes de probar                            │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     │ Tras T segundos
                     ▼
┌──────────────────────────────────────────────────────────────┐
│                  HALF-OPEN (Probando)                         │
│  Permite 1 llamada de prueba                                  │
│  Si OK → CLOSED                                               │
│  Si FALLO → OPEN (vuelve a esperar T segundos)                │
└──────────────────────────────────────────────────────────────┘
```

---

## 1. Circuit Breaker Básico

### Configuración en Program.cs

```csharp
using Polly;
using Polly.Extensions.Http;

builder.Services.AddHttpClient<IBannerClient, BannerClient>()
    .AddPolicyHandler(GetCircuitBreakerPolicy());

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5, // Abre tras 5 fallos
            durationOfBreak: TimeSpan.FromSeconds(30), // Espera 30s antes de HALF-OPEN
            onBreak: (outcome, duration) =>
            {
                Console.WriteLine($"⚠️ Circuit ABIERTO por {duration.TotalSeconds}s tras {outcome.Exception?.Message}");
            },
            onReset: () =>
            {
                Console.WriteLine("✅ Circuit CERRADO - Servicio recuperado");
            },
            onHalfOpen: () =>
            {
                Console.WriteLine("🔄 Circuit HALF-OPEN - Probando servicio...");
            });
}
```

---

## 2. Advanced Circuit Breaker

### Configuración con Métricas

```csharp
static IAsyncPolicy<HttpResponseMessage> GetAdvancedCircuitBreakerPolicy(
    ILogger logger,
    TelemetryClient? telemetry = null)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .Or<TimeoutException>()
        .AdvancedCircuitBreakerAsync(
            failureThreshold: 0.5, // Abre si 50% de llamadas fallan
            samplingDuration: TimeSpan.FromSeconds(10), // En ventana de 10s
            minimumThroughput: 10, // Mínimo 10 llamadas en ventana
            durationOfBreak: TimeSpan.FromMinutes(1), // Espera 1min
            onBreak: (outcome, duration, context) =>
            {
                logger.LogError(
                    "⚠️ Circuit BREAKER ABIERTO por {Duration}s. Razón: {Reason}",
                    duration.TotalSeconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());

                telemetry?.TrackEvent("CircuitBreakerOpen", new Dictionary<string, string>
                {
                    ["Service"] = context.PolicyKey ?? "Unknown",
                    ["Duration"] = duration.TotalSeconds.ToString(),
                    ["Reason"] = outcome.Exception?.Message ?? "HTTP Error"
                });
            },
            onReset: context =>
            {
                logger.LogInformation("✅ Circuit BREAKER CERRADO - Servicio recuperado");

                telemetry?.TrackEvent("CircuitBreakerClosed", new Dictionary<string, string>
                {
                    ["Service"] = context.PolicyKey ?? "Unknown"
                });
            },
            onHalfOpen: () =>
            {
                logger.LogWarning("🔄 Circuit BREAKER HALF-OPEN - Probando servicio...");
            });
}
```

---

## 3. Combinar Retry + Circuit Breaker

### Pipeline Completo

```csharp
builder.Services.AddHttpClient<IBannerClient, BannerClient>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

// ⚠️ ORDEN IMPORTA:
// 1. Retry (interno) - Reintenta errores transitorios
// 2. Circuit Breaker (externo) - Protege si servicio cae completamente

// FLUJO:
// Request → Circuit Breaker → Retry → HttpClient
// Si Circuit OPEN → falla inmediatamente (no entra a Retry)
// Si Circuit CLOSED → Retry maneja errores transitorios
```

### Retry + Circuit Breaker + Timeout

```csharp
using Polly.Timeout;

builder.Services.AddHttpClient<IBannerClient, BannerClient>()
    .AddPolicyHandler(GetTimeoutPolicy())       // 1. Timeout (más interno)
    .AddPolicyHandler(GetRetryPolicy())         // 2. Retry
    .AddPolicyHandler(GetCircuitBreakerPolicy()); // 3. Circuit Breaker (más externo)

static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
}
```

---

## 4. Fallback Strategy

### Respuesta por Defecto cuando Circuit Abierto

```csharp
builder.Services.AddHttpClient<IBannerClient, BannerClient>()
    .AddPolicyHandler(GetFallbackPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

static IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy()
{
    return Policy<HttpResponseMessage>
        .Handle<BrokenCircuitException>() // Cuando el circuito está abierto
        .Or<HttpRequestException>()
        .FallbackAsync(
            fallbackValue: new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    message = "Servicio temporalmente no disponible",
                    usandoCache = true,
                    timestamp = DateTime.UtcNow
                }))
            },
            onFallbackAsync: async (outcome, context) =>
            {
                Console.WriteLine($"💾 Usando fallback por: {outcome.Exception?.Message}");
                await Task.CompletedTask;
            });
}
```

---

## 5. Polly v8 - Circuit Breaker (Nueva API)

```csharp
using Polly;
using Polly.CircuitBreaker;

builder.Services.AddResiliencePipeline("banner-api", pipelineBuilder =>
{
    pipelineBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
        FailureRatio = 0.5, // Abre si 50% de llamadas fallan
        SamplingDuration = TimeSpan.FromSeconds(10),
        MinimumThroughput = 10,
        BreakDuration = TimeSpan.FromMinutes(1),
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable),
        OnOpened = args =>
        {
            Console.WriteLine($"⚠️ Circuit ABIERTO");
            return default;
        },
        OnClosed = args =>
        {
            Console.WriteLine($"✅ Circuit CERRADO");
            return default;
        },
        OnHalfOpened = args =>
        {
            Console.WriteLine($"🔄 Circuit HALF-OPEN");
            return default;
        }
    });
});

builder.Services.AddHttpClient<IBannerClient, BannerClient>()
    .AddResilienceHandler("banner-api");
```

---

## 📊 Escenarios de Configuración

| Escenario | Failure Threshold | Sampling Duration | Break Duration |
|-----------|-------------------|-------------------|----------------|
| **Alta criticidad** | 30% (0.3) | 5s | 30s |
| **Uso normal** | 50% (0.5) | 10s | 1min |
| **Tolerante** | 70% (0.7) | 30s | 5min |

---

## 🎯 Casos de Uso

### Banner API (Crítico)

```csharp
.AdvancedCircuitBreakerAsync(
    failureThreshold: 0.3, // Abre si 30% falla
    samplingDuration: TimeSpan.FromSeconds(5),
    minimumThroughput: 5,
    durationOfBreak: TimeSpan.FromSeconds(30));
```

### Oracle HCM (Tolerante)

```csharp
.CircuitBreakerAsync(
    handledEventsAllowedBeforeBreaking: 10, // Abre tras 10 fallos
    durationOfBreak: TimeSpan.FromMinutes(5));
```

---

## ⚠️ Monitoreo y Alertas

### Métricas Importantes

```csharp
public class CircuitBreakerMonitor
{
    private static int _circuitOpenCount = 0;
    private static DateTime? _lastOpenTime = null;

    public static void OnBreak(DelegateResult<HttpResponseMessage> outcome, TimeSpan duration)
    {
        _circuitOpenCount++;
        _lastOpenTime = DateTime.UtcNow;

        // Alertar si el circuito se abre más de 3 veces en 1 hora
        if (_circuitOpenCount > 3)
        {
            // Enviar alerta a Slack/Teams/Email
            AlertingService.SendAlert(
                "⚠️ Circuit Breaker abierto 3+ veces en 1 hora",
                $"Servicio: Banner API\nÚltima apertura: {_lastOpenTime}");
        }
    }
}
```

---

## 🧪 Testing

```csharp
[Fact]
public async Task CircuitBreaker_Opens_After_Consecutive_Failures()
{
    // Arrange
    var callCount = 0;
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

    var httpClient = new HttpClient(mockHandler.Object);
    var circuitBreakerPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 3, durationOfBreak: TimeSpan.FromSeconds(10));

    // Act - primeros 3 fallos
    for (int i = 0; i < 3; i++)
    {
        try
        {
            await circuitBreakerPolicy.ExecuteAsync(() => httpClient.GetAsync("https://test.com"));
        }
        catch { }
    }

    // Act - 4ta llamada debe fallar inmediatamente con BrokenCircuitException
    var act = () => circuitBreakerPolicy.ExecuteAsync(() => httpClient.GetAsync("https://test.com"));

    // Assert
    await act.Should().ThrowAsync<BrokenCircuitException>();
}
```

---

## 📚 Referencias

- [Polly Circuit Breaker](https://www.pollydocs.org/strategies/circuit-breaker.html)
- [Martin Fowler: Circuit Breaker](https://martinfowler.com/bliki/CircuitBreaker.html)

---

*Pattern: circuit-breaker - STIC.IA v3.7.0*
