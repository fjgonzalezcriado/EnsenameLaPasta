# cert-expiry-reminder.ps1 - Aviso/bloqueo sobre certificados x.509 caducados o proximos a caducar
# Evento: PreToolUse[Write|Edit]
# Exit 0 = permitir (con aviso si caduca <90d), Exit 1 = AVISO informativo (no bloquea)
# Version: 1.1.0 (Plantilla STIC.IA v3.9.0 hotfix #28)
#   v1.1.0: politica Comillas WARN-first.
#           CAMBIO: default de configuracion.certBlockOnExpired pasa de TRUE a FALSE.
#           Razon: editar un cert caducado SI es un error frecuente, pero bloquear
#           genera workarounds (Set-Content) que ocultan el problema. Mejor warn
#           visible cada Edit + dejar que el dev decida (renovar vs revisar).
#           Proyectos que prefieren strict pueden opt-in con certBlockOnExpired=true.
#           Cuando hay bloqueo: exit 1 (AVISO) en lugar de exit 2 (BLOCK), patron
#           WARN-first del ecosistema.
#   v1.0.0: default bloqueante, configurable (v3.9.0 inicial, ADR-034)
#
# Detecta edicion de archivos: *.pfx, *.cer, *.crt, *.p12
# Para cada uno, lee la fecha de caducidad del cert via .NET X509Certificate2.
# Comportamiento:
#   - Cert caducado (NotAfter < hoy): AVISO por defecto. Configurable con
#     ESTADO_PROYECTO.json.configuracion.certBlockOnExpired = true para bloqueo strict.
#   - Caduca en <30 dias: warning rojo (no bloquea)
#   - Caduca en <90 dias: warning amarillo (no bloquea)
#   - Caduca en >=90 dias: silencioso (exit 0 sin output)
#
# Origen: H3 propuesto por Pasada 3 (caso EWP Intercambio). Generalizado para
# cualquier consumidor con mTLS/cert-based auth (no solo EWP).

param()
. "$PSScriptRoot/hook-helpers.ps1"

$hookData = Get-HookInput
$filePath = Get-FilePath $hookData
if (-not $filePath) { exit 0 }

# Solo procesar extensiones de cert
$ext = [System.IO.Path]::GetExtension($filePath).ToLower()
$certExtensions = @('.pfx', '.cer', '.crt', '.p12')
if ($certExtensions -notcontains $ext) { exit 0 }

# Helper: emitir aviso por stderr
function Write-Aviso { param([string]$Msg) [Console]::Error.WriteLine($Msg) }

# Si el archivo NO existe aun en disco (Write nuevo), no podemos leer cert -> avisar generico
if (-not (Test-Path $filePath)) {
    Write-Aviso "AVISO cert-expiry-reminder: vas a crear un cert nuevo ($filePath). Verifica:"
    Write-Aviso "  1. Su fecha de caducidad (NotAfter) cubre el periodo de uso planeado"
    Write-Aviso "  2. Esta protegido por .gitignore (no commitear .pfx con clave privada)"
    Write-Aviso "  3. Su clave privada esta en Azure Key Vault si es para produccion"
    exit 0
}

# Leer config (umbrales custom + bloqueo configurable)
$repoRoot = if ($env:CLAUDE_PROJECT_DIR) { $env:CLAUDE_PROJECT_DIR } else { (Get-Location).Path }
$estadoFile = Join-Path $repoRoot '_duran/ESTADO_PROYECTO.json'
$umbralWarning = 90
$umbralCritico = 30
$bloqueaCaducado = $false   # v1.1.0 default: SOLO AVISO (WARN-first). Opt-in para bloqueo strict.

if (Test-Path $estadoFile) {
    try {
        $estado = Get-Content $estadoFile -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($estado.configuracion) {
            if ($null -ne $estado.configuracion.certWarningDias) {
                $umbralWarning = [int]$estado.configuracion.certWarningDias
            }
            if ($null -ne $estado.configuracion.certCriticalDias) {
                $umbralCritico = [int]$estado.configuracion.certCriticalDias
            }
            if ($null -ne $estado.configuracion.certBlockOnExpired) {
                $bloqueaCaducado = [bool]$estado.configuracion.certBlockOnExpired
            }
        }
    } catch { }  # JSON corrupto - usar defaults
}

# Leer el cert via .NET
try {
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($filePath)
    $notAfter = $cert.NotAfter
    $subject = $cert.Subject
    $issuer = $cert.Issuer
} catch {
    # Si el cert tiene password (.pfx) o esta corrupto, no podemos leer NotAfter
    # No bloquear - solo avisar
    Write-Aviso "AVISO cert-expiry-reminder: no se pudo leer fecha de caducidad de $filePath"
    Write-Aviso "  Razon: $($_.Exception.Message)"
    Write-Aviso "  (puede ser .pfx con password o cert corrupto). Verifica manualmente."
    exit 0
}

$ahora = Get-Date
$diasRestantes = [int]($notAfter - $ahora).TotalDays

# Truncar nombres largos para legibilidad
$subjectShort = if ($subject.Length -gt 80) { $subject.Substring(0, 77) + "..." } else { $subject }
$fileName = Split-Path $filePath -Leaf

# Cert caducado
if ($diasRestantes -lt 0) {
    $diasCaducado = [Math]::Abs($diasRestantes)
    Write-Aviso ""
    Write-Aviso "AVISO cert-expiry-reminder: $fileName esta CADUCADO hace $diasCaducado dias."
    Write-Aviso "  Subject: $subjectShort"
    Write-Aviso "  Issuer: $issuer"
    Write-Aviso "  NotAfter: $($notAfter.ToString('yyyy-MM-dd'))"
    Write-Aviso ""
    Write-Aviso "  Editar un cert caducado en lugar de renovar suele ser error."
    Write-Aviso "  Fix recomendado: renovar el cert con CA, NO modificar el archivo caducado."
    if ($bloqueaCaducado) {
        Write-Aviso "  Modo strict (certBlockOnExpired=true): bloqueando edit."
        exit 1   # v1.1.0: exit 1 (AVISO) en lugar de exit 2 (BLOCK)
    } else {
        Write-Aviso "  Modo default (WARN-first): edit permitido, decide tu si renovar o continuar."
        Write-Aviso "  Para bloquear: setear configuracion.certBlockOnExpired=true en _duran/ESTADO_PROYECTO.json"
        exit 1
    }
}

# Cert critico (<30 dias)
if ($diasRestantes -lt $umbralCritico) {
    Write-Aviso "AVISO cert-expiry-reminder: $fileName caduca en $diasRestantes dias (umbral critico: $umbralCritico)."
    Write-Aviso "  Subject: $subjectShort"
    Write-Aviso "  NotAfter: $($notAfter.ToString('yyyy-MM-dd'))"
    Write-Aviso "  ACCION INMEDIATA: solicitar renovacion del cert AHORA."
    exit 0
}

# Cert proximo (<90 dias)
if ($diasRestantes -lt $umbralWarning) {
    Write-Aviso "AVISO cert-expiry-reminder: $fileName caduca en $diasRestantes dias (umbral warning: $umbralWarning)."
    Write-Aviso "  Subject: $subjectShort"
    Write-Aviso "  NotAfter: $($notAfter.ToString('yyyy-MM-dd'))"
    Write-Aviso "  Planifica renovacion en las proximas semanas."
    exit 0
}

# Cert OK (>=90 dias) - silencioso
exit 0
