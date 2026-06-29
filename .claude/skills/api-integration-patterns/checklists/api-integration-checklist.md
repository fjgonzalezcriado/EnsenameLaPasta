# Checklist: Integración con API Externa

> **Versión**: 1.0.0
> **Uso**: Verificar antes de considerar completa una integración API

---

## 📋 PRE-INTEGRACIÓN

### Análisis de la API

- [ ] Documentación API leída y comprendida
- [ ] Endpoints necesarios identificados
- [ ] Esquema de autenticación confirmado (OAuth2, API Key, HMAC)
- [ ] Rate limits documentados (requests/min, requests/día)
- [ ] SLA conocido (uptime, tiempo de respuesta)
- [ ] Contacto técnico del proveedor disponible
- [ ] Ambiente de pruebas/sandbox configurado

### Credenciales y Accesos

- [ ] Client ID / API Key obtenidos
- [ ] Client Secret / API Secret obtenidos
- [ ] Scopes/permisos solicitados y aprobados
- [ ] IP de Comillas whitelisteada (si aplica)
- [ ] Credenciales almacenadas en **Azure Key Vault** (NUNCA en código)

---

## 🔧 IMPLEMENTACIÓN

### HttpClient Configuration

- [ ] IHttpClientFactory configurado (NO `new HttpClient()`)
- [ ] BaseAddress configurado
- [ ] Timeout configurado (30s default, ajustar según API)
- [ ] User-Agent header configurado
- [ ] Accept header configurado (`application/json`)

### Autenticación

- [ ] Token Service implementado (para OAuth2)
- [ ] Tokens cacheados en IMemoryCache (NO variables estáticas)
- [ ] Renovación automática de tokens implementada
- [ ] BearerTokenHandler o ApiKeyHandler configurado
- [ ] Manejo de 401 Unauthorized (invalidar caché y reintentar)

### Resiliencia

- [ ] Retry Policy configurado (Polly)
  - [ ] Retry solo en errores transitorios (429, 503, timeout)
  - [ ] Backoff exponencial implementado
  - [ ] Jitter configurado (evitar thundering herd)
- [ ] Circuit Breaker configurado
  - [ ] Umbral de fallos definido (ej: 50% en 10s)
  - [ ] Duración de break definida (ej: 1 min)
  - [ ] Eventos onOpen/onClose logueados
- [ ] Timeout configurado explícitamente

### Logging

- [ ] LoggingHandler configurado
- [ ] Request/Response logueados (sin datos sensibles)
- [ ] Correlation ID inyectado en headers (`X-Correlation-ID`)
- [ ] Errores logueados con contexto suficiente
- [ ] Métricas de latencia capturadas

### Manejo de Respuestas

- [ ] Status codes 2xx manejados correctamente
- [ ] 404 Not Found manejado (retornar null, no lanzar excepción)
- [ ] 429 Rate Limit manejado (retry con Retry-After)
- [ ] 5xx Server Error manejado (retry con circuit breaker)
- [ ] Deserialización con manejo de errores
- [ ] Respuestas grandes paginadas

---

## ✅ CALIDAD

### Tests

- [ ] Tests unitarios escritos (>80% cobertura)
  - [ ] Mock HttpMessageHandler configurado
  - [ ] Casos exitosos probados
  - [ ] Casos de error probados (404, 429, 503)
  - [ ] Timeout probado
- [ ] Tests de integración con API real (ambiente test)
- [ ] Tests de carga/stress realizados (si API crítica)

### Documentación

- [ ] README.md con ejemplo de uso creado
- [ ] Endpoints consumidos documentados
- [ ] DTOs documentados con XML comments
- [ ] Errores comunes y troubleshooting documentados
- [ ] Configuración appsettings.json documentada

### Code Review

- [ ] Código revisado por par
- [ ] Secrets verificados (NO en código, TODO en Key Vault)
- [ ] Naming conventions aplicadas
- [ ] SOLID principles respetados
- [ ] DRY (Don't Repeat Yourself) aplicado

---

## 🚀 PRE-PRODUCCIÓN

### Configuración Ambiente

- [ ] appsettings.Production.json configurado
- [ ] Azure Key Vault configurado para producción
- [ ] Connection strings actualizados
- [ ] Timeouts ajustados (si difieren de dev)
- [ ] Logging level ajustado (Information en prod, Debug en dev)

### Monitoreo

- [ ] Application Insights configurado
- [ ] Alertas configuradas:
  - [ ] Alerta si circuit breaker abierto >3 veces/hora
  - [ ] Alerta si tasa de error >5%
  - [ ] Alerta si latencia media >2s
- [ ] Dashboard con métricas clave creado
- [ ] Runbook de incidencias actualizado

### Seguridad

- [ ] Checklist de seguridad completado (ver security-checklist.md)
- [ ] HTTPS obligatorio verificado
- [ ] Validación de certificados SSL habilitada
- [ ] Rate limiting implementado (si exponemos endpoints)
- [ ] OWASP Top 10 revisado

---

## 📊 POST-DESPLIEGUE

### Validación

- [ ] Smoke test ejecutado en producción
- [ ] Primera llamada real exitosa verificada
- [ ] Logs verificados (sin errores críticos)
- [ ] Métricas iniciales normales
- [ ] Equipo de soporte notificado

### Handover

- [ ] Documentación entregada a soporte
- [ ] Credenciales de producción compartidas (via Key Vault)
- [ ] Contactos de escalamiento compartidos
- [ ] Procedimientos de rollback documentados
- [ ] Retrospectiva de integración realizada

---

## ⚠️ RED FLAGS (Revisar si detectas)

- [ ] ❌ `new HttpClient()` en código
- [ ] ❌ Secrets hardcodeados
- [ ] ❌ Retry sin límite (bucle infinito)
- [ ] ❌ Retry en errores 4xx (Bad Request, Unauthorized)
- [ ] ❌ Timeout no configurado o >2 minutos
- [ ] ❌ Tokens en variables estáticas
- [ ] ❌ Deserialización sin try-catch
- [ ] ❌ Logging de datos sensibles (tokens, passwords)
- [ ] ❌ HTTP en lugar de HTTPS
- [ ] ❌ Sin tests unitarios

---

**Progreso**: ___/50 items completados

*Checklist: api-integration-checklist - STIC.IA v3.7.0*
