# mcp-sync.ps1
# Script estable que invoca el comando /mcp-sync.
# Sincroniza 6 categorias del proyecto con el MCP Server STIC.IA v2.
#
# Origen: Hotfix #8 v3.9.0 (ADR-035/036) - evita que cada ejecucion de /mcp-sync
# pida a Claude generar codigo ad-hoc (frágil, con bugs de parse).
#
# Uso:
#   pwsh .claude/scripts/mcp-sync.ps1                  # todas las categorias habilitadas
#   pwsh .claude/scripts/mcp-sync.ps1 -DryRun          # muestra que se enviaria
#   pwsh .claude/scripts/mcp-sync.ps1 -Categoria nugets  # solo una

#Requires -Version 5.1

[CmdletBinding()]
param(
    [switch]$DryRun,
    [ValidateSet('all', 'decisiones', 'lecciones', 'nugets', 'deuda', 'evolutivos', 'equipo')]
    [string]$Categoria = 'all'
)

$ErrorActionPreference = 'Stop'

# ---------- Pre-checks ----------

$credsPath = '_duran/.mcp-credentials.json'
$estadoPath = '_duran/ESTADO_PROYECTO.json'

if (-not (Test-Path $credsPath)) {
    Write-Host "ERROR: $credsPath no existe. Ejecuta /mcp-register primero." -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $estadoPath)) {
    Write-Host "ERROR: $estadoPath no existe. Ejecuta /onboarding primero." -ForegroundColor Red
    exit 1
}

$creds = Get-Content $credsPath -Raw | ConvertFrom-Json
$estado = Get-Content $estadoPath -Raw | ConvertFrom-Json

if (-not $estado.mcpSync -or -not $estado.mcpSync.habilitado) {
    Write-Host "ERROR: mcpSync.habilitado = false. Ejecuta /mcp-register para habilitar." -ForegroundColor Red
    exit 1
}

$serverUrl = if ($creds.serverUrl) { $creds.serverUrl } else { 'https://demowww.comillas.edu/claude-stic/mcp' }
$projectId = $creds.projectId
$apiKey = $creds.apiKey
$headers = @{ 'X-Stic-Api-Key' = $apiKey }
$categorias = $estado.mcpSync.categorias

function ShouldSync($name) {
    if ($Categoria -ne 'all') {
        return $Categoria -eq $name
    }
    # Si la categoria existe en mcpSync.categorias, respetar el valor.
    # Si NO existe (categoria nueva post-registro), defaultear a TRUE - asi nuevas
    # categorias funcionan sin obligar a re-ejecutar /mcp-register en cada hotfix.
    if ($null -eq $categorias) { return $true }
    $prop = $categorias.PSObject.Properties[$name]
    if ($null -eq $prop) { return $true }
    return $prop.Value -eq $true
}

# ---------- Parsers ----------

function Parse-Decisiones {
    if (-not (Test-Path '_duran/DECISIONES.md')) { return @() }
    $content = Get-Content '_duran/DECISIONES.md' -Raw
    $items = @()
    # Patron: ## ADR-XXX: Titulo  (o ### D1: ...)
    $pattern = '(?ms)^##+\s+(ADR-\d+|D\d+)[:\s]+([^\r\n]+).*?(?=^##+\s+(?:ADR-|D\d+)|\Z)'
    foreach ($m in [regex]::Matches($content, $pattern)) {
        $codigo = $m.Groups[1].Value.Trim()
        $titulo = $m.Groups[2].Value.Trim()
        $body = $m.Value

        # Skip placeholders del template (ej. "[Titulo de la Decision]", "[YYYY-MM-DD]").
        # Heuristica: si el titulo es exactamente "[...]" o contiene solo bracket-text, ignorar.
        if ($titulo -match '^\[.+\]$') { continue }

        $estado_ = $null
        if ($body -match '\*\*Estado\*\*\s*:\s*(?:[\p{So}\p{S}]\s*)?([A-Za-z]+)') { $estado_ = $matches[1] }
        # Skip si el "estado" es tambien placeholder tipo "[Aceptada/Deprecada/...]"
        if ($estado_ -match '^\[') { $estado_ = $null }
        $fecha = $null
        if ($body -match '\*\*Fecha\*\*\s*:\s*(\d{4}-\d{2}-\d{2})') { $fecha = $matches[1] }
        $tags = $null
        if ($body -match '\*\*Tags\*\*\s*:\s*([^\r\n]+)') {
            $t = $matches[1].Trim()
            if ($t -notmatch '^\[') { $tags = $t }
        }

        $items += @{
            codigo = $codigo
            titulo = $titulo
            estado = $estado_
            fechaDecision = $fecha
            tags = $tags
        }
    }
    return $items
}

function Parse-Lecciones {
    if (-not (Test-Path '_duran/LECCIONES.md')) { return @() }
    $content = Get-Content '_duran/LECCIONES.md' -Raw
    $items = @()
    $pattern = '(?ms)^###\s+([^\r\n]+)(.*?)(?=^###\s+|\Z)'
    foreach ($m in [regex]::Matches($content, $pattern)) {
        $titulo = $m.Groups[1].Value.Trim()
        $body = $m.Groups[2].Value

        # Skip placeholders del template (ej. "PAT-001: [Nombre del patron]").
        if ($titulo -match '\[.+\]') { continue }

        # Skip headers estructurales del template (no son lecciones reales).
        # Una leccion real SIEMPRE tiene al menos un campo etiquetado **Categoria/Severidad/...**: en body.
        # Si el body NO tiene NINGUNO de esos campos -> es un H3 estructural (ej. "Automaticamente", "Manualmente").
        if ($body -notmatch '(?im)^\s*\*?\*?(Categoria|Severidad|Fecha|Descripcion|Patron|Aplicacion|Impacto|Solucion|Workaround|Contexto)\*?\*?\s*:') {
            continue
        }

        $categoria_ = $null
        if ($body -match '(?im)^\s*\*?\*?Categoria\*?\*?\s*:\s*([^\r\n]+)') {
            $c = $matches[1].Trim()
            if ($c -notmatch '^\[') { $categoria_ = $c }
        }
        $severidad = $null
        if ($body -match '(?im)^\s*\*?\*?Severidad\*?\*?\s*:\s*(alta|media|baja)') { $severidad = $matches[1].ToLower() }
        $fecha = $null
        if ($body -match '(?im)^\s*\*?\*?Fecha\*?\*?\s*:\s*(\d{4}-\d{2}-\d{2})') { $fecha = $matches[1] }

        $descripcion = ($body -replace '(?im)^\s*\*?\*?(Categoria|Severidad|Fecha)\*?\*?\s*:\s*[^\r\n]+\r?\n', '').Trim()
        if ($descripcion.Length -gt 4000) { $descripcion = $descripcion.Substring(0, 4000) + '...' }

        $items += @{
            titulo = $titulo
            categoria = $categoria_
            descripcion = $descripcion
            severidad = $severidad
            fechaAprendizaje = $fecha
        }
    }
    return $items
}

function Parse-Nugets {
    $items = @{}  # packageId -> @{packageId, version, isCpm, esVulnerable}

    # 1. Directory.Packages.props (CPM)
    foreach ($props in (Get-ChildItem -Recurse -Filter 'Directory.Packages.props' -ErrorAction SilentlyContinue)) {
        try {
            [xml]$x = Get-Content $props.FullName
            # Aplanar todos los <ItemGroup>: cuando hay varios, $x.Project.ItemGroup es array
            # y necesitamos iterar cada uno explicitamente.
            $itemGroups = @($x.Project.ItemGroup)
            foreach ($ig in $itemGroups) {
                if (-not $ig) { continue }
                $pvs = @($ig.PackageVersion)
                foreach ($pv in $pvs) {
                    if (-not $pv -or -not $pv.Include) { continue }
                    $id = $pv.Include
                    $ver = $pv.Version
                    if ($id -and $ver) {
                        $items[$id] = @{packageId = $id; version = $ver; isCpm = $true; esVulnerable = $false}
                    }
                }
            }
        } catch {}
    }

    # 2. *.csproj (PackageReference no-CPM)
    foreach ($csproj in (Get-ChildItem -Recurse -Filter '*.csproj' -ErrorAction SilentlyContinue)) {
        try {
            [xml]$x = Get-Content $csproj.FullName
            $itemGroups = @($x.Project.ItemGroup)
            foreach ($ig in $itemGroups) {
                if (-not $ig) { continue }
                $refs = @($ig.PackageReference)
                foreach ($r in $refs) {
                    if (-not $r -or -not $r.Include) { continue }
                    $id = $r.Include
                    $ver = if ($r.Version) { $r.Version } elseif ($items[$id]) { $items[$id].version } else { $null }
                    if (-not $ver) { continue }
                    if (-not $items.ContainsKey($id)) {
                        $items[$id] = @{packageId = $id; version = $ver; isCpm = $false; esVulnerable = $false}
                    }
                }
            }
        } catch {}
    }

    return @($items.Values)
}

function Parse-Deuda {
    if (-not (Test-Path '_duran/DEUDA_TECNICA.md')) { return @() }
    $content = Get-Content '_duran/DEUDA_TECNICA.md' -Raw
    $items = @()

    # Helper: normalizar severidad libre -> schema server (alta|media|baja|null)
    function Normalize-Severidad($s) {
        if (-not $s) { return $null }
        $low = $s.ToLower()
        if ($low -match 'crit|alta|alto|high') { return 'alta' }
        if ($low -match 'media|medium|moderada?') { return 'media' }
        if ($low -match 'baja|low|menor') { return 'baja' }
        return $null
    }
    # Helper: normalizar estado libre -> schema server
    function Normalize-Estado($s) {
        if (-not $s) { return 'pendiente' }
        $low = $s.ToLower()
        if ($low -match 'progreso|wip|en_curso') { return 'en_progreso' }
        if ($low -match 'resuelt|cerrad|fix|done') { return 'resuelta' }
        if ($low -match 'descart|cancel|reject') { return 'descartada' }
        # 'aceptada' / 'pendiente' / 'identificada' -> pendiente (deuda activa)
        return 'pendiente'
    }

    # Patron PRIMARIO: bloque '### DT-XXX: Titulo' con campos **Categoria/Criticidad/Estado**.
    # Es el formato real usado por proyectos Comillas (auto-generado por /nuevo-deuda).
    $pattern = '(?ms)^###\s+(DT-\d+|TD-\d+)[:\s]+([^\r\n]+)(.*?)(?=^###\s+(?:DT-|TD-)|\Z)'
    foreach ($m in [regex]::Matches($content, $pattern)) {
        $codigo = $m.Groups[1].Value.Trim()
        $titulo = $m.Groups[2].Value.Trim()
        # Skip placeholders del template (ej. "[Titulo]")
        if ($titulo -match '^\[.+\]$') { continue }
        $body = $m.Groups[3].Value

        $severidad = $null
        # Probar Criticidad primero (estandar Comillas), luego Severidad
        if ($body -match '(?im)^\s*\*?\*?Criticidad\*?\*?\s*:\s*([^\r\n]+)') {
            $severidad = Normalize-Severidad $matches[1].Trim()
        } elseif ($body -match '(?im)^\s*\*?\*?Severidad\*?\*?\s*:\s*([^\r\n]+)') {
            $severidad = Normalize-Severidad $matches[1].Trim()
        }

        $estado = 'pendiente'
        if ($body -match '(?im)^\s*\*?\*?Estado\*?\*?\s*:\s*([^\r\n]+)') {
            $estado = Normalize-Estado $matches[1].Trim()
        }

        $items += @{
            codigo = $codigo
            titulo = $titulo
            severidad = $severidad
            estado = $estado
        }
    }

    # Si el patron primario no encontro nada, fallback al patron LEGACY tabla.
    if ($items.Count -eq 0) {
        foreach ($line in ($content -split "`n")) {
            if ($line -match '^\|?\s*(DT-\d+|TD-\d+)\s*\|\s*([^|]+?)\s*\|\s*([^|]*?)\s*\|\s*([^|]*?)\s*\|') {
                $codigo = $matches[1]
                $titulo = $matches[2].Trim()
                if ($titulo -match '^\[.+\]$') { continue }
                $items += @{
                    codigo = $codigo
                    titulo = $titulo
                    severidad = Normalize-Severidad $matches[3].Trim()
                    estado = Normalize-Estado $matches[4].Trim()
                }
            }
        }
    }
    return $items
}

function Parse-Evolutivos {
    $items = @()
    $bloqueadores = @()
    if (-not $estado.evolutivos) { return @{evolutivos = @(); bloqueadores = @()} }

    # Iteracion por bucket: el ESTADO se infiere del bucket (pendientes/enProgreso/completados)
    # cuando el campo no esta explicito en el evolutivo - estandar STIC.IA v3.7+.
    $buckets = @(
        @{ lista = $estado.evolutivos.pendientes;  estadoDefault = 'pendiente' },
        @{ lista = $estado.evolutivos.enProgreso;  estadoDefault = 'en_progreso' },
        @{ lista = $estado.evolutivos.completados; estadoDefault = 'completado' }
    )

    foreach ($b in $buckets) {
        if (-not $b.lista) { continue }
        foreach ($e in $b.lista) {
            if (-not $e -or -not $e.codigo) { continue }
            # Titulo: soporta 'titulo' o 'nombre' (esquemas distintos en el wild).
            # Si ambos vacios, derivar del codigo para nunca enviar null al server (SP requiere).
            $titulo = if ($e.titulo) { $e.titulo } elseif ($e.nombre) { $e.nombre } else { $e.codigo }
            $estadoFinal = if ($e.estado) { $e.estado } else { $b.estadoDefault }
            $items += @{
                codigo = $e.codigo
                titulo = $titulo
                estado = $estadoFinal
                asignadoA = $e.asignadoA
                prioridad = $e.prioridad
                fechaCreacion = $e.fechaCreacion
                fechaInicio = $e.fechaInicio
                fechaEstimada = $e.fechaEstimada
                fechaCompletado = $e.fechaCompletado
            }
        }
    }

    if ($estado.desarrollo -and $estado.desarrollo.bloqueadores) {
        foreach ($b in $estado.desarrollo.bloqueadores) {
            if (-not $b) { continue }
            $desc = if ($b -is [string]) { $b } else { $b.descripcion }
            if (-not $desc) { continue }
            $bloqueadores += @{
                descripcion = $desc
                estaResuelto = $false
            }
        }
    }

    return @{evolutivos = @($items); bloqueadores = @($bloqueadores)}
}

function Parse-Equipo {
    $items = @()
    if ($estado.equipo -and $estado.equipo.miembros) {
        foreach ($m in $estado.equipo.miembros) {
            if (-not $m -or -not $m.usuario -or $m.usuario.StartsWith('ejemplo_')) { continue }
            $roles = if ($m.roles) {
                if ($m.roles -is [array]) { $m.roles -join ',' } else { $m.roles }
            } elseif ($m.rol) { $m.rol } else { $null }
            $items += @{
                usuario = $m.usuario
                nombreCompleto = $m.nombre
                roles = $roles
                esActivo = $true
            }
        }
    }
    return $items
}

function Parse-Branching {
    # Devuelve siempre un hashtable (nunca $null) — el server rechaza branching=null
    # con HTTP 400. Si no hay config, se envia un hashtable con campos en ''.
    $empty = @{ estrategia = ''; mergeStrategy = ''; convencionRamas = ''; ramaBase = '' }
    if (-not $estado.configuracion) { return $empty }

    # 1. Nuevo esquema canonico: configuracion.branching.*
    if ($estado.configuracion.branching -and $estado.configuracion.branching.estrategia) {
        $b = $estado.configuracion.branching
        return @{
            estrategia = if ($b.estrategia) { $b.estrategia } else { '' }
            mergeStrategy = if ($b.mergeStrategy) { $b.mergeStrategy } else { '' }
            convencionRamas = if ($b.convencionRamas) { $b.convencionRamas } else { '' }
            ramaBase = if ($b.ramaBase) { $b.ramaBase } else { '' }
        }
    }

    # 2. Esquema legacy plano: configuracion.{convencionRamas,ramaBase}
    if ($estado.configuracion.convencionRamas -or $estado.configuracion.ramaBase) {
        return @{
            estrategia = ''
            mergeStrategy = ''
            convencionRamas = if ($estado.configuracion.convencionRamas) { $estado.configuracion.convencionRamas } else { '' }
            ramaBase = if ($estado.configuracion.ramaBase) { $estado.configuracion.ramaBase } else { '' }
        }
    }

    return $empty
}

# ---------- Recolectar ----------

Write-Host "Recolectando datos del proyecto..." -ForegroundColor Cyan
# @(...) en el lado del call-site garantiza array (defensa contra PS unwrap de single-element).
$decisiones = @(if (ShouldSync 'decisiones') { Parse-Decisiones })
$lecciones  = @(if (ShouldSync 'lecciones')  { Parse-Lecciones })
$nugets     = @(if (ShouldSync 'nugets')     { Parse-Nugets })
$deuda      = @(if (ShouldSync 'deuda')      { Parse-Deuda })
$evolBloq   = if (ShouldSync 'evolutivos') { Parse-Evolutivos } else { @{evolutivos = @(); bloqueadores = @()} }
$equipo     = @(if (ShouldSync 'equipo')     { Parse-Equipo })
# Branching siempre se envia (es un hashtable, no un array). Parse-Branching nunca devuelve $null.
$branching  = if (ShouldSync 'equipo') { Parse-Branching } else { @{estrategia=''; mergeStrategy=''; convencionRamas=''; ramaBase=''} }

# ---------- Resumen / dry-run ----------

Write-Host ""
Write-Host "Listo para sincronizar:" -ForegroundColor Cyan
Write-Host "  decisiones : $($decisiones.Count)"
Write-Host "  lecciones  : $($lecciones.Count)"
Write-Host "  nugets     : $($nugets.Count)"
Write-Host "  deuda      : $($deuda.Count)"
Write-Host "  evolutivos : $($evolBloq.evolutivos.Count) + $($evolBloq.bloqueadores.Count) bloqueadores"
Write-Host "  equipo     : $($equipo.Count)"
Write-Host "  branching  : $(if ($branching.estrategia) { $branching.estrategia } elseif ($branching.ramaBase) { "(legacy ramaBase=$($branching.ramaBase))" } else { '(no config)' })"
Write-Host ""
Write-Host "Destino: $serverUrl"
Write-Host "ProjectId: $projectId"
Write-Host ""

if ($DryRun) {
    Write-Host "Dry-run — no se envia nada." -ForegroundColor Yellow
    exit 0
}

# ---------- Enviar ----------

$totalErrors = 0

function SendPost($url, $body, $label) {
    try {
        $resp = Invoke-RestMethod -Uri $url -Method Post -Headers $headers `
            -Body ($body | ConvertTo-Json -Depth 6 -Compress) `
            -ContentType 'application/json' -TimeoutSec 60
        Write-Host "  [OK] $label" -ForegroundColor Green
        return $resp
    } catch {
        $status = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        # Intentar leer el body de la respuesta de error (utiles para 4xx con detalle)
        $errBody = ''
        try {
            if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
                $errBody = $_.ErrorDetails.Message
            } elseif ($_.Exception.Response) {
                $stream = $_.Exception.Response.GetResponseStream()
                if ($stream) {
                    $reader = New-Object System.IO.StreamReader($stream)
                    $errBody = $reader.ReadToEnd()
                }
            }
        } catch {}
        $shortBody = if ($errBody.Length -gt 300) { $errBody.Substring(0, 300) + '...' } else { $errBody }
        Write-Host "  [FAIL] $label (HTTP ${status})" -ForegroundColor Red
        if ($shortBody) { Write-Host "         Server: $shortBody" -ForegroundColor DarkRed }
        $script:totalErrors++
        return $null
    }
}

# Decisiones + lecciones via /v2/duran/batch
if (($decisiones.Count + $lecciones.Count) -gt 0) {
    Write-Host "Enviando DURAN (decisiones + lecciones)..." -ForegroundColor Cyan
    # @(...) garantiza array JSON aunque haya 1 solo elemento (PS unwrap defense).
    $body = @{
        projectId = $projectId
        decisiones = @($decisiones)
        lecciones = @($lecciones)
    }
    SendPost "$serverUrl/v2/duran/batch" $body "duran/batch ($($decisiones.Count) decisiones + $($lecciones.Count) lecciones)" | Out-Null
}

# Nugets + deuda + evolutivos + equipo + branching via /v2/sync/f3-batch
$hasBranching = $branching.estrategia -or $branching.ramaBase
$f3Items = $nugets.Count + $deuda.Count + $evolBloq.evolutivos.Count + $evolBloq.bloqueadores.Count + $equipo.Count
if ($f3Items -gt 0 -or $hasBranching) {
    Write-Host "Enviando F3 (nugets + deuda + evolutivos + equipo + branching)..." -ForegroundColor Cyan
    $body = @{
        projectId = $projectId
        nugets = @($nugets)
        deuda = @($deuda)
        evolutivos = @($evolBloq.evolutivos)
        bloqueadores = @($evolBloq.bloqueadores)
        equipo = @($equipo)
        branching = $branching
    }
    $f3Label = "f3-batch (N=$($nugets.Count) D=$($deuda.Count) E=$($evolBloq.evolutivos.Count) B=$($evolBloq.bloqueadores.Count) Eq=$($equipo.Count) Br=$(if ($hasBranching) { 1 } else { 0 }))"
    SendPost "$serverUrl/v2/sync/f3-batch" $body $f3Label | Out-Null
}

# ---------- Actualizar ultimaSync ----------

if ($totalErrors -eq 0) {
    $estado.mcpSync.ultimaSync = (Get-Date).ToString('o')
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText((Resolve-Path $estadoPath), ($estado | ConvertTo-Json -Depth 10), $utf8NoBom)

    # Hotfix #17: enviar tambien heartbeat al hub para actualizar UltimoHeartbeat
    # tanto en mcp.Proyectos como en mcp.ProyectoDevs (dashboard inventory + tab Equipo).
    # Best-effort: si falla el heartbeat, NO marcar la sync como fallida.
    try {
        $versionStic = if ($estado.ecosistema -and $estado.ecosistema.version) { $estado.ecosistema.version } else { "3.9.0" }
        $hbBody = @{
            projectId = $projectId
            versionStic = $versionStic
        } | ConvertTo-Json -Compress
        Invoke-RestMethod -Uri "$serverUrl/v2/heartbeat" -Method Post -Headers $headers `
            -Body $hbBody -ContentType 'application/json' -TimeoutSec 30 -ErrorAction Stop | Out-Null
    } catch {
        # Silencioso: el sync de contenido fue OK, el heartbeat es metadato secundario
    }

    Write-Host ""
    Write-Host "Sincronizacion completada. ultimaSync actualizado en ESTADO_PROYECTO.json" -ForegroundColor Green
    Write-Host "Dashboard: https://demowww.comillas.edu/claude-stic/docs/dashboard/" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host ""
    Write-Host "Sincronizacion incompleta ($totalErrors errores). ultimaSync NO actualizado." -ForegroundColor Yellow
    exit 1
}
