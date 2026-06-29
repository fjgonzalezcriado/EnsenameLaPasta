# Graceful Shutdown - Cierre Controlado

> Las aplicaciones deben cerrarse de forma controlada en produccion.

---

## Principios

1. **No perder requests activos** - Drenar antes de cerrar
2. **Timeout 30s** - Forzar cierre tras 30 segundos
3. **Notificar load balancer** - Reportar Unhealthy
4. **Cerrar conexiones** - DB, cache, colas

---

## 1. IHostApplicationLifetime

| Evento | Uso |
|--------|-----|
| `ApplicationStarted` | Logging, service discovery |
| `ApplicationStopping` | Drenar requests |
| `ApplicationStopped` | Cleanup, flush logs |

```csharp
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStopping.Register(() =>
{
    app.Logger.LogWarning("Parada solicitada - drenando...");
});

lifetime.ApplicationStopped.Register(() =>
{
    Log.CloseAndFlush();
});
```

---

## 2. Timeout de Shutdown

```csharp
builder.Host.ConfigureHostOptions(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30));
```

---

## 3. BackgroundService StopAsync

```csharp
public class Worker : BackgroundService
{
    private readonly ConcurrentBag<Task> _tareas = new();

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            _tareas.Add(ProcesarAsync(ct));
            await Task.Delay(1000, ct);
        }
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        var pendientes = _tareas.Where(t => !t.IsCompleted).ToArray();
        if (pendientes.Length > 0)
            await Task.WhenAny(Task.WhenAll(pendientes),
                Task.Delay(TimeSpan.FromSeconds(25), ct));
        await base.StopAsync(ct);
    }
}
```

---

## 4. Health Check Durante Shutdown

```csharp
public class ShutdownHealthCheck : IHealthCheck
{
    private static bool _shuttingDown;
    public static void SetShuttingDown() => _shuttingDown = true;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext ctx, CancellationToken ct = default)
        => Task.FromResult(_shuttingDown
            ? HealthCheckResult.Unhealthy("Cerrando")
            : HealthCheckResult.Healthy("OK"));
}
```

---

## 5. Signals

| Senal | Origen | Efecto |
|-------|--------|--------|
| SIGTERM | Docker/K8s/systemd | ApplicationStopping |
| SIGINT | Ctrl+C | ApplicationStopping |

---

## 6. Windows Service / systemd

```csharp
builder.Services.AddWindowsService(o =>
    o.ServiceName = "Comillas.{{NombreProyecto}}.Worker");
builder.Services.AddSystemd(); // Linux
```

---

## Secuencia

```
1. Senal recibida
2. ApplicationStopping
3. Health -> Unhealthy
4. Esperar 5s (LB)
5. Rechazar nuevos requests
6. Drenar activos (25s)
7. StopAsync workers
8. ApplicationStopped
9. Flush logs
10. Proceso terminado
```

---

*Skill resilience-patterns v3.7.0 - Graceful Shutdown*
