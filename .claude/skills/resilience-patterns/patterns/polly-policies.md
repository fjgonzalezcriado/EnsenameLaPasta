# Polly v8+ - Politicas de Resiliencia

> **IMPORTANTE**: Polly v8 usa la nueva API de **Resilience Pipeline** (no la antigua Policy API de Polly v7).
> Todos los ejemplos usan la API moderna con `AddResilienceHandler` y `ResiliencePipelineBuilder`.

---

## Paquetes NuGet Necesarios

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="8.*" />
  <PackageReference Include="Microsoft.Extensions.Resilience" Version="8.*" />
  <PackageReference Include="Polly" Version="8.*" />
</ItemGroup>
```

---

## 1. Retry - Reintentos con Backoff Exponencial

3 intentos, backoff exponencial (1s, 2s, 4s), con jitter para evitar thundering herd.

```csharp
builder.Services.AddHttpClient<IMiServicio, MiServicio>()
    .AddResilienceHandler("retry-pipeline", pipeline =>
    {
        pipeline.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>()
                .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                    || r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
                    || r.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)
        });
    });
```

---

## 2. Circuit Breaker

5 fallos en 30s -> abierto 30s -> half-open.

```csharp
pipeline.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
{
    FailureRatio = 0.5,
    SamplingDuration = TimeSpan.FromSeconds(30),
    MinimumThroughput = 5,
    BreakDuration = TimeSpan.FromSeconds(30),
    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
        .Handle<HttpRequestException>()
        .Handle<TimeoutRejectedException>()
        .HandleResult(r => (int)r.StatusCode >= 500)
});
```

---

## 3. Timeout

```csharp
pipeline.AddTimeout(new TimeoutStrategyOptions
{
    Timeout = TimeSpan.FromSeconds(30)
});
```

---

## 4. Bulkhead - Limitar Concurrencia

```csharp
pipeline.AddConcurrencyLimiter(new ConcurrencyLimiterOptions
{
    PermitLimit = 10,
    QueueLimit = 20
});
```

---

## 5. Rate Limiter

```csharp
pipeline.AddRateLimiter(new SlidingWindowRateLimiterOptions
{
    PermitLimit = 100,
    Window = TimeSpan.FromMinutes(1),
    SegmentsPerWindow = 6,
    QueueLimit = 10
});
```

---

## 6. Pipeline Combinado (Timeout -> Retry -> Circuit Breaker)

```csharp
builder.Services.AddHttpClient<IMiServicio, MiServicio>(client =>
{
    client.BaseAddress = new Uri(configuration["Servicios:MiServicio:BaseUrl"]!);
})
.AddResilienceHandler("servicio-completo", pipeline =>
{
    pipeline.AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(30) });

    pipeline.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .Handle<TimeoutRejectedException>()
            .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                || r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
    });

    pipeline.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 5,
        BreakDuration = TimeSpan.FromSeconds(30),
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .HandleResult(r => (int)r.StatusCode >= 500)
    });
});
```

---

## 7. HttpClientFactory con Resiliencia Estandar

```csharp
builder.Services.AddHttpClient<IMiServicio, MiServicio>()
    .AddStandardResilienceHandler();

// Con configuracion personalizada
builder.Services.AddHttpClient<IMiServicio, MiServicio>()
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 5;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(60);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
    });
```

---

## 8. Configuracion via appsettings.json

```json
{
  "Resilience": {
    "Default": {
      "Retry": { "MaxRetryAttempts": 3, "DelaySeconds": 1 },
      "CircuitBreaker": { "FailureRatio": 0.5, "BreakDurationSeconds": 30 },
      "Timeout": { "TimeoutSeconds": 30 }
    }
  }
}
```

---

## Resumen

| Estrategia | Proposito | Config defecto |
|------------|-----------|---------------|
| **Retry** | Transient faults | 3 intentos, exponencial + jitter |
| **Circuit Breaker** | Servicio saturado | 5 fallos/30s, abierto 30s |
| **Timeout** | Tiempo espera | 30 segundos |
| **Bulkhead** | Concurrencia | 10 concurrentes, 20 cola |
| **Rate Limiter** | Tasa requests | 100/minuto |

---

*Skill resilience-patterns v3.7.0 - Polly Policies*
