#Requires -Version 5.1
<#
.SYNOPSIS
    STIC.IA smoke test: compilacion tras invocar una skill.

.DESCRIPTION
    Flujo:
      1. Captura baseline SHA (git rev-parse HEAD).
      2. Llama a Claude Code con el prompt indicado (skill genera codigo).
      3. Ejecuta build (dotnet build o msbuild, autodetectado).
      4. Evalua resultado (exit 0 y, con -Strict, sin warnings nuevos).
      5. Rollback al baseline (git reset --hard), salvo -KeepChanges.

    Exit codes:
      0   PASS (build OK)
      1   FAIL (build con errores o entorno invalido)
      2   Rollback fallo (working tree modificado, intervencion manual)

.PARAMETER Prompt
    Prompt a pasar a Claude Code. Por defecto, genera una entidad CRUD smoke.

.PARAMETER BuildCommand
    Comando de build. Autodetecta si se omite: dotnet build si hay .csproj,
    msbuild si solo hay .sln legacy.

.PARAMETER KeepChanges
    No hace rollback al final. Util para diagnosticar errores de compilacion.

.PARAMETER Strict
    Falla tambien con warnings nuevos, no solo con errores.

.PARAMETER DryRun
    Valida pre-requisitos y detecta build command sin llamar a Claude.

.EXAMPLE
    powershell -File .\.claude\stic-smoke-tests\scripts\smoke-compile.ps1

.EXAMPLE
    powershell -File .\scripts\smoke-compile.ps1 -Prompt "Genera un endpoint GET /api/smoke" -Strict
#>

[CmdletBinding()]
param(
    [string]$Prompt = 'Genera el CRUD de una entidad smoke llamada _SmokeTestEntity con Id(int) y Nombre(nvarchar 50). No anadas autenticacion.',
    [string]$BuildCommand,
    [switch]$KeepChanges,
    [switch]$Strict,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

function Write-Step { param([string]$Msg) Write-Host "==> $Msg" -ForegroundColor Cyan }
function Write-OK   { param([string]$Msg) Write-Host "    [OK] $Msg" -ForegroundColor Green }
function Write-Fail { param([string]$Msg) Write-Host "    [FAIL] $Msg" -ForegroundColor Red }

# ---------------------------------------------------------------------------
# Pre-flight checks
# ---------------------------------------------------------------------------

Write-Step "STIC.IA smoke-compile"

# Git must be present
try {
    $null = & git --version 2>&1
    if ($LASTEXITCODE -ne 0) { throw "git not found" }
} catch {
    Write-Fail "git no esta disponible en PATH"
    exit 1
}

# Repo must be a git repo
try {
    $null = & git rev-parse --is-inside-work-tree 2>&1
    if ($LASTEXITCODE -ne 0) { throw "not a git repo" }
} catch {
    Write-Fail "No es un repositorio git"
    exit 1
}

# Working tree must be clean
$status = & git status --porcelain
if ($status) {
    Write-Fail "Working tree NO esta limpio. Commit o stash antes de correr el smoke."
    Write-Host $status
    exit 1
}
Write-OK "Working tree limpio"

# Autodetect build command
if (-not $BuildCommand) {
    $hasCsproj = Get-ChildItem -Path . -Filter *.csproj -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    $hasSln    = Get-ChildItem -Path . -Filter *.sln -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($hasCsproj) {
        # dotnet disponible?
        $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
        if ($dotnet) {
            $BuildCommand = 'dotnet build --verbosity minimal --nologo'
        } elseif ($hasSln) {
            $BuildCommand = 'msbuild /verbosity:minimal /nologo'
        }
    }
}
if (-not $BuildCommand) {
    Write-Fail "No pude detectar build command (no hay .csproj/.sln o falta dotnet/msbuild)"
    exit 1
}
Write-OK "Build command: $BuildCommand"

# claude CLI present?
$claude = Get-Command claude -ErrorAction SilentlyContinue
if (-not $claude) {
    Write-Fail "Claude Code CLI no esta en PATH. Instalar y autenticar primero."
    exit 1
}
Write-OK "Claude CLI: $($claude.Source)"

if ($DryRun) {
    Write-Host ""
    Write-Step "DRY-RUN completado - pre-requisitos OK. No se llama a Claude."
    exit 0
}

# ---------------------------------------------------------------------------
# 1. Capture baseline
# ---------------------------------------------------------------------------

Write-Step "1/4 Capturando baseline"
$baseline = (& git rev-parse HEAD).Trim()
Write-OK "Baseline SHA: $baseline"

# ---------------------------------------------------------------------------
# 2. Invoke Claude (skill fires from description)
# ---------------------------------------------------------------------------

Write-Step "2/4 Invocando Claude con prompt"
Write-Host "    Prompt: $Prompt"
try {
    $null = & claude -p "$Prompt" 2>&1
} catch {
    Write-Fail "Claude -p fallo: $_"
    if (-not $KeepChanges) { & git reset --hard $baseline | Out-Null }
    exit 1
}
Write-OK "Claude finalizo la generacion"

# Si Claude no produjo cambios, el smoke no valida nada util
$changes = & git status --porcelain
if (-not $changes) {
    Write-Fail "Claude no genero cambios (skill no se activo o fallo en silencio)"
    exit 1
}
$changeCount = ($changes -split "`n").Count
Write-OK "Cambios detectados: $changeCount archivo(s)"

# ---------------------------------------------------------------------------
# 3. Build
# ---------------------------------------------------------------------------

Write-Step "3/4 Ejecutando build ($BuildCommand)"
$buildOut = Invoke-Expression "$BuildCommand 2>&1"
$buildExit = $LASTEXITCODE

$errorCount = ($buildOut | Select-String -Pattern '\b(error|Error)\s+[A-Z]+[0-9]+:' -AllMatches).Matches.Count
$warningCount = ($buildOut | Select-String -Pattern '\b(warning|Warning)\s+[A-Z]+[0-9]+:' -AllMatches).Matches.Count

Write-Host "    Exit:     $buildExit"
Write-Host "    Errores:  $errorCount"
Write-Host "    Warnings: $warningCount"

# ---------------------------------------------------------------------------
# 4. Evaluate and rollback
# ---------------------------------------------------------------------------

Write-Step "4/4 Evaluando resultado"
$pass = ($buildExit -eq 0) -and ($errorCount -eq 0)
if ($Strict -and $warningCount -gt 0) {
    $pass = $false
    Write-Fail "Strict mode: $warningCount warnings encontrados"
}

if (-not $KeepChanges) {
    try {
        & git reset --hard $baseline | Out-Null
        $afterReset = & git status --porcelain
        if ($afterReset) {
            Write-Fail "Rollback incompleto - working tree sigue modificado"
            exit 2
        }
        Write-OK "Rollback al baseline $baseline correcto"
    } catch {
        Write-Fail "Rollback fallo: $_"
        exit 2
    }
} else {
    Write-Host "    -KeepChanges activo: NO se hace rollback"
}

if ($pass) {
    Write-Host ""
    Write-Host "======================================================" -ForegroundColor Green
    Write-Host "  SMOKE COMPILE: PASS" -ForegroundColor Green
    Write-Host "======================================================" -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "======================================================" -ForegroundColor Red
    Write-Host "  SMOKE COMPILE: FAIL" -ForegroundColor Red
    Write-Host "======================================================" -ForegroundColor Red
    if ($errorCount -gt 0) {
        Write-Host ""
        Write-Host "Primeros 10 errores de build:"
        $buildOut | Select-String -Pattern '\b(error|Error)\s+[A-Z]+[0-9]+:' | Select-Object -First 10 | ForEach-Object { Write-Host "  $_" }
    }
    exit 1
}
