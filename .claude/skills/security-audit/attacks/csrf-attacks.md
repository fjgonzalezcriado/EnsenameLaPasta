# CSRF (Cross-Site Request Forgery)

> Skill: security-audit | Version: 3.5.0

Falsificacion de solicitudes entre sitios: un atacante fuerza al navegador del usuario a enviar peticiones no deseadas a una aplicacion donde esta autenticado.

> Ver tambien: `checklists/api-security.md`, `patterns/secure-headers.md`

---

## Como Funciona

1. Usuario autenticado en `app.comillas.edu` (cookie de sesion activa)
2. Usuario visita `sitio-malicioso.com`
3. Sitio malicioso contiene formulario oculto que envia POST a `app.comillas.edu/api/transfer`
4. El navegador incluye automaticamente la cookie de sesion
5. La peticion se ejecuta con los permisos del usuario

---

## Prevencion por Framework

### C# / .NET 10

```csharp
// Configurar Anti-Forgery en Program.cs
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "__Host-XSRF";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// En Controllers MVC - validar token automaticamente
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Transfer(TransferRequest request)
{
    // Token CSRF validado automaticamente
    await _service.TransferAsync(request);
    return Ok();
}

// En Razor Views
<form method="post" asp-action="Transfer">
    @Html.AntiForgeryToken()
    <!-- campos del formulario -->
</form>

// Para APIs con SPA: configurar cookie SameSite
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
```

### Django (Python)

```html
<form method="post">
    {% csrf_token %}
    <button type="submit">Enviar</button>
</form>
```

### Laravel (PHP)

```html
<form method="POST">
    @csrf
    <!-- campos del formulario -->
</form>
```

### Express (Node.js)

```javascript
const csrf = require('csurf');
const csrfProtection = csrf({ cookie: true });

app.post('/transfer', csrfProtection, (req, res) => {
    // Token CSRF validado
});
```

---

## Configuracion de Cookies SameSite

```http
Set-Cookie: session=abc123; SameSite=Strict; Secure; HttpOnly
```

| Valor SameSite | Comportamiento |
|----------------|----------------|
| **Strict** | Cookie nunca se envia en peticiones cross-site |
| **Lax** | Se envia en navegacion GET top-level |
| **None** | Se envia siempre (requiere Secure) |

---

## Checklist

- [ ] Anti-forgery tokens en todos los formularios POST
- [ ] `SameSite=Strict` o `Lax` en cookies de sesion
- [ ] Cabecera `Origin` validada en peticiones sensibles
- [ ] APIs REST con tokens Bearer (no cookies) son inmunes a CSRF

---

*Pattern v3.7.0*
