# Smoke 02 · Compilacion

> **El test mas valioso de los tres.** Verifica que el codigo generado por una
> skill compila en el stack real de este proyecto. Es el que mas incidentes
> reales captura (incompatibilidades `.NET 4.8` vs `.NET 10`, ORM equivocado,
> NuGets incompatibles, etc.).

---

## Pre-requisitos

- El proyecto compila en el estado actual (`dotnet build` u `msbuild` OK).
- Working tree **limpio** (`git status` sin cambios pendientes).
- Claude Code instalado y autenticado.

---

## Flujo de prueba

```
1. Capturar baseline SHA         -> git rev-parse HEAD
2. Invocar skill de generacion   -> claude -p "<prompt>"
3. Build                         -> dotnet build  /  msbuild
4. Evaluar resultado
   - Exit 0 y sin warnings nuevos -> PASS
   - Errores de compilacion       -> FAIL (revisar skill)
5. Rollback                      -> git reset --hard <baseline>
```

El rollback es parte del test — no queremos dejar `_SmokeTestEntity` en el
repo permanentemente.

---

## Casos de prueba recomendados

### Caso 2.1 · CRUD generado compila

**Prompt para Claude**:

> Genera el CRUD completo para la entidad `_SmokeTestEntity` con Id(int) y
> Nombre(nvarchar 50). Usa {ORM} y el schema `{SCHEMA}`.

**Sustituir**:

- `{ORM}` = `Dapper` para .NET 4.8 / `EF Core 10` para .NET 10.
- `{SCHEMA}` = `lic`, `ewp`, `dbo`, segun proyecto.

**Esperado**: `dotnet build` OK, cero errores, cero warnings nuevos.

**Regresiones historicas que este test captura**:

- Skill introduce `using Microsoft.EntityFrameworkCore;` en proyecto Dapper.
- Skill usa sintaxis C# 12 (`required`, primary constructors) en `net48`.
- Skill genera `Task<T>` en un proyecto `net48` sin `System.Threading.Tasks.Extensions`.
- Skill asume `dotnet` disponible cuando el proyecto usa `msbuild` legacy.

---

### Caso 2.2 · API endpoint compila

**Prompt**:

> Genera un endpoint Minimal API para `GET /api/{resource}/healthsmoke` que
> devuelva `{status:"ok", version}`. Sin autenticacion (smoke).

**Esperado**: build OK. El endpoint se puede descartar con el rollback.

---

### Caso 2.3 · Test unitario compila

**Prompt**:

> Genera un test xUnit para `_SmokeTestEntity.EsValida()` que cubra los casos
> positivo y negativo con FluentAssertions y Moq.

**Esperado**: `dotnet test --no-run` (o `dotnet build` del proyecto de tests)
OK. NO ejecutar los tests en este smoke — el objetivo es verificar que
compilan, no que pasan (los tests de entidades nuevas fallaran por logica
inventada).

---

## Script de ejecucion · `scripts/smoke-compile.ps1`

```powershell
# Se ejecuta desde la raiz del proyecto consumidor
powershell -File .\.claude\stic-smoke-tests\scripts\smoke-compile.ps1

# Opciones:
#   -Prompt "<prompt>"    Prompt custom en lugar del default
#   -BuildCommand "..."   msbuild o dotnet build (autodetectado)
#   -KeepChanges          No hacer rollback (diagnostico)
#   -Strict               Falla tambien con warnings nuevos
#   -DryRun               Valida pre-requisitos sin llamar a Claude
```

El script realiza los 5 pasos automaticamente y emite exit 0 (PASS) o 1 (FAIL).

---

## Integracion con CI (nightly)

Para incluir este smoke en el pipeline nightly del proyecto consumidor:

```yaml
# azure-pipelines.yml del proyecto
schedules:
  - cron: '0 5 * * *'
    displayName: 'Nightly smoke compile'
    branches: { include: [main] }

jobs:
  - job: SmokeCompile
    steps:
      - task: PowerShell@2
        displayName: 'STIC.IA smoke compile'
        inputs:
          filePath: '.claude/stic-smoke-tests/scripts/smoke-compile.ps1'
          arguments: '-Strict'
          failOnStderr: false
```

Si falla, abrir Work Item «Eval gap» en el constructor con:

- Prompt exacto usado.
- Error de compilacion (output de `dotnet build`).
- Stack: `net{X.Y}`, `{ORM}`, `{SCHEMA}`, pins NuGet relevantes.

---

## Criterios de aceptacion

- **PASS**: los 3 casos (2.1, 2.2, 2.3) compilan sin errores y rollback limpio.
- **WARN**: 2 de 3 compilan → investigar, no bloquear release.
- **FAIL**: ≤1 compila → abrir Work Item inmediato y revertir a la version anterior del ecosistema.

---

## Troubleshooting rapido

| Sintoma | Accion |
|---|---|
| `The type or namespace 'IRequest' could not be found` | Skill esta usando MediatR en un proyecto sin MediatR. Confirmar que el proyecto usa `Mediator` (gratuito) o tiene MediatR instalado |
| `The name 'required' does not exist in the current context` | Sintaxis C# 11+ en proyecto `net48`. La skill no esta respetando la TFM |
| `Type or namespace 'EntityFrameworkCore' does not exist` | Skill metiendo EF en proyecto Dapper. Bug de la skill para este stack |
| Build lento (>5 min) | Probablemente restore NuGets por primera vez. Precalentar con `dotnet restore` antes del smoke |
