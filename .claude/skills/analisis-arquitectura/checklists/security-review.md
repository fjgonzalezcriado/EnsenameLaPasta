# Checklist de Revisión de Seguridad

> Skill: analisis-arquitectura
> Versión: 2.8.0

---

## Descripción

Checklist para revisión de seguridad de aplicaciones .NET basado en
OWASP Top 10 y mejores prácticas de desarrollo seguro.

---

## 1. Autenticación

### Configuración

- [ ] Autenticación con Azure AD / Microsoft Entra ID
- [ ] Tokens JWT con expiración corta (< 1 hora)
- [ ] Refresh tokens con rotación
- [ ] HTTPS obligatorio

### Código

- [ ] `[Authorize]` en controllers/endpoints protegidos
- [ ] Validación de claims del token
- [ ] No hardcodear credenciales en código
- [ ] Logout invalida tokens

```csharp
// ✅ Correcto
[Authorize(Roles = "Admin")]
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(int id)

// ❌ Incorrecto - sin autorización
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(int id)
```

---

## 2. Autorización

### Configuración

- [ ] Autorización basada en roles definida
- [ ] Políticas de autorización personalizadas
- [ ] Principio de mínimo privilegio

### Código

- [ ] Verificar permisos antes de operaciones sensibles
- [ ] No confiar en datos del cliente para autorización
- [ ] Logs de accesos denegados

```csharp
// ✅ Correcto - verificar propiedad del recurso
[Authorize]
public async Task<IActionResult> GetMyBeca(int id)
{
    var beca = await _service.GetByIdAsync(id);
    if (beca.UsuarioId != User.GetUserId())
        return Forbid();
    return Ok(beca);
}
```

---

## 3. Gestión de Secretos

### Nunca en Código

- [ ] Sin connection strings con credenciales
- [ ] Sin API keys hardcodeadas
- [ ] Sin passwords en appsettings.json
- [ ] Sin secretos en logs

### Gestión Correcta

- [ ] User Secrets en desarrollo
- [ ] Azure Key Vault en producción
- [ ] Variables de entorno en CI/CD
- [ ] Rotación de secretos documentada

```json
// ❌ NUNCA
{
  "ConnectionStrings": {
    "Default": "Server=prod;Password=123456"
  }
}

// ✅ Correcto - referencia a Key Vault
{
  "KeyVault": {
    "Url": "https://mi-keyvault.vault.azure.net/"
  }
}
```

---

## 4. Validación de Inputs (A03:2021)

### Validación

- [ ] FluentValidation en todos los inputs
- [ ] Validación en servidor (no solo cliente)
- [ ] Límites de longitud definidos
- [ ] Tipos de datos correctos

### Sanitización

- [ ] Encoding de outputs HTML
- [ ] Parámetros en queries SQL (no concatenación)
- [ ] Validación de URLs antes de redirección

```csharp
// ✅ Correcto
public class CreateBecaValidator : AbstractValidator<CreateBecaCommand>
{
    public CreateBecaValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty()
            .MaximumLength(200)
            .Matches(@"^[\w\s\-áéíóúñÁÉÍÓÚÑ]+$");
    }
}
```

---

## 5. SQL Injection (A03:2021)

### Prevención

- [ ] Usar Entity Framework con LINQ
- [ ] Parámetros en queries raw
- [ ] No concatenar strings en SQL
- [ ] Stored procedures con parámetros

```csharp
// ❌ NUNCA - SQL Injection
var sql = $"SELECT * FROM Users WHERE Name = '{nombre}'";

// ✅ Correcto - parámetros
var sql = "SELECT * FROM Users WHERE Name = @nombre";
await context.Database.ExecuteSqlRawAsync(sql, new SqlParameter("@nombre", nombre));

// ✅ Mejor - LINQ
var user = await context.Users.FirstOrDefaultAsync(u => u.Name == nombre);
```

---

## 6. XSS - Cross-Site Scripting (A03:2021)

### Prevención

- [ ] Encoding automático en Razor (`@variable`)
- [ ] `@Html.Raw()` solo con datos confiables
- [ ] Content-Security-Policy header
- [ ] HttpOnly en cookies

```razor
@* ✅ Correcto - encoding automático *@
<p>@Model.Nombre</p>

@* ❌ Peligroso - sin encoding *@
<p>@Html.Raw(Model.Descripcion)</p>
```

---

## 7. CSRF - Cross-Site Request Forgery (A01:2021)

### Prevención

- [ ] `[ValidateAntiForgeryToken]` en POST/PUT/DELETE
- [ ] Tokens anti-forgery en formularios
- [ ] SameSite cookies
- [ ] Verificar Origin/Referer en APIs

```csharp
// ✅ Correcto
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(BecaModel model)
```

---

## 8. Exposición de Datos Sensibles (A02:2021)

### Datos en Reposo

- [ ] Cifrado de columnas sensibles en BD
- [ ] TDE habilitado en SQL Server
- [ ] Backups cifrados

### Datos en Tránsito

- [ ] HTTPS obligatorio
- [ ] TLS 1.2 mínimo
- [ ] HSTS habilitado

### Datos en Logs

- [ ] No loguear contraseñas
- [ ] No loguear tokens completos
- [ ] No loguear datos personales (DNI, email completo)
- [ ] Enmascaramiento de datos sensibles

```csharp
// ❌ NUNCA
_logger.LogInformation("Login: {Email}, Password: {Password}", email, password);

// ✅ Correcto
_logger.LogInformation("Login attempt for user {UserId}", userId);
```

---

## 9. Configuración de Seguridad (A05:2021)

### Headers HTTP

- [ ] `X-Content-Type-Options: nosniff`
- [ ] `X-Frame-Options: DENY`
- [ ] `X-XSS-Protection: 1; mode=block`
- [ ] `Strict-Transport-Security`
- [ ] `Content-Security-Policy`

```csharp
// Program.cs
app.UseHsts();
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    await next();
});
```

### Errores

- [ ] `customErrors="On"` en producción
- [ ] No exponer stack traces
- [ ] Mensajes de error genéricos al usuario
- [ ] Logs detallados solo internamente

---

## 10. Componentes Vulnerables (A06:2021)

### Dependencias

- [ ] NuGets actualizados
- [ ] Sin vulnerabilidades conocidas (CVEs)
- [ ] Análisis con `dotnet list package --vulnerable`
- [ ] Dependabot o similar activado

### Verificación

```bash
# Verificar vulnerabilidades
dotnet list package --vulnerable

# Actualizar paquetes
dotnet outdated
```

---

## 11. Logging y Monitorización (A09:2021)

### Qué Loguear

- [ ] Intentos de autenticación (éxito y fallo)
- [ ] Accesos denegados
- [ ] Errores de validación
- [ ] Operaciones sensibles (crear, eliminar)

### Qué NO Loguear

- [ ] Contraseñas
- [ ] Tokens de acceso
- [ ] Datos personales completos
- [ ] Números de tarjeta

### Alertas

- [ ] Múltiples intentos fallidos de login
- [ ] Accesos desde ubicaciones inusuales
- [ ] Errores 500 frecuentes

---

## 12. Control de Acceso (A01:2021)

### Verificaciones

- [ ] Verificar autorización en cada operación
- [ ] No exponer IDs secuenciales predecibles
- [ ] Verificar propiedad de recursos
- [ ] Rate limiting en APIs públicas

```csharp
// ✅ Verificar que el usuario puede acceder al recurso
public async Task<BecaDto?> GetByIdAsync(int id, ClaimsPrincipal user)
{
    var beca = await _repository.GetByIdAsync(id);

    if (beca is null)
        return null;

    // Verificar acceso
    if (!user.IsInRole("Admin") && beca.CreatedBy != user.GetUserId())
        throw new ForbiddenAccessException();

    return _mapper.Map<BecaDto>(beca);
}
```

---

## Resultado de la Revisión

| Categoría | Cumple | Parcial | No Cumple | N/A |
|-----------|--------|---------|-----------|-----|
| Autenticación | ☐ | ☐ | ☐ | ☐ |
| Autorización | ☐ | ☐ | ☐ | ☐ |
| Secretos | ☐ | ☐ | ☐ | ☐ |
| Validación | ☐ | ☐ | ☐ | ☐ |
| SQL Injection | ☐ | ☐ | ☐ | ☐ |
| XSS | ☐ | ☐ | ☐ | ☐ |
| CSRF | ☐ | ☐ | ☐ | ☐ |
| Datos Sensibles | ☐ | ☐ | ☐ | ☐ |
| Configuración | ☐ | ☐ | ☐ | ☐ |
| Dependencias | ☐ | ☐ | ☐ | ☐ |
| Logging | ☐ | ☐ | ☐ | ☐ |
| Control Acceso | ☐ | ☐ | ☐ | ☐ |

---

**Fecha de revisión:** _______________
**Revisado por:** _______________
**Proyecto:** _______________

---

*Checklist v1.0 - analisis-arquitectura skill*
