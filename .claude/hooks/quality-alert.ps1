# quality-alert.ps1 - Alertar si muchos warnings o deuda tecnica
# Evento: PostToolUse[Bash]
# Exit 0 = siempre (informativo)
# Version: 1.0.0 (STIC.IA v3.8.0)

param()
. "$PSScriptRoot/hook-helpers.ps1"

$hookData = Get-HookInput; $command = Get-ToolInput $hookData
if (-not $command) { exit 0 }

$output = Get-ToolOutput $hookData

# Solo actuar en dotnet build
if ($command -notmatch 'dotnet\s+build') { exit 0 }

# Contar warnings por categoria
$nullableWarnings = ([regex]::Matches($output, 'warning CS86\d{2}')).Count
$obsoleteWarnings = ([regex]::Matches($output, 'warning CS0618|warning CS0619')).Count
$analyzerWarnings = ([regex]::Matches($output, 'warning IDE\d{4}|warning CA\d{4}')).Count
$totalWarnings = ([regex]::Matches($output, 'warning (CS|IDE|CA)\d{4}')).Count

# Alertas por umbral
if ($nullableWarnings -gt 20) {
    Write-Host "CALIDAD: $nullableWarnings warnings de nullable reference types. Considerar activar <Nullable>enable</Nullable> y corregir."
}

if ($obsoleteWarnings -gt 5) {
    Write-Host "CALIDAD: $obsoleteWarnings APIs obsoletas detectadas. Planificar actualizacion."
}

if ($analyzerWarnings -gt 15) {
    Write-Host "CALIDAD: $analyzerWarnings warnings de analyzers. Ejecutar /clean para limpieza sistematica."
}

if ($totalWarnings -gt 50) {
    Write-Host "CALIDAD CRITICA: $totalWarnings warnings totales. El proyecto necesita una sesion de limpieza con /clean."
}

# Detectar patrones de deuda tecnica en output
if ($output -match 'TODO|HACK|FIXME') {
    $todoCount = ([regex]::Matches($output, 'TODO|HACK|FIXME')).Count
    if ($todoCount -gt 0) {
        Write-Host "DEUDA TECNICA: $todoCount TODO/HACK/FIXME encontrados en output del build."
    }
}

exit 0
