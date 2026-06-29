# Smoke 03 · Convenciones locales del proyecto

> Verifica que el codigo generado por skills **respeta las particularidades de
> este proyecto** aunque compile. Una skill puede generar codigo valido en
> general pero que rompa la nomenclatura Comillas, use el schema equivocado,
> introduzca dependencias que violen pins de NuGet o mezcle estilos.

---

## Pre-requisitos

- Tests 01-activation y 02-compilation en **PASS**.
- Working tree limpio (`git status`).
- `ESTADO_PROYECTO.json` con la configuracion del proyecto actualizada.

---

## Checklist generico (todos los proyectos Comillas)

Ejecutar tras generar codigo con alguna skill. Cada `[ ]` es un check manual o
con `grep`:

- [ ] Namespace respeta el patron `Comillas.<Area>.<Proyecto>.<Capa>`.
- [ ] No se introducen `using System.Threading.Tasks.Extensions` innecesarios.
- [ ] Ningun `catch (Exception ex) { }` vacio (siempre loggear o re-lanzar).
- [ ] Ningun hardcoded secret (`password`, `api_key`, `connection string` con credenciales).
- [ ] No se usa `Console.WriteLine` en codigo de produccion.
- [ ] Campos privados con prefijo `_camelCase`, propiedades `PascalCase`.
- [ ] Metodos `async` tienen sufijo `Async`.
- [ ] Tests siguen `<Metodo>_<Condicion>_<ResultadoEsperado>`.

---

## Checklist por perfil de proyecto

Selecciona el bloque que aplica a este proyecto y recorre sus checks. Si el
proyecto es mixto, aplica varios.

### Perfil A · `.NET 4.8` + Dapper + schema propio (GestorLicencias, ErpSync)

- [ ] `TargetFramework` sigue siendo `net48` tras la generacion (no `net10.0`).
- [ ] Ningun `using Microsoft.EntityFrameworkCore;` introducido.
- [ ] SPs generados usan el schema correcto (`lic.`, `ewp.`, no `dbo.` por defecto).
- [ ] Los nombres de SPs siguen el patron Comillas **sin prefijo** `usp_`/`sp_`/`pr_`.
- [ ] `SET NOCOUNT ON;` como primera instruccion en cada SP.
- [ ] Parametros tipados (no `sql_variant`), sin `SELECT *`.
- [ ] Funciones SI llevan prefijo `fn_` / `fnt_`, vistas `vw_`.
- [ ] No se introducen NuGets v10+ incompatibles (`Azure.Core > 1.50`,
      `Microsoft.Extensions.* > 10.0.x`).

### Perfil B · `.NET 10` + EF Core + Clean Architecture (Comillas.EuPeace, proyectos nuevos)

- [ ] `TargetFramework` = `net10.0`.
- [ ] Entidades en `Domain/`, servicios en `Application/`, repos en `Infrastructure/`.
- [ ] DbContext usa `Set<T>()` (no campos `DbSet` con autoproperties simples).
- [ ] Consultas de lectura con `AsNoTracking()`.
- [ ] Valida con FluentValidation, no con DataAnnotations solas.
- [ ] Minimal API o Controller segun convencion del proyecto (no mezclar ambas).
- [ ] Nullable reference types activado en proyectos nuevos.

### Perfil C · Integraciones externas (EWP, Banner, Oracle HCM, Sigma)

- [ ] El endpoint integrado mantiene compatibilidad con la version API declarada.
- [ ] Credenciales desde Azure Key Vault, nunca en `appsettings`.
- [ ] `HttpClient` via `IHttpClientFactory` con politica Polly.
- [ ] Retry con backoff, no blind retries.
- [ ] Logs estructurados (Serilog) con el EndpointId + StatusCode.
- [ ] Compatible con Factsheet API v1.2.0 (caso EuPeace).
- [ ] Compatible con `FOR JSON PATH` (caso ErpSync).
- [ ] Manifest XML no se corrompe al editar endpoints (caso EuPeace).

### Perfil D · Proyectos con Azure DevOps (tfs.comillas.edu)

- [ ] `api-version=6.0` en llamadas REST (no `7.0`, que es solo cloud).
- [ ] Autenticacion NTLM `--negotiate -u :` (no PAT ni OAuth).
- [ ] Estados de work items correctos segun proceso (Scrum: `Committed`,
      Basic: `Doing`, Agile: `Active`). **Nunca `In Progress`**.
- [ ] Widgets Markdown: `settings` es markdown crudo, NO `{"content":"..."}`.
- [ ] `contributionId` correcto:
      `ms.vss-dashboards-web.Microsoft.VisualStudioOnline.Dashboards.MarkdownWidget`.
- [ ] Team tiene `includeChildren: true` tras crear sub-areas (fix Fase 3f.1).

---

## Verificacion automatizada (grep post-generacion)

Guardar el siguiente script en `scripts/check-conventions.ps1` y ejecutarlo
tras generar codigo con una skill:

```powershell
#Requires -Version 5.1
# Corre grep sobre el diff y reporta violaciones. Exit 1 si encuentra alguna.

param(
    [string]$Profile = 'A',     # A=.NET4.8 Dapper, B=.NET10 EF, C=Integraciones, D=AzDO
    [string]$BaselineSha = 'HEAD~1'
)

$violations = @()
$changed = git diff --name-only $BaselineSha HEAD

foreach ($f in $changed) {
    if (-not (Test-Path $f)) { continue }

    $content = Get-Content $f -Raw -ErrorAction SilentlyContinue
    if (-not $content) { continue }

    # Checks universales
    if ($content -match 'catch\s*\(\s*Exception\s+\w+\s*\)\s*\{\s*\}') {
        $violations += "  [UNIV] catch vacio en $f"
    }
    if ($content -match 'Console\.WriteLine') {
        $violations += "  [UNIV] Console.WriteLine en $f"
    }
    if ($content -match '(password|apiKey|api_key)\s*=\s*["''][^"''$\{]+["'']') {
        $violations += "  [UNIV] posible secret hardcoded en $f"
    }

    # Checks por perfil
    switch ($Profile) {
        'A' {
            if ($content -match 'using Microsoft\.EntityFrameworkCore') {
                $violations += "  [A] EF Core en proyecto Dapper: $f"
            }
            if ($content -match 'CREATE PROCEDURE\s+(usp_|sp_|pr_)') {
                $violations += "  [A] SP con prefijo usp_/sp_/pr_ en $f"
            }
            if ($content -match 'SELECT\s+\*' -and $f -match '\.sql$') {
                $violations += "  [A] SELECT * en $f"
            }
        }
        'B' {
            if ($content -match '<TargetFramework>net48</TargetFramework>' -and $f -match '\.csproj$') {
                $violations += "  [B] Proyecto nuevo con TFM net48: $f"
            }
        }
        'D' {
            if ($content -match 'api-version=7\.') {
                $violations += "  [D] api-version 7.x en TFS on-premises: $f"
            }
            if ($content -match '"In Progress"') {
                $violations += "  [D] Estado 'In Progress' en work item: $f"
            }
        }
    }
}

if ($violations.Count -eq 0) {
    Write-Host "Conventions smoke: PASS"
    exit 0
} else {
    Write-Host "Conventions smoke: FAIL"
    $violations | ForEach-Object { Write-Host $_ }
    exit 1
}
```

---

## Criterios de aceptacion

- **PASS**: cero violaciones del checklist aplicable al perfil.
- **WARN**: 1-2 violaciones menores (estilo) → corregir a mano, seguir.
- **FAIL**: violacion critica (EF en Dapper, `In Progress`, secret hardcoded) →
  revertir cambios y abrir Work Item «Eval gap».

---

## Mantener este checklist vivo

Cuando aparezca una nueva convencion en este proyecto (ej. nueva integracion,
nuevo ORM), **anadela aqui** en el bloque de perfil correspondiente. El smoke
es especifico de este proyecto; el constructor no sabe cuales son vuestras
particularidades hasta que las documentais.
