# Seguridad de Metodos HTTP

> Skill: security-audit | Version: 3.5.0

Verificacion de que solo los metodos HTTP necesarios estan habilitados y los demas son rechazados.

> Ver tambien: `checklists/api-security.md`, `patterns/secure-headers.md`

---

## Pruebas

### Descubrir metodos permitidos

```bash
# OPTIONS revela metodos permitidos
curl -X OPTIONS https://ejemplo.comillas.edu -i

# Probar metodos peligrosos
curl -X DELETE https://ejemplo.comillas.edu/recurso -i
curl -X PUT https://ejemplo.comillas.edu --upload-file test.html -i
curl -X TRACE https://ejemplo.comillas.edu -i

# Probar evasion con metodo arbitrario
curl -X FOO https://ejemplo.comillas.edu/admin -i
```

### Respuesta esperada (segura)

```http
HTTP/1.1 405 Method Not Allowed
Allow: GET, HEAD, POST
```

### Respuesta insegura (problematica)

```http
HTTP/1.1 200 OK
Allow: GET, HEAD, POST, PUT, DELETE, OPTIONS, TRACE, PATCH
```

---

## Prevencion

### C# / .NET 10

```csharp
// Restringir metodos en controladores
[HttpGet]  // Solo acepta GET
[HttpPost] // Solo acepta POST
public async Task<IActionResult> MiEndpoint() { }

// Middleware para bloquear metodos no deseados globalmente
app.Use(async (context, next) =>
{
    var allowedMethods = new[] { "GET", "HEAD", "POST", "PUT", "DELETE", "OPTIONS" };
    if (!allowedMethods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = 405;
        return;
    }
    // Bloquear TRACE (prevenir XST)
    if (context.Request.Method.Equals("TRACE", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = 405;
        return;
    }
    await next();
});
```

### IIS (web.config)

```xml
<system.webServer>
  <security>
    <requestFiltering>
      <verbs>
        <add verb="TRACE" allowed="false" />
        <add verb="OPTIONS" allowed="false" />
      </verbs>
    </requestFiltering>
  </security>
</system.webServer>
```

---

## Metodos HTTP y Riesgos

| Metodo | Uso legitimo | Riesgo si esta abierto |
|--------|-------------|------------------------|
| GET | Consultar recursos | Bajo (lectura) |
| POST | Crear recursos | Medio (requiere validacion) |
| PUT | Reemplazar recursos | Alto (puede sobrescribir) |
| DELETE | Eliminar recursos | Alto (puede borrar datos) |
| PATCH | Actualizar parcialmente | Medio |
| OPTIONS | Preflight CORS | Bajo (informativo) |
| TRACE | Debug | **Alto** (XST, fuga de headers) |
| HEAD | Consultar headers | Bajo |

---

## Checklist

- [ ] Solo los metodos necesarios estan habilitados por endpoint
- [ ] TRACE esta deshabilitado en todos los servidores
- [ ] Metodos arbitrarios (FOO, BAR) devuelven 405
- [ ] OPTIONS controlado (no expone informacion innecesaria)
- [ ] PUT y DELETE protegidos con autorizacion

---

*Checklist v3.7.0*
