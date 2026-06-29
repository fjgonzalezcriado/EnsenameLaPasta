# multi-rol-coordination.ps1 - Aviso de coordinacion en proyectos colaborativos
# Evento: PostToolUse[Edit]
# Exit 0 (no bloquea) — solo aviso pasivo
# Version: 1.0.0 (Plantilla STIC.IA v3.9.0, item J4 bloque J, ADR-034)
#
# Logica:
#   - Si el proyecto tiene >1 miembro en equipo.miembros[]
#   - Y se edita un archivo en _duran/EVOLUTIVOS/, _duran/ESTADO_PROYECTO.json (campo evolutivos),
#     o se modifica un evolutivo no asignado al usuario actual
#   - Mostrar recordatorio: "Considera notificar a {otros_devs} de los cambios en {evolutivo}"
#
# Identificacion del usuario actual: git config user.name (con fallback a git_author_name del miembro)
#
# Anti-ruido:
#   - Solo dispara si equipo.miembros[].Count >= 2 (proyectos colaborativos)
#   - No dispara si el evolutivo esta sin asignar (asignadoA = null)

param()
. "$PSScriptRoot/hook-helpers.ps1"

$hookData = Get-HookInput
if (-not $hookData) { exit 0 }

$filePath = Get-FilePath $hookData
if (-not $filePath) { exit 0 }

# Solo aplica a archivos de DURAN relacionados con evolutivos
$normalized = ($filePath -replace '\\', '/')
$isEvolutivoFile = $normalized -match '_duran/EVOLUTIVOS/' -or
                   $normalized -match '_duran/ESTADO_PROYECTO\.json$' -or
                   $normalized -match '_duran/FUNCIONALIDADES\.md$'
if (-not $isEvolutivoFile) { exit 0 }

# Localizar ESTADO_PROYECTO.json
$repoRoot = if ($env:CLAUDE_PROJECT_DIR) { $env:CLAUDE_PROJECT_DIR } else { (Get-Location).Path }
$estadoFile = Join-Path $repoRoot '_duran/ESTADO_PROYECTO.json'
if (-not (Test-Path $estadoFile)) { exit 0 }

try {
    $estado = Get-Content $estadoFile -Raw -Encoding UTF8 | ConvertFrom-Json
} catch { exit 0 }

# Verificar proyecto colaborativo (>=2 miembros reales con campo 'usuario')
if (-not $estado.equipo -or -not $estado.equipo.miembros) { exit 0 }
$miembrosReales = @($estado.equipo.miembros | Where-Object { $_.usuario -and $_.usuario -notmatch '^ejemplo_' })
if ($miembrosReales.Count -lt 2) { exit 0 }

# Identificar usuario git actual
$gitUser = $null
try {
    $gitUser = (& git config user.name 2>$null).Trim()
} catch { }
if (-not $gitUser) { exit 0 }

# Resolver usuario git a entrada en equipo.miembros (matching contra git_author_name OR nombre)
$miembroActual = $miembrosReales | Where-Object {
    $_.git_author_name -eq $gitUser -or $_.nombre -eq $gitUser
} | Select-Object -First 1

if (-not $miembroActual) {
    # Usuario git no encontrado en equipo.miembros - posible miembro fantasma
    # No disparar aviso (eso lo cubre duran-coherence-checker R2)
    exit 0
}

$usuarioActualAlias = $miembroActual.usuario

# Leer evolutivos en progreso
if (-not $estado.evolutivos -or -not $estado.evolutivos.enProgreso) { exit 0 }
$evolutivosActivos = @($estado.evolutivos.enProgreso | Where-Object { $_.codigo })

if ($evolutivosActivos.Count -eq 0) { exit 0 }

# Identificar otros miembros (no el actual)
$otrosMiembros = $miembrosReales | Where-Object { $_.usuario -ne $usuarioActualAlias }
if ($otrosMiembros.Count -eq 0) { exit 0 }

# Determinar si la edicion toca un evolutivo no asignado al usuario actual
$evolutivosAjenos = @()
foreach ($ev in $evolutivosActivos) {
    # Si el evolutivo esta sin asignar (asignadoA null), no contar
    if (-not $ev.asignadoA) { continue }
    # Si esta asignado a OTRO miembro
    if ($ev.asignadoA -ne $usuarioActualAlias) {
        $evolutivosAjenos += $ev
    }
}

# Si edita ESTADO_PROYECTO.json o FUNCIONALIDADES.md global: avisar genericamente
# Si edita EVOLUTIVOS/{CODIGO}.md: identificar el evolutivo especifico
$evolutivoEditado = $null
if ($normalized -match '_duran/EVOLUTIVOS/([^/]+)\.md$') {
    $codigoArchivo = $Matches[1]
    $evolutivoEditado = $evolutivosActivos | Where-Object { $_.codigo -eq $codigoArchivo } | Select-Object -First 1
}

# Construir aviso solo si hay caso real de coordinacion
$avisos = @()

if ($evolutivoEditado -and $evolutivoEditado.asignadoA -and $evolutivoEditado.asignadoA -ne $usuarioActualAlias) {
    # Editando evolutivo asignado a otro
    $owner = $evolutivosActivos | Where-Object { $_.codigo -eq $evolutivoEditado.codigo } | Select-Object -First 1
    $ownerMiembro = $miembrosReales | Where-Object { $_.usuario -eq $owner.asignadoA } | Select-Object -First 1
    $ownerNombre = if ($ownerMiembro) { $ownerMiembro.nombre } else { $owner.asignadoA }
    $avisos += "Editaste $($evolutivoEditado.codigo) '$($evolutivoEditado.titulo)' que esta asignado a $ownerNombre ($($owner.asignadoA))."
    $avisos += "  Considera notificar a $ownerNombre por Teams/email de los cambios."
} elseif ($normalized -match '_duran/ESTADO_PROYECTO\.json$' -and $evolutivosAjenos.Count -gt 0) {
    # Editando ESTADO_PROYECTO global con evolutivos ajenos activos
    $avisos += "Editaste _duran/ESTADO_PROYECTO.json. Hay $($evolutivosAjenos.Count) evolutivo(s) activos asignados a otros miembros:"
    foreach ($ev in $evolutivosAjenos) {
        $ownerMiembro = $miembrosReales | Where-Object { $_.usuario -eq $ev.asignadoA } | Select-Object -First 1
        $ownerNombre = if ($ownerMiembro) { $ownerMiembro.nombre } else { $ev.asignadoA }
        $avisos += "  - $($ev.codigo) '$($ev.titulo)' -> $ownerNombre"
    }
    $avisos += "  Si tus cambios afectan alguno, notifica al responsable."
}

if ($avisos.Count -eq 0) { exit 0 }

Write-Output "[multi-rol-coordination] Aviso de coordinacion (proyecto con $($miembrosReales.Count) miembros activos):"
foreach ($a in $avisos) {
    Write-Output $a
}

exit 0
