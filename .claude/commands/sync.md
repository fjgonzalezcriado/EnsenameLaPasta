Sincroniza documentación de contexto con el código actual

# Sincronizar Contexto

## Contexto
Actualizar la documentación en `_duran/` para reflejar el estado real del código en `03_Desarrollo/`

## IMPORTANTE: Estructura de Carpetas

```
MiProyecto/
├── _duran/                  ← DOCUMENTACIÓN (se actualiza)
│   ├── DEPENDENCIAS.md
│   ├── FUNCIONALIDADES.md
│   └── HISTORIAL_CAMBIOS.md
├── 03_Desarrollo/              ← CÓDIGO FUENTE (se lee)
│   ├── MiSolucion.sln|.slnx    ← .slnx solo .NET 8+
│   └── ...
└── ...
```

## Tareas a Ejecutar

### 1. Analizar Código Actual en 03_Desarrollo/

Escanear rápidamente:

```powershell
# Estructura de proyectos
$projects = Get-ChildItem -Path "03_Desarrollo" -Filter "*.csproj" -Recurse

# Dependencias (NuGet packages)
$packages = @()
foreach ($csproj in $projects) {
    $content = Get-Content $csproj.FullName -Raw
    $matches = [regex]::Matches($content, '<PackageReference Include="([^"]+)" Version="([^"]+)"')
    foreach ($match in $matches) {
        $packages += @{
            Name = $match.Groups[1].Value
            Version = $match.Groups[2].Value
            Project = $csproj.Name
        }
    }
}

# Clases y servicios principales
$services = Get-ChildItem -Path "03_Desarrollo" -Filter "*Service.cs" -Recurse
$controllers = Get-ChildItem -Path "03_Desarrollo" -Filter "*Controller.cs" -Recurse
$repositories = Get-ChildItem -Path "03_Desarrollo" -Filter "*Repository.cs" -Recurse
```

### 2. Comparar con Documentación

Leer `_duran/DEPENDENCIAS.md` y comparar:

```
🔄 SINCRONIZACIÓN DE CONTEXTO
═══════════════════════

🔍 Código analizado: 03_Desarrollo/

📦 DEPENDENCIAS
   Documentadas: [N]
   En código: [M]

   ➕ Nuevas (no documentadas):
      • Package.Nuevo v1.0.0

   ➖ Eliminadas (documentadas pero no en código):
      • Package.Viejo

   🔄 Actualizadas:
      • Package.X: 1.0.0 → 2.0.0
```

### 3. Comparar Funcionalidades

Leer `_duran/FUNCIONALIDADES.md` y detectar:
- Módulos nuevos no documentados
- Módulos documentados que ya no existen

```
📋 FUNCIONALIDADES

   🆕 Detectadas en código (no documentadas):
      • 03_Desarrollo/MiProyecto.API/Controllers/NuevoController.cs
      • 03_Desarrollo/MiProyecto.Domain/Services/NuevoService.cs

   ❓ Documentadas (no encontradas en código):
      • ModuloAntiguo
```

### 4. Comparar Estructura de Proyectos

```
📂 ESTRUCTURA (en 03_Desarrollo/)

   Proyectos documentados: [N]
   Proyectos en código: [M]

   🆕 Proyectos nuevos:
      • 03_Desarrollo/MiProyecto.NewModule/

   ❓ Proyectos eliminados:
      • MiProyecto.OldModule (documentado pero no existe)
```

### 5. Proponer Actualizaciones

```
¿Actualizar documentación automáticamente?

1. Sí, actualizar todo
2. Revisar cambio por cambio
3. No, solo mostrar diferencias
```

### 6. Aplicar Cambios

Si el usuario acepta:

**Actualizar `_duran/DEPENDENCIAS.md`:**
```markdown
# Dependencias del Proyecto

## Paquetes NuGet (03_Desarrollo/)

| Paquete | Versión | Proyecto |
|---------|---------|----------|
| Microsoft.AspNetCore.Mvc | 8.0.0 | MiProyecto.API |
| Dapper | 2.1.0 | MiProyecto.Infrastructure |

_Última sincronización: [FECHA]_
```

**Actualizar `_duran/FUNCIONALIDADES.md`:**
- Añadir módulos detectados
- Marcar como ❓ los no encontrados

**Registrar en `_duran/HISTORIAL_CAMBIOS.md`:**
```markdown
### [FECHA] - Sincronización de contexto

- **Tipo: 🔄 Sincronización**
- **Dependencias: +[N] añadidas, -[M] eliminadas, ~[O] actualizadas**
- **Funcionalidades: +[N] detectadas**
- **Código analizado: 03_Desarrollo/**
```

### 7. Confirmación

```
✅ SINCRONIZACIÓN COMPLETADA
═══════════════════════

🔍 Código analizado: 03_Desarrollo/

📦 Dependencias: [N] actualizadas
📋 Funcionalidades: [M] actualizadas
📝 Historial: Entrada añadida

📁 Archivos modificados:
   • _duran/DEPENDENCIAS.md
   • _duran/FUNCIONALIDADES.md
   • _duran/HISTORIAL_CAMBIOS.md

💡 Próximos pasos:
   • /commit -m "docs: Sincronizar contexto con código"
   • /estado - Ver dashboard actualizado
```

---

## Detección Automática de Cambios

```powershell
# Comparar fecha de última sincronización con última modificación de código
$ultimaSync = (Get-Content "_duran/ESTADO_PROYECTO.json" | ConvertFrom-Json).ultimaSync
$ultimaModCodigo = (Get-ChildItem -Path "03_Desarrollo" -Filter "*.cs" -Recurse |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1).LastWriteTime

if ($ultimaModCodigo -gt $ultimaSync) {
    Write-Host "⚠️ Código modificado desde última sincronización" -ForegroundColor Yellow
    Write-Host "   Ejecuta /sync para actualizar documentación" -ForegroundColor Cyan
}
```
