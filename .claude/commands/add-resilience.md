Configura políticas de resiliencia Polly para HttpClients

# Configurar Resiliencia

Wizard para configurar políticas de resiliencia (retry, circuit breaker, timeout) en HttpClients del proyecto siguiendo los estándares de Comillas.

---

## PASO 1: Detectar proyecto

1. **Buscar proyecto principal en `03_Desarrollo/`:**
   - Localizar archivos `.csproj` (buscar el proyecto Web/API principal y proyecto de Infrastructure)
   - Escanear registros de `AddHttpClient` en Program.cs y archivos de configuración
   - Escanear configuración Polly existente (`AddResilienceHandler`, `AddPolicyHandler`)
   - Listar HttpClients descubiertos

2. **Mostrar HttpClients encontrados:**:
```
HttpClients detectados:
  1. BannerApiClient     - Program.cs:45
  2. OracleHcmClient     - Program.cs:52
  3. SigmaClient         - Program.cs:58
  [ninguno encontrado]

Polly existente: {0}
```

3. **Si no hay HttpClients: preguntar si desea crear uno nuevo primero**

---

## PASO 2: Wizard interactivo

Para cada HttpClient (o el especificado por el usuario):

```
Configurar resiliencia para: {0}

Selecciona las políticas a aplicar:

RECOMENDADAS:
  [x] Retry                - 3 intentos, backoff exponencial con jitter
  [x] Circuit Breaker      - 5 fallos -> 30s circuito abierto
  [x] Timeout              - 30s timeout por petición

OPCIONALES:
  [ ] Bulkhead             - Limitar peticiones concurrentes
  [ ] Standard resilience  - Handler estándar (incluye retry+cb+timeout)

¿Deseas usar el Standard Resilience Handler? (recomendado para la mayoría de casos) [S/n]
```

**Si el usuario elige Standard Resilience Handler: aplicar la configuración por defecto de Microsoft.Extensions.Http.Resilience que incluye retry, circuit breaker y timeout con valores sensatos.**

**Si el usuario prefiere configuración manual: permitir ajustar valores para cada política.**

Repetir para cada HttpClient seleccionado.

---

## PASO 3: Añadir NuGets

Listar e instalar los paquetes necesarios:

```powershell
dotnet add package Microsoft.Extensions.Http.Resilience
dotnet add package Microsoft.Extensions.Resilience
dotnet add package Polly
```

Ejecutar `dotnet add package` para cada paquete en el directorio del `.csproj` principal.

---

## PASO 4: Generar código

1. **Leer plantilla desde `.claude/skills/resilience-patterns/templates/ResiliencePolicies.cs.template`**
   - Si la plantilla no existe, generar el código directamente

2. **Reemplazar variables:**:
   - `{{NombreProyecto}}` con el nombre del proyecto detectado
   - `{{Namespace}}` con el namespace correspondiente

3. **Crear archivo de políticas:**:
   - Ruta: `03_Desarrollo/src/Comillas.{0}.Infrastructure/Resilience/ResiliencePolicies.cs`
   - Contenido: clase estática con métodos de extensión para cada política configurada
   - Retry: backoff exponencial con jitter (Polly.Contrib.WaitAndRetry decorrelado)
   - Circuit Breaker: configurar umbral de fallos, duración de apertura, sampling duration
   - Timeout: timeout por petición individual
   - Bulkhead: limitar concurrencia si seleccionado

4. **Modificar Program.cs:**:
   - Añadir `using` del namespace de Resilience
   - Para cada HttpClient, añadir las políticas configuradas:
     ```csharp
     // Si usa Standard Resilience Handler (recomendado):
     builder.Services.AddHttpClient<IBannerApiClient, BannerApiClient>(client =>
     {
         client.BaseAddress = new Uri(configuration["ExternalServices:Banner:BaseUrl"]!);
     })
     .AddStandardResilienceHandler();

     // Si usa configuración personalizada:
     builder.Services.AddHttpClient<IBannerApiClient, BannerApiClient>(client =>
     {
         client.BaseAddress = new Uri(configuration["ExternalServices:Banner:BaseUrl"]!);
     })
     .AddResilienceHandler("banner", builder =>
     {
         builder.AddRetry(new HttpRetryStrategyOptions
         {
             MaxRetryAttempts = 3,
             BackoffType = DelayBackoffType.Exponential,
             UseJitter = true,
             Delay = TimeSpan.FromSeconds(1)
         });
         builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
         {
             SamplingDuration = TimeSpan.FromSeconds(60),
             FailureRatio = 0.5,
             MinimumThroughput = 5,
             BreakDuration = TimeSpan.FromSeconds(30)
         });
         builder.AddTimeout(TimeSpan.FromSeconds(30));
     });
     ```

5. **Añadir configuración a appsettings.json:**:
   - Sección `Resilience` con valores configurables por servicio:
     ```json
     {
       "Resilience": {
         "BannerApi": {
           "RetryCount": 3,
           "RetryBaseDelay": "00:00:01",
           "CircuitBreakerFailureRatio": 0.5,
           "CircuitBreakerBreakDuration": "00:00:30",
           "TimeoutSeconds": 30
         }
       }
     }
     ```

---

## PASO 5: Verificar y resumen

1. **Compilar el proyecto:**:
```powershell
dotnet build 03_Desarrollo/src/Comillas.[Nombre].Web/
```

2. **Verificar que compila sin errores**

3. **Mostrar resumen:**:
```
+---------------------------------------------------------+
|  RESILIENCIA CONFIGURADA                                |
+---------------------------------------------------------+
|                                                         |
|  Servicios configurados:                                |
|    BannerApiClient:                                     |
|      [x] Retry: 3 intentos, exponential+jitter          |
|      [x] Circuit Breaker: 5 fallos -> 30s abierto       |
|      [x] Timeout: 30s                                   |
|    OracleHcmClient:                                     |
|      [x] Standard Resilience Handler                    |
|                                                         |
|  Archivos creados/modificados:                          |
|    + Infrastructure/Resilience/ResiliencePolicies.cs    |
|    ~ Program.cs                                         |
|    ~ appsettings.json                                   |
|                                                         |
|  Comportamiento del Circuit Breaker:                    |
|    CERRADO -> normal, peticiones pasan                  |
|    ABIERTO -> falla rápido, no llama al servicio        |
|    SEMI-ABIERTO -> prueba con 1 petición                |
|                                                         |
+---------------------------------------------------------+
```

---

## RECORDATORIOS CRÍTICOS

- **Siempre usar jitter** con backoff exponencial para evitar thundering herd
- **No reintentar errores 4xx** (errores del cliente) - solo reintentar 5xx y timeouts
- **Circuit breaker protege servicios downstream** - no quitar aunque parezca innecesario
- **Timeout por petición** debe ser menor que el timeout total del retry
- Los valores de configuración deben ser **ajustables via appsettings** sin recompilar
- En producción, monitorizar los eventos del circuit breaker con logs/métricas
- **Skill de referencia**: `.claude/skills/resilience-patterns/`
- **Guía de referencia**: `Documentos_Base/07_Resiliencia/GUIA_RESILIENCIA.md`
