# token-validation-guard.ps1 - Verificar TokenValidationParameters
# Evento: PostToolUse[Write|Edit]
# Exit 0 = siempre (avisos informativos)
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

# Solo actuar si hay TokenValidationParameters
if ($content -notmatch 'TokenValidationParameters') { exit 0 }

$warnings = 0

# === CHECKLIST TOKEN VALIDATION ===

# ValidateIssuer
if ($content -match 'ValidateIssuer\s*=\s*false') {
    Write-Host "SEGURIDAD: ValidateIssuer=false. Cualquier emisor seria aceptado."
    $warnings++
}

# ValidateAudience
if ($content -match 'ValidateAudience\s*=\s*false') {
    Write-Host "SEGURIDAD: ValidateAudience=false. Tokens para otras apps serian aceptados."
    $warnings++
}

# ValidateLifetime
if ($content -match 'ValidateLifetime\s*=\s*false') {
    Write-Host "SEGURIDAD: ValidateLifetime=false. Tokens expirados serian aceptados."
    $warnings++
}

# ValidateIssuerSigningKey
if ($content -match 'ValidateIssuerSigningKey\s*=\s*false') {
    Write-Host "SEGURIDAD: ValidateIssuerSigningKey=false. Tokens sin firma valida serian aceptados."
    $warnings++
}

# RequireExpirationTime
if ($content -match 'RequireExpirationTime\s*=\s*false') {
    Write-Host "SEGURIDAD: RequireExpirationTime=false. Tokens sin expiracion serian aceptados."
    $warnings++
}

# Algoritmos inseguros
if ($content -match 'SecurityAlgorithms\.(HmacSha256|None)|"none"|"HS256"') {
    if ($content -match '"none"' -or $content -match 'SecurityAlgorithms\.None') {
        Write-Host "SEGURIDAD CRITICA: Algoritmo 'none' detectado. Tokens sin firma serian aceptados."
        $warnings++
    }
}

# ClockSkew excesivo
if ($content -match 'ClockSkew\s*=\s*TimeSpan\.MaxValue|ClockSkew\s*=\s*TimeSpan\.FromHours') {
    Write-Host "SEGURIDAD: ClockSkew excesivo. Maximo recomendado: 5 minutos."
    $warnings++
}

if ($warnings -gt 0) {
    Write-Host ""
    Write-Host "Se detectaron $warnings problema(s) en TokenValidationParameters."
    Write-Host "Referencia: Estandar Comillas - Azure AD con validacion completa obligatoria."
    Write-Host "Checklist: ValidateIssuer=true, ValidateAudience=true, ValidateLifetime=true,"
    Write-Host "           ValidateIssuerSigningKey=true, ClockSkew<=5min."
}

exit 0
