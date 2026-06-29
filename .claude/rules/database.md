---
globs:
  - "**/*.sql"
  - "**/Scripts/**/*.sql"
  - "**/Migrations/**/*.sql"
  - "**/StoredProcedures/**/*.sql"
  - "**/Procedures/**/*.sql"
  - "**/Views/**/*.sql"
  - "**/Functions/**/*.sql"
  - "**/Triggers/**/*.sql"
  - "**/Tables/**/*.sql"
  - "**/Indexes/**/*.sql"
  - "**/Seeds/**/*.sql"
  - "**/Data/**/*.sql"
  - "**/*_Create.sql"
  - "**/*_Alter.sql"
  - "**/*_Drop.sql"
  - "**/*_Insert.sql"
  - "**/*_Update.sql"
  - "**/*_Migration.sql"
  - "**/Repositorios/**/*.cs"
  - "**/Repositories/**/*.cs"
  - "**/*Repository.cs"
  - "**/*Repositorio.cs"
  - "**/ProcedimientoAlmacenado.cs"
  - "**/StoredProcedures/**/*.cs"
  - "**/DbContext.cs"
  - "**/*DbContext.cs"
---

# Reglas para Base de Datos - SQL Server / Azure SQL

> Este archivo aplica cuando Claude trabaja con scripts T-SQL, stored procedures, vistas, funciones y objetos de base de datos **o con repositorios C# que invocan SPs**.
> **Entorno:** SQL Server 2017 (default Comillas) · SQL Server 2019/2022/Azure SQL si el proyecto lo indica.

---

## ERRORES COMUNES A EVITAR (lee esto PRIMERO)

Antes de crear CUALQUIER objeto SQL, verificar nomenclatura Comillas. Estas son las violaciones más frecuentes:

| ❌ KO | ✅ OK | Razón |
|---|---|---|
| `CREATE PROCEDURE [dbo].[int_ListaCarreras_leer]` (en SP **nuevo**) | `CREATE PROCEDURE academico.ListarCarreras` | **Trampa BD cross-schema**: SP nuevo en BD con SPs legacy → seguir §8.1, NO imitar vecinos |
| `CREATE PROCEDURE usp_ObtenerNominacion` | `CREATE PROCEDURE ewp.ObtenerNominacion` | Prohibidos los prefijos `usp_`, `sp_`, `pr_`, `proc_`, `pa_` |
| `CREATE PROCEDURE GesInter.Alias_Listar` | `CREATE PROCEDURE GesInter.ListarAliases` | Prohibidos los sufijos `_Listar`, `_Guardar`, `_Eliminar`, `_L`, `_G` |
| `CREATE PROCEDURE ewp.Obtener_Nominacion` | `CREATE PROCEDURE ewp.ObtenerNominacion` | Verbo + entidad **pegados** PascalCase, sin `_` |
| `CREATE PROCEDURE ObtenerNominacion` | `CREATE PROCEDURE ewp.ObtenerNominacion` | Siempre con schema (incluso `dbo`) |
| `CREATE FUNCTION ewp.CalcularTasa(...)` | `CREATE FUNCTION ewp.fn_CalcularTasa(...)` | Funciones escalares con prefijo `fn_` |
| `CREATE FUNCTION ewp.fn_BecasPorAnio() RETURNS TABLE` | `CREATE FUNCTION ewp.fnt_BecasPorAnio() RETURNS TABLE` | TVFs con prefijo `fnt_`, no `fn_` |
| `CREATE VIEW ewp.Activas` | `CREATE VIEW ewp.vw_Activas` | Vistas con prefijo `vw_` |
| `CREATE TRIGGER trg_Estudiante_Ins` | `CREATE TRIGGER academico.tr_Estudiante_AfterInsert` | Triggers con prefijo `tr_` + `{Tabla}_{Evento}` |
| `SELECT STRING_AGG(... ORDINAL)` | `STRING_AGG(...)` | `ORDINAL` es 2022+; en 2017 no existe |
| `SELECT * FROM GENERATE_SERIES(1, 10)` | CTE recursivo o tabla de números | `GENERATE_SERIES` es 2022+ |
| `a IS NOT DISTINCT FROM b` | `((a = b) OR (a IS NULL AND b IS NULL))` | `IS [NOT] DISTINCT FROM` es 2022+ |
| `GREATEST(a, b, c)` | `CASE WHEN …` o `(SELECT MAX(v) FROM (VALUES (a),(b),(c)) x(v))` | `GREATEST` / `LEAST` son 2022+ |
| Collation `*_UTF8` | Collation clásica `SQL_Latin1_General_CP1_CI_AI` | UTF-8 collations son 2019+ |

**Excepción legacy**: si el archivo está en `Legacy/`, `Obsoleto/`, `MigracionPendiente/` o tiene el comentario `-- LEGACY: no renombrar` al inicio, mantener la nomenclatura existente — NO corregir.

**Excepción schemas históricos**: `GesInter` y `PlazasIntercambio` son PascalCase legítimo (mantener tal cual). El resto de schemas nuevos: lowercase (`academico`, `financiero`, `ewp`, etc.).

> ⚠️ **Trampa común — BD cross-schema con muchos SPs legacy**
>
> Si creas un SP **nuevo** en una BD que ya tiene SPs con patrón antiguo (ej. `SecreBD` con `int_ListaTitulaciones_leer`, `int_ListaSemestres_leer`), **NO replicar ese patrón**. La regla "respetar legacy" SOLO aplica al **MODIFICAR** SPs existentes. Los SPs nuevos siempre siguen §8.1, incluso siendo el único moderno en su BD.
>
> Razonamiento defectuoso a evitar: "esta BD está llena de `int_*_leer` → coherencia → mi SP también". El estándar Comillas prevalece sobre la inferencia visual de archivos vecinos.
>
> Antes de cualquier `CREATE PROCEDURE` en archivo nuevo: verificar contra la tabla §8.1. El hook `sql-nomenclatura-guard.ps1` v1.1.0+ detecta prefijos legacy custom (`int_`, `aud_`, `tmp_`, `migr_`, `old_`, `bak_`) en SPs nuevos.

> Ver también: `CLAUDE_BASE_COMILLAS.md` §8.1 (siempre cargado en contexto) con la tabla completa + features 2017 permitidas vs features post-2017 a evitar.

---

## VERSIONES SOPORTADAS

**Default Comillas: SQL Server 2017.** No usar features de versiones posteriores sin indicación explícita del proyecto (campo `baseDatos.versionSqlServer` en `_duran/ESTADO_PROYECTO.json` o mención explícita en el `CLAUDE.md` del consumidor).

| Versión | Estado | Notas |
|---------|--------|-------|
| **SQL Server 2017** | ✅ **Default** | La mayoría del parque Comillas. Usar por defecto. |
| SQL Server 2019 | ✅ Soportado | Solo si el proyecto lo indica |
| SQL Server 2022 | ✅ Soportado | Solo si el proyecto lo indica |
| Azure SQL | ✅ Soportado | Consideraciones especiales |
| SQL Server 2016 | ⚠️ Limitado | Evitar nuevos desarrollos; mantenimiento solamente |

### Features POST-2017 a NO usar por defecto

| Feature | Versión mínima | Alternativa en 2017 |
|---|---|---|
| `STRING_SPLIT` con `ordinal` | 2022 | Sólo `value` disponible en 2017 |
| `GENERATE_SERIES` | 2022 | Tabla de números o CTE recursivo |
| `DATE_BUCKET` | 2022 | Aritmética con `DATEADD` / `DATEDIFF` |
| `IS [NOT] DISTINCT FROM` | 2022 | `((a = b) OR (a IS NULL AND b IS NULL))` |
| `GREATEST` / `LEAST` | 2022 | `CASE` o subquery con `VALUES` |
| `BIT_COUNT`, `LEFT_SHIFT`, `RIGHT_SHIFT` | 2022 | Funciones bitwise manuales |
| `APPROX_PERCENTILE_CONT` | 2022 | `PERCENTILE_CONT` dentro de CTE |
| `OPENJSON WITH PATH strict` | 2022 | `PATH` sin `strict` |
| UTF-8 collations (`*_UTF8`) | 2019 | Collations clásicas `SQL_Latin1_General_*` |
| Inline TVF optimizaciones avanzadas | 2019 | Reescribir como Table Variable o temp table |
| Always Encrypted con enclaves | 2019 | Always Encrypted básico |

### Features SÍ disponibles en 2017 (usar con confianza)

`STRING_AGG`, `TRIM`, `TRANSLATE`, `CONCAT_WS`, `OPENJSON` básico, temporal tables system-versioned, columnstore actualizable, Graph tables, Adaptive Query Processing básico, Resumable online index rebuild.

---

## PARTE 1: CONVENCIONES DE NOMBRADO - COMILLAS

### 1.1 Reglas Generales

| Elemento | Convención | Ejemplo | ❌ Evitar |
|----------|------------|---------|-----------| 
| **Tablas** | PascalCase, singular | `Estudiante`, `SolicitudBeca` | `estudiantes`, `tbl_Estudiante` |
| **Columnas** | PascalCase | `FechaNacimiento`, `NumeroDocumento` | `fecha_nacimiento`, `fNac` |
| **Primary Key** | `Id` o `{Tabla}Id` | `Id`, `EstudianteId` | `ID`, `id_estudiante` |
| **Foreign Key** | `{TablaReferenciada}Id` | `EstudianteId`, `BecaId` | `FK_Est`, `idEstudiante` |
| **Stored Procedures** | `{schema}.{Accion}{Entidad}` | `ewp.ObtenerNominacion` | `usp_`, `sp_`, `pr_` |
| **Vistas** | `{schema}.vw_{Descripcion}` | `ewp.vw_NominacionesActivas` | `Vista_`, `v_` |
| **Funciones Escalares** | `{schema}.fn_{Descripcion}` | `ewp.fn_CalcularTasa` | `func_`, `f_` |
| **Funciones Tabla** | `{schema}.fnt_{Descripcion}` | `ewp.fnt_ObtenerBecasPorAnio` | `fn_tabla_` |
| **Triggers** | `{schema}.tr_{Tabla}_{Evento}` | `academico.tr_Estudiante_AfterInsert` | `trigger_`, `trg_` |
| **Índices** | `IX_{Tabla}_{Columnas}` | `IX_Estudiante_Email` | `idx1`, `index_email` |
| **Índices Únicos** | `UX_{Tabla}_{Columnas}` | `UX_Estudiante_NumeroDocumento` | `unique_doc` |
| **Constraints PK** | `PK_{Tabla}` | `PK_Estudiante` | `PrimaryKey_1` |
| **Constraints FK** | `FK_{TablaHija}_{TablaPadre}` | `FK_Solicitud_Estudiante` | `FK_1`, `fk_sol_est` |
| **Constraints Check** | `CK_{Tabla}_{Columna}` | `CK_Estudiante_Edad` | `check_1` |
| **Constraints Default** | `DF_{Tabla}_{Columna}` | `DF_Estudiante_FechaRegistro` | `default_fecha` |
| **Schemas** | lowercase | `academico`, `financiero`, `ewp` | `Academico`, `FINANCIERO` |

### 1.2 Nomenclatura de Stored Procedures (SIN prefijo)

> ⚠️ **IMPORTANTE COMILLAS**: NO usamos prefijos `usp_`, `sp_`, `pr_` en procedimientos almacenados.
> Los procedimientos usan el formato: `{schema}.{Accion}{Entidad}`

```sql
-- =============================================
-- CONVENCIÓN COMILLAS: {schema}.{Accion}{Entidad}
-- ❌ NO usar prefijos usp_, sp_, pr_
-- =============================================

-- CRUD Básico
{schema}.Insertar{Entidad}        -- ewp.InsertarNominacion
{schema}.Actualizar{Entidad}      -- ewp.ActualizarNominacion  
{schema}.Eliminar{Entidad}        -- ewp.EliminarNominacion
{schema}.Desactivar{Entidad}      -- ewp.DesactivarNominacion (borrado lógico)

-- Consultas
{schema}.Obtener{Entidad}         -- ewp.ObtenerNominacion (por ID)
{schema}.Obtener{Entidades}       -- ewp.ObtenerNominaciones (listado)
{schema}.Buscar{Entidades}        -- ewp.BuscarNominaciones (con filtros)
{schema}.Existe{Entidad}          -- ewp.ExisteNominacion

-- Operaciones de Negocio  
{schema}.{Accion}{Entidad}        -- ewp.AprobarNominacion
{schema}.Procesar{Proceso}        -- ewp.ProcesarIntercambio

-- Reportes
{schema}.Rpt{NombreReporte}       -- ewp.RptNominacionesPorPais
```

### 1.3 Nomenclatura de Funciones (CON prefijo fn_)

> ✅ Las funciones SÍ llevan prefijo `fn_` o `fnt_`

```sql
-- Funciones Escalares (devuelven un valor)
{schema}.fn_{Descripcion}         -- ewp.fn_CalcularTasa
                                  -- academico.fn_CalcularEdad

-- Funciones con Valor de Tabla (TVF)
{schema}.fnt_{Descripcion}        -- ewp.fnt_ObtenerBecasPorAnio
```

### 1.4 Nomenclatura de Vistas (CON prefijo vw_)

> ✅ Las vistas SÍ llevan prefijo `vw_`

```sql
{schema}.vw_{Descripcion}         -- ewp.vw_NominacionesActivas
                                  -- academico.vw_EstudiantesActivos
```

### 1.5 Ejemplos Completos por Schema

```sql
-- =============================================
-- SCHEMA: ewp (Erasmus Without Paper)
-- =============================================
ewp.ObtenerNominacion             -- SP: Obtener nominación por ID
ewp.ObtenerNominaciones           -- SP: Listar nominaciones
ewp.InsertarNominacion            -- SP: Crear nueva nominación
ewp.ActualizarNominacion          -- SP: Actualizar nominación
ewp.AprobarNominacion             -- SP: Operación de negocio
ewp.fn_CalcularTasa               -- Función: Calcular tasa
ewp.fn_ObtenerEstadoIntercambio   -- Función: Obtener estado
ewp.fnt_ObtenerBecasPorAnio       -- TVF: Becas por año
ewp.vw_NominacionesActivas        -- Vista: Nominaciones activas

-- =============================================
-- SCHEMA: academico
-- =============================================
academico.ObtenerEstudiante       -- SP: Obtener estudiante
academico.InsertarMatricula       -- SP: Crear matrícula
academico.fn_CalcularEdad         -- Función: Calcular edad
academico.vw_EstudiantesActivos   -- Vista: Estudiantes activos

-- =============================================
-- SCHEMA: financiero
-- =============================================
financiero.ProcesarPago           -- SP: Procesar pago
financiero.fn_CalcularDescuento   -- Función: Calcular descuento
financiero.vw_PagosVencidos       -- Vista: Pagos vencidos
```

### 1.6 Tipos de Datos Recomendados

| Uso | Tipo Recomendado | ❌ Evitar | Notas |
|-----|------------------|-----------|-------|
| Identificadores | `INT IDENTITY` o `BIGINT` | `UNIQUEIDENTIFIER` como PK clustered | GUID como PK causa fragmentación |
| GUIDs | `UNIQUEIDENTIFIER` | - | Solo cuando se requiere unicidad global |
| Texto corto (<100) | `NVARCHAR(n)` | `VARCHAR`, `CHAR` | Soporte Unicode |
| Texto largo | `NVARCHAR(MAX)` | `TEXT` | TEXT está deprecado |
| Fechas | `DATETIME2(3)` | `DATETIME` | Mayor precisión, menor espacio |
| Solo fecha | `DATE` | `DATETIME` | Sin componente de hora |
| Solo hora | `TIME(0)` | `DATETIME` | Sin componente de fecha |
| Dinero | `DECIMAL(18,2)` | `MONEY`, `FLOAT` | Evita errores de redondeo |
| Booleanos | `BIT` | `INT`, `CHAR(1)` | Valores 0/1 |
| Porcentajes | `DECIMAL(5,2)` | `FLOAT` | Ej: 99.99% |
| JSON | `NVARCHAR(MAX)` con CHECK | - | SQL 2016+: `ISJSON()` |

---

## PARTE 2: PLANTILLA DE STORED PROCEDURES

### 2.1 Plantilla Estándar Completa

```sql
-- =============================================
-- Autor:           {NombreAutor}
-- Fecha Creación:  {FechaCreacion}
-- Descripción:     {DescripcionBreve}
-- =============================================
-- Historial de Cambios:
-- Fecha        | Autor          | Descripción
-- -------------|----------------|------------------------------------------
-- {Fecha}      | {Autor}        | Creación inicial
-- =============================================
CREATE OR ALTER PROCEDURE [{schema}].[{Accion}{Entidad}]
    -- Parámetros de entrada
    @Id INT,
    @Parametro1 NVARCHAR(100),
    @Parametro2 INT = NULL,              -- Parámetro opcional con default
    
    -- Parámetros de salida
    @RowsAffected INT = 0 OUTPUT,
    @ErrorMessage NVARCHAR(500) = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;  -- Rollback automático en errores
    
    -- Variables locales
    DECLARE @Result INT = 0;
    DECLARE @TransactionStarted BIT = 0;
    
    BEGIN TRY
        -- Iniciar transacción si no hay una activa
        IF @@TRANCOUNT = 0
        BEGIN
            BEGIN TRANSACTION;
            SET @TransactionStarted = 1;
        END
        
        -- =============================================
        -- VALIDACIONES
        -- =============================================
        IF @Id IS NULL
        BEGIN
            SET @ErrorMessage = 'El parámetro @Id es requerido';
            RAISERROR(@ErrorMessage, 16, 1);
            RETURN -1;
        END
        
        -- =============================================
        -- LÓGICA PRINCIPAL
        -- =============================================
        
        -- Tu código aquí...
        
        -- =============================================
        -- COMMIT Y RETORNO
        -- =============================================
        IF @TransactionStarted = 1
            COMMIT TRANSACTION;
            
        SET @RowsAffected = @@ROWCOUNT;
        RETURN 0;
        
    END TRY
    BEGIN CATCH
        -- Rollback si iniciamos la transacción
        IF @TransactionStarted = 1 AND @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Capturar información del error
        SET @ErrorMessage = ERROR_MESSAGE();
        
        -- Re-lanzar el error
        THROW;
    END CATCH
END
GO
```

### 2.2 Ejemplo: Stored Procedure de Consulta

```sql
-- =============================================
-- Autor:           STIC Comillas
-- Fecha Creación:  2026-01-24
-- Descripción:     Obtiene nominaciones con paginación y filtros
-- =============================================
CREATE OR ALTER PROCEDURE [ewp].[ObtenerNominaciones]
    @Estado NVARCHAR(50) = NULL,
    @FechaDesde DATE = NULL,
    @FechaHasta DATE = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Calcular total
    SELECT @TotalCount = COUNT(*)
    FROM ewp.Nominacion n
    WHERE (@Estado IS NULL OR n.Estado = @Estado)
      AND (@FechaDesde IS NULL OR n.FechaCreacion >= @FechaDesde)
      AND (@FechaHasta IS NULL OR n.FechaCreacion <= @FechaHasta);
    
    -- Obtener página
    SELECT 
        n.Id,
        n.EstudianteId,
        e.Nombre AS EstudianteNombre,
        n.Estado,
        n.FechaCreacion,
        n.FechaModificacion
    FROM ewp.Nominacion n
    INNER JOIN academico.Estudiante e ON n.EstudianteId = e.Id
    WHERE (@Estado IS NULL OR n.Estado = @Estado)
      AND (@FechaDesde IS NULL OR n.FechaCreacion >= @FechaDesde)
      AND (@FechaHasta IS NULL OR n.FechaCreacion <= @FechaHasta)
    ORDER BY n.FechaCreacion DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO
```

---

## PARTE 3: PLANTILLA DE FUNCIONES

### 3.1 Función Escalar

```sql
-- =============================================
-- Autor:           STIC Comillas
-- Fecha Creación:  2026-01-24
-- Descripción:     Calcula la tasa aplicable
-- =============================================
CREATE OR ALTER FUNCTION [ewp].[fn_CalcularTasa]
(
    @TipoIntercambio NVARCHAR(50),
    @Duracion INT
)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @Tasa DECIMAL(18,2);
    
    SELECT @Tasa = CASE 
        WHEN @TipoIntercambio = 'ERASMUS' AND @Duracion <= 3 THEN 250.00
        WHEN @TipoIntercambio = 'ERASMUS' AND @Duracion <= 6 THEN 200.00
        WHEN @TipoIntercambio = 'ERASMUS' THEN 150.00
        WHEN @TipoIntercambio = 'BILATERAL' THEN 300.00
        ELSE 0.00
    END;
    
    RETURN @Tasa;
END
GO
```

### 3.2 Función con Valor de Tabla (TVF)

```sql
-- =============================================
-- Autor:           STIC Comillas
-- Fecha Creación:  2026-01-24
-- Descripción:     Obtiene becas por año académico
-- =============================================
CREATE OR ALTER FUNCTION [ewp].[fnt_ObtenerBecasPorAnio]
(
    @AnioAcademico INT
)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        b.Id,
        b.EstudianteId,
        e.Nombre AS EstudianteNombre,
        b.Importe,
        b.Estado
    FROM ewp.Beca b
    INNER JOIN academico.Estudiante e ON b.EstudianteId = e.Id
    WHERE b.AnioAcademico = @AnioAcademico
);
GO
```

---

## PARTE 4: PLANTILLA DE VISTAS

```sql
-- =============================================
-- Autor:           STIC Comillas
-- Fecha Creación:  2026-01-24
-- Descripción:     Vista de nominaciones activas con información completa
-- =============================================
CREATE OR ALTER VIEW [ewp].[vw_NominacionesActivas]
AS
SELECT 
    n.Id,
    n.EstudianteId,
    e.Nombre AS EstudianteNombre,
    e.Email AS EstudianteEmail,
    n.UniversidadDestinoId,
    u.Nombre AS UniversidadDestino,
    n.Estado,
    n.FechaCreacion,
    n.FechaModificacion,
    ewp.fn_CalcularTasa(n.TipoIntercambio, n.DuracionMeses) AS TasaAplicable
FROM ewp.Nominacion n
INNER JOIN academico.Estudiante e ON n.EstudianteId = e.Id
INNER JOIN ewp.Universidad u ON n.UniversidadDestinoId = u.Id
WHERE n.Estado IN ('Pendiente', 'EnProceso', 'Aprobada');
GO
```

---

## PARTE 5: PALABRAS CLAVE T-SQL

### 5.1 Convención: MAYÚSCULAS

> ✅ Las palabras clave de T-SQL deben escribirse en **MAYÚSCULAS**

```sql
-- ✅ CORRECTO
SELECT Id, Nombre, FechaCreacion
FROM ewp.Nominacion
WHERE Estado = 'Activo'
ORDER BY FechaCreacion DESC;

-- ❌ INCORRECTO
select id, nombre, fechacreacion
from ewp.nominacion
where estado = 'Activo'
order by fechacreacion desc;
```

### 5.2 Lista de Palabras Clave Principales

```sql
-- DDL (Data Definition Language)
CREATE, ALTER, DROP, TRUNCATE, RENAME

-- DML (Data Manipulation Language)  
SELECT, INSERT, UPDATE, DELETE, MERGE

-- Cláusulas
FROM, WHERE, JOIN, INNER, LEFT, RIGHT, OUTER, FULL
ON, AND, OR, NOT, IN, EXISTS, BETWEEN, LIKE
GROUP BY, HAVING, ORDER BY, ASC, DESC
UNION, INTERSECT, EXCEPT

-- Funciones de agregación
COUNT, SUM, AVG, MIN, MAX, STRING_AGG

-- Control de flujo
IF, ELSE, BEGIN, END, WHILE, BREAK, CONTINUE
CASE, WHEN, THEN, ELSE, END
TRY, CATCH, THROW, RAISERROR

-- Transacciones
BEGIN TRANSACTION, COMMIT, ROLLBACK, SAVE TRANSACTION

-- Otros
DECLARE, SET, PRINT, RETURN
NULL, IS NULL, IS NOT NULL, COALESCE, ISNULL
TOP, OFFSET, FETCH, NEXT, ROWS, ONLY
WITH, AS, OVER, PARTITION BY, ROW_NUMBER
```

---

## PARTE 6: BUENAS PRÁCTICAS

### 6.1 Rendimiento

```sql
-- ✅ Usar EXISTS en lugar de IN para subconsultas
SELECT * FROM ewp.Nominacion n
WHERE EXISTS (SELECT 1 FROM academico.Estudiante e WHERE e.Id = n.EstudianteId AND e.Activo = 1);

-- ✅ Evitar SELECT *
SELECT Id, Nombre, Estado FROM ewp.Nominacion;

-- ✅ Usar índices apropiados
CREATE NONCLUSTERED INDEX IX_Nominacion_Estado 
ON ewp.Nominacion(Estado) INCLUDE (EstudianteId, FechaCreacion);
```

### 6.2 Seguridad

```sql
-- ✅ Usar parámetros, nunca concatenar strings
EXEC ewp.ObtenerNominacion @Id = @InputId;

-- ❌ NUNCA hacer esto (SQL Injection)
EXEC('SELECT * FROM Nominacion WHERE Id = ' + @InputId);
```

### 6.3 Manejo de NULL

```sql
-- ✅ Usar COALESCE o ISNULL
SELECT COALESCE(n.Observaciones, 'Sin observaciones') AS Observaciones
FROM ewp.Nominacion n;

-- ✅ Comparar NULL correctamente
WHERE Campo IS NULL
WHERE Campo IS NOT NULL
```
