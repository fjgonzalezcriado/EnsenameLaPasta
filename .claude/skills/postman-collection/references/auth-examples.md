# Ejemplos de Configuración de Autenticación

> Ejemplos de pre-request scripts para diferentes modos de autenticación en Postman.
> Incluye: JWT Bearer, Azure AD / Microsoft Entra ID.

---

## JWT Bearer (más común en Comillas)

```javascript
// Pre-request script de colección
if (!pm.environment.get("token")) {
    console.log("⚠️ Token no configurado. Ejecuta primero /auth/login");
}

pm.request.headers.add({
    key: "Authorization",
    value: "Bearer " + pm.environment.get("token")
});
```

---

## Azure AD / Microsoft Entra ID

```javascript
// Pre-request script para Azure AD
const tokenUrl = pm.environment.get("azureAdTokenUrl");
const clientId = pm.environment.get("clientId");
const clientSecret = pm.environment.get("clientSecret");
const scope = pm.environment.get("scope");

pm.sendRequest({
    url: tokenUrl,
    method: 'POST',
    header: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: {
        mode: 'urlencoded',
        urlencoded: [
            { key: 'grant_type', value: 'client_credentials' },
            { key: 'client_id', value: clientId },
            { key: 'client_secret', value: clientSecret },
            { key: 'scope', value: scope }
        ]
    }
}, function (err, res) {
    if (!err) {
        pm.environment.set("token", res.json().access_token);
    }
});
```

---

> Para el algoritmo completo de detección de autenticación, ver `patterns/auth-detection.md`.
> Para templates de pre-request por modo, ver `templates/auth-prerequest-*.js.template`.
