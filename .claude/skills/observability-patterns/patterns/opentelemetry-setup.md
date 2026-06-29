# OpenTelemetry - Configuracion .NET 10

> Guia completa para configurar OpenTelemetry en proyectos .NET 10 de Comillas.

---

## Conceptos Clave

| Concepto | Descripcion |
|----------|-------------|
| **Activity** | Equivalente a un Span en OpenTelemetry. Representa una operacion |
| **ActivitySource** | Crea Activities. Equivalente a un Tracer |
| **Meter** | Instrumento para crear metricas |
| **Exporter** | Envia telemetria a un backend (App Insights, Console, OTLP) |
| **Instrumentation** | Captura automatica de telemetria de librerias (ASP.NET, HTTP, SQL) |

---

## Configuracion en Program.cs

```csharp
// Program.cs - .NET 10
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Configurar OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: builder.Configuration["OpenTelemetry:ServiceName"] ?? "Comillas.MiApp",
            serviceVersion: builder.Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["service.namespace"] = "Comillas"
        }))
    .WithTracing(tracing => tracing
        // Instrumentacion automatica
        .AddAspNetCoreInstrumentation(options =>
        {
            // Filtrar health checks y assets estaticos
            options.Filter = httpContext =>
                !httpContext.Request.Path.StartsWithSegments("/health") &&
                !httpContext.Request.Path.StartsWithSegments("/favicon");
        })
        .AddHttpClientInstrumentation(options =>
        {
            // No trazar llamadas a Application Insights
            options.FilterHttpRequestMessage = request =>
                request.RequestUri?.Host != "dc.services.visualstudio.com";
        })
        .AddSqlClientInstrumentation(options =>
        {
            options.SetDbStatementForText = true;  // Solo en desarrollo
            options.RecordException = true;
        })
        // Activity Sources personalizados
        .AddSource("Comillas.*"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        // Metricas personalizadas
        .AddMeter("Comillas.*"));

// Exporters (segun entorno)
if (builder.Environment.IsDevelopment())
{
    // Console exporter para desarrollo
    builder.Services.AddOpenTelemetry()
        .WithTracing(t => t.AddConsoleExporter())
        .WithMetrics(m => m.AddConsoleExporter());
}

// Application Insights (produccion y desarrollo)
var appInsightsCs = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(appInsightsCs))
{
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor(options =>
        {
            options.ConnectionString = appInsightsCs;
        });
}
```

---

## Activity Sources Personalizados

```csharp
// Definir ActivitySource para trazas personalizadas
public static class TelemetryConstants
{
    public static readonly ActivitySource BecasActivitySource =
        new("Comillas.MiApp.Becas", "1.0.0");

    public static readonly ActivitySource IntegracionesActivitySource =
        new("Comillas.MiApp.Integraciones", "1.0.0");
}

// Uso en servicios
public class BecaService : IBecaService
{
    public async Task<BecaDto> ProcesarAsync(int becaId, CancellationToken ct)
    {
        using var activity = TelemetryConstants.BecasActivitySource
            .StartActivity("ProcesarBeca");

        activity?.SetTag("beca.id", becaId);

        try
        {
            var resultado = await _repository.GetByIdAsync(becaId, ct);
            activity?.SetTag("beca.estado", resultado?.Estado.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);
            return resultado;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

---

## Metricas Personalizadas

```csharp
// Definir metricas de negocio
public static class BecasMetrics
{
    private static readonly Meter Meter = new("Comillas.MiApp.Becas", "1.0.0");

    // Counter - numero total de operaciones
    public static readonly Counter<long> BecasCreadas =
        Meter.CreateCounter<long>("becas.creadas", "becas",
            "Numero total de becas creadas");

    // Histogram - distribucion de importes
    public static readonly Histogram<double> ImportesBecas =
        Meter.CreateHistogram<double>("becas.importe", "EUR",
            "Distribucion de importes de becas");

    // UpDownCounter - becas activas en un momento dado
    public static readonly UpDownCounter<long> BecasActivas =
        Meter.CreateUpDownCounter<long>("becas.activas", "becas",
            "Numero de becas activas actualmente");
}

// Uso
BecasMetrics.BecasCreadas.Add(1, new KeyValuePair<string, object?>("tipo", "excelencia"));
BecasMetrics.ImportesBecas.Record(5000.0);
BecasMetrics.BecasActivas.Add(1);
```

---

## Configuracion appsettings.json

```json
{
  "OpenTelemetry": {
    "ServiceName": "Comillas.MiApp",
    "ServiceVersion": "1.0.0"
  },
  "ApplicationInsights": {
    "ConnectionString": "PLACEHOLDER_USAR_KEY_VAULT_EN_PRODUCCION"
  }
}
```

---

## NuGets Necesarios

```xml
<ItemGroup>
  <!-- OpenTelemetry Core -->
  <PackageReference Include="OpenTelemetry" Version="1.*" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.*" />

  <!-- Instrumentacion automatica -->
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.*" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.*" />
  <PackageReference Include="OpenTelemetry.Instrumentation.SqlClient" Version="1.*" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.*" />

  <!-- Exporters -->
  <PackageReference Include="Azure.Monitor.OpenTelemetry.AspNetCore" Version="1.*" />
  <PackageReference Include="OpenTelemetry.Exporter.Console" Version="1.*" />
  <!-- Opcional: OTLP para Grafana/Jaeger -->
  <!-- <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.*" /> -->
</ItemGroup>
```

---

## Filtrado de Trazas

```csharp
// No trazar endpoints de health o assets estaticos
.AddAspNetCoreInstrumentation(options =>
{
    options.Filter = httpContext =>
    {
        var path = httpContext.Request.Path.Value ?? "";
        return !path.StartsWith("/health") &&
               !path.StartsWith("/favicon") &&
               !path.StartsWith("/_framework") &&
               !path.EndsWith(".css") &&
               !path.EndsWith(".js");
    };
})
```

---

*Pattern opentelemetry-setup v3.7.0*
