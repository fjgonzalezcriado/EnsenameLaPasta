# OAuth2 Token Lifecycle in Postman

> **Purpose**: Manage OAuth2 token acquisition, caching, expiry, and refresh in Postman collections.
> **Version**: 1.0.0

---

## Token Lifecycle Overview

```
[Request] → [Check token exists?]
                │
         ┌──────┴──────┐
         │ NO           │ YES
         ▼              ▼
   [Request token]  [Check expiry]
         │              │
         │         ┌────┴────┐
         │         │ Valid    │ Expired
         │         ▼         ▼
         │    [Use token]  [Refresh or re-request]
         │                   │
         └───────────────────┘
                │
            [Store token + expiry]
                │
            [Execute request with Bearer header]
```

---

## Token Expiry Tracking

### Using `Date.now()` for Expiry Check

```javascript
// Store expiry timestamp when receiving token
const expiresIn = response.expires_in; // seconds
const expiryTimestamp = Date.now() + (expiresIn * 1000);
pm.environment.set("tokenExpiry", String(expiryTimestamp));

// Check before each request (with 60s buffer)
const tokenExpiry = parseInt(pm.environment.get("tokenExpiry"));
const isExpired = !tokenExpiry || Date.now() > (tokenExpiry - 60000);
```

### Why 60-Second Buffer?

- Network latency between check and actual request execution
- Prevents edge cases where token expires mid-flight
- Standard practice for OAuth2 client implementations

---

## Grant Types

### Client Credentials (Machine-to-Machine)

Most common for API testing at Comillas. No user interaction required.

```javascript
body: {
    mode: "urlencoded",
    urlencoded: [
        { key: "grant_type", value: "client_credentials" },
        { key: "client_id", value: clientId },
        { key: "client_secret", value: clientSecret },
        { key: "scope", value: scope }
    ]
}
```

### Authorization Code (User Context)

Required when API needs user identity. Use Postman's built-in OAuth2 Authorization tab.

1. Set Auth type to "OAuth 2.0" in Postman
2. Configure callback URL: `https://oauth.pstmn.io/v1/callback`
3. Set Auth URL and Token URL from Azure AD
4. Click "Get New Access Token"

### Refresh Token

```javascript
// If refresh_token is available
const refreshToken = pm.environment.get("refreshToken");

if (refreshToken) {
    pm.sendRequest({
        url: tokenUrl,
        method: "POST",
        header: { "Content-Type": "application/x-www-form-urlencoded" },
        body: {
            mode: "urlencoded",
            urlencoded: [
                { key: "grant_type", value: "refresh_token" },
                { key: "client_id", value: clientId },
                { key: "client_secret", value: clientSecret },
                { key: "refresh_token", value: refreshToken }
            ]
        }
    }, function (err, res) {
        if (!err && res.code === 200) {
            const json = res.json();
            pm.environment.set("token", json.access_token);
            pm.environment.set("refreshToken", json.refresh_token);
            pm.environment.set("tokenExpiry",
                String(Date.now() + (json.expires_in * 1000)));
        }
    });
}
```

---

## Secret Storage

### Environment Variables with `type: "secret"`

```json
{
    "key": "clientSecret",
    "value": "",
    "type": "secret",
    "enabled": true
}
```

Variables with `type: "secret"`:
- Are masked in Postman UI (shown as `••••••`)
- Are NOT synced to Postman cloud (team workspaces)
- Must be set manually on each machine
- Are passed securely in Newman via `--env-var`

### Variables That Should Be Secrets

| Variable | Why |
|----------|-----|
| `clientSecret` | OAuth2 client credential |
| `basicPassword` | Basic auth password |
| `apiKey` | API key value |
| `httpSignaturePrivateKey` | Signing key |
| `refreshToken` | Can obtain new access tokens |

---

## Newman CI/CD Integration

### Passing Secrets in CI

```bash
# Never store secrets in environment files committed to Git
newman run collection.json \
    -e environment_dev.json \
    --env-var "clientSecret=$CLIENT_SECRET" \
    --env-var "token=$PRE_OBTAINED_TOKEN"
```

### Pre-Obtaining Token in CI

```bash
# Option 1: Use curl to get token before Newman
TOKEN=$(curl -s -X POST "$TOKEN_URL" \
    -d "grant_type=client_credentials" \
    -d "client_id=$CLIENT_ID" \
    -d "client_secret=$CLIENT_SECRET" \
    -d "scope=$SCOPE" | jq -r '.access_token')

newman run collection.json \
    -e environment_dev.json \
    --env-var "token=$TOKEN"
```

---

*Pattern v1.0.0 - OAuth2 Token Lifecycle for Postman*
