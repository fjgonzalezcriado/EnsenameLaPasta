#Requires -Version 5.1
<#
.SYNOPSIS
    Generate C# API Client from OpenAPI/Swagger spec

.DESCRIPTION
    Script para generar cliente C# desde especificación OpenAPI usando NSwag o Kiota.
    Genera DTOs, interfaces y cliente HTTP tipado.

.PARAMETER OpenApiUrl
    URL del archivo OpenAPI/Swagger (ej: https://api.comillas.edu/swagger/v1/swagger.json)

.PARAMETER OpenApiFile
    Ruta al archivo OpenAPI/Swagger local (alternativa a OpenApiUrl)

.PARAMETER OutputPath
    Carpeta destino para archivos generados

.PARAMETER Namespace
    Namespace para las clases generadas

.PARAMETER ClientName
    Nombre del cliente generado (ej: BannerClient)

.PARAMETER Tool
    Herramienta a usar: NSwag o Kiota

.EXAMPLE
    .\Generate-ApiClient.ps1 -OpenApiUrl "https://banner.comillas.edu/api/swagger/v1/swagger.json" -OutputPath ".\Generated" -Namespace "Comillas.Banner.Client" -ClientName "BannerClient"

.EXAMPLE
    .\Generate-ApiClient.ps1 -OpenApiFile ".\swagger.json" -OutputPath ".\Generated" -Namespace "Comillas.Api" -Tool Kiota
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$OpenApiUrl,

    [Parameter(Mandatory = $false)]
    [string]$OpenApiFile,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$Namespace,

    [Parameter(Mandatory = $false)]
    [string]$ClientName = "ApiClient",

    [Parameter(Mandatory = $false)]
    [ValidateSet("NSwag", "Kiota")]
    [string]$Tool = "NSwag"
)

# ============================================================================
# FUNCIONES
# ============================================================================

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Test-ToolInstalled {
    param([string]$Command)

    try {
        $null = Get-Command $Command -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

function Install-NSwag {
    Write-ColorOutput "`n[NSwag] Instalando NSwag.ConsoleCore..." "Cyan"

    try {
        dotnet tool install --global NSwag.ConsoleCore --version 14.*
        Write-ColorOutput "[NSwag] ✅ Instalado correctamente" "Green"
    }
    catch {
        Write-ColorOutput "[NSwag] ❌ Error al instalar: $($_.Exception.Message)" "Red"
        throw
    }
}

function Install-Kiota {
    Write-ColorOutput "`n[Kiota] Instalando Kiota..." "Cyan"

    try {
        dotnet tool install --global Microsoft.OpenApi.Kiota
        Write-ColorOutput "[Kiota] ✅ Instalado correctamente" "Green"
    }
    catch {
        Write-ColorOutput "[Kiota] ❌ Error al instalar: $($_.Exception.Message)" "Red"
        throw
    }
}

function Generate-WithNSwag {
    param(
        [string]$Input,
        [string]$OutputPath,
        [string]$Namespace,
        [string]$ClientName
    )

    Write-ColorOutput "`n[NSwag] Generando cliente..." "Cyan"

    $outputFile = Join-Path $OutputPath "$ClientName.cs"

    $nswagCommand = "nswag openapi2csclient " +
                    "/input:$Input " +
                    "/output:$outputFile " +
                    "/namespace:$Namespace " +
                    "/className:$ClientName " +
                    "/generateClientInterfaces:true " +
                    "/generateDtoTypes:true " +
                    "/injectHttpClient:true " +
                    "/useBaseUrl:false"

    try {
        Invoke-Expression $nswagCommand

        if (Test-Path $outputFile) {
            Write-ColorOutput "[NSwag] ✅ Cliente generado: $outputFile" "Green"
            Write-ColorOutput "[NSwag] Tamaño: $((Get-Item $outputFile).Length / 1KB) KB" "Gray"

            # Contar líneas
            $lines = (Get-Content $outputFile).Count
            Write-ColorOutput "[NSwag] Líneas de código: $lines" "Gray"
        }
        else {
            Write-ColorOutput "[NSwag] ⚠️ Archivo no encontrado, pero comando ejecutado" "Yellow"
        }
    }
    catch {
        Write-ColorOutput "[NSwag] ❌ Error: $($_.Exception.Message)" "Red"
        throw
    }
}

function Generate-WithKiota {
    param(
        [string]$Input,
        [string]$OutputPath,
        [string]$Namespace
    )

    Write-ColorOutput "`n[Kiota] Generando cliente..." "Cyan"

    $kiotaCommand = "kiota generate " +
                    "-l CSharp " +
                    "-d $Input " +
                    "-o $OutputPath " +
                    "-n $Namespace"

    try {
        Invoke-Expression $kiotaCommand

        Write-ColorOutput "[Kiota] ✅ Cliente generado en: $OutputPath" "Green"

        # Listar archivos generados
        $files = Get-ChildItem -Path $OutputPath -Recurse -File
        Write-ColorOutput "[Kiota] Archivos generados: $($files.Count)" "Gray"

        foreach ($file in $files | Select-Object -First 10) {
            Write-ColorOutput "  - $($file.Name)" "Gray"
        }

        if ($files.Count > 10) {
            Write-ColorOutput "  ... y $($files.Count - 10) más" "Gray"
        }
    }
    catch {
        Write-ColorOutput "[Kiota] ❌ Error: $($_.Exception.Message)" "Red"
        throw
    }
}

# ============================================================================
# MAIN
# ============================================================================

Write-ColorOutput "╔══════════════════════════════════════════════════════════╗" "Cyan"
Write-ColorOutput "║  GENERADOR DE CLIENTE API                                ║" "Cyan"
Write-ColorOutput "╚══════════════════════════════════════════════════════════╝" "Cyan"

# Validar entrada
if (-not $OpenApiUrl -and -not $OpenApiFile) {
    Write-ColorOutput "`n❌ Error: Debes especificar OpenApiUrl o OpenApiFile" "Red"
    exit 1
}

if ($OpenApiUrl -and $OpenApiFile) {
    Write-ColorOutput "`n⚠️ Warning: Se especificaron ambos OpenApiUrl y OpenApiFile. Usando OpenApiUrl." "Yellow"
}

$input = if ($OpenApiUrl) { $OpenApiUrl } else { $OpenApiFile }

Write-ColorOutput "`nConfiguración:" "Yellow"
Write-ColorOutput "Input: $input" "Gray"
Write-ColorOutput "Output: $OutputPath" "Gray"
Write-ColorOutput "Namespace: $Namespace" "Gray"
Write-ColorOutput "Cliente: $ClientName" "Gray"
Write-ColorOutput "Tool: $Tool" "Gray"

# Crear carpeta de salida si no existe
if (-not (Test-Path $OutputPath)) {
    Write-ColorOutput "`n[INFO] Creando carpeta: $OutputPath" "Yellow"
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
}

# Verificar herramienta
$toolCommand = $Tool.ToLower()

if (-not (Test-ToolInstalled $toolCommand)) {
    Write-ColorOutput "`n⚠️ $Tool no está instalado. Instalando..." "Yellow"

    switch ($Tool) {
        "NSwag" { Install-NSwag }
        "Kiota" { Install-Kiota }
    }
}
else {
    Write-ColorOutput "`n✅ $Tool ya está instalado" "Green"
}

# Generar cliente
switch ($Tool) {
    "NSwag" {
        Generate-WithNSwag -Input $input -OutputPath $OutputPath -Namespace $Namespace -ClientName $ClientName
    }
    "Kiota" {
        Generate-WithKiota -Input $input -OutputPath $OutputPath -Namespace $Namespace
    }
}

# Siguiente pasos
Write-ColorOutput "`n╔══════════════════════════════════════════════════════════╗" "Cyan"
Write-ColorOutput "║  ✅ GENERACIÓN COMPLETADA                                ║" "Green"
Write-ColorOutput "╚══════════════════════════════════════════════════════════╝" "Cyan"

Write-ColorOutput "`nPróximos pasos:" "Yellow"
Write-ColorOutput "1. Revisar archivos generados en: $OutputPath" "White"
Write-ColorOutput "2. Añadir al proyecto:" "White"
Write-ColorOutput "   dotnet add reference $OutputPath" "Gray"
Write-ColorOutput "3. Registrar en Program.cs:" "White"
Write-ColorOutput "   builder.Services.AddHttpClient<I$ClientName, $ClientName>()" "Gray"
Write-ColorOutput "4. Usar en tu código:" "White"
Write-ColorOutput "   var result = await _client.GetSomethingAsync();" "Gray"

Write-ColorOutput "`n📚 Documentación:" "Yellow"
Write-ColorOutput "NSwag: https://github.com/RicoSuter/NSwag" "Gray"
Write-ColorOutput "Kiota: https://learn.microsoft.com/en-us/openapi/kiota/" "Gray"

exit 0
