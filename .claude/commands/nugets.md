Gestión avanzada de paquetes NuGet (analizar, auditar, actualizar, consolidar)

# Gestión de Paquetes NuGet

> 🔒 **SEGURIDAD: Este comando puede detectar vulnerabilidades CVE en dependencias.**
> Ejecutar `--audit` regularmente como parte del ciclo de desarrollo.

---

## Skills Asociados

Claude activará automáticamente estos skills cuando:

| Skill | Descripción | CVEs, Central Package Management, categorización |
|-------|------------------|---------------|
| **nugets-management** | `--audit`, `--consolidate`, análisis profundo | CVEs, Central Package Management, categorización |
| **security-audit** | Vulnerabilidades críticas detectadas | OWASP, detección de secrets, hardening |

Claude activará automáticamente estos skills cuando:
- Se detecten vulnerabilidades que requieran análisis de impacto
- Se solicite implementar Central Package Management
- Se necesite verificar compatibilidad con TargetFramework
- Se encuentren CVEs críticos (se invoca security-audit adicional)

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `--list` | Lista todos los paquetes instalados con versiones |
| `--audit` | 🔒 **Auditoría de seguridad - Detecta CVEs y vulnerabilidades** |
| `--outdated` | Muestra paquetes con actualizaciones disponibles |
| `--update [paquete]` | Actualiza paquete(s) a última versión compatible |
| `--consolidate` | Detecta y corrige versiones inconsistentes entre proyectos |
| (sin parámetro) | Muestra resumen general + paquetes desactualizados críticos |

---

## IMPORTANTE: Ubicación del Código

```
MiProyecto/
├── 03_Desarrollo/              ← ⭐ BUSCAR .csproj AQUÍ
│   ├── MiSolucion.sln|.slnx    ← .slnx solo .NET 8+
│   ├── MiApi/MiApi.csproj
│   ├── MiDomain/MiDomain.csproj
│   └── ...
└── ...
```

---

## Ejecutar:

### Sin parámetro (ANÁLISIS DE NUGETS)

```
▶ ANÁLISIS DE NUGETS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📦 RESUMEN
┌─────────────────────────────────────────────────┐
│  Total paquetes únicos:     45                  │
│  Proyectos analizados:      8                   │
│  Paquetes desactualizados:  12                  │
│  🔴 Vulnerabilidades:       2                   │
└─────────────────────────────────────────────────┘

⚠️ REQUIEREN ATENCIÓN INMEDIATA

🔴 VULNERABILIDADES DETECTADAS
┌─────────────────────────────────────────────────┐
│ Newtonsoft.Json 12.0.3                          │
│ CVE-2024-21907 (CRÍTICAS)                        │
│ → Actualizar a >= 13.0.1                        │
├─────────────────────────────────────────────────┤
│ System.Text.Json 6.0.0                          │
│ CVE-2024-30105 (ALTAS)                           │
│ → Actualizar a >= 6.0.10                        │
└─────────────────────────────────────────────────┘

📊 TOP 5 DESACTUALIZADOS (por antigüedad)

| Paquete | Actual | Última | Proyectos |
|---------|--------|--------|-----------|
| AutoMapper | 10.1.1 | 13.0.1 | 5 |
| FluentValidation | 9.5.0 | 11.9.0 | 4 |
| Serilog | 2.10.0 | 3.1.1 | 6 |
| xunit | 2.4.1 | 2.7.0 | 3 |
| Moq | 4.16.1 | 4.20.70 | 3 |

💡 Ejecutar:
   /nugets --audit      → Ver todas las vulnerabilidades
   /nugets --outdated   → Ver todos los desactualizados
   /nugets --update     → Actualizar todo (interactivo)
```

---

### `--list` - LISTADO DE PAQUETES NUGET

```powershell
# Ejecutar en 03_Desarrollo/
Get-ChildItem -Path "03_Desarrollo" -Filter "*.csproj" -Recurse | ForEach-Object {
    Write-Host "`n📁 $($_.Name)" -ForegroundColor Cyan
    [xml]$proj = Get-Content $_.FullName
    $proj.Project.ItemGroup.PackageReference | ForEach-Object {
        Write-Host "  📦 $($_.Include) v$($_.Version)"
    }
}
```

**Output esperado:**

```
▶ LISTADO DE PAQUETES NUGET
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📁 MiApi.csproj
├── Microsoft.AspNetCore.OpenApi          9.0.0
├── Swashbuckle.AspNetCore                6.5.0
├── Serilog.AspNetCore                    8.0.0
├── FluentValidation.AspNetCore           11.3.0
└── AutoMapper.Extensions.Microsoft.DI    12.0.1

📁 MiDomain.csproj
├── FluentValidation                      11.9.0
├── Mediator.Abstractions                 2.1.7   ← MIT (gratuito)
└── Mediator.SourceGenerator              2.1.7   ← Source generators

📁 MiInfrastructure.csproj
├── Microsoft.EntityFrameworkCore         8.0.0
├── Microsoft.EntityFrameworkCore.SqlServer  8.0.0
├── Dapper                                2.1.35
└── StackExchange.Redis                   2.7.33

📁 MiApi.Tests.csproj
├── xunit                                 2.7.0
├── xunit.runner.visualstudio             2.5.7
├── Moq                                   4.20.70
├── FluentAssertions                      6.12.0
└── Microsoft.NET.Test.Sdk                17.9.0

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📊 Total: {0} paquetes únicos en {1} proyectos
```

---

### `--audit` - AUDITORÍA DE SEGURIDAD NUGET 🔒

```powershell
# Ejecutar dotnet list package --vulnerable
cd 03_Desarrollo
dotnet list package --vulnerable --include-transitive
```

**Output esperado:**

```
▶ AUDITORÍA DE SEGURIDAD NUGET
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🔒 ANÁLISIS DE VULNERABILIDADES (CVE)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🔴 CRÍTICAS (2)
┌─────────────────────────────────────────────────────────────────┐
│ 📦 Newtonsoft.Json 12.0.3                                       │
│ ─────────────────────────────────────────────────────────────── │
│ CVE-2024-21907                                                  │
│ Severidad: CRÍTICAS (9.8)                                        │
│ Tipo: Remote Code Execution                                     │
│ Descripción: Unsafe deserialization allows RCE                  │
│ Afecta: MiApi.csproj, MiInfrastructure.csproj                   │
│ ─────────────────────────────────────────────────────────────── │
│ ✅ SOLUCIÓN: Actualizar a >= 13.0.1                             │
│    dotnet add package Newtonsoft.Json --version 13.0.3          │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 📦 System.Text.Json 6.0.0 (transitiva)                          │
│ ─────────────────────────────────────────────────────────────── │
│ CVE-2024-30105                                                  │
│ Severidad: ALTAS (7.5)                                           │
│ Tipo: Denial of Service                                         │
│ Descripción: Stack overflow via deeply nested JSON              │
│ Afecta: Vía Microsoft.AspNetCore.* 6.0.x                        │
│ ─────────────────────────────────────────────────────────────── │
│ ✅ SOLUCIÓN: Migrar a .NET 8+ o añadir explícitamente           │
│    dotnet add package System.Text.Json --version 8.0.3          │
└─────────────────────────────────────────────────────────────────┘

🟠 ALTAS (1)
┌─────────────────────────────────────────────────────────────────┐
│ 📦 Microsoft.Data.SqlClient 4.1.0                               │
│ CVE-2024-0056 | Severidad: ALTAS (7.1)                           │
│ ✅ Actualizar a >= 5.1.5                                        │
└─────────────────────────────────────────────────────────────────┘

🟡 MEDIAS (0)
✅ No se encontraron vulnerabilidades de severidad MEDIAS

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 RESUMEN DE AUDITORÍA
┌─────────────────────────────────────────────────┐
│  🔴 Críticas:    2                              │
│  🟠 Altas:       1                              │
│  🟡 Medias:      0                              │
│  🟢 Bajas:       0                              │
│  ─────────────────────────────────────────────  │
│  ⚠️  TOTAL:      3 vulnerabilidades             │
└─────────────────────────────────────────────────┘

🎯 ACCIONES REQUERIDAS

1. 🔴 [URGENTE] Actualizar Newtonsoft.Json
   cd 03_Desarrollo
   dotnet add MiApi/MiApi.csproj package Newtonsoft.Json -v 13.0.3
   dotnet add MiInfrastructure/MiInfrastructure.csproj package Newtonsoft.Json -v 13.0.3

2. 🔴 [URGENTE] Actualizar System.Text.Json
   dotnet add package System.Text.Json -v 8.0.3

3. 🟠 [IMPORTANTE] Actualizar SqlClient
   dotnet add package Microsoft.Data.SqlClient -v 5.1.5

💡 Para actualizar todo automáticamente:
   /nugets --update
```

---

### `--outdated` - PAQUETES DESACTUALIZADOS

```powershell
cd 03_Desarrollo
dotnet list package --outdated
```

**Output esperado:**

```
▶ PAQUETES DESACTUALIZADOS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 RESUMEN POR PROYECTO

📁 MiApi.csproj ({0} desactualizados)
┌────────────────────────────────┬──────────┬──────────┬────────────┐
│ Paquete                        │ Actual   │ Última   │ Tipo       │
├────────────────────────────────┼──────────┼──────────┼────────────┤
│ Swashbuckle.AspNetCore         │ 6.5.0    │ 6.7.3    │ 🟢 Minor   │
│ Serilog.AspNetCore             │ 7.0.0    │ 8.0.1    │ 🟡 Major   │
│ AutoMapper.Extensions.MS.DI    │ 11.0.0   │ 12.0.1   │ 🟡 Major   │
└────────────────────────────────┴──────────┴──────────┴────────────┘

📁 MiDomain.csproj ({0} desactualizados)
┌────────────────────────────────┬──────────┬──────────┬────────────┐
│ MediatR                        │ 11.1.0   │ 12.2.0   │ 🟡 Major   │
│ ⚠️ v12+ es COMERCIAL ($500-2000/año)                      │
│ 💡 Alternativa: Mediator (MIT/gratuito) - Ver GUIA_ARQUITECTURA  │
└────────────────────────────────┴──────────┴──────────┴────────────┘

📁 MiInfrastructure.csproj ({0} desactualizados)
┌────────────────────────────────┬──────────┬──────────┬────────────┐
│ Microsoft.EntityFrameworkCore  │ 7.0.14   │ 8.0.6    │ 🟡 Major   │
│ Dapper                         │ 2.0.123  │ 2.1.35   │ 🟢 Minor   │
└────────────────────────────────┴──────────┴──────────┴────────────┘

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📈 ESTADÍSTICAS
┌─────────────────────────────────────────────────┐
│  Total desactualizados:  6                      │
│  🟡 Major (breaking):    4                      │
│  🟢 Minor (compatible):  2                      │
│  🔵 Patch (seguro):      0                      │
└─────────────────────────────────────────────────┘

⚠️ RECOMENDACIONES

🟡 ACTUALIZACIONES MAJOR (revisar breaking changes):
   • Serilog 7.x → 8.x: Cambios en configuración
   • EF Core 7.x → 8.x: Requiere .NET 8
   • MediatR 11.x → 12.x: ⚠️ LICENCIA COMERCIAL ($500-2000/año)
     → Alternativa: Mediator (martinothamar, MIT/gratuito)
     → Ver: Documentos_Base/01_Estructura_Tecnica/GUIA_ARQUITECTURA.md

🟢 ACTUALIZACIONES MINOR (seguras):
   • Swashbuckle, Dapper: Actualizar directamente

💡 Comandos:
   /nugets --update Swashbuckle.AspNetCore   → Actualiza uno
   /nugets --update                          → Actualiza todos (interactivo)
```

---

### `--update [paquete]` - ACTUALIZACIÓN DE PAQUETES

```powershell
# Actualizar específico
dotnet add package [NombrePaquete] --version [version]

# Actualizar todos los de un proyecto
dotnet outdated --upgrade
```

**Output esperado:**

```
▶ ACTUALIZACIÓN DE PAQUETES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📦 Actualizando: Swashbuckle.AspNetCore

Proyecto: MiApi.csproj
├── Versión actual:  6.5.0
├── Versión nueva:   6.7.3
├── Tipo cambio:     🟢 Minor (compatible)
└── Estado::          ✅ Actualizado

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ ACTUALIZACIÓN COMPLETADA

📋 Cambios realizados:
   • MiApi.csproj: Swashbuckle.AspNetCore 6.5.0 → 6.7.3

⚠️ IMPORTANTE: Ejecutar después:
   1. dotnet restore
   2. dotnet build
   3. dotnet test

💡 Verificar que todo funciona antes de commit
```

---

### `--consolidate` - CONSOLIDACIÓN DE VERSIONES

```
▶ CONSOLIDACIÓN DE VERSIONES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

⚠️ INCONSISTENCIAS DETECTADAS

┌─────────────────────────────────────────────────────────────────┐
│ 📦 Newtonsoft.Json                                              │
│ ─────────────────────────────────────────────────────────────── │
│ MiApi.csproj:            13.0.1                                 │
│ MiInfrastructure.csproj: 12.0.3  ← ⚠️ DESACTUALIZADO           │
│ MiWorker.csproj:         13.0.3  ← ✅ Más reciente             │
│ ─────────────────────────────────────────────────────────────── │
│ 💡 Recomendación: Usar {0} en todos                          │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 📦 FluentValidation                                             │
│ ─────────────────────────────────────────────────────────────── │
│ MiApi.csproj:    11.9.0                                         │
│ MiDomain.csproj: 11.5.1  ← ⚠️ DESACTUALIZADO                   │
│ ─────────────────────────────────────────────────────────────── │
│ 💡 Recomendación: Usar {0} en todos                          │
└─────────────────────────────────────────────────────────────────┘

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 RESUMEN
┌─────────────────────────────────────────────────┐
│  Paquetes con versiones inconsistentes: 2       │
│  Proyectos afectados: 4                         │
└─────────────────────────────────────────────────┘

🔧 SOLUCIÓN RECOMENDADA: Directory.Packages.props

Crear archivo en raíz de solución para centralizar versiones:

```xml
<!-- 03_Desarrollo/Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageVersion Include="FluentValidation" Version="11.9.0" />
    <!-- ... más paquetes ... -->
  </ItemGroup>
</Project>
```

💡 Comandos:
   ¿Aplicar consolidación automática? (esto modificará los .csproj)
```

---

## Integración con Otros Comandos

| Comando | Relación |
|---------|----------|
| `/analizar` | Incluye resumen de NuGets en análisis completo |
| `/analizar --security` | Ejecuta `--audit` automáticamente |
| `/prepara-entrega` | Verifica que no haya CVEs críticos |
| `/test` | Verifica compatibilidad tras actualización |

---

## GESTIÓN NUGET COMPLETADA

```
📦 GESTIÓN NUGET COMPLETADA
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 Estado actual:
   • Paquetes totales: [N]
   • Desactualizados: [N]
   • Vulnerabilidades: [N]

💡 Próximos pasos:
   • /nugets --audit      → Revisar seguridad
   • /nugets --update     → Actualizar paquetes
   • /test                → Verificar compatibilidad
   • /commit              → Guardar cambios
```

---

## Uso de Skills Avanzados

### Cuando se detectan CVEs críticos

Si `--audit` detecta vulnerabilidades CRÍTICAS o ALTAS, Claude automáticamente:

1. Activa **nugets-management** para:
   - Interpretar severidad CVSS
   - Verificar versiones seguras
   - Comprobar compatibilidad con TargetFramework

2. Puede activar **security-audit** si:
   - Hay CVEs relacionados con deserialización (RCE)
   - Se detectan paquetes con problemas de autenticación
   - El proyecto necesita hardening adicional

### Solicitar análisis profundo

```
Usuario: "Analiza las dependencias NuGet y busca vulnerabilidades"
→ Claude activa: nugets-management skill

Usuario: "Implementa Central Package Management"
→ Claude activa: nugets-management skill (sección Directory.Packages.props)

Usuario: "Revisa la seguridad del proyecto"
→ Claude activa: security-audit skill

Usuario: "Busca secrets expuestos y actualiza los NuGets vulnerables"
→ Claude activa: ambos skills
```

### Acceso directo a skills

Los skills también se pueden invocar directamente si Claude detecta:
- Menciones a "CVE", "vulnerabilidad", "OWASP"
- Solicitudes de "Central Package Management" o "Directory.Packages.props"
- Peticiones de "auditoría de seguridad" o "buscar secrets"

---

## Verificacion de Licencias (v3.8.2)

Al ejecutar `--audit` o sin parametro, Claude debe verificar las licencias de los paquetes instalados:

```powershell
# Listar licencias de todos los paquetes
dotnet list package --format json | ConvertFrom-Json
# O revisar manualmente en cada .csproj las licencias conocidas
```

**Clasificacion de licencias:**

| Licencia | Estado | Accion |
|----------|--------|--------|
| MIT, Apache-2.0, BSD | ✅ Compatible | Ninguna |
| LGPL-2.1, LGPL-3.0 | ⚠️ Cuidado | Verificar que se usa como libreria, no modificado |
| GPL-2.0, GPL-3.0 | 🔴 Conflictivo | Alertar al usuario — incompatible con proyectos privativos Comillas |
| Comercial (MediatR v12+) | 🟡 Coste | Alertar — recomendar Mediator (MIT) |
| Sin licencia / Custom | ⚠️ Revisar | Requiere aprobacion manual del JP |

**Output esperado:**

```
🔒 LICENCIAS
━━━━━━━━━━━━━━
⚠️ MediatR 12.2.0 → Licencia COMERCIAL ($500-2000/año)
   💡 Alternativa: Mediator (MIT, gratuito, API compatible)
🔴 PaqueteX 1.0.0 → Licencia GPL-3.0
   ⚠️ INCOMPATIBLE con proyectos privativos Comillas
✅ 43 paquetes con licencias compatibles (MIT/Apache/BSD)
```

---

## Deteccion de Paquetes Deprecated (v3.8.2)

Al ejecutar `--audit` o `--outdated`, detectar paquetes marcados como **deprecated** en NuGet.org:

```powershell
dotnet list package --deprecated
```

**Paquetes comunes deprecated en stack Comillas:**

| Paquete deprecated | Alternativa | Razon |
|-------------------|-------------|-------|
| `Microsoft.AspNetCore.Mvc.NewtonsoftJson` | `System.Text.Json` nativo | Rendimiento + mantenimiento |
| `Swashbuckle.AspNetCore` (.NET 10) | OpenAPI nativo `AddOpenApi()` | Incluido en framework |
| `AutoMapper` (v13+) | `Mapster` o mapeo manual | Complejidad innecesaria para DTOs simples |
| `MediatR` (v12+) | `Mediator` (martinothamar) | Licencia comercial |

**Output esperado:**

```
⚠️ PAQUETES DEPRECATED
━━━━━━━━━━━━━━━━━━━━━━
📦 Swashbuckle.AspNetCore 6.7.3
   Estado: DEPRECATED en NuGet.org
   Razon: Reemplazado por OpenAPI nativo en .NET 10
   Alternativa: builder.Services.AddOpenApi()
   Afecta: MiApi.csproj

📦 AutoMapper.Extensions.Microsoft.DI 12.0.1
   Estado: DEPRECATED
   Razon: AutoMapper 13+ unifica DI extensions
   Alternativa: Actualizar a AutoMapper 13+ o usar Mapster
```

---

## Compatibilidad Cruzada entre Paquetes (v3.8.2)

Al ejecutar `--outdated` o `--update`, verificar que los paquetes son compatibles entre si:

**Matriz de compatibilidad conocida:**

| Paquete | Requiere | Conflicto con |
|---------|----------|---------------|
| EF Core 10.x | .NET 10+ | EF Core 8.x en misma solucion |
| Microsoft.Identity.Web 3.x | .NET 8+ | Azure.Identity 1.x legacy |
| Polly 8.x | .NET 8+ | Polly 7.x (API incompatible) |
| FluentValidation 11.9+ | .NET 8+ | FluentValidation.AspNetCore (deprecated) |
| xUnit 2.7+ | .NET 8+ | xunit.runner.console legacy |
| Serilog 4.x | .NET 8+ | Serilog.Sinks.Console 5.x (ok) vs 4.x (incompatible) |

**Output esperado al detectar incompatibilidad:**

```
⚠️ COMPATIBILIDAD CRUZADA
━━━━━━━━━━━━━━━━━━━━━━━━━
🔴 EF Core 10.0.0 + TargetFramework net8.0 → INCOMPATIBLE
   EF Core 10.x requiere .NET 10+
   Accion: Migrar a net10.0 primero, luego actualizar EF Core

🟡 FluentValidation 11.9.0 + FluentValidation.AspNetCore 11.3.0
   FluentValidation.AspNetCore esta DEPRECATED
   Accion: Usar FluentValidation.DependencyInjectionExtensions
```

---

## Estrategia de Rollback (v3.8.2)

Al ejecutar `--update`, si la actualizacion rompe la compilacion o tests:

**Protocolo de rollback:**

1. **Intento 1**: Corregir el codigo afectado (breaking change menor)
2. **Intento 2**: Si el esfuerzo es desproporcionado, revertir el paquete:
   ```powershell
   dotnet add package [Paquete] --version [version-anterior]
   ```
3. **Documentar** el bloqueo en `_duran/DEUDA_TECNICA.md`:
   ```
   ## Paquete bloqueado: [Nombre]
   - Version actual: X.Y.Z
   - Version deseada: A.B.C
   - Motivo bloqueo: [breaking change en API X]
   - Esfuerzo estimado: [horas]
   - Riesgo de no actualizar: [CVE-XXXX si aplica]
   ```
4. **Continuar** con el resto de actualizaciones

---

## Propuesta Dependabot / Renovate (v3.8.2)

Al final de `--consolidate` o `--audit`, si el proyecto no tiene automatizacion de dependencias, proponer:

```yaml
# .github/dependabot.yml (si GitHub)
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/03_Desarrollo"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 5
    labels: ["dependencies", "nuget"]
    ignore:
      - dependency-name: "MediatR"
        versions: [">=12.0.0"]
```

```json
// renovate.json (si Azure DevOps / genérico)
{
  "$schema": "https://docs.renovatebot.com/renovate-schema.json",
  "extends": ["config:base"],
  "packageRules": [
    {
      "matchManagers": ["nuget"],
      "matchUpdateTypes": ["minor", "patch"],
      "automerge": true
    },
    {
      "matchPackageNames": ["MediatR"],
      "enabled": false
    }
  ]
}
```

---

## Historico de Auditorias DURAN (v3.8.2)

Tras cada ejecucion de `--audit`, guardar resultado en `_duran/AUDITORIA_NUGET.json`:

```json
{
  "_comentario": "Historico de auditorias NuGet. Se actualiza con /nugets --audit",
  "ultimaAuditoria": "2026-04-16",
  "auditorias": [
    {
      "fecha": "2026-04-16",
      "ejecutadoPor": "HV",
      "vulnerabilidades": {
        "criticas": 0,
        "altas": 1,
        "medias": 2,
        "bajas": 0
      },
      "deprecated": 2,
      "licenciasConflictivas": 0,
      "paquetesTotales": 45,
      "paquetesDesactualizados": 8,
      "accionesTomadas": [
        "Actualizado Newtonsoft.Json 12.0.3 → 13.0.3 (CVE-2024-21907)",
        "Bloqueado: EF Core 10.x requiere migrar a .NET 10 primero"
      ]
    }
  ]
}
```

Claude debe LEER este archivo al inicio de `--audit` para comparar con la auditoria anterior y detectar **regresiones** (nuevas vulnerabilidades desde la ultima auditoria).

---

*Comando actualizado v3.8.2 - Con licencias, deprecated, cross-compat, rollback, Dependabot, historico DURAN*
