# Seguridad JWT (JSON Web Tokens)

> Skill: security-audit | Version: 3.5.0

Ataques comunes a JWT: alg:none, confusion RS256/HS256, validacion segura.

> Ver tambien: `owasp/owasp-top10-2025.md` (A07), `patterns/authentication-patterns.md`

---

## Ataques Comunes

### Ataque de Algoritmo "none"

```json
// Cabecera JWT manipulada
{"alg": "none", "typ": "JWT"}
// El token resultante tiene firma vacia: cabecera.payload.
```

Si el servidor no valida el algoritmo, acepta tokens sin firma.

### Confusion de Algoritmo (RS256 -> HS256)

```javascript
// INSEGURO - Misma verificacion para ambos algoritmos
jwt.verify(token, publicKey);  // RS256
jwt.verify(token, secretKey);  // HS256
// El atacante puede usar la clave publica como secreto HMAC!
```

```sh
# Extraer clave publica para confusion de algoritmo
openssl s_client -connect ejemplo.org:443 | openssl x509 -pubkey -noout
```

**Explicacion:** Si el servidor usa la misma clave para TLS y JWT, un atacante puede:
1. Obtener la clave publica (es publica)
2. Firmar un token con HMAC usando esa clave publica como secreto
3. El servidor verifica con la misma clave y acepta el token

---

## Validacion Segura

### C# / .NET 10

```csharp
// Configuracion segura de validacion JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://auth.comillas.edu",
            ValidateAudience = true,
            ValidAudience = "api.comillas.edu",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            // CRITICO: Especificar algoritmos permitidos
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
        };
    });
```

### Node.js

```javascript
// SEGURO - Especificar algoritmo explicitamente
const decoded = jwt.verify(token, secret, {
    algorithms: ['HS256'],  // Solo permitir HS256
    issuer: 'https://auth.comillas.edu',
    audience: 'api.comillas.edu'
});
```

---

## Checklist de Seguridad JWT

- [ ] Especificar siempre los algoritmos permitidos en la verificacion
- [ ] Usar algoritmos asimetricos (RS256, ES256) cuando sea posible
- [ ] Implementar gestion adecuada de claves (rotacion periodica)
- [ ] Validar todos los claims: `exp`, `iss`, `aud`, `nbf`
- [ ] Invalidar tokens en el servidor tras el cierre de sesion
- [ ] No almacenar datos sensibles en el payload (es base64, no cifrado)
- [ ] Usar tiempos de expiracion cortos (15-60 minutos)
- [ ] Implementar refresh tokens con rotacion

---

*Pattern v3.7.0*
