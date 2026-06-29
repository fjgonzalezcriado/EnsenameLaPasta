# OWASP Top 10 - .NET 10

> Skill: security-audit | Version: 3.5.0

Las 10 vulnerabilidades mas criticas aplicadas a .NET 10.

> Ver tambien: `owasp/owasp-top10-2025.md` para la referencia completa multi-lenguaje OWASP 2025.

---

## A01: Broken Access Control

**Mitigacion:** Usar `[Authorize]` con politicas, nunca confiar en datos del cliente.
```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", p => p.RequireRole("Admin"))
    .AddPolicy("Owner", p => p.AddRequirements(new OwnerRequirement()));
```

## A02: Cryptographic Failures

**Mitigacion:** Usar Data Protection API, nunca algoritmos propios.
```csharp
builder.Services.AddDataProtection()
    .PersistKeysToAzureBlobStorage(blobUri)
    .ProtectKeysWithAzureKeyVault(keyId, credential);
```

## A03: Injection

**Mitigacion:** Siempre parametrizar queries, usar EF Core.
```csharp
// MAL: context.Database.ExecuteSqlRaw($"SELECT * WHERE Name = '{input}'");
// BIEN:
context.Database.ExecuteSqlInterpolated($"SELECT * WHERE Name = {input}");
```

## A04: Insecure Design

**Mitigacion:** Validacion en multiples capas, modelado de amenazas, rate limiting.
```csharp
// Validacion de dominio - limites de negocio
if (request.Importe > 100_000)
    return Result.Failure("Importe maximo excedido");

// Rate limiting en .NET 10
builder.Services.AddRateLimiter(options =>
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    }));
```

## A05: Security Misconfiguration

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
app.UseHttpsRedirection();
```

## A06: Vulnerable Components

```bash
# Detectar vulnerabilidades en dependencias
dotnet list package --vulnerable --include-transitive
```

## A07: Authentication Failures

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
```

## A08: Data Integrity Failures

**Mitigacion:** Nunca usar BinaryFormatter, validar integridad de paquetes NuGet.
```csharp
// INSEGURO - BinaryFormatter esta obsoleto y es peligroso
// var formatter = new BinaryFormatter();
// SEGURO - Usar System.Text.Json
var data = JsonSerializer.Deserialize<MyType>(jsonString);
```

## A09: Security Logging

```csharp
logger.LogWarning("Intento de acceso no autorizado: {User} a {Resource}",
    user.Identity?.Name, context.Request.Path);
```

## A10: SSRF (Server-Side Request Forgery)

**Mitigacion:** Whitelist de dominios permitidos para peticiones salientes.
```csharp
var allowedDomains = new[] { "api.comillas.edu", "services.comillas.edu" };
var uri = new Uri(request.QueryString["url"]);
if (!allowedDomains.Contains(uri.Host))
    return BadRequest("Dominio no permitido");
```

---

*Pattern v3.7.0*
