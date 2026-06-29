---
name: observability-patterns
description: >
  Configures observability and telemetry for .NET applications:
  OpenTelemetry setup, Serilog structured logging, custom metrics,
  distributed tracing, and Application Insights integration.
  USE FOR: configuring OpenTelemetry, Serilog setup, structured logging,
  Application Insights, health checks, metricas, trazabilidad,
  configurar telemetria, configurar logging.
  DO NOT USE FOR: resilience patterns (use resilience-patterns),
  performance profiling (use performance-profiler agent),
  security monitoring (use security-audit).
---

# Observability Patterns - Comillas

Este skill implementa patrones de observabilidad y telemetria siguiendo estandares STIC.

---

## Cuando Usar Este Skill

1. **OpenTelemetry** - Configurar trazabilidad distribuida y metricas con OpenTelemetry .NET 10
2. **Serilog** - Configurar logging estructurado con sinks (Console, File, App Insights)
3. **Structured Logging** - Implementar best practices de logging con cumplimiento RGPD
4. **Application Insights** - Integrar con Azure Monitor para produccion

---

## Activacion del Skill

### Por Comando

| Comando | Accion |
|---------|--------|
| `/add-telemetry` | Activa el skill para configurar OpenTelemetry + Serilog en el proyecto |

### Por Contexto (Automatico)

Claude activa este skill automaticamente cuando detecta:
- Usuario menciona "telemetria", "logging", "OpenTelemetry", "Serilog"
- Se pide configurar metricas, trazas o logs estructurados
- Se trabaja con Application Insights o exporters
- Se menciona "observabilidad", "metricas", "tracing", "correlacion"
- Se solicita configurar niveles de log o filtrado por namespace

---

## Stack de Observabilidad

| Paquete | Version | Uso |
|---------|---------|-----|
| **OpenTelemetry** | 1.x | SDK base de telemetria |
| **OpenTelemetry.Extensions.Hosting** | 1.x | Integracion con Host .NET |
| **OpenTelemetry.Instrumentation.AspNetCore** | 1.x | Instrumentacion automatica HTTP |
| **OpenTelemetry.Instrumentation.Http** | 1.x | Instrumentacion HttpClient |
| **OpenTelemetry.Instrumentation.SqlClient** | 1.x | Instrumentacion SQL Server |
| **Azure.Monitor.OpenTelemetry.AspNetCore** | 1.x | Exporter Application Insights |
| **Serilog.AspNetCore** | 8.x | Logging estructurado |
| **Serilog.Sinks.Console** | 6.x | Sink consola |
| **Serilog.Sinks.File** | 6.x | Sink archivo rotativo |
| **Serilog.Sinks.ApplicationInsights** | 4.x | Sink App Insights |
| **Serilog.Enrichers.Environment** | 3.x | Enricher maquina/proceso |
| **Serilog.Enrichers.Thread** | 4.x | Enricher ThreadId |

---

## Tres Pilares de Observabilidad

| Pilar | Herramienta | Proposito |
|-------|-------------|-----------|
| **Logs** | Serilog | Eventos discretos con contexto estructurado |
| **Traces** | OpenTelemetry | Flujo de una peticion a traves de servicios |
| **Metrics** | OpenTelemetry | Mediciones agregadas (latencia, throughput, errores) |

---

## RGPD - Datos que NUNCA se Deben Loguear

| Dato | Ejemplo | Motivo |
|------|---------|--------|
| Contrasenas | `password`, `secret` | Seguridad |
| Tokens | `Bearer eyJ...`, `api_key` | Seguridad |
| DNI/NIE | `12345678A` | Dato personal |
| Emails personales | `nombre@gmail.com` | Dato personal |
| Telefonos | `+34 612 345 678` | Dato personal |
| Datos bancarios | IBAN, tarjeta de credito | Dato financiero |
| Datos de salud | Diagnosticos, medicaciones | Dato sensible |
| Direcciones | Domicilio personal | Dato personal |

**Regla**: Loguear IDs internos, nunca datos personales directamente.

---

## Patrones Disponibles

### En `patterns/`

| Archivo | Descripcion |
|---------|-------------|
| `opentelemetry-setup.md` | Configuracion completa OpenTelemetry .NET 10 (traces, metrics, exporters) |
| `serilog-config.md` | Configuracion Serilog con sinks, enrichers y filtrado por namespace |
| `structured-logging.md` | Best practices de logging estructurado y cumplimiento RGPD |

### En `templates/`

| Archivo | Descripcion |
|---------|-------------|
| `TelemetryConfig.cs.template` | Extension methods para configurar OpenTelemetry en Program.cs |
| `SerilogConfig.cs.template` | Extension methods para configurar Serilog en Program.cs |

---

## Checklist de Observabilidad

### Antes de desplegar a produccion

- [ ] Serilog configurado con niveles apropiados (Warning+ en produccion)
- [ ] OpenTelemetry configurado con instrumentacion ASP.NET Core, HTTP y SQL
- [ ] Application Insights exporter configurado con ConnectionString de Key Vault
- [ ] Verificado que NO se loguean datos personales (RGPD)
- [ ] Enrichers configurados (Machine, Thread, RequestId)
- [ ] Logging estructurado (templates con parametros, no concatenacion)
- [ ] Rotacion de archivos de log configurada (30 dias maximo)
- [ ] Metricas de negocio definidas si aplica

---

*Skill observability-patterns v3.7.0*
