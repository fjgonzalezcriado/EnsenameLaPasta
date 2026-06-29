# Checklist: Seguridad en Integraciones API

> **Versión**: 1.0.0
> **Uso**: Verificar aspectos de seguridad antes de desplegar integración API
> **Basado en**: OWASP API Security Top 10

---

## 🔒 AUTENTICACIÓN Y AUTORIZACIÓN

### Credenciales

- [ ] **CRÍTICO**: Client Secrets almacenados en Azure Key Vault (NO en código)
- [ ] **CRÍTICO**: API Keys almacenados en Azure Key Vault (NO en appsettings.json)
- [ ] Credenciales diferentes por ambiente (dev/test/prod)
- [ ] Tokens OAuth2 con expiración adecuada (<1 hora)
- [ ] Tokens almacenados en IMemoryCache (NO en variables estáticas)
- [ ] Renovación automática de tokens implementada
- [ ] Secrets rotados periódicamente (cada 90 días mínimo)

### Validación de Tokens

- [ ] Validación de token antes de cada llamada
- [ ] Manejo de 401 Unauthorized (invalidar caché, renovar token)
- [ ] Manejo de 403 Forbidden (loguear intento, no reintentar)
- [ ] Scopes OAuth2 con principio de mínimo privilegio
- [ ] Tokens NO logueados (ni en desarrollo)

---

## 🔐 TRANSPORTE Y CIFRADO

### HTTPS

- [ ] **CRÍTICO**: Solo HTTPS permitido (NO HTTP)
- [ ] Validación de certificados SSL habilitada
- [ ] TLS 1.2+ requerido (TLS 1.0/1.1 deshabilitados)
- [ ] Certificate pinning considerado (APIs críticas)
- [ ] Man-in-the-middle (MITM) attacks mitigados

### Headers de Seguridad

- [ ] `X-Content-Type-Options: nosniff` configurado
- [ ] `X-Frame-Options: DENY` configurado
- [ ] `X-XSS-Protection: 1; mode=block` configurado
- [ ] `Strict-Transport-Security` (HSTS) configurado
- [ ] `Content-Security-Policy` (CSP) configurado si aplica

---

## 📊 LOGGING Y AUDITORÍA

### Datos Sensibles

- [ ] **CRÍTICO**: Passwords NUNCA logueados
- [ ] **CRÍTICO**: Tokens NUNCA logueados (ni parciales)
- [ ] API Keys NUNCA logueados
- [ ] Client Secrets NUNCA logueados
- [ ] DNI/NIF/NIE enmascarados en logs (RGPD)
- [ ] Emails parcialmente enmascarados (`j***@comillas.edu`)
- [ ] Números de tarjeta enmascarados (si aplica)

### Auditoría

- [ ] Llamadas API logueadas con Correlation ID
- [ ] Usuario/aplicación que inicia llamada registrado
- [ ] Timestamp de cada operación registrado
- [ ] Errores de autenticación logueados (intentos fallidos)
- [ ] Logs centralizados (Application Insights, Elastic, etc.)
- [ ] Retención de logs según normativa (mínimo 1 año)

---

## ⚠️ VALIDACIÓN DE DATOS

### Entrada (Request)

- [ ] Validación de parámetros antes de enviar a API
- [ ] Sanitización de inputs (evitar injection)
- [ ] Límites de tamaño implementados (max request size)
- [ ] Tipos de datos validados (int, email, url, etc.)
- [ ] FluentValidation o DataAnnotations usado
- [ ] Caracteres especiales escapados

### Salida (Response)

- [ ] Validación de schema de respuesta
- [ ] Deserialización con try-catch robusto
- [ ] Respuestas inesperadas manejadas sin crash
- [ ] XSS mitigado al mostrar datos en UI
- [ ] HTML encoding aplicado antes de renderizar

---

## 🚦 RATE LIMITING Y DoS

### Protección

- [ ] Rate limiting implementado (máx requests/min)
- [ ] Circuit breaker configurado (evitar cascada de fallos)
- [ ] Timeout configurado (evitar hanging requests)
- [ ] Retry con backoff exponencial (NO retry inmediato)
- [ ] Máximo de reintentos definido (NO bucles infinitos)
- [ ] DoS involuntario evitado (no saturar API externa)

### Headers Respetados

- [ ] `Retry-After` header respetado (429 Rate Limit)
- [ ] `X-RateLimit-*` headers interpretados
- [ ] Backoff exponencial aplicado en 429/503

---

## 🔍 OWASP API SECURITY TOP 10

### API1: Broken Object Level Authorization

- [ ] IDs de recursos validados antes de enviar
- [ ] No exponer IDs internos directamente
- [ ] Autorización verificada por recurso

### API2: Broken Authentication

- [ ] OAuth2 o autenticación robusta implementada
- [ ] Tokens con expiración
- [ ] Refresh tokens rotados

### API3: Broken Object Property Level Authorization

- [ ] Solo campos necesarios enviados
- [ ] Mass assignment evitado
- [ ] DTOs específicos por operación

### API4: Unrestricted Resource Consumption

- [ ] Rate limiting implementado
- [ ] Paginación usada para listados
- [ ] Timeouts configurados

### API5: Broken Function Level Authorization

- [ ] Permisos verificados antes de operaciones
- [ ] Roles/scopes validados

### API6: Unrestricted Access to Sensitive Business Flows

- [ ] Operaciones críticas con confirmación adicional
- [ ] Alertas en operaciones anómalas

### API7: Server Side Request Forgery (SSRF)

- [ ] URLs validadas antes de requests
- [ ] Whitelist de dominios permitidos
- [ ] NO permitir URLs arbitrarias del usuario

### API8: Security Misconfiguration

- [ ] Configuración de seguridad revisada
- [ ] Secrets en Key Vault (NO hardcodeados)
- [ ] HTTPS obligatorio
- [ ] Error messages genéricos en producción

### API9: Improper Inventory Management

- [ ] APIs consumidas documentadas
- [ ] Versiones de APIs documentadas
- [ ] Deprecaciones monitoreadas

### API10: Unsafe Consumption of APIs

- [ ] Validación de responses de APIs externas
- [ ] No confiar ciegamente en datos externos
- [ ] Sanitización de datos recibidos

---

## 🛡️ RGPD Y PRIVACIDAD

### Datos Personales

- [ ] Datos personales mínimos enviados a API
- [ ] Consentimiento del usuario obtenido (si aplica)
- [ ] Datos enmascarados en logs y telemetría
- [ ] DPO notificado de nueva integración con datos personales
- [ ] Transferencias internacionales evaluadas (RGPD Art. 44-50)

### Derechos de los Interesados

- [ ] Procedimiento de acceso implementado (Art. 15 RGPD)
- [ ] Procedimiento de rectificación implementado (Art. 16 RGPD)
- [ ] Procedimiento de supresión implementado (Art. 17 RGPD)
- [ ] Procedimiento de portabilidad implementado (Art. 20 RGPD)

---

## 🧪 TESTING DE SEGURIDAD

### Pruebas Realizadas

- [ ] Secrets scanning ejecutado (no secrets en código)
- [ ] Dependency scan ejecutado (vulnerabilidades en NuGets)
- [ ] Penetration testing realizado (si API crítica)
- [ ] OWASP ZAP scan ejecutado
- [ ] Man-in-the-middle test realizado

### Casos de Test

- [ ] Test con credenciales inválidas
- [ ] Test con token expirado
- [ ] Test con rate limit excedido
- [ ] Test con datos maliciosos (SQL injection, XSS)
- [ ] Test con timeouts
- [ ] Test con respuestas malformadas

---

## ⚠️ RED FLAGS DE SEGURIDAD

- [ ] ❌ Secrets hardcodeados en código
- [ ] ❌ HTTP en lugar de HTTPS
- [ ] ❌ Validación de certificados SSL deshabilitada
- [ ] ❌ Tokens logueados
- [ ] ❌ Passwords en logs
- [ ] ❌ Retry ilimitado (DoS involuntario)
- [ ] ❌ Deserialización sin validación
- [ ] ❌ Datos personales sin enmascarar en logs
- [ ] ❌ API Keys en appsettings.json (deben estar en Key Vault)
- [ ] ❌ Errores con stack traces en producción

---

## 📋 APROBACIONES REQUERIDAS

- [ ] Code review de seguridad completado
- [ ] Aprobación del Security Champion del equipo
- [ ] Aprobación del DPO (si hay datos personales)
- [ ] Aprobación del responsable de infraestructura
- [ ] Penetration testing aprobado (si API crítica)

---

**Progreso**: ___/80 items completados

**Severidad Alta detectada**: ___
**Severidad Media detectada**: ___
**Severidad Baja detectada**: ___

*Checklist: security-checklist - STIC.IA v3.7.0*
