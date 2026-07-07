# dotnet-primary-ctor-guard.ps1 - Recuerda usar constructor primario (IDE0290)
# Evento: PreToolUse[Write|Edit]
# Exit 1 = AVISO informativo (NO bloquea el Write/Edit), Exit 0 = sin hallazgos
# Version: 1.0.0
#   Heuristica textual rapida (no ejecuta dotnet build/format): detecta clases cuyo
#   constructor SOLO asigna parametros a campos (patron IDE0290) y avisa para escribirlo
#   como constructor primario conservando los campos _field. WARN-first (mismo patron que
#   secret-scanner.ps1 / sql-nomenclatura-guard.ps1). Complementa la regla
#   .claude/rules/dotnet-code-style.md. Portatil: copiar a .claude/hooks/ + registrar en
#   settings.json (PreToolUse Write|Edit).

param()
. "$PSScriptRoot/hook-helpers.ps1"

$hookData = Get-HookInput
$toolInput = Get-ToolInput $hookData
if (-not $toolInput) { exit 0 }

# Solo archivos .cs (ni migraciones ni Designer autogenerados)
$filePath = Get-FilePath $hookData
if (-not $filePath) { exit 0 }
if ($filePath -notmatch '\.cs$') { exit 0 }
if ($filePath -match '[/\\]Migrations[/\\]|\.Designer\.cs$|\.g\.cs$|GlobalUsings') { exit 0 }

# Contenido nuevo (new_string en Edit, content en Write); des-escapar JSON.
$content = Get-NewContent $hookData
if (-not $content) { exit 0 }
$content = $content -replace '\\r\\n', "`n" -replace '\\n', "`n" -replace '\\"', '"' -replace '\\\\', '\'

$flagged = New-Object System.Collections.Generic.HashSet[string]

function Test-PureAssignmentBody {
    param([string]$Body)
    # true si el cuerpo son SOLO asignaciones "campo = param;" (sin logica, ifs, llamadas).
    $stmts = $Body -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
    if ($stmts.Count -eq 0) { return $false }
    foreach ($s in $stmts) {
        # quitar comentarios de linea
        $s = ($s -replace '//.*$', '').Trim()
        if ($s -eq '') { continue }
        if ($s -notmatch '^(this\.)?[A-Za-z_]\w*\s*=\s*[A-Za-z_]\w*$') { return $false }
    }
    return $true
}

function Test-NotAlreadyPrimary {
    param([string]$Name)
    # si ya hay "class <Name>(" es constructor primario -> no avisar
    return ($content -notmatch "class\s+$([regex]::Escape($Name))\s*\(")
}

function Test-SingleCtor {
    param([string]$Name)
    # IDE0290 solo aplica con UN constructor (sin overloads)
    $n = ([regex]::Matches($content, "\b(public|internal|protected|private)\s+$([regex]::Escape($Name))\s*\(")).Count
    return ($n -le 1)
}

# 1) Constructor multilinea con cuerpo de solo asignaciones (sin llaves anidadas).
$rxBlock = [regex]'(?ms)\b(?:public|internal|protected)\s+(?<name>[A-Za-z_]\w*)\s*\((?<params>[^)]+)\)\s*\{(?<body>[^{}]*)\}'
foreach ($m in $rxBlock.Matches($content)) {
    $name = $m.Groups['name'].Value
    if ($name -in @('if', 'for', 'while', 'switch', 'foreach', 'using', 'lock', 'catch')) { continue }
    if (Test-PureAssignmentBody $m.Groups['body'].Value -and (Test-NotAlreadyPrimary $name) -and (Test-SingleCtor $name)) {
        [void]$flagged.Add($name)
    }
}

# 2) Constructor con cuerpo de expresion: public Foo(Bar b) => _b = b;
$rxExpr = [regex]'(?m)\b(?:public|internal|protected)\s+(?<name>[A-Za-z_]\w*)\s*\((?<params>[^)]+)\)\s*=>\s*(?<asgn>(?:this\.)?[A-Za-z_]\w*\s*=\s*[A-Za-z_]\w*)\s*;'
foreach ($m in $rxExpr.Matches($content)) {
    $name = $m.Groups['name'].Value
    if (Test-NotAlreadyPrimary $name -and (Test-SingleCtor $name)) {
        [void]$flagged.Add($name)
    }
}

if ($flagged.Count -gt 0) {
    $names = ($flagged | Sort-Object) -join ', '
    Write-Host "AVISO (IDE0290): constructor que solo asigna campos en: $names"
    Write-Host "  Recomendado: constructor primario conservando los campos _field, p.ej."
    Write-Host "    public sealed class Foo(IBar bar) : IFoo { private readonly IBar _bar = bar; }"
    Write-Host "  Arreglo en bloque: dotnet format style <sln> --diagnostics IDE0290 --severity info"
    Write-Host "  (informativo, no bloquea; ver .claude/rules/dotnet-code-style.md)"
    exit 1
}

exit 0
