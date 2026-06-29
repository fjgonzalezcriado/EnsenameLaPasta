Genera documentación técnica del código fuente

# Comando: /documentar

Genera documentación técnica del código fuente ubicado en `03_Desarrollo/`.

## 🧠 Extended Thinking Mode

**think hard** - Analiza el código en profundidad para generar documentación precisa y útil.

---

## IMPORTANTE: Estructura de Carpetas

```
MiProyecto/
├── _duran/                  ← Contexto del proyecto
│   ├── FUNCIONALIDADES.md
│   └── DEPENDENCIAS.md
├── 03_Desarrollo/              ← CÓDIGO FUENTE A DOCUMENTAR ⭐
│   ├── MiSolucion.sln|.slnx    ← .slnx solo .NET 8+
│   ├── MiProyecto.API/
│   └── ...
└── 06_Documentacion/           ← DOCUMENTACIÓN GENERADA ⭐
    ├── API/
    │   └── README_API.md
    ├── Arquitectura/
    │   └── ARQUITECTURA.md
    ├── Codigo/
    │   └── [Proyecto]/
    └── README.md
```

---

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `--api` | Documentar endpoints de la API |
| `--code` | Documentar clases y métodos principales |
| `--readme` | Generar README.md del proyecto |
| `--full` | Documentación completa (default) |
| `[ruta]` | Archivo o carpeta específica a documentar |

## Ejemplos de Uso

```bash
# Documentación completa
/documentar

# Solo documentar API
/documentar --api

# Documentar un archivo específico
/documentar 03_Desarrollo/MiProyecto.Domain/Services/BecaService.cs

# Documentar un proyecto completo
/documentar 03_Desarrollo/MiProyecto.Domain/
```

---

## Tareas a Ejecutar

### 1. Analizar Código Fuente

```powershell
# Detectar proyectos en 03_Desarrollo/
$projects = Get-ChildItem -Path "03_Desarrollo" -Filter "*.csproj" -Recurse

# Contar archivos por tipo
$csFiles = Get-ChildItem -Path "03_Desarrollo" -Filter "*.cs" -Recurse |
    Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" }
```

### 2. Generar Documentación de API (--api)

**Buscar Controllers en `03_Desarrollo/`:**

```powershell
$controllers = Get-ChildItem -Path "03_Desarrollo" -Filter "*Controller.cs" -Recurse
```

**Crear `06_Documentacion/API/README_API.md`:**

```markdown
# Documentación de API

## Resumen
- **Versión**: [detectada]
- **Base URL**: [detectada de launchSettings.json]
- **Autenticación**: [JWT/Azure AD/etc.]

## Endpoints

### 📋 BecasController
Base: `/api/becas`

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| GET | `/` | Lista todas las becas | 🔒 |
| GET | `/{id}` | Obtiene beca por ID | 🔒 |
| POST | `/` | Crea nueva beca | 🔒 Admin |
| PUT | `/{id}` | Actualiza beca | 🔒 Admin |
| DELETE | `/{id}` | Elimina beca | 🔒 Admin |

#### GET /api/becas
**Descripción**: Obtiene listado paginado de becas.

**Parámetros Query**:
| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| page | int | No | Página (default: 1) |
| pageSize | int | No | Tamaño (default: 10) |
| estado | string | No | Filtro por estado |

**Respuesta 200**:
```json
{
  "data": [...],
  "total": 100,
  "page": 1,
  "pageSize": 10
}
```

**Códigos de Error**:
| Código | Descripción |
|--------|-------------|
| 401 | No autenticado |
| 403 | Sin permisos |
| 500 | Error interno |

---

### 📋 [OtroController]
...
```

### 3. Generar Documentación de Código (--code)

**Analizar clases principales en `03_Desarrollo/`:**

```powershell
# Buscar servicios, repositorios, entidades
$services = Get-ChildItem -Path "03_Desarrollo" -Filter "*Service.cs" -Recurse
$repositories = Get-ChildItem -Path "03_Desarrollo" -Filter "*Repository.cs" -Recurse
$entities = Get-ChildItem -Path "03_Desarrollo" -Filter "*.cs" -Recurse |
    Where-Object { $_.Directory.Name -eq "Entities" -or $_.Directory.Name -eq "Models" }
```

**Crear `06_Documentacion/Codigo/[Proyecto]/README.md`:**

```markdown
# MiProyecto.Domain

## Descripción
Capa de dominio con entidades y lógica de negocio.

## Estructura
```
MiProyecto.Domain/
├── Entities/
│   ├── Beca.cs
│   └── Usuario.cs
├── Services/
│   └── BecaService.cs
├── Interfaces/
│   └── IBecaRepository.cs
└── Exceptions/
    └── BecaNotFoundException.cs
```

## Entidades

### Beca
**Ubicación**: `03_Desarrollo/MiProyecto.Domain/Entities/Beca.cs`

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| Id | int | Identificador único |
| Nombre | string | Nombre de la beca |
| Estado | EstadoBeca | Estado actual |
| FechaCreacion | DateTime | Fecha de creación |

### Usuario
...

## Servicios

### BecaService
**Ubicación**: `03_Desarrollo/MiProyecto.Domain/Services/BecaService.cs`

**Responsabilidad**: Gestión de becas y validaciones de negocio.

**Métodos**:
| Método | Parámetro | Retorno | Descripción |
|--------|------------|---------|-------------|
| GetByIdAsync | int id | Task<Beca> | Obtiene beca por ID |
| CreateAsync | BecaDto dto | Task<Beca> | Crea nueva beca |
| ApproveAsync | int id | Task<bool> | Aprueba una beca |

**Dependencias**:
- `IBecaRepository`
- `ILogger<BecaService>`
- `IValidator<BecaDto>`
```

### 4. Generar README Principal (--readme)

**Crear `06_Documentacion/README.md`:**

```markdown
# [Nombre del Proyecto]

## Descripción
[Extraído de _duran/FUNCIONALIDADES.md]

## Requisitos
- .NET [versión]
- SQL Server [versión]
- [Otros requisitos]

## Estructura del Proyecto

```
MiProyecto/
├── 03_Desarrollo/              ← Código fuente
│   ├── MiProyecto.sln|.slnx    ← .slnx solo .NET 8+
│   ├── MiProyecto.API/         ← Web API
│   ├── MiProyecto.Domain/      ← Dominio
│   ├── MiProyecto.Infrastructure/ ← Infraestructura
│   └── MiProyecto.Tests/       ← Tests
├── _duran/                  ← Documentación de contexto
├── 01_Diseno/                  ← Diagramas
└── 06_Documentacion/           ← Esta documentación
```

## Instalación

```bash
# Clonar repositorio
git clone [url]

# Restaurar dependencias
cd 03_Desarrollo
dotnet restore

# Configurar base de datos
# [instrucciones]

# Ejecutar
dotnet run --project MiProyecto.API
```

## Configuración

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "Jwt": {
    "Key": "...",
    "Issuer": "..."
  }
}
```

## API
Ver [documentación de API](./API/README_API.md)

## Arquitectura
Ver [documentación de arquitectura](./Arquitectura/ARQUITECTURA.md)

## Tests

```bash
cd 03_Desarrollo
dotnet test
```

## Equipo
[Extraído de _duran/ESTADO_PROYECTO.json]

## Enlaces
- [Funcionalidades](_duran/FUNCIONALIDADES.md)
- [Decisiones técnicas](_duran/DECISIONES.md)
- [Diagramas](01_Diseno/Arquitectura/)
```

### 5. Documentación Completa (--full)

Ejecutar todos los anteriores:
1. Documentación de API
2. Documentación de código por proyecto
3. README principal
4. Generar índice

**Crear `06_Documentacion/INDEX.md`:**

```markdown
# Índice de Documentación

## 📚 Documentación General
- [README Principal](./README.md)
- [Arquitectura](./Arquitectura/ARQUITECTURA.md)

## 📌 API
- [Documentación de Endpoints](./API/README_API.md)
- [Autenticación](./API/AUTH.md)
- [Códigos de Error](./API/ERRORS.md)

## 💻 Código
- [MiProyecto.API](./Codigo/MiProyecto.API/README.md)
- [MiProyecto.Domain](./Codigo/MiProyecto.Domain/README.md)
- [MiProyecto.Infrastructure](./Codigo/MiProyecto.Infrastructure/README.md)

## 📊 Contexto del Proyecto
- [Funcionalidades](../_duran/FUNCIONALIDADES.md)
- [Dependencias](../_duran/DEPENDENCIAS.md)
- [Decisiones técnicas](../_duran/DECISIONES.md)
- [Deuda Técnica](../_duran/DEUDA_TECNICA.md)

## 📈 Diagramas
- [Componentes](../01_Diseno/Arquitectura/DIAGRAMA_COMPONENTES.md)
- [Dependencias](../01_Diseno/Arquitectura/DIAGRAMA_DEPENDENCIAS.md)
- [Entidades](../01_Diseno/Arquitectura/DIAGRAMA_ENTIDADES.md)
```

---

## 6. 6. Actualizar Contexto

**Actualizar `_duran/ESTADO_PROYECTO.json`:**

```json
{
  "documentacion": {
    "ultimaGeneracion": "[FECHA]",
    "archivosGenerados": [N],
    "ubicacion": "06_Documentacion",
    "tiposGenerados": ["api", "code", "readme"]
  }
}
```

---

## Output Final

```
📚 DOCUMENTACIÓN GENERADA
═══════════════════════

📂 Ubicación: 06_Documentacion/

✅ Archivos creados:
   • README.md - Documentación principal
   • INDEX.md - Índice de navegación
   • API/README_API.md - [N] endpoints documentados
   • Codigo/MiProyecto.Domain/README.md
   • Codigo/MiProyecto.API/README.md
   • Arquitectura/ARQUITECTURA.md

📊 Estadísticas:
   • endpoints documentados: [N]
   • Clases documentadas: [N]
   • Proyectos cubiertos: [N]

💡 Próximos pasos:
   • Revisar documentación generada
   • /commit -m "docs: Actualizar documentación técnica"
   • Añadir ejemplos de uso donde falten
```

---

## Actualizar Visual Studio

**Después de crear la documentación, ejecutar:**

```powershell
.\.claude\commands\integracion-vs.ps1
```

Esto añadirá los nuevos archivos de documentación a los Solution Folders de Visual Studio.

---

## Relacionados

- /analizar - Análisis de código (genera diagramas)
- /nuevo-evolutivo - Crear evolutivo de documentación
- /commit - Guardar documentación generada
