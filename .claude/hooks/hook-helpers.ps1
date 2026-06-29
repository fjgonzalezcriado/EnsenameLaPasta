# hook-helpers.ps1 - Funciones comunes para hooks STIC.IA
# Todos los hooks importan este archivo: . "$PSScriptRoot/hook-helpers.ps1"
# Version: 1.1.0 (STIC.IA v3.8.7)
#   v1.1.0: + Invoke-GitWithRetry, Invoke-GitReadOnly, Test-GitLockHealthy,
#             Clear-StaleGitLock para evitar colisiones con index.lock.
#   v1.0.0: helpers de input/output para hooks (Get-HookInput, Get-ToolInput, ...)
#
# Protocolo Claude Code hooks:
#   - PreToolUse/PostToolUse: JSON via stdin con tool_name, tool_input, tool_output
#   - SessionStart/Stop: No reciben stdin
#   - stdout → contexto Claude (PreToolUse/SessionStart) o ignorado (PostToolUse/Stop)
#   - Exit 0 = permitir, Exit 2 = bloquear (solo PreToolUse)

function Get-HookInput {
    <#
    .SYNOPSIS
    Lee y parsea el JSON de stdin que Claude Code envia a hooks.
    Fallback a variables de entorno para compatibilidad.
    .OUTPUTS
    PSCustomObject con propiedades: tool_name, tool_input, tool_output (segun evento)
    #>

    $hookData = $null

    # 1. Intentar leer JSON desde stdin
    try {
        if (-not [Console]::IsInputRedirected) {
            # No hay stdin, usar fallback
        } else {
            $stdinContent = [Console]::In.ReadToEnd()
            if ($stdinContent -and $stdinContent.Trim().Length -gt 0) {
                $hookData = $stdinContent | ConvertFrom-Json -ErrorAction Stop
            }
        }
    } catch {
        # stdin no disponible o no es JSON valido
    }

    # 2. Fallback a variables de entorno (compatibilidad)
    if (-not $hookData) {
        $hookData = [PSCustomObject]@{
            tool_name  = $env:CLAUDE_TOOL_NAME
            tool_input = $env:CLAUDE_TOOL_INPUT
            tool_output = $env:CLAUDE_TOOL_OUTPUT
        }
    }

    return $hookData
}

function Get-ToolInput {
    <#
    .SYNOPSIS
    Obtiene el input del tool (command para Bash, file_path+content para Write/Edit)
    #>
    param([PSCustomObject]$HookData)

    if (-not $HookData) { return $null }

    # Si tool_input es string, devolverlo directamente
    if ($HookData.tool_input -is [string]) {
        return $HookData.tool_input
    }

    # Si es objeto, convertirlo a string JSON para regex matching
    if ($HookData.tool_input) {
        return ($HookData.tool_input | ConvertTo-Json -Compress -Depth 5)
    }

    return $null
}

function Get-ToolOutput {
    <#
    .SYNOPSIS
    Obtiene el output del tool (solo PostToolUse)
    #>
    param([PSCustomObject]$HookData)

    if (-not $HookData) { return $null }

    if ($HookData.tool_output -is [string]) {
        return $HookData.tool_output
    }

    if ($HookData.tool_output) {
        return ($HookData.tool_output | ConvertTo-Json -Compress -Depth 5)
    }

    return $null
}

function Get-FilePath {
    <#
    .SYNOPSIS
    Extrae file_path del input (Write/Edit tools)
    #>
    param([PSCustomObject]$HookData)

    if (-not $HookData) { return $null }

    # Objeto con file_path
    if ($HookData.tool_input -and $HookData.tool_input.file_path) {
        return $HookData.tool_input.file_path
    }

    # String con file_path en JSON
    $toolInput = Get-ToolInput $HookData
    if ($toolInput -match '"file_path"\s*:\s*"([^"]+)"') {
        return $Matches[1]
    }

    # Fallback env var
    if ($env:CLAUDE_EDITED_FILE) {
        return $env:CLAUDE_EDITED_FILE
    }

    return $null
}

function Get-NewContent {
    <#
    .SYNOPSIS
    Extrae new_string (Edit) o content (Write) del input
    #>
    param([PSCustomObject]$HookData)

    if (-not $HookData) { return $null }

    # Objeto con new_string o content
    if ($HookData.tool_input) {
        if ($HookData.tool_input.new_string) { return $HookData.tool_input.new_string }
        if ($HookData.tool_input.content) { return $HookData.tool_input.content }
    }

    # String con new_string/content en JSON
    $toolInput = Get-ToolInput $HookData
    if ($toolInput -match '"new_string"\s*:\s*"((?:[^"\\]|\\.)*)"') { return $Matches[1] }
    if ($toolInput -match '"content"\s*:\s*"((?:[^"\\]|\\.)*)"') { return $Matches[1] }

    return $null
}

# ============================================================================
# GIT HELPERS (v1.1.0) - Manejo robusto de index.lock concurrente
# ============================================================================
#
# Problema: VS Code/GitLens, fsmonitor y otros hooks pueden ejecutar `git status`
# en paralelo a operaciones de /actualizar, causando exit 128:
#   "fatal: Unable to create '.git/index.lock': File exists."
#
# Solucion en 2 capas:
#   1. Invoke-GitReadOnly: para `git status`/`git diff` (no toca el indice).
#      Usa GIT_OPTIONAL_LOCKS=0 para no crear locks read-only.
#   2. Invoke-GitWithRetry: para `git add`/`commit`/`checkout` (modifican indice).
#      Retry con backoff exponencial ante exit 128 + "index.lock".
# ============================================================================

function Invoke-GitReadOnly {
    <#
    .SYNOPSIS
    Ejecuta `git` read-only sin crear locks (GIT_OPTIONAL_LOCKS=0).
    Apto para status, diff, log, branch -l, ls-files, rev-parse, etc.
    .PARAMETER GitArgs
    Array con los argumentos de git, ej. @('status', '--short')
    .PARAMETER WorkingDir
    Repositorio destino. Default: cwd.
    .OUTPUTS
    Hashtable con: ExitCode, StdOut, StdErr
    #>
    param(
        [Parameter(Mandatory)][string[]]$GitArgs,
        [string]$WorkingDir = (Get-Location).Path
    )

    $prevValue = $env:GIT_OPTIONAL_LOCKS
    $env:GIT_OPTIONAL_LOCKS = '0'
    try {
        $stdoutFile = [System.IO.Path]::GetTempFileName()
        $stderrFile = [System.IO.Path]::GetTempFileName()
        try {
            $proc = Start-Process -FilePath 'git' -ArgumentList $GitArgs `
                -WorkingDirectory $WorkingDir -NoNewWindow -Wait -PassThru `
                -RedirectStandardOutput $stdoutFile `
                -RedirectStandardError $stderrFile
            return @{
                ExitCode = $proc.ExitCode
                StdOut   = (Get-Content $stdoutFile -Raw -ErrorAction SilentlyContinue)
                StdErr   = (Get-Content $stderrFile -Raw -ErrorAction SilentlyContinue)
            }
        } finally {
            Remove-Item $stdoutFile, $stderrFile -ErrorAction SilentlyContinue
        }
    } finally {
        if ($null -eq $prevValue) {
            Remove-Item Env:GIT_OPTIONAL_LOCKS -ErrorAction SilentlyContinue
        } else {
            $env:GIT_OPTIONAL_LOCKS = $prevValue
        }
    }
}

function Invoke-GitWithRetry {
    <#
    .SYNOPSIS
    Ejecuta `git` con retry exponencial ante "index.lock: File exists" (exit 128).
    Apto para add, commit, checkout, restore, rm -- ops que modifican el indice.
    .PARAMETER GitArgs
    Array con los argumentos de git, ej. @('add', '.claude/settings.local.json')
    .PARAMETER MaxRetries
    Numero maximo de reintentos. Default: 5 (total ~15s con backoff).
    .PARAMETER InitialBackoffMs
    Espera inicial en ms. Default: 300. Se duplica cada retry.
    .PARAMETER WorkingDir
    Repositorio destino. Default: cwd.
    .OUTPUTS
    Hashtable con: ExitCode, StdOut, StdErr, Attempts (numero de intentos realizados)
    #>
    param(
        [Parameter(Mandatory)][string[]]$GitArgs,
        [int]$MaxRetries = 5,
        [int]$InitialBackoffMs = 300,
        [string]$WorkingDir = (Get-Location).Path
    )

    $attempt = 0
    $backoff = $InitialBackoffMs
    $result = $null

    while ($attempt -le $MaxRetries) {
        $attempt++

        $stdoutFile = [System.IO.Path]::GetTempFileName()
        $stderrFile = [System.IO.Path]::GetTempFileName()
        try {
            $proc = Start-Process -FilePath 'git' -ArgumentList $GitArgs `
                -WorkingDirectory $WorkingDir -NoNewWindow -Wait -PassThru `
                -RedirectStandardOutput $stdoutFile `
                -RedirectStandardError $stderrFile

            $stdout = Get-Content $stdoutFile -Raw -ErrorAction SilentlyContinue
            $stderr = Get-Content $stderrFile -Raw -ErrorAction SilentlyContinue

            $result = @{
                ExitCode = $proc.ExitCode
                StdOut   = $stdout
                StdErr   = $stderr
                Attempts = $attempt
            }

            # Exito o error no relacionado con lock -> devolver sin retry
            if ($proc.ExitCode -eq 0) { return $result }
            $isLockError = ($proc.ExitCode -eq 128) -and ($stderr -match 'index\.lock.*File exists')
            if (-not $isLockError) { return $result }

            # Error de lock: si quedan retries, esperar y reintentar
            if ($attempt -le $MaxRetries) {
                Start-Sleep -Milliseconds $backoff
                $backoff = [Math]::Min($backoff * 2, 5000)  # cap a 5s por intento
            }
        } finally {
            Remove-Item $stdoutFile, $stderrFile -ErrorAction SilentlyContinue
        }
    }

    return $result
}

function Test-GitLockHealthy {
    <#
    .SYNOPSIS
    Comprueba si .git/index.lock esta presente y, en caso afirmativo, su edad.
    .PARAMETER RepoPath
    Ruta al repo. Default: cwd.
    .OUTPUTS
    Hashtable con: HasLock (bool), AgeSeconds (double, -1 si no hay lock), LockPath (string)
    #>
    param([string]$RepoPath = (Get-Location).Path)

    $lockPath = Join-Path $RepoPath '.git/index.lock'
    if (-not (Test-Path $lockPath)) {
        return @{ HasLock = $false; AgeSeconds = -1; LockPath = $lockPath }
    }
    $lockFile = Get-Item $lockPath -ErrorAction SilentlyContinue
    if (-not $lockFile) {
        return @{ HasLock = $false; AgeSeconds = -1; LockPath = $lockPath }
    }
    $ageSec = ((Get-Date) - $lockFile.LastWriteTime).TotalSeconds
    return @{ HasLock = $true; AgeSeconds = $ageSec; LockPath = $lockPath }
}

function Clear-StaleGitLock {
    <#
    .SYNOPSIS
    Elimina .git/index.lock si lleva mas de MinAgeSeconds sin actualizarse
    (huerfano de un proceso git muerto). Si esta activo, NO toca nada.
    .PARAMETER RepoPath
    Ruta al repo. Default: cwd.
    .PARAMETER MinAgeSeconds
    Umbral en segundos. Default: 30.
    .OUTPUTS
    Hashtable con: Removed (bool), Reason (string)
    #>
    param(
        [string]$RepoPath = (Get-Location).Path,
        [int]$MinAgeSeconds = 30
    )

    $info = Test-GitLockHealthy -RepoPath $RepoPath
    if (-not $info.HasLock) {
        return @{ Removed = $false; Reason = 'no-lock' }
    }
    $ageInt = [int]$info.AgeSeconds
    if ($info.AgeSeconds -lt $MinAgeSeconds) {
        return @{ Removed = $false; Reason = "active-lock (${ageInt}s < ${MinAgeSeconds}s)" }
    }
    try {
        Remove-Item $info.LockPath -Force -ErrorAction Stop
        return @{ Removed = $true; Reason = "stale-lock-removed (age ${ageInt}s)" }
    } catch {
        return @{ Removed = $false; Reason = "remove-failed: $_" }
    }
}
