# Azure AD / Microsoft Entra ID

> Skill: azure-config | Version: 3.1.0

Configuracion de autenticacion Azure AD para APIs y aplicaciones web.

---

## API - JWT Bearer

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
```

## App Registration

1. Azure Portal > App registrations > New registration
2. Redirect URI: `https://localhost:7xxx/signin-oidc`
3. API permissions: Microsoft Graph > User.Read
4. Expose an API: Application ID URI = `api://CLIENT_ID`

## Roles y Claims

```json
{
  "appRoles": [
    { "displayName": "Admin", "value": "Admin", "allowedMemberTypes": ["User"] },
    { "displayName": "Reader", "value": "Reader", "allowedMemberTypes": ["User"] }
  ]
}
```

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
    options.AddPolicy("RequireReader", p => p.RequireRole("Admin", "Reader"));
});
```

## Token para APIs downstream

```csharp
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApi(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("GraphApi", builder.Configuration.GetSection("GraphApi"))
    .AddInMemoryTokenCaches();
```

---

*Pattern v3.1.0*
