# SSRF (Server-Side Request Forgery)

> Skill: security-audit | Version: 3.5.0

Falsificacion de solicitudes del lado del servidor: el atacante hace que el servidor realice peticiones a recursos internos.

> Ver tambien: `owasp/owasp-top10-2025.md` (A10), `patterns/secure-coding-checklist.md`

---

## Vectores de Ataque

```
# Escaneo de red interna
http://192.168.1.1/admin
http://10.0.0.1:8080/api-interna
http://localhost:6379/    (Redis)

# Metadatos cloud
http://169.254.169.254/latest/meta-data/    (AWS)
http://169.254.169.254/metadata/instance    (Azure)
http://metadata.google.internal/            (GCP)

# Acceso a archivos via generadores PDF
<iframe src="file:///etc/passwd" width="400" height="400">
<iframe src="file:///c:/windows/win.ini" width="400" height="400">

# Contrabando de protocolos
gopher://servidor-interno:25/
dict://servidor-interno:11211/
```

---

## Prevencion

### C# / .NET 10

```csharp
// INSEGURO - URL del usuario sin validar
var url = request.QueryString["url"];
var response = await _httpClient.GetAsync(url);

// SEGURO - Whitelist de dominios permitidos
var allowedDomains = new[] { "api.comillas.edu", "services.comillas.edu" };
var uri = new Uri(request.QueryString["url"]!);

if (!allowedDomains.Contains(uri.Host))
    return BadRequest("Dominio no permitido");

// Validar que no sea IP interna
if (IPAddress.TryParse(uri.Host, out var ip))
{
    if (IsPrivateIP(ip))
        return BadRequest("Acceso a IP interna no permitido");
}

var response = await _httpClient.GetAsync(uri);

static bool IsPrivateIP(IPAddress ip)
{
    byte[] bytes = ip.GetAddressBytes();
    return bytes[0] switch
    {
        10 => true,
        172 => bytes[1] >= 16 && bytes[1] <= 31,
        192 => bytes[1] == 168,
        127 => true,
        169 => bytes[1] == 254,
        _ => false
    };
}
```

### Python

```python
# SEGURO - Validar URL antes de fetch
from urllib.parse import urlparse
import ipaddress

ALLOWED_HOSTS = {"api.comillas.edu", "services.comillas.edu"}

def safe_fetch(url: str):
    parsed = urlparse(url)
    if parsed.hostname not in ALLOWED_HOSTS:
        raise ValueError("Dominio no permitido")
    try:
        ip = ipaddress.ip_address(parsed.hostname)
        if ip.is_private:
            raise ValueError("IP interna no permitida")
    except ValueError:
        pass  # Es hostname, no IP
    return requests.get(url, timeout=10)
```

---

## Checklist

- [ ] Whitelist de dominios permitidos para peticiones salientes
- [ ] Bloquear acceso a IPs privadas (10.x, 172.16-31.x, 192.168.x)
- [ ] Bloquear acceso a metadatos cloud (169.254.169.254)
- [ ] No pasar URLs del usuario directamente a generadores PDF/imagen
- [ ] Timeout corto en peticiones salientes

---

*Pattern v3.7.0*
