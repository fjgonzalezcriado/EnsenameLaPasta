# Serilog - Configuracion .NET 10

> Guia completa para configurar Serilog en proyectos .NET 10 de Comillas.

---

## Configuracion Basica en Program.cs

```csharp
// Program.cs - Configuracion temprana (antes de builder)
using Serilog;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Iniciando aplicacion {ApplicationName}", "Comillas.MiApp");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ... resto de configuracion ...

    var app = builder.Build();

    // Middleware de logging de requests
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            diagnosticContext.Set("UserId", httpContext.User?.Identity?.Name ?? "anonymous");
        };
        // No loguear health checks
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (httpContext.Request.Path.StartsWithSegments("/health"))
                return Serilog.Events.LogEventLevel.Verbose;
            if (ex != null)
                return Serilog.Events.LogEventLevel.Error;
            if (elapsed > 5000)
                return Serilog.Events.LogEventLevel.Warning;
            return Serilog.Events.LogEventLevel.Information;
        };
    });

    // ... pipeline HTTP ...

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Error fatal al iniciar la aplicacion");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
```

---

## Configuracion en appsettings.json

### Desarrollo (appsettings.Development.json)

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Information",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}  {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

### Produccion (appsettings.json)

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Serilog.Sinks.File",
      "Serilog.Sinks.ApplicationInsights"
    ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
        "System": "Warning",
        "Comillas": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "fileSizeLimitBytes": 104857600,
          "rollOnFileSizeLimit": true
        }
      },
      {
        "Name": "ApplicationInsights",
        "Args": {
          "connectionString": "",
          "telemetryConverter": "Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter, Serilog.Sinks.ApplicationInsights"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

---

## Niveles de Log por Namespace

| Namespace | Desarrollo | Produccion | Motivo |
|-----------|-----------|------------|--------|
| Default | Debug | Information | Todo lo de la aplicacion |
| Microsoft | Information | Warning | Framework, solo errores en prod |
| Microsoft.AspNetCore | Warning | Warning | Reduce ruido de middleware |
| Microsoft.EFCore.Database.Command | Information | Warning | Ver queries en dev, ocultar en prod |
| System | Warning | Warning | Sistema, solo errores |
| Comillas.* | Debug | Information | Codigo propio, maximo detalle en dev |

---

## Sinks Disponibles

| Sink | Uso | NuGet |
|------|-----|-------|
| Console | Desarrollo, depuracion | Serilog.Sinks.Console |
| File | Logs persistentes rotativos | Serilog.Sinks.File |
| Application Insights | Produccion, Azure Monitor | Serilog.Sinks.ApplicationInsights |
| Seq | Servidor de logs centralizado | Serilog.Sinks.Seq |

---

## Enrichers

```csharp
// Enrichers enriquecen cada evento con informacion contextual
.Enrich.FromLogContext()           // Propiedades del scope
.Enrich.WithMachineName()          // Nombre del servidor
.Enrich.WithThreadId()             // ID del thread
.Enrich.WithProcessId()            // ID del proceso
.Enrich.WithEnvironmentName()      // Development/Production
```

---

## NuGets Necesarios

```xml
<ItemGroup>
  <PackageReference Include="Serilog.AspNetCore" Version="8.*" />
  <PackageReference Include="Serilog.Sinks.Console" Version="6.*" />
  <PackageReference Include="Serilog.Sinks.File" Version="6.*" />
  <PackageReference Include="Serilog.Sinks.ApplicationInsights" Version="4.*" />
  <PackageReference Include="Serilog.Enrichers.Environment" Version="3.*" />
  <PackageReference Include="Serilog.Enrichers.Thread" Version="4.*" />
  <PackageReference Include="Serilog.Enrichers.Process" Version="3.*" />
</ItemGroup>
```

---

*Pattern serilog-config v3.7.0*
