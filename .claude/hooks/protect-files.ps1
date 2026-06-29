# protect-files.ps1 - Bloquear escritura en archivos protegidos
# Evento: PreToolUse[Write|Edit]
# Exit 2 = bloquear (certs/keys, daño irreversible), Exit 1 = AVISO (configs PROD)
# Exit 0 = permitir
# Version: 1.1.0 (STIC.IA v3.9.0 hotfix #28)
#   v1.1.0: politica WARN-first selectiva.
#           CAMBIOS:
#             - Certs (.pfx, .key, .pem, .p12, .cer, .crt): SIGUEN BLOCK (exit 2).
#               Razon: editar private key destruye el cert, no hay caso legitimo
#               de edicion (se renueva, no se modifica). Es daño irreversible.
#             - Configs de PROD (appsettings.Production.json, etc.): pasan a WARN
#               (exit 1). Razon: devs si editan estos archivos legitimamente (log
#               levels, allowed origins, feature flags). Bloquear forza workarounds
#               tipo Set-Content que pierden auditabilidad.
#             - Mensajes via [Console]::Error.WriteLine para visibilidad en Claude Code.
#   v1.0.0: todos los archivos sensibles bloqueados (v3.8.0)

param()
. "$PSScriptRoot/hook-helpers.ps1"

$hookData = Get-HookInput; $toolInput = Get-ToolInput $hookData
if (-not $toolInput) { exit 0 }

# Helper: emitir mensaje por stderr (visible para Claude Code)
function Write-Aviso { param([string]$Msg) [Console]::Error.WriteLine($Msg) }

# Extraer file_path del input
$filePath = $null
if ($toolInput -match '"file_path"\s*:\s*"([^"]+)"') {
    $filePath = $Matches[1]
}
if (-not $filePath) { exit 0 }

$fileName = Split-Path $filePath -Leaf
$extension = [System.IO.Path]::GetExtension($fileName).ToLower()

# === BLOCK ESTRICTO (daño irreversible) ===

# Certificados y claves privadas: editar = destruir. Renovar via CA, NO modificar.
$blockedExtensions = @('.pfx', '.key', '.pem', '.p12', '.cer', '.crt')
if ($blockedExtensions -contains $extension) {
    Write-Aviso "BLOQUEADO: No se permite escribir archivos de certificado/clave ($fileName)"
    Write-Aviso "  Razon: editar private key destruye el cert. Renovar con CA, NO modificar."
    Write-Aviso "  Si necesitas crear un cert NUEVO: hacerlo fuera de Claude (openssl, certmgr, etc.)"
    exit 2
}

# === WARN (configs PROD - se editan legitimamente pero con cuidado) ===

# Archivos de produccion sensibles
$warnProdFiles = @(
    'Production.json',
    'appsettings.Production.json',
    'appsettings.Staging.json',
    'web.Production.config'
)
if ($warnProdFiles -contains $fileName) {
    Write-Aviso "AVISO: Editando configuracion de PRODUCCION ($fileName)."
    Write-Aviso "  Verifica:"
    Write-Aviso "    1. ¿El cambio es realmente necesario en PROD ya, o puede esperar a develop primero?"
    Write-Aviso "    2. ¿Tienes backup del archivo original (commit anterior en git)?"
    Write-Aviso "    3. ¿Has revisado con el JP/responsable tecnico antes de desplegar?"
    Write-Aviso "  Politica Comillas WARN-first: edit permitido, decision del dev."
    exit 1
}

# === AVISOS INFORMATIVOS (no bloquean ni hacen exit 1, solo informan) ===

# Connection strings en appsettings
if ($fileName -match '^appsettings.*\.json$' -and $toolInput -match '(Password|pwd|Server=|Data Source)') {
    Write-Aviso "AVISO: Posible connection string en $fileName. Usa Azure Key Vault para produccion."
}

# .env files
if ($fileName -match '^\.env') {
    Write-Aviso "AVISO: Archivo .env detectado ($fileName). Verifica que esta en .gitignore."
}

# Archivos de configuracion critica
$warnFiles = @('global.json', 'Directory.Build.props', 'Directory.Packages.props', 'NuGet.Config')
if ($warnFiles -contains $fileName) {
    Write-Aviso "AVISO: Modificando archivo de configuracion critica ($fileName)."
}

exit 0
