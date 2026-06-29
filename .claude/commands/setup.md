Setup completo de proyecto Comillas - instala comandos, estructura y plantilla

# Setup Completo de Proyecto Comillas

## 🧠 Extended Thinking Mode

**think hard** - Analiza cuidadosamente la estructura del proyecto antes de configurar.

## Propósito
Comando único que configura todo lo necesario para un proyecto Comillas:
1. ✅ Verifica pre-requisitos
2. 📥 Descarga e instala comandos personalizados
3. 📁 Crea estructura de carpetas (incluyendo 03_Desarrollo/)
4. 📄 Descarga archivos de plantilla
5. 📦 Mueve código existente a 03_Desarrollo/
6. 🔎 Agrega a solución VS

---

## IMPORTANTE: Estructura Final

```
MiProyecto/                     ← Raíz del repo (Git/TFS)
├── CLAUDE.md
├── .claude/
│   ├── commands/
│   └── rules/
├── _duran/
├── 00_Gestion/
├── 01_Diseno/
├── 02_Entorno/
├── 03_Desarrollo/              ← TODO EL CÓDIGO AQUÍ ⭐
│   ├── MiSolucion.sln
│   ├── MiProyecto.API/
│   ├── MiProyecto.Domain/
│   └── ...
├── 04_Pruebas/
├── 05_CICD/
├── 06_Documentacion/
├── 07_UAP/
└── Documentos_Base/
```

---

## PRE-REQUISITOS

### Verificar Claude Code inicializado

```powershell
$claudeMdExists = Test-Path "CLAUDE.md"
$claudeFolderExists = Test-Path ".claude"

if (-not $claudeMdExists -and -not $claudeFolderExists) {
    Write-Host ""
    Write-Host "⚠️  CLAUDE CODE NO INICIALIZADO" -ForegroundColor Yellow
    Write-Host "════════════════════════════════" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Ejecuta primero:" -ForegroundColor Cyan
    Write-Host "  /init" -ForegroundColor White
    Write-Host ""
    Write-Host "Después vuelve a ejecutar:" -ForegroundColor Cyan
    Write-Host "  /setup" -ForegroundColor White
    Write-Host ""
    return
}

Write-Host "✅ Claude Code inicializado" -ForegroundColor Green
```

### Detectar proyecto existente

```powershell
# Nota: .slnx es el nuevo formato XML disponible en .NET 8+ (VS 2022 17.10+)
$slnFileRoot = Get-ChildItem -Filter "*.sln" -Depth 0 -ErrorAction SilentlyContinue | Select-Object -First 1
$slnxFileRoot = Get-ChildItem -Filter "*.slnx" -Depth 0 -ErrorAction SilentlyContinue | Select-Object -First 1
$slnFileDev = Get-ChildItem -Path "03_Desarrollo" -Filter "*.sln" -ErrorAction SilentlyContinue | Select-Object -First 1
$slnxFileDev = Get-ChildItem -Path "03_Desarrollo" -Filter "*.slnx" -ErrorAction SilentlyContinue | Select-Object -First 1

if ($slnxFileDev) {
    $slnFile = $slnxFileDev
    $codigoEnDesarrollo = $true
    Write-Host "✅ Solución (.slnx) ya en 03_Desarrollo/: $($slnFile.Name)" -ForegroundColor Green
} elseif ($slnFileDev) {
    $slnFile = $slnFileDev
    $codigoEnDesarrollo = $true
    Write-Host "✅ Solución (.sln) ya en 03_Desarrollo/: $($slnFile.Name)" -ForegroundColor Green
} elseif ($slnxFileRoot) {
    $slnFile = $slnxFileRoot
    $codigoEnDesarrollo = $false
    Write-Host "⚠️  Solución (.slnx) en raíz - Se moverá a 03_Desarrollo/" -ForegroundColor Yellow
} elseif ($slnFileRoot) {
    $slnFile = $slnFileRoot
    $codigoEnDesarrollo = $false
    Write-Host "⚠️  Solución (.sln) en raíz - Se moverá a 03_Desarrollo/" -ForegroundColor Yellow
} else {
    $nombreProyecto = Split-Path -Leaf (Get-Location)
    Write-Host "⚠️  Sin solución (.sln/.slnx) - usando nombre de carpeta: $nombreProyecto" -ForegroundColor Yellow
}
```

**Matriz de compatibilidad de formatos:**
```
┌───────────────────────────────────────────────────────────────┐
│ .NET 4.x  │ .NET 7  │ .NET 8  │ .NET 9  │ .NET 10 │ Formato    │
├───────────┼─────────┼─────────┼─────────┼─────────┼────────────┤
│    ✅     │   ✅    │   ✅    │   ✅    │   ✅    │ .sln       │
│    ❌     │   ❌    │   ✅    │   ✅    │   ✅    │ .slnx      │
└───────────────────────────────────────────────────────────────┘
```

---

## CONFIGURACIÓN DE FUENTES

```yaml
fuente_activa: mcp  # mcp | azure-devops | github | gitlab | raw-url | local

mcp:
  servidor: "plantilla-otic"

azure-devops:
  org: "comillas"
  project: "ComunesSTIC"
  repo: "STIC.IA"
  branch: "main"

local:
  path: "D:\\tfs.comillas.edu\\ComunesSTIC\\STIC.IA"
```

### Argumentos

```
/setup                          → Usa fuente predeterminada (mcp)
/setup --fuente=local           → Usa clon local
/setup --skip-commands          → Solo estructura, no comandos
/setup --skip-move              → No mover código a 03_Desarrollo/
/setup --force                  → Sobrescribir archivos existentes
```

---

## FASE 1: Crear Estructura de Carpetas

```powershell
Write-Host ""
Write-Host "📁 Creando estructura de carpetas..." -ForegroundColor Cyan
Write-Host ""

$carpetas = @(
    ".claude",
    ".claude/commands",
    ".claude/rules",
    "_duran",
    "_duran/specs",
    "_duran/reuniones",
    "00_Gestion",
    "00_Gestion/Requerimientos",
    "00_Gestion/Reuniones",
    "01_Diseno",
    "01_Diseno/Arquitectura",
    "02_Entorno",
    "03_Desarrollo",        # ← CÓDIGO VA AQUÍ
    "04_Pruebas",
    "04_Pruebas/resultados",
    "05_CICD",
    "06_Documentacion",
    "06_Documentacion/API",
    "07_UAP",
    "Documentos_Base"
)

foreach ($carpeta in $carpetas) {
    if (-not (Test-Path $carpeta)) {
        New-Item -ItemType Directory -Path $carpeta -Force | Out-Null
        Write-Host "  ✅ $carpeta" -ForegroundColor Green
    } else {
        Write-Host "  ⭐️ $carpeta (existe)" -ForegroundColor DarkGray
    }
}
```

---

## FASE 2: Mover Código Existente a 03_Desarrollo/

**CRÍTICO: Si hay código en la raíz, moverlo a `03_Desarrollo/`.**

```powershell
if (-not $codigoEnDesarrollo -and $slnFileRoot) {
    Write-Host ""
    Write-Host "📦 Moviendo código a 03_Desarrollo/..." -ForegroundColor Cyan
    Write-Host ""

    # Patrones de archivos/carpetas de código (incluye .slnx para .NET 8+)
    $codigoPatrones = @(
        "*.sln",
        "*.slnx",
        "*.csproj",
        ".gitignore",
        ".editorconfig",
        "Directory.Build.props",
        "Directory.Build.targets",
        "global.json",
        "nuget.config"
    )

    # Carpetas excluidas (no mover)
    $excluidas = @(
        ".claude", "_duran", ".git", ".vs", "node_modules",
        "00_Gestion", "01_Diseno", "02_Entorno", "03_Desarrollo",
        "04_Pruebas", "05_CICD", "06_Documentacion", "07_UAP",
        "Documentos_Base"
    )

    # Detectar carpetas de proyectos .NET
    Get-ChildItem -Directory | Where-Object { $_.Name -notin $excluidas } | ForEach-Object {
        $hasCsproj = Get-ChildItem -Path $_.FullName -Filter "*.csproj" -Recurse -Depth 2
        if ($hasCsproj) {
            Move-Item -Path $_.FullName -Destination "03_Desarrollo/$($_.Name)" -Force
            Write-Host "  → $($_.Name) → 03_Desarrollo/" -ForegroundColor Magenta
        }
    }

    # Mover archivos de código en raíz
    foreach ($patron in $codigoPatrones) {
        Get-ChildItem -Filter $patron -File | ForEach-Object {
            Move-Item -Path $_.FullName -Destination "03_Desarrollo/$($_.Name)" -Force
            Write-Host "  → $($_.Name) → 03_Desarrollo/" -ForegroundColor Magenta
        }
    }

    Write-Host ""
    Write-Host "  ✅ Código movido a 03_Desarrollo/" -ForegroundColor Green
}
```

---

## FASE 3: Instalar Comandos Personalizados

### Lista de comandos a instalar

```yaml
comandos:
  # Core
  - nombre: "analizar"
    descripcion: "Análisis de arquitectura y código"

  - nombre: "onboarding"
    descripcion: "Contextualización del proyecto"

  - nombre: "estado"
    descripcion: "Dashboard del proyecto"

  # Desarrollo
  - nombre: "nuevo-evolutivo"
    descripcion: "Iniciar nuevo evolutivo"

  - nombre: "revision"
    descripcion: "Análisis de impacto"

  - nombre: "test"
    descripcion: "Ejecutar tests"

  - nombre: "commit"
    descripcion: "Commit con conventional commits"

  - nombre: "documentar"
    descripcion: "Generar documentación"

  # Sesión
  - nombre: "continuar"
    descripcion: "Retomar sesión"

  - nombre: "pausar"
    descripcion: "Guardar estado"

  - nombre: "sos"
    descripcion: "Guardado de emergencia"

  # Otros
  - nombre: "migrar"
    descripcion: "Migración de .NET"

  - nombre: "prepara-entrega"
    descripcion: "Preparar release"
```

```powershell
Write-Host ""
Write-Host "📥 Instalando comandos personalizados..." -ForegroundColor Cyan
Write-Host ""

$comandosInstalados = 0

foreach ($cmd in $comandos) {
    $localPath = ".claude/commands/$($cmd.nombre).md"

    if ((Test-Path $localPath) -and -not $force) {
        Write-Host "  ⭐️ $($cmd.nombre) (existe)" -ForegroundColor DarkGray
    } else {
        $contenido = Get-PlantillaFile -filePath ".claude/commands/$($cmd.nombre).md"
        if ($contenido) {
            Set-Content -Path $localPath -Value $contenido -Encoding UTF8
            Write-Host "  ✅ $($cmd.nombre)" -ForegroundColor Green
            $comandosInstalados++
        }
    }
}

Write-Host ""
Write-Host "   Comandos instalados: $comandosInstalados" -ForegroundColor Cyan
```

---

## FASE 4: Descargar Archivos de Plantilla

```powershell
Write-Host ""
Write-Host "📄 Descargando archivos de plantilla..." -ForegroundColor Cyan
Write-Host ""

$archivos = @(
    @{ remote = "_duran/ESTADO_PROYECTO.json"; local = "_duran/ESTADO_PROYECTO.json" },
    @{ remote = "_duran/DECISIONES.md"; local = "_duran/DECISIONES.md" },
    @{ remote = "_duran/DEPENDENCIAS.md"; local = "_duran/DEPENDENCIAS.md" },
    @{ remote = "_duran/FUNCIONALIDADES.md"; local = "_duran/FUNCIONALIDADES.md" },
    @{ remote = "_duran/HISTORIAL_CAMBIOS.md"; local = "_duran/HISTORIAL_CAMBIOS.md" },
    @{ remote = "_duran/DEUDA_TECNICA.md"; local = "_duran/DEUDA_TECNICA.md" },
    @{ remote = ".claude/CLAUDE_BASE_COMILLAS.md"; local = ".claude/CLAUDE_BASE_COMILLAS.md" }
)

foreach ($archivo in $archivos) {
    if ((Test-Path $archivo.local) -and -not $force) {
        Write-Host "  ⭐️ $($archivo.local) (existe)" -ForegroundColor DarkGray
    } else {
        $contenido = Get-PlantillaFile -filePath $archivo.remote
        if ($contenido) {
            Set-Content -Path $archivo.local -Value $contenido -Encoding UTF8
            Write-Host "  ✅ $($archivo.local)" -ForegroundColor Green
        }
    }
}
```

---

## FASE 5: Configurar ESTADO_PROYECTO.json

```powershell
$estadoPath = "_duran/ESTADO_PROYECTO.json"
$estado = @{
    proyecto = @{
        nombre = $nombreProyecto
        rutaCodigo = "03_Desarrollo"
        fechaSetup = (Get-Date -Format "yyyy-MM-dd")
    }
    estado = @{
        modo = "puesta_a_punto"
        fase_actual = "setup"
    }
}

$estado | ConvertTo-Json -Depth 10 | Set-Content -Path $estadoPath -Encoding UTF8
Write-Host "  ✅ ESTADO_PROYECTO.json configurado" -ForegroundColor Green
```

---

## FASE 6: Resumen Final

```
╔═══════════════════════════════════════════════════════════════╗
║                    SETUP COMPLETADO                               ║
╚═══════════════════════════════════════════════════════════════╝

📋 Proyecto: [NOMBRE_PROYECTO]
🌐 Fuente:   [FUENTE_USADA]

┌───────────────────────────────────────────────────────────────┐
│  📁 ESTRUCTURA CREADA                                           │
├───────────────────────────────────────────────────────────────┤
│  ✅ .claude/commands/    ([N] comandos)                         │
│  ✅ .claude/rules/                                              │
│  ✅ _duran/                                                  │
│  ✅ 00_Gestion/                                                 │
│  ✅ 01_Diseno/Arquitectura/                                     │
│  ✅ 02_Entorno/                                                 │
│  ✅ 03_Desarrollo/        ← TODO EL CÓDIGO AQUÍ ⭐                       │
│  ✅ 04_Pruebas/                                                 │
│  ✅ 05_CICD/                                                    │
│  ✅ 06_Documentacion/                                           │
│  ✅ 07_UAP/                                                     │
│  ✅ Documentos_Base/                                            │
└───────────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────┐
│  📦 CÓDIGO REORGANIZADO                                         │
├───────────────────────────────────────────────────────────────┤
│  ✅ Solución movida a: 03_Desarrollo/[nombre].sln               │
│  ✅ Proyectos movidos: [N]                                      │
│  ✅ Archivos de config movidos                                  │
└───────────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────┐
│  📥 COMANDOS INSTALADOS                                         │
├───────────────────────────────────────────────────────────────┤
│  ✅ /analizar        ✅ /nuevo-evolutivo   ✅ /continuar        │
│  ✅ /onboarding      ✅ /revision          ✅ /pausar           │
│  ✅ /estado          ✅ /test              ✅ /sos              │
│  ✅ /commit          ✅ /documentar        ✅ /migrar           │
└───────────────────────────────────────────────────────────────┘

═══════════════════════════════════════════════════════════════

🎯 PRÓXIMOS PASOS:

   1. /onboarding    Configurar datos del equipo
                 (owner, JP, RT, stack, integraciones)

   2. /analizar      Análisis técnico del código en 03_Desarrollo/
                 (seguridad, deuda técnica, diagramas)

   3. /estado        Ver dashboard del proyecto

═══════════════════════════════════════════════════════════════

📋 FLUJO COMPLETO:
   /init → /setup → /onboarding → /analizar
   ✅       ✅        ◻            ◻

💡 Tip: Ejecuta /onboarding ahora para completar la configuración.

📌 NOTA: El código está ahora en 03_Desarrollo/
       Abre la solución desde: 03_Desarrollo/[nombre].sln
```

---

## TROUBLESHOOTING

| Error | Causa | Solución |
|-------|-------|----------|
| "Claude Code no inicializado" | Falta /init | Ejecutar `/init` primero |
| "No se pudo conectar" | Red/Auth | Usar `--fuente=local` |
| "Código no movido" | Ya en 03_Desarrollo/ | Normal si ya está organizado |
| "Permiso denegado" | Carpeta protegida | Ejecutar como admin |

---

## Configuración

```yaml
version: "2.2.0"
autor: "Comillas STIC"
ultima_actualizacion: "2026-01-21"
```
