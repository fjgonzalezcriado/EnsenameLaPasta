# Cabeceras de Seguridad HTTP

> Skill: security-audit | Version: 3.5.0

Headers de seguridad obligatorios para aplicaciones web Comillas, incluyendo CSP y Cross-Origin.

> Ver tambien: `attacks/clickjacking.md`, `attacks/xss-in-depth.md`

---

## Headers Recomendados

| Header | Valor | Proposito |
|--------|-------|-----------|
| Strict-Transport-Security | max-age=31536000; includeSubDomains; preload | Forzar HTTPS |
| Content-Security-Policy | default-src 'self' | Controlar origenes |
| X-Content-Type-Options | nosniff | Evitar MIME sniffing |
| X-Frame-Options | DENY | Evitar clickjacking |
| X-XSS-Protection | 0 | Desactivar filtro XSS legacy (puede causar problemas) |
| Referrer-Policy | strict-origin-when-cross-origin | Controlar referer |
| Permissions-Policy | camera=(), microphone=(), geolocation=() | Restringir APIs |
| Cross-Origin-Opener-Policy | same-origin | Aislar contexto de navegacion |
| Cross-Origin-Resource-Policy | same-origin | Proteger recursos |
| Cross-Origin-Embedder-Policy | require-corp | Restringir embebidos |

---

## Politica de Seguridad de Contenido (CSP)

```http
# CSP estricta
Content-Security-Policy: default-src 'self'; script-src 'self'; object-src 'none'; base-uri 'none';

# Con scripts basados en nonce
Content-Security-Policy: script-src 'nonce-r4nd0m' 'strict-dynamic'; object-src 'none'; base-uri 'none';

# Prevenir enmarcado
Content-Security-Policy: frame-ancestors 'none';
```

---

## Configuraciones Inseguras a Evitar

```http
# INSEGURO - No usar estos valores
Access-Control-Allow-Origin: *
X-Permitted-Cross-Domain-Policies: all
Referrer-Policy: unsafe-url
Content-Security-Policy: default-src *; script-src 'unsafe-inline' 'unsafe-eval'
```

---

## Implementacion

### C# / .NET 10 - Middleware

```csharp
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["X-XSS-Protection"] = "0";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; object-src 'none'";
    headers["Cross-Origin-Opener-Policy"] = "same-origin";
    headers["Cross-Origin-Resource-Policy"] = "same-origin";
    await next();
});

// HSTS (solo HTTPS, fuera de desarrollo)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();
```

### Web.config (.NET 4.x)

```xml
<system.webServer>
  <httpProtocol>
    <customHeaders>
      <remove name="X-Powered-By" />
      <add name="X-Frame-Options" value="DENY" />
      <add name="X-Content-Type-Options" value="nosniff" />
      <add name="Referrer-Policy" value="strict-origin-when-cross-origin" />
      <add name="Content-Security-Policy"
           value="default-src 'self'; script-src 'self'; object-src 'none'" />
    </customHeaders>
  </httpProtocol>
</system.webServer>
```

---

*Pattern v3.7.0*
