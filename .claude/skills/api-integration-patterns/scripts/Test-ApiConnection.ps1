#Requires -Version 5.1
<#
.SYNOPSIS
    Test connection to external API

.DESCRIPTION
    Script para probar conexión a API externa con autenticación OAuth2 o API Key.
    Útil para validar credenciales y verificar que la API está accesible.

.PARAMETER ApiUrl
    URL base de la API (ej: https://api.comillas.edu)

.PARAMETER Endpoint
    Endpoint específico a probar (ej: /api/v1/health)

.PARAMETER AuthType
    Tipo de autenticación: OAuth2, ApiKey, None

.PARAMETER ClientId
    Client ID para OAuth2

.PARAMETER ClientSecret
    Client Secret para OAuth2

.PARAMETER TokenUrl
    URL para obtener token OAuth2

.PARAMETER ApiKey
    API Key para autenticación simple

.EXAMPLE
    .\Test-ApiConnection.ps1 -ApiUrl "https://banner.comillas.edu/api" -Endpoint "/health" -AuthType None

.EXAMPLE
    .\Test-ApiConnection.ps1 -ApiUrl "https://banner.comillas.edu/api" -Endpoint "/students" -AuthType OAuth2 -ClientId "comillas-app" -ClientSecret "secret" -TokenUrl "https://auth.comillas.edu/oauth/token"

.EXAMPLE
    .\Test-ApiConnection.ps1 -ApiUrl "https://api.external.com" -Endpoint "/data" -AuthType ApiKey -ApiKey "sk-1234567890"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ApiUrl,

    [Parameter(Mandatory = $true)]
    [string]$Endpoint,

    [Parameter(Mandatory = $false)]
    [ValidateSet("None", "OAuth2", "ApiKey")]
    [string]$AuthType = "None",

    [Parameter(Mandatory = $false)]
    [string]$ClientId,

    [Parameter(Mandatory = $false)]
    [string]$ClientSecret,

    [Parameter(Mandatory = $false)]
    [string]$TokenUrl,

    [Parameter(Mandatory = $false)]
    [string]$ApiKey,

    [Parameter(Mandatory = $false)]
    [int]$TimeoutSeconds = 30
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

function Get-OAuth2Token {
    param(
        [string]$TokenUrl,
        [string]$ClientId,
        [string]$ClientSecret
    )

    Write-ColorOutput "`n[OAuth2] Obteniendo token..." "Cyan"
    Write-ColorOutput "Token URL: $TokenUrl" "Gray"

    $body = @{
        grant_type    = "client_credentials"
        client_id     = $ClientId
        client_secret = $ClientSecret
    }

    try {
        $response = Invoke-RestMethod -Uri $TokenUrl -Method Post -Body $body -ContentType "application/x-www-form-urlencoded" -TimeoutSec $TimeoutSeconds

        Write-ColorOutput "[OAuth2] ✅ Token obtenido correctamente" "Green"
        Write-ColorOutput "Token Type: $($response.token_type)" "Gray"
        Write-ColorOutput "Expires In: $($response.expires_in) segundos" "Gray"

        return $response.access_token
    }
    catch {
        Write-ColorOutput "[OAuth2] ❌ Error al obtener token" "Red"
        Write-ColorOutput "Error: $($_.Exception.Message)" "Red"
        throw
    }
}

function Test-ApiEndpoint {
    param(
        [string]$FullUrl,
        [hashtable]$Headers,
        [int]$TimeoutSeconds
    )

    Write-ColorOutput "`n[API] Llamando endpoint..." "Cyan"
    Write-ColorOutput "URL: $FullUrl" "Gray"
    Write-ColorOutput "Headers: $($Headers.Keys -join ', ')" "Gray"

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        $response = Invoke-WebRequest -Uri $FullUrl -Method Get -Headers $Headers -TimeoutSec $TimeoutSeconds

        $stopwatch.Stop()

        Write-ColorOutput "`n[API] ✅ Llamada exitosa" "Green"
        Write-ColorOutput "Status Code: $($response.StatusCode) $($response.StatusDescription)" "Green"
        Write-ColorOutput "Tiempo: $($stopwatch.ElapsedMilliseconds)ms" "Gray"
        Write-ColorOutput "Content-Type: $($response.Headers['Content-Type'])" "Gray"
        Write-ColorOutput "Content-Length: $($response.RawContentLength) bytes" "Gray"

        # Mostrar primeros 500 caracteres del body
        if ($response.Content.Length -gt 0) {
            Write-ColorOutput "`nResponse Body (primeros 500 chars):" "Cyan"
            $preview = $response.Content.Substring(0, [Math]::Min(500, $response.Content.Length))
            Write-ColorOutput $preview "White"

            if ($response.Content.Length > 500) {
                Write-ColorOutput "..." "Gray"
            }
        }

        return $true
    }
    catch {
        $stopwatch.Stop()

        Write-ColorOutput "`n[API] ❌ Error en llamada" "Red"
        Write-ColorOutput "Error: $($_.Exception.Message)" "Red"

        if ($_.Exception.Response) {
            $statusCode = $_.Exception.Response.StatusCode.value__
            Write-ColorOutput "Status Code: $statusCode" "Red"

            # Leer body de error si existe
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $errorBody = $reader.ReadToEnd()
            $reader.Close()

            if ($errorBody) {
                Write-ColorOutput "`nError Body:" "Red"
                Write-ColorOutput $errorBody "Red"
            }
        }

        return $false
    }
}

# ============================================================================
# MAIN
# ============================================================================

Write-ColorOutput "╔══════════════════════════════════════════════════════════╗" "Cyan"
Write-ColorOutput "║  TEST DE CONEXIÓN API                                    ║" "Cyan"
Write-ColorOutput "╚══════════════════════════════════════════════════════════╝" "Cyan"

Write-ColorOutput "`nConfiguración:" "Yellow"
Write-ColorOutput "API URL: $ApiUrl" "Gray"
Write-ColorOutput "Endpoint: $Endpoint" "Gray"
Write-ColorOutput "Auth Type: $AuthType" "Gray"
Write-ColorOutput "Timeout: ${TimeoutSeconds}s" "Gray"

# Preparar headers
$headers = @{
    "User-Agent" = "Comillas-API-Test/1.0"
    "Accept"     = "application/json"
}

# Manejar autenticación
switch ($AuthType) {
    "OAuth2" {
        if (-not $ClientId -or -not $ClientSecret -or -not $TokenUrl) {
            Write-ColorOutput "`n❌ Error: OAuth2 requiere ClientId, ClientSecret y TokenUrl" "Red"
            exit 1
        }

        $token = Get-OAuth2Token -TokenUrl $TokenUrl -ClientId $ClientId -ClientSecret $ClientSecret
        $headers["Authorization"] = "Bearer $token"
    }
    "ApiKey" {
        if (-not $ApiKey) {
            Write-ColorOutput "`n❌ Error: ApiKey es requerido para AuthType=ApiKey" "Red"
            exit 1
        }

        $headers["X-API-Key"] = $ApiKey
    }
    "None" {
        Write-ColorOutput "`n[INFO] Sin autenticación" "Yellow"
    }
}

# Construir URL completa
$fullUrl = "$ApiUrl$Endpoint"

# Probar endpoint
$success = Test-ApiEndpoint -FullUrl $fullUrl -Headers $headers -TimeoutSeconds $TimeoutSeconds

# Resultado final
Write-ColorOutput "`n╔══════════════════════════════════════════════════════════╗" "Cyan"
if ($success) {
    Write-ColorOutput "║  ✅ TEST EXITOSO                                         ║" "Green"
} else {
    Write-ColorOutput "║  ❌ TEST FALLIDO                                         ║" "Red"
}
Write-ColorOutput "╚══════════════════════════════════════════════════════════╝" "Cyan"

exit ($success ? 0 : 1)
