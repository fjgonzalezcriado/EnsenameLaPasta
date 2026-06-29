# Deteccion de Secretos en Codigo

> Skill: security-audit | Version: 3.5.0

Patrones regex para detectar secretos hardcodeados en codigo fuente, resumen de exposicion y recomendaciones de gestion.

> Ver tambien: `patterns/secure-coding-checklist.md`, `reports/report-format.md`

---

## Patrones de Deteccion

| Tipo | Regex |
|------|-------|
| Connection String | `(Server|Data Source)=.*(Password|Pwd)=` |
| API Key | `[aA][pP][iI][_-]?[kK][eE][yY]\s*[=:]\s*["'][^"']{20,}` |
| JWT Token | `eyJ[A-Za-z0-9_-]{10,}\.eyJ[A-Za-z0-9_-]{10,}` |
| Azure Storage Key | `DefaultEndpointsProtocol=https;AccountName=.*;AccountKey=.*` |
| Azure AD Secret | `[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}` |
| Password in JSON | `"[pP]assword"\s*:\s*"[^"]+"` |
| Private Key | `-----BEGIN\s+(RSA\s+)?PRIVATE\s+KEY-----` |
| GitHub Token | `gh[ps]_[a-zA-Z0-9]{36}` |
| Generic Secret | `(?i)(secret|token|credential)\s*[:=]\s*["'][^"']{16,}` |

### Palabras clave a buscar

```
password, passwd, pwd, secret, token, api_key, apikey,
client_secret, connection_string, connectionstring,
private_key, access_key, secret_key, credentials,
bearer, authorization, auth_token
```

---

## .gitignore Recomendado

```
appsettings.Development.json
*.pfx
*.key
.env
secrets/
```

## Pre-commit Hook

```bash
#!/bin/sh
if git diff --cached | grep -iE "(password|secret|apikey|connectionstring)\s*=\s*['"][^'"]+['"]"; then
  echo "ADVERTENCIA: Posible secreto detectado. Revisar antes de commit."
  exit 1
fi
```

---

## Resumen de Exposicion

Documentar cada secreto detectado con esta estructura:

```
| Tipo de Secreto | Estado | Ubicacion | Accion Requerida |
|-----------------|:------:|-----------|------------------|
| Azure AD Client Secret | CRITICO - Expuesto | appsettings.json:XX | Rotar inmediatamente |
| Connection String | MEDIO - En config | web.config:XX | Mover a Key Vault |
| API Keys | CRITICO - Hardcoded | codigo.cs:XX | Externalizar |
| SMTP Credentials | ALTO - En codigo | email.cs:XX | Usar secretos seguros |
```

### Formato de Secretos Detectados (Ofuscados)

Siempre ofuscar el valor real del secreto en el informe:

```
Archivo: {RUTA}
Linea {NN}: {TIPO_SECRETO} = "SECRETO_OMITIDO"
```

---

## Recomendaciones de Gestion de Secretos

| Entorno | Solucion Recomendada |
|---------|---------------------|
| Desarrollo | User Secrets (dotnet user-secrets), .env con .gitignore |
| CI/CD | Variables de entorno cifradas, secretos del pipeline |
| Produccion Azure | Azure Key Vault + Managed Identity |
| Produccion AWS | AWS Secrets Manager + IAM Roles |
| Produccion On-Premise | HashiCorp Vault / DPAPI |

---

*Pattern v3.7.0*
