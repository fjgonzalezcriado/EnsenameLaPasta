<#
.SYNOPSIS
    Integra documentacion STIC en Visual Studio con Solution Folders jerarquicos

.DESCRIPTION
    Crea una carpeta contenedora _STIC.IA con documentacion util para desarrolladores:

    _STIC.IA/
    +-- Raiz              (CLAUDE.md, README.md)
    +-- skills/           (templates de codigo reutilizables)
    +-- _duran/        (specs/, guias-uso/, reuniones/)
    +-- 00_Gestion/
    +-- 01_Diseno/
    +-- 06_Documentacion/
    +-- Documentos_Base/
    +-- ...

    NO incluye .claude/commands ni .claude/rules (son configuracion interna de Claude)

.NOTES
    Version: 3.9.0
    Requiere: PowerShell 7.0+
    - Solo documentacion util para desarrolladores
    - Skills con templates de codigo (.cs, .cshtml, .css, etc.)
    - Sin commands/rules (fontaneria de Claude)
    - .slnx usa formato plano con nombres jerarquicos (Visual Studio compatible)

.EXAMPLE
    .\.claude\commands\integracion-vs.ps1
    .\.claude\commands\integracion-vs.ps1 -Force
    .\.claude\commands\integracion-vs.ps1 -All
#>

param(
    [switch]$Force,
    [switch]$All,
    [string]$Solution
)

$ScriptVersion = "3.9.0"
$ContainerName = "_STIC.IA"
$VersionMarker = "# STIC-Integracion v$ScriptVersion"

Write-Host ""
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "  INTEGRACION VISUAL STUDIO - Solution Folders v$ScriptVersion" -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""

# ===============================================================
# VERIFICACION
# ===============================================================
if (-not (Test-Path "CLAUDE.md")) {
    Write-Host "[ERROR] No se encuentra CLAUDE.md. Ejecutar desde la raiz del proyecto." -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Ejecutando desde raiz del proyecto" -ForegroundColor Green

# ===============================================================
# BUSCAR SOLUCIONES
# ===============================================================
$allSlnx = @(Get-ChildItem -Path "03_Desarrollo" -Filter "*.slnx" -Recurse -Depth 3 -ErrorAction SilentlyContinue)
$allSln = @(Get-ChildItem -Path "03_Desarrollo" -Filter "*.sln" -Recurse -Depth 3 -ErrorAction SilentlyContinue)
$allSolutions = @($allSlnx) + @($allSln) | Where-Object { $_ -ne $null }

if ($allSolutions.Count -eq 0) {
    Write-Host "[ERROR] No se encontraron archivos .sln/.slnx en 03_Desarrollo/" -ForegroundColor Red
    exit 1
}

Write-Host "[INFO] Encontradas $($allSolutions.Count) soluciones" -ForegroundColor Gray

# ===============================================================
# CARGAR CONFIGURACION DESDE JSON (si existe)
# El JSON es la fuente de verdad para la estructura
# ===============================================================
$estructuraJsonPath = Join-Path $PSScriptRoot "..\estructura-solucion.json"
$estructuraConfig = $null
if (Test-Path $estructuraJsonPath) {
    try {
        $estructuraConfig = Get-Content $estructuraJsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
        Write-Host "[OK] Configuracion cargada desde estructura-solucion.json" -ForegroundColor Green
    }
    catch {
        Write-Host "[WARN] Error leyendo estructura-solucion.json, usando valores por defecto" -ForegroundColor Yellow
    }
}

# ===============================================================
# DEFINICION DE ESTRUCTURA
# IMPORTANTE: Mantener sincronizado con estructura-solucion.json
# El JSON es la fuente de verdad; este array es el fallback
# ===============================================================
$folderStructure = @(
    @{
        Name = "Raiz"
        Path = $null
        Files = @("CLAUDE.md", "README.md", "INICIO_RAPIDO.md")
    },
    @{
        # Skills: templates de codigo reutilizables
        # Incluye: .md, .cs.template, .cshtml, .css, .js, etc.
        Name = "skills"
        Path = ".claude/skills"
        Filter = "*"  # Todos los archivos
    },
    @{
        # Hooks: scripts de sesion (banner, etc.)
        Name = "hooks"
        Path = ".claude/hooks"
        Filter = "*"
    },
    @{
        Name = "_duran"
        Path = "_duran"
        Filter = "*"
    },
    @{
        Name = "_atlas"
        Path = "_atlas"
        Filter = "*"
    },
    @{
        Name = "00_Gestion"
        Path = "00_Gestion"
        Filter = "*.md"
    },
    @{
        Name = "01_Diseno"
        Path = "01_Diseno"
        Filter = "*.md"
    },
    @{
        Name = "02_Entorno"
        Path = "02_Entorno"
        Filter = "*"
    },
    @{
        Name = "04_Pruebas"
        Path = "04_Pruebas"
        Filter = "*.md"
    },
    @{
        Name = "05_CICD"
        Path = "05_CICD"
        Filter = "*"
        ExtraFiles = @("azure-pipelines.yml")
    },
    @{
        Name = "06_Documentacion"
        Path = "06_Documentacion"
        Filter = "*.md"
    },
    @{
        Name = "07_UAP"
        Path = "07_UAP"
        Filter = "*.md"
    },
    @{
        Name = "Documentos_Base"
        Path = "Documentos_Base"
        Filter = "*.md"
    }
)

# ===============================================================
# FUNCIONES
# ===============================================================

function Get-RelativePrefix {
    param([string]$SlnFolder, [string]$ProjectRoot)

    $slnNorm = $SlnFolder.TrimEnd('\')
    $rootNorm = $ProjectRoot.TrimEnd('\')

    if ($slnNorm -eq $rootNorm) { return "" }

    $current = $slnNorm
    $levels = 0
    while ($current -ne $rootNorm -and $current.Length -gt 3 -and $levels -lt 10) {
        $current = Split-Path $current -Parent
        $levels++
    }

    if ($current -eq $rootNorm) { return ("..\" * $levels) } else { return "..\.." }
}

function New-Guid { return "{$([guid]::NewGuid().ToString().ToUpper())}" }

function Get-FolderFiles {
    param(
        [string]$BasePath,
        [string]$Prefix,
        [string]$Filter = "*",
        [bool]$IncludePS1 = $false,
        [string[]]$ExtraFiles = @()
    )

    $result = @{
        Folders = @{}
        Files = @{}
        Parents = @{}
    }

    if (-not (Test-Path $BasePath)) { return $result }

    # Buscar archivos
    $files = Get-ChildItem -Path $BasePath -Recurse -File -ErrorAction SilentlyContinue

    # Filtrar por extension (si no es *)
    if ($Filter -ne "*") {
        $ext = $Filter -replace "\*", ""
        $files = $files | Where-Object { $_.Extension -eq $ext }
    }

    # Incluir .ps1 si se solicita
    if ($IncludePS1) {
        $ps1Files = Get-ChildItem -Path $BasePath -Recurse -Filter "*.ps1" -ErrorAction SilentlyContinue
        $files = @($files) + @($ps1Files) | Sort-Object FullName -Unique
    }

    $projectRoot = (Get-Location).Path
    $baseNorm = $BasePath.Replace('/', '\').TrimEnd('\')

    foreach ($file in $files) {
        $relPath = $file.FullName.Replace("$projectRoot\", "")
        $dir = (Split-Path $relPath -Parent).Replace('/', '\')

        # Determinar subcarpeta dentro del BasePath
        $dirNorm = $dir.TrimEnd('\')
        $subFolder = if ($dirNorm -eq $baseNorm) {
            ""
        } elseif ($dirNorm.Length -gt $baseNorm.Length -and $dirNorm.StartsWith($baseNorm)) {
            $dirNorm.Substring($baseNorm.Length + 1)
        } else {
            ""
        }

        # Registrar carpeta y sus padres
        if ($subFolder -ne "" -and -not $result.Folders.ContainsKey($subFolder)) {
            $result.Folders[$subFolder] = New-Guid

            # Registrar carpetas padre intermedias
            $parts = $subFolder -split '\\'
            for ($i = 0; $i -lt $parts.Count - 1; $i++) {
                $parentPath = ($parts[0..$i]) -join '\'
                if (-not $result.Folders.ContainsKey($parentPath)) {
                    $result.Folders[$parentPath] = New-Guid
                }
            }

            $parent = Split-Path $subFolder -Parent
            if ($parent) {
                $result.Parents[$subFolder] = $parent
            }
        }

        # Registrar archivo
        if (-not $result.Files.ContainsKey($subFolder)) {
            $result.Files[$subFolder] = @()
        }
        $result.Files[$subFolder] += "${Prefix}$relPath"
    }

    # Archivos extra
    foreach ($extra in $ExtraFiles) {
        if (Test-Path $extra) {
            if (-not $result.Files.ContainsKey("")) { $result.Files[""] = @() }
            $result.Files[""] += "${Prefix}$extra"
        }
    }

    return $result
}

function Process-Solution {
    param([System.IO.FileInfo]$SolutionFile, [switch]$ForceRecreate)

    $slnPath = $SolutionFile.FullName
    $slnName = $SolutionFile.Name
    $sfGuid = '{2150E333-8FDC-42A3-9474-1A3956D46DE8}'

    Write-Host ""
    Write-Host "+-------------------------------------------------------------------+" -ForegroundColor White
    Write-Host "| PROCESANDO: $($slnName.PadRight(51)) |" -ForegroundColor White
    Write-Host "+-------------------------------------------------------------------+" -ForegroundColor White

    # Calcular prefijo
    $projectRoot = (Get-Location).Path
    $slnFolder = Split-Path $slnPath -Parent
    $prefix = Get-RelativePrefix -SlnFolder $slnFolder -ProjectRoot $projectRoot

    Write-Host "  Prefijo relativo: '$prefix'" -ForegroundColor Gray

    # Leer .sln
    $content = Get-Content $slnPath -Raw -Encoding UTF8

    # Verificar si ya tiene STIC
    if ($content -match "STIC-Integracion v" -and -not $ForceRecreate) {
        $versionMatch = [regex]::Match($content, "STIC-Integracion v([\d\.]+)")
        $installedVersion = $versionMatch.Groups[1].Value
        Write-Host "  [SKIP] Ya tiene STIC v$installedVersion. Usa -Force para actualizar." -ForegroundColor Yellow
        return @{ Name = $slnName; Status = "Skipped"; Version = $installedVersion }
    }

    # Restaurar desde backup si -Force
    if ($ForceRecreate) {
        $backupPath = "$slnPath.bak"
        if (Test-Path $backupPath) {
            $content = Get-Content $backupPath -Raw -Encoding UTF8
            Write-Host "  [OK] Restaurado desde backup" -ForegroundColor Green
        } else {
            # Limpiar Solution Folders existentes
            $content = $content -replace "(?s)Project\(`"$([regex]::Escape($sfGuid))`"\).*?EndProject\r?\n?", ""
            $content = $content -replace "(?s)\tGlobalSection\(NestedProjects\).*?EndGlobalSection\r?\n?", ""
            $content = $content -replace "# STIC-Integracion v[\d\.]+\r?\n?", ""
        }
    }

    # Crear backup
    $backupPath = "$slnPath.bak"
    if (-not (Test-Path $backupPath)) {
        Copy-Item $slnPath $backupPath -Force
        Write-Host "  [OK] Backup creado" -ForegroundColor Gray
    }

    # ===============================================================
    # CONSTRUIR SOLUTION FOLDERS
    # ===============================================================

    $projects = ""
    $nested = @()
    $stats = @{ Folders = 0; Files = 0 }

    # Crear carpeta contenedora _STIC
    $containerGuid = New-Guid
    $projects += @"

Project("$sfGuid") = "$ContainerName", "$ContainerName", "$containerGuid"
EndProject
"@
    $stats.Folders++

    # Procesar cada carpeta de la estructura
    foreach ($def in $folderStructure) {
        $name = $def.Name
        Write-Host "  Procesando: $name" -ForegroundColor Cyan

        # Caso especial: archivos en raiz
        if ($null -eq $def.Path) {
            $rootFiles = $def.Files | Where-Object { Test-Path $_ } | ForEach-Object { "${prefix}$_" }
            if ($rootFiles.Count -gt 0) {
                $guid = New-Guid
                $items = ($rootFiles | ForEach-Object { "`t`t$_ = $_" }) -join "`r`n"
                $projects += @"

Project("$sfGuid") = "$name", "$name", "$guid"
	ProjectSection(SolutionItems) = preProject
$items
	EndProjectSection
EndProject
"@
                $nested += "`t`t$guid = $containerGuid"
                $stats.Folders++
                $stats.Files += $rootFiles.Count
                Write-Host "    [OK] $($rootFiles.Count) archivos" -ForegroundColor Green
            }
            continue
        }

        # Verificar si existe
        if (-not (Test-Path $def.Path)) {
            Write-Host "    [--] No existe" -ForegroundColor DarkGray
            continue
        }

        # Obtener archivos
        $data = Get-FolderFiles `
            -BasePath $def.Path `
            -Prefix $prefix `
            -Filter $(if ($def.Filter) { $def.Filter } else { "*" }) `
            -IncludePS1 $(if ($null -ne $def.IncludePS1) { $def.IncludePS1 } else { $false }) `
            -ExtraFiles $(if ($def.ExtraFiles) { $def.ExtraFiles } else { @() })

        # Crear carpeta principal
        $mainGuid = New-Guid
        $mainFiles = $data.Files[""]
        $mainItems = if ($mainFiles) { ($mainFiles | ForEach-Object { "`t`t$_ = $_" }) -join "`r`n" } else { "" }

        $projects += @"

Project("$sfGuid") = "$name", "$name", "$mainGuid"
	ProjectSection(SolutionItems) = preProject
$mainItems
	EndProjectSection
EndProject
"@
        $nested += "`t`t$mainGuid = $containerGuid"
        $stats.Folders++
        if ($mainFiles) { $stats.Files += $mainFiles.Count }

        # Crear subcarpetas
        foreach ($sub in $data.Folders.Keys | Sort-Object) {
            $subGuid = $data.Folders[$sub]
            $subName = Split-Path $sub -Leaf
            $subFiles = $data.Files[$sub]
            $subItems = if ($subFiles) { ($subFiles | ForEach-Object { "`t`t$_ = $_" }) -join "`r`n" } else { "" }

            $projects += @"

Project("$sfGuid") = "$subName", "$subName", "$subGuid"
	ProjectSection(SolutionItems) = preProject
$subItems
	EndProjectSection
EndProject
"@
            # Determinar padre
            $parentKey = $data.Parents[$sub]
            $parentGuid = if ($parentKey) { $data.Folders[$parentKey] } else { $mainGuid }
            $nested += "`t`t$subGuid = $parentGuid"

            $stats.Folders++
            if ($subFiles) { $stats.Files += $subFiles.Count }
        }

        $subCount = $data.Folders.Count
        $fileCountMeasure = ($data.Files.Values | ForEach-Object { $_.Count } | Measure-Object -Sum)
        $fileCount = if ($fileCountMeasure.Sum) { $fileCountMeasure.Sum } else { 0 }
        Write-Host "    [OK] $subCount subcarpetas, $fileCount archivos" -ForegroundColor Green
    }

    # ===============================================================
    # INSERTAR EN .SLN
    # ===============================================================

    # Insertar comentario de version al inicio
    if ($content -notmatch "^#") {
        $content = "$VersionMarker`r`n$content"
    }

    # Insertar proyectos antes de Global
    $lines = $content -split "`r?`n"
    $globalIdx = 0
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "^Global\s*$") { $globalIdx = $i; break }
    }

    if ($globalIdx -eq 0) {
        Write-Host "  [ERROR] No se encontro seccion Global" -ForegroundColor Red
        return @{ Name = $slnName; Status = "Error" }
    }

    $before = $lines[0..($globalIdx - 1)] -join "`r`n"
    $after = $lines[$globalIdx..($lines.Count - 1)] -join "`r`n"

    # Insertar NestedProjects
    if ($nested.Count -gt 0) {
        $nestedSection = @"
	GlobalSection(NestedProjects) = preSolution
$($nested -join "`r`n")
	EndGlobalSection
"@
        $after = $after -replace "(EndGlobal\s*)$", "$nestedSection`r`n`$1"
    }

    $content = $before + $projects + "`r`n" + $after

    # Guardar
    Set-Content $slnPath $content -Encoding UTF8 -NoNewline

    Write-Host ""
    Write-Host "  [OK] $($stats.Folders) carpetas, $($stats.Files) archivos" -ForegroundColor Green

    return @{ Name = $slnName; Status = "Updated"; Folders = $stats.Folders; Files = $stats.Files }
}

# ===============================================================
# FUNCIÓN PARA PROCESAR .SLNX (FORMATO XML PLANO)
# Visual Studio requiere formato plano con nombres jerárquicos:
#   <Folder Name="/_STIC.IA/" />
#   <Folder Name="/_STIC.IA/Raiz/"> ... </Folder>
#   <Folder Name="/_STIC.IA/skills/azure-config/"> ... </Folder>
# ===============================================================
function Process-SolutionSlnx {
    param([System.IO.FileInfo]$SolutionFile, [switch]$ForceRecreate)

    $slnxPath = $SolutionFile.FullName
    $slnxName = $SolutionFile.Name

    Write-Host ""
    Write-Host "+-------------------------------------------------------------------+" -ForegroundColor White
    Write-Host "| PROCESANDO (SLNX): $($slnxName.PadRight(43)) |" -ForegroundColor White
    Write-Host "+-------------------------------------------------------------------+" -ForegroundColor White

    # Calcular prefijo relativo
    $projectRoot = (Get-Location).Path
    $slnxFolder = Split-Path $slnxPath -Parent
    $prefix = Get-RelativePrefix -SlnFolder $slnxFolder -ProjectRoot $projectRoot

    Write-Host "  Prefijo relativo: '$prefix'" -ForegroundColor Gray

    # Cargar XML
    [xml]$xml = Get-Content $slnxPath -Raw -Encoding UTF8

    # Verificar si ya tiene STIC.IA (buscar carpetas que empiecen con /_STIC.IA)
    $existingSTIC = $xml.Solution.Folder | Where-Object { $_.Name -like "/$ContainerName/*" -or $_.Name -eq "/$ContainerName/" }
    if ($existingSTIC -and -not $ForceRecreate) {
        Write-Host "  [SKIP] Ya tiene carpetas $ContainerName. Usa -Force para actualizar." -ForegroundColor Yellow
        return @{ Name = $slnxName; Status = "Skipped"; Version = "slnx" }
    }

    # Crear backup
    $backupPath = "$slnxPath.bak"
    if (-not (Test-Path $backupPath)) {
        Copy-Item $slnxPath $backupPath -Force
        Write-Host "  [OK] Backup creado" -ForegroundColor Gray
    }

    # Si -Force, restaurar desde backup o eliminar carpetas STIC existentes
    if ($ForceRecreate) {
        if (Test-Path $backupPath) {
            [xml]$xml = Get-Content $backupPath -Raw -Encoding UTF8
            Write-Host "  [OK] Restaurado desde backup" -ForegroundColor Green
        } else {
            # Eliminar carpetas STIC.IA existentes (formato plano: /_STIC.IA/*, /_STIC.IA/)
            $foldersToRemove = @($xml.Solution.Folder | Where-Object {
                $_.Name -like "/$ContainerName/*" -or
                $_.Name -eq "/$ContainerName/" -or
                $_.Name -eq "/Contexto Claude/" -or
                $_.Name -eq "/Documentacion/"
            })
            foreach ($folder in $foldersToRemove) {
                if ($folder) {
                    $xml.Solution.RemoveChild($folder) | Out-Null
                }
            }
            Write-Host "  [OK] Carpetas anteriores eliminadas" -ForegroundColor Green
        }
    }

    # ===============================================================
    # CONSTRUIR ESTRUCTURA XML PLANA
    # ===============================================================

    $stats = @{ Folders = 0; Files = 0 }
    $foldersToInsert = @()  # Lista de carpetas a insertar en orden

    # 1. Carpeta contenedora _STIC.IA (vacía)
    $sticContainer = $xml.CreateElement("Folder")
    $sticContainer.SetAttribute("Name", "/$ContainerName/")
    $foldersToInsert += $sticContainer
    $stats.Folders++

    # 2. Procesar cada carpeta de la estructura
    foreach ($def in $folderStructure) {
        $name = $def.Name
        Write-Host "  Procesando: $name" -ForegroundColor Cyan

        # Caso especial: archivos en raiz
        if ($null -eq $def.Path) {
            $rootFiles = $def.Files | Where-Object { Test-Path $_ }
            if ($rootFiles.Count -gt 0) {
                # Crear carpeta plana: /_STIC.IA/Raiz/
                $flatFolder = $xml.CreateElement("Folder")
                $flatFolder.SetAttribute("Name", "/$ContainerName/$name/")

                foreach ($file in $rootFiles) {
                    $fileElement = $xml.CreateElement("File")
                    $fileElement.SetAttribute("Path", "${prefix}$file")
                    $flatFolder.AppendChild($fileElement) | Out-Null
                    $stats.Files++
                }

                $foldersToInsert += $flatFolder
                $stats.Folders++
                Write-Host "    [OK] $($rootFiles.Count) archivos" -ForegroundColor Green
            }
            continue
        }

        # Verificar si existe la carpeta
        if (-not (Test-Path $def.Path)) {
            Write-Host "    [--] No existe" -ForegroundColor DarkGray
            continue
        }

        # Obtener archivos
        $data = Get-FolderFiles `
            -BasePath $def.Path `
            -Prefix $prefix `
            -Filter $(if ($def.Filter) { $def.Filter } else { "*" }) `
            -IncludePS1 $(if ($null -ne $def.IncludePS1) { $def.IncludePS1 } else { $false }) `
            -ExtraFiles $(if ($def.ExtraFiles) { $def.ExtraFiles } else { @() })

        # Crear carpeta principal plana: /_STIC.IA/{name}/
        $mainFolder = $xml.CreateElement("Folder")
        $mainFolder.SetAttribute("Name", "/$ContainerName/$name/")

        # Añadir archivos de la carpeta principal
        $mainFiles = $data.Files[""]
        if ($mainFiles) {
            foreach ($file in $mainFiles) {
                $fileElement = $xml.CreateElement("File")
                $fileElement.SetAttribute("Path", $file)
                $mainFolder.AppendChild($fileElement) | Out-Null
                $stats.Files++
            }
        }

        $foldersToInsert += $mainFolder
        $stats.Folders++

        # Crear subcarpetas en formato plano con nombres jerárquicos
        # Ej: /_STIC.IA/skills/azure-config/, /_STIC.IA/_duran/docs/
        $sortedSubs = $data.Folders.Keys | Sort-Object
        foreach ($sub in $sortedSubs) {
            # sub = "azure-config" o "patterns" etc.
            # Construir ruta plana completa
            $flatPath = $sub -replace '\\', '/'

            $subFolder = $xml.CreateElement("Folder")
            $subFolder.SetAttribute("Name", "/$ContainerName/$name/$flatPath/")

            $subFiles = $data.Files[$sub]
            if ($subFiles) {
                foreach ($file in $subFiles) {
                    $fileElement = $xml.CreateElement("File")
                    $fileElement.SetAttribute("Path", $file)
                    $subFolder.AppendChild($fileElement) | Out-Null
                    $stats.Files++
                }
            }

            $foldersToInsert += $subFolder
            $stats.Folders++
        }

        $subCount = $data.Folders.Count
        $fileCountMeasure = ($data.Files.Values | ForEach-Object { $_.Count } | Measure-Object -Sum)
        $fileCount = if ($fileCountMeasure.Sum) { $fileCountMeasure.Sum } else { 0 }
        Write-Host "    [OK] $subCount subcarpetas, $fileCount archivos" -ForegroundColor Green
    }

    # ===============================================================
    # INSERTAR CARPETAS EN EL XML
    # ===============================================================

    # Encontrar punto de inserción (después de Configurations o al principio)
    $configNode = $xml.Solution.Configurations
    $insertAfter = $configNode

    foreach ($folder in $foldersToInsert) {
        if ($insertAfter) {
            $xml.Solution.InsertAfter($folder, $insertAfter) | Out-Null
            $insertAfter = $folder
        } else {
            $firstChild = $xml.Solution.FirstChild
            if ($firstChild) {
                $xml.Solution.InsertBefore($folder, $firstChild) | Out-Null
                $insertAfter = $folder
            } else {
                $xml.Solution.AppendChild($folder) | Out-Null
                $insertAfter = $folder
            }
        }
    }

    # Guardar XML con formato
    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.IndentChars = "  "
    $settings.Encoding = [System.Text.Encoding]::UTF8

    $writer = [System.Xml.XmlWriter]::Create($slnxPath, $settings)
    $xml.Save($writer)
    $writer.Close()

    Write-Host ""
    Write-Host "  [OK] $($stats.Folders) carpetas, $($stats.Files) archivos (formato plano)" -ForegroundColor Green

    return @{ Name = $slnxName; Status = "Updated"; Folders = $stats.Folders; Files = $stats.Files }
}

# ===============================================================
# SELECCION DE SOLUCIONES
# ===============================================================
$toProcess = @()

if ($Solution) {
    $found = $allSolutions | Where-Object { $_.Name -eq $Solution }
    if ($found) { $toProcess = @($found) }
    else {
        Write-Host "[ERROR] Solucion '$Solution' no encontrada" -ForegroundColor Red
        exit 1
    }
}
elseif ($All) {
    $toProcess = $allSolutions
    Write-Host "[INFO] Procesando todas las soluciones" -ForegroundColor Cyan
}
elseif ($allSolutions.Count -eq 1) {
    $toProcess = $allSolutions
}
else {
    Write-Host ""
    Write-Host "Soluciones encontradas:" -ForegroundColor White
    for ($i = 0; $i -lt $allSolutions.Count; $i++) {
        Write-Host "  [$($i + 1)] $($allSolutions[$i].Name)" -ForegroundColor Cyan
    }
    Write-Host "  [A] Todas" -ForegroundColor Green
    Write-Host "  [Q] Cancelar" -ForegroundColor Gray
    Write-Host ""

    $sel = Read-Host "Selecciona"

    if ($sel -eq "Q" -or $sel -eq "q") { exit 0 }
    elseif ($sel -eq "A" -or $sel -eq "a") { $toProcess = $allSolutions }
    elseif ($sel -match "^\d+$" -and [int]$sel -ge 1 -and [int]$sel -le $allSolutions.Count) {
        $toProcess = @($allSolutions[[int]$sel - 1])
    }
}

# ===============================================================
# PROCESAR (DETECTAR EXTENSIÓN)
# ===============================================================
$results = @()
foreach ($sln in $toProcess) {
    if ($sln.Extension -eq ".slnx") {
        $results += Process-SolutionSlnx -SolutionFile $sln -ForceRecreate:$Force
    } else {
        $results += Process-Solution -SolutionFile $sln -ForceRecreate:$Force
    }
}

# Limpiar nul
@("nul", "03_Desarrollo\nul") | Where-Object { Test-Path $_ } | ForEach-Object {
    Remove-Item $_ -Force -ErrorAction SilentlyContinue
}

# ===============================================================
# RESUMEN
# ===============================================================
Write-Host ""
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "  COMPLETADO - Solution Folders v$ScriptVersion" -ForegroundColor Green
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""

foreach ($r in $results) {
    $icon = if ($r.Status -eq "Updated") { "[OK]" } elseif ($r.Status -eq "Skipped") { "[--]" } else { "[!!]" }
    $color = if ($r.Status -eq "Updated") { "Green" } elseif ($r.Status -eq "Skipped") { "Yellow" } else { "Red" }
    $info = if ($r.Folders) { "($($r.Folders) carpetas)" } elseif ($r.Version) { "(v$($r.Version))" } else { "" }
    Write-Host "  $icon $($r.Name) $info" -ForegroundColor $color
}

Write-Host ""
Write-Host "  Estructura en Visual Studio:" -ForegroundColor White
Write-Host "  $ContainerName/" -ForegroundColor Cyan
Write-Host "    +-- Raiz (CLAUDE.md, README.md)" -ForegroundColor Gray
Write-Host "    +-- skills/ (templates de codigo)" -ForegroundColor Gray
Write-Host "    +-- _duran/ (specs/, guias-uso/)" -ForegroundColor Gray
Write-Host "    +-- 00_Gestion/, 01_Diseno/, ..." -ForegroundColor Gray
Write-Host "    +-- Documentos_Base/" -ForegroundColor Gray
Write-Host ""
Write-Host "  NO incluye: commands/, rules/ (config interna Claude)" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  Para actualizar: .\.claude\commands\integracion-vs.ps1 -Force" -ForegroundColor Yellow
Write-Host ""
