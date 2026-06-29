# antipattern-guard.ps1 - Detectar anti-patrones .NET en archivos staged
# Evento: PreToolUse[Bash] (cuando el comando es git commit)
# Exit 1 = AVISO informativo (no bloquea commit), Exit 0 = sin hallazgos
# Version: 2.0.0 (STIC.IA v3.9.0)
#   v2.0.0: politica Comillas WARN-first (igual que secret-scanner.ps1 v2.0).
#           CAMBIOS:
#             - Todos los ERROR -> AVISO, exit 2 -> exit 1 (no bloquea commit)
#             - Mensajes via [Console]::Error.WriteLine para que Claude Code
#               surface correctamente al usuario (Write-Host iba al stream
#               Information de PS y resultaba en "No stderr output").
#             - El hook escanea contenido COMPLETO de archivos staged, no
#               solo el diff. Un refactor en archivo con .Result legacy
#               disparaba block aunque el commit no introdujera el antipatron.
#           Bloqueo activable bumpeando a v3.0.0 si Sistemas lo mandata.
#   v1.0.1 (STIC.IA v3.8.7): + GIT_OPTIONAL_LOCKS=0 para evitar colisiones
#                              index.lock con VS Code/IDE.
#   v1.0.0: deteccion 7 antipatrones .NET. Referencia: .NET Claude Kit
#           pre-commit-antipattern.sh adaptado para PowerShell/Comillas.
#
# Detecta: DateTime.Now, new HttpClient(), async void, .Result, sync-over-async,
#           catch generico, string interpolation en logs, connection strings hardcoded

param()
. "$PSScriptRoot/hook-helpers.ps1"

$hookData = Get-HookInput; $command = Get-ToolInput $hookData
if (-not $command) { exit 0 }

# Solo actuar cuando el comando es git commit
if ($command -notmatch 'git\s+commit') { exit 0 }

# Obtener archivos .cs en staging (sin crear lock para no colisionar con IDE)
try {
    $env:GIT_OPTIONAL_LOCKS = '0'
    $stagedFiles = git diff --cached --name-only --diff-filter=ACM 2>$null | Where-Object { $_ -match '\.cs$' }
} catch {
    exit 0
}

if (-not $stagedFiles -or $stagedFiles.Count -eq 0) { exit 0 }

# Helper: emitir aviso por stderr (visible para Claude Code)
function Write-Aviso {
    param([string]$Msg)
    [Console]::Error.WriteLine($Msg)
}

Write-Aviso "Verificando $($stagedFiles.Count) archivo(s) C# en staging..."

$warnings = 0

foreach ($file in $stagedFiles) {
    if (-not (Test-Path $file)) { continue }
    $content = Get-Content $file -Raw -ErrorAction SilentlyContinue
    if (-not $content) { continue }
    $lines = Get-Content $file -ErrorAction SilentlyContinue

    $lineNum = 0
    foreach ($line in $lines) {
        $lineNum++

        # AP001: async void (excepto event handlers)
        if ($line -match 'async\s+void' -and $line -notmatch 'EventArgs') {
            Write-Aviso "AVISO $file`:$lineNum - async void detectado. Recomendado: async Task."
            $warnings++
        }

        # AP002: .Result o .GetAwaiter().GetResult() (sync-over-async)
        if ($line -match '\.Result\b' -or $line -match '\.GetAwaiter\(\)\.GetResult\(\)') {
            Write-Aviso "AVISO $file`:$lineNum - sync-over-async (.Result / .GetAwaiter().GetResult())"
            $warnings++
        }

        # AP003: new HttpClient() (socket exhaustion)
        if ($line -match 'new\s+HttpClient\s*\(') {
            Write-Aviso "AVISO $file`:$lineNum - new HttpClient(). Recomendado: IHttpClientFactory."
            $warnings++
        }

        # AP004: DateTime.Now / DateTime.UtcNow (untestable)
        if ($line -match 'DateTime\.(Now|UtcNow)') {
            Write-Aviso "AVISO $file`:$lineNum - DateTime.Now/UtcNow. Recomendado: TimeProvider."
            $warnings++
        }

        # AP005: catch(Exception) broad catch
        if ($line -match 'catch\s*\(\s*Exception\s*\)' -or $line -match 'catch\s*\(\s*Exception\s+\w+\s*\)') {
            Write-Aviso "AVISO $file`:$lineNum - catch(Exception) generico. Recomendado: capturar excepciones especificas."
            $warnings++
        }

        # AP006: String interpolation en logging
        if ($line -match 'Log(Information|Warning|Error|Debug|Critical)\s*\(\s*\$"') {
            Write-Aviso "AVISO $file`:$lineNum - Interpolacion en log. Recomendado: template `"Msg {Param}`", value"
            $warnings++
        }

        # COMILLAS: Connection string hardcoded
        if ($line -match '(Server|Data Source|Initial Catalog|Password)\s*=' -and $line -notmatch '(appsettings|configuration|IOptions|GetConnectionString)') {
            if ($line -match '"[^"]*Server\s*=') {
                Write-Aviso "AVISO $file`:$lineNum - Connection string hardcoded. Recomendado: appsettings + Key Vault."
                $warnings++
            }
        }
    }
}

if ($warnings -gt 0) {
    Write-Aviso ""
    Write-Aviso "Se encontraron $warnings posible(s) antipatron(es) en archivos staged. Politica Comillas:"
    Write-Aviso "  - Entorno seguro y deuda tecnica progresiva - NO bloqueante actualmente"
    Write-Aviso "  - El hook escanea contenido completo del archivo, no solo el diff."
    Write-Aviso "    Un refactor sobre archivo con .Result/DateTime.Now/etc legacy lo dispara."
    Write-Aviso "  - Si el antipatron es NUEVO (introducido por este commit): corregirlo."
    Write-Aviso "  - Si es legacy preexistente: documentar en _duran/DEUDA_TECNICA.md como DT-XXX."
    Write-Aviso "  - Si Sistemas decide enforcement: hook bumpea a v3.0.0 con exit 2"
    exit 1   # exit 1 = AVISO informativo, patron WARN-first (igual secret-scanner v2.0)
}

Write-Aviso "OK: Verificacion de anti-patrones completada (0 hallazgos)."
exit 0
