# Example: Polly Avanzado - Resilience Pipeline

> **Patrón**: Retry + Circuit Breaker + Timeout + Fallback
> **Versión**: Polly v8+
> **Uso**: Máxima resiliencia para APIs críticas

---

## Pipeline Completo

```csharp
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using Polly.Timeout;

builder.Services.AddResiliencePipeline("critical-api", pipelineBuilder =>
{
    // 1. TIMEOUT (más interno - se ejecuta primero)
    pipelineBuilder.AddTimeout(TimeSpan.FromSeconds(10));

    // 2. RETRY con backoff exponencial + jitter
    pipelineBuilder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromSeconds(1),
        UseJitter = true,
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .Handle<TimeoutException>()
            .HandleResult(r =>
                r.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable),
        OnRetry = args =>
        {
            Console.WriteLine($"Retry {args.AttemptNumber} tras {args.RetryDelay}");
            return default;
        }
    });

    // 3. CIRCUIT BREAKER
    pipelineBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(10),
        MinimumThroughput = 10,
        BreakDuration = TimeSpan.FromMinutes(1),
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable),
        OnOpened = args =>
        {
            Console.WriteLine("⚠️ Circuit ABIERTO");
            return default;
        }
    });

    // 4. FALLBACK (más externo)
    pipelineBuilder.AddFallback(new FallbackStrategyOptions<HttpResponseMessage>
    {
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<BrokenCircuitException>(),
        FallbackAction = args =>
        {
            var fallbackResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    message = "Servicio temporalmente no disponible",
                    usandoFallback = true
                }))
            };

            return Outcome.FromResult(fallbackResponse);
        }
    });
});

// Uso en HttpClient
builder.Services.AddHttpClient<ICriticalApiClient, CriticalApiClient>()
    .AddResilienceHandler("critical-api");
```

---

## Con Telemetría (Application Insights)

```csharp
builder.Services.AddResiliencePipeline("api-with-telemetry", (pipelineBuilder, context) =>
{
    var telemetry = context.ServiceProvider.GetService<TelemetryClient>();

    pipelineBuilder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 3,
        OnRetry = args =>
        {
            telemetry?.TrackEvent("ApiRetry", new Dictionary<string, string>
            {
                ["Attempt"] = args.AttemptNumber.ToString(),
                ["Outcome"] = args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString() ?? "Unknown"
            });
            return default;
        }
    });
});
```

---

*Example: polly-advanced - STIC.IA v3.7.0*
