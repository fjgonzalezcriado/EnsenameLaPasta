#Requires -Version 5.1
<#
.SYNOPSIS
    Genera el baseline de archivos SQL legacy que incumplen la nomenclatura Comillas.

.DESCRIPTION
    Escanea los .sql del proyecto y detecta los que ya violaban la nomenclatura
    (prefijos usp_/sp_/pr_, sufijos _Listar/_Guardar/..., vistas/funciones sin prefijo,
    features post-2017). Escribe sus rutas relativas en .claude/sql-legacy-baseline.txt.

    El hook sql-nomenclatura-guard.ps1 omitira esos archivos al editarlos, pero
    SEGUIRA bloqueando archivos nuevos o renombrados que no esten en el baseline.

.PARAMETER ProjectDir
    Directorio raiz del proyecto. Por defecto, el directorio actual.

.PARAMETER OutputFile
    Ruta del baseline. Por defecto, .claude/sql-legacy-baseline.txt.

.PARAMETER DryRun
    Solo muestra que archivos se anadirian, sin escribir el archivo.

.EXAMPLE
    pwsh .claude/hooks/sql-baseline-generate.ps1

.EXAMPLE
    pwsh .claude/hooks/sql-baseline-generate.ps1 -DryRun
#>
[CmdletBinding()]
param(
    [string]$ProjectDir = (Get-Location).Path,
    [string]$OutputFile = "",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

if (-not $OutputFile) {
    $OutputFile = Join-Path $ProjectDir ".claude/sql-legacy-baseline.txt"
}

Write-Host "[SQL Baseline] Escaneando $ProjectDir..." -ForegroundColor Cyan

$sqlFiles = Get-ChildItem -Path $ProjectDir -Recurse -Filter "*.sql" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules|\.git|packages)\\' }

Write-Host "[SQL Baseline] $($sqlFiles.Count) archivos .sql encontrados" -ForegroundColor Gray

$violations = New-Object System.Collections.Generic.List[string]

foreach ($file in $sqlFiles) {
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    if (-not $content) { continue }

    # Saltar archivos ya marcados como legacy inline
    if ($content -match '(?m)^\s*--\s*LEGACY:\s*no\s+renombrar') { continue }
    if ($file.FullName -match 'Legacy[/\\]|Obsoleto[/\\]|MigracionPendiente[/\\]') { continue }

    $hasViolation = $false

    # 1. CREATE PROCEDURE con prefijo prohibido
    if ($content -match '(?im)CREATE\s+(?:OR\s+ALTER\s+)?PROC(?:EDURE)?\s+\[?(?:\w+\]?\.\[?)?\[?(usp|sp|pr|proc|pa)_\w+\]?') {
        $hasViolation = $true
    }

    # 2. CREATE PROCEDURE con sufijo prohibido
    if ($content -match '(?im)CREATE\s+(?:OR\s+ALTER\s+)?PROC(?:EDURE)?\s+[^\r\n]*_(Listar|Guardar|Eliminar|Leer|Actualizar|Insertar|L|G|E|A)\b') {
        $hasViolation = $true
    }

    # 3. Funcion escalar sin prefijo fn_
    if ($content -match '(?im)CREATE\s+(?:OR\s+ALTER\s+)?FUNCTION\s+\[?\w+\]?\.\[?(?!fn_|fnt_)\w+\]?\s*\([^)]*\)\s*RETURNS\s+(?!TABLE)') {
        $hasViolation = $true
    }

    # 4. TVF sin prefijo fnt_
    if ($content -match '(?im)CREATE\s+(?:OR\s+ALTER\s+)?FUNCTION\s+\[?\w+\]?\.\[?fn_\w+\]?\s*\([^)]*\)\s*RETURNS\s+TABLE') {
        $hasViolation = $true
    }

    # 5. Vista sin prefijo vw_
    if ($content -match '(?im)CREATE\s+(?:OR\s+ALTER\s+)?VIEW\s+\[?\w+\]?\.\[?(?!vw_)\w+\]?') {
        $hasViolation = $true
    }

    # 6. Trigger sin prefijo tr_
    if ($content -match '(?im)CREATE\s+(?:OR\s+ALTER\s+)?TRIGGER\s+\[?(?:\w+\]?\.\[?)?\[?(?!tr_)\w+\]?') {
        $hasViolation = $true
    }

    # 7. SP sin schema (solo dbo por defecto)
    if ($content -match '(?im)CREATE\s+(?:OR\s+ALTER\s+)?PROC(?:EDURE)?\s+\[?[A-Za-z]\w*\]?\s*(\(|AS|\r|\n)') {
        # Matches "CREATE PROC Nombre" sin schema
        $hasViolation = $true
    }

    if ($hasViolation) {
        $relativePath = $file.FullName -replace [regex]::Escape($ProjectDir + "\"), "" -replace "\\", "/"
        $violations.Add($relativePath) | Out-Null
    }
}

Write-Host ""
Write-Host "[SQL Baseline] $($violations.Count) archivos legacy detectados" -ForegroundColor Yellow

if ($violations.Count -eq 0) {
    Write-Host "[SQL Baseline] No se detectaron violaciones. No se creara baseline." -ForegroundColor Green
    exit 0
}

# Ordenar para estabilidad del archivo
$sorted = $violations | Sort-Object

if ($DryRun) {
    Write-Host ""
    Write-Host "[SQL Baseline] DRY RUN - archivos que se anadirian:" -ForegroundColor Cyan
    foreach ($v in $sorted) { Write-Host "  $v" -ForegroundColor Gray }
    Write-Host ""
    Write-Host "[SQL Baseline] Ejecuta sin -DryRun para escribir $OutputFile" -ForegroundColor Cyan
    exit 0
}

# Crear directorio padre si no existe
$parent = Split-Path $OutputFile -Parent
if (-not (Test-Path $parent)) {
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
}

# Escribir baseline (UTF-8 sin BOM)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines($OutputFile, $sorted, $utf8NoBom)

Write-Host ""
Write-Host "[SQL Baseline] Baseline creado: $OutputFile" -ForegroundColor Green
Write-Host "[SQL Baseline] $($sorted.Count) archivos listados" -ForegroundColor Green
Write-Host ""
Write-Host "Proximos pasos:" -ForegroundColor Cyan
Write-Host "  1. Revisar $OutputFile y quitar archivos que SI deberian respetar la nomenclatura" -ForegroundColor Gray
Write-Host "  2. Commit del baseline al repo" -ForegroundColor Gray
Write-Host "  3. El hook sql-nomenclatura-guard.ps1 ya omitira esos archivos al editarlos" -ForegroundColor Gray
