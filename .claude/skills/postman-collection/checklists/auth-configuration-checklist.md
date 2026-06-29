# Auth Configuration Checklist for Postman Collections

> **Purpose**: Verify authentication is correctly configured in generated Postman collections.
> **Version**: 1.0.0

---

## Pre-Generation

- [ ] **Auth mode detected** - Scanned Program.cs/Startup.cs for auth middleware
- [ ] **Auth mode documented** - Collection description mentions the detected auth mode
- [ ] **Correct template selected** - Using the right `auth-prerequest-*.js.template`

## Collection-Level Auth

- [ ] **Auth object set** - Collection `auth` property matches detected mode (bearer/apikey/basic/noauth)
- [ ] **Pre-request script installed** - Collection-level pre-request script handles token acquisition
- [ ] **Token expiry check** - Pre-request script checks token validity before requesting new one (60s buffer)
- [ ] **Error handling** - Pre-request script logs errors to console on auth failure

## Environment Variables

- [ ] **All auth variables present** - Required variables for detected auth mode are defined
- [ ] **Secrets marked as secret** - clientSecret, apiKey, basicPassword use `"type": "secret"`
- [ ] **Per-environment values** - Dev, Pre, Pro environments have distinct auth values
- [ ] **No hardcoded credentials** - Environment files committed to Git contain only placeholders
- [ ] **Token expiry variable** - `tokenExpiry` variable exists for caching token validity

## Requests

- [ ] **Health check without auth** - First request in collection uses `"auth": {"type": "noauth"}`
- [ ] **Auth-protected requests inherit** - API requests inherit collection-level auth (no per-request auth unless different)
- [ ] **Login/token endpoints** - If project has login endpoint, it's in an "Auth" folder with `noauth`

## Newman / CI/CD

- [ ] **Secrets via --env-var** - Documentation shows how to pass secrets via CLI
- [ ] **No secrets in committed files** - Environment files in Git have empty secret values
- [ ] **Pipeline example** - Azure DevOps or GitHub Actions snippet included

## Documentation

- [ ] **Auth section in README** - Postman README explains how to configure authentication
- [ ] **Pre-request script commented** - Script has clear comments explaining what each block does
- [ ] **Troubleshooting** - Common auth errors and their fixes documented

---

*Checklist v1.0.0 - Auth Configuration for Postman Collections*
