# Auth Detection Algorithm for Postman Collections

> **Purpose**: Detect the authentication mode of a .NET project and configure Postman collections accordingly.
> **Version**: 1.0.0

---

## 3-Step Detection Algorithm

### Step 1: SCAN - Files to Inspect

Claude MUST scan these files in order:

1. **Program.cs / Startup.cs** - Primary source of auth configuration
2. **appsettings.json** - Configuration sections (AzureAd, Authentication)
3. ***.csproj** - NuGet packages that indicate auth libraries
4. **Controllers/*.cs** - `[Authorize]` attributes, auth schemes

### Step 2: RESOLVE - Signal-to-Mode Mapping

| Signal | Auth Mode | Confidence |
|--------|-----------|------------|
| `AddMicrosoftIdentityWebApi` + section `AzureAd` | **Azure AD** | High |
| `AddAuthentication(JwtBearerDefaults)` without Azure AD section | **OAuth2/JWT** | High |
| `ApiKeyMiddleware` or `X-API-Key` header check | **API Key** | High |
| `BasicAuthenticationHandler` or `AddScheme<BasicAuth>` | **Basic Auth** | High |
| `HmacAuthenticationHandler` or `HttpSignature` | **HttpSignature** | High |
| `AddCertificateForwarding` or `ClientCertificate` | **mTLS** | Medium |
| `AddAuthentication().AddCookie()` only | **Cookie** | Medium |
| No auth middleware detected | **No Auth** | High |

#### Priority Resolution (when multiple signals present)

1. Azure AD takes precedence over generic JWT
2. HttpSignature takes precedence if `HttpSignatureClient` tables exist
3. API Key is additive (often combined with Azure AD for external clients)
4. mTLS is typically infrastructure-level (not collection-level)

### Step 3: CONFIGURE - Template Selection per Mode

| Auth Mode | Collection Auth | Pre-Request Script | Environment Variables |
|-----------|----------------|-------------------|----------------------|
| **Azure AD** | `"type": "bearer"` | `auth-prerequest-azuread.js.template` | tenantId, clientId, clientSecret, scope, azureAdTokenUrl |
| **OAuth2/JWT** | `"type": "bearer"` | Token request to `/auth/token` | tokenUrl, clientId, clientSecret, scope |
| **API Key** | `"type": "apikey"` | `auth-prerequest-apikey.js.template` | apiKey, apiKeyHeader |
| **Basic Auth** | `"type": "basic"` | `auth-prerequest-basic.js.template` | basicUsername, basicPassword |
| **HttpSignature** | `"type": "bearer"` | Custom (see `http-signature-mtls.md`) | httpSignaturePrivateKey, httpSignatureAlgorithm |
| **mTLS** | Certificate-based | N/A (Postman Settings) | pfxPath, pfxPassword |
| **No Auth** | `"type": "noauth"` | None | N/A |

---

## Detection Code Patterns

### Azure AD Detection

```csharp
// Signal: AddMicrosoftIdentityWebApi in Program.cs/Startup.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Confirm: AzureAd section in appsettings.json
"AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "...",
    "ClientId": "...",
    "Audience": "api://..."
}
```

### API Key Detection

```csharp
// Signal: Custom middleware or attribute
[ApiKeyAuth]
public class MyController { }

// Or in Program.cs
app.UseMiddleware<ApiKeyMiddleware>();

// Or header check in code
var apiKey = context.Request.Headers["X-API-Key"];
```

### Basic Auth Detection

```csharp
// Signal: Basic auth handler registration
builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
        "BasicAuthentication", null);
```

### HttpSignature Detection

```csharp
// Signal: Custom handler with signature verification
builder.Services.AddAuthentication()
    .AddScheme<HmacAuthenticationOptions, HmacAuthenticationHandler>(...);

// Or table with HttpSignatureClient keys
// HttpSignatureClientPublic / HttpSignatureClientPrivate
```

---

## Applying to Collection

After detection, Claude should:

1. Set collection-level `auth` object matching the detected mode
2. Add the appropriate pre-request script at collection level
3. Generate environment files with all required auth variables per environment (Dev/Pre/Pro)
4. Document the auth mode in the collection description
5. Add a "Health Check" request WITHOUT auth as the first request in the collection
6. Add an "Auth Test" folder with requests that validate the auth configuration

---

*Pattern v1.0.0 - Auth Detection for Postman Collections*
