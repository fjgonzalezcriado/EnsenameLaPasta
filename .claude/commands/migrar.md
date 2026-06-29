Asiste en la migración de proyectos entre versiones de .NET

# /migrar - Migración entre versiones de .NET

Asiste en la migración de proyectos entre versiones de .NET.

## 🧠 Extended Thinking Mode

**think hard - Analiza cuidadosamente el impacto de la migración en todo el proyecto.**

---

## IMPORTANTE: Ubicación del Código

El código a migrar está en `03_Desarrollo/`. Los cambios de migración se aplican ahí.

```
MiProyecto/
├── _duran/
│   ├── ESTADO_PROYECTO.json   ← Registrar progreso migración
│   └── DECISIONES.md          ← ADRs de migración
├── 03_Desarrollo/             ← CÓDIGO A MIGRAR ⭐
│   ├── MiSolucion.sln
│   ├── MiProyecto.API/
│   │   └── MiProyecto.API.csproj  ← Modificar TargetFramework
│   ├── MiProyecto.Domain/
│   └── ...
└── 06_Documentacion/          ← Guías de migración
```

---

## Uso

```
/migrar [versión-destino]
```

Ejemplos:
```bash
/migrar net10
/migrar net9
/migrar --analyze  # Solo analizar, no migrar
```

---

## Proceso

### 1. Análisis de Versión Actual

Detectar versión actual del proyecto en `03_Desarrollo/`:

```powershell
# Buscar TargetFramework en archivos .csproj
Get-ChildItem -Path "03_Desarrollo" -Recurse -Filter "*.csproj" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    if ($content -match '<TargetFramework>([^<]+)</TargetFramework>') {
        Write-Host "$($_.Name): $($Matches[1])"
    }
}
```

- Buscar `<TargetFramework>` en archivos .csproj
- Identificar dependencias NuGet
- Detectar patrones de código específicos de versión

### 2. Generar Informe de Migración

```markdown
## Informe de Migración

### Proyecto
- Nombre: [detectado]
- Ubicación: 03_Desarrollo/
- Versión actual: .NET [X]
- Versión destino: .NET [Y]

### Proyectos a Migrar (en 03_Desarrollo/)
| Proyecto | Ruta | Versión Actual | Target |
|----------|------|----------------|--------|
| MiProyecto.API | 03_Desarrollo/MiProyecto.API/ | net8.0 | net10.0 |
| MiProyecto.Domain | 03_Desarrollo/MiProyecto.Domain/ | net8.0 | net10.0 |
| MiProyecto.Tests | 03_Desarrollo/MiProyecto.Tests/ | net8.0 | net10.0 |

### Dependencias
| Paquete | Versión actual | Compatible | Acción |
|---------|----------------|------------|--------|
| EntityFramework | 6.x | ❌ | Migrar a EF Core 10 |
| Newtonsoft.Json | 13.x | ✅ | Mantener |
| ... | ... | ... | ... |

### Cambios de Código Necesarios
1. [ ] Program.cs - Nuevo modelo de hosting
2. [ ] Startup.cs → Program.cs
3. [ ] Web.config → appsettings.json
4. [ ] Global.asax → Eliminar

### APIs Obsoletas Detectadas
- HttpContext.Current → IHttpContextAccessor
- ConfigurationManager → IConfiguration
- ...

### Estimación de Esfuerzo
- Complejidad: [Alta/Media/Baja]
- Tiempo estimado: [X días/semanas]
- Riesgo: [Alto/Medio/Bajo]
```

Guardar informe en `06_Documentacion/migracion/INFORME_MIGRACION_[VERSION].md`

### 3. Plan de Migración por Capas

```
ORDEN RECOMENDADO (en 03_Desarrollo/):

1. Domain → 03_Desarrollo/MiProyecto.Domain/
   Menor dependencia de infraestructura

2. Application → 03_Desarrollo/MiProyecto.Application/
   Depende solo de Domain

3. Infrastructure → 03_Desarrollo/MiProyecto.Infrastructure/
   EF, repositorios

4. Web/API → 03_Desarrollo/MiProyecto.API/
   Último, más cambios de hosting

5. Tests → 03_Desarrollo/MiProyecto.Tests/
   Después de migrar el código principal

Cada capa = un evolutivo separado (/nuevo-evolutivo MIG-00X)
```

### 4. Checklist de Migración

```markdown
## Pre-migración
- [ ] Backup del proyecto (rama o copia)
- [ ] Tests existentes pasan (`/test`)
- [ ] Documentar versiones actuales en `_duran/DEPENDENCIAS.md`
- [ ] Crear evolutivo: `/nuevo-evolutivo "MIG-001: Migración a .NET 10"`

## Durante
- [ ] Crear rama feature/migracion-netX
- [ ] Migrar capa por capa (en 03_Desarrollo/)
- [ ] Tests después de cada capa
- [ ] Documentar decisiones en `_duran/DECISIONES.md`

## Post-migración
- [ ] Todos los tests pasan
- [ ] Validación funcional
- [ ] Actualizar `_duran/DEPENDENCIAS.md`
- [ ] PR para revisión
- [ ] `/finalizar-evolutivo MIG-001`
```

### 5. Ejecutar Migración

**Modificar .csproj en 03_Desarrollo/:**

```powershell
# Actualizar TargetFramework en todos los .csproj
Get-ChildItem -Path "03_Desarrollo" -Recurse -Filter "*.csproj" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $newContent = $content -replace '<TargetFramework>net8\.0</TargetFramework>', '<TargetFramework>net10.0</TargetFramework>'
    Set-Content -Path $_.FullName -Value $newContent
    Write-Host "Actualizado: $($_.FullName)"
}
```

**Actualizar paquetes NuGet:**

```powershell
Push-Location "03_Desarrollo"
dotnet restore
dotnet list package --outdated
Pop-Location
```

### 6. Conversión de Formato de Solución (.sln → .slnx)

> **IMPORTANTE: Solo para .NET 8+ (Visual Studio 2022 17.10+)**

```
┌────────────────────────────────────────────────────────────┐
│ MATRIZ DE COMPATIBILIDAD - FORMATO DE SOLUCIÓN                  │
├────────────────────────────────────────────────────────────┤
│ .NET 4.x  │ .NET 7  │ .NET 8  │ .NET 9  │ .NET 10 │ Formato    │
├───────────┼─────────┼─────────┼─────────┼─────────┼────────────┤
│    ✅     │   ✅    │   ✅    │   ✅    │   ✅    │ .sln       │
│    ❌     │   ❌    │   ✅    │   ✅    │   ✅    │ .slnx      │
└────────────────────────────────────────────────────────────┘
```

**Detectar versión y ofrecer conversión:**

```powershell
# Buscar TargetFramework en archivos .csproj
$csprojFiles = Get-ChildItem -Path "03_Desarrollo" -Recurse -Filter "*.csproj"
$targetFramework = $null

foreach ($csproj in $csprojFiles) {
    $content = Get-Content $csproj.FullName -Raw
    if ($content -match '<TargetFramework>(net[0-9]+\.0)</TargetFramework>') {
        $targetFramework = $Matches[1]
        break
    }
}

# Determinar si puede usar .slnx
$puedeUsarSlnx = $targetFramework -match 'net(8|9|10|1[1-9]|[2-9][0-9])\.0'

if ($puedeUsarSlnx) {
    Write-Host "✅ El proyecto usa $targetFramework - compatible con .slnx"
    Write-Host ""
    Write-Host "¿Desea convertir el archivo .sln a .slnx?"
    Write-Host "[S] Sí - Convertir a .slnx (formato XML limpio)"
    Write-Host "[N] No - Mantener .sln (compatibilidad máxima)"
}
```

**Ventajas de .slnx:**
- Formato XML legible y editable manualmente
- Sin GUIDs complejos ni secciones difíciles de entender
- Mejor integración con control de versiones (menos conflictos)
- Estructura declarativa más limpia

**Proceso de conversión:**

```powershell
# Convertir .sln a .slnx (requiere VS 2022 17.10+)
$slnPath = Get-ChildItem -Path "03_Desarrollo" -Filter "*.sln" | Select-Object -First 1

if ($slnPath -and $puedeUsarSlnx) {
    # Abrir en VS y guardar como .slnx
    Write-Host "📋 Instrucciones para convertir a .slnx:"
    Write-Host "1. Abrir $($slnPath.FullName) en Visual Studio 2022 17.10+"
    Write-Host "2. Archivo → Guardar como → Seleccionar formato 'Solution (*.slnx)'"
    Write-Host "3. Verificar que todos los proyectos cargan correctamente"
    Write-Host "4. Eliminar el archivo .sln original (opcional)"
}
```

**ADR para documentar la decisión:**

```markdown
### [ADR-XXX] Migración de formato de solución .sln a .slnx
- **Fecha**: [FECHA]
- **Estado**: Implementada
- **Contexto**: Proyecto migrado a .NET [X], compatible con .slnx
- **Decisión**: Convertir a formato .slnx para mejor legibilidad y menos conflictos en VCS
- **Consecuencias**:
  - Requiere VS 2022 17.10+ para abrir
  - Mejor experiencia con control de versiones
  - No compatible con .NET 7 o anterior
```

---

### 7. Registrar en Contexto

#### .NET 4.x → .NET 10

**Cambios principales:**
- `Web.config` → `appsettings.json`
- `Global.asax` → `Program.cs`
- `packages.config` → `PackageReference` en .csproj
- Entity Framework 6 → EF Core 10
- `HttpContext.Current` → `IHttpContextAccessor`

**Código típico a cambiar (en 03_Desarrollo/):**
```csharp
// ANTES (.NET 4.x) - 03_Desarrollo/MiProyecto.Web/
var setting = ConfigurationManager.AppSettings["MiSetting"];

// DESPUÉS (.NET 10) - 03_Desarrollo/MiProyecto.API/
var setting = _configuration["MiSetting"];
```

#### .NET 8/9 → .NET 10

**Cambios principales:**
- `AddSwaggerGen()` → `AddOpenApi()` (opcional pero recomendado)
- `Host.CreateDefaultBuilder` → `Host.CreateApplicationBuilder`
- Nuevas APIs de C# 13
- **Considerar conversión a .slnx (formato de solución XML limpio)**

```csharp
// ANTES (.NET 8) - 03_Desarrollo/MiProyecto.API/Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSwaggerGen();

// DESPUÉS (.NET 10) - 03_Desarrollo/MiProyecto.API/Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
```

### 7. Registrar en Contexto

**Actualizar `_duran/ESTADO_PROYECTO.json`:**
```json
{
  "proyecto": {
    "dotnetVersion": "10.0",
    "migracionDesde": "8.0",
    "fechaMigracion": "[FECHA]"
  }
}
```

**Añadir ADR en `_duran/DECISIONES.md`:**
```markdown
### [ADR-XXX] Migración a .NET 10
- **Fecha**: [FECHA]
- **Estado**: Implementada
- **Contexto**: .NET 9 finaliza soporte en Mayo 2026
- **Decisión**: Migrar todos los proyectos en 03_Desarrollo/ a .NET 10
- **Consecuencias**:
  - Mejor rendimiento
  - Soporte hasta 2028
  - Requiere actualizar algunos paquetes NuGet
```

---

## Notas

- La migración debe hacerse en rama separada
- No mezclar migración con cambios funcionales
- Priorizar .NET 9 → .NET 10 (fin de soporte Mayo 2026)
- Documentar todo en `_duran/DECISIONES.md`
- El código está en `03_Desarrollo/`, no en la raíz

---

## Relacionados

- /analizar - Análisis previo del proyecto
- /nuevo-evolutivo - Crear evolutivo de migración
- /test - Ejecutar tests de regresión
- /commit - Guardar cambios de migración
