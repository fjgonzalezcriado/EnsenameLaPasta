# CLAUDE_BASE_COMILLAS.md

> **Versión**: 3.7.0
> **Fecha**: Enero 2026  
> **Organización**: Universidad Pontificia Comillas - STIC

---

## Estándares Generales de Desarrollo

Este documento contiene los estándares y convenciones que Claude debe aplicar en **todos** los proyectos de Comillas, independientemente de la versión de .NET o la arquitectura específica.

---

## 1. VERSIONES .NET SOPORTADAS

### Estado Actual (Enero 2026)

| Versión | Tipo | Soporte hasta | Recomendación |
|---------|------|---------------|---------------|
| **.NET 10** | LTS | Nov 2028 | ✅ **Usar para nuevos proyectos** |
| **.NET 9** | STS | **May 2026** | ⚠️ **Migrar a .NET 10 urgente** |
| **.NET 8** | LTS | **Nov 2026** | ⚠️ **Planificar migración a .NET 10** |
| **.NET 4.x** | Legacy | Mantenimiento | 📦 Mantener, migrar según prioridad |

### Política de Versiones

```
NUEVOS PROYECTOS      → .NET 10 (obligatorio)
EVOLUTIVOS .NET 10    → Mantener en .NET 10
EVOLUTIVOS .NET 9     → Migrar a .NET 10 antes de trabajar
EVOLUTIVOS .NET 8     → Evaluar migración según tamaño
EVOLUTIVOS .NET 4.x   → Mantener en 4.x (migración separada)
```

### Detección Automática

Claude debe detectar la versión del proyecto antes de aplicar reglas:

```xml
<!-- Buscar en .csproj -->
<TargetFramework>net10.0</TargetFramework>  <!-- .NET 10 -->
<TargetFramework>net9.0</TargetFramework>   <!-- .NET 9 -->
<TargetFramework>net8.0</TargetFramework>   <!-- .NET 8 -->
<TargetFramework>net48</TargetFramework>    <!-- .NET Framework 4.8 -->
```

---

## 2. NOMENCLATURA Y CONVENCIONES

### Espacios de Nombres

```csharp
// Estructura estándar
Comillas.[Área].[Proyecto].[Capa]

// Ejemplos
Comillas.RRHH.GestionBecas.Domain
Comillas.RRHH.GestionBecas.Application
Comillas.RRHH.GestionBecas.Infrastructure
Comillas.RRHH.GestionBecas.Web
Comillas.Academico.Matriculacion.Api
```

### Nombres de Clases

| Tipo | Convención | Ejemplo |
|------|------------|---------|
| Entidad | Sustantivo singular | `Beca`, `Estudiante` |
| Servicio | Sustantivo + Service | `BecaService`, `EmailService` |
| Repositorio | Sustantivo + Repository | `BecaRepository` |
| Controlador | Sustantivo + Controller | `BecasController` |
| Validador | Nombre + Validator | `CreateBecaRequestValidator` |
| DTO | Nombre + Dto | `BecaDto`, `BecaDetalleDto` |
| Request | Acción + Nombre + Request | `CreateBecaRequest` |
| Response | Nombre + Response | `PaginatedResponse<T>` |

### Nombres de Variables y Métodos

```csharp
// Variables - camelCase
var becaActual = ...;
var totalSolicitudes = ...;

// Métodos - PascalCase, verbo + sustantivo
public async Task<BecaDto?> GetByIdAsync(int id, CancellationToken ct)
public async Task<Beca> CreateAsync(CreateBecaRequest request, CancellationToken ct)
public bool EstaActiva()
public void Publicar()

// Métodos async - sufijo Async
GetByIdAsync, CreateAsync, DeleteAsync

// Métodos booleanos - prefijo Is/Has/Can/Esta/Tiene/Puede
IsValid(), HasSolicitudes(), CanPublish()
EstaActiva(), TieneSolicitudes(), PuedePublicar()
```

---

## 3. ESTRUCTURA DE PROYECTOS

### Clean Architecture (Proyectos nuevos .NET 10)

```
src/
├── Comillas.MiApp.Domain/           ← Entidades, Value Objects, Interfaces
├── Comillas.MiApp.Application/      ← Servicios, DTOs, Validadores
├── Comillas.MiApp.Infrastructure/   ← EF Core, Repositorios, Externos
└── Comillas.MiApp.Web/              ← Controllers, Views, Program.cs

tests/
├── Comillas.MiApp.Domain.Tests/
├── Comillas.MiApp.Application.Tests/
└── Comillas.MiApp.Integration.Tests/
```

### Proyectos Legacy (.NET 4.x)

Claude debe **respetar** la estructura existente:

```
MiProyecto/
├── MiProyecto.Web/          ← UI + Controllers
├── MiProyecto.Business/     ← Servicios + Lógica
├── MiProyecto.Data/         ← Acceso a datos
└── MiProyecto.Entities/     ← Modelos
```

**No imponer Clean Architecture** en proyectos legacy.

---

## 4. PATRONES DE CÓDIGO

### Inyección de Dependencias

```csharp
// ✅ CORRECTO - Constructor injection
public class BecaService : IBecaService
{
    private readonly IBecaRepository _repository;
    private readonly ILogger<BecaService> _logger;

    public BecaService(IBecaRepository repository, ILogger<BecaService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
}

// ❌ INCORRECTO - new directo
public class BecaService
{
    private readonly BecaRepository _repository = new BecaRepository();
}
```

### Async/Await

```csharp
// ✅ CORRECTO - Async completo con CancellationToken
public async Task<BecaDto?> GetByIdAsync(int id, CancellationToken ct = default)
{
    var beca = await _repository.GetByIdAsync(id, ct);
    return beca is null ? null : MapToDto(beca);
}

// ❌ INCORRECTO - .Result bloquea el hilo
public BecaDto? GetById(int id)
{
    var beca = _repository.GetByIdAsync(id).Result; // ❌ Bloqueante
    return MapToDto(beca);
}
```

### Null Safety (C# 13 / .NET 10)

```csharp
// ✅ CORRECTO - Nullable reference types
public async Task<BecaDto?> GetByIdAsync(int id, CancellationToken ct)
{
    var beca = await _repository.GetByIdAsync(id, ct);
    return beca?.MapToDto();
}

// ✅ CORRECTO - Pattern matching
if (beca is not null)
{
    // usar beca
}

// ✅ CORRECTO - Null coalescing
var nombre = beca?.Nombre ?? "Sin nombre";
```

---

## 5. LOGGING

### Estándar: Serilog

```csharp
// Configuración en Program.cs (.NET 10)
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// Uso estructurado
_logger.LogInformation("Beca {BecaId} creada por {Usuario}", beca.Id, usuario);
_logger.LogWarning("Intento de acceso denegado a beca {BecaId}", becaId);
_logger.LogError(ex, "Error al procesar beca {BecaId}", becaId);
```

### Niveles de Log

| Nivel | Uso |
|-------|-----|
| **Debug** | Desarrollo, detalle técnico |
| **Information** | Operaciones normales importantes |
| **Warning** | Situaciones anómalas no críticas |
| **Error** | Errores que requieren atención |
| **Fatal** | Errores que impiden funcionamiento |

---

## 6. MANEJO DE ERRORES

### Excepciones Personalizadas

```csharp
// Jerarquía de excepciones Comillas
public class ComillasException : Exception { ... }
public class DomainException : ComillasException { ... }
public class NotFoundException : ComillasException { ... }
public class ValidationException : ComillasException { ... }
public class BusinessRuleException : ComillasException { ... }
```

### ProblemDetails (APIs .NET 10)

```csharp
// Respuestas de error estándar RFC 7807
{
    "type": "https://comillas.edu/errors/not-found",
    "title": "Recurso no encontrado",
    "status": 404,
    "detail": "No existe una beca con ID 123",
    "instance": "/api/becas/123"
}
```

---

## 7. SEGURIDAD

### Secretos — política Comillas (WARN-first, BLOCK-when-mandated)

**Estado actual (2026-05-21)**: Sistemas Comillas **NO** requiere Azure Key Vault obligatorio aún. El entorno se considera seguro (red interna + AD + VPN). La mayoría de proyectos tienen secrets hardcoded en `appsettings.json` commiteados — es **deuda técnica conocida** que se migra progresivamente.

**Comportamiento del hook `secret-scanner.ps1`**:

| Versión | Política | Exit code | Acción |
|---|---|---|---|
| **v2.0.0** (actual) | WARN-first | `exit 1` (informativo) | Detecta + avisa + permite Write/Edit. Patrón `sql-nomenclatura-guard` |
| **v3.0.0** (futuro) | BLOCK-when-mandated | `exit 2` (bloquea) | Se activa cuando Sistemas mandate KV obligatorio |

**Cuándo escala a v3.0.0**: solo si Sistemas Comillas anuncia política oficial "KV obligatorio en todos los proyectos producción". Hasta entonces, el hook informa pero no bloquea trabajo legítimo de mantenimiento.

### Patrón WARN-first vs BLOCK (cuándo aplicarlo)

Este patrón es general — aplicable a otros hooks defensivos del ecosistema:

| Caso | Política recomendada |
|---|---|
| Defecto crítico, daño irreversible (force push, DROP TABLE) | **BLOCK** (exit 2) — `bash-guard.ps1` |
| Convención que aún tiene deuda legacy masiva | **WARN-first** (exit 1) — `secret-scanner.ps1`, `sql-nomenclatura-guard.ps1` |
| Convención nueva sin deuda | **BLOCK** (exit 2) directamente |

**Regla**: hook nunca debe romper trabajo legítimo de mantenimiento si la "violación" ya existe en git history y no hay política organizacional que lo mandate. La defensa correcta es **visibilidad** (warning visible en cada Edit), no **fricción** (block que obliga workaround).

### Migración progresiva a Key Vault

Si tu proyecto NO usa KV aún:

1. **NO bloquea trabajo**: warnings son informativos, edita normal
2. **Documenta en `_duran/DEUDA_TECNICA.md`** entradas tipo `SEC-XXX: secretos en appsettings`
3. **Migra cuando puedas**: hay skill `azure-config` con templates KV listos
4. **Sin deadline forzado**: cada proyecto decide su ritmo. Sistemas anunciará oficialmente cuando enforcement entre

### Buenas prácticas (recomendadas, no obligatorias hoy)

```csharp
// ❌ Anti-patrón (genera AVISO, no bloquea)
var connectionString = "Server=prod;Password=123456";
var apiKey = "sk-xxxxx";

// ✅ Desarrollo - User Secrets
dotnet user-secrets set "ConnectionStrings:Default" "Server=..."

// ✅ Producción - Azure Key Vault (futuro estándar)
builder.Configuration.AddAzureKeyVault(...);

// ✅ Aceptable transitoriamente - appsettings con secret + DEUDA_TECNICA documentada
// (warning visible cada Edit, deuda explícita, plan de migración registrado)
```

### Autenticación

```csharp
// Azure AD / Microsoft Entra ID (estándar Comillas)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
```

### Autorización

```csharp
// Roles Comillas estándar
[Authorize(Roles = "Admin")]           // Administradores
[Authorize(Roles = "Gestor")]          // Gestores funcionales
[Authorize(Roles = "Usuario")]         // Usuarios normales
[Authorize(Policy = "RequireGestor")]  // Políticas personalizadas
```

---

## 8. BASE DE DATOS

### 8.1 SQL Server — versión y nomenclatura (OBLIGATORIO · aplica en archivos Y chat)

> **Esta sección siempre está en contexto** (CLAUDE.md siempre cargado) para que la nomenclatura se aplique también cuando Claude propone SPs en chat sin editar un archivo `.sql`.

#### Versión por defecto: SQL Server 2017

**Regla**: salvo que el proyecto indique explícitamente otra versión en `_duran/ESTADO_PROYECTO.json` o en su propio `CLAUDE.md`, usar **SQL Server 2017** como target. Esto garantiza que los scripts generados sean compatibles con la mayoría del parque de servidores Comillas.

Features **disponibles en 2017** que sí se pueden usar:
- `STRING_AGG` (reemplaza `FOR XML PATH` para concatenar)
- `TRIM`, `TRANSLATE`, `CONCAT_WS`
- `OPENJSON` (básico, sin `WITH` path strict)
- System-versioned temporal tables
- Columnstore indexes actualizables
- `SELECT … INTO` con paralelismo
- Graph tables (aunque raramente se usen)
- Adaptive Query Processing básico
- Resumable online index rebuild

Features **POST-2017 a NO usar** salvo instrucción explícita:

| Feature | Versión mínima | No usar porque |
|---|---|---|
| `STRING_SPLIT` con parámetro de ordinal | 2022 | En 2017 sólo devuelve `value` (sin `ordinal`) |
| `GENERATE_SERIES` | 2022 | No existe en 2017, usar tabla de números o CTE recursivo |
| `DATE_BUCKET` | 2022 | Simular con aritmética `DATEADD/DATEDIFF` |
| `IS [NOT] DISTINCT FROM` | 2022 | Usar `((a = b) OR (a IS NULL AND b IS NULL))` |
| `GREATEST` / `LEAST` | 2022 | Usar `CASE` o subquery con `VALUES` |
| `BIT_COUNT`, `LEFT_SHIFT`, `RIGHT_SHIFT` | 2022 | Funciones bitwise 2022 |
| `APPROX_PERCENTILE_CONT` | 2022 | Usar `PERCENTILE_CONT` dentro de CTE |
| `OPENJSON WITH (..., PATH strict)` avanzado | 2022 | Sintaxis `strict` añadida en 2022 |
| Always Encrypted con enclaves | 2019 | Requiere infraestructura adicional |
| Inline Table Variables de 100+ filas optimizadas | 2019 | Mejora de 2019; en 2017 peor plan |
| UTF-8 collations (`_UTF8`) | 2019 | No existen en 2017 |

Features **anteriores a 2017 a NO usar** por obsolescencia:
- `TEXT`, `NTEXT`, `IMAGE` → usar `NVARCHAR(MAX)`, `VARBINARY(MAX)`
- Concatenación manual con `FOR XML PATH` cuando `STRING_AGG` cubre el caso
- Cursores que se pueden reescribir como `CROSS APPLY` o CTE

#### Nomenclatura Comillas — tabla patrón

| Tipo | Patrón | OK | KO |
|---|---|---|---|
| **Stored Procedure** | `{schema}.{Acción}{Entidad}` | `ewp.ObtenerNominacion`, `GesInter.ListarAliases` | `usp_ObtenerNominacion`, `GesInter.Alias_Listar`, `sp_GetAliases`, `pr_ObtenerNominacion` |
| **Función escalar** | `{schema}.fn_{Descripcion}` | `ewp.fn_CalcularTasa` | `ewp.CalcularTasa` (falta prefijo), `ewp.func_CalcTasa` |
| **Función tabla (TVF)** | `{schema}.fnt_{Descripcion}` | `ewp.fnt_BecasPorAnio` | `ewp.fn_BecasPorAnio` (usa `fn_` en vez de `fnt_`) |
| **Vista** | `{schema}.vw_{Descripcion}` | `ewp.vw_NominacionesActivas` | `ewp.Vista_Activas`, `ewp.v_Activas`, `ewp.Activas` |
| **Trigger** | `{schema}.tr_{Tabla}_{Evento}` | `academico.tr_Estudiante_AfterInsert` | `academico.trigger_est_ins`, `academico.trg_Estudiante` |
| **Índice** | `IX_{Tabla}_{Columnas}` | `IX_Estudiante_Email` | `idx1`, `index_email` |
| **Constraint PK/FK** | `PK_{Tabla}` / `FK_{Hija}_{Padre}` | `PK_Estudiante`, `FK_Solicitud_Estudiante` | `PrimaryKey_1`, `FK_1` |

#### Verbos permitidos para SPs (no exhaustivo)

`Listar`, `Obtener`, `Buscar`, `Insertar`, `Actualizar`, `Eliminar`, `Guardar`, `Procesar`, `Anadir`, `Aprobar`, `Cancelar`, `Activar`, `Desactivar`, `Validar`, `Importar`, `Exportar`, `Registrar`, `Notificar`, `Consultar`, `Contar`, `Existe`, `Generar`, `Calcular`, `Asignar`, `Liberar`, `Marcar`, `Desmarcar`, `Enviar`, `Recibir`, `Sincronizar`, `Mover`, `Copiar`, `Archivar`, `Restaurar`.

#### Schemas Comillas

**Lowercase** (convención moderna): `academico`, `financiero`, `ewp`, `rrhh`, `biblioteca`, `licencias`, `sync`.

**PascalCase legacy permitido** (mantener tal cual, NO normalizar): `GesInter`, `PlazasIntercambio`. Son nombres históricos del área de Gestión de Intercambios y se respetan por compatibilidad.

#### Prohibido

- **Sufijos**: `_Listar`, `_Guardar`, `_Eliminar`, `_Leer`, `_Actualizar`, `_Insertar`, `_L`, `_G`, `_E`, `_A`.
  - KO: `GesInter.Alias_Listar` → OK: `GesInter.ListarAliases`.
- **Prefijos**: `usp_`, `sp_`, `pr_`, `proc_`, `pa_`.
  - KO: `ewp.usp_ObtenerNominacion` → OK: `ewp.ObtenerNominacion`.
- **Separador `_` entre verbo y sustantivo**: `ObtenerNominacion` (pegado PascalCase), no `Obtener_Nominacion`.
- **Nombres sin schema**: siempre prefijar con schema, incluso en `dbo` (`dbo.Insertar…`).
- **Funciones sin prefijo**: `fn_` para escalares, `fnt_` para tabla.

#### Excepciones — código legacy

Si se toca código migrado que ya tiene convención vieja (ej. `dbo.int_*`, `usp_GetX`, `Alias_Listar`), **mantener los nombres existentes**. NO renombrar en el mismo commit donde se toca la lógica — eso provoca conflictos en `sqlproj` y referencias cruzadas.

Marcadores que activan el modo "legacy respetar":
- El archivo está en `Legacy/`, `Obsoleto/`, `MigracionPendiente/`.
- Hay un comentario `-- LEGACY: no renombrar` al inicio del archivo.
- El nombre actual del SP empieza por `usp_`, `sp_`, `pr_` (asumir código migrado).
- La ruta relativa del archivo está listada en `.claude/sql-legacy-baseline.txt` (baseline de legacy del proyecto).

> ⚠️ **TRAMPA — BD cross-schema con SPs legacy mayoritarios**
>
> La regla "respetar legacy" **SOLO aplica al MODIFICAR SPs existentes**. Cuando creas un SP **nuevo** en una BD que ya tiene SPs con patrón antiguo (ej. `SecreBD` llena de `int_ListaTitulaciones_leer`, `int_ListaSemestres_leer`), **NO replicar ese patrón**. Los SPs nuevos siempre siguen §8.1, incluso siendo el único moderno en su BD.
>
> El sesgo de imitación visual ("toda la BD usa `int_*_leer`, el mío también para coherencia") es un razonamiento defectuoso. El estándar Comillas se aplica a código nuevo independientemente del patrón vecino. Si el SP nuevo debe coexistir con la convención legacy, está bien: la coherencia se logra **migrando los legacy progresivamente**, no clonando el patrón viejo.
>
> **Antes de cualquier `CREATE PROCEDURE [schema].[Nombre]` en archivo nuevo:** verificar la tabla §8.1 (verbos: `Listar`, `Obtener`, `Insertar`, `Actualizar`, `Eliminar`, etc.). NO inferir nomenclatura por archivos vecinos.
>
> Caso real: en Abril 2026 se creó `int_ListaCarreras_leer` en `SecreBD` imitando `int_ListaTitulaciones_leer`. Lo correcto era `ListarCarreras` (con schema apropiado). El hook `sql-nomenclatura-guard.ps1` v1.1.0+ detecta esta trampa con la regla 8 (prefijos legacy custom: `int_`, `aud_`, `tmp_`, `migr_`).

#### Baseline legacy (`.claude/sql-legacy-baseline.txt`)

Para proyectos con mucho código SQL preexistente que incumple la nomenclatura nueva, se puede generar un **baseline** que el hook `sql-nomenclatura-guard.ps1` respetará. El baseline es un archivo de texto con una ruta relativa por línea (forward-slashes), ej:

```
03_Desarrollo/MiApp/BaseDatos/StoredProcedures/E4/Alias_Listar.sql
03_Desarrollo/MiApp/BaseDatos/StoredProcedures/ActualizarAliasEstudiante.sql
```

Generación automática:

```powershell
pwsh .claude/hooks/sql-baseline-generate.ps1            # escribe .claude/sql-legacy-baseline.txt
pwsh .claude/hooks/sql-baseline-generate.ps1 -DryRun    # solo muestra qué archivos incluiría
```

Comportamiento: los archivos en baseline se editan sin bloqueos, pero el hook emite una **nota no bloqueante** recordando actualizar el baseline si el archivo se renombra. Archivos NUEVOS o fuera del baseline siguen sujetos a la nomenclatura estricta. Se recomienda commitear el baseline al repo.

#### Plantilla mínima de SP correcta

```sql
-- =============================================
-- Autor:           STIC Comillas
-- Fecha Creación:  YYYY-MM-DD
-- Descripción:     {qué hace}
-- SQL Server:      2017+
-- =============================================
CREATE OR ALTER PROCEDURE [ewp].[ObtenerNominacion]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SELECT Id, EstudianteId, Estado, FechaCreacion
    FROM ewp.Nominacion
    WHERE Id = @Id;
END
GO
```

---

### 8.2 Entity Framework Core 10 (Proyectos nuevos)

```csharp
// Configuración recomendada
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(3);
        sqlOptions.CommandTimeout(30);
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }));
```

### 8.3 Entity Framework 6 (Legacy)

```csharp
// Mantener patrones existentes
public class MiDbContext : DbContext
{
    public MiDbContext() : base("name=DefaultConnection") { }
}
```

### 8.4 Consultas Optimizadas

```csharp
// ✅ Proyección - Solo lo necesario
var dtos = await _context.Becas
    .Where(b => b.Estado == EstadoBeca.Activa)
    .Select(b => new BecaDto { Id = b.Id, Nombre = b.Nombre })
    .ToListAsync(ct);

// ✅ AsNoTracking para lecturas
var becas = await _context.Becas
    .AsNoTracking()
    .ToListAsync(ct);

// ✅ Paginación
var page = await _context.Becas
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(ct);
```

---

## 9. TESTING

### Framework Estándar

```xml
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="FluentAssertions" Version="6.*" />
<PackageReference Include="Moq" Version="4.*" />
<PackageReference Include="Testcontainers.MsSql" Version="3.*" />
```

### Nomenclatura

```
Método_Escenario_ResultadoEsperado

Ejemplos:
GetById_BecaExiste_RetornaBeca
GetById_BecaNoExiste_RetornaNull
Create_NombreVacio_LanzaValidationException
```

### Cobertura Mínima

| Capa | Mínimo |
|------|--------|
| Domain | 90% |
| Application | 80% |
| Infrastructure | 60% |

---

## 10. DOCUMENTACIÓN

### Comentarios XML (APIs públicas)

```csharp
/// <summary>
/// Obtiene una beca por su identificador.
/// </summary>
/// <param name="id">Identificador único de la beca.</param>
/// <param name="ct">Token de cancelación.</param>
/// <returns>La beca si existe, null en caso contrario.</returns>
/// <exception cref="ArgumentException">Si el id es menor o igual a 0.</exception>
public async Task<BecaDto?> GetByIdAsync(int id, CancellationToken ct = default)
```

### README de Proyecto

Todo proyecto debe tener README.md con:
- Descripción del proyecto
- Requisitos previos
- Instrucciones de instalación
- Configuración necesaria
- Cómo ejecutar tests

---

## 11. GIT Y COMMITS

### Conventional Commits

```
tipo(ámbito): descripción corta

Tipos:
- feat: Nueva funcionalidad
- fix: Corrección de bug
- docs: Documentación
- style: Formato (sin cambio de código)
- refactor: Refactorización
- test: Tests
- chore: Tareas de mantenimiento

Ejemplos:
feat(becas): añadir filtro por estado
fix(solicitudes): corregir validación de fecha
docs(readme): actualizar instrucciones de instalación
```

### Ramas

> La nomenclatura y rama base se leen de `configuracion.branching` en `_duran/ESTADO_PROYECTO.json`.

**Nomenclatura semántica** (default):
```
main              ← Producción
develop           ← Desarrollo (solo GitFlow)
feature/XXX-desc  ← Nuevas funcionalidades
bugfix/XXX-desc   ← Correcciones
hotfix/XXX-desc   ← Urgentes en producción
```

**Nomenclatura temporal** (developer-branch y otros):
```
dev.{usuario}               ← Rama personal remota
yyyyMMdd-DT-nnn-desc        ← Deuda técnica (local)
yyyyMMdd-HV-nnn-desc        ← Evolutivo (local)
yyyyMMdd-BUG-nnn-desc       ← Corrección (local)
```

**Rama base configurable**: `main`, `master`, `develop`, `dev.{usuario}` — según `configuracion.branching.ramaBase`.

---

## 12. CONSIDERACIONES ESPECIALES

### Proyectos .NET 4.x

Cuando Claude trabaje con proyectos legacy:

1. **Respetar arquitectura existente** - No imponer Clean Architecture
2. **Mantener patrones del proyecto** - Usar los mismos que ya existen
3. **No introducir dependencias incompatibles** - Verificar compatibilidad
4. **Entity Framework 6** - No usar EF Core
5. **Web.config** - No usar appsettings.json

### Migración a .NET 10

Cuando se planifique migración:

1. **Analizar dependencias** - Verificar compatibilidad con .NET 10
2. **Migrar por capas** - Domain → Application → Infrastructure → Web
3. **Tests de regresión** - Antes y después de cada capa
4. **No cambiar comportamiento** - Solo migrar, no mejorar simultáneamente

---

## 13. CONTACTO Y SOPORTE

- **Equipo**: STIC - Servicio de Tecnologías de la Información
- **Email**: soporte.stic@comillas.edu
- **Documentación**: [Interno Comillas]

---

## 14. REFERENCIAS A DOCUMENTACIÓN COMPLEMENTARIA

Este documento define **cómo escribir código**. Para información detallada sobre **infraestructura, seguridad avanzada y entorno de desarrollo**, Claude debe consultar:

### 📚 @Documentos_Base/01_Estructura_Tecnica/ESTRUCTURA_TECNICA.md

Contiene información **CRÍTICA** y **OBLIGATORIA** sobre:

| Sección | Tema | Cuándo consultar |
|---------|------|------------------|
| **3. Seguridad** | OWASP Top 10, checklist obligatorio | Siempre en código que maneje datos |
| **3.2** | Azure Key Vault (OBLIGATORIO) | Cualquier secreto o credencial |
| **3.4** | Validación de inputs (FluentValidation) | Cualquier entrada de usuario |
| **4. Infraestructura Balanceada** | Principio "sin estado local" | Diseño de cualquier servicio |
| **4.2** | Azure Blob Storage (OBLIGATORIO) | Almacenamiento de archivos |
| **4.3** | Redis Caché Distribuida | Caché, sesiones |
| **4.4** | Tabla NO hacer / SÍ hacer | Decisiones de arquitectura |
| **5. Proyectos Legacy** | WebForms, Web.config, hardening | Proyectos .NET 4.x |
| **6. Docker Compose** | SQL Server, Redis, Azurite, Mailhog | Entorno de desarrollo |
| **10. Application Insights** | TelemetryClient, TrackEvent | Telemetría avanzada |

### ⚠️ REGLAS DE CONSULTA OBLIGATORIA

Claude **DEBE** consultar ESTRUCTURA_TECNICA.md cuando trabaje en:

```
ALMACENAMIENTO DE ARCHIVOS
├── ❌ NO usar: wwwroot/uploads/, disco local, Path.GetTempPath()
├── ✅ SÍ usar: Azure Blob Storage
└── 📖 Consultar: Sección 4.2 - Código completo de BlobStorageService

CACHÉ Y SESIONES
├── ❌ NO usar: MemoryCache, Session en memoria
├── ✅ SÍ usar: Redis (IDistributedCache)
└── 📖 Consultar: Sección 4.3 - Configuración y uso

SEGURIDAD
├── 📖 Consultar: Sección 3.1 - OWASP Top 10 completo
├── 📖 Consultar: Sección 3.2 - Key Vault obligatorio
└── 📖 Consultar: Sección 3.4 - FluentValidation

PROYECTOS WEBFORMS / .NET 4.x
├── 📖 Consultar: Sección 5 - Headers de seguridad Web.config
├── 📖 Consultar: Sección 5.4 - Documento de Decisión (plantilla)
└── ⚠️ Generar análisis de riesgos antes de modificar

ENTORNO DE DESARROLLO
├── 📖 Consultar: Sección 6.1 - Docker Compose (puertos, imágenes)
├── 📖 Consultar: Sección 6.2 - Archivos de configuración
└── ⚠️ Intercalación BD: SQL_Latin1_General_CP1250_CI_AS

LOGGING AVANZADO
├── 📖 Consultar: Sección 10.1 - Application Insights con TelemetryClient
├── 📖 Consultar: Sección 10.3 - Qué NO loguear (datos sensibles)
└── ⚠️ NUNCA loguear: contraseñas, tokens, DNI, emails completos
```

### 📋 Información específica en ESTRUCTURA_TECNICA (no duplicada aquí)

| Tema | Por qué no está aquí |
|------|---------------------|
| Código completo `BlobStorageService` | Implementación de ~30 líneas |
| Código completo `Redis IDistributedCache` | Implementación con opciones |
| Checklist OWASP Top 10 | Tabla detallada con 10 vulnerabilidades |
| Headers seguridad Web.config | XML específico para legacy |
| Docker Compose completo | YAML con 4 servicios |
| `TelemetryClient` avanzado | Código con `StartOperation`, `TrackEvent` |

### 🔗 Otras referencias en la plantilla

| Archivo | Contenido |
|---------|-----------|
| `.claude/rules/*.md` | Reglas condicionales por tipo de archivo |
| `_duran/ESTADO_PROYECTO.json` | Estado actual del proyecto |
| `_duran/DEPENDENCIAS.md` | Dependencias y versiones |
| `Documentos_Base/05_Plantillas_SQL/` | Plantillas SQL estándar |

---

*Este documento es la base de estándares para todos los proyectos.*  
*Las reglas condicionales en `.claude/rules/` complementan con detalles específicos.*  
*Para infraestructura y seguridad avanzada, consultar `ESTRUCTURA_TECNICA.md`.*

---

**Versión**: 3.7.0
**Última actualización**: Marzo 2026
