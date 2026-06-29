# Health Checks para Servicios Azure

> Skill: azure-config
> Versión: 2.8.0

---

## Descripción

Configuración de health checks para monitorizar la disponibilidad
de servicios Azure: SQL, Blob Storage, Redis, Key Vault, Service Bus.

---

## Configuración en Program.cs (.NET 10)

```csharp
var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════
// HEALTH CHECKS
// ═══════════════════════════════════════════════════════════════════════════

builder.Services.AddHealthChecks()
    // SQL Server / Azure SQL
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
        healthQuery: "SELECT 1",
        name: "sqlserver",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["db", "sql", "ready"])

    // Azure Blob Storage
    .AddAzureBlobStorage(
        connectionString: builder.Configuration["Azure:BlobStorage:ConnectionString"]!,
        containerName: "documentos",
        name: "blob-storage",
        failureStatus: HealthStatus.Degraded,
        tags: ["storage", "azure", "ready"])

    // Redis
    .AddRedis(
        redisConnectionString: builder.Configuration["Redis:ConnectionString"]!,
        name: "redis",
        failureStatus: HealthStatus.Degraded,
        tags: ["cache", "redis", "ready"])

    // Azure Key Vault
    .AddAzureKeyVault(
        new Uri(builder.Configuration["KeyVault:Url"]!),
        new DefaultAzureCredential(),
        setup => { },
        name: "keyvault",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["secrets", "azure", "ready"])

    // Azure Service Bus
    .AddAzureServiceBusQueue(
        connectionString: builder.Configuration["Azure:ServiceBus:ConnectionString"]!,
        queueName: "mi-cola",
        name: "servicebus-queue",
        failureStatus: HealthStatus.Degraded,
        tags: ["messaging", "azure"])

    // Health check personalizado
    .AddCheck<CustomDependencyHealthCheck>(
        "external-api",
        failureStatus: HealthStatus.Degraded,
        tags: ["api", "external"]);

// ═══════════════════════════════════════════════════════════════════════════
// HEALTH CHECKS UI (Opcional)
// ═══════════════════════════════════════════════════════════════════════════

builder.Services.AddHealthChecksUI(setup =>
{
    setup.SetEvaluationTimeInSeconds(30);
    setup.MaximumHistoryEntriesPerEndpoint(50);
    setup.AddHealthCheckEndpoint("API", "/health");
})
.AddInMemoryStorage();

var app = builder.Build();

// ═══════════════════════════════════════════════════════════════════════════
// ENDPOINTS DE HEALTH
// ═══════════════════════════════════════════════════════════════════════════

// Endpoint simple para balanceadores
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false, // No ejecuta ningún check, solo verifica que la app responde
    ResponseWriter = WriteMinimalResponse
});

// Endpoint para checks de "readiness" (listos para recibir tráfico)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteDetailedResponse
});

// Endpoint completo con todos los checks
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteDetailedResponse
});

// UI de health checks
app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
    options.ApiPath = "/health-api";
});

// ═══════════════════════════════════════════════════════════════════════════
// RESPONSE WRITERS
// ═══════════════════════════════════════════════════════════════════════════

static Task WriteMinimalResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "text/plain";
    return context.Response.WriteAsync(report.Status.ToString());
}

static async Task WriteDetailedResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var response = new
    {
        status = report.Status.ToString(),
        totalDuration = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            duration = e.Value.Duration.TotalMilliseconds,
            description = e.Value.Description,
            exception = e.Value.Exception?.Message,
            data = e.Value.Data
        })
    };

    await context.Response.WriteAsJsonAsync(response);
}
```

---

## Health Check Personalizado

```csharp
public class CustomDependencyHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomDependencyHealthCheck> _logger;
    private readonly string _apiUrl;

    public CustomDependencyHealthCheck(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CustomDependencyHealthCheck> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _apiUrl = configuration["ExternalApi:HealthUrl"]!;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5)); // Timeout de 5 segundos

            var response = await _httpClient.GetAsync(_apiUrl, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("API externa respondiendo correctamente");
            }

            _logger.LogWarning("API externa respondió con status {StatusCode}", response.StatusCode);

            return HealthCheckResult.Degraded(
                $"API externa respondió con status {response.StatusCode}",
                data: new Dictionary<string, object>
                {
                    ["statusCode"] = (int)response.StatusCode,
                    ["url"] = _apiUrl
                });
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Timeout conectando con API externa");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error conectando con API externa");
            return HealthCheckResult.Unhealthy(
                "Error conectando con API externa",
                exception: ex);
        }
    }
}
```

---

## Health Check para Worker Service

```csharp
public class WorkerHealthCheck : IHealthCheck
{
    private static DateTime _lastExecution = DateTime.MinValue;
    private static readonly TimeSpan MaxInterval = TimeSpan.FromMinutes(10);

    public static void ReportExecution()
    {
        _lastExecution = DateTime.UtcNow;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_lastExecution == DateTime.MinValue)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "Worker no ha ejecutado todavía",
                data: new Dictionary<string, object>
                {
                    ["lastExecution"] = "never"
                }));
        }

        var timeSinceLastExecution = DateTime.UtcNow - _lastExecution;

        if (timeSinceLastExecution > MaxInterval)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Última ejecución hace {timeSinceLastExecution.TotalMinutes:F0} minutos",
                data: new Dictionary<string, object>
                {
                    ["lastExecution"] = _lastExecution,
                    ["minutesSinceLastExecution"] = timeSinceLastExecution.TotalMinutes
                }));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Última ejecución hace {timeSinceLastExecution.TotalSeconds:F0} segundos",
            data: new Dictionary<string, object>
            {
                ["lastExecution"] = _lastExecution,
                ["secondsSinceLastExecution"] = timeSinceLastExecution.TotalSeconds
            }));
    }
}
```

---

## Health Check para Base de Datos con Query

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(
        ApplicationDbContext context,
        ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verificar conexión
            await _context.Database.CanConnectAsync(cancellationToken);

            // Verificar que hay datos (opcional)
            var becasCount = await _context.Becas.CountAsync(cancellationToken);

            return HealthCheckResult.Healthy(
                "Base de datos accesible",
                data: new Dictionary<string, object>
                {
                    ["totalBecas"] = becasCount,
                    ["server"] = _context.Database.GetDbConnection().DataSource
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando base de datos");
            return HealthCheckResult.Unhealthy(
                "Error conectando con base de datos",
                exception: ex);
        }
    }
}
```

---

## Respuesta JSON Ejemplo

```json
{
  "status": "Healthy",
  "totalDuration": 245.32,
  "checks": [
    {
      "name": "sqlserver",
      "status": "Healthy",
      "duration": 45.21,
      "description": null,
      "exception": null,
      "data": {}
    },
    {
      "name": "redis",
      "status": "Healthy",
      "duration": 12.45,
      "description": null,
      "exception": null,
      "data": {}
    },
    {
      "name": "blob-storage",
      "status": "Healthy",
      "duration": 156.78,
      "description": null,
      "exception": null,
      "data": {}
    },
    {
      "name": "external-api",
      "status": "Degraded",
      "duration": 30.88,
      "description": "API externa respondió con status 503",
      "exception": null,
      "data": {
        "statusCode": 503,
        "url": "https://api.externa.com/health"
      }
    }
  ]
}
```

---

## Packages NuGet

```xml
<ItemGroup>
  <PackageReference Include="AspNetCore.HealthChecks.SqlServer" Version="8.*" />
  <PackageReference Include="AspNetCore.HealthChecks.Redis" Version="8.*" />
  <PackageReference Include="AspNetCore.HealthChecks.AzureKeyVault" Version="8.*" />
  <PackageReference Include="AspNetCore.HealthChecks.AzureStorage" Version="8.*" />
  <PackageReference Include="AspNetCore.HealthChecks.AzureServiceBus" Version="8.*" />
  <PackageReference Include="AspNetCore.HealthChecks.UI" Version="8.*" />
  <PackageReference Include="AspNetCore.HealthChecks.UI.InMemory.Storage" Version="8.*" />
</ItemGroup>
```

---

## Integración con Azure

### Application Insights

```csharp
// Los health checks se reportan automáticamente a App Insights
builder.Services.AddApplicationInsightsTelemetry();
```

### Azure Monitor Alerts

```bash
# Crear alerta cuando el health check falla
az monitor metrics alert create \
  --name "health-check-failed" \
  --resource-group mi-rg \
  --scopes /subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.Web/sites/<app> \
  --condition "avg HealthCheckStatus < 1" \
  --window-size 5m \
  --evaluation-frequency 1m
```

---

## Checklist

- [ ] `/health/live` para Kubernetes liveness probe
- [ ] `/health/ready` para readiness probe
- [ ] Timeouts configurados en checks externos
- [ ] Tags para filtrar por tipo de check
- [ ] Logging de errores en checks
- [ ] Alertas configuradas en Azure Monitor

---

*Pattern v1.0 - azure-config skill*
