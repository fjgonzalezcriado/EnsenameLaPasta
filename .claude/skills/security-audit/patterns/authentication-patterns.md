# Patrones de Autenticacion Azure AD

> Skill: security-audit | Version: 3.1.0

Configuracion de Azure AD / Microsoft Entra ID para aplicaciones .NET 10.

---

## JWT Bearer Setup

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
    options.AddPolicy("RequireReader", p => p.RequireClaim("groups", "readers-group-id"));
});
```

## appsettings.json

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "TENANT_ID",
    "ClientId": "CLIENT_ID",
    "Audience": "api://CLIENT_ID"
  }
}
```

## Multi-tenant

```csharp
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApi(options =>
    {
        options.TokenValidationParameters.ValidIssuers = new[]
        {
            "https://login.microsoftonline.com/TENANT_1/v2.0",
            "https://login.microsoftonline.com/TENANT_2/v2.0"
        };
    }, options => builder.Configuration.Bind("AzureAd", options));
```

---

*Pattern v3.1.0*
