# Pattern: Retry Policy

> **Pattern**: Retry con backoff exponencial
> **Problema**: Errores transitorios en APIs externas
> **Solución**: Polly v8+ con resilience pipelines

---

## 🎯 Problema

Las APIs externas pueden fallar temporalmente:
- **429 Rate Limit Exceeded** - Demasiadas peticiones
- **503 Service Unavailable** - Servicio temporalmente caído
- **TimeoutException** - Red lenta o servicio ocupado
- **Network errors** - Pérdida temporal de conectividad

```csharp
// ❌ SIN retry - falla al primer error
var response = await _httpClient.GetAsync("https://api.comillas.edu/data");
response.EnsureSuccessStatusCode(); // Lanza excepción si hay error
```

---

## ✅ Solución: Polly Retry Policy

### ⚠️ IMPORTANTE: Polly v8+

```xml
<!-- Polly v8+ - Resilience Pipelines (nueva API) -->
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.*" />
<PackageReference Include="Polly.Extensions.Http" Version="3.*" />
```

> **Migración**: Polly v8 introduce `ResiliencePipeline` en lugar de `Policy<T>`.
> La API legacy sigue funcionando pero es recomendable migrar.

---

## Estrategias de Retry

| Estrategia | Uso | Espera entre reintentos |
|------------|-----|-------------------------|
| **Retry Simple** | Errores poco frecuentes | Fija (ej: 1s, 1s, 1s) |
| **Exponential Backoff** | Rate limits, congestión | Creciente (1s, 2s, 4s, 8s) |
| **Wait and Retry** | Errores transitorios | Configurable |
| **Jitter** | Evitar thundering herd | Backoff + aleatorio |

---

## 1. Retry Simple (3 intentos, espera fija)

### Configuración en Program.cs

```csharp
using Polly;
using Polly.Extensions.Http;

builder.Services.AddHttpClient<IBannerClient, BannerClient>()
    .AddPolicyHandler(GetRetryPolicy());

// Método helper
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // 5xx, 408, 429
        .Or<TimeoutException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                var logger = context.GetLogger();
                logger?.LogWarning(
                    "Reintento {RetryCount} tras {Delay}ms. Motivo: {Reason}",
                    retryCount,
                    timespan.TotalMilliseconds,
                    outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString());
            });
}
```

---

## 2. Exponential Backoff con Jitter

### Configuración Avanzada

```csharp
static IAsyncPolicy<HttpResponseMessage> GetAdvancedRetryPolicy()
{
    var jitterer = new Random();

    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .Or<TimeoutException>()
        .WaitAndRetryAsync(
            retryCount: 5,
            sleepDurationProvider: (retryAttempt, context) =>
            {
                // Backoff exponencial: 1s, 2s, 4s, 8s, 16s
                var exponentialDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));

                // Jitter: ±20% aleatorio para evitar thundering herd
                var jitter = TimeSpan.FromMilliseconds(
                    jitterer.Next(0, (int)(exponentialDelay.TotalMilliseconds * 0.2)));

                return exponentialDelay + jitter;
            },
            onRetryAsync: async (outcome, timespan, retryCount, context) =>
            {
                var logger = context.GetLogger();

                logger?.LogWarning(
                    "Reintento {RetryCount}/{MaxRetries} tras {Delay}ms. " +
                    "StatusCode: {StatusCode}, Exception: {Exception}",
                    retryCount,
                    5,
                    timespan.TotalMilliseconds,
                    outcome.Result?.StatusCode,
                    outcome.Exception?.Message);

                // Opcional: métricas
                var telemetry = context.GetTelemetryClient();
                telemetry?.TrackEvent("HttpRetry", new Dictionary<string, string>
                {
                    ["RetryCount"] = retryCount.ToString(),
                    ["StatusCode"] = outcome.Result?.StatusCode.ToString() ?? "N/A"
                });

                await Task.CompletedTask;
            });
}
```

---

## 3. Retry Solo para Códigos Específicos

```csharp
static IAsyncPolicy<HttpResponseMessage> GetSelectiveRetryPolicy()
{
    return Policy
        .HandleResult<HttpResponseMessage>(r =>
            r.StatusCode == System.Net.HttpStatusCode.TooManyRequests || // 429
            r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || // 503
            r.StatusCode == System.Net.HttpStatusCode.GatewayTimeout) // 504
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"Reintento {retryCount} tras {timespan.TotalSeconds}s debido a {outcome.Result.StatusCode}");
            });
}
```

---

## 4. Retry con Condiciones Personalizadas

```csharp
static IAsyncPolicy<HttpResponseMessage> GetCustomRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(response =>
        {
            // Reintentar si el response body indica error temporal
            var content = response.Content.ReadAsStringAsync().Result;
            return content.Contains("TemporaryError") || content.Contains("TryAgain");
        })
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(2 * retryAttempt),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"Retry {retryCount} - Custom condition met");
            });
}
```

---

## 5. Polly v8 - Resilience Pipelines (Nueva API)

### Configuración Polly v8+

```csharp
using Polly;
using Polly.Retry;

builder.Services.AddResiliencePipeline("banner-api", pipelineBuilder =>
{
    pipelineBuilder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromSeconds(1),
        UseJitter = true,
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .Handle<TimeoutException>()
            .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests),
        OnRetry = args =>
        {
            Console.WriteLine($"Retry {args.AttemptNumber} tras {args.RetryDelay}");
            return default;
        }
    });
});

// Uso en HttpClient
builder.Services.AddHttpClient<IBannerClient, BannerClient>()
    .AddResilienceHandler("banner-api");
```

---

## 📊 Comparativa Estrategias

| Estrategia | 1er retry | 2do retry | 3er retry | 4to retry | Total |
|------------|-----------|-----------|-----------|-----------|-------|
| **Fijo (1s)** | 1s | 1s | 1s | 1s | ~4s |
| **Exponencial** | 1s | 2s | 4s | 8s | ~15s |
| **Exponencial + Jitter** | 1.2s | 2.3s | 3.8s | 9.1s | ~16.4s |

---

## 🎯 Casos de Uso

### Banner API (Rate Limit 429)

```csharp
builder.Services.AddHttpClient<IBannerClient, BannerClient>()
    .AddPolicyHandler(Policy
        .HandleResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 5,
            sleepDurationProvider: retryAttempt =>
            {
                // Leer Retry-After header si existe
                if (outcome.Result.Headers.RetryAfter?.Delta.HasValue == true)
                    return outcome.Result.Headers.RetryAfter.Delta.Value;

                // Backoff exponencial por defecto
                return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
            }));
```

### Oracle HCM (Timeouts)

```csharp
builder.Services.AddHttpClient<IOracleHcmClient, OracleHcmClient>()
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .Or<TimeoutException>()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(5 * retryAttempt)));
```

---

## ⚠️ Errores NO Recuperables (NO reintentar)

```csharp
// ❌ NO reintentar estos códigos:
// 400 Bad Request - Request inválida
// 401 Unauthorized - Credenciales incorrectas
// 403 Forbidden - Sin permisos
// 404 Not Found - Recurso no existe
// 422 Unprocessable Entity - Validación fallida

static bool ShouldNotRetry(HttpResponseMessage response)
{
    var nonRetryableStatusCodes = new[]
    {
        System.Net.HttpStatusCode.BadRequest,
        System.Net.HttpStatusCode.Unauthorized,
        System.Net.HttpStatusCode.Forbidden,
        System.Net.HttpStatusCode.NotFound,
        (System.Net.HttpStatusCode)422 // Unprocessable Entity
    };

    return nonRetryableStatusCodes.Contains(response.StatusCode);
}
```

---

## 🧪 Testing

```csharp
[Fact]
public async Task HttpClient_With_Retry_Succeeds_On_Third_Attempt()
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
        .ReturnsAsync(() =>
        {
            callCount++;
            if (callCount < 3)
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Success")
            };
        });

    var httpClient = new HttpClient(mockHandler.Object);
    var retryPolicy = GetRetryPolicy();

    // Act
    var response = await retryPolicy.ExecuteAsync(() => httpClient.GetAsync("https://test.com"));

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    callCount.Should().Be(3);
}
```

---

## 📚 Referencias

- [Polly v8 docs](https://www.pollydocs.org/)
- [Resilience Pipelines](https://www.pollydocs.org/strategies/retry.html)
- [HttpClientFactory + Polly](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly)

---

*Pattern: retry-policy - STIC.IA v3.7.0*
