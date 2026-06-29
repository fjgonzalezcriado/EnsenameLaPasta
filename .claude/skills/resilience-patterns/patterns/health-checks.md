# Health Checks - Endpoints Obligatorios

> Todo proyecto Comillas en produccion debe tener health checks configurados.

---

## Paquetes NuGet

```xml
<ItemGroup>
  <PackageReference Include="AspNetCore.HealthChecks.SqlServer" Version="8.*" />
  <PackageReference Include="AspNetCore.HealthChecks.Redis" Version="8.*" />
  <PackageReference Include="AspNetCore.HealthChecks.UI" Version="8.*" />
  <PackageReference Include="AspNetCore.HealthChecks.UI.Client" Version="8.*" />
  <PackageReference Include="AspNetCore.HealthChecks.UI.InMemory.Storage" Version="8.*" />
</ItemGroup>
```

---

## Endpoints

| Endpoint | Proposito | Checks | Uso |
|----------|-----------|--------|-----|
| `/health` | Estado general | Todos | Dashboards |
| `/health/ready` | Readiness | DB, Redis, Storage | K8s readinessProbe |
| `/health/live` | Liveness | Self check | K8s livenessProbe |

---

## Checks Obligatorios

### SQL Server

```csharp
services.AddHealthChecks()
    .AddSqlServer(
        connectionString: configuration.GetConnectionString("DefaultConnection")!,
        name: "sqlserver",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready", "db" });
```

### Self Check

```csharp
services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("OK"),
        tags: new[] { "live" });
```

---

## Checks Opcionales

### Redis

```csharp
.AddRedis(configuration.GetConnectionString("Redis")!,
    name: "redis", failureStatus: HealthStatus.Degraded,
    tags: new[] { "ready", "cache" })
```

### Azure Blob Storage

```csharp
.AddAzureBlobStorage(configuration["AzureStorage:ConnectionString"]!,
    name: "azure-blob", failureStatus: HealthStatus.Degraded,
    tags: new[] { "ready", "storage" })
```

### Custom Health Check

```csharp
public class CriticalTableHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public CriticalTableHealthCheck(ApplicationDbContext context)
        => _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var count = await _context.Configuraciones.CountAsync(ct);
            return count == 0
                ? HealthCheckResult.Degraded("Tabla vacia")
                : HealthCheckResult.Healthy($"OK - {count} registros");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Error accediendo tabla critica", ex);
        }
    }
}
```

---

## Configuracion en Program.cs

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "sqlserver", failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready", "db" })
    .AddCheck("self", () => HealthCheckResult.Healthy("OK"),
        tags: new[] { "live" });

app.MapHealthChecks("/health", new HealthCheckOptions
{ ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

---

## Respuesta JSON

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.123",
  "entries": {
    "sqlserver": { "status": "Healthy", "tags": ["ready", "db"] },
    "self": { "status": "Healthy", "description": "OK", "tags": ["live"] }
  }
}
```

| Estado | HTTP |
|--------|------|
| Healthy | 200 |
| Degraded | 200 |
| Unhealthy | 503 |

---

## Kubernetes / Docker

```yaml
livenessProbe:
  httpGet: { path: /health/live, port: 8080 }
  initialDelaySeconds: 10
  periodSeconds: 15
readinessProbe:
  httpGet: { path: /health/ready, port: 8080 }
  initialDelaySeconds: 5
  periodSeconds: 10
```

---

## Seguridad

Health endpoints NO deben exponer info sensible en produccion.

---

*Skill resilience-patterns v3.7.0 - Health Checks*
