# Seguridad Especifica por Framework

> Skill: security-audit | Version: 3.5.0

Configuraciones de seguridad especificas para los frameworks mas comunes: Django, Express, Laravel, Rails y Spring.

> Ver tambien: `owasp/owasp-top10-2025.md` para referencia general, `owasp/owasp-dotnet.md` para .NET 10.

---

## Django (Python)

```python
# Middleware de seguridad (settings.py)
MIDDLEWARE = [
    'django.middleware.security.SecurityMiddleware',
    'django.middleware.clickjacking.XFrameOptionsMiddleware',
    'django.middleware.csrf.CsrfViewMiddleware',
]

# Configuraciones de seguridad obligatorias
SECURE_SSL_REDIRECT = True
SECURE_HSTS_SECONDS = 31536000
SECURE_HSTS_INCLUDE_SUBDOMAINS = True
SESSION_COOKIE_SECURE = True
CSRF_COOKIE_SECURE = True
SECURE_BROWSER_XSS_FILTER = True
SECURE_CONTENT_TYPE_NOSNIFF = True
```

**Riesgos especificos Django:**
- `{{ user_input|safe }}` - Deshabilita auto-escapado XSS
- `DEBUG = True` en produccion - Expone trazas de pila
- `ALLOWED_HOSTS = ['*']` - Vulnerable a ataques de cabecera Host

---

## Node.js / Express

```javascript
// Cabeceras de seguridad con Helmet.js
const helmet = require('helmet');
app.use(helmet());
app.use(helmet.noSniff());

// Deshabilitar compresion WebSocket (prevenir CRIME/BREACH)
const wss = new WebSocket.Server({ perMessageDeflate: false });

// Sanitizar datos del usuario para respuestas de API
function sanitizarUsuario(user) {
    return { id: user.id, username: user.username, fullName: user.fullName };
}
```

**Riesgos especificos Express:**
- `dangerouslySetInnerHTML` en React - XSS
- `eval()` / `new Function()` con entrada del usuario
- Inyeccion NoSQL en MongoDB: `{$gt: ""}` en parametros

---

## Laravel (PHP)

```php
// Prevenir asignacion masiva
$request->user()->fill($request->only(['name', 'email']))->save();

// Prevenir inyeccion SQL en consultas raw
User::whereRaw('email = ?', [$request->input('email')])->get();

// Prevenir inyeccion de comandos
exec('whois ' . escapeshellcmd($request->input('domain')));

// Prevenir recorrido de rutas
basename($request->input('filename'));

// Validar nombres de columna (prevenir inyeccion SQL via nombres de columna)
$request->validate(['sortBy' => 'in:price,updated_at']);
```

**Riesgos especificos Laravel:**
- `$request->all()` en `forceFill()` - Asignacion masiva
- Blade `{!! $var !!}` - Sin escapado XSS
- `DB::raw()` sin parametros

---

## Ruby on Rails

```ruby
# Prevenir inyeccion SQL
@projects = Project.where("name like ?", "%#{ActiveRecord::Base.sanitize_sql_like(params[:name])}%")

# Usar sesiones respaldadas por base de datos (prevenir ataques de replay)
Project::Application.config.session_store :active_record_store

# NUNCA usar con entrada del usuario
raw @product.name          # INSEGURO
@product.name.html_safe    # INSEGURO
link_to "Sitio", 'javascript:alert(1);'.html_safe  # INSEGURO

# SEGURO - auto-escapado por defecto
<%= @product.name %>
```

**Riesgos especificos Rails:**
- `raw`, `html_safe`, `<%== %>` - Deshabilitan auto-escapado
- `find_by_sql` con interpolacion de strings
- Mass assignment sin `strong_parameters`

---

## Spring Boot (Java)

```java
// Siempre usar PreparedStatement
String query = "SELECT * FROM users WHERE name = ?";
PreparedStatement pstmt = connection.prepareStatement(query);
pstmt.setString(1, userInput);

// Validacion de entrada
if (!Pattern.matches("[a-zA-Z0-9\\s\\-]{1,50}", userInput)) {
    return false;
}

// Sanitizacion de salida (OWASP Java Encoder + HTML Sanitizer)
PolicyFactory policy = new HtmlPolicyBuilder().allowElements("p", "strong").toFactory();
String safeOutput = policy.sanitize(untrustedHtml);
safeOutput = Encode.forHtml(safeOutput);
```

**Riesgos especificos Spring:**
- `@RequestParam` sin validacion
- `Statement.executeQuery()` con concatenacion
- Deserializacion con `ObjectInputStream`
- SpEL (Spring Expression Language) con entrada del usuario

---

## Tabla Comparativa de Protecciones

| Proteccion | Django | Express | Laravel | Rails | Spring |
|------------|--------|---------|---------|-------|--------|
| **Auto-escapado XSS** | Si (templates) | No (manual) | Si (Blade `{{ }}`) | Si (ERB `<%= %>`) | No (manual) |
| **CSRF** | Middleware | csurf (manual) | Middleware | Middleware | Spring Security |
| **SQL Param.** | ORM | Manual | Eloquent | ActiveRecord | JPA/Hibernate |
| **Headers** | SecurityMiddleware | Helmet.js | Middleware | Config | Spring Security |

---

*Pattern v3.7.0*
