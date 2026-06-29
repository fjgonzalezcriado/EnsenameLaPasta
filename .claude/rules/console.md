---
globs:
  - "**/Workers/**/*.cs"
  - "**/Services/**/*Service.cs"
  - "**/Jobs/**/*.cs"
  - "**/Tasks/**/*.cs"
  - "**/Handlers/**/*.cs"
  - "**/Console/**/*.cs"
  - "**/*Worker.cs"
  - "**/*Job.cs"
  - "**/*Task.cs"
  - "**/*HostedService.cs"
---

# Reglas para Aplicaciones de Consola y Servicios Windows

> Este archivo aplica cuando Claude trabaja con aplicaciones de consola,
> tareas programadas o servicios Windows.
> **Detecta automáticamente** la versión de .NET del proyecto.

---

## DETECCIÓN DE VERSIÓN

```xml
<!-- En .csproj -->
<TargetFramework>net10.0</TargetFramework>  <!-- .NET 10 -->
<TargetFramework>net9.0</TargetFramework>   <!-- .NET 9 -->
<TargetFramework>net8.0</TargetFramework>   <!-- .NET 8 -->
<TargetFramework>net48</TargetFramework>    <!-- .NET Framework 4.8 -->
```

---

## TIPOS DE APLICACIONES

| Tipo | Ejecución | Versiones |
|------|-----------|-----------|
| **Console App** | Manual / Programador de Tareas | Todas |
| **Worker Service** | Servicio Windows / systemd | .NET 8+ |
| **Windows Service** | Servicio Windows tradicional | .NET 4.x |
| **Hosted Service** | Dentro de aplicación web | .NET 8+ |

---

## PARTE 1: WORKER SERVICE .NET 10 (Recomendado)

### Program.cs - .NET 10

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .CreateLogger();

try
{
    Log.Information("Iniciando servicio {ServiceName}", "Comillas.Worker");

    var builder = Host.CreateApplicationBuilder(args);

    // ═══════════════════════════════════════════════════════════════
    // CONFIGURACIÓN .NET 10 - Validación integrada
    // ═══════════════════════════════════════════════════════════════
    
    builder.Services.AddOptionsWithValidateOnStart<WorkerSettings>()
        .Bind(builder.Configuration.GetSection("WorkerSettings"))
        .ValidateDataAnnotations();

    // Base de datos
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.EnableRetryOnFailure(3)));

    // Servicios
    builder.Services.AddScoped<IBecaService, BecaService>();
    builder.Services.AddSingleton<IEmailService, EmailService>();

    // Workers con mejoras .NET 10
    builder.Services.AddHostedService<ProcesadorBecasWorker>();
    builder.Services.AddHostedService<NotificacionesWorker>();

    // Health checks para monitorización
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("database")
        .AddCheck<WorkerHealthCheck>("worker");

    // Serilog
    builder.Services.AddSerilog();

    // Servicio Windows / systemd
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "Comillas.GestionBecas.Worker";
    });
    
    // Linux systemd
    builder.Services.AddSystemd();

    var host = builder.Build();
    
    // Endpoint de health (opcional, para K8s/Docker)
    // host.MapHealthChecks("/health");
    
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Error fatal al iniciar el servicio");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
```

### Worker con BackgroundService - .NET 10

```csharp
public class ProcesadorBecasWorker : BackgroundService
{
    private readonly ILogger<ProcesadorBecasWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<WorkerSettings> _settings;
    private readonly TimeProvider _timeProvider;

    public ProcesadorBecasWorker(
        ILogger<ProcesadorBecasWorker> logger,
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<WorkerSettings> settings,
        TimeProvider timeProvider)  // .NET 10 - TimeProvider para testing
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _settings = settings;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker iniciado a las {Time}", 
            _timeProvider.GetUtcNow());

        // .NET 10 - PeriodicTimer mejorado
        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(_settings.CurrentValue.IntervaloMinutos),
            _timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcesarBecasPendientesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker cancelado correctamente");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ciclo de procesamiento");
                // Continuar con el siguiente ciclo
            }
        }

        _logger.LogInformation("Worker detenido");
    }

    private async Task ProcesarBecasPendientesAsync(CancellationToken ct)
    {
        // ⚠️ IMPORTANTE: Crear scope para servicios Scoped
        await using var scope = _scopeFactory.CreateAsyncScope();
        var becaService = scope.ServiceProvider.GetRequiredService<IBecaService>();

        var becasPendientes = await becaService.GetPendientesAsync(ct);
        
        _logger.LogInformation("Encontradas {Count} becas pendientes", 
            becasPendientes.Count());

        // .NET 10 - Parallel.ForEachAsync mejorado
        await Parallel.ForEachAsync(
            becasPendientes,
            new ParallelOptions 
            { 
                MaxDegreeOfParallelism = 4,
                CancellationToken = ct 
            },
            async (beca, token) =>
            {
                await using var innerScope = _scopeFactory.CreateAsyncScope();
                var service = innerScope.ServiceProvider.GetRequiredService<IBecaService>();
                
                _logger.LogDebug("Procesando beca {BecaId}", beca.Id);
                await service.ProcesarAsync(beca.Id, token);
            });

        _logger.LogInformation("Ciclo completado");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Solicitud de parada recibida");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Worker detenido correctamente");
    }
}
```

### Health Check para Worker

```csharp
public class WorkerHealthCheck : IHealthCheck
{
    private readonly ILogger<WorkerHealthCheck> _logger;
    private static DateTime _lastExecution = DateTime.MinValue;
    private static readonly TimeSpan MaxInterval = TimeSpan.FromMinutes(15);

    public WorkerHealthCheck(ILogger<WorkerHealthCheck> logger)
    {
        _logger = logger;
    }

    public static void ReportExecution() => _lastExecution = DateTime.UtcNow;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var timeSinceLastExecution = DateTime.UtcNow - _lastExecution;

        if (_lastExecution == DateTime.MinValue)
        {
            return Task.FromResult(HealthCheckResult.Degraded("Worker no ha ejecutado aún"));
        }

        if (timeSinceLastExecution > MaxInterval)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Última ejecución hace {timeSinceLastExecution.TotalMinutes:F0} minutos"));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Última ejecución hace {timeSinceLastExecution.TotalSeconds:F0} segundos"));
    }
}
```

---

## PARTE 2: COMPARATIVA POR VERSIÓN

### .NET 10 vs .NET 8/9 vs .NET 4.x

| Característica | .NET 4.x | .NET 8/9 | .NET 10 |
|----------------|----------|----------|---------|
| **Host** | ServiceBase | Host.CreateDefaultBuilder | Host.CreateApplicationBuilder |
| **Timer** | System.Timers.Timer | PeriodicTimer | PeriodicTimer + TimeProvider |
| **DI Scope** | Manual | CreateScope() | CreateAsyncScope() |
| **Cancellation** | Manual | CancellationToken | CancellationToken mejorado |
| **Parallel** | Parallel.ForEach | Parallel.ForEachAsync | Parallel.ForEachAsync optimizado |
| **Logging** | log4net/NLog | Serilog | Serilog + OpenTelemetry |

---

## PARTE 3: .NET 8/9 (PREPARAR MIGRACIÓN)

```csharp
// Program.cs - .NET 8/9
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<WorkerSettings>(
            context.Configuration.GetSection("WorkerSettings"));
            
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));
            
        services.AddHostedService<ProcesadorBecasWorker>();
    })
    .UseSerilog();

// Diferencias para migración a .NET 10:
// - CreateDefaultBuilder → CreateApplicationBuilder
// - Configure<T> → AddOptionsWithValidateOnStart<T>
// - CreateScope() → CreateAsyncScope()
```

---

## PARTE 4: .NET FRAMEWORK 4.x (LEGACY)

```csharp
// Windows Service tradicional
public partial class BecasService : ServiceBase
{
    private Timer _timer;
    private readonly ILog _log = LogManager.GetLogger(typeof(BecasService));

    public BecasService()
    {
        InitializeComponent();
    }

    protected override void OnStart(string[] args)
    {
        _log.Info("Servicio iniciado");
        _timer = new Timer(ProcesarBecas, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
    }

    private void ProcesarBecas(object state)
    {
        try
        {
            using (var context = new ApplicationDbContext())
            {
                var becasPendientes = context.Becas
                    .Where(b => b.Estado == EstadoBeca.Pendiente)
                    .ToList();

                foreach (var beca in becasPendientes)
                {
                    // Procesar...
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error("Error procesando becas", ex);
        }
    }

    protected override void OnStop()
    {
        _log.Info("Servicio detenido");
        _timer?.Dispose();
    }
}

// Program.cs para Windows Service .NET 4.x
static class Program
{
    static void Main()
    {
        ServiceBase[] ServicesToRun = new ServiceBase[]
        {
            new BecasService()
        };
        ServiceBase.Run(ServicesToRun);
    }
}
```

---

## PARTE 5: CONFIGURACIÓN

### appsettings.json - Todas las versiones modernas

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "WorkerSettings": {
    "IntervaloMinutos": 5,
    "MaxReintentos": 3,
    "TimeoutSegundos": 300,
    "MaxParallelism": 4,
    "HoraInicioVentana": "08:00",
    "HoraFinVentana": "20:00"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      { 
        "Name": "File", 
        "Args": { 
          "path": "logs/worker-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

### Settings con validación (.NET 10)

```csharp
public class WorkerSettings
{
    [Required]
    [Range(1, 60)]
    public int IntervaloMinutos { get; set; } = 5;

    [Range(1, 10)]
    public int MaxReintentos { get; set; } = 3;

    [Range(30, 600)]
    public int TimeoutSegundos { get; set; } = 300;

    [Range(1, 16)]
    public int MaxParallelism { get; set; } = 4;
}
```

---

## PARTE 6: INSTALACIÓN COMO SERVICIO

### Windows Service (.NET 10)

```powershell
# Publicar
dotnet publish -c Release -o C:\Services\Comillas.Worker

# Crear servicio
sc.exe create "ComillasGestionBecasWorker" `
    binPath="C:\Services\Comillas.Worker\Comillas.Worker.exe" `
    start=auto `
    DisplayName="Comillas - Gestión de Becas Worker"

# Configurar recuperación ante fallos
sc.exe failure "ComillasGestionBecasWorker" `
    reset=86400 `
    actions=restart/60000/restart/60000/restart/60000

# Descripción
sc.exe description "ComillasGestionBecasWorker" `
    "Procesa becas pendientes cada 5 minutos"

# Iniciar
sc.exe start "ComillasGestionBecasWorker"
```

### Linux systemd (.NET 10)

```ini
# /etc/systemd/system/comillas-worker.service
[Unit]
Description=Comillas Gestión de Becas Worker
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/comillas/worker
ExecStart=/opt/comillas/worker/Comillas.Worker
Restart=always
RestartSec=10
User=comillas
Environment=DOTNET_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

```bash
# Comandos systemd
sudo systemctl daemon-reload
sudo systemctl enable comillas-worker
sudo systemctl start comillas-worker
sudo systemctl status comillas-worker
```

---

## PARTE 7: GRACEFUL SHUTDOWN

```csharp
public class GracefulShutdownWorker : BackgroundService
{
    private readonly ILogger<GracefulShutdownWorker> _logger;
    private readonly ConcurrentBag<Task> _tareasEnProceso = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var tarea = ProcesarItemAsync(stoppingToken);
            _tareasEnProceso.Add(tarea);

            // Limpiar tareas completadas periódicamente
            var completadas = _tareasEnProceso.Where(t => t.IsCompleted).ToList();
            foreach (var t in completadas)
            {
                _tareasEnProceso.TryTake(out _);
            }

            await Task.Delay(1000, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Esperando {Count} tareas pendientes...", 
            _tareasEnProceso.Count);

        // Esperar a que terminen con timeout
        var timeout = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        var pending = Task.WhenAll(_tareasEnProceso.Where(t => !t.IsCompleted));

        await Task.WhenAny(pending, timeout);

        if (!pending.IsCompleted)
        {
            _logger.LogWarning("Timeout esperando tareas, forzando cierre");
        }

        await base.StopAsync(cancellationToken);
    }
}
```

---

## CHECKLIST POR VERSIÓN

### .NET 10 ✅
- [ ] Host.CreateApplicationBuilder
- [ ] AddOptionsWithValidateOnStart
- [ ] CreateAsyncScope()
- [ ] TimeProvider para testing
- [ ] PeriodicTimer
- [ ] Health checks

### .NET 8/9 (Migrar)
- [ ] Identificar CreateDefaultBuilder → CreateApplicationBuilder
- [ ] Migrar Configure<T> → AddOptionsWithValidateOnStart
- [ ] Usar CreateAsyncScope

### .NET 4.x (Mantener)
- [ ] ServiceBase tradicional
- [ ] System.Timers.Timer
- [ ] log4net/NLog
- [ ] No introducir async/await complejo

---

*Regla condicional v3.7.0 - Multi-versión .NET*
