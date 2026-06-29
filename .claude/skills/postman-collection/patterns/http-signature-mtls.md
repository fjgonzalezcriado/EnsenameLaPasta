# HttpSignature and mTLS - Postman Configuration

> **Purpose**: Configure Postman for APIs using HttpSignature (RSA key pairs) or mTLS authentication.
> **Version**: 1.0.0
> **Context**: Comillas uses HttpSignature with public/private key pairs for inter-university APIs (e.g., EWP/Erasmus).

---

## HttpSignature Authentication

### Overview

HttpSignature uses asymmetric cryptography (RSA key pairs) to sign HTTP requests:

- **Client** signs the request with its **private key**
- **Server** verifies the signature with the client's **public key**
- Keys are registered per `UniversidadId` in a lookup table

### Key Table Structure (Comillas Pattern)

```
┌─────────────────────────────────────────────────────────────┐
│ UniversidadId │ HttpSignatureClientPublic  │ ClientPrivate   │
│               │ HttpSignatureServerPublic  │ ServerPrivate   │
├───────────────┼────────────────────────────┼─────────────────┤
│ COMILLAS-01   │ <RSA Public Key XML>       │ <RSA Private>   │
│ PARTNER-02    │ <RSA Public Key XML>       │ <RSA Private>   │
└─────────────────────────────────────────────────────────────┘
```

**Signing flow:**
1. Client takes HTTP method + URL + headers + body
2. Signs with `HttpSignatureClientPrivate` using RSA-SHA256
3. Server verifies with `HttpSignatureClientPublic`
4. Response signed with `HttpSignatureServerPrivate`
5. Client verifies response with `HttpSignatureServerPublic`

---

### Postman Limitations

Postman **does not natively support** RSA signing in pre-request scripts. The `crypto` module is not available in the Postman sandbox.

**Workarounds:**

#### Option 1: Newman with External Script (Recommended for CI/CD)

```bash
# Pre-sign the request externally, pass signature as env var
SIGNATURE=$(node sign-request.js --key private.pem --method GET --url "/api/endpoint")

newman run collection.json \
    -e environment.json \
    --env-var "httpSignature=$SIGNATURE"
```

#### Option 2: External Signing Service

```javascript
// Pre-request script calls a local signing service
pm.sendRequest({
    url: "http://localhost:3000/sign",
    method: "POST",
    header: { "Content-Type": "application/json" },
    body: {
        mode: "raw",
        raw: JSON.stringify({
            method: pm.request.method,
            url: pm.request.url.toString(),
            body: pm.request.body ? pm.request.body.raw : "",
            algorithm: pm.environment.get("httpSignatureAlgorithm") || "RSA-SHA256"
        })
    }
}, function (err, res) {
    if (!err && res.code === 200) {
        const signature = res.json().signature;
        pm.request.headers.add({
            key: "X-Signature",
            value: signature
        });
        console.log("🔑 HttpSignature added to request");
    }
});
```

#### Option 3: Manual Signature (Development/Testing)

For quick manual testing, generate a signature externally and paste it:

```bash
# Generate signature with OpenSSL
echo -n "GET /api/nominaciones" | openssl dgst -sha256 -sign private.pem | base64
```

Then set in Postman environment:
- `httpSignature`: The base64-encoded signature
- Add header `X-Signature: {{httpSignature}}` to requests

---

### Environment Variables for HttpSignature

| Variable | Value | Type |
|----------|-------|------|
| `httpSignaturePrivateKey` | PEM-encoded private key (for local signing) | **secret** |
| `httpSignatureAlgorithm` | `RSA-SHA256` (default) | default |
| `httpSignatureKeyId` | Key identifier (e.g., `COMILLAS-01`) | default |
| `signingServiceUrl` | URL of local signing service (Option 2) | default |

---

### Security Notes

- **Demo keys** can be regenerated freely for testing
- **Production keys** MUST NOT be used in Postman - use only in deployed services
- Store private keys in Azure Key Vault in production environments
- Rotate keys according to university security policy

---

## mTLS (Mutual TLS)

### Overview

mTLS requires both client and server to present TLS certificates. This is infrastructure-level authentication, not typically configured at the API collection level.

> **Note**: mTLS is legacy at Comillas. New integrations use HttpSignature or Azure AD. Documented here for reference.

### Postman Configuration

1. Open Postman **Settings** (gear icon)
2. Go to **Certificates** tab
3. Add client certificate:
   - **Host**: `api.partner.edu`
   - **PFX file**: `client-cert.pfx`
   - **Passphrase**: `{pfx-password}`

### Environment Variables

| Variable | Value | Type |
|----------|-------|------|
| `pfxPath` | Path to .pfx file | default |
| `pfxPassword` | PFX passphrase | **secret** |

### Newman with mTLS

```bash
newman run collection.json \
    -e environment.json \
    --ssl-client-cert client-cert.pem \
    --ssl-client-key client-key.pem \
    --ssl-client-passphrase "$PFX_PASSWORD"
```

---

## Decision Guide

| Scenario | Recommended Auth |
|----------|-----------------|
| Internal Comillas APIs | Azure AD (standard) |
| External partner APIs (EWP) | HttpSignature |
| Legacy partner integration | mTLS |
| Third-party APIs | API Key or OAuth2 |
| Development/quick testing | Basic Auth or No Auth |

---

*Pattern v1.0.0 - HttpSignature and mTLS for Postman*
