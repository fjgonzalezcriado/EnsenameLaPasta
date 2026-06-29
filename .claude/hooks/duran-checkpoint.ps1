# duran-checkpoint.ps1 - Actualizar DURAN y verificar cambios sin commit
# Evento: Stop
# Exit 0 = siempre (no bloquea cierre)
# Version: 1.0.1 (STIC.IA v3.8.7)
#   v1.0.1: + GIT_OPTIONAL_LOCKS=0 para evitar colisiones index.lock con VS Code/IDE.

param()

$projectDir = $env:CLAUDE_PROJECT_DIR
if (-not $projectDir) { $projectDir = Get-Location }

$duranDir = Join-Path $projectDir "_duran"
if (-not (Test-Path $duranDir)) { exit 0 }

# Read-only git ops: evitar crear lock para no colisionar con IDE/otros hooks
$env:GIT_OPTIONAL_LOCKS = '0'

# === 1. Verificar cambios sin commit ===
$uncommitted = @()
try {
    $status = git status --porcelain 2>$null
    if ($status) {
        $uncommitted = @($status | ForEach-Object { $_.Substring(3).Trim() })
    }
} catch { }

if ($uncommitted.Count -gt 0) {
    Write-Host ""
    Write-Host "AVISO: $($uncommitted.Count) archivo(s) sin commit:"
    $uncommitted | Select-Object -First 10 | ForEach-Object { Write-Host "  - $_" }
    if ($uncommitted.Count -gt 10) {
        Write-Host "  ... y $($uncommitted.Count - 10) mas"
    }
    Write-Host "Considera ejecutar /commit antes de cerrar."
    Write-Host ""
}

# === 2. Actualizar timestamp en ESTADO_PROYECTO.json ===
$estadoFile = Join-Path $duranDir "ESTADO_PROYECTO.json"
if (Test-Path $estadoFile) {
    try {
        $estado = Get-Content $estadoFile -Raw -Encoding UTF8 | ConvertFrom-Json
        $ahora = Get-Date -Format "yyyy-MM-dd"
        $usuario = $null

        try { $usuario = git config user.name 2>$null } catch { }
        if (-not $usuario) { $usuario = $env:USERNAME }

        # Actualizar ultimaSesion
        if ($estado.ultimaSesion) {
            $estado.ultimaSesion.fecha = $ahora
            $estado.ultimaSesion.usuario = $usuario
            if ($uncommitted.Count -gt 0) {
                $estado.ultimaSesion.archivosModificados = @($uncommitted | Select-Object -First 20)
            }
        }

        # Actualizar estado.ultima_actualizacion
        if ($estado.estado) {
            $estado.estado.ultima_actualizacion = $ahora
            $estado.estado.actualizado_por = $usuario
        }

        $estado | ConvertTo-Json -Depth 10 | Set-Content -Path $estadoFile -Encoding UTF8
    } catch {
        # Silencioso - no bloquear cierre por error de escritura
    }
}

# === 3. Verificar si hay alertas pendientes ===
if (Test-Path $estadoFile) {
    try {
        $estado = Get-Content $estadoFile -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($estado.alertas -and $estado.alertas.activas -and $estado.alertas.activas.Count -gt 0) {
            Write-Host "RECORDATORIO: $($estado.alertas.activas.Count) alerta(s) activa(s) en el proyecto."
        }
    } catch { }
}

exit 0
