#Requires -Version 5.1
# SCRIPT: Migrate-ToCPM | Skill: nugets-management | Version: 3.5.0
# Migra una solucion a Central Package Management

param(
    [string]$SolutionDir = (Get-Location).Path,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
Write-Host "=== Migracion a Central Package Management ===" -ForegroundColor Cyan
Write-Host "Directorio: $SolutionDir"
if ($DryRun) { Write-Host "[DRY RUN] No se modificaran archivos" -ForegroundColor Yellow }

# 1. Buscar todos los .csproj
$projects = Get-ChildItem -Path $SolutionDir -Filter "*.csproj" -Recurse
Write-Host "Encontrados $($projects.Count) proyectos"

# 2. Extraer PackageReference con Version
$packages = @{}
foreach ($proj in $projects) {
    [xml]$xml = Get-Content -Path $proj.FullName -Raw
    $refs = $xml.SelectNodes("//PackageReference[@Version]")
    foreach ($ref in $refs) {
        $name = $ref.GetAttribute("Include")
        $version = $ref.GetAttribute("Version")
        if (-not $packages.ContainsKey($name)) {
            $packages[$name] = $version
        } else {
            # Tomar la version mas alta
            if ([version]($version -replace '[^0-9.]','') -gt [version]($packages[$name] -replace '[^0-9.]','')) {
                $packages[$name] = $version
            }
        }
    }
}

Write-Host "Encontrados $($packages.Count) paquetes unicos"

# 3. Generar Directory.Packages.props
$propsPath = Join-Path $SolutionDir "Directory.Packages.props"
$sb = [System.Text.StringBuilder]::new()
$null = $sb.AppendLine('<Project>')
$null = $sb.AppendLine('  <PropertyGroup>')
$null = $sb.AppendLine('    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>')
$null = $sb.AppendLine('  </PropertyGroup>')
$null = $sb.AppendLine('  <ItemGroup>')
foreach ($pkg in ($packages.GetEnumerator() | Sort-Object Key)) {
    $null = $sb.AppendLine("    <PackageVersion Include=\"$($pkg.Key)\" Version=\"$($pkg.Value)\" />")
}
$null = $sb.AppendLine('  </ItemGroup>')
$null = $sb.AppendLine('</Project>')

if (-not $DryRun) {
    Set-Content -Path $propsPath -Value $sb.ToString() -Encoding UTF8
    Write-Host "Creado: $propsPath" -ForegroundColor Green
} else {
    Write-Host "[DRY RUN] Se crearia: $propsPath"
}

# 4. Eliminar Version de PackageReference en .csproj
$modified = 0
foreach ($proj in $projects) {
    $content = Get-Content -Path $proj.FullName -Raw
    $original = $content
    $content = $content -replace '(<PackageReference\s+Include="[^"]+")\s+Version="[^"]+"', '$1'
    if ($content -ne $original) {
        $modified++
        if (-not $DryRun) {
            Set-Content -Path $proj.FullName -Value $content -Encoding UTF8
            Write-Host "Modificado: $($proj.Name)" -ForegroundColor Green
        } else {
            Write-Host "[DRY RUN] Se modificaria: $($proj.Name)"
        }
    }
}

Write-Host ""
Write-Host "=== Resumen ===" -ForegroundColor Cyan
Write-Host "Paquetes unicos: $($packages.Count)"
Write-Host "Proyectos modificados: $modified / $($projects.Count)"
if ($DryRun) { Write-Host "Ejecutar sin -DryRun para aplicar cambios" -ForegroundColor Yellow }
