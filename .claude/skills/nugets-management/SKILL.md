---
name: nugets-management
description: >
  Manages NuGet packages for .NET projects: Central Package Management (CPM),
  version strategy, vulnerability auditing, package consolidation, and
  migration to Directory.Packages.props.
  USE FOR: managing NuGet packages, CPM migration, version strategy,
  vulnerability scanning, package updates, gestionar paquetes,
  actualizar NuGets, migrar a CPM.
  DO NOT USE FOR: code security auditing (use security-audit),
  architecture decisions (use analisis-arquitectura),
  build errors (use build-fixer agent).
allowed-tools: Read, Grep, Glob, Bash(dotnet *)
---

# Gestión de NuGets - Comillas

Este skill ayuda a gestionar paquetes NuGet siguiendo estándares STIC.

---

## Activación del Skill

### Por Comando (explícita)

| Comando | Acción |
|---------|--------|
| `/nugets` | Análisis general de dependencias |
| `/nugets analizar` | Escaneo completo de paquetes del proyecto |
| `/nugets audit` | Auditoría de seguridad (CVEs) |
| `/nugets actualizar` | Propuestas de actualización con compatibilidad |
| `/nugets estandarizar` | Migración a Central Package Management |
| `/nugets documentar` | Generar documentación en `_duran/DEPENDENCIAS.md` |

### Por Contexto (automática)

Claude activa este skill automáticamente cuando detecta:
- Mención de "NuGet", "paquetes", "dependencias"
- Preguntas sobre vulnerabilidades o CVEs
- Trabajo con archivos `.csproj` o `Directory.Packages.props`
- Solicitudes de actualización de librerías

---

## Cuándo Usar Este Skill

1. **Auditoría de seguridad** - Detectar vulnerabilidades en dependencias
2. **Actualización** - Identificar paquetes desactualizados
3. **Consolidación** - Unificar versiones entre proyectos
4. **Análisis** - Entender el stack tecnológico del proyecto

---

## Comandos de Análisis

### Listar paquetes con vulnerabilidades

```bash
# Vulnerabilidades en el proyecto actual
dotnet list package --vulnerable

# Incluir transitivas (dependencias de dependencias)
dotnet list package --vulnerable --include-transitive

# En toda la solución
dotnet list *.sln package --vulnerable --include-transitive
```

### Listar paquetes desactualizados

```bash
# Paquetes con versiones nuevas disponibles
dotnet list package --outdated

# Incluir prereleases
dotnet list package --outdated --include-prerelease

# En formato JSON para procesamiento
dotnet list package --outdated --format json
```

### Listar todos los paquetes

```bash
# Todos los paquetes del proyecto
dotnet list package

# Incluir transitivas
dotnet list package --include-transitive

# Solo top-level (directas)
dotnet list package --top-level-only
```

---

## Interpretación de CVEs

### Niveles de Severidad

| Severidad | CVSS Score | Acción Requerida | Plazo |
|-----------|------------|------------------|-------|
| **Critical** | 9.0 - 10.0 | INMEDIATA | < 24 horas |
| **High** | 7.0 - 8.9 | URGENTE | < 1 semana |
| **Medium** | 4.0 - 6.9 | PLANIFICAR | < 1 mes |
| **Low** | 0.1 - 3.9 | EVALUAR | Próximo sprint |

### Formato de Reporte

```markdown
## ⚠️ Vulnerabilidades Detectadas

### CRÍTICAS (Acción inmediata)

| Paquete | Versión | CVE | Severidad | Versión Segura |
|---------|---------|-----|-----------|----------------|
| Newtonsoft.Json | 12.0.1 | CVE-2024-XXXX | Critical | 13.0.3+ |

### ALTAS (Urgente)

| Paquete | Versión | CVE | Severidad | Versión Segura |
|---------|---------|-----|-----------|----------------|
| System.Text.Json | 6.0.0 | CVE-2024-YYYY | High | 8.0.4+ |

### Recomendaciones
1. Actualizar inmediatamente paquetes críticos
2. Planificar actualización de paquetes altos
3. Verificar compatibilidad antes de actualizar
```

---

## Central Package Management

### Configuración Directory.Packages.props

```xml
<Project>
  <PropertyGroup>
    <!-- Habilitar Central Package Management -->
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <!-- ═══════════════════════════════════════════════════════════ -->
    <!-- MICROSOFT / FRAMEWORK -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.0" />

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!-- AZURE -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <PackageVersion Include="Azure.Identity" Version="1.13.0" />
    <PackageVersion Include="Azure.Extensions.AspNetCore.Configuration.Secrets" Version="1.3.2" />
    <PackageVersion Include="Azure.Storage.Blobs" Version="12.22.0" />
    <PackageVersion Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.22.0" />

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!-- VALIDACIÓN Y MAPPING -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <PackageVersion Include="FluentValidation" Version="11.10.0" />
    <PackageVersion Include="FluentValidation.AspNetCore" Version="11.3.0" />
    <PackageVersion Include="AutoMapper" Version="13.0.1" />

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!-- LOGGING -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <PackageVersion Include="Serilog" Version="4.1.0" />
    <PackageVersion Include="Serilog.AspNetCore" Version="8.0.3" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.File" Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.ApplicationInsights" Version="4.0.0" />

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!-- CQRS / MEDIATOR -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <PackageVersion Include="MediatR" Version="12.4.1" />

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!-- TESTING -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="FluentAssertions" Version="6.12.1" />
    <PackageVersion Include="Moq" Version="4.20.72" />
    <PackageVersion Include="Testcontainers.MsSql" Version="3.10.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
    <PackageVersion Include="coverlet.collector" Version="6.0.2" />
  </ItemGroup>
</Project>
```

### Uso en .csproj (sin versión)

```xml
<!-- Proyecto.csproj -->
<ItemGroup>
  <!-- La versión se toma de Directory.Packages.props -->
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
  <PackageReference Include="FluentValidation" />
  <PackageReference Include="Serilog.AspNetCore" />
</ItemGroup>
```

### Migración a Central Package Management

```bash
# 1. Crear Directory.Packages.props en la raíz de la solución
# 2. Ejecutar el siguiente script PowerShell para extraer versiones:

# migrate-to-cpm.ps1
$projects = Get-ChildItem -Recurse -Filter "*.csproj"
$packages = @{}

foreach ($project in $projects) {
    [xml]$csproj = Get-Content $project.FullName
    $refs = $csproj.SelectNodes("//PackageReference")
    foreach ($ref in $refs) {
        $name = $ref.GetAttribute("Include")
        $version = $ref.GetAttribute("Version")
        if ($version -and !$packages.ContainsKey($name)) {
            $packages[$name] = $version
        }
    }
}

# Generar XML
$packages.GetEnumerator() | Sort-Object Name | ForEach-Object {
    Write-Output "<PackageVersion Include=`"$($_.Key)`" Version=`"$($_.Value)`" />"
}
```

---

## Categorización de Paquetes

### Por Función

| Categoría | Paquetes Típicos | Capa |
|-----------|------------------|------|
| **ORM** | EntityFrameworkCore, Dapper | Infrastructure |
| **Logging** | Serilog, NLog | Transversal |
| **Validación** | FluentValidation | Application |
| **Mapping** | AutoMapper, Mapster | Application |
| **Testing** | xUnit, NUnit, Moq | Tests |
| **Azure** | Azure.*, Microsoft.Azure.* | Infrastructure |
| **Serialización** | Newtonsoft.Json, System.Text.Json | Transversal |
| **HTTP** | Refit, Polly | Infrastructure |
| **Caché** | StackExchange.Redis | Infrastructure |

### Paquetes Recomendados Comillas

```markdown
## Stack Recomendado .NET 10

### Obligatorios
- Microsoft.EntityFrameworkCore.SqlServer (ORM)
- FluentValidation (Validación)
- Serilog.AspNetCore (Logging)
- Azure.Identity (Autenticación Azure)

### Recomendados
- MediatR (CQRS)
- AutoMapper (Mapping)
- Polly (Resiliencia)
- xUnit + FluentAssertions + Moq (Testing)

### Evitar
- Newtonsoft.Json (usar System.Text.Json)
- Swashbuckle (usar OpenAPI nativo .NET 10)
- Autofac (usar DI nativo)
```

---

## Compatibilidad TargetFramework

### Verificación de Compatibilidad

```bash
# Ver TargetFramework del proyecto
grep -r "<TargetFramework>" *.csproj

# Verificar compatibilidad de un paquete (NuGet.org)
# Buscar en la pestaña "Dependencies" del paquete
```

### Matriz de Compatibilidad

| Paquete | net48 | net8.0 | net9.0 | net10.0 |
|---------|-------|--------|--------|---------|
| EF Core 10.x | ❌ | ❌ | ❌ | ✅ |
| EF Core 9.x | ❌ | ✅ | ✅ | ✅ |
| EF Core 8.x | ❌ | ✅ | ✅ | ✅ |
| EF 6.x | ✅ | ❌ | ❌ | ❌ |
| FluentValidation 11.x | ✅ | ✅ | ✅ | ✅ |
| Serilog 4.x | ❌ | ✅ | ✅ | ✅ |
| xUnit 2.x | ✅ | ✅ | ✅ | ✅ |

### Reglas por Framework

```markdown
## .NET 10 (Nuevos proyectos)
- Usar versiones más recientes de paquetes
- OpenAPI nativo (no Swashbuckle)
- EF Core 10.x

## .NET 8/9 (Migración planificada)
- Mantener versiones estables
- Preparar migración a .NET 10

## .NET Framework 4.x (Legacy)
- No actualizar a versiones incompatibles
- Entity Framework 6.x (no EF Core)
- Newtonsoft.Json (no System.Text.Json avanzado)
```

---

## Comandos de Actualización

### Actualizar paquete específico

```bash
# Actualizar a última versión estable
dotnet add package PackageName

# Actualizar a versión específica
dotnet add package PackageName --version 1.2.3

# Actualizar solo si hay versión segura
dotnet add package PackageName --prerelease
```

### Actualizar todos los paquetes

```bash
# Script PowerShell para actualizar todos
$projects = Get-ChildItem -Recurse -Filter "*.csproj"
foreach ($project in $projects) {
    dotnet list $project.FullName package --outdated --format json |
    ConvertFrom-Json |
    ForEach-Object {
        $_.projects.frameworks.topLevelPackages |
        ForEach-Object {
            dotnet add $project.FullName package $_.id
        }
    }
}
```

---

## Checklist de Gestión NuGet

### Auditoría Inicial
- [ ] Ejecutar `dotnet list package --vulnerable`
- [ ] Identificar CVEs críticos y altos
- [ ] Documentar paquetes a actualizar
- [ ] Verificar compatibilidad con TargetFramework

### Actualización
- [ ] Actualizar paquetes críticos primero
- [ ] Ejecutar tests después de cada actualización
- [ ] Verificar que la aplicación compila
- [ ] Probar funcionalidad afectada

### Consolidación
- [ ] Implementar Central Package Management
- [ ] Unificar versiones entre proyectos
- [ ] Eliminar paquetes no utilizados
- [ ] Documentar decisiones en DEPENDENCIAS.md

---

*Skill nugets-management v3.7.0*
