# Path Traversal (Recorrido de Rutas)

> Skill: security-audit | Version: 3.5.0

Ataque que permite acceder a archivos fuera del directorio previsto mediante secuencias `../`.

> Ver tambien: `owasp/owasp-top10-2025.md` (A01), `patterns/secure-coding-checklist.md`

---

## Patrones de Ataque

```
# Basico
../../../etc/passwd
..\..\..\windows\win.ini

# Codificacion URL
..%2f..%2f..%2fetc%2fpasswd
%2e%2e%2f%2e%2e%2f

# Codificacion UTF-8
%c0%ae%c0%ae/%c0%ae%c0%ae/etc/passwd

# Doble codificacion
..%252f..%252f..%252fetc%252fpasswd

# Evasion con barras extra
....//....//etc/passwd
..\/..\/etc/passwd

# Null byte (lenguajes legacy)
../../../etc/passwd%00.jpg
```

---

## Prevencion

### C# / .NET 10

```csharp
// INSEGURO - Ruta del usuario sin validar
[HttpGet("download")]
public IActionResult Download([FromQuery] string filename)
{
    var path = Path.Combine("wwwroot/uploads", filename);
    return PhysicalFile(path, "application/octet-stream");
}

// SEGURO - Validar y sanitizar nombre de archivo
[HttpGet("download")]
public IActionResult Download([FromQuery] string filename)
{
    // 1. Extraer solo el nombre (elimina componentes de directorio)
    var safeName = Path.GetFileName(filename);

    // 2. Validar que no esta vacio tras sanitizar
    if (string.IsNullOrWhiteSpace(safeName))
        return BadRequest("Nombre de archivo invalido");

    // 3. Construir ruta completa y verificar que esta dentro del directorio permitido
    var basePath = Path.GetFullPath("wwwroot/uploads");
    var fullPath = Path.GetFullPath(Path.Combine(basePath, safeName));

    if (!fullPath.StartsWith(basePath))
        return BadRequest("Acceso denegado");

    if (!System.IO.File.Exists(fullPath))
        return NotFound();

    return PhysicalFile(fullPath, "application/octet-stream");
}
```

### PHP / Laravel

```php
// INSEGURO
return response()->download(storage_path('content/') . $request->input('filename'));

// SEGURO - basename() elimina componentes de directorio
$safeName = basename($request->input('filename'));
$path = storage_path('content/' . $safeName);

if (!file_exists($path)) {
    abort(404);
}
return response()->download($path);
```

### Python / Django

```python
import os

# SEGURO - Validar que la ruta resuelta esta dentro del directorio base
BASE_DIR = "/app/uploads"
requested = os.path.realpath(os.path.join(BASE_DIR, filename))

if not requested.startswith(BASE_DIR):
    raise PermissionError("Acceso denegado")
```

---

## Checklist

- [ ] Usar `Path.GetFileName()` para extraer solo el nombre de archivo
- [ ] Validar con `Path.GetFullPath()` que la ruta resuelta esta dentro del directorio permitido
- [ ] Nunca concatenar rutas con entrada del usuario directamente
- [ ] Almacenar archivos subidos fuera de la raiz web
- [ ] Content-Disposition: attachment para descargas

---

*Pattern v3.7.0*
