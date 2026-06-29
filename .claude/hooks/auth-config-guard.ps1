# auth-config-guard.ps1 - Validar configuracion de autenticacion (Azure AD + providers extensibles)
# Evento: PreToolUse[Write|Edit]
# Exit 1 = AVISO informativo (no bloquea Write/Edit), Exit 0 = sin hallazgos
# Version: 2.1.0 (Plantilla STIC.IA v3.9.0 hotfix #28)
#   v2.1.0: politica Comillas WARN-first (igual secret-scanner v2.0 y antipattern-guard v2.0).
#           CAMBIOS:
#             - exit 2 -> exit 1 (no bloquea Edit, queda como AVISO informativo)
#             - Mensajes via [Console]::Error.WriteLine (stderr) para que Claude Code
#               surface al usuario correctamente.
#             - Razon: configs OAuth pueden tener providers legitimos (Banner, Sigma,
#               EWP) no-Azure AD que el hook no conoce. WARN respeta auth alternativos
#               + Sistemas no requiere Azure AD obligatorio universal.
#           Bloqueo activable bumpeando a v3.0.0 si Sistemas lo mandata.
#   v2.0.0: extensible via _duran/oauth-providers.json (J2). Azure AD sigue hardcoded como default.
#   v1.0.0: solo Azure AD hardcoded (v3.8.0)

param()
. "$PSScriptRoot/hook-helpers.ps1"

$hookData = Get-HookInput
if (-not $hookData) { exit 0 }

# Solo archivos de configuracion y Program.cs/Startup.cs
$filePath = Get-FilePath $hookData
if (-not $filePath) { exit 0 }
if ($filePath -notmatch 'appsettings.*\.json$|Program\.cs$|Startup\.cs$|\.cs$') { exit 0 }

# Extraer contenido (decodificado si viene como objeto, escapado si viene como string)
$content = Get-NewContent $hookData
if (-not $content) { exit 0 }

# Helper v2.1.0: emitir aviso por stderr (visible para Claude Code)
function Write-Aviso { param([string]$Msg) [Console]::Error.WriteLine($Msg) }

$errors = 0
$warnings = 0

# ============================================================================
# REGLAS AZURE AD HARDCODED (v1.0.0 - backwards compatible)
# ============================================================================

# TenantId debe ser GUID (no "common" ni "organizations" en produccion)
if ($content -match '"TenantId"\s*:\s*"(common|organizations|consumers)"') {
    Write-Aviso "AVISO [AzureAd]: TenantId='$($Matches[1])' es inseguro. Usar GUID del tenant Comillas."
    $errors++
}

# Redirect URIs no deben tener wildcards
if ($content -match 'RedirectUri.*\*|CallbackPath.*\*|redirect_uri.*\*') {
    Write-Aviso "AVISO [AzureAd]: Wildcard en redirect URI detectado. Usar URIs exactas."
    $errors++
}

# Audience debe estar configurado
if ($content -match 'AzureAd|JwtBearer|AddMicrosoftIdentity') {
    if ($content -notmatch 'Audience|ValidAudience|ValidAudiences') {
        Write-Aviso "AVISO [AzureAd]: Configuracion Azure AD sin Audience. Verificar que ValidateAudience=true."
        $warnings++
    }
}

# ValidateIssuer/Audience/Lifetime deben ser true
if ($content -match 'ValidateIssuer\s*=\s*false') {
    Write-Aviso "AVISO [AzureAd]: ValidateIssuer=false es inseguro. Debe ser true."
    $errors++
}
if ($content -match 'ValidateAudience\s*=\s*false') {
    Write-Aviso "AVISO [AzureAd]: ValidateAudience=false es inseguro. Debe ser true."
    $errors++
}
if ($content -match 'ValidateLifetime\s*=\s*false') {
    Write-Aviso "AVISO [AzureAd]: ValidateLifetime=false es inseguro. Tokens expirados serian aceptados."
    $errors++
}

# ClockSkew excesivo (>5 min)
if ($content -match 'ClockSkew\s*=\s*TimeSpan\.From(Minutes|Hours)\s*\(\s*(\d+)') {
    $unit = $Matches[1]
    $value = [int]$Matches[2]
    if ($unit -eq "Hours" -or ($unit -eq "Minutes" -and $value -gt 5)) {
        Write-Aviso "AVISO [AzureAd]: ClockSkew de $value $unit es excesivo. Maximo recomendado: 5 minutos."
        $warnings++
    }
}

# ============================================================================
# REGLAS EXTENSIBLES via _duran/oauth-providers.json (v2.0.0)
# ============================================================================
# Schema esperado:
# {
#   "providers": [
#     {
#       "name": "OracleCloud",
#       "description": "Oracle HCM Cloud OAuth2",
#       "rules": [
#         {
#           "pattern": "regex (escapado JSON)",
#           "severity": "block" | "warn",
#           "message": "mensaje para el dev"
#         }
#       ]
#     }
#   ]
# }
# Si no existe el archivo, el hook funciona como v1.0.0 (solo Azure AD).
# El consumidor lo crea opt-in para sus dominios (Oracle, Banner, Sigma, etc.).

$repoRoot = if ($env:CLAUDE_PROJECT_DIR) { $env:CLAUDE_PROJECT_DIR } else { (Get-Location).Path }
$providersFile = Join-Path $repoRoot '_duran/oauth-providers.json'

if (Test-Path $providersFile) {
    try {
        $providersConfig = Get-Content $providersFile -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($providersConfig.providers) {
            foreach ($provider in $providersConfig.providers) {
                if (-not $provider.name -or -not $provider.rules) { continue }

                foreach ($rule in $provider.rules) {
                    if (-not $rule.pattern -or -not $rule.severity) { continue }

                    try {
                        if ($content -match $rule.pattern) {
                            $msg = if ($rule.message) { $rule.message } else { "Regla violada" }
                            $matchedText = $Matches[0]
                            $matchedShort = if ($matchedText.Length -gt 60) { $matchedText.Substring(0, 57) + "..." } else { $matchedText }

                            switch ($rule.severity.ToLower()) {
                                'block' {
                                    Write-Aviso "AVISO [$($provider.name)]: $msg"
                                    Write-Aviso "  Match: $matchedShort"
                                    $errors++
                                }
                                'warn' {
                                    Write-Aviso "AVISO [$($provider.name)]: $msg"
                                    Write-Aviso "  Match: $matchedShort"
                                    $warnings++
                                }
                            }
                        }
                    } catch {
                        Write-Aviso "AVISO [auth-config-guard]: regex invalida en provider '$($provider.name)': $($rule.pattern)"
                    }
                }
            }
        }
    } catch {
        Write-Aviso "AVISO [auth-config-guard]: no se pudo parsear _duran/oauth-providers.json: $($_.Exception.Message)"
    }
}

# ============================================================================
# RESULTADO
# ============================================================================

if ($errors -gt 0 -or $warnings -gt 0) {
    $total = $errors + $warnings
    [Console]::Error.WriteLine("")
    [Console]::Error.WriteLine("Se detectaron $total posible(s) hallazgo(s) de configuracion auth. Politica Comillas:")
    [Console]::Error.WriteLine("  - WARN-first (no bloqueante): configs OAuth pueden tener providers legitimos no-AzureAD")
    [Console]::Error.WriteLine("  - Estandar Comillas: Azure AD con tenant especifico para apps internas")
    [Console]::Error.WriteLine("  - Providers extensibles: _duran/oauth-providers.json (Banner, Sigma, EWP, etc.)")
    [Console]::Error.WriteLine("  - Si la auth es legitima no-AzureAD: documentar en _duran/DECISIONES.md como ADR")
    [Console]::Error.WriteLine("  - Si Sistemas decide enforcement: hook bumpea a v3.0.0 con exit 2")
    exit 1
}

exit 0
