Redactar un Work Item «Eval gap» tras un fallo de smoke test del consumidor

# /eval-gap - Redactar Eval gap para el constructor

> Ayuda a documentar un fallo reproducible de un smoke test (`stic-smoke-tests/`)
> y prepara la descripcion + curl REST para abrir un Work Item «Eval gap» en
> el proyecto del constructor (`ClaudeCodeSTIC` en `tfs.comillas.edu`).

---

## REGLAS CRITICAS

- **NO crear el Work Item** sin confirmacion explicita del usuario.
- **Verificar duplicados** ANTES de proponer creacion (WIQL).
- **Solo abrir gap** si el smoke fallo reproduciblemente >=2 veces (no flaky).
- **Solo abrir gap** si los evals del constructor estan en verde (sino, el fallo
  no es un gap — es un bug ya conocido del constructor).

---

## Parametros

| Parametro | Descripcion |
|---|---|
| (sin parametro) | Drafter interactivo: te pregunta y compone el Work Item |
| `--smoke <01|02|03>` | Pre-rellena el smoke especifico que fallo |
| `--skill <name>` | Pre-rellena el skill afectado |
| `--from-log <path>` | Lee un fichero de log de fallo y extrae la info |
| `--list-open` | Solo lista los Eval gap abiertos (no propone creacion) |
| `--dry-run` | Genera la descripcion + curl pero NO ejecuta nada (default) |
| `--live` | Tras revisar, ejecuta el curl POST contra TFS (requiere confirmacion) |

---

## Fase 1: Verificar pre-requisitos

```
================================================================
  /eval-gap - PRE-REQUISITOS
================================================================
```

1. Existe `.claude/stic-smoke-tests/eval-gap-template.md` (instalado por
   `arranque.ps1` v3.8.4+). Si no existe, abortar y sugerir update.
2. `_duran/ESTADO_PROYECTO.json` tiene `infraestructura.azureDevOps.url`. Si no,
   pedirla manualmente.
3. Conectividad a `tfs.comillas.edu` (verificar con `curl --negotiate -u : <URL>/_apis/projects?api-version=6.0`).

---

## Fase 2: Recopilar contexto

Preguntar al usuario (o leer de `--from-log` si se especifica):

| Pregunta | Ejemplo |
|---|---|
| ¿Que smoke fallo? | `02-compilation.md` |
| ¿Que skill esperabas que se activase? | `generador-crud` |
| ¿Que skill se activo (si alguna)? | (ninguna) o `analisis-arquitectura` |
| ¿Cual fue el prompt exacto? | "Genera CRUD para `_SmokeTestEntity`..." |
| ¿Cual fue el error/output? | (pegar primeras 30 lineas) |
| ¿Reprodujiste el fallo >=2 veces? | sí/no |
| ¿Bloquea release? | sí (prio 1) / no (prio 2) |

Tambien leer del entorno automaticamente:

- Branch + commit SHA (`git rev-parse HEAD`).
- Stack del proyecto desde `_duran/ESTADO_PROYECTO.json`:
  - `proyecto.nombre`, `infraestructura.entornos`, NuGets pinados.
- Version del ecosistema desde `Publicacion/VERSION.json` o de la propia ruta
  `arranque.ps1` instalada.

---

## Fase 3: Buscar duplicados (WIQL)

Antes de proponer crear, lanzar:

```sql
SELECT [System.Id], [System.Title], [System.Tags], [System.State]
FROM WorkItems
WHERE [System.TeamProject] = 'ClaudeCodeSTIC'
  AND [System.Tags] CONTAINS 'eval-gap'
  AND [System.Tags] CONTAINS '{skill}'
  AND [System.State] IN ('To Do', 'Doing', 'New', 'Approved', 'Committed')
```

Mostrar resultados al usuario:

```
================================================================
  Eval gaps abiertos para skill `generador-crud`:

  #1234 [P2] [Eval gap] generador-crud: introduce EF Core en net48 (Doing, HV)
  #1287 [P2] [Eval gap] generador-crud: SP sin SET NOCOUNT ON  (To Do, HV)

  Crear nuevo igualmente? (s/N)
================================================================
```

Si el usuario escribe `n`, abortar — sugerir comentar en el item existente.

---

## Fase 4: Componer el Work Item

Generar la descripcion siguiendo `eval-gap-template.md`:

- Titulo:  `[Eval gap] {skill}: {sintoma corto}` (max 80 chars).
- Tags:    `eval-gap; smoke-{01|02|03}; {skill}`.
- Area:    `ClaudeCodeSTIC/Calidad`.
- Priority: `1` si bloquea release, `2` por defecto.
- Description: render del template MD con todos los campos rellenados.

Mostrar preview al usuario:

```
================================================================
  PREVIEW del Work Item (no creado todavia)
================================================================

  Title:    [Eval gap] generador-crud: introduce EF Core en net48
  Tags:     eval-gap; smoke-02; generador-crud
  Area:     ClaudeCodeSTIC/Calidad
  Priority: 2
  Assigned: HV (mantenedor del skill)

  Description (truncada a 30 lineas):
  ----------------------------------------------------------------
  ## Resumen
  El skill generador-crud introduce 'using Microsoft.EntityFrameworkCore;'
  en GestorLicencias, que es .NET 4.8 + Dapper. Build falla con CS0246.

  ## Smoke test que fallo
  | Repo consumidor | GestorLicencias / main / a1b2c3d |
  ...
  ----------------------------------------------------------------

  Acciones:
    [1] Crear ahora (curl POST a tfs.comillas.edu)
    [2] Copiar al portapapeles para crear desde la UI web
    [3] Guardar como _duran/eval-gaps/draft-{timestamp}.md y salir
    [4] Cancelar
================================================================
```

---

## Fase 5: Crear (solo con --live + confirmacion)

Si el usuario confirma `[1]`:

```bash
URL=$(jq -r '.infraestructura.azureDevOps.url' _duran/ESTADO_PROYECTO.json | sed 's|/[^/]*$||')
WIT_TYPE="Issue"   # o "Product%20Backlog%20Item" segun proceso

curl -s --negotiate -u : \
  "${URL}/ClaudeCodeSTIC/_apis/wit/workitems/\$${WIT_TYPE}?api-version=6.0" \
  -X POST \
  -H "Content-Type: application/json-patch+json" \
  --data-binary "@/tmp/eval-gap-payload.json"
```

Donde `/tmp/eval-gap-payload.json` es el JSON-Patch con todos los campos.

Tras el POST exitoso, mostrar el numero del Work Item creado y guardar
referencia en `_duran/eval-gaps/`:

```
{
  "wi_id": 1342,
  "url": "https://tfs.comillas.edu/.../_workitems/edit/1342",
  "skill": "generador-crud",
  "smoke": "02-compilation.md",
  "consumer": "GestorLicencias",
  "created_at": "2026-04-20T15:30:00",
  "branch": "main",
  "sha": "a1b2c3d"
}
```

---

## Fase 6 (opcional): notificar

Si el usuario lo pide, generar un resumen para Teams/email del mantenedor:

```
Asunto: [Eval gap #1342] generador-crud: introduce EF Core en net48

Hola HV,

Smoke test 02-compilation fallo en GestorLicencias (rama main, SHA a1b2c3d).
El skill generador-crud introduce 'using Microsoft.EntityFrameworkCore;' en
un proyecto .NET 4.8 + Dapper, lo que rompe el build con error CS0246.

Detalles: https://tfs.comillas.edu/.../_workitems/edit/1342

Prioridad: P2. SLA: 1 sprint del constructor.
```

---

## Fallback si no hay conectividad TFS

Si la peticion REST falla por conectividad o auth, **NUNCA** bloquear al
usuario. Guardar el draft en `_duran/eval-gaps/pending-{timestamp}.md` con la
descripcion completa, y mostrar:

```
⚠️  No se pudo crear el Work Item ahora (sin conectividad / 401).

Draft guardado en:
  _duran/eval-gaps/pending-20260420-153012.md

Cuando tengas conexion:
  - Re-ejecuta /eval-gap --from-log _duran/eval-gaps/pending-20260420-153012.md --live
  - O copia/pega la descripcion en la UI de tfs.comillas.edu
```

---

## Salida del comando

```
================================================================
  /eval-gap COMPLETADO
================================================================
  Work Item:    #1342 (creado / draft / cancelado)
  Smoke:        02-compilation.md (FAIL)
  Skill:        generador-crud
  Prioridad:    P2
  SLA:          ~1 sprint constructor (~14 dias)
  URL:          https://tfs.comillas.edu/.../_workitems/edit/1342

  Proximos pasos del mantenedor:
    1. Reproducir el caso en ClaudeCodeSTIC
    2. Anadir eval negativo a `eval-set.json` del skill
    3. Ajustar description (USE FOR / DO NOT USE FOR) si aplica
    4. PR contra main → pipeline `validate-skills` debe pasar
    5. Cerrar Work Item con commit referencia
================================================================
```

---

## Anti-patrones

- NO abrir gap si el smoke nunca paso en este proyecto (es bug del consumidor).
- NO abrir si el constructor tiene el pipeline `validate-skills` en rojo
  (esperar a que se estabilice).
- NO inventar campos del template — si falta info, preguntar.
- NO crear con `--live` sin haber mostrado preview y obtener confirmacion.

---

## Referencia

- Template: `.claude/stic-smoke-tests/eval-gap-template.md`
- Smoke tests: `.claude/stic-smoke-tests/{01-activation,02-compilation,03-conventions}.md`
- Politica completa: ADR-029 en el constructor (`_estado/DECISIONES.md`)
