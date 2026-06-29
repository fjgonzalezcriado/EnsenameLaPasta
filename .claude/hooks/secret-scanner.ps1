# secret-scanner.ps1 - Detectar secrets/credenciales antes de escribir
# Evento: PreToolUse[Write|Edit]
# Exit 1 = AVISO informativo (no bloquea Write/Edit), Exit 0 = sin hallazgos
# Version: 2.0.0 (STIC.IA v3.9.0)
#   v2.0.0: politica Comillas — todas las detecciones son AVISO no BLOQUEO.
#           Decision: Sistemas no requiere KV obligatorio aun (entorno seguro
#           Comillas: red interna + AD + VPN). Bloqueo activable bumpeando a
#           v3.0.0 cuando Sistemas lo mandate. Mismo patron que
#           sql-nomenclatura-guard.ps1 (exit 1 = informativo, no impide Write).
#   v1.1.0: + deteccion de tokens hardcoded en bloques mcpServers de
#           .mcp.json, ~/.claude.json y .claude/settings*.json (GitHub PAT,
#           Atlassian, claves _TOKEN/_KEY/_SECRET sin ${env:...} placeholder).
#   v1.0.0: deteccion generica connection strings, API keys, bearer tokens, etc.

param()
. "$PSScriptRoot/hook-helpers.ps1"

$hookData = Get-HookInput; $toolInput = Get-ToolInput $hookData
if (-not $toolInput) { exit 0 }

# Extraer contenido nuevo (new_string para Edit, content para Write)
$content = $null
if ($toolInput -match '"new_string"\s*:\s*"((?:[^"\\]|\\.)*)"') {
    $content = $Matches[1]
} elseif ($toolInput -match '"content"\s*:\s*"((?:[^"\\]|\\.)*)"') {
    $content = $Matches[1]
}
if (-not $content) { exit 0 }

# v1.1.0: des-escapar secuencias JSON (\" -> ", \\ -> \, \n -> newline)
# Necesario para que los patrones que buscan comillas literales (ej. "ghp_...") funcionen.
$content = $content -replace '\\n', "`n" -replace '\\"', '"' -replace '\\\\', '\'

# No escanear archivos de config de ejemplo o templates
$filePath = ""
if ($toolInput -match '"file_path"\s*:\s*"([^"]+)"') {
    $filePath = $Matches[1]
}
if ($filePath -match '\.template$|\.example$|\.sample$|SKILL\.md$|README\.md$') { exit 0 }

$warnings = 0

# === PATRONES DE SECRETS ===

# Connection strings con password
if ($content -match 'Password\s*=\s*[^;]{3,}|pwd\s*=\s*[^;]{3,}' -and $content -notmatch 'GetConnectionString|IOptions|configuration\[|builder\.Configuration') {
    Write-Host "AVISO: Connection string con password detectada. Recomendado: Azure Key Vault o User Secrets."
    $warnings++
}

# API Keys hardcoded
if ($content -match '"(sk-[a-zA-Z0-9]{20,}|api[_-]?key[_-]?[=:]\s*[''"][a-zA-Z0-9]{10,})"') {
    Write-Host "AVISO: API key hardcoded detectada. Recomendado: configuracion o Key Vault."
    $warnings++
}

# Bearer tokens
if ($content -match '"Bearer\s+[a-zA-Z0-9\-._~+/]+=*"' -and $content -notmatch 'example|placeholder|test') {
    Write-Host "AVISO: Bearer token hardcoded detectado."
    $warnings++
}

# Azure Storage keys
if ($content -match 'AccountKey\s*=\s*[a-zA-Z0-9+/]{40,}') {
    Write-Host "AVISO: Azure Storage key hardcoded. Recomendado: DefaultAzureCredential."
    $warnings++
}

# Passwords en strings literales (alta confianza)
if ($content -match '"password"\s*:\s*"[^"]{4,}"' -and $content -notmatch 'schema|example|placeholder|validation|error') {
    Write-Host "AVISO: Password en string literal. Recomendado: configuracion segura."
    $warnings++
}

# === v1.1.0: MCP servers con secrets hardcoded en bloque env ===
# Cubre .mcp.json (project), ~/.claude.json (user), .claude/settings*.json
$isMcpConfig = $filePath -match '\.mcp\.json$|[/\\]\.claude\.json$|[/\\]claude\.json$|\.claude[/\\]settings(\.local)?\.json$'
$mentionsMcpServers = $content -match '"mcpServers"\s*:'

if ($isMcpConfig -or $mentionsMcpServers) {
    # GitHub PAT clasico (ghp_) o fine-grained (github_pat_)
    if ($content -match '"(ghp_[A-Za-z0-9]{36,}|github_pat_[A-Za-z0-9_]{30,})"') {
        Write-Host "AVISO: GitHub PAT hardcoded en config MCP. Recomendado: placeholder `${env:GITHUB_TOKEN} y setx en sesion."
        $warnings++
    }
    # Atlassian API token en JIRA_API_TOKEN/ATLASSIAN_API_TOKEN/CONFLUENCE_API_TOKEN con valor literal
    $rxAtlassian = [regex]'"(JIRA|ATLASSIAN|CONFLUENCE)_API_TOKEN"\s*:\s*"((?!\$\{)[A-Za-z0-9]{20,})"'
    if ($rxAtlassian.IsMatch($content)) {
        Write-Host "AVISO: Atlassian/Jira/Confluence API token hardcoded en config MCP. Recomendado: `${env:JIRA_API_TOKEN}."
        $warnings++
    }
    # Anthropic API key
    if ($content -match '"(sk-ant-[A-Za-z0-9_-]{40,})"') {
        Write-Host "AVISO: Anthropic API key hardcoded en config MCP. Recomendado: `${env:ANTHROPIC_API_KEY}."
        $warnings++
    }
    # Generic env block: cualquier clave _TOKEN/_KEY/_SECRET/_PASSWORD con valor literal (no placeholder)
    # Excluye: ${env:..}, {{var}}, <placeholder>, TODO, YOUR_*, PLACEHOLDER, EXAMPLE, SAMPLE, REPLACE_*
    $rxEnvLiteral = [regex]'"(\w*(?:TOKEN|KEY|SECRET|PASSWORD|PWD)\w*)"\s*:\s*"(?!\$\{|\{\{|<|TODO|YOUR_|PLACEHOLDER|EXAMPLE|SAMPLE|REPLACE_|XXX|---)([^"]{12,})"'
    foreach ($m in $rxEnvLiteral.Matches($content)) {
        $envKey = $m.Groups[1].Value
        $valuePreview = $m.Groups[2].Value.Substring(0, [Math]::Min(8, $m.Groups[2].Value.Length))
        Write-Host "AVISO: Posible secret hardcoded en bloque MCP env (clave '$envKey', valor empieza con '$valuePreview...'). Recomendado: `${env:$envKey}."
        $warnings++
    }
}

if ($warnings -gt 0) {
    Write-Host ""
    Write-Host "Se detectaron $warnings posible(s) secret(s) hardcoded. Politica Comillas:"
    Write-Host "  - Entorno seguro (red interna Comillas, AD, VPN) - NO bloqueante actualmente"
    Write-Host "  - Recomendado migrar progresivamente a Azure Key Vault"
    Write-Host "  - Documentar en _duran/DEUDA_TECNICA.md como SEC-XXX si no se migra ya"
    Write-Host "  - Si Sistemas decide enforcement: hook bumpea a v3.0.0 con exit 2"
    exit 1   # exit 1 = AVISO informativo (no impide el Write), patron sql-nomenclatura-guard
}

exit 0
