# Regla: Azure DevOps Awareness

> Esta regla se aplica SIEMPRE en proyectos que usen Azure DevOps (tfs.comillas.edu).
> Tiene 2 modos: **pasivo** (default, solo sugiere) y **activo** (opt-in, sincroniza automaticamente).
> El modo se lee de `_duran/ESTADO_PROYECTO.json → infraestructura.azureDevOps.autoSync`.

---

## Deteccion

Esta regla se activa cuando:
- `_duran/ESTADO_PROYECTO.json` tiene `infraestructura.cicd.plataforma` != null
- O el proyecto tiene repositorio en `tfs.comillas.edu`
- O el usuario menciona Azure DevOps, Boards, PBIs, work items

Si `infraestructura.cicd.plataforma` es null y no hay indicios de AzDO, esta regla no aplica.

---

## Modo de operacion

Leer `_duran/ESTADO_PROYECTO.json → infraestructura.azureDevOps.autoSync`:

| Valor | Modo | Comportamiento |
|-------|------|----------------|
| `false` o ausente | **Pasivo** (default) | Solo muestra recordatorios. NO ejecuta nada contra AzDO. |
| `true` | **Activo** (opt-in) | Crea/actualiza PBIs automaticamente al usar /nuevo-evolutivo, /finalizar-evolutivo, etc. |

Para activar el modo activo, el usuario debe:
1. Haber ejecutado `/devops-sync` al menos una vez (para que existan epicId y features)
2. Establecer `autoSync: true` en ESTADO_PROYECTO.json (manual o via /devops-sync --enable-auto)

---

## MODO PASIVO (default) — Solo recordatorios

### Tras /finalizar-evolutivo

```
💡 Evolutivo [CODIGO] completado. ¿Quieres actualizar el PBI en Azure DevOps?
   → /devops-sync --update-pbi [CODIGO]
   → O manualmente en tfs.comillas.edu → Boards → mover a Done
```

### Tras /commit con conventional commit

```
💡 Commit: fix(becas): corregir validacion de fecha
   → Si hay un Bug asociado en Boards, considera referenciar: #PBI-XXX
   → Azure DevOps asocia commits automaticamente si incluyes #ID en el mensaje
```

### Al crear endpoints nuevos (Controllers, Minimal APIs)

```
💡 Nuevo endpoint detectado: POST /api/becas
   → ¿Existe un PBI en Boards para esta funcionalidad?
   → Considera ejecutar /devops-sync para actualizar la Wiki de endpoints
```

### Al detectar configuracion AzDO en ESTADO_PROYECTO.json

Si `infraestructura.azureDevOps.epicId` existe, al inicio de sesion mostrar contexto:

```
📋 Azure DevOps: Epic #{epicId} activo
   Features en progreso: [lista de features]
   Ultima sincronizacion: [fecha]
   Modo: pasivo (sugerencias) — activar con /devops-sync --enable-auto
```

### Tras /analizar o /analisis-arquitectura

```
💡 Analisis completado. ¿Quieres sincronizar hallazgos con Azure DevOps?
   → /devops-sync genera PBIs desde hallazgos de seguridad, deuda tecnica, etc.
```

---

## MODO ACTIVO (opt-in, autoSync: true) — Sincronizacion automatica

> **PREREQUISITO**: `azureDevOps.autoSync = true` Y `azureDevOps.epicId` != null
> Si falta alguno, caer al modo pasivo.

### Deteccion de proceso (CRITICO)

El proyecto puede usar **Scrum** o **Basic**. Leer `azureDevOps.proceso` de ESTADO_PROYECTO.json.
Si no existe, detectar con la API: `GET /_apis/projects/{project}?api-version=6.0` → campo `capabilities.processTemplate.templateName`.

| Proceso | Jerarquia | Work item tipo | Estados |
|---------|-----------|----------------|---------|
| **Scrum** | Epic → Feature → PBI | Product Backlog Item | New → Approved → **Committed** → Done |
| **Basic** | Epic → Issue → Task | Issue | **To Do** → **Doing** → **Done** |

> **SIEMPRE** verificar el proceso antes de crear work items o transicionar estados.
> Usar el estado incorrecto devuelve error 400.

### Crear work items automaticamente

**Si proceso = Scrum:**

| Evento | Accion en Boards |
|--------|-----------------|
| `/nuevo-evolutivo` (HV-*) | Crear PBI titulo `{codigo}: {titulo}`, estado **Committed** |
| Nueva DT en DEUDA_TECNICA.md | Crear PBI titulo `{codigo}: {titulo}`, estado **New** |
| DT que se empieza a trabajar | Transicionar PBI a **Committed** |

**Si proceso = Basic:**

| Evento | Accion en Boards |
|--------|-----------------|
| `/nuevo-evolutivo` (HV-*) | Crear Issue titulo `{codigo}: {titulo}`, estado **Doing** |
| Nueva DT en DEUDA_TECNICA.md | Crear Issue titulo `{codigo}: {titulo}`, estado **To Do** |
| DT que se empieza a trabajar | Transicionar Issue a **Doing** |

**Mapeo area → agrupacion (ambos procesos):**

En Scrum se agrupan bajo Features. En Basic se agrupan con tags (no hay Features).

| Area detectada | Scrum: Feature padre | Basic: Tag |
|----------------|---------------------|------------|
| Endpoints, datos, CRUD | `features.endpoints` | `API-Endpoints` |
| APIs externas, HttpClient | `features.integraciones` | `Integraciones` |
| Auth, headers, CORS, seguridad | `features.seguridad` | `Seguridad` |
| Logging, health checks, telemetria | `features.observabilidad` | `Observabilidad` |
| Tests, refactor, DTs | `features.calidad` | `Calidad` |

**Prioridad automatica:**

| Tipo | Prioridad AzDO |
|------|----------------|
| HV-* (evolutivo) | 1 |
| DT-* Alta | 1 |
| DT-* Media | 2 |
| DT-* Baja | 3 |

**Ejecucion:** Usar Azure DevOps REST API via curl o MCP si esta disponible:

```bash
# Leer config
URL=$(jq -r '.infraestructura.azureDevOps.url' _duran/ESTADO_PROYECTO.json)
FEATURE_ID=$(jq -r '.infraestructura.azureDevOps.features.{area}' _duran/ESTADO_PROYECTO.json)

# Lookup displayName del asignado (CRITICO en TFS on-premises)
# System.AssignedTo REQUIERE displayName, NO acepta email/UPN en on-premises.
# Fuente: equipo.miembros[x].nombre donde usuario == evolutivo.asignadoA
ASIGNADO_ALIAS=$(jq -r '.evolutivos.enProgreso[] | select(.codigo=="{CODIGO}") | .asignadoA' _duran/ESTADO_PROYECTO.json)
ASIGNADO_DISPLAY=$(jq -r --arg a "$ASIGNADO_ALIAS" '.equipo.miembros[] | select(.usuario==$a) | .nombre' _duran/ESTADO_PROYECTO.json)

# Crear PBI con parent link + assignee
# Si ASIGNADO_DISPLAY esta vacio, omitir System.AssignedTo (NO pasar email/alias)
curl -s --negotiate -u : \
  # Scrum: Product Backlog Item / Basic: Issue
  "${URL}/_apis/wit/workitems/\$${WORK_ITEM_TYPE}?api-version=6.0" \
  -X POST -H "Content-Type: application/json-patch+json" \
  -d '[
    {"op":"add","path":"/fields/System.Title","value":"{CODIGO}: {titulo}"},
    {"op":"add","path":"/fields/System.Description","value":"{descripcion}"},
    {"op":"add","path":"/fields/Microsoft.VSTS.Common.Priority","value":{prioridad}},
    {"op":"add","path":"/fields/System.AssignedTo","value":"'"$ASIGNADO_DISPLAY"'"},
    {"op":"add","path":"/relations/-","value":{
      "rel":"System.LinkTypes.Hierarchy-Reverse",
      "url":"${URL}/_apis/wit/workitems/${PARENT_ID}"
    }}
  ]'
  # WORK_ITEM_TYPE: "Product%20Backlog%20Item" (Scrum) o "Issue" (Basic)
  # PARENT_ID: Feature ID (Scrum) o Epic ID (Basic, no hay Features)
```

> **IMPORTANTE `System.AssignedTo` en TFS on-premises**: usar **displayName** (ej. `Francisco Javier Gonzalez Criado`), NO email. Pasar `fjgonzalez@comillas.edu` devuelve HTTP 400 `unknown identity`. Si no hay match en `equipo.miembros`, OMITIR el campo (trabajo sin asignar) antes que enviar alias/email invalido.

### Transicionar a Done (ambos procesos)

| Evento | Scrum | Basic |
|--------|-------|-------|
| `/finalizar-evolutivo` | PBI → **Done** | Issue → **Done** |
| DT marcada ✅ Completado | PBI → **Done** | Issue → **Done** |
| DT marcada ❌ Descartada | PBI → **Done** + nota | Issue → **Done** + nota |

```bash
# Buscar PBI por titulo
curl -s --negotiate -u : "${URL}/_apis/wit/wiql?api-version=6.0" \
  -X POST -H "Content-Type: application/json" \
  -d '{"query":"SELECT [System.Id] FROM WorkItems WHERE [System.Title] CONTAINS '\''{CODIGO}'\'' AND [System.WorkItemType] = '\''Product Backlog Item'\''"}'

# Transicionar a Done
curl -s --negotiate -u : "${URL}/_apis/wit/workitems/{PBI_ID}?api-version=6.0" \
  -X PATCH -H "Content-Type: application/json-patch+json" \
  -d '[{"op":"add","path":"/fields/System.State","value":"Done"}]'
```

### Coherencia de Features

Tras crear o cerrar PBIs, verificar:
- Si una Feature tiene PBIs pendientes → Feature debe estar **Committed**
- Si todos los PBIs de una Feature estan Done → Feature puede estar **Done**

### Si hay error de conectividad

**NO bloquear el trabajo.** Mostrar warning y continuar:

```
⚠️ No se pudo sincronizar con Azure DevOps (sin conectividad).
   PBI pendiente de crear: {CODIGO}: {titulo}
   Se reintentara en la proxima operacion o con /devops-sync --retry
```

Guardar operaciones pendientes en `_duran/devops_pending_ops.json`:

```json
[
  {
    "tipo": "crear_pbi",
    "codigo": "HV-19",
    "titulo": "Filtro de becas por estado",
    "featureArea": "endpoints",
    "prioridad": 1,
    "fecha": "2026-04-16",
    "reintentos": 0
  }
]
```

### Resumen tras cada sincronizacion

```
✅ Azure DevOps actualizado:
   PBI #1234 "HV-19: Filtro de becas" → Committed
```

---

## Trazabilidad Git ↔ Azure DevOps

### Formato de referencia en commits (ambos modos)

Cuando el proyecto tiene AzDO configurado, los commits deben incluir referencia al work item:

```
feat(becas): añadir filtro por estado #1234

fix(auth): corregir token refresh #1456
```

Azure DevOps asocia automaticamente el commit al work item si se incluye `#ID`.

### Formato en ramas (ambos modos)

Si `configuracion.branching.convencionRamas` incluye `{codigo}`, el codigo debe coincidir
con el ID del PBI/Bug en Boards cuando sea posible:

```
feature/1234-filtro-becas
bugfix/1456-token-refresh
```

---

## Configuracion en ESTADO_PROYECTO.json

```json
{
  "infraestructura": {
    "azureDevOps": {
      "url": "https://tfs.comillas.edu/COLECCION/PROYECTO",
      "teamName": "Equipo Principal",
      "epicId": 1234,
      "features": {
        "endpoints": 1235,
        "integraciones": 1236,
        "seguridad": 1237,
        "observabilidad": 1238,
        "calidad": 1239
      },
      "proceso": "Scrum",
      "_proceso_opciones": ["Scrum", "Basic", "Agile", "CMMI"],
      "autoSync": false,
      "ultimaSync": null,
      "pendingOps": "_duran/devops_pending_ops.json"
    }
  }
}
```

| Campo | Descripcion |
|-------|-------------|
| `autoSync` | `false` = modo pasivo (default). `true` = modo activo (opt-in). |
| `features` | Mapeo area → ID de Feature en AzDO. Se configura con `/devops-sync`. |
| `pendingOps` | Ruta al archivo de operaciones pendientes por error de conectividad. |

Si `azureDevOps` no existe, no mostrar recordatorios.
Si existe pero `epicId` es null, sugerir `/devops-sync` para primera configuracion.
Si `autoSync` es true pero `epicId` es null, caer a modo pasivo con warning.

---

## NOTAS TECNICAS — TFS ON-PREMISES (CRITICO)

### Estados por proceso

**Proceso Scrum** (Epic → Feature → PBI):

| Estado | Uso |
|--------|-----|
| **New** | Pendiente, recien creado |
| **Approved** | Aprobado en backlog |
| **Committed** | En desarrollo activo |
| **Done** | Completado |

**Proceso Basic** (Epic → Issue → Task):

| Estado | Uso |
|--------|-----|
| **To Do** | Pendiente |
| **Doing** | En desarrollo activo |
| **Done** | Completado |

> **NUNCA usar "In Progress"** — no existe en ningun proceso de TFS.
> Detectar proceso del proyecto ANTES de crear/transicionar work items.

### Formato widgets Markdown en Dashboard

El campo `settings` del widget Markdown debe contener **markdown crudo directamente**,
NO envuelto en `{"content":"..."}`. TFS on-premises (v2020) no parsea el JSON wrapper.

```
# CORRECTO (TFS on-premises):
"settings": "## Titulo\n\n| Col | Val |\n|---|---|\n| A | B |"

# INCORRECTO (muestra JSON como texto plano):
"settings": json.dumps({"content": "## Titulo\n\n..."})
```

ContributionId: `ms.vss-dashboards-web.Microsoft.VisualStudioOnline.Dashboards.MarkdownWidget`

Para actualizar widgets existentes: PUT del dashboard completo (no widget individual) con eTag.

### API version

Usar `api-version=6.0` para la mayoria de endpoints en Azure DevOps Server 2020 (tfs.comillas.edu).
No usar `api-version=7.0` que es para Azure DevOps cloud.

**Excepciones que requieren `-preview`** en TFS on-premises:

| Endpoint | api-version correcta |
|---|---|
| `/_apis/work/backlogs` | **5.1-preview** (no acepta 6.0 sin -preview) |
| `/_apis/work/backlogs/{id}/workItems` | **5.1-preview** |
| Otros `/_apis/work/*` | probar `5.1-preview` o `6.0-preview.1` antes de asumir que no funciona |

Patron general: si recibes HTTP 400 o "api-version not supported" en un endpoint, probar con `{version}-preview` antes de descartar.

### System.AssignedTo requiere displayName, NO email

TFS on-premises **no resuelve** email/UPN como identidad. El campo `System.AssignedTo` acepta **solo displayName** tal cual aparece en Active Directory.

```
# CORRECTO (TFS on-premises):
{"op":"add","path":"/fields/System.AssignedTo","value":"Francisco Javier Gonzalez Criado"}

# INCORRECTO (devuelve HTTP 400 "unknown identity"):
{"op":"add","path":"/fields/System.AssignedTo","value":"fjgonzalez@comillas.edu"}
{"op":"add","path":"/fields/System.AssignedTo","value":"fgonzalez"}
```

**Fuente del displayName**: `equipo.miembros[x].nombre` de ESTADO_PROYECTO.json (donde `usuario == alias_git`). Si el usuario no esta en `equipo.miembros` o el campo `nombre` esta vacio, **omitir `System.AssignedTo` del payload** — dejar work item sin asignar es preferible a fallar con 400.

Alternativa para descubrir displayName en runtime: `GET /_apis/identities?searchFilter=General&filterValue={alias}&api-version=6.0`.

> NOTA: En Azure DevOps cloud (dev.azure.com) si se acepta email/UPN. Esta restriccion es exclusiva de la instalacion on-premises (TFS/Azure DevOps Server).

---

## NO hacer (en ambos modos)

- **NO bloquear** el flujo de trabajo por falta de PBI asociado o error de conectividad
- **NO crear** PBIs en modo pasivo (solo sugerir)
- **NO activar** modo activo sin que el usuario lo haya configurado explicitamente
- **NO asumir** conectividad — siempre manejar error con graceful degradation
- **NO usar "In Progress"** en estados PBI — usar **Committed** (proceso Scrum)
- **NO envolver markdown en JSON** para widgets dashboard — usar markdown crudo
- **NO usar api-version=7.0** — usar **6.0** (maximo para TFS 2020 on-premises)
- **NO pasar email/UPN a System.AssignedTo** — usar displayName exacto de AD (ej. `Francisco Javier Gonzalez Criado`), nunca `fjgonzalez@comillas.edu`

---

*Regla condicional v3.8.2 - Azure DevOps Awareness (pasivo + activo opt-in) - Fix TFS bugs*
