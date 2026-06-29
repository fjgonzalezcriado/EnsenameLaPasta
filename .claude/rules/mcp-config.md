---
globs:
  - "_duran/ESTADO_PROYECTO.json"
  - "_duran/.mcp-credentials.json"
  - "_duran/.mcp-project.json"
---

# Reglas para configuración MCP STIC.IA (ADR-037 + ADR-038)

> Esta regla se activa al editar archivos de configuración del Hub MCP STIC.IA.
> Previene bugs de privacidad y de duplicación de identidad capturados como lecciones del proyecto.

---

## Modelo de identidad (NO mezclar fuentes)

Hay **tres archivos** distintos con responsabilidades estrictamente separadas:

| Archivo | Scope | Commiteable | Contenido permitido |
|---|---|---|---|
| `_duran/.mcp-project.json` | per-PROYECTO | ✅ sí | `projectId`, `serverUrl`, `registeredAt`, `registeredBy` |
| `_duran/ESTADO_PROYECTO.json.mcpSync` | per-PROYECTO | ✅ sí | `habilitado`, `categorias`, `projectId`, `serverUrl`, `ultimaSync` |
| `_duran/.mcp-credentials.json` | per-DEV | ❌ no (gitignored) | `apiKey`, `devAlias`, `devEmail`, `telemetryOptIn`, `serverUrl`, `projectId` |

---

## Reglas absolutas al editar `_duran/ESTADO_PROYECTO.json`

### ❌ NUNCA hacer

1. **NO añadir `telemetryOptIn` dentro de `mcpSync`**. Ese campo es per-DEV. Si lo encuentras, **elimínalo** (residual pre-v3.9.0). Razón: el JSON se commitea — un dev cambiaría el opt-in de todo el equipo sin consentimiento.
2. **NO añadir `apiKey`** dentro de `mcpSync` ni en ningún sitio de `ESTADO_PROYECTO.json`. Es per-dev y secreto.
3. **NO añadir `devAlias` ni `devEmail`** dentro de `mcpSync`. Son per-dev.
4. **NO modificar `projectId` ni `serverUrl` manualmente**. Los rellena `arranque.ps1` automáticamente.
5. **NO desactivar `habilitado=false` manualmente**. Si no quieres sincronizar, ejecuta `/mcp-forget` que limpia todo.

### ✅ SÍ hacer

1. Editar `mcpSync.categorias.{x}` para activar/desactivar qué se sincroniza (per-proyecto, decisión del equipo).
2. Limpiar campos residuales: si ves `telemetryOptIn` en `mcpSync`, elimínalo y avisa al usuario.

---

## Reglas absolutas al editar `_duran/.mcp-credentials.json`

### ❌ NUNCA hacer

1. **NO commitear este archivo** (está en `.gitignore` por defecto, no quitarlo de ahí).
2. **NO compartir la `apiKey` con otros devs** — cada dev tiene la suya. Si otro dev necesita acceso, debe ejecutar `irm | iex` en su clon para que se una via `/v2/join`.
3. **NO modificar `projectId` ni `apiKey` manualmente** — si están corruptos, ejecutar `/mcp-forget` + re-instalar.

### ✅ SÍ hacer

1. Modificar `telemetryOptIn` si el usuario lo pide explícitamente — pero preferir `/mcp-register` que también sincroniza con el hub vía `POST /v2/opt-in`.

---

## Reglas absolutas al editar `_duran/.mcp-project.json`

Este archivo **se commitea**. Contiene la identidad del proyecto en el hub para que otros devs del equipo se unan al clonar.

### ❌ NUNCA hacer

1. **NO añadir credenciales** (apiKey, devAlias, devEmail) aquí. Es para todo el equipo.
2. **NO modificar `projectId`** — rompería la unión de otros devs y la sincronización.

### ✅ SÍ hacer

1. Mover este archivo si se reorganiza la estructura del repo (raro).

---

## Si el usuario pide "configurar telemetría"

Respuesta correcta: ejecutar `/mcp-register` que:
1. Muestra el privacy notice
2. Pide opt-in explícito (3 opciones)
3. Actualiza `_duran/.mcp-credentials.json.telemetryOptIn` (local, gitignored)
4. POST `/v2/opt-in` al hub → actualiza `mcp.ProyectoDevs.TelemetryOptIn` (per-dev)

**NO** editar `ESTADO_PROYECTO.json.mcpSync.telemetryOptIn` (no existe ese campo por diseño).

---

## Lecciones aprendidas que originaron esta regla

- **2026-05-20**: 3 proyectos quedaron con `mcpSync.telemetryOptIn=true` en JSON commiteable (residual pre-v3.9.0) mientras `.mcp-credentials.json.telemetryOptIn=false`. Inconsistencia detectada al ejecutar `/mcp-sync`.
- **Origen del bug**: convención no documentada. Otro Claude (en sesión futura) podría replicar el error añadiendo el campo al lugar más visible.

> Referencia: ADR-037 (multi-dev) + ADR-038 (identidad por email) en `_estado/DECISIONES.md` del repo Constructor STIC.IA.
