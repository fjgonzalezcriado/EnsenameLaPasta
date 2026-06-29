Prepara versión para release - genera documentación de entrega

# /prepara-entrega - Preparar versión para release

## Parámetro Recibido
- **Versión**: $ARGUMENTS (ej: v1.2.0)

Si no se proporciona, preguntar o detectar de `.csproj` en `03_Desarrollo/`.

## IMPORTANTE: Estructura de Carpetas

```
MiProyecto/
├── _duran/                  ← Estado del proyecto
├── 03_Desarrollo/              ← CÓDIGO FUENTE ⭐
│   ├── MiSolucion.sln|.slnx    ← .slnx solo .NET 8+
│   ├── MiProyecto.API/
│   │   └── MiProyecto.API.csproj  ← Versión aquí
│   └── ...
├── 06_Documentacion/           ← Changelogs y release notes
└── ...
```

## Tareas a Ejecutar

### 1. Verificaciones Pre-Entrega

```powershell
# Compilar desde 03_Desarrollo/
Push-Location "03_Desarrollo"
dotnet build --no-restore
Pop-Location

# Ejecutar tests (detecta .slnx o .sln)
$sln = Get-ChildItem -Path "03_Desarrollo" -Filter "*.slnx" | Select-Object -First 1
if (-not $sln) { $sln = Get-ChildItem -Path "03_Desarrollo" -Filter "*.sln" | Select-Object -First 1 }
dotnet test $sln.FullName
```

```
📝 VERIFICACIONES PRE-ENTREGA
────────────────────

✅ Compilación exitosa (03_Desarrollo/)
✅ Tests pasando
✅ Sin cambios pendientes de commit
✅ Sin TODOs críticos
⚠️ Deuda técnica crítica: [N] items

¿Continuar? (s/n)
```

### 2. Recopilar Información

Detectar automáticamente::
- Versión anterior (del último tag o release)
- Evolutivos completados desde última versión
- Commits desde última versión

```powershell
# Detectar versión actual desde .csproj
$csproj = Get-ChildItem -Path "03_Desarrollo" -Filter "*.csproj" -Recurse | Select-Object -First 1
$content = Get-Content $csproj.FullName -Raw
if ($content -match '<Version>([^<]+)</Version>') {
    $versionActual = $Matches[1]
}
```

### 3. Generar Changelog

```markdown
# Changelog - [versión]

**Fecha**: [FECHA_ACTUAL]

## ✨ Nuevas Funcionalidades
- [EVO-XXX] Descripción

## 🐛 Correcciones
- [fix] Descripción

## 🔧 Mejoras
- [refactor] Descripción

## 📦 Dependencias Actualizadas
- Package.X: 1.0.0 → 2.0.0
```

Guardar en `06_Documentacion/CHANGELOG_[version].md`

### 4. Generar Notas de Release

```markdown
# Release Notes - [versión]

## Resumen
[Descripción general de la versión]

## Requisitos
- .NET [versión]
- SQL Server [versión]

## Instalación
1. Paso 1
2. Paso 2

## Configuración
[Cambios en configuración necesarios]

## Breaking Changes
[Lista de cambios que rompen compatibilidad, si hay]

## Problemas Conocidos
[Lista de issues conocidos, si hay]
```

Guardar en `06_Documentacion/RELEASE_NOTES_[version].md`

### 5. Actualizar Versión en Archivos

Si hay archivos de versión en `03_Desarrollo/`::

```powershell
# Actualizar todos los .csproj
Get-ChildItem -Path "03_Desarrollo" -Filter "*.csproj" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $newContent = $content -replace '<Version>[^<]+</Version>', "<Version>$nuevaVersion</Version>"
    Set-Content -Path $_.FullName -Value $newContent
}
```

- `03_Desarrollo/**/*.csproj` → `<Version>`
- `03_Desarrollo/**/package.json` → `"version"`
- `03_Desarrollo/**/AssemblyInfo.cs` → `AssemblyVersion`

```
📝 ¿Actualizar versión en archivos del proyecto? (s/n)
```

### 6. Crear Tag (Git)

```
🏷️ ¿Crear tag de versión? (s/n)

git tag -a v[versión] -m "Release [versión]"
```

### 7. Actualizar Estado

`_duran/ESTADO_PROYECTO.json`:
```json
{
  "proyecto": {
    "version": "[versión]",
    "ultimaEntrega": "[FECHA_ACTUAL]",
    "rutaCodigo": "03_Desarrollo"
  }
}
```

### 8. Generar Resumen

```
📦 ENTREGA PREPARADA
────────────────

📋 Versión: [versión]
📅 Fecha: [FECHA_ACTUAL]
📂 Código: 03_Desarrollo/

📄 Documentos generados:
   • 06_Documentacion/CHANGELOG_[version].md
   • 06_Documentacion/RELEASE_NOTES_[version].md

✨ Evolutivos incluidos: [N]
🐛 Fixes incluidos: [M]

🏷️ Tag creado: v[versión]

💡 Próximos pasos::
   1. Revisar documentos generados
   2. git push --tags (si aplica)
   3. Desplegar a entorno
   4. Comunicar a stakeholders
```
