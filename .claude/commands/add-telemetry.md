Configura OpenTelemetry y Serilog en el proyecto

# Configurar Telemetría

Wizard para configurar observabilidad (OpenTelemetry + Serilog) en el proyecto siguiendo los estándares de Comillas.

---

## PASO 1: Detectar proyecto

1. **Buscar proyecto principal en `03_Desarrollo/`:**
   - Localizar archivos `.csproj` (buscar el proyecto Web/API principal)
   - Detectar `TargetFramework` (net10.0, net9.0, etc.)
   - Localizar `Program.cs`
   - Verificar si ya existe configuración de telemetría (`AddOpenTelemetry`, `UseSerilog`)
   - Verificar si Serilog ya está configurado en appsettings.json

2. **Si ya existe configuración:**:
   - Informar al usuario que ya hay telemetría configurada
   - Mostrar qué componentes están activos
   - Preguntar si desea ampliar o reconfigurar

3. **Mostrar resumen de detección:**:
```
Proyecto detectado: Comillas.{0}.Web
Framework: {0}
Program.cs: {0}
OpenTelemetry existente: {0}
Serilog existente: {0}
Application Insights: {0}
```

---

## PASO 2: Wizard interactivo

Preguntar al usuario qué componentes desea configurar:

```
Selecciona los componentes de telemetría a configurar:

RECOMENDADOS:
  [x] OpenTelemetry (traces + metrics)   - Trazabilidad distribuida
  [x] Serilog (structured logging)       - Logging estructurado
  [x] Application Insights exporter      - Exportar a Azure Monitor

OPCIONALES:
  [ ] Console exporter                   - Para desarrollo local
  [ ] OTLP exporter                      - Para Grafana/Jaeger

Cuales deseas configurar? (separados por coma, ej: otel,serilog,appinsights)
```

---

## PASO 3: Añadir NuGets

Según la selección del PASO 2, listar e instalar los paquetes necesarios:

**Si OpenTelemetry seleccionado:**
```powershell
dotnet add package OpenTelemetry
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Instrumentation.SqlClient
```

**Si Serilog seleccionado:**
```powershell
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Enrichers.Thread
```

**Si Application Insights seleccionado:**
```powershell
dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore
dotnet add package Serilog.Sinks.ApplicationInsights
```

**Si Console exporter seleccionado:**
```powershell
dotnet add package OpenTelemetry.Exporter.Console
```

**Si OTLP exporter seleccionado:**
```powershell
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

Ejecutar `dotnet add package` para cada paquete seleccionado en el directorio del `.csproj` principal.

---

## PASO 4: Generar código

1. **Leer plantillas desde `.claude/skills/observability-patterns/templates/`**
   - `TelemetryConfig.cs.template` para OpenTelemetry
   - `SerilogConfig.cs.template` para Serilog
   - Si las plantillas no existen, generar el código directamente

2. **Reemplazar variables:**:
   - `{{NombreProyecto}}` con el nombre del proyecto detectado
   - `{{Namespace}}` con el namespace correspondiente
   - `{{ServiceName}}` con el nombre del servicio para trazas

3. **Generar TelemetryConfig.cs (si OpenTelemetry seleccionado):**:
   - Ruta: `03_Desarrollo/src/Comillas.{0}.Infrastructure/Telemetry/TelemetryConfig.cs`
   - Clase estática con método de extensión `AddCustomTelemetry()`
   - Configurar TracerProvider con instrumentación de ASP.NET Core, HTTP y SQL
   - Configurar MeterProvider con métricas de ASP.NET Core
   - Añadir exporters según selección (App Insights, Console, OTLP)

4. **Generar SerilogConfig.cs (si Serilog seleccionado):**:
   - Ruta: `03_Desarrollo/src/Comillas.{0}.Infrastructure/Logging/SerilogConfig.cs`
   - Clase estática con método de extensión `AddCustomSerilog()`
   - Configurar sinks: Console, File, Application Insights (si seleccionado)
   - Configurar enrichers: Environment, Thread, RequestId
   - Configurar filtrado de logs por namespace

5. **Modificar Program.cs:**:
   - Añadir `using` de los namespaces correspondientes
   - Si OpenTelemetry: `builder.Services.AddCustomTelemetry(builder.Configuration);`
   - Si Serilog: `builder.Host.UseSerilog();` y configuración
   - Añadir middleware de correlación de trazas

---

## PASO 5: Configurar appsettings

1. **Añadir sección Serilog a appsettings.json (si seleccionado):**:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

2. **Añadir ApplicationInsights (si seleccionado):**:
```json
{
  "ApplicationInsights": {
    "ConnectionString": "PLACEHOLDER_USAR_KEY_VAULT_EN_PRODUCCION"
  }
}
```

3. **Añadir OpenTelemetry (si seleccionado):**:
```json
{
  "OpenTelemetry": {
    "ServiceName": "Comillas.[Nombre]",
    "ServiceVersion": "1.0.0"
  }
}
```

---

## PASO 6: Verificar y resumen

1. **Compilar el proyecto:**:
```powershell
dotnet build 03_Desarrollo/src/Comillas.[Nombre].Web/
```

2. **Verificar que compila sin errores**

3. **Mostrar resumen:**:
```
+---------------------------------------------------------+
|  TELEMETRÍA CONFIGURADA                                 |
+---------------------------------------------------------+
|                                                         |
|  Componentes activos:                                   |
|    [x/--] OpenTelemetry (traces + metrics)              |
|    [x/--] Serilog (structured logging)                  |
|    [x/--] Application Insights exporter                 |
|    [x/--] Console exporter                              |
|    [x/--] OTLP exporter                                 |
|                                                         |
|  Archivos creados/modificados:                          |
|    + Infrastructure/Telemetry/TelemetryConfig.cs        |
|    + Infrastructure/Logging/SerilogConfig.cs             |
|    ~ Program.cs                                         |
|    ~ appsettings.json                                   |
|                                                         |
+---------------------------------------------------------+
```

4. **Recordatorio RGPD - Datos que NUNCA se deben loguear:**:
```
ATENCIÓN - RGPD: No loguear datos personales:
  - Contraseñas o tokens de autenticación
  - DNI, NIE, pasaporte
  - Emails personales
  - Números de teléfono
  - Datos bancarios
  - Datos de salud
  - Direcciones personales
```

---

## RECORDATORIOS CRÍTICOS

- **NUNCA loguear datos sensibles** (contraseñas, tokens, DNI, emails personales)
- **ApplicationInsights ConnectionString** debe venir de Azure Key Vault en producción
- Configurar **niveles de log apropiados**: Information para desarrollo, Warning para producción
- Los **enrichers** ayudan a correlacionar logs entre servicios
- Usar **structured logging** (templates con parámetros, no concatenación de strings)
- **Skill de referencia**: `.claude/skills/observability-patterns/`
- **Guía de referencia**: `Documentos_Base/06_Observabilidad/GUIA_OBSERVABILIDAD.md`
