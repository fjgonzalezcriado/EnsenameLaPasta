Configura health checks (/health, /ready, /live) en el proyecto

# Configurar Health Checks

Wizard para configurar health checks en el proyecto siguiendo los estándares de Comillas.

---

## PASO 1: Detectar proyecto

1. **Buscar proyecto principal en `03_Desarrollo/`:**
   - Localizar archivos `.csproj` (buscar el proyecto Web/API principal)
   - Detectar `TargetFramework` (net10.0, net9.0, etc.)
   - Localizar `Program.cs`
   - Buscar configuración de health checks existente (`AddHealthChecks`, `MapHealthChecks`)

2. **Si ya existen health checks:**:
   - Informar al usuario que ya hay configuración previa
   - Preguntar si desea ampliarla o reconfigurar desde cero

3. **Mostrar resumen de detección:**:
```
Proyecto detectado: Comillas.{0}.Web
Framework: {0}
Program.cs: {0}
Health checks existentes: {0}
```

---

## PASO 2: Wizard interactivo

Preguntar al usuario qué checks desea configurar:

```
Selecciona los health checks a configurar:

OBLIGATORIOS (siempre incluidos):
  [x] SQL Server       - Verifica conexión a base de datos
  [x] Self check       - Verifica que la aplicación responde

OPCIONALES:
  [ ] Redis            - Verifica conexión a cache Redis
  [ ] Azure Blob       - Verifica acceso a Azure Blob Storage
  [ ] Custom business  - Check personalizado de lógica de negocio
  [ ] Health UI        - Dashboard visual (solo desarrollo)

Cuales deseas añadir? (separados por coma, ej: redis,blob)
```

**Nota**: SQL Server y Self check son **obligatorios** y se incluyen siempre, no se pueden desmarcar.

---

## PASO 3: Añadir NuGets

Según la selección del PASO 2, listar e instalar los paquetes necesarios:

**Siempre incluidos:**
```powershell
dotnet add package AspNetCore.HealthChecks.SqlServer
dotnet add package AspNetCore.HealthChecks.UI.Client
```

**Condicionales:**
```powershell
# Si Redis seleccionado:
dotnet add package AspNetCore.HealthChecks.Redis

# Si Azure Blob seleccionado:
dotnet add package AspNetCore.HealthChecks.AzureBlobStorage

# Si Health UI seleccionado:
dotnet add package AspNetCore.HealthChecks.UI
dotnet add package AspNetCore.HealthChecks.UI.InMemory.Storage
```

Ejecutar `dotnet add package` para cada paquete seleccionado en el directorio del `.csproj` principal.

---

## PASO 4: Generar código

1. **Leer plantilla desde `.claude/skills/resilience-patterns/templates/HealthChecksConfig.cs.template`**
   - Si la plantilla no existe, generar el código directamente

2. **Reemplazar variables:**:
   - `{{NombreProyecto}}` con el nombre del proyecto detectado (ej: `Comillas.MiApp`)
   - `{{Namespace}}` con el namespace correspondiente

3. **Crear archivo de configuración:**:
   - Ruta: `03_Desarrollo/src/Comillas.{0}.Infrastructure/HealthChecks/HealthChecksConfig.cs`
   - Contenido: clase estática con método de extensión `AddCustomHealthChecks()`
   - Incluir checks seleccionados en PASO 2::
     - SQL Server: verificar conexión con query `SELECT 1`
     - Self: endpoint básico que retorna Healthy
     - Redis: verificar conexión con ping
     - Azure Blob: verificar acceso al container
     - Custom: plantilla para check de negocio personalizado

4. **Modificar Program.cs:**:
   - Añadir `using` del namespace de HealthChecks
   - Añadir `builder.Services.AddCustomHealthChecks(builder.Configuration);` en la sección de servicios
   - Añadir mappings de endpoints::
     ```csharp
     app.MapHealthChecks("/health", new HealthCheckOptions
     {
         ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
     });
     app.MapHealthChecks("/health/ready", new HealthCheckOptions
     {
         Predicate = check => check.Tags.Contains("ready")
     });
     app.MapHealthChecks("/health/live", new HealthCheckOptions
     {
         Predicate = check => check.Tags.Contains("live")
     });
     ```
   - Si Health UI seleccionado, añadir `app.MapHealthChecksUI();`

5. **Añadir configuración a appsettings.json:**:
   - Sección `HealthChecks` con timeouts y configuraciones específicas
   - Connection strings referenciadas desde la sección existente

---

## PASO 5: Verificar

1. **Compilar el proyecto:**:
```powershell
dotnet build 03_Desarrollo/src/Comillas.[Nombre].Web/
```

2. **Verificar que compila sin errores**

3. **Si hay errores: mostrarlos al usuario y sugerir correcciones**

---

## PASO 6: Resumen

Mostrar resumen final:

```
+---------------------------------------------------------+
|  HEALTH CHECKS CONFIGURADOS                             |
+---------------------------------------------------------+
|                                                         |
|  Endpoints:                                             |
|    GET /health       -> Todos los checks                |
|    GET /health/ready -> Checks de disponibilidad        |
|    GET /health/live  -> Checks de vida (básico)         |
|                                                         |
|  Checks activos:                                        |
|    [x] SQL Server (obligatorio)                         |
|    [x] Self check (obligatorio)                         |
|    [x/--] Redis                                         |
|    [x/--] Azure Blob Storage                            |
|    [x/--] Custom business                               |
|    [x/--] Health UI                                     |
|                                                         |
|  Archivos modificados:                                  |
|    + Infrastructure/HealthChecks/HealthChecksConfig.cs   |
|    ~ Program.cs                                         |
|    ~ appsettings.json                                   |
|                                                         |
|  Probar con:                                            |
|    curl https://localhost:5001/health                    |
|    curl https://localhost:5001/health/ready              |
|    curl https://localhost:5001/health/live               |
|                                                         |
+---------------------------------------------------------+
```

**Checklist post-configuración:**
- [ ] Verificar connection string de SQL Server en appsettings
- [ ] Si Redis: verificar connection string de Redis
- [ ] Si Azure Blob: verificar connection string de Storage
- [ ] Configurar monitoring en Azure para alertar si /health falla
- [ ] No exponer información sensible en respuestas de health en producción

---

## RECORDATORIOS CRÍTICOS

- **SQL Server** y **Self check** son SIEMPRE obligatorios, nunca omitirlos
- **Nunca exponer información sensible** en las respuestas de health check en producción
- Los health checks deben ser **ligeros y rápidos** (< 5 segundos)
- Usar **tags** para diferenciar entre checks de readiness y liveness
- En producción, considerar **restringir acceso** a los endpoints de health
- **Skill de referencia**: `.claude/skills/resilience-patterns/`
- **Guía de referencia**: `Documentos_Base/07_Resiliencia/GUIA_RESILIENCIA.md`
