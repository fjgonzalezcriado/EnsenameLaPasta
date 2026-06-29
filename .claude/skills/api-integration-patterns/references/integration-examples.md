# Contenido Clave y Casos de Uso

> Detalle de los patterns, templates, examples, checklists y scripts del skill.
> Referencia completa para implementar integraciones con APIs externas.

---

## Patterns (6)

1. **httpclient-factory.md**
   - IHttpClientFactory (typed/named clients)
   - Configuracion en Program.cs
   - Lifetime management
   - Evitar socket exhaustion

2. **retry-policy.md**
   - Polly v8+ (resilience pipelines)
   - WaitAndRetry con backoff exponencial
   - Retry solo en errores transitorios (429, 503, timeout)
   - Jitter para evitar thundering herd

3. **circuit-breaker.md**
   - Circuit breaker states (Closed, Open, Half-Open)
   - Configuracion umbrales (failure rate, duracion)
   - Fallback strategies
   - Telemetria (logs, metrics)

4. **oauth2-client-credentials.md**
   - OAuth2 Client Credentials Flow
   - Renovacion automatica de tokens
   - Almacenamiento seguro (IMemoryCache)
   - Azure AD / Entra ID integration

5. **response-caching.md**
   - IMemoryCache / IDistributedCache
   - Cache-Control headers
   - Invalidacion de cache
   - Cache aside pattern

6. **api-versioning.md**
   - URL versioning (/api/v1/users)
   - Header versioning (X-API-Version)
   - Query string versioning (?version=1.0)
   - Deprecation policies

---

## Templates (7)

1. **ApiClient.cs.template**
   ```csharp
   public class GenericApiClient
   {
       private readonly HttpClient _httpClient;
       private readonly ILogger<GenericApiClient> _logger;

       public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
       {
           // Implementacion generica
       }
   }
   ```

2. **BannerClient.cs.template**
   - Typed HttpClient para Banner API
   - Metodos: GetStudent(), GetCourses(), EnrollStudent()
   - Autenticacion OAuth2 + API Key
   - Manejo errores especificos Banner

3. **OracleHcmClient.cs.template**
   - Typed HttpClient para Oracle HCM Cloud
   - Metodos: GetEmployees(), GetPayroll(), GetAbsences()
   - Autenticacion OAuth2
   - Paginacion (next links)

4. **SigmaClient.cs.template**
   - Typed HttpClient para Sigma
   - Metodos: GetNominas(), GetEmployeePayslips()
   - Autenticacion custom (API Key + HMAC)

5. **ApiOptions.cs.template**
   ```csharp
   public class BannerApiOptions
   {
       public string BaseUrl { get; set; }
       public string ClientId { get; set; }
       public string ClientSecret { get; set; }
       public int TimeoutSeconds { get; set; } = 30;
   }
   ```

6. **ApiResponse.cs.template**
   - Wrapper generico para respuestas
   - Success/Error handling
   - Metadata (status code, headers, elapsed time)

7. **DelegatingHandler.cs.template**
   - LoggingHandler (log request/response)
   - AuthenticationHandler (inject OAuth2 token)
   - CorrelationIdHandler (X-Correlation-ID)

---

## Examples (4)

1. **banner-api-example.md**
   - Caso completo: matricular estudiante
   - Configuracion appsettings.json
   - Registro servicios (DI)
   - Uso desde Controller/Command
   - Tests unitarios (MockHttpMessageHandler)

2. **oracle-hcm-example.md**
   - Caso completo: obtener empleados activos
   - OAuth2 flow
   - Paginacion de resultados
   - Cache de respuestas (IMemoryCache)

3. **sigma-example.md**
   - Caso completo: obtener nominas del mes
   - Autenticacion HMAC
   - Manejo errores especificos
   - Retry policy (429 Rate Limit)

4. **polly-advanced.md**
   - Combinar retry + circuit breaker
   - Resilience pipeline v8
   - Telemetria con Application Insights
   - Fallback values

---

## Checklists (2)

1. **api-integration-checklist.md**
   - [ ] HttpClientFactory configurado (evitar socket exhaustion)
   - [ ] Timeout configurado (30s default)
   - [ ] Retry policy para errores transitorios
   - [ ] Circuit breaker para proteger sistema
   - [ ] Autenticacion implementada (OAuth2/API Key)
   - [ ] Secretos en Azure Key Vault (NO hardcoded)
   - [ ] Logging request/response (sin datos sensibles)
   - [ ] Tests unitarios con MockHttpMessageHandler
   - [ ] Tests integracion con API real (ambiente test)
   - [ ] Documentacion endpoints consumidos

2. **security-checklist.md**
   - [ ] Usar HTTPS obligatorio
   - [ ] Validar certificados SSL (NO AllowInvalidCertificates)
   - [ ] Tokens en IMemoryCache (NO static variables)
   - [ ] Secretos en Key Vault
   - [ ] Rate limiting implementado
   - [ ] Input validation (sanitizar datos)
   - [ ] Output encoding (evitar XSS)
   - [ ] Audit logging (quien llamo que endpoint)
   - [ ] CORS configurado (si aplica)
   - [ ] OWASP API Security Top 10

---

## Scripts (2)

1. **Test-ApiConnection.ps1**
   - Script PowerShell para test rapido conexion
   - Obtener OAuth2 token
   - Llamar endpoint test
   - Validar respuesta

2. **Generate-ApiClient.ps1**
   - Generar cliente C# desde OpenAPI spec
   - Usar NSwag o Kiota
   - Configurar namespaces
   - Generar modelos DTOs

---

## Casos de Uso

### Caso 1: Nueva integracion con Banner
```
Usuario: "Necesito integrar con Banner para matricular estudiantes"

Claude activa skill y proporciona:
1. Pattern: oauth2-client-credentials.md
2. Template: BannerClient.cs.template
3. Example: banner-api-example.md
4. Checklist: api-integration-checklist.md

Resultado: Cliente Banner funcional con OAuth2 y retry policy
```

### Caso 2: Refactorizar HttpClient existente
```
Usuario: "Tengo un HttpClient estatico que causa problemas de sockets"

Claude activa skill y proporciona:
1. Pattern: httpclient-factory.md
2. Analiza codigo existente
3. Propone refactor a IHttpClientFactory
4. Actualiza Program.cs con DI

Resultado: HttpClient refactorizado con factory pattern
```

### Caso 3: Añadir resiliencia a llamadas API
```
Usuario: "Las llamadas a Oracle HCM fallan a veces, necesito reintentos"

Claude activa skill y proporciona:
1. Pattern: retry-policy.md + circuit-breaker.md
2. Example: polly-advanced.md
3. Configuracion Polly v8
4. Telemetria con Application Insights

Resultado: Llamadas resilientes con retry + circuit breaker
```
