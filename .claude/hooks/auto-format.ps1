# auto-format.ps1 - Auto-formato C# tras escritura
# Evento: PostToolUse[Write|Edit]
# Exit 0 = siempre (no bloquea, solo formatea)
# Version: 1.0.0 (STIC.IA v3.8.0)
# Referencia: .NET Claude Kit post-edit-format.sh

param()
. "$PSScriptRoot/hook-helpers.ps1"

# Obtener archivo editado via hook-helpers (maneja stdin JSON + fallback env vars)
$editedFile = Get-FilePath (Get-HookInput)

# Solo procesar archivos .cs
if (-not $editedFile) { exit 0 }
if ($editedFile -notmatch '\.cs$') { exit 0 }
if (-not (Test-Path $editedFile -ErrorAction SilentlyContinue)) { exit 0 }

# Buscar .csproj o .sln subiendo directorios
$dir = Split-Path $editedFile -Parent
$projectFile = $null
$searchDir = $dir

for ($i = 0; $i -lt 10; $i++) {
    if (-not $searchDir -or $searchDir -eq (Split-Path $searchDir -Parent)) { break }

    $csproj = Get-ChildItem $searchDir -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($csproj) {
        $projectFile = $csproj.FullName
        break
    }

    $sln = Get-ChildItem $searchDir -Filter "*.sln" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($sln) {
        $projectFile = $sln.FullName
        break
    }

    $slnx = Get-ChildItem $searchDir -Filter "*.slnx" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($slnx) {
        $projectFile = $slnx.FullName
        break
    }

    $searchDir = Split-Path $searchDir -Parent
}

if (-not $projectFile) { exit 0 }

# Ejecutar dotnet format scoped al archivo
try {
    $relativePath = [System.IO.Path]::GetRelativePath((Split-Path $projectFile -Parent), $editedFile)
    & dotnet format $projectFile --include $relativePath --verbosity quiet 2>$null
} catch {
    # Silencioso - no bloquear por error de formato
}

exit 0
