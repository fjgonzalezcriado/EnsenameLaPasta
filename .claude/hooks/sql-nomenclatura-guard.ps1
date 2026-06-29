# sql-nomenclatura-guard.ps1 - Detectar violaciones de nomenclatura SQL Comillas
# Evento: PostToolUse[Write|Edit]
# NO bloqueante: emite warning en stderr (exit 1 = informativo, no impide el Write)
# Version: 1.1.0 (STIC.IA v3.8.7)
#   v1.1.0: scope explicito a 03_Desarrollo/SQL/, Scripts/, Database/, Deploy/.
#           Nuevo patron 8: prefijos legacy custom (int_, aud_, tmp_, migr_) en SPs nuevos.
#   v1.0.0: scope BaseDatos/StoredProcedures/ + extension .sql/.sqlproj.

param()
. "$PSScriptRoot/hook-helpers.ps1"

$hookData = Get-HookInput
$toolInput = Get-ToolInput $hookData
if (-not $toolInput) { exit 0 }

# Detectar ruta del archivo y contenido escrito
$filePath = ""
if ($toolInput -match '"file_path"\s*:\s*"([^"]+)"') {
    $filePath = $Matches[1]
}

$content = $null
if ($toolInput -match '"new_string"\s*:\s*"((?:[^"\\]|\\.)*)"') {
    $content = $Matches[1]
} elseif ($toolInput -match '"content"\s*:\s*"((?:[^"\\]|\\.)*)"') {
    $content = $Matches[1]
}
if (-not $content) { exit 0 }

# Solo escanear si el contenido o el path sugieren SQL
$isSqlFile = $filePath -match '\.(sql|sqlproj|tsql|ddl|dml)$'
$isRepoFile = $filePath -match 'Repository\.cs$|Repositorio\.cs$|Repositorios[/\\]|Repositories[/\\]|StoredProcedures[/\\]|DbContext\.cs$|ProcedimientoAlmacenado\.cs$'
# v1.1.0: scope explicito a scripts de deploy fuera de sqlproj (zona ciega historica)
$isDeployScript = $filePath -match '03_Desarrollo[/\\]SQL[/\\]|[/\\]Scripts[/\\][^/\\]+\.sql$|[/\\]Database[/\\][^/\\]+\.sql$|[/\\]Deploy[/\\][^/\\]+\.sql$|[/\\]Migrations[/\\][^/\\]+\.sql$'
$mentionsSqlObject = $content -match 'CREATE\s+(OR\s+ALTER\s+)?(PROCEDURE|PROC|FUNCTION|VIEW|TRIGGER)'

if (-not ($isSqlFile -or $isRepoFile -or $isDeployScript -or $mentionsSqlObject)) { exit 0 }

# Respetar marcadores de legacy (3 vías)
# Vía 1: ruta contiene carpeta legacy
if ($filePath -match 'Legacy[/\\]|Obsoleto[/\\]|MigracionPendiente[/\\]') { exit 0 }
# Vía 2: marcador inline al inicio del archivo
if ($content -match '(?m)^\s*--\s*LEGACY:\s*no\s+renombrar') { exit 0 }
# Vía 3: baseline de legacy en .claude/sql-legacy-baseline.txt
$projectDir = $env:CLAUDE_PROJECT_DIR
if (-not $projectDir) { $projectDir = (Get-Location).Path }
$baselineFile = Join-Path $projectDir ".claude/sql-legacy-baseline.txt"
if (Test-Path $baselineFile) {
    $relativePath = $filePath -replace [regex]::Escape($projectDir + "\"), "" -replace "\\", "/"
    $baseline = Get-Content $baselineFile -ErrorAction SilentlyContinue
    if ($baseline -and ($baseline -contains $relativePath)) {
        [Console]::Error.WriteLine("[SQL] Nota: '$relativePath' esta en baseline legacy. Si lo renombras, actualiza .claude/sql-legacy-baseline.txt")
        exit 0
    }
}

# Destravar el contenido (el JSON escapa comillas y saltos)
$decoded = $content -replace '\\n', "`n" -replace '\\"', '"' -replace '\\\\', '\'

$warnings = New-Object System.Collections.Generic.List[string]

# --- Patrones de violacion ---

# 1. CREATE PROCEDURE con prefijo usp_/sp_/pr_/proc_/pa_
$rxPrefProc = [regex]'(?im)CREATE\s+(?:OR\s+ALTER\s+)?PROC(?:EDURE)?\s+\[?(?:\w+\]?\.\[?)?\[?(usp|sp|pr|proc|pa)_\w+\]?'
foreach ($m in $rxPrefProc.Matches($decoded)) {
    $warnings.Add("[SQL] CREATE PROCEDURE con prefijo prohibido '$($m.Groups[1].Value)_': $($m.Value.Trim())")
}

# 2. CREATE PROCEDURE con sufijo _Listar/_Guardar/_Eliminar/_Leer/_L/_G
$rxSufijoProc = [regex]'(?im)CREATE\s+(?:OR\s+ALTER\s+)?PROC(?:EDURE)?\s+\[?\w+\]?\.\[?\w+_(Listar|Guardar|Eliminar|Leer|Actualizar|Insertar|Buscar|L|G|E|A|B)\]?\b'
foreach ($m in $rxSufijoProc.Matches($decoded)) {
    $warnings.Add("[SQL] CREATE PROCEDURE con sufijo prohibido '_$($m.Groups[1].Value)': $($m.Value.Trim())")
}

# 3. CREATE PROCEDURE sin schema (solo nombre)
$rxSinSchema = [regex]'(?im)CREATE\s+(?:OR\s+ALTER\s+)?PROC(?:EDURE)?\s+\[?\w+\]?\s*(\(|@|AS\b)'
foreach ($m in $rxSinSchema.Matches($decoded)) {
    # Descartar si de hecho hay schema.nombre
    if ($m.Value -notmatch '\.\w') {
        $warnings.Add("[SQL] CREATE PROCEDURE sin schema (usar 'schema.Nombre'): $($m.Value.Substring(0, [Math]::Min($m.Value.Length, 80)).Trim())")
    }
}

# 4. CREATE FUNCTION sin prefijo fn_/fnt_
$rxFuncSinPref = [regex]'(?im)CREATE\s+(?:OR\s+ALTER\s+)?FUNCTION\s+\[?\w+\]?\.\[?(\w+)\]?\s*\('
foreach ($m in $rxFuncSinPref.Matches($decoded)) {
    $name = $m.Groups[1].Value
    if ($name -notmatch '^(fn_|fnt_)') {
        $warnings.Add("[SQL] CREATE FUNCTION sin prefijo 'fn_' (escalar) o 'fnt_' (tabla): $($m.Value.Trim())")
    }
}

# 5. CREATE FUNCTION escalar con fnt_ o tabla con fn_ (errores cruzados)
#    Detectar tabla por "RETURNS TABLE" en las 500 chars siguientes
$rxFuncConRet = [regex]'(?is)CREATE\s+(?:OR\s+ALTER\s+)?FUNCTION\s+\[?\w+\]?\.\[?(fn_|fnt_)(\w+)\]?\s*\([^)]*\)\s*RETURNS\s+(TABLE|\w+)'
foreach ($m in $rxFuncConRet.Matches($decoded)) {
    $prefix = $m.Groups[1].Value
    $returns = $m.Groups[3].Value.ToUpper()
    if ($prefix -eq 'fn_' -and $returns -eq 'TABLE') {
        $warnings.Add("[SQL] Funcion tabla (RETURNS TABLE) debe usar prefijo 'fnt_', no 'fn_': $($m.Value.Substring(0, [Math]::Min($m.Value.Length, 80)).Trim())")
    }
    if ($prefix -eq 'fnt_' -and $returns -ne 'TABLE') {
        $warnings.Add("[SQL] Funcion escalar debe usar prefijo 'fn_', no 'fnt_': $($m.Value.Substring(0, [Math]::Min($m.Value.Length, 80)).Trim())")
    }
}

# 6. CREATE VIEW sin prefijo vw_
$rxView = [regex]'(?im)CREATE\s+(?:OR\s+ALTER\s+)?VIEW\s+\[?\w+\]?\.\[?(\w+)\]?\b'
foreach ($m in $rxView.Matches($decoded)) {
    $name = $m.Groups[1].Value
    if ($name -notmatch '^vw_') {
        $warnings.Add("[SQL] CREATE VIEW sin prefijo 'vw_': $($m.Value.Trim())")
    }
}

# 7. CREATE TRIGGER sin prefijo tr_
$rxTrigger = [regex]'(?im)CREATE\s+(?:OR\s+ALTER\s+)?TRIGGER\s+\[?\w+\]?\.\[?(\w+)\]?\b'
foreach ($m in $rxTrigger.Matches($decoded)) {
    $name = $m.Groups[1].Value
    if ($name -notmatch '^tr_') {
        $warnings.Add("[SQL] CREATE TRIGGER sin prefijo 'tr_': $($m.Value.Trim())")
    }
}

# 8. CREATE PROCEDURE con prefijos legacy custom (int_, aud_, tmp_, migr_)
# Trampa: BD cross-schema con SPs legacy mayoritarios (ej. SecreBD con int_*_leer).
# Para SPs NUEVOS, aplicar §8.1 aunque los vecinos sean legacy. NO imitar el patron.
# Si el archivo esta en baseline, este check ya se salto al inicio.
$rxLegacyClone = [regex]'(?im)CREATE\s+(?:OR\s+ALTER\s+)?PROC(?:EDURE)?\s+\[?\w+\]?\.\[?(int|aud|tmp|migr|old|bak)_\w+\]?'
foreach ($m in $rxLegacyClone.Matches($decoded)) {
    $prefix = $m.Groups[1].Value.ToLower()
    $warnings.Add("[SQL] CREATE PROCEDURE con prefijo legacy custom '${prefix}_' en archivo NUEVO: $($m.Value.Trim())")
    $warnings.Add("       -> Trampa: NO imitar patron legacy de SPs vecinos. Aplicar §8.1: {schema}.{Accion}{Entidad}")
    $warnings.Add("       -> Si es legacy intencional, mover a Legacy/ o anadir a .claude/sql-legacy-baseline.txt")
}

# --- Features POST-2017 que no deberian usarse por defecto ---

if ($decoded -match '(?i)GENERATE_SERIES\s*\(') {
    $warnings.Add("[SQL-2017] GENERATE_SERIES es SQL Server 2022+. Usar CTE recursivo o tabla de numeros.")
}
if ($decoded -match '(?i)IS\s+(NOT\s+)?DISTINCT\s+FROM') {
    $warnings.Add("[SQL-2017] IS [NOT] DISTINCT FROM es 2022+. Usar '((a = b) OR (a IS NULL AND b IS NULL))'.")
}
if ($decoded -match '(?i)\bDATE_BUCKET\s*\(') {
    $warnings.Add("[SQL-2017] DATE_BUCKET es 2022+. Usar aritmetica con DATEADD/DATEDIFF.")
}
if ($decoded -match '(?i)\b(GREATEST|LEAST)\s*\(') {
    $warnings.Add("[SQL-2017] GREATEST/LEAST son 2022+. Usar CASE WHEN o subquery con VALUES.")
}
if ($decoded -match '(?i)STRING_SPLIT\s*\([^)]+,\s*[^,)]+,\s*1\s*\)') {
    $warnings.Add("[SQL-2017] STRING_SPLIT con 3er parametro (enable_ordinal) es 2022+. En 2017 solo 2 parametros.")
}
if ($decoded -match '(?i)_UTF8\b') {
    $warnings.Add("[SQL-2017] Collations _UTF8 son 2019+. Usar collations clasicas SQL_Latin1_General_*.")
}

# --- Emitir warnings ---

if ($warnings.Count -eq 0) { exit 0 }

# stderr para que claude lo reciba como "logueado pero no bloqueante"
[Console]::Error.WriteLine("")
[Console]::Error.WriteLine("+----------------------------------------------------------------+")
[Console]::Error.WriteLine("| sql-nomenclatura-guard: $($warnings.Count) violacion(es) detectada(s)")
[Console]::Error.WriteLine("+----------------------------------------------------------------+")
foreach ($w in $warnings) {
    [Console]::Error.WriteLine("  - $w")
}
[Console]::Error.WriteLine("")
[Console]::Error.WriteLine("Referencia: CLAUDE_BASE_COMILLAS.md seccion 8.1 / rules/database.md")
[Console]::Error.WriteLine("Patron OK:  CREATE PROCEDURE {schema}.{Accion}{Entidad}")
[Console]::Error.WriteLine("            CREATE FUNCTION {schema}.fn_{Descr}  |  fnt_ para TVF")
[Console]::Error.WriteLine("            CREATE VIEW      {schema}.vw_{Descr}")
[Console]::Error.WriteLine("Excepcion:  Legacy/, Obsoleto/, MigracionPendiente/, -- LEGACY: no renombrar")
[Console]::Error.WriteLine("            o ruta listada en .claude/sql-legacy-baseline.txt")
[Console]::Error.WriteLine("Scope:      .sql/.sqlproj/.tsql + 03_Desarrollo/SQL/, Scripts/, Database/, Deploy/")
[Console]::Error.WriteLine("")

# exit 1 = warning visible a Claude pero NO bloquea el Write (a diferencia de exit 2)
exit 1
