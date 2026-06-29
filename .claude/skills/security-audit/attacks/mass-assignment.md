# Mass Assignment (Asignacion Masiva)

> Skill: security-audit | Version: 3.5.0

Vulnerabilidad donde el atacante modifica propiedades del modelo que no deberian ser editables (ej: `isAdmin`, `role`, `price`).

> Ver tambien: `owasp/owasp-top10-2025.md` (A01, A04), `patterns/secure-coding-checklist.md`

---

## Ejemplo de Ataque

```http
POST /api/users/update HTTP/1.1
Content-Type: application/json

{
    "name": "Juan",
    "email": "juan@comillas.edu",
    "role": "Admin",        <!-- El atacante anade este campo -->
    "isActive": true         <!-- Y este -->
}
```

---

## Prevencion

### C# / .NET 10

```csharp
// INSEGURO - Bind directo de todo el modelo
[HttpPost]
public async Task<IActionResult> Update([FromBody] User user)
{
    _context.Users.Update(user); // Actualiza TODOS los campos
    await _context.SaveChangesAsync();
    return Ok();
}

// SEGURO - Usar DTOs con propiedades explicitas
public record UpdateUserRequest
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    // NO incluir Role, IsAdmin, IsActive, etc.
}

[HttpPut("{id}")]
public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
{
    var user = await _context.Users.FindAsync(id);
    if (user is null) return NotFound();

    user.Name = request.Name;
    user.Email = request.Email;
    // Solo se actualizan los campos del DTO
    await _context.SaveChangesAsync();
    return NoContent();
}

// ALTERNATIVA - Bind explicitamente los campos permitidos
[HttpPost]
public async Task<IActionResult> Update(
    [Bind("Name,Email")] User user)
{
    // Solo Name y Email se bindean del request
}
```

### PHP / Laravel

```php
// INSEGURO
$request->user()->forceFill($request->all())->save();

// SEGURO - Solo permitir campos especificos
$request->user()->fill($request->only(['name', 'email']))->save();

// O usar datos validados
$request->user()->fill($request->validated())->save();

// En el modelo - definir $fillable
class User extends Model
{
    protected $fillable = ['name', 'email'];
    // role, is_admin NO estan en $fillable
}
```

### Ruby on Rails

```ruby
# SEGURO - Strong Parameters
def user_params
    params.require(:user).permit(:name, :email)
    # role, is_admin NO se permiten
end
```

---

## Checklist

- [ ] Usar DTOs/ViewModels con propiedades explicitas (nunca bind directo a entidades)
- [ ] Definir `$fillable` o `$guarded` en modelos (Laravel)
- [ ] Usar Strong Parameters (Rails)
- [ ] Nunca usar `[FromBody] Entity` directamente en controllers
- [ ] Validar con FluentValidation que solo llegan campos esperados

---

*Pattern v3.7.0*
