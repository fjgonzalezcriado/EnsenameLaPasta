#Requires -Version 5.1
# SCRIPT: Audit-Packages | Skill: nugets-management | Version: 3.5.0
# Auditoria de vulnerabilidades en paquetes NuGet

param(
    [string]$SolutionDir = (Get-Location).Path,
    [switch]$IncludeTransitive,
    [string]$OutputFile
)

$ErrorActionPreference = 'Stop'
Write-Host "=== Auditoria de Vulnerabilidades NuGet ===" -ForegroundColor Cyan

# Construir comando
$cmd = "dotnet list \"$SolutionDir\" package --vulnerable"
if ($IncludeTransitive) { $cmd += " --include-transitive" }

# Ejecutar
Write-Host "Ejecutando: $cmd"
$output = Invoke-Expression $cmd 2>&1

# Parsear resultado
$vulnerabilities = @()
$currentProject = ""
foreach ($line in ($output -split [Environment]::NewLine)) {
    if ($line -match "^\s*>\s*(\S+)") {
        $pkg = $Matches[1]
        $parts = ($line.Trim() -split "\s+")
        if ($parts.Count -ge 4) {
            $vulnerabilities += [PSCustomObject]@{
                Project  = $currentProject
                Package  = $pkg
                Resolved = $parts[1]
                Severity = $parts[2]
                Advisory = if ($parts.Count -ge 4) { $parts[3] } else { "" }
            }
        }
    }
    elseif ($line -match "^\s*(\S+\.csproj)") {
        $currentProject = $Matches[1]
    }
}

# Mostrar resultados
if ($vulnerabilities.Count -eq 0) {
    Write-Host "No se encontraron vulnerabilidades." -ForegroundColor Green
} else {
    Write-Host "Encontradas $($vulnerabilities.Count) vulnerabilidades:" -ForegroundColor Red
    $vulnerabilities | Format-Table -AutoSize

    # Resumen por severidad
    $summary = $vulnerabilities | Group-Object Severity | Select-Object Name, Count
    Write-Host "Resumen:"
    foreach ($s in $summary) {
        $color = switch ($s.Name) { "Critical" { "Red" } "High" { "Yellow" } default { "White" } }
        Write-Host "  $($s.Name): $($s.Count)" -ForegroundColor $color
    }
}

# Guardar reporte
if ($OutputFile) {
    $vulnerabilities | ConvertTo-Json | Set-Content -Path $OutputFile -Encoding UTF8
    Write-Host "Reporte guardado en: $OutputFile" -ForegroundColor Green
}
