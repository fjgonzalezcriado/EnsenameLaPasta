Integración con Jira Software (consultar, crear, actualizar issues)

# Integración con Jira Software

> **REQUISITO**: Este comando requiere que el MCP Server de Atlassian esté configurado.
> Ejecutar: `claude mcp add --transport sse atlassian https://mcp.atlassian.com/v1/sse --scope user`

---

## Configuración Previa

### Verificar MCP Server

```bash
# En Claude Code
/mcp

# Debe aparecer "atlassian" en la lista de servidores conectados
```

### Configuración del Proyecto

El comando lee la configuración de `_duran/ESTADO_PROYECTO.json`:

```json
{
  "jira": {
    "habilitado": true,
    "proyectoKey": "STIC",
    "url": "https://comillas.atlassian.net"
  }
}
```

---

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| (sin parámetro) | Muestra mis tareas asignadas |
| `--mis-tareas` | Alias de sin parámetro |
| `--buscar <texto>` | Busca issues por texto o JQL |
| `--ver <TICKET-ID>` | Muestra detalles de un ticket |
| `--crear <título>` | Crea nuevo issue |
| `--iniciar <TICKET-ID>` | Inicia trabajo en ticket (crea evolutivo vinculado) |
| `--estado <TICKET-ID> <estado>` | Cambia estado del ticket |
| `--comentar <TICKET-ID> <texto>` | Añade comentario al ticket |

---

## Uso por Parámetro

### Sin parámetro / `--mis-tareas`

Muestra issues asignados al usuario actual:

```
▶ MIS TAREAS EN JIRA
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📋 TAREAS ASIGNADAS (5)

┌─────────────────────────────────────────────────────────────────┐
│ 🔵 STIC-456: Implementar filtro de fechas en búsqueda          │
│ ─────────────────────────────────────────────────────────────── │
│ Estado: In Progress │ Prioridad: Media │ Sprint: 12             │
│ Creado: 2026-01-25 │ Actualizado: hace 2 días                   │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 🟡 STIC-478: Corregir validación de email                      │
│ ─────────────────────────────────────────────────────────────── │
│ Estado: To Do │ Prioridad: Alta │ Sprint: 12                    │
│ Creado: 2026-01-27                                              │
└─────────────────────────────────────────────────────────────────┘

📊 RESUMEN
┌─────────────────────────────────────────────────┐
│  To Do:        2                                │
│  In Progress:  2                                │
│  In Review:    1                                │
└─────────────────────────────────────────────────┘

💡 Comandos:
   /jira --ver STIC-456      → Ver detalles
   /jira --iniciar STIC-478  → Iniciar trabajo (crea evolutivo)
```

---

### `--buscar <texto>`

Busca issues por texto libre o JQL:

```bash
# Búsqueda simple
/jira --buscar "filtro fechas"

# JQL avanzado
/jira --buscar "project = STIC AND status = 'To Do' AND priority = High"
```

**Output:**

```
▶ RESULTADOS DE BÚSQUEDA
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Query: filtro fechas
Resultados: 3 issues encontrados

| Ticket    | Título                              | Estado      | Asignado   |
|-----------|-------------------------------------|-------------|------------|
| STIC-456  | Implementar filtro de fechas        | In Progress | @hvaler    |
| STIC-234  | Filtro de fechas en exportación     | Done        | @jgarcia   |
| STIC-123  | Bug: filtro no guarda fechas        | Closed      | @mlopez    |

💡 /jira --ver STIC-456 para más detalles
```

---

### `--ver <TICKET-ID>`

Muestra detalles completos de un ticket:

```
▶ DETALLE DE TICKET
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

╔══════════════════════════════════════════════════════════════╗
║  STIC-456: Implementar filtro de fechas en búsqueda          ║
╠══════════════════════════════════════════════════════════════╣
║  Tipo: Task          │ Prioridad: Media                      ║
║  Estado: In Progress │ Resolución: -                         ║
║  Sprint: Sprint 12   │ Story Points: 5                       ║
╚══════════════════════════════════════════════════════════════╝

📝 DESCRIPCIÓN
───────────────────────────────────────────────────────────────
Añadir filtro por rango de fechas en la pantalla de búsqueda
de solicitudes. El usuario debe poder:
- Seleccionar fecha desde
- Seleccionar fecha hasta
- Limpiar filtro
───────────────────────────────────────────────────────────────

👤 PERSONAS
┌─────────────────────────────────────────────────┐
│ Creador:   Juan García (jgarcia@comillas.edu)   │
│ Asignado:  HV (hvaler@comillas.edu)             │
│ Reporter:  María López                          │
└─────────────────────────────────────────────────┘

📅 FECHAS
┌─────────────────────────────────────────────────┐
│ Creado:      2026-01-25 10:30                   │
│ Actualizado: 2026-01-28 15:45                   │
│ Due Date:    2026-02-01                         │
└─────────────────────────────────────────────────┘

💬 ÚLTIMOS COMENTARIOS (2)
───────────────────────────────────────────────────────────────
[2026-01-28] HV: Iniciada implementación del componente DatePicker
[2026-01-26] JG: Añadido mockup de diseño en Figma
───────────────────────────────────────────────────────────────

🔗 ENLACES
├── Evolutivo local: HV-25 (vinculado)
├── PR: #123 (abierto)
└── Figma: [mockup-filtro-fechas]

💡 Acciones:
   /jira --estado STIC-456 "In Review"  → Cambiar estado
   /jira --comentar STIC-456 "texto"    → Añadir comentario
```

---

### `--crear <título>`

Crea un nuevo issue en Jira:

```bash
# Crear tarea simple
/jira --crear "Añadir validación de DNI en formulario"

# Con opciones
/jira --crear --tipo Bug --prioridad Alta "Error en exportación PDF"
```

**Flujo interactivo:**

```
▶ CREAR NUEVO ISSUE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Título: Añadir validación de DNI en formulario

¿Tipo de issue?
  1. Task (por defecto)
  2. Bug
  3. Story
  4. Epic

> 1

¿Prioridad?
  1. Low
  2. Medium (por defecto)
  3. High
  4. Critical

> 2

¿Añadir descripción? (Enter para omitir)
> Validar formato DNI español (8 dígitos + letra)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ ISSUE CREADO

STIC-512: Añadir validación de DNI en formulario
URL: https://comillas.atlassian.net/browse/STIC-512

¿Iniciar trabajo ahora? [s/N]
> s

✅ Evolutivo STIC-512 creado y vinculado
   Rama: feature/STIC-512
```

---

### `--iniciar <TICKET-ID>`

Inicia trabajo en un ticket de Jira (crea evolutivo vinculado):

```
▶ INICIAR TRABAJO EN TICKET
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Ticket: STIC-456 - Implementar filtro de fechas en búsqueda

Acciones a realizar:
1. ✅ Cambiar estado en Jira: To Do → In Progress
2. ✅ Crear evolutivo local vinculado
3. ✅ Crear rama: feature/STIC-456
4. ✅ Actualizar ESTADO_PROYECTO.json

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ TRABAJO INICIADO

Evolutivo: STIC-456
Rama: feature/STIC-456
Estado Jira: In Progress

📝 Spec creado: _duran/specs/STIC-456.md

💡 Comandos útiles:
   /estado                           → Ver progreso
   /commit "mensaje"                 → Commit (incluye [STIC-456])
   /jira --comentar STIC-456 "..."   → Actualizar Jira
   /finalizar-evolutivo              → Cierra ticket automáticamente
```

---

### `--estado <TICKET-ID> <nuevo-estado>`

Cambia el estado de un ticket:

```bash
/jira --estado STIC-456 "In Review"
```

**Output:**

```
▶ CAMBIAR ESTADO
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Ticket: STIC-456
Estado actual: In Progress
Nuevo estado: In Review

✅ Estado actualizado correctamente

Transición: In Progress → In Review
```

---

### `--comentar <TICKET-ID> <texto>`

Añade un comentario al ticket:

```bash
/jira --comentar STIC-456 "Implementación completada, pendiente de tests"
```

**Output:**

```
▶ AÑADIR COMENTARIO
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Ticket: STIC-456

Comentario añadido:
"Implementación completada, pendiente de tests"

✅ Comentario publicado correctamente
```

---

## Integración con Evolutivos

### Cuando existe un ticket Jira vinculado

Si el evolutivo actual está vinculado a un ticket Jira, los comandos se comportan así:

| Comando | Comportamiento con Jira |
|---------|-------------------------|
| `/commit "mensaje"` | Incluye `[STIC-XXX]` en el mensaje |
| `/finalizar-evolutivo` | Cambia estado en Jira a "Done" + comentario |
| `/pausar` | Añade comentario en Jira indicando pausa |
| `/continuar` | Cambia estado a "In Progress" si estaba pausado |

### Ejemplo de `/finalizar-evolutivo` con Jira

```
▶ FINALIZAR EVOLUTIVO
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Evolutivo: STIC-456
Vinculado a: Jira STIC-456

Acciones a realizar:
1. ✅ Merge rama feature/STIC-456 → main
2. ✅ Actualizar estado Jira: In Progress → Done
3. ✅ Añadir comentario en Jira con resumen de cambios
4. ✅ Marcar evolutivo como completado

Resumen de cambios:
- 8 archivos modificados
- 3 commits: feat(filtros): implementar DatePicker...

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ EVOLUTIVO FINALIZADO

Jira STIC-456 actualizado:
- Estado: Done
- Comentario: "Implementación completada. 8 archivos, 3 commits."
```

---

## Mapeo de Estados

| Estado Evolutivo | Estado Jira | Transición |
|------------------|-------------|------------|
| `pendiente` | To Do | Al crear |
| `en_progreso` | In Progress | Al `/jira --iniciar` o `/continuar` |
| `pausado` | In Progress | Mantiene (añade comentario) |
| `en_revision` | In Review | Al crear PR |
| `completado` | Done | Al `/finalizar-evolutivo` |

---

## Configuración Avanzada

### En `_duran/ESTADO_PROYECTO.json`

```json
{
  "jira": {
    "habilitado": true,
    "proyectoKey": "STIC",
    "url": "https://comillas.atlassian.net",
    "tipoIssueDefault": "Task",
    "autoSyncEstado": true,
    "autoComentario": true,
    "prefijoBranch": "feature/"
  }
}
```

### Opciones de comportamiento

| Opción | Descripción | Default |
|--------|-------------|---------|
| `autoSyncEstado` | Sincroniza estados automáticamente | `true` |
| `autoComentario` | Añade comentarios automáticos en commits | `true` |
| `prefijoBranch` | Prefijo para ramas vinculadas | `feature/` |

---

## Sin MCP Server configurado

Si el MCP Server de Atlassian no está configurado, el comando mostrará:

```
⚠️ MCP SERVER DE ATLASSIAN NO CONFIGURADO

Para usar /jira, necesitas configurar el MCP Server:

1. Ejecutar en terminal:
   claude mcp add --transport sse atlassian https://mcp.atlassian.com/v1/sse --scope user

2. Autenticarte en el navegador con tu cuenta de Atlassian

3. Verificar conexión:
   /mcp

📚 Más información: Documentacion/03_Desarrolladores/INTEGRACION_JIRA.md
```

---

## Resolución de Problemas

### Error de autenticación

```
❌ Error de autenticación con Atlassian

Posibles causas:
1. Token expirado - Re-autenticar con:
   claude mcp remove atlassian
   claude mcp add --transport sse atlassian https://mcp.atlassian.com/v1/sse --scope user

2. Permisos insuficientes - Verificar acceso al proyecto STIC

3. Sin conexión - Verificar conectividad a comillas.atlassian.net
```

### Ticket no encontrado

```
❌ Ticket STIC-999 no encontrado

Verifica:
- El ID del ticket es correcto
- Tienes acceso al proyecto STIC
- El ticket no ha sido eliminado
```

---

*Comando añadido en v3.7.0 - Integración con Jira Software*
