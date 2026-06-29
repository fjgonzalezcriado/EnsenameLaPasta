# Pattern: OAuth2 Client Credentials Flow

> **Pattern**: OAuth2 Client Credentials
> **Problema**: Autenticación máquina-a-máquina
> **Solución**: Token Bearer renovado automáticamente

---

## 🎯 Problema

APIs requieren autenticación segura:
- **API Keys estáticas** - inseguras, sin expiración
- **Basic Auth** - credenciales en cada request
- **Tokens manuales** - hay que renovarlos manualmente

```csharp
// ❌ NUNCA: API Key hardcodeada
request.Headers.Add("X-API-Key", "sk-1234567890abcdef");

// ❌ NUNCA: Basic Auth con password en claro
request.Headers.Authorization = new AuthenticationHeaderValue(
    "Basic",
    Convert.ToBase64String(Encoding.UTF8.GetBytes("user:password")));
```

---

## ✅ Solución: OAuth2 Client Credentials

### Flow Diagram

```
┌──────────────┐                                      ┌──────────────┐
│              │  1. POST /token                      │              │
│              │     client_id=xxx                    │              │
│   Tu App     │     client_secret=yyy               │   Auth       │
│              │     grant_type=client_credentials    │   Server     │
│              │                                      │              │
│              │  ←─────────────────────────────────  │              │
│              │  2. { "access_token": "...",         │              │
│              │       "expires_in": 3600 }           │              │
└──────┬───────┘                                      └──────────────┘
       │
       │  3. GET /api/students
       │     Authorization: Bearer eyJhbGc...
       ▼
┌──────────────┐
│              │
│   API        │
│   Externa    │
│              │
└──────────────┘
```

---

## 1. Implementación Manual

### Token Service

```csharp
public interface ITokenService
{
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);
}

public class TokenService : ITokenService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TokenService> _logger;
    private readonly TokenOptions _options;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public TokenService(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<TokenOptions> options,
        ILogger<TokenService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        // 1. Intentar obtener token desde caché
        if (_cache.TryGetValue("access_token", out string? cachedToken))
        {
            _logger.LogDebug("Token obtenido desde caché");
            return cachedToken!;
        }

        // 2. Si no hay token en caché, obtener uno nuevo (thread-safe)
        await _semaphore.WaitAsync(ct);
        try
        {
            // Double-check después del lock
            if (_cache.TryGetValue("access_token", out cachedToken))
                return cachedToken!;

            _logger.LogInformation("Obteniendo nuevo token de {TokenUrl}", _options.TokenUrl);

            var tokenResponse = await RequestTokenAsync(ct);

            // 3. Cachear con expiración (90% del expires_in para renovar antes)
            var cacheExpiration = TimeSpan.FromSeconds(tokenResponse.ExpiresIn * 0.9);
            _cache.Set("access_token", tokenResponse.AccessToken, cacheExpiration);

            _logger.LogInformation("Token obtenido y cacheado por {Expiration}s", cacheExpiration.TotalSeconds);

            return tokenResponse.AccessToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<TokenResponse> RequestTokenAsync(CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["scope"] = _options.Scope ?? string.Empty
            })
        };

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);

        if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            throw new InvalidOperationException("Token response inválido");

        return tokenResponse;
    }
}

public record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string TokenType);

public class TokenOptions
{
    public string TokenUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string? Scope { get; set; }
}
```

---

## 2. DelegatingHandler para Inyectar Token

### AuthenticationHandler

```csharp
public class BearerTokenHandler : DelegatingHandler
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<BearerTokenHandler> _logger;

    public BearerTokenHandler(ITokenService tokenService, ILogger<BearerTokenHandler> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 1. Obtener token (desde caché o renovar)
        var token = await _tokenService.GetAccessTokenAsync(cancellationToken);

        // 2. Inyectar en Authorization header
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _logger.LogDebug("Token inyectado en request a {Uri}", request.RequestUri);

        // 3. Enviar request
        var response = await base.SendAsync(request, cancellationToken);

        // 4. Si 401, invalidar caché y reintentar una vez
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Token rechazado (401), invalidando caché y reintentando");

            // Invalidar caché para forzar renovación
            // (implementar IMemoryCache.Remove en TokenService)

            token = await _tokenService.GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }
}
```

---

## 3. Configuración en Program.cs

### Setup Completo

```csharp
// Token Service con HttpClient propio
builder.Services.AddHttpClient<ITokenService, TokenService>(client =>
{
    // NO configurar BaseAddress aquí, se usa TokenUrl de opciones
});

// Opciones desde configuración
builder.Services.Configure<TokenOptions>(
    builder.Configuration.GetSection("BannerApi:OAuth"));

// Handler con inyección de token
builder.Services.AddTransient<BearerTokenHandler>();

// HttpClient de Banner con handler
builder.Services.AddHttpClient<IBannerClient, BannerClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BannerApi:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<BearerTokenHandler>();

// Memory Cache para almacenar tokens
builder.Services.AddMemoryCache();
```

### appsettings.json

```json
{
  "BannerApi": {
    "BaseUrl": "https://banner.comillas.edu/api",
    "OAuth": {
      "TokenUrl": "https://auth.comillas.edu/oauth/token",
      "ClientId": "comillas-app",
      "ClientSecret": "{{KeyVault}}",
      "Scope": "students:read courses:read"
    }
  }
}
```

---

## 4. Azure AD / Microsoft Entra ID

### Usando Microsoft.Identity.Web

```csharp
// Program.cs
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");

builder.Services.AddHttpClient<IMicrosoftGraphClient, MicrosoftGraphClient>(client =>
{
    client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
})
.AddMicrosoftGraphAppAuthenticationHandler(options =>
{
    options.Scopes = new[] { "https://graph.microsoft.com/.default" };
});
```

### appsettings.json (Azure AD)

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "{{KeyVault}}"
  }
}
```

---

## 5. Configuración Avanzada

### Múltiples Scopes

```csharp
var tokenRequest = new Dictionary<string, string>
{
    ["grant_type"] = "client_credentials",
    ["client_id"] = clientId,
    ["client_secret"] = clientSecret,
    ["scope"] = "students:read students:write courses:read"
};
```

### Refresh Token (si aplica)

```csharp
// Nota: Client Credentials NO usa refresh tokens
// Para user flows (Authorization Code), sí hay refresh tokens

if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
{
    _cache.Set("refresh_token", tokenResponse.RefreshToken, TimeSpan.FromDays(30));
}
```

---

## 🔒 Seguridad

### ✅ Buenas Prácticas

```csharp
// ✅ Client Secret en Azure Key Vault
var secretClient = new SecretClient(
    new Uri("https://your-vault.vault.azure.net/"),
    new DefaultAzureCredential());

var secret = await secretClient.GetSecretAsync("BannerApiClientSecret");
builder.Configuration["BannerApi:OAuth:ClientSecret"] = secret.Value.Value;

// ✅ Tokens en memoria (IMemoryCache), NUNCA en disco
// ✅ HTTPS obligatorio para token endpoint
// ✅ Scope mínimo necesario (least privilege)
```

### ❌ Errores Comunes

```csharp
// ❌ Token en variable estática (no se renueva)
private static string _cachedToken;

// ❌ Client Secret en código
var clientSecret = "1234567890abcdef";

// ❌ HTTP sin TLS
var tokenUrl = "http://auth.comillas.edu/oauth/token"; // ❌
```

---

## 🧪 Testing

### Mock Token Service

```csharp
public class MockTokenService : ITokenService
{
    public Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        return Task.FromResult("mock-token-12345");
    }
}

// En tests
services.AddSingleton<ITokenService, MockTokenService>();
```

---

## 📚 Referencias

- [OAuth 2.0 Client Credentials](https://oauth.net/2/grant-types/client-credentials/)
- [Microsoft Identity Platform](https://learn.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow)

---

*Pattern: oauth2-client-credentials - STIC.IA v3.7.0*
