# STIC.IA · Smoke Tests del proyecto consumidor

> Tres pruebas ligeras para verificar que el **ecosistema STIC.IA** funciona
> correctamente en **este proyecto** tras cada actualizacion (`arranque.ps1 update`).

---

## Proposito

Los *evals* del constructor (`ClaudeCodeSTIC`) validan las skills **en aislamiento**.
Los *smoke tests* de este directorio validan que funcionan **en tu stack real**:

- ¿Claude Code invoca la skill correcta ante prompts tipicos de este proyecto?
- ¿El codigo generado compila con tu version de .NET y tus dependencias?
- ¿Se respetan las convenciones locales (schema SQL, pins NuGet, auth, etc.)?

Si el constructor esta en verde pero aqui falla algun smoke, significa que la
skill asume algo que no aplica a tu stack. En ese caso, **abrir Work Item
«Eval gap»** en `ClaudeCodeSTIC` (ver seccion «Feedback loop»).

---

## Las tres suites

| Archivo | Que valida | Cuando ejecutar | Coste |
|---|---|---|---|
| `01-activation.md` | Claude invoca la skill correcta | Tras cada update del ecosistema | 5 min |
| `02-compilation.md` | El codigo generado **compila** en tu stack | Tras cada update + en CI nightly | 10-15 min |
| `03-conventions.md` | Convenciones locales respetadas (schema, pins, ORM) | Tras cada update | 5 min |

**Minimo viable si tienes prisa**: ejecutar solo `02-compilation.md`. Ese test
detecta ~80 % de los problemas reales (ej. skill que usa `using Microsoft.EntityFrameworkCore`
en un proyecto .NET 4.8 con Dapper).

---

## Como usar

### 1. Personaliza los tests

Los tres archivos traen prompts **genericos**. Reemplaza los `{placeholders}`
por entidades y casos reales de este proyecto. Ejemplos por proyecto:

| Placeholder | GestorLicencias | Comillas.EuPeace | ErpSync |
|---|---|---|---|
| `{ENTITY}` | `SolicitudLicencia` | `Nominacion` | `EmpleadoHCM` |
| `{SCHEMA}` | `lic` | `ewp` | `dbo` |
| `{ORM}` | Dapper | EF Core 10 | Dapper |
| `{NET}` | net48 | net10.0 | net48 |
| `{AUTH}` | Azure AD | Azure AD + Factsheet API | Integrated |

### 2. Ejecuta una suite

Cada archivo `.md` tiene una seccion «Como ejecutarlo» con el comando concreto.
Los dos mas utiles:

```powershell
# Smoke de compilacion (el mas valioso)
powershell -File .\.claude\stic-smoke-tests\scripts\smoke-compile.ps1

# Smoke de activacion (valida que invoca la skill correcta)
claude --debug -p "Necesito un CRUD para {ENTITY}" 2>&1 | `
  Select-String "loading skill: generador-crud"
```

### 3. Integracion automatica (opcional)

Anadir un step al pipeline de CI del proyecto consumidor:

```yaml
# azure-pipelines.yml del proyecto consumidor
- task: PowerShell@2
  displayName: 'STIC.IA smoke compile'
  inputs:
    filePath: '.claude/stic-smoke-tests/scripts/smoke-compile.ps1'
    arguments: '-Strict'
  condition: succeededOrFailed()
```

---

## Cuando ejecutar cada suite

| Momento | Ejecutar |
|---|---|
| Tras `arranque.ps1 -InstallMode update` | Las tres |
| Tras cambiar el modelo (Sonnet -> Opus, etc.) | Solo `01-activation` |
| Onboarding de un dev nuevo al proyecto | Solo `01-activation` (como smoke de familiarizacion) |
| CI nightly del proyecto | Solo `02-compilation` |
| Antes de un release del proyecto | Las tres |

---

## Feedback loop al constructor (Fase 4)

Si un smoke test **falla reproduciblemente** en este proyecto pero los evals
del constructor `ClaudeCodeSTIC` estan en verde, significa una de dos cosas:

1. La skill asume algo que no aplica aqui (ej. .NET 10 vs .NET 4.8).
2. Hay una convencion local no documentada.

**Accion**: abrir un Work Item **«Eval gap»** en el proyecto `ClaudeCodeSTIC`
(tfs.comillas.edu). Hay tres formas:

### Opcion A · Comando guiado (recomendado)

```
/eval-gap --smoke 02 --skill generador-crud --from-log build.log
```

El comando recopila el contexto, busca duplicados con WIQL, muestra preview
y solo crea el Work Item con tu confirmacion. Ver `.claude/commands/eval-gap.md`
para todos los flags.

### Opcion B · Plantilla manual

Copiar la descripcion de `eval-gap-template.md` en este mismo directorio y
crear el Work Item desde la UI de tfs.comillas.edu. Incluye:

- Tag `eval-gap; smoke-{01|02|03}; {skill}`
- Area Path `ClaudeCodeSTIC/Calidad`
- Prioridad 1 (bloquea release) o 2 (default)

### Opcion C · curl REST

El template incluye el curl exacto contra `tfs.comillas.edu` con
autenticacion NTLM y `api-version=6.0`. Util si trabajas desde terminal sin
Claude Code.

### SLA del constructor

| Prioridad | Criterio | SLA |
|---|---|---|
| **P1** | Bloquea release de consumidor critico | 2 dias laborables |
| **P2** | Default. Smoke falla reproduciblemente, sin bloqueo | 1 sprint del constructor |
| **P3** | Flaky o cosmetico | Backlog sin SLA |

El ciclo se cierra cuando el smoke vuelve a pasar tras `arranque.ps1 update` —
**no** cuando el constructor mergea el fix. Esto evita falsos cierres.

---

## Referencias

- Plantilla del Work Item: `eval-gap-template.md` (en este mismo directorio)
- Comando: `/eval-gap` (`.claude/commands/eval-gap.md`)
- Politica completa + KPIs: `Documentacion/05_Analisis_Internos/POLITICA_FEEDBACK_LOOP_EVAL_GAP.html` (en el constructor)
- Decision arquitectonica: ADR-029 en `_estado/DECISIONES.md` del constructor
- Motor de evals del constructor: `ClaudeCodeSTIC/Scripts/run_eval.py`, `run_quality_eval.py`
- Pipeline CI del constructor: `ClaudeCodeSTIC/.azuredevops/pipelines/validate-skills.yml`
- Analisis estrategico: `ClaudeCodeSTIC/Documentacion/05_Analisis_Internos/ANALISIS_ESTRATEGIA_EVALS_SKILLS_STIC_IA.html`
