# post-build-check.ps1 - Verificar resultado de build/test tras ejecucion
# Evento: PostToolUse[Bash]
# Exit 0 = siempre (informativo, no bloquea)
# Version: 1.0.0 (STIC.IA v3.8.0)

param()
. "$PSScriptRoot/hook-helpers.ps1"

$hookData = Get-HookInput; $command = Get-ToolInput $hookData
if (-not $command) { exit 0 }

$output = Get-ToolOutput $hookData

# Solo actuar en comandos dotnet build/test
if ($command -notmatch 'dotnet\s+(build|test|publish)') { exit 0 }

# Detectar resultado del build
if ($command -match 'dotnet\s+build') {
    if ($output -match 'Build succeeded') {
        # Silencioso en exito
    }
    elseif ($output -match 'Build FAILED|error CS\d{4}') {
        $errorCount = ([regex]::Matches($output, 'error CS\d{4}')).Count
        Write-Host "BUILD FALLIDO: $errorCount error(es) detectado(s). Revisar antes de continuar."
    }

    # Contar warnings
    $warningCount = ([regex]::Matches($output, 'warning CS\d{4}')).Count
    if ($warningCount -gt 10) {
        Write-Host "AVISO: $warningCount warnings en build. Considerar ejecutar /clean para limpiar."
    }
}

# Detectar resultado de tests
if ($command -match 'dotnet\s+test') {
    if ($output -match 'Failed!\s+.*Failed:\s*(\d+)') {
        $failedTests = $Matches[1]
        Write-Host "TESTS FALLIDOS: $failedTests test(s) fallido(s). Revisar antes de commit."
    }
    elseif ($output -match 'No test is available') {
        Write-Host "AVISO: No se encontraron tests en el proyecto."
    }
}

# Detectar publish
if ($command -match 'dotnet\s+publish') {
    if ($output -match 'error') {
        Write-Host "PUBLISH FALLIDO: Errores detectados en la publicacion."
    }
}

exit 0
