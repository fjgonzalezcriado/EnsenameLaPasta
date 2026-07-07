# dotnet-code-style-guard.ps1 - Recuerda reglas de estilo y rendimiento .NET (IDExxxx / CAxxxx)
# Evento: PreToolUse[Write|Edit]
# Exit 1 = AVISO informativo (NO bloquea el Write/Edit), Exit 0 = sin hallazgos
# Version: 2.1.0
#   v2.1.0: anadidas reglas de RENDIMIENTO (analizadores CA) detectables textualmente con bajo
#           falso positivo: CA1827 (.Count()==0/>0 -> Any()), CA1820 (== "" -> IsNullOrEmpty/Length),
#           CA1834 (StringBuilder.Append("x") de 1 char -> Append('x')), CA1848/CA1873 (logging con
#           interpolacion $"..." -> plantilla estructurada / IsEnabled). Las CA que requieren
#           analisis semantico (CA1859 tipo concreto, CA1861 array constante como arg, CA1822 miembro
#           estatico, CA1873 boxing en logging estructurado) las detecta 'dotnet format analyzers'.
#   v2.0.0: ampliado del solo-IDE0290 a un conjunto de reglas de estilo .NET detectables
#           textualmente y comprobadas en el proyecto: IDE0290 (constructor primario),
#           IDE0300 (expresion de coleccion en arrays), IDE0028 (expr. de coleccion en
#           inicializadores), IDE0063 (using simple), IDE0330 (System.Threading.Lock).
#           Las no fiables textualmente (IDE0042 desconstruccion, IDE0305 fluida) se dejan al
#           'dotnet format' que sugiere el mensaje. WARN-first (no bloquea). Complementa la
#           regla .claude/rules/dotnet-code-style.md. Portatil (copiar + registrar en settings).
#   v1.0.0: solo IDE0290 (dotnet-primary-ctor-guard.ps1).
# NOTA: solo ASCII en el codigo (Windows PowerShell malinterpreta flechas Unicode como comillas).

param()
. "$PSScriptRoot/hook-helpers.ps1"

$hookData = Get-HookInput
$toolInput = Get-ToolInput $hookData
if (-not $toolInput) { exit 0 }

# Solo archivos .cs (ni migraciones ni autogenerados)
$filePath = Get-FilePath $hookData
if (-not $filePath) { exit 0 }
if ($filePath -notmatch '\.cs$') { exit 0 }
if ($filePath -match '[/\\]Migrations[/\\]|\.Designer\.cs$|\.g\.cs$|GlobalUsings') { exit 0 }

# Contenido nuevo (new_string en Edit, content en Write); des-escapar JSON.
$content = Get-NewContent $hookData
if (-not $content) { exit 0 }
$content = $content -replace '\\r\\n', "`n" -replace '\\n', "`n" -replace '\\"', '"' -replace '\\\\', '\'

$findings = New-Object System.Collections.Generic.List[string]

# ── IDE0290: constructor que SOLO asigna campos -> constructor primario
function Test-PureAssignmentBody {
    param([string]$Body)
    $stmts = $Body -split ';' | ForEach-Object { ($_ -replace '//.*$', '').Trim() } | Where-Object { $_ -ne '' }
    if ($stmts.Count -eq 0) { return $false }
    foreach ($s in $stmts) {
        if ($s -notmatch '^(this\.)?[A-Za-z_]\w*\s*=\s*[A-Za-z_]\w*$') { return $false }
    }
    return $true
}
$ctorNames = New-Object System.Collections.Generic.HashSet[string]
$rxBlock = [regex]'(?ms)\b(?:public|internal|protected)\s+(?<name>[A-Za-z_]\w*)\s*\((?<params>[^)]+)\)\s*\{(?<body>[^{}]*)\}'
foreach ($m in $rxBlock.Matches($content)) {
    $name = $m.Groups['name'].Value
    if ($name -in @('if', 'for', 'while', 'switch', 'foreach', 'using', 'lock', 'catch', 'fixed')) { continue }
    $isPrimary = $content -match "class\s+$([regex]::Escape($name))\s*\("
    $ctorCount = ([regex]::Matches($content, "\b(public|internal|protected|private)\s+$([regex]::Escape($name))\s*\(")).Count
    if ((Test-PureAssignmentBody $m.Groups['body'].Value) -and -not $isPrimary -and $ctorCount -le 1) {
        [void]$ctorNames.Add($name)
    }
}
$rxExpr = [regex]'(?m)\b(?:public|internal|protected)\s+(?<name>[A-Za-z_]\w*)\s*\((?<params>[^)]+)\)\s*=>\s*(?:this\.)?[A-Za-z_]\w*\s*=\s*[A-Za-z_]\w*\s*;'
foreach ($m in $rxExpr.Matches($content)) {
    $name = $m.Groups['name'].Value
    $isPrimary = $content -match "class\s+$([regex]::Escape($name))\s*\("
    $ctorCount = ([regex]::Matches($content, "\b(public|internal|protected|private)\s+$([regex]::Escape($name))\s*\(")).Count
    if (-not $isPrimary -and $ctorCount -le 1) { [void]$ctorNames.Add($name) }
}
if ($ctorNames.Count -gt 0) {
    $findings.Add("IDE0290 (constructor primario): $(($ctorNames | Sort-Object) -join ', ') -- class Foo(Dep d) { private readonly Dep _d = d; }")
}

# ── IDE0300: expresion de coleccion en arrays (new[]{...} / new T[]{...} -> [...])
if ($content -match '\bnew\s*\[\s*\]\s*\{' -or $content -match '\bnew\s+[A-Za-z_][\w.]*\[\]\s*\{') {
    $findings.Add('IDE0300 (expresion de coleccion): new[] { ... } -- usar [ ... ]')
}

# ── IDE0028: expr. de coleccion en inicializadores de coleccion
if ($content -match '\bnew\s+(?:List|Dictionary|HashSet|Collection|ObservableCollection|IList|ICollection)\s*<[^>]*>\s*\(\s*\)\s*\{') {
    $findings.Add('IDE0028 (inicializador de coleccion): new List<T>() { ... } -- usar [ ... ]')
}

# ── IDE0063: using simple (using (var x = ...) { } -> using var x = ...;)
if ($content -match '(?m)\busing\s*\(\s*(?:var|[A-Za-z_][\w<>.\[\], ]*?)\s+\w+\s*=') {
    $findings.Add('IDE0063 (using simple): using (var x = ...) { } -- usar using var x = ...;')
}

# ── IDE0330: System.Threading.Lock en vez de object para lock
if ($content -match '\bprivate\s+(?:readonly\s+)?object\s+\w+\s*=\s*new\s*\(\s*\)\s*;' -and $content -match '\block\s*\(') {
    $findings.Add('IDE0330 (Lock): object _gate = new(); + lock() -- usar System.Threading.Lock')
}

# ── Rendimiento (analizadores CA) detectables textualmente ────────────────────

# CA1827: .Count() comparado con 0 -> Any() / !Any()
if ($content -match '\.Count\(\)\s*(==|!=|>|<|>=|<=)\s*0\b') {
    $findings.Add('CA1827 (perf): .Count() == 0 / > 0 -- usar !Any() / Any() (no enumera toda la coleccion)')
}

# CA1820: comparar string con "" -> string.IsNullOrEmpty / Length
if ($content -match '(==|!=)\s*""' -or $content -match '(==|!=)\s*string\.Empty\b') {
    $findings.Add('CA1820 (perf): s == "" -- usar string.IsNullOrEmpty(s) o s.Length == 0')
}

# CA1834: StringBuilder.Append("x") con string de UN caracter -> Append(char)
if ($content -match '\.Append\("(?:[^"\\]|\\.)"\)') {
    $findings.Add('CA1834 (perf): StringBuilder.Append("x") de 1 caracter -- usar el literal char Append(''x'')')
}

# CA1848 / CA1873: logging con interpolacion $"..." -> plantilla estructurada (+ IsEnabled)
if ($content -match '(?i)\b(?:_?log(?:ger)?)\.Log\w*\(\s*(?:LogLevel\.\w+\s*,\s*)?\$"') {
    $findings.Add('CA1848/CA1873 (perf logging): _logger.LogXxx($"...") -- usar plantilla estructurada "{Campo}" con args (evita interpolacion/boxing eager); en hot paths, LoggerMessage o guarda IsEnabled')
}

if ($findings.Count -gt 0) {
    Write-Host "AVISO (estilo/rendimiento .NET): posibles reglas aplicables en este archivo:"
    foreach ($f in $findings) { Write-Host "  - $f" }
    Write-Host "  Estilo (IDExxxx): dotnet format style <sln> --diagnostics <IDExxxx> --severity info"
    Write-Host "  Rendimiento (CAxxxx): dotnet format analyzers <sln> --diagnostics <CAxxxx> --severity info"
    Write-Host "  Conserva la convencion de campos _field. Informativo, no bloquea."
    Write-Host "  Ver .claude/rules/dotnet-code-style.md (cubre tambien CA1859/CA1861/CA1822/CA1873 via analizadores)."
    exit 1
}

exit 0
