# Clickjacking (Secuestro de Clics)

> Skill: security-audit | Version: 3.5.0

Ataque donde un sitio malicioso carga la aplicacion victima en un iframe invisible, engañando al usuario para que haga clic en elementos ocultos.

> Ver tambien: `patterns/secure-headers.md`, `owasp/owasp-top10-2025.md` (A02)

---

## Deteccion

```html
<!-- Pagina de prueba: si el sitio carga en el iframe = VULNERABLE -->
<html>
<body>
    <h1>Test de Clickjacking</h1>
    <iframe src="https://tu-aplicacion.comillas.edu"
            width="800" height="600"
            style="opacity: 0.5;">
    </iframe>
</body>
</html>
<!-- Si se muestra el contenido del iframe, falta proteccion -->
```

---

## Prevencion

### C# / .NET 10

```csharp
// Opcion 1: Middleware de cabeceras (recomendado)
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors 'none'");
    await next();
});

// Opcion 2: En controlador especifico (si algunas paginas permiten iframe)
[HttpGet]
public IActionResult EmbeddablePage()
{
    Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    Response.Headers.Append("Content-Security-Policy",
        "frame-ancestors 'self' https://portal.comillas.edu");
    return View();
}
```

### Cabeceras HTTP

```http
# Bloquear completamente el enmarcado
X-Frame-Options: DENY
Content-Security-Policy: frame-ancestors 'none';

# Permitir solo mismo origen
X-Frame-Options: SAMEORIGIN
Content-Security-Policy: frame-ancestors 'self';

# Permitir dominios especificos
Content-Security-Policy: frame-ancestors 'self' https://portal.comillas.edu;
```

### HTML5 Sandbox (para iframes que TU incluyes)

```html
<!-- Restringir capacidades del iframe -->
<iframe src="https://externo.com"
        sandbox="allow-scripts allow-same-origin"
        width="500" height="300">
</iframe>
```

---

## Diferencia entre X-Frame-Options y CSP frame-ancestors

| Cabecera | Soporte | Granularidad |
|----------|---------|--------------|
| X-Frame-Options | Legacy + moderno | DENY o SAMEORIGIN solamente |
| CSP frame-ancestors | Moderno | Multiples dominios, 'self', 'none' |

> Recomendacion: usar **ambas** para maxima compatibilidad.

---

## Checklist

- [ ] `X-Frame-Options: DENY` en todas las respuestas
- [ ] `Content-Security-Policy: frame-ancestors 'none'` como refuerzo
- [ ] Verificar con pagina de prueba iframe que la proteccion funciona
- [ ] Si hay paginas que deben ser embebibles, usar `SAMEORIGIN` o dominios especificos

---

*Pattern v3.7.0*
