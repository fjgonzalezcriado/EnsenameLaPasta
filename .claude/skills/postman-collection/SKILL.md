---
name: postman-collection
description: >
  Generates Postman collections for API testing with automatic authentication
  detection: scans Program.cs to detect Azure AD, OAuth2, API Key, Basic Auth,
  or HttpSignature, then configures collection-level auth, pre-request scripts,
  environment variables per environment (Dev/Pre/Pro), test assertions, and
  folder organization. Includes token lifecycle management with expiry caching.
  USE FOR: generating Postman collections, API testing environments, auth detection
  (Azure AD/OAuth2/API Key/Basic), Newman CI/CD, crear coleccion Postman, tests API,
  environments.
  DO NOT USE FOR: implementing API endpoints (use generador-crud), implementing
  OAuth2 in C# (use api-integration-patterns), auditing auth security
  (use security-audit).
---

> **Auto-invocación**: Claude activa este skill automáticamente cuando detecta necesidad de generar colecciones Postman, environments o tests para APIs.

---

## Activación Automática

Claude debe activar este skill cuando el usuario:
- Menciona "Postman", "colección", "collection"
- Pide "generar tests de API"
- Solicita "documentar endpoints para Postman"
- Menciona "environments" o "variables de entorno" para APIs
- Pide "exportar API a Postman"

### Comandos de activación

```
/postman                    → Menú interactivo
/postman collection         → Genera colección desde controllers
/postman environment        → Genera environments (Dev/Pre/Pro)
/postman tests              → Añade tests a colección existente
/postman full               → Todo: colección + environments + tests
/postman swagger            → Importa desde OpenAPI/Swagger
```

---

## Deteccion de Autenticacion

Claude SIEMPRE debe detectar el modo de autenticacion del proyecto antes de generar colecciones Postman. El flujo es:

### Fase 1: Escanear

Buscar en `Program.cs` / `Startup.cs`:
- `AddMicrosoftIdentityWebApi` + seccion `AzureAd` → **Azure AD**
- `AddAuthentication(JwtBearerDefaults)` sin Azure AD → **OAuth2/JWT**
- `ApiKeyMiddleware` / `X-API-Key` → **API Key**
- `BasicAuthenticationHandler` → **Basic Auth**
- `HmacAuthenticationHandler` / `HttpSignature` → **HttpSignature**
- `AddCertificateForwarding` → **mTLS**

### Fase 2: Configurar

Segun el modo detectado:
- Seleccionar template de pre-request script (`auth-prerequest-*.js.template`)
- Configurar objeto `auth` en la coleccion
- Generar variables de entorno especificas del auth

### Fase 3: Verificar

Usar `checklists/auth-configuration-checklist.md` para validar la configuracion.

> Ver: `patterns/auth-detection.md` para el algoritmo completo.

---

## Capacidades del Skill

### 1. Generar Colección desde Código

Escanea `*Controller.cs` y genera `postman_collection.json`:

- Detecta rutas (`[Route]`, `[HttpGet]`, etc.)
- Extrae parámetros (query, body, path)
- Genera ejemplos de request/response
- Organiza por carpetas (un folder por controller)
- Incluye descripción de cada endpoint

### 2. Generar Environments

Crea archivos de environment para diferentes entornos:

- `postman_environment_dev.json`
- `postman_environment_pre.json`
- `postman_environment_pro.json`

Variables comunes:
- `{{baseUrl}}` - URL base del API
- `{{token}}` - Token de autenticación
- `{{apiKey}}` - API Key si aplica
- Variables específicas del proyecto

### 3. Generar Tests Automáticos

Añade scripts de test a cada request:

- Validación de status code
- Validación de schema de respuesta
- Validación de tiempo de respuesta
- Tests de campos obligatorios
- Tests de autenticación

### 4. Importar desde Swagger/OpenAPI

Si existe `swagger.json` o endpoint `/swagger/v1/swagger.json`:

- Parsea definición OpenAPI
- Genera colección compatible
- Mapea schemas a ejemplos
- Preserva documentación

---

## Estructura de Salida

```
06_Documentacion/
└── Postman/
    ├── [Proyecto]_collection.json      ← Colección principal
    ├── [Proyecto]_environment_dev.json ← Environment desarrollo
    ├── [Proyecto]_environment_pre.json ← Environment preproducción
    ├── [Proyecto]_environment_pro.json ← Environment producción
    └── README.md                        ← Guía de uso
```

---

## Recursos del Skill

### Templates

| Archivo | Descripcion |
|---------|-------------|
| `templates/collection.json.template` | Plantilla de coleccion con multi-auth (`{{AUTH_CONFIG}}`) |
| `templates/environment.json.template` | Variables de entorno con bloques por modo auth |
| `templates/request.json.template` | Plantilla de request individual |
| `templates/tests.js.template` | Tests + pre-request scripts (18 constantes) |
| `templates/auth-prerequest-azuread.js.template` | Pre-request Azure AD con token expiry |
| `templates/auth-prerequest-apikey.js.template` | Pre-request API Key (header/query) |
| `templates/auth-prerequest-basic.js.template` | Pre-request Basic Auth (Base64) |

### Patterns

| Archivo | Descripcion |
|---------|-------------|
| `patterns/auth-detection.md` | Algoritmo de deteccion de autenticacion (3 fases) |
| `patterns/azure-ad-auth.md` | Azure AD: variables, flujos, Newman CI/CD |
| `patterns/oauth2-token-lifecycle.md` | Gestion de tokens: expiry, refresh, cache |
| `patterns/http-signature-mtls.md` | HttpSignature (RSA) + mTLS (legacy) |

### Checklists

| Archivo | Descripcion |
|---------|-------------|
| `checklists/auth-configuration-checklist.md` | Verificacion configuracion auth (15 items) |

### References

| Archivo | Descripcion |
|---------|-------------|
| `references/auth-examples.md` | Ejemplos pre-request scripts: JWT Bearer, Azure AD |
| `references/test-assertions.md` | Tests por verbo HTTP: GET, POST, PUT, DELETE, errores |

---

## Flujo de Trabajo

### Paso 1: Análisis

```
Claude analiza:
1. Buscar *Controller.cs en el proyecto
2. Detectar framework (Minimal APIs vs Controllers)
3. Identificar rutas y métodos HTTP
4. Extraer DTOs de request/response
5. Detectar autenticación configurada
```

### Paso 2: Generación

```
Claude genera:
1. Estructura de colección con folders
2. Requests con ejemplos
3. Variables de colección
4. Pre-request scripts (auth)
5. Tests por endpoint
```

### Paso 3: Environments

```
Claude crea environments:
1. Detectar URLs de appsettings.*.json
2. Generar variables por entorno
3. Incluir placeholders para secretos
```

### Paso 4: Documentación

```
Claude documenta:
1. README con instrucciones de importación
2. Descripción de cada endpoint
3. Ejemplos de uso
4. Notas sobre autenticación
```

---

## Variables de Colección Recomendadas

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `baseUrl` | URL base del API | `https://localhost:7001/api` |
| `token` | JWT o access token | `eyJhbG...` |
| `apiVersion` | Versión del API | `v1` |
| `lastCreatedId` | ID del último recurso creado | `123` |
| `testEmail` | Email para pruebas | `test@comillas.edu` |
| `testUserId` | ID de usuario de prueba | `1` |

---

## Integración con CI/CD

### Newman (CLI de Postman)

```bash
# Instalar Newman
npm install -g newman

# Ejecutar colección
newman run Comillas.Becas_collection.json \
    -e Comillas.Becas_environment_dev.json \
    --reporters cli,json \
    --reporter-json-export results.json

# Con variables de entorno de CI
newman run collection.json \
    --env-var "baseUrl=$API_URL" \
    --env-var "token=$API_TOKEN"
```

### Pipeline Azure DevOps

```yaml
- task: Npm@1
  inputs:
    command: 'custom'
    customCommand: 'install -g newman'

- script: |
    newman run $(Build.SourcesDirectory)/06_Documentacion/Postman/*_collection.json \
      -e $(Build.SourcesDirectory)/06_Documentacion/Postman/*_environment_dev.json \
      --reporters cli,junit \
      --reporter-junit-export $(Build.SourcesDirectory)/TestResults/postman-results.xml
  displayName: 'Run Postman Tests'

- task: PublishTestResults@2
  inputs:
    testResultsFormat: 'JUnit'
    testResultsFiles: '**/postman-results.xml'
```

---

## Notas para Claude

1. **SIEMPRE escanear Program.cs primero** - Detectar el modo de autenticacion antes de generar la coleccion. Ver `patterns/auth-detection.md`
2. **Detectar patron de rutas**: Comillas usa `/api/[controller]` generalmente
3. **Respetar convenciones**: Nombres en espanol si el proyecto los usa
4. **Incluir ejemplos realistas**: Usar datos que tengan sentido en el dominio
5. **Configurar auth collection-level**: No repetir auth en cada request (heredar de coleccion)
6. **Health check sin auth**: Primer request de la coleccion debe ser `/health` con `noauth`
7. **Secrets como type:secret**: clientSecret, apiKey, basicPassword, httpSignaturePrivateKey
8. **Generar tests progresivos**: Los tests de DELETE deben ir al final
9. **Considerar paginacion**: Incluir parametros `page` y `pageSize` en GETs de listado
10. **Token lifecycle**: Usar pre-request script con caching y expiry check (60s buffer)

---

*Skill v2.0.0 - Postman Collection Generator with Auth Detection*
