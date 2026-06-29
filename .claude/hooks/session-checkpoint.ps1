# session-checkpoint.ps1 - Registrar metricas de sesion al cerrar
# Evento: Stop
# Exit 0 = siempre (no bloquea cierre)
# Version: 1.0.1 (STIC.IA v3.8.7)
#   v1.0.1: + GIT_OPTIONAL_LOCKS=0 para evitar colisiones index.lock con VS Code/IDE.
#
# Registra en _duran/METRICAS.json:
# - Fecha y duracion de sesion
# - Skills invocados (detectados por archivos modificados en .claude/skills/)
# - Hooks ejecutados (bloqueos detectados)
# - Archivos modificados
# - Commits realizados

param()

$projectDir = $env:CLAUDE_PROJECT_DIR
if (-not $projectDir) { $projectDir = Get-Location }

$duranDir = Join-Path $projectDir "_duran"
$metricasFile = Join-Path $duranDir "METRICAS.json"

# Crear archivo si no existe
if (-not (Test-Path $metricasFile)) {
    $initial = @{
        version = "1.0.0"
        sesiones = @()
    }
    $initial | ConvertTo-Json -Depth 10 | Set-Content -Path $metricasFile -Encoding UTF8
}

# Leer metricas existentes
try {
    $metricas = Get-Content $metricasFile -Raw -Encoding UTF8 | ConvertFrom-Json
} catch {
    $metricas = @{ version = "1.0.0"; sesiones = @() }
}

# Recopilar datos de la sesion
$ahora = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$fecha = Get-Date -Format "yyyy-MM-dd"

# Read-only git ops: evitar crear lock para no colisionar con IDE/otros hooks
$env:GIT_OPTIONAL_LOCKS = '0'

# Archivos modificados (git)
$archivosModificados = @()
try {
    $gitStatus = git status --porcelain 2>$null
    if ($gitStatus) {
        $archivosModificados = @($gitStatus | ForEach-Object { $_.Substring(3).Trim() })
    }
} catch { }

# Commits de hoy (proxy de actividad)
$commitsHoy = 0
try {
    $commits = git log --oneline --since="$fecha" 2>$null
    if ($commits) {
        $commitsHoy = @($commits).Count
    }
} catch { }

# Detectar skills que pudieron usarse (archivos .md/.template tocados en skills/)
$skillsUsados = @()
try {
    $skillsDir = Join-Path $projectDir ".claude" "skills"
    if (Test-Path $skillsDir) {
        $recentSkills = Get-ChildItem $skillsDir -Directory | Where-Object {
            $_.LastWriteTime -gt (Get-Date).AddHours(-4)
        }
        $skillsUsados = @($recentSkills | ForEach-Object { $_.Name })
    }
} catch { }

# Detectar agents disponibles
$agentsCount = 0
try {
    $agentsDir = Join-Path $projectDir ".claude" "agents"
    if (Test-Path $agentsDir) {
        $agentsCount = @(Get-ChildItem $agentsDir -Filter "*.md").Count
    }
} catch { }

# Detectar estado proyecto
$modo = "desconocido"
$version = "0.0.0"
try {
    $estadoFile = Join-Path $duranDir "ESTADO_PROYECTO.json"
    if (Test-Path $estadoFile) {
        $estado = Get-Content $estadoFile -Raw -Encoding UTF8 | ConvertFrom-Json
        $modo = $estado.estado.modo
        $version = $estado.estado.version_actual
    }
} catch { }

# Construir registro de sesion
$sesion = @{
    fecha = $ahora
    archivosModificados = $archivosModificados.Count
    archivos = if ($archivosModificados.Count -le 20) { $archivosModificados } else { $archivosModificados[0..19] }
    commitsHoy = $commitsHoy
    skillsTocados = $skillsUsados
    agentsDisponibles = $agentsCount
    modoProyecto = $modo
    versionProyecto = $version
}

# Añadir sesion (mantener ultimas 50)
if (-not $metricas.sesiones) {
    $metricas | Add-Member -NotePropertyName "sesiones" -NotePropertyValue @() -Force
}

$sesiones = @($metricas.sesiones) + @($sesion)
if ($sesiones.Count -gt 50) {
    $sesiones = $sesiones[($sesiones.Count - 50)..($sesiones.Count - 1)]
}
$metricas.sesiones = $sesiones

# Guardar
try {
    $metricas | ConvertTo-Json -Depth 10 | Set-Content -Path $metricasFile -Encoding UTF8
} catch { }

exit 0
