#Requires -Version 5.1
<#
.SYNOPSIS
    Contexto de inicio de sesion STIC.IA para Claude Code
.DESCRIPTION
    Hook SessionStart que inyecta contexto del proyecto en Claude.
    Lee ESTADO_PROYECTO.json y emite informacion relevante a stdout,
    que Claude Code captura como contexto inicial de la sesion.
    Tambien detecta complementos faltantes y avisa al usuario (stderr).
.NOTES
    STIC.IA v3.8.0 - Universidad Pontificia Comillas
    stdout -> contexto para Claude (no visible al usuario)
    stderr -> visible al usuario en terminal
#>

$ProjectDir = if ($env:CLAUDE_PROJECT_DIR) { $env:CLAUDE_PROJECT_DIR } else { Get-Location }
$EstadoFile = Join-Path (Join-Path $ProjectDir "_duran") "ESTADO_PROYECTO.json"

# Valores por defecto
$Version = "?.?.?"
$Idioma = "es-ES"
$Modo = ""
$Proyecto = ""
$Evolutivo = ""
$Fase = ""
$Alertas = @()

if (Test-Path $EstadoFile) {
    try {
        $estado = Get-Content $EstadoFile -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($estado.ecosistema.version) { $Version = $estado.ecosistema.version }
        if ($estado.configuracion.idioma) { $Idioma = $estado.configuracion.idioma }
        if ($estado.estado.modo) { $Modo = $estado.estado.modo }
        if ($estado.estado.fase_actual) { $Fase = $estado.estado.fase_actual }
        if ($estado.proyecto.nombre -and $estado.proyecto.nombre -ne "[NOMBRE_PROYECTO]") {
            $Proyecto = $estado.proyecto.nombre
        }
        if ($estado.evolutivoActivo) {
            $Evolutivo = $estado.evolutivoActivo
        }
        if ($estado.alertas.activas -and $estado.alertas.activas.Count -gt 0) {
            $Alertas = $estado.alertas.activas
        }
    } catch {
        # Silenciar errores de parseo
    }
}

# Fallback: leer version de VERSION.json si no se obtuvo de ESTADO_PROYECTO.json
if ($Version -eq "?.?.?") {
    $VersionFile = Join-Path (Join-Path $ProjectDir "_duran") "VERSION.json"
    if (Test-Path $VersionFile) {
        try {
            $vData = Get-Content $VersionFile -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($vData.installedVersion) { $Version = $vData.installedVersion }
        } catch { }
    }
}

# === DETECTAR ESTADO DE COMPLEMENTOS ===
$roslynTool = $false
$roslynMcp = $false
$context7Mcp = $false
$dotnetSkills = $false

try {
    $tools = & dotnet tool list -g 2>&1 | Out-String
    if ($tools -match "cwm\.roslynnavigator|cwm-roslyn-navigator") {
        $roslynTool = $true
    }
} catch { }

try {
    $mcpList = & claude mcp list 2>&1 | Out-String
    if ($mcpList -match "cwm-roslyn-navigator") { $roslynMcp = $true }
    if ($mcpList -match "context7") { $context7Mcp = $true }
} catch { }

try {
    $pluginList = & claude plugin list 2>&1 | Out-String
    if ($pluginList -match "dotnet" -and $pluginList -notmatch "No plugins installed") {
        $dotnetSkills = $true
    }
} catch { }

# === ACTUALIZAR companyAnnouncements DINAMICAMENTE ===
try {
    $settingsFile = Join-Path (Join-Path $ProjectDir ".claude") "settings.json"
    if (Test-Path $settingsFile) {
        # Construir banner dinamico
        $bannerParts = @()
        $bannerParts += "STIC.IA v$Version"
        if ($Proyecto) { $bannerParts += $Proyecto }

        # Rama Git (sin crear lock para no colisionar con IDE)
        try {
            $env:GIT_OPTIONAL_LOCKS = '0'
            $branch = & git -C $ProjectDir rev-parse --abbrev-ref HEAD 2>&1
            if ($LASTEXITCODE -eq 0 -and $branch) {
                $bannerParts += "rama: $branch"
            }
        } catch { }

        if ($Evolutivo) { $bannerParts += "evolutivo: $Evolutivo" }
        if ($Modo) { $bannerParts += "modo: $Modo" }

        $line1 = $bannerParts -join " | "

        # Linea 2: complementos
        $compParts = @()
        if ($roslynTool -and $roslynMcp) { $compParts += "Roslyn" }
        if ($context7Mcp) { $compParts += "Context7" }
        if ($dotnetSkills) { $compParts += "dotnet/skills" }
        $missingParts = @()
        if (-not $roslynTool -or -not $roslynMcp) { $missingParts += "Roslyn" }
        if (-not $context7Mcp) { $missingParts += "Context7" }
        if (-not $dotnetSkills) { $missingParts += "dotnet/skills" }

        $line2 = ""
        if ($compParts.Count -gt 0) { $line2 += "MCPs: $($compParts -join ', ')" }
        if ($missingParts.Count -gt 0) {
            if ($line2) { $line2 += " | " }
            $line2 += "Falta: $($missingParts -join ', ')"
        }

        $NL = [char]10
        $bannerText = $line1
        if ($line2) { $bannerText += "${NL}$line2" }
        $bannerText += "${NL}/sos para ayuda rapida"

        # Leer settings, actualizar solo companyAnnouncements, escribir
        $settings = Get-Content $settingsFile -Raw -Encoding UTF8 | ConvertFrom-Json
        $settings.companyAnnouncements = @($bannerText)
        $jsonText = $settings | ConvertTo-Json -Depth 10
        [System.IO.File]::WriteAllText($settingsFile, $jsonText, [System.Text.UTF8Encoding]::new($false))
    }
} catch {
    # Silenciar - no bloquear sesion por error en banner
}

# === CHECK UPDATE DISPONIBLE (silencioso si falla) ===
# v3.8.7: consulta VERSION.json del IIS y cachea en _duran/.update-check.json (TTL 24h)
# El statusline lee el cache (lectura barata) sin volver a consultar la red.
$updateInfo = $null
$cacheFile  = Join-Path (Join-Path $ProjectDir "_duran") ".update-check.json"
$cacheValid = $false

if (Test-Path $cacheFile) {
    try {
        $cache = Get-Content $cacheFile -Raw -Encoding UTF8 | ConvertFrom-Json
        $checkedAt = [DateTime]::Parse($cache.checkedAt)
        $ageHours = ((Get-Date).ToUniversalTime() - $checkedAt).TotalHours

        # Invalidar cache si el localVersion cacheado no coincide con la version actual del consumidor.
        # Esto previene que tras /actualizar, el cache (con localVersion=3.8.6) siga marcando
        # updateAvailable=true cuando el consumidor ya esta en 3.8.7.
        $cacheLocalVer = $null
        if ($cache.PSObject.Properties.Name -contains 'localVersion') {
            $cacheLocalVer = $cache.localVersion
        }
        $cacheLocalStale = ($cacheLocalVer -and $Version -ne "?.?.?" -and $cacheLocalVer -ne $Version)

        if ($ageHours -lt 24 -and -not $cacheLocalStale) {
            $updateInfo = $cache
            $cacheValid = $true
        }
    } catch { }
}

if (-not $cacheValid) {
    try {
        $localVersionFile = Join-Path (Join-Path $ProjectDir "_duran") "VERSION.json"
        $localVer = $null
        $localHash = $null
        $serverUrl = "https://demowww.comillas.edu/claude-stic"

        if (Test-Path $localVersionFile) {
            $lv = Get-Content $localVersionFile -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($lv.installedVersion) { $localVer = $lv.installedVersion }
            if ($lv.PSObject.Properties.Name -contains 'installedZipSha256') {
                $localHash = $lv.installedZipSha256
            }
            if ($lv.PSObject.Properties.Name -contains 'serverUrl' -and $lv.serverUrl) {
                $serverUrl = $lv.serverUrl
            }
        }

        # [DESACTIVADO] Contacto con servidor Comillas deshabilitado (proyecto personal, go-dark).
        # El bloque catch trata esto como "sin conectividad" de forma silenciosa.
        # Linea original eliminada: Invoke-RestMethod a $serverUrl/VERSION.json
        throw "STIC update-check disabled (go-dark)"

        $updateAvailable = $false
        $updateType = "none"
        if ($localVer -and $remote.version) {
            if ($localVer -ne $remote.version) {
                $updateAvailable = $true
                $updateType = "minor"
            } elseif ($localHash -and $remote.zipSha256 -and $localHash -ne $remote.zipSha256) {
                $updateAvailable = $true
                $updateType = "hotfix"
            }
        }

        $updateInfo = [PSCustomObject]@{
            checkedAt        = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
            localVersion     = $localVer
            localZipSha256   = $localHash
            serverVersion    = $remote.version
            serverZipSha256  = $remote.zipSha256
            updateAvailable  = $updateAvailable
            updateType       = $updateType
            serverUrl        = $serverUrl
        }

        # Escribir cache (UTF-8 sin BOM, RFC 8259)
        $cacheJson = $updateInfo | ConvertTo-Json -Depth 5
        [System.IO.File]::WriteAllText($cacheFile, $cacheJson, [System.Text.UTF8Encoding]::new($false))
    } catch {
        # Sin conectividad / IIS no responde / timeout. Silencioso.
        # El cache anterior (si existe pero > 24h) sigue siendo legible por el statusline,
        # se quedara mostrando el ultimo estado conocido.
    }
}

# === EMITIR CONTEXTO A STDOUT (para Claude) ===
$output = @()
$output += "[STIC.IA SessionStart Context]"
$output += "Ecosistema: STIC.IA v$Version (DURAN + ATLAS)"
if ($Proyecto) { $output += "Proyecto: $Proyecto" }
if ($Modo) { $output += "Modo: $Modo" }
if ($Fase) { $output += "Fase: $Fase" }
$output += "Idioma: $Idioma"
if ($Evolutivo) { $output += "Evolutivo activo: $Evolutivo" }
if ($Alertas.Count -gt 0) {
    $output += "ALERTAS:"
    foreach ($alerta in $Alertas) { $output += "  - $alerta" }
}

# Estado de complementos (siempre mostrar)
$roslynStatus = if ($roslynTool -and $roslynMcp) { "instalado" } else { "NO instalado" }
$context7Status = if ($context7Mcp) { "instalado" } else { "NO instalado" }
$dotnetStatus = if ($dotnetSkills) { "instalado" } else { "NO instalado" }
$output += "Complementos:"
$output += "  - MCP Roslyn (analisis semantico C#): $roslynStatus"
$output += "  - MCP Context7 (documentacion librerias): $context7Status"
$output += "  - dotnet/skills (68 skills Microsoft): $dotnetStatus"

$missingCritical = @()
$missingOptional = @()
if (-not $roslynTool -or -not $roslynMcp) { $missingCritical += "MCP Roslyn" }
if (-not $context7Mcp) { $missingCritical += "MCP Context7" }
if (-not $dotnetSkills) { $missingOptional += "dotnet/skills" }

# [DESACTIVADO] Nag de complementos faltantes (apuntaba a arranque.ps1 de Comillas).
# Proyecto personal en go-dark: no sugerir reinstalar context7 ni ejecutar arranque.ps1.

# Update STIC.IA disponible (v3.8.7+)
if ($updateInfo -and $updateInfo.updateAvailable) {
    if ($updateInfo.updateType -eq "minor") {
        $output += "UPDATE STIC.IA DISPONIBLE: v$($updateInfo.serverVersion) (local v$($updateInfo.localVersion)). Sugerir al usuario ejecutar /actualizar."
    } elseif ($updateInfo.updateType -eq "hotfix") {
        $output += "HOTFIX STIC.IA DISPONIBLE: v$($updateInfo.localVersion) (zipSha256 distinto en servidor). Sugerir al usuario ejecutar /actualizar para aplicar retro-fix."
    }
}

Write-Output ($output -join "`n")
