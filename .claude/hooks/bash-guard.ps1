# bash-guard.ps1 - Bloquear operaciones destructivas
# Evento: PreToolUse[Bash]
# Exit 2 = bloquear, Exit 0 = permitir
# Version: 2.0.0 (STIC.IA v3.9.0)
#   v2.0.0: + try/catch wrapper global (no rompe flujo si helper falla)
#           + git restore . (equivalente moderno de git checkout .)
#           + git branch -D / --delete --force
#           + git commit/push --no-verify (saltea hooks defensivos)
#           + git stash drop/clear
#           + SQL DROP ampliado (COLUMN, CONSTRAINT, INDEX, VIEW, PROCEDURE,
#             FUNCTION, TRIGGER) y ALTER TABLE...DROP
#           + --force-with-lease NO bloqueado (es seguro vs --force)
#           + word boundaries (\b) para evitar falsos positivos
#           + safe targets ampliados para rm -rf (.next, dist, coverage)
#   v1.0.0 (STIC.IA v3.8.0): 7 patrones basicos sin manejo de errores

param()

# ─── Wrapper de seguridad ─────────────────────────────────────────
# Cualquier excepcion no controlada -> exit 0 (permitir) + log a stderr.
# Mejor un falso negativo silencioso que un hook que rompe el flujo.
try {
    . "$PSScriptRoot/hook-helpers.ps1"

    $hookData = Get-HookInput
    $command = Get-ToolInput $hookData
    if (-not $command) { exit 0 }

    # ── Operaciones git destructivas ──────────────────────────────────

    # --force-with-lease es SEGURO (chequea remoto antes de sobrescribir).
    # Solo bloquear --force / -f puros.
    if ($command -match '\bgit\s+push\b' -and `
        $command -match '(\B--force\b|\B-f\b)' -and `
        $command -notmatch '--force-with-lease') {
        Write-Host "BLOQUEADO: git push --force/-f. Usa --force-with-lease (mas seguro) o un push normal."
        exit 2
    }

    if ($command -match '\bgit\s+reset\s+--hard\b') {
        Write-Host "BLOQUEADO: git reset --hard descartara todos los cambios no committeados."
        exit 2
    }

    if ($command -match '\bgit\s+clean\s+-[a-zA-Z]*f') {
        Write-Host "BLOQUEADO: git clean -f eliminara archivos no rastreados permanentemente."
        exit 2
    }

    # v2.0.0: cubre legacy 'git checkout .' y moderno 'git restore .'
    if ($command -match '\bgit\s+(checkout|restore)\s+\.\B') {
        Write-Host "BLOQUEADO: descartara todos los cambios sin stage (git checkout/restore .)."
        exit 2
    }

    # v2.0.0: git branch -D / --delete --force borra rama sin garantia de merge.
    # -cmatch (case-sensitive) para distinguir -D (force) de -d (safe).
    if ($command -cmatch '\bgit\s+branch\s+-D\b' -or `
        $command -match '\bgit\s+branch\s+--delete\s+--force\b') {
        Write-Host "BLOQUEADO: git branch -D borra una rama sin garantia de merge. Usa -d en su lugar."
        exit 2
    }

    # v2.0.0: --no-verify saltea pre-commit/pre-push hooks (red flag)
    if ($command -match '\bgit\s+(commit|push|merge|rebase)\b.*--no-verify\b') {
        Write-Host "BLOQUEADO: --no-verify saltea hooks defensivos. Si fallan, arregla la causa raiz."
        exit 2
    }

    # v2.0.0: git stash drop/clear elimina stashes sin recuperacion
    if ($command -match '\bgit\s+stash\s+(drop|clear)\b') {
        Write-Host "BLOQUEADO: git stash drop/clear elimina stashes sin posibilidad de recuperar."
        exit 2
    }

    # ── Eliminacion recursiva peligrosa ───────────────────────────────
    # Cubre: rm -rf, rm -fr, rm --recursive --force, Remove-Item -Recurse -Force
    $rmDestructive = '\brm\s+-[a-zA-Z]*r[a-zA-Z]*f\b|\brm\s+-[a-zA-Z]*f[a-zA-Z]*r\b|\brm\s+--recursive\b.*--force\b|Remove-Item.*-Recurse.*-Force'
    if ($command -match $rmDestructive) {
        # Permitir en targets seguros (carpetas de build/cache/IDE)
        $safeTargets = '(node_modules|bin[\\/]|obj[\\/]|TestResults|\.vs|packages[\\/]|\.next|dist[\\/]|coverage[\\/]|out[\\/])'
        if ($command -notmatch $safeTargets) {
            Write-Host "BLOQUEADO: Eliminacion recursiva forzada detectada. Verifica el path."
            exit 2
        }
    }

    # ── Operaciones SQL destructivas ──────────────────────────────────

    # v2.0.0: DROP ampliado a 8 tipos de objetos
    if ($command -match '\bDROP\s+(TABLE|DATABASE|SCHEMA|COLUMN|CONSTRAINT|INDEX|VIEW|PROCEDURE|FUNCTION|TRIGGER)\b') {
        Write-Host "BLOQUEADO: Operacion SQL DROP destructiva detectada. Verifica scope y backup."
        exit 2
    }

    if ($command -match '\bTRUNCATE\s+TABLE\b') {
        Write-Host "BLOQUEADO: TRUNCATE TABLE no es transaccional y borra todos los datos."
        exit 2
    }

    # DELETE FROM sin WHERE. Negative lookahead: bloquear si NO va seguido de WHERE.
    # Cubre tanto 'DELETE FROM T' final como 'DELETE FROM T;' o dentro de quotes.
    if ($command -match '\bDELETE\s+FROM\s+\w+\b(?!\s+WHERE\b)') {
        Write-Host "BLOQUEADO: DELETE FROM sin WHERE detectado. Anade clausula WHERE."
        exit 2
    }

    # v2.0.0: ALTER TABLE ... DROP COLUMN/CONSTRAINT
    if ($command -match '\bALTER\s+TABLE\s+\w+.*\bDROP\s+(COLUMN|CONSTRAINT)\b') {
        Write-Host "BLOQUEADO: ALTER TABLE...DROP destructivo detectado. Verifica scope."
        exit 2
    }

    # ── Proteccion de archivos sensibles Comillas ─────────────────────
    if ($command -match '\.(pfx|key|pem|p12)\b') {
        if ($command -match '\b(rm|del|Remove-Item|move|mv)\b') {
            Write-Host "BLOQUEADO: Operacion destructiva sobre certificado/clave privada detectada."
            exit 2
        }
    }

    # ── Advertencia dotnet run (no bloquea) ───────────────────────────
    if ($command -match '\bdotnet\s+run\b') {
        Write-Host "AVISO: dotnet run detectado. Verifica que launchSettings.json existe y el perfil es correcto."
    }

    exit 0
}
catch {
    # Error inesperado: permitir operacion en lugar de romper el flujo.
    # Log a stderr para diagnostico (no interfiere con exit 0).
    [Console]::Error.WriteLine("bash-guard.ps1: error inesperado, permitiendo operacion. Detalle: $_")
    exit 0
}
