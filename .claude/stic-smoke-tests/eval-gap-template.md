# Plantilla · Work Item «Eval gap»

> **Cuando usarla**: cuando un smoke test del consumidor falla reproduciblemente
> y los evals del constructor (`ClaudeCodeSTIC`) estan en verde. El gap es una
> evidencia de que el constructor no captura una particularidad de este stack.

---

## Convencion (no requiere customizar TFS)

No creamos un WIT nuevo en el process template. **Reusamos el tipo nativo del
proyecto** (`Issue` en Basic, `Product Backlog Item` en Scrum) con:

| Campo | Valor |
|---|---|
| **Title** | `[Eval gap] {skill}: {sintoma corto}` |
| **Tags** | `eval-gap; smoke-{01|02|03}; {skill}` |
| **Area Path** | `ClaudeCodeSTIC/Calidad` |
| **Iteration** | Sprint actual del constructor |
| **Priority** | `2` (default) — `1` si bloquea release |
| **Parent** | Epic «Calidad del ecosistema» (si existe en el proyecto del constructor) |
| **Assigned To** | Mantenedor del skill afectado (HV por defecto) |
| **State** | `To Do` (Basic) / `New` (Scrum) — **NUNCA** `In Progress` |

---

## Plantilla de descripcion (copiar tal cual y rellenar)

```markdown
## Resumen
{Una frase que explique el gap. Ejemplo: "El skill generador-crud introduce
'using Microsoft.EntityFrameworkCore' en proyectos .NET 4.8 + Dapper".}

## Smoke test que fallo

| Campo | Valor |
|---|---|
| Repo consumidor | {repo + branch + commit SHA} |
| Smoke ejecutado | `01-activation.md` / `02-compilation.md` / `03-conventions.md` |
| Skill esperada / activada | {esperada} / {obtenida} |
| Resultado | FAIL ({pass}/{total} tests) |

## Reproduccion (pasos minimos)

1. `git clone <repo> && git checkout <branch>`
2. `arranque.ps1 -InstallMode update`
3. `claude -p "{prompt exacto del test}"`
4. `dotnet build` (o el comando del proyecto)
5. **Esperado**: build OK · **Obtenido**: {error}

## Stack relevante

- TargetFramework: {netX.Y o net48}
- ORM: {EF Core 10 / Dapper / EF 6}
- Schema SQL: {lic / ewp / dbo / ...}
- Auth: {Azure AD / Integrated / mTLS}
- Pins NuGet criticos: {Azure.Core 1.50.0, Microsoft.Extensions.* 10.0.1, ...}
- Sistema operativo / agente CI: {Windows 11 / BUILDERS pool}
- Version del ecosistema STIC.IA: {leer de Publicacion/VERSION.json}

## Output del fallo

```
{pegar aqui los primeros 30-50 lineas del log de error: dotnet build,
 claude --debug, o el output del smoke script}
```

## Causa hipotetica (opcional)

{Si tienes una hipotesis sobre por que el constructor no captura este caso.
 Ejemplo: "El eval-set.json de generador-crud no tiene ningun query negativo
 que mencione .NET 4.8 ni Dapper, asi que el motor no aprende a evitarlo".}

## Cierre del ciclo (lo aplica el mantenedor del constructor)

- [ ] Anadido eval negativo / rama condicional al skill `{skill}` que cubra este caso
- [ ] `eval-set.json` actualizado y trigger eval re-ejecutado
- [ ] `test-cases.json` con assertion del caso especifico (si aplica)
- [ ] Skill description ajustada con USE FOR / DO NOT USE FOR si la fix es de description
- [ ] Pipeline `validate-skills.yml` en verde tras el cambio
- [ ] Commit referencia este Work Item: `fix(skill): {desc} #{wi-id}`
- [ ] Smoke test del consumidor re-ejecutado y en verde
- [ ] Work Item movido a `Done`
```

---

## WIQL para listar todos los gaps abiertos

Guardar como saved query en `ClaudeCodeSTIC/_apis/wit/queries/Calidad`:

```sql
SELECT
  [System.Id],
  [System.Title],
  [System.Tags],
  [System.AssignedTo],
  [System.State],
  [Microsoft.VSTS.Common.Priority]
FROM WorkItems
WHERE
  [System.TeamProject] = 'ClaudeCodeSTIC'
  AND [System.Tags] CONTAINS 'eval-gap'
  AND [System.State] IN ('To Do', 'Doing', 'New', 'Approved', 'Committed')
ORDER BY [Microsoft.VSTS.Common.Priority] ASC, [System.ChangedDate] DESC
```

## WIQL para metricas de feedback loop (mensual)

```sql
SELECT
  [System.Id], [System.Title], [System.Tags],
  [System.CreatedDate], [System.ClosedDate],
  [Microsoft.VSTS.Common.Priority]
FROM WorkItems
WHERE
  [System.TeamProject] = 'ClaudeCodeSTIC'
  AND [System.Tags] CONTAINS 'eval-gap'
  AND [System.ClosedDate] >= @StartOfMonth('-1')
  AND [System.State] = 'Done'
ORDER BY [System.ClosedDate] DESC
```

---

## Crear el Work Item via REST (opcional)

Si prefieres no usar la UI, este curl crea el item desde linea de comandos
contra `tfs.comillas.edu` (autenticacion NTLM Negotiate):

```bash
URL="https://tfs.comillas.edu/DefaultCollection/ClaudeCodeSTIC"

# Reemplaza WORK_ITEM_TYPE segun el proceso del proyecto:
#   - Basic:    "Issue"
#   - Scrum:    "Product%20Backlog%20Item"
#   - Agile:    "User%20Story"

curl -s --negotiate -u : \
  "${URL}/_apis/wit/workitems/\$Issue?api-version=6.0" \
  -X POST \
  -H "Content-Type: application/json-patch+json" \
  -d '[
    { "op": "add", "path": "/fields/System.Title",
      "value": "[Eval gap] generador-crud: introduce EF Core en proyecto net48" },
    { "op": "add", "path": "/fields/System.Tags",
      "value": "eval-gap; smoke-02; generador-crud" },
    { "op": "add", "path": "/fields/System.AreaPath",
      "value": "ClaudeCodeSTIC/Calidad" },
    { "op": "add", "path": "/fields/Microsoft.VSTS.Common.Priority", "value": 2 },
    { "op": "add", "path": "/fields/System.Description",
      "value": "<aqui va el markdown de la plantilla, escapado>" }
  ]'
```

> **Nota TFS on-premises**: usar `api-version=6.0` (no 7.0). El campo
> `System.State` se omite a proposito — TFS asigna el estado inicial del proceso
> (`To Do` en Basic, `New` en Scrum) automaticamente.

---

## SLA y prioridad (politica del constructor)

| Prioridad | Criterio | Tiempo de respuesta |
|---|---|---|
| 1 | Bloquea release de un consumidor critico (GestorLicencias, EuPeace, iMat2) | **2 dias laborables** para diagnostico + fix |
| 2 | Default. Smoke falla reproduciblemente, sin bloqueo inmediato | 1 sprint del constructor |
| 3 | Smoke falla esporadicamente (flaky) o es cosmetico | Backlog, sin SLA |

El contador empieza en la fecha de **creacion del Work Item**, no en la fecha
del fallo del smoke.

---

## Anti-patrones

- **NO abrir** un eval-gap si el smoke nunca paso en este proyecto (es bug del
  consumidor, no del constructor).
- **NO cerrar** un eval-gap solo porque el smoke pasa ahora — debe quedar
  evidencia (eval negativo, test-case, doc) de que el caso esta cubierto.
- **NO duplicar**: antes de abrir, ejecutar la WIQL de arriba y comprobar si
  ya existe uno con el mismo skill + smoke + sintoma.

---

## Referencia rapida

- Smoke tests: `.claude/stic-smoke-tests/{01,02,03}-*.md`
- Comando drafter (si esta disponible): `/eval-gap`
- Politica completa: ver ADR-029 en `_estado/DECISIONES.md` del constructor
