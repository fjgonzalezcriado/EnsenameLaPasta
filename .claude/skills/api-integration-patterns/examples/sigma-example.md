# Example: Integración con Sigma (Nóminas)

> **Caso de uso**: Obtener nóminas del mes actual
> **API**: Sigma
> **Autenticación**: HMAC (API Key + Secret)
> **Características**: Rate Limiting (429), Custom Authentication

---

## 1. Configuración

```json
{
  "SigmaApi": {
    "BaseUrl": "https://sigma.comillas.edu/api",
    "ApiKey": "{{KeyVault}}",
    "ApiSecret": "{{KeyVault}}",
    "TimeoutSeconds": 30,
    "MaxRetries": 5
  }
}
```

---

## 2. Registro con Retry para Rate Limit

```csharp
builder.Services.AddHttpClient<ISigmaClient, SigmaClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<SigmaApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
})
.AddPolicyHandler(Policy
    .HandleResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(
        retryCount: 5,
        sleepDurationProvider: (retryAttempt, outcome, context) =>
        {
            // Leer Retry-After header si existe
            if (outcome.Result.Headers.RetryAfter?.Delta.HasValue == true)
                return outcome.Result.Headers.RetryAfter.Delta.Value;

            // Backoff exponencial si no hay Retry-After
            return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
        },
        onRetryAsync: async (outcome, timespan, retryCount, context) =>
        {
            Console.WriteLine($"⚠️ Rate Limit (429). Reintento {retryCount} tras {timespan.TotalSeconds}s");
            await Task.CompletedTask;
        }));
```

---

## 3. Service

```csharp
public class PayrollService : IPayrollService
{
    private readonly ISigmaClient _sigmaClient;

    public async Task<PayrollSummaryDto> GetCurrentMonthPayrollAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var payrolls = await _sigmaClient.GetPayrollsAsync(now.Year, now.Month, ct);

        if (!payrolls.Any())
            throw new InvalidOperationException($"No hay nóminas para {now:yyyy-MM}");

        var currentPayroll = payrolls.First();

        return new PayrollSummaryDto(
            currentPayroll.PayrollId,
            currentPayroll.Year,
            currentPayroll.Month,
            currentPayroll.EmployeeCount,
            currentPayroll.TotalAmount,
            currentPayroll.Status);
    }
}
```

---

## 4. Rate Limit Response

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 60
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 0
X-RateLimit-Reset: 1644940800

{
  "error": "Rate limit exceeded",
  "message": "Demasiadas peticiones. Máximo 100 por minuto."
}
```

---

*Example: sigma-example - STIC.IA v3.7.0*
