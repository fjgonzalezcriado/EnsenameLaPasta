# duran-freshness.ps1 - Aviso pasivo sobre frescura de DURAN
# Evento: UserPromptSubmit
# Exit 0 = continuar (stdout se inyecta como contexto adicional)
# NO bloquea jamas.
# Version: 1.0.0 (Plantilla STIC.IA v3.9.0, item H1 bloque H, ADR-033)
#
# Logica:
#   - Si _duran/ESTADO_PROYECTO.json.ultimaSesion.fecha es > N dias atras, avisar
#   - Si hay evolutivos.enProgreso[] con fechaInicio > M dias y sin actualizar, avisar
#   - Umbrales configurables via configuracion.duranFreshnessDiasSesion y .duranFreshnessDiasEvolutivo
#     (defaults: 7 dias sesion, 5 dias evolutivo)
#
# Anti-ruido: prompts <10 chars no disparan.

param()
. "$PSScriptRoot/hook-helpers.ps1"

$hookData = Get-HookInput
if (-not $hookData) { exit 0 }

# Anti-ruido: prompt trivial
$prompt = $null
if ($hookData.prompt -is [string]) { $prompt = $hookData.prompt }
elseif ($hookData.prompt) { $prompt = ($hookData.prompt | Out-String).Trim() }
if (-not $prompt -or $prompt.Length -lt 10) { exit 0 }

# Localizar ESTADO_PROYECTO.json
$repoRoot = if ($env:CLAUDE_PROJECT_DIR) { $env:CLAUDE_PROJECT_DIR } else { (Get-Location).Path }
$estadoFile = Join-Path $repoRoot '_duran/ESTADO_PROYECTO.json'
if (-not (Test-Path $estadoFile)) { exit 0 }  # No es proyecto STIC.IA o aun no onboarded

try {
    $estado = Get-Content $estadoFile -Raw -Encoding UTF8 | ConvertFrom-Json
} catch { exit 0 }  # JSON corrupto - no bloquear

# Umbrales (con defaults)
$umbralSesionDias = 7
$umbralEvolutivoDias = 5
if ($estado.configuracion) {
    if ($estado.configuracion.duranFreshnessDiasSesion) {
        $umbralSesionDias = [int]$estado.configuracion.duranFreshnessDiasSesion
    }
    if ($estado.configuracion.duranFreshnessDiasEvolutivo) {
        $umbralEvolutivoDias = [int]$estado.configuracion.duranFreshnessDiasEvolutivo
    }
}

$ahora = Get-Date
$avisos = @()

# Check 1: ultimaSesion.fecha
if ($estado.ultimaSesion -and $estado.ultimaSesion.fecha) {
    try {
        $fechaSesion = [DateTime]::Parse($estado.ultimaSesion.fecha)
        $diasDesde = [int]($ahora - $fechaSesion).TotalDays
        if ($diasDesde -gt $umbralSesionDias) {
            $usuario = if ($estado.ultimaSesion.usuario) { $estado.ultimaSesion.usuario } else { 'desconocido' }
            $avisos += "Ultima sesion DURAN hace $diasDesde dias (>$umbralSesionDias). Usuario: $usuario. Considera /continuar o /sesion para refrescar contexto."
        }
    } catch { }  # Fecha mal parseada - skip
}

# Check 2: evolutivos en progreso sin actualizar
if ($estado.evolutivos -and $estado.evolutivos.enProgreso -and $estado.evolutivos.enProgreso.Count -gt 0) {
    foreach ($ev in $estado.evolutivos.enProgreso) {
        # Solo procesar entradas reales (no comentarios/estructura)
        if (-not $ev.codigo) { continue }

        $fechaRef = $null
        # Preferir fechaInicio, fallback fechaCreacion
        if ($ev.fechaInicio) {
            try { $fechaRef = [DateTime]::Parse($ev.fechaInicio) } catch { }
        }
        if (-not $fechaRef -and $ev.fechaCreacion) {
            try { $fechaRef = [DateTime]::Parse($ev.fechaCreacion) } catch { }
        }
        if (-not $fechaRef) { continue }

        $diasDesde = [int]($ahora - $fechaRef).TotalDays
        if ($diasDesde -gt $umbralEvolutivoDias) {
            $titulo = if ($ev.titulo) { $ev.titulo } else { '(sin titulo)' }
            $avisos += "Evolutivo $($ev.codigo) '$titulo' lleva $diasDesde dias en progreso (>$umbralEvolutivoDias). Considera actualizar estado o pausar."
        }
    }
}

if ($avisos.Count -eq 0) { exit 0 }

# Construir aviso (stdout -> contexto Claude)
Write-Output "[duran-freshness] DURAN puede estar desfasada:"
foreach ($a in $avisos) {
    Write-Output "  - $a"
}
Write-Output "Umbrales actuales: sesion=$umbralSesionDias dias, evolutivo=$umbralEvolutivoDias dias (configurables en _duran/ESTADO_PROYECTO.json.configuracion.duranFreshness*)."

exit 0
