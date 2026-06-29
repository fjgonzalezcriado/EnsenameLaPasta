---
name: resilience-patterns
description: >
  Configures resilience and reliability patterns for .NET applications:
  Polly v8+ retry policies, circuit breaker, bulkhead isolation,
  health checks (/health, /ready, /live), and graceful shutdown.
  USE FOR: Polly v8 retry policies, circuit breakers, health checks,
  graceful shutdown, configurar resiliencia, politicas de reintento,
  circuit breaker.
  DO NOT USE FOR: observability/logging (use observability-patterns),
  performance optimization (use performance-profiler agent),
  security (use security-audit).
---

# Resilience Patterns - Comillas

Este skill implementa patrones de resiliencia y fiabilidad siguiendo estandares STIC.

---

## Cuando Usar Este Skill

1. **Health checks** - Configurar endpoints /health, /health/ready, /health/live obligatorios para produccion
2. **Politicas Polly** - Configurar retry, circuit breaker, timeout y bulkhead para HttpClients y servicios externos
3. **Graceful shutdown** - Implementar cierre controlado con drain de requests activos
4. **Circuit breaker** - Proteger servicios externos con circuit breaker y fallback

---

## Activacion del Skill

### Por Comando

| Comando | Accion |
|---------|--------|
| `/health-check` | Activa el skill para configurar health checks (endpoints, checks obligatorios y opcionales) |
| `/add-resilience` | Activa el skill para configurar politicas de resiliencia Polly en HttpClients |

### Por Contexto (Automatico)

Claude activa este skill automaticamente cuando detecta:
- Usuario menciona "resiliencia", "circuit breaker", "health check", "retry"
- Se pide configurar reintentos o politicas de Polly
- Se trabaja con HttpClientFactory y servicios externos
- Se menciona "Polly", "timeout", "bulkhead", "rate limiter"
- Se solicita graceful shutdown o drain de requests

---

## Stack de Resiliencia

| Paquete | Version | Uso |
|---------|---------|-----|
| **Microsoft.Extensions.Http.Resilience** | 8.x | Resiliencia integrada para HttpClientFactory |
| **Polly** | 8.x | Politicas de resiliencia (Retry, CB, Timeout, Bulkhead) |
| **AspNetCore.HealthChecks.SqlServer** | 8.x | Health check para SQL Server |
| **AspNetCore.HealthChecks.Redis** | 8.x | Health check para Redis |
| **AspNetCore.HealthChecks.UI** | 8.x | UI para visualizar health checks (opcional, desarrollo) |

---

## Health Checks - Endpoints

| Endpoint | Descripcion | Checks incluidos |
|----------|-------------|------------------|
| `/health` | Todos los checks (agregado) | SQL Server + Self + Redis + APIs externas + custom |
| `/health/ready` | Readiness - Dependencias listas | SQL Server + Redis + Storage (tag: `ready`) |
| `/health/live` | Liveness - Proceso vivo | Self check basico (tag: `live`) |

### Checks Obligatorios

| Check | Tipo | Tag | Motivo |
|-------|------|-----|--------|
| **SQL Server** | Base de datos | `ready`, `db` | Toda aplicacion Comillas usa SQL Server |
| **Self** | Basico | `live` | Verificar que el proceso responde |

### Checks Opcionales

| Check | Tipo | Tag | Cuando usar |
|-------|------|-----|-------------|
| Redis | Cache | `ready`, `cache` | Si se usa cache distribuida |
| External APIs | HTTP | `ready`, `api` | Si se consumen APIs externas |
| Azure Blob | Storage | `ready`, `storage` | Si se usa Azure Blob Storage |
| Custom business | Logica | `ready`, `business` | Verificar tabla critica o SP |

---

## Patrones Disponibles

### En `patterns/`

| Archivo | Descripcion |
|---------|-------------|
| `polly-policies.md` | Politicas Polly v8+ completas (Retry, CB, Timeout, Bulkhead, Rate Limiter) |
| `health-checks.md` | Health checks obligatorios y opcionales con configuracion completa |
| `graceful-shutdown.md` | Cierre controlado con drain de requests y BackgroundService |

### En `templates/`

| Archivo | Descripcion |
|---------|-------------|
| `HealthChecksConfig.cs.template` | Extension methods para configurar health checks en Program.cs |
| `ResiliencePolicies.cs.template` | Extension methods para configurar Polly en HttpClients |

---

## Checklist de Resiliencia

### Antes de desplegar a produccion

- [ ] Health checks configurados (`/health`, `/health/ready`, `/health/live`)
- [ ] Polly policies configuradas para todos los HttpClients externos
- [ ] Graceful shutdown implementado (drain de requests activos)
- [ ] Circuit breaker configurado para servicios externos criticos
- [ ] Timeouts configurados (30s default por request)
- [ ] Retry con backoff exponencial y jitter para transient faults
- [ ] Health check de SQL Server obligatorio
- [ ] Health endpoints NO exponen informacion sensible en produccion

---

*Skill resilience-patterns v3.7.0*