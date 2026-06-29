# OWASP Top 10 - 2025 (Multi-Lenguaje)

> Skill: security-audit | Version: 3.5.0

Las 10 vulnerabilidades mas criticas segun OWASP 2025, con ejemplos en C#, Java, Python, PHP, Ruby, Go y JavaScript.

> Ver tambien: `owasp/owasp-dotnet.md` para ejemplos especificos .NET 10, `owasp/owasp-frameworks.md` para seguridad por framework.

---

## A01:2025 - Control de Acceso Roto

**Descripcion:** Las restricciones sobre usuarios autenticados no se aplican correctamente.

**Vectores de Ataque:**
- Manipulacion de URLs y parametros (IDOR)
- Escalada de privilegios (horizontal/vertical)
- Manipulacion/evasion de JWT
- Configuracion incorrecta de CORS (`Access-Control-Allow-Origin: *`)
- Ausencia de controles de acceso en POST/PUT/DELETE

**Patrones de Deteccion:**
```
- Uso directo de IDs del usuario sin verificacion de autorizacion
- Falta de validacion de roles/permisos antes del acceso a datos
- Verificaciones de roles hardcodeadas en lugar de autorizacion centralizada
- Endpoints de API sin middleware de autenticacion
- Ausencia de protecciones CSRF
```

**Remediacion C#:**
```csharp
// INSEGURO - Sin verificacion de autorizacion
public ActionResult Edit(int id)
{
    var user = _context.Users.FirstOrDefault(e => e.Id == id);
    return View("Details", new UserViewModel(user));
}

// SEGURO - Verificar propiedad del recurso
public ActionResult Edit(int id)
{
    var user = _context.Users.FirstOrDefault(e => e.Id == id);
    if (user.Id != _userIdentity.GetUserId())
        return Forbid();
    return View("Edit", new UserViewModel(user));
}
```

```java
// SEGURO - Java: verificar propiedad
public ActionResult edit(int id) {
    var user = context.Users.firstOrDefault(e -> e.getId() == id);
    if (user.getId() != userIdentity.getUserId()) {
        return new ErrorView("No tiene permisos para esta accion");
    }
    return new EditView(new UserViewModel(user));
}
```

**Remediacion general:**
- Denegar por defecto; aplicar control de acceso del lado del servidor
- Implementar mecanismos de autorizacion centralizados y reutilizarlos
- Registrar fallos de control de acceso y alertar a los administradores
- Limitar la tasa de acceso a APIs
- Invalidar tokens JWT en el servidor tras el cierre de sesion
- Deshabilitar listado de directorios

---

## A02:2025 - Configuracion de Seguridad Incorrecta

**Descripcion:** Configuraciones por defecto inseguras, almacenamiento en la nube abierto, cabeceras HTTP mal configuradas, mensajes de error detallados.

**Vectores de Ataque:**
- Credenciales por defecto (admin/admin)
- DEBUG = True en produccion
- Cabeceras de seguridad ausentes
- Listado de directorios habilitado
- Sistemas/frameworks sin parchear

**Lista de Verificacion de Cabeceras:**
```http
Content-Security-Policy: default-src 'self'; script-src 'self'
Strict-Transport-Security: max-age=31536000; includeSubDomains
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: no-referrer
Permissions-Policy: geolocation=(), camera=(), microphone=()
```

**Configuraciones inseguras a evitar:**
```http
Access-Control-Allow-Origin: *
X-Permitted-Cross-Domain-Policies: all
Referrer-Policy: unsafe-url
```

**Remediacion:**
- Implementar proceso de bastionado identico en todos los entornos
- Eliminar funcionalidades, componentes y ejemplos no utilizados
- Automatizar el proceso de verificacion de configuraciones

---

## A03:2025 - Fallos en la Cadena de Suministro de Software

**Descripcion:** Vulnerabilidades por componentes con vulnerabilidades conocidas, falta de verificacion de integridad, pipelines CI/CD inseguros.

**Deteccion por lenguaje:**
```bash
# .NET
dotnet list package --vulnerable --include-transitive

# JavaScript/Node.js
npm audit
npm audit fix

# Python
pip-audit
safety check -r requirements.txt

# General
semgrep --config p/default
snyk test
trivy fs .
```

**Remediacion:**
- Inventariar continuamente todas las versiones de componentes
- Monitorizar CVE/NVD para vulnerabilidades
- Obtener componentes solo de fuentes oficiales
- Preferir paquetes firmados
- Usar herramientas SCA en CI/CD

---

## A04:2025 - Fallos Criptograficos

**Descripcion:** Fallos relacionados con la criptografia que conducen a la exposicion de datos sensibles.

**Algoritmos debiles a detectar:**
```
MD5, SHA1 (para fines de seguridad)
DES, 3DES, RC4
Modo ECB para cifrados de bloque
Claves de cifrado hardcodeadas
HTTP en lugar de HTTPS
TLS 1.0, 1.1
```

**Remediacion C#:**
```csharp
// INSEGURO
var hash = MD5.Create().ComputeHash(data);

// SEGURO
var hash = SHA256.Create().ComputeHash(data);

// Para contrasenas
var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
```

```python
# INSEGURO
import hashlib
digest = hashlib.md5(b"datos")

# SEGURO
digest = hashlib.sha256(b"datos")

# Para contrasenas - usar bcrypt, scrypt o Argon2
import bcrypt
hashed = bcrypt.hashpw(password.encode(), bcrypt.gensalt())
```

---

## A05:2025 - Inyeccion

**Descripcion:** Los datos proporcionados por el usuario no se validan, filtran ni sanean.

**Tipos:** SQL, NoSQL, Comandos del SO, LDAP, XPath, ORM, OGNL, EL, SSTI

**Vectores de Ataque:**
```
# Inyeccion SQL
' OR '1'='1
' UNION SELECT 1,2,3--
'; DROP TABLE users;--

# Inyeccion de Comandos
; ls -la /
| cat /etc/passwd
$(whoami)

# SSTI
{{7*7}}
${7*7}
```

**Remediacion por lenguaje:**

```csharp
// INSEGURO
var sql = "UPDATE User SET FirstName = " + firstname + " WHERE Id = " + id;

// SEGURO - Entity Framework parametrizado
var sql = @"UPDATE [User] SET FirstName = @FirstName WHERE Id = @Id";
context.Database.ExecuteSqlCommand(sql,
    new SqlParameter("@FirstName", firstname),
    new SqlParameter("@Id", id));
```

```java
// INSEGURO
String query = "SELECT * FROM users WHERE name = '" + userInput + "'";

// SEGURO - PreparedStatement
String query = "SELECT * FROM users WHERE name = ?";
PreparedStatement pstmt = connection.prepareStatement(query);
pstmt.setString(1, userInput);
```

```python
# INSEGURO - Inyeccion de Comandos
import os
os.system("ping " + user_input)

# SEGURO
import subprocess
subprocess.run(["ping", user_input], check=True)
```

```php
// INSEGURO
$query = "SELECT * FROM users WHERE id = " . $_GET['id'];

// SEGURO - PDO
$stmt = $pdo->prepare("SELECT * FROM users WHERE id = ?");
$stmt->execute([$_GET['id']]);
```

```ruby
# INSEGURO
@projects = Project.where("name like '" + name + "'")

# SEGURO
@projects = Project.where("name like ?", "%#{ActiveRecord::Base.sanitize_sql_like(params[:name])}%")
```

```go
// INSEGURO
query := "SELECT * FROM users WHERE email = '" + email + "'"

// SEGURO
query := "SELECT id, name, email FROM users WHERE email = ?"
row := db.QueryRow(query, email)
```

```javascript
// INSEGURO - Inyeccion NoSQL (MongoDB)
db.users.find({username: req.body.username, password: req.body.password})

// SEGURO
const username = String(req.body.username);
const password = String(req.body.password);
db.users.find({username: username, password: password})
```

---

## A06:2025 - Diseno Inseguro

**Descripcion:** Defectos en el diseno que no pueden corregirse mediante la implementacion.

**Verificaciones:**
- Se ha realizado modelado de amenazas?
- Se han definido requisitos de seguridad?
- Se usan patrones de diseno seguros?
- Se han considerado casos de abuso/uso indebido?
- Se ha implementado rate limiting?
- Se aplica defensa en profundidad?

---

## A07:2025 - Fallos de Autenticacion

**Descripcion:** Autenticacion rota y gestion de sesiones deficiente.

**Vectores de Ataque:**
- Relleno de credenciales (credential stuffing)
- Ataques de fuerza bruta
- Credenciales por defecto
- Fijacion de sesion
- IDs de sesion expuestos en URLs
- Ausencia de MFA

**Remediacion:**
- Implementar MFA (autenticacion multifactor)
- No desplegar con credenciales por defecto
- Verificar contrasenas contra las 10.000 peores contrasenas
- Seguir directrices NIST 800-63B
- Limitar/retrasar intentos fallidos de inicio de sesion
- Usar gestor de sesiones del lado del servidor con IDs de alta entropia
- Invalidar sesiones tras cierre de sesion, inactividad y tiempos absolutos

---

## A08:2025 - Fallos de Integridad del Software y los Datos

**Descripcion:** Codigo e infraestructura que no protegen contra violaciones de integridad.

**Patrones de deserializacion insegura:**

```csharp
// INSEGURO - BinaryFormatter esta obsoleto y es peligroso
// var formatter = new BinaryFormatter();
// object obj = formatter.Deserialize(stream);

// SEGURO - Usar System.Text.Json
var data = JsonSerializer.Deserialize<MyType>(jsonString);
```

```java
// INSEGURO - Deserializacion Java
ObjectInputStream ois = new ObjectInputStream(input);
Object obj = ois.readObject(); // Riesgo de RCE!
```

```python
# INSEGURO
import pickle
data = pickle.loads(untrusted_data)  # Riesgo de RCE!
```

**Verificaciones:**
- Actualizaciones de software sin firmar/verificar
- Pipelines CI/CD inseguros
- Actualizacion automatica sin verificacion de integridad

---

## A09:2025 - Fallos en el Registro y Alerta de Seguridad

**Descripcion:** Registro y monitorizacion insuficientes que permiten ataques no detectados.

**Eventos que deben registrarse:**
1. Todos los intentos de inicio de sesion (exitosos/fallidos)
2. Fallos de control de acceso
3. Fallos de validacion de entrada del lado del servidor
4. Transacciones de alto valor con pista de auditoria
5. Eventos de seguridad con: marca de tiempo, severidad, identidad, IP, resultado

**Remediacion:**
- Usar gestion centralizada de logs (ELK, Splunk)
- Implementar alertas en tiempo real para actividad sospechosa
- Proteger pistas de auditoria con controles de integridad
- Establecer plan de respuesta a incidentes (NIST 800-61)

---

## A10:2025 - Manejo Inadecuado de Condiciones Excepcionales

**Descripcion:** La aplicacion no maneja correctamente los errores, revelando informacion o entrando en estados inseguros.

**Patrones a detectar:**
```
- Trazas de pila expuestas a usuarios
- Mensajes de error de base de datos visibles
- Rutas internas/IPs filtradas
- Bloques catch vacios
- Patrones fail-open (permitir acceso en caso de error)
```

**Manejo seguro:**
```csharp
// C# - Manejo seguro de errores
try
{
    await ProcessPaymentAsync(request);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error procesando pago para usuario {UserId}", userId);
    return StatusCode(500, new ProblemDetails
    {
        Title = "Error interno",
        Detail = "No se pudo procesar el pago"  // Sin detalles tecnicos
    });
}
```

```go
func (s *server) ProcessPayment(ctx context.Context, req *pb.PaymentRequest) (*pb.PaymentResponse, error) {
    if err := validatePayment(req); err != nil {
        log.Printf("Validacion de pago fallida para usuario %s: %v", getUserID(ctx), err)
        return nil, status.Error(codes.InvalidArgument, "solicitud de pago invalida")
    }
}
```

---

*Pattern v3.7.0*
