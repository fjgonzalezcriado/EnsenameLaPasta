# sql-injection-guard.ps1 - Detectar SQL inseguro en codigo C#
# Evento: PostToolUse[Write|Edit]
# Exit 0 = siempre (avisos, no bloquea - el dev decide)
# Version: 1.0.0 (STIC.IA v3.8.0)

param()
. "$PSScriptRoot/hook-helpers.ps1"

$hookData = Get-HookInput; $toolInput = Get-ToolInput $hookData
if (-not $toolInput) { exit 0 }

# Solo archivos .cs
$filePath = ""
if ($toolInput -match '"file_path"\s*:\s*"([^"]+)"') {
    $filePath = $Matches[1]
}
if ($filePath -notmatch '\.cs$') { exit 0 }

# Extraer contenido
$content = $null
if ($toolInput -match '"new_string"\s*:\s*"((?:[^"\\]|\\.)*)"') {
    $content = $Matches[1]
} elseif ($toolInput -match '"content"\s*:\s*"((?:[^"\\]|\\.)*)"') {
    $content = $Matches[1]
}
if (-not $content) { exit 0 }

$warnings = 0

# === PATRONES SQL INSEGURO ===

# FromSqlRaw con interpolacion de string
if ($content -match 'FromSqlRaw\s*\(\s*\$"' -or $content -match 'FromSqlRaw\s*\(\s*"[^"]*"\s*\+') {
    Write-Host "SQL INJECTION: FromSqlRaw con interpolacion/concatenacion. Usar FromSqlInterpolated o parametros."
    $warnings++
}

# ExecuteSqlRaw con interpolacion
if ($content -match 'ExecuteSqlRaw\s*\(\s*\$"' -or $content -match 'ExecuteSqlRaw\s*\(\s*"[^"]*"\s*\+') {
    Write-Host "SQL INJECTION: ExecuteSqlRaw con interpolacion. Usar ExecuteSqlInterpolated."
    $warnings++
}

# SqlCommand con concatenacion
if ($content -match 'SqlCommand\s*\(\s*"[^"]*"\s*\+' -or $content -match 'CommandText\s*=\s*"[^"]*"\s*\+') {
    Write-Host "SQL INJECTION: SqlCommand con concatenacion de string. Usar SqlParameter."
    $warnings++
}

# EXEC con concatenacion en string SQL
if ($content -match "EXEC\s*\(\s*'[^']*'\s*\+|EXEC\s*\(\s*""[^""]*""\s*\+") {
    Write-Host "SQL INJECTION: EXEC con concatenacion. Usar sp_executesql con parametros."
    $warnings++
}

# string.Format en queries SQL
if ($content -match 'string\.Format\s*\(.*SELECT|string\.Format\s*\(.*INSERT|string\.Format\s*\(.*UPDATE|string\.Format\s*\(.*DELETE') {
    Write-Host "SQL INJECTION: string.Format en query SQL. Usar parametros."
    $warnings++
}

if ($warnings -gt 0) {
    Write-Host ""
    Write-Host "Se detectaron $warnings patron(es) de SQL injection potencial."
    Write-Host "Referencia: OWASP A03:2021 - Injection"
    Write-Host "Solucion: Usar siempre parametros (@param) o FromSqlInterpolated."
}

exit 0
