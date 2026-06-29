# Azure AD / Microsoft Entra ID - Postman Configuration

> **Purpose**: Configure Postman collections for APIs protected by Azure AD.
> **Version**: 1.0.0

---

## Environment Variables per Environment

### Development

| Variable | Value | Type |
|----------|-------|------|
| `tenantId` | `{dev-tenant-id}` | default |
| `clientId` | `{dev-client-id}` | default |
| `clientSecret` | `{dev-client-secret}` | **secret** |
| `scope` | `api://{dev-client-id}/.default` | default |
| `azureAdTokenUrl` | `https://login.microsoftonline.com/{dev-tenant-id}/oauth2/v2.0/token` | default |

### Pre-production / Production

Same structure, different values per App Registration. Each environment has its own App Registration in Azure portal.

> **Important**: Never share client secrets between environments. Each environment should use its own App Registration with appropriate permissions.

---

## Pre-Request Script (Collection Level)

Use `auth-prerequest-azuread.js.template` at the collection level. It handles:

1. **Token caching** - Stores token in environment variable
2. **Expiry check** - 60-second buffer before requesting new token
3. **Error handling** - Console error with descriptive messages
4. **Automatic refresh** - Requests new token when expired

### Client Credentials Flow (Machine-to-Machine)

```javascript
// This is the most common flow for API testing at Comillas
// App Registration → API Permissions → Application permissions
grant_type: "client_credentials"
scope: "api://{clientId}/.default"
```

### On-Behalf-Of Flow (User Delegation)

For APIs that require user context (e.g., accessing downstream APIs on behalf of the user):

```javascript
// Requires user token first (from login or Azure AD auth code flow)
pm.sendRequest({
    url: pm.environment.get("azureAdTokenUrl"),
    method: "POST",
    header: { "Content-Type": "application/x-www-form-urlencoded" },
    body: {
        mode: "urlencoded",
        urlencoded: [
            { key: "grant_type", value: "urn:ietf:params:oauth:grant-type:jwt-bearer" },
            { key: "client_id", value: pm.environment.get("clientId") },
            { key: "client_secret", value: pm.environment.get("clientSecret") },
            { key: "assertion", value: pm.environment.get("userToken") },
            { key: "scope", value: pm.environment.get("downstreamScope") },
            { key: "requested_token_use", value: "on_behalf_of" }
        ]
    }
}, function (err, res) {
    if (!err && res.code === 200) {
        pm.environment.set("downstreamToken", res.json().access_token);
    }
});
```

---

## Collection Auth Object

```json
{
    "auth": {
        "type": "bearer",
        "bearer": [
            {
                "key": "token",
                "value": "{{token}}",
                "type": "string"
            }
        ]
    }
}
```

---

## App Registration Differences per Environment

| Setting | Dev | Pre | Pro |
|---------|-----|-----|-----|
| Tenant | Same or separate | Staging tenant | Production tenant |
| Redirect URIs | `https://localhost:*` | `https://pre.comillas.edu` | `https://api.comillas.edu` |
| API Permissions | All scopes | Restricted | Minimal required |
| Token Lifetime | Default (1h) | Default (1h) | May be shorter |
| Secret Expiry | 2 years | 1 year | 6 months |

---

## Newman CI/CD Integration

```bash
# Pass Azure AD credentials via CLI environment variables
newman run collection.json \
    -e environment_dev.json \
    --env-var "clientSecret=$AZURE_CLIENT_SECRET" \
    --env-var "tenantId=$AZURE_TENANT_ID" \
    --env-var "clientId=$AZURE_CLIENT_ID"
```

For Azure DevOps pipelines:

```yaml
- script: |
    newman run collection.json \
      -e environment_dev.json \
      --env-var "clientSecret=$(AzureClientSecret)" \
      --env-var "tenantId=$(AzureTenantId)"
  displayName: "Run Postman Collection with Azure AD"
```

---

## Troubleshooting

| Error | Cause | Fix |
|-------|-------|-----|
| `AADSTS700016` | Wrong clientId | Verify App Registration |
| `AADSTS7000218` | Missing client_secret | Check secret not expired |
| `AADSTS65001` | Missing API permission | Add permission + admin consent |
| `AADSTS50011` | Wrong redirect URI | Not applicable for client_credentials |
| `AADSTS700024` | Wrong grant type | Use `client_credentials` for M2M |

---

*Pattern v1.0.0 - Azure AD Authentication for Postman*
