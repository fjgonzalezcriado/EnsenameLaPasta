# statusline.ps1 - StatusLine personalizado Claude Code
# Formato (con ANSI colors):  [proyecto]  git:rama[*]  STIC.IA vX.Y.Z  @ modelo
#
# Nota: PS 5.1 + Unicode emojis por stdout fallan silenciosamente en captura
#       de Claude Code. Usamos ASCII + ANSI escape codes (soportados en
#       terminales modernos: Windows Terminal, VS Code, ConEmu, etc.)

$ErrorActionPreference = "SilentlyContinue"

# --- ANSI color codes ------------------------------------------------------
$ESC = [char]27
$RESET   = "$ESC[0m"
$DIM     = "$ESC[2m"      # Gris tenue (labels / separadores)
$CYAN    = "$ESC[96m"     # Cyan brillante (nombre proyecto)
$GREEN   = "$ESC[32m"     # Verde (rama git limpia)
$YELLOW  = "$ESC[33m"     # Amarillo (dirty flag *)
$MAGENTA = "$ESC[95m"     # Magenta brillante (version STIC.IA)
$BLUE    = "$ESC[94m"     # Azul brillante (modelo Claude)

# --- Leer input ------------------------------------------------------------
$raw = ($input | Out-String).Trim()
$cwd = $null
$modelDisplay = $null

if ($raw) {
    try {
        $data = $raw | ConvertFrom-Json
        $cwd = $data.cwd
        if ($data.model) {
            $modelDisplay = $data.model.display_name
            if (-not $modelDisplay) { $modelDisplay = $data.model.id }
        }
    } catch { }
}

if (-not $cwd) { $cwd = (Get-Location).Path }

# --- Proyecto --------------------------------------------------------------
$folder = Split-Path $cwd -Leaf
$projInfo = "${DIM}[${RESET}${CYAN}${folder}${RESET}${DIM}]${RESET}"

# --- Git branch + dirty flag ----------------------------------------------
$gitInfo = ""
$gitDir = Join-Path $cwd ".git"
if (Test-Path $gitDir) {
    $headFile = Join-Path $gitDir "HEAD"
    if (Test-Path $headFile) {
        $head = (Get-Content $headFile -Raw).Trim()
        $branch = $null
        if ($head -match '^ref:\s+refs/heads/(.+)$') {
            $branch = $Matches[1]
        } elseif ($head.Length -ge 7) {
            $branch = $head.Substring(0, 7)
        }

        if ($branch) {
            $dirtyFlag = ""
            try {
                $porcelain = & git -C $cwd status --porcelain=v1 2>$null | Select-Object -First 1
                if ($porcelain) { $dirtyFlag = "${YELLOW}*${RESET}" }
            } catch { }
            $gitInfo = "  ${DIM}git:${RESET}${GREEN}${branch}${RESET}${dirtyFlag}"
        }
    }
}

# --- STIC.IA version -------------------------------------------------------
$sticVersion = $null

$duranPath = Join-Path $cwd "_duran\VERSION.json"
if (Test-Path $duranPath) {
    try {
        $v = Get-Content $duranPath -Raw | ConvertFrom-Json
        if ($v.installedVersion) { $sticVersion = $v.installedVersion }
    } catch { }
}

if (-not $sticVersion) {
    $estadoPath = Join-Path $cwd "_estado\VERSION.json"
    if (Test-Path $estadoPath) {
        try {
            $v = Get-Content $estadoPath -Raw | ConvertFrom-Json
            if ($v.version -and $v.version.actual) { $sticVersion = $v.version.actual }
        } catch { }
    }
}

$sticInfo = ""
$sticHash = $null
if ($sticVersion) {
    # STIC.IA en magenta bold (resalta como marca); version en magenta normal
    $BOLD = "$ESC[1m"
    $sticInfo = "  ${BOLD}${MAGENTA}STIC.IA${RESET} ${MAGENTA}v${sticVersion}${RESET}"

    # Capturar hash local actual para comparacion con cache de update-check
    if (Test-Path $duranPath) {
        try {
            $v = Get-Content $duranPath -Raw | ConvertFrom-Json
            if ($v.PSObject.Properties.Name -contains 'installedZipSha256') {
                $sticHash = $v.installedZipSha256
            }
        } catch { }
    }

    # v3.8.7: indicador de update disponible (lee cache de banner.ps1)
    # _duran/.update-check.json es escrito por banner.ps1 al inicio de sesion.
    # Defensa extra: comparar el server cacheado contra la version/hash actual del consumidor.
    # Si tras /actualizar el cache no se ha refrescado todavia, evitamos mostrar un falso positivo.
    $updateCacheFile = Join-Path $cwd "_duran\.update-check.json"
    if (Test-Path $updateCacheFile) {
        try {
            $uc = Get-Content $updateCacheFile -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($uc.updateAvailable) {
                $UPD_COLOR = "$ESC[93m"  # Amarillo brillante (llamar atencion)
                if ($uc.updateType -eq "minor") {
                    # Solo si el server tiene una version distinta a la instalada actual
                    if ($uc.serverVersion -and $uc.serverVersion -ne $sticVersion) {
                        $sticInfo += " ${BOLD}${UPD_COLOR}^v$($uc.serverVersion)${RESET}"
                    }
                } elseif ($uc.updateType -eq "hotfix") {
                    # Solo si el hash del server difiere del local actual
                    if ($uc.serverZipSha256 -and $sticHash -and $uc.serverZipSha256 -ne $sticHash) {
                        $sticInfo += " ${BOLD}${UPD_COLOR}^hotfix${RESET}"
                    }
                }
            }
        } catch { }
    }
}

# --- Modelo ----------------------------------------------------------------
$modelInfo = ""
if ($modelDisplay) {
    $modelInfo = "  ${DIM}@${RESET} ${BLUE}${modelDisplay}${RESET}"
}

# --- Output ----------------------------------------------------------------
$line = "${projInfo}${gitInfo}${sticInfo}${modelInfo}"
[Console]::Out.Write($line)
