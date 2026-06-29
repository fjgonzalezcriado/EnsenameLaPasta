Genera documentación funcional orientada a usuarios finales, QA y Product Owners

Genera documentación funcional orientada a usuarios finales, QA y Product Owners.

---

## Uso

```bash
/documentar-uso [tipo]

Tipos (opcional, auto-detectado si no se especifica):
  api       - Documentación de API REST
  web       - Documentación de aplicación web
  servicio  - Documentación de servicio Windows/Worker
  cli       - Documentación de aplicación de consola
```

---

## Diferencia con /documentar

| Aspecto | `/documentar` (Técnica) | `/documentar-uso` (Funcional) |
|---------|-------------------------|-------------------------------|
| **Audiencia** | Desarrolladores | Usuarios, QA, POs, Soporte |
| **Contenido** | Arquitectura, código, clases | Pantallas, endpoints, ejemplos |
| **Enfoque** | Cómo está construido | Cómo se utiliza |
| **Output** | SPEC_*.md, README.md | GUIA_USO_*.md |

---

## Instrucciones para Claude

### PASO 1: Detectar tipo de proyecto

```
1. Buscar Controllers con [ApiController] → API REST
2. Buscar carpeta Views/ o Pages/ → Web MVC/Razor
3. Buscar archivos .razor → Blazor (tratar como Web)
4. Buscar ServiceBase o IHostedService → Servicio
5. Buscar Program.cs con args parsing → Consola/CLI
6. Si solo hay clases públicas → Librería (usar CLI format)
```

Si el usuario especifica tipo, usar ese; si no, detectar automáticamente.

### PASO 2: Recopilar información

#### Para API REST:
- Buscar todos los Controllers con `[ApiController]`
- Extraer atributos: `[HttpGet]`, `[HttpPost]`, `[Route]`, `[Authorize]`
- Analizar parámetros: `[FromQuery]`, `[FromBody]`, `[FromRoute]`
- Buscar validaciones: `[Required]`, `[MaxLength]`, etc.
- Leer XML comments si existen
- Verificar si hay Swagger/OpenAPI configurado

#### Para Web MVC/Razor:
- Listar Views/Pages principales
- Analizar ViewModels/DTOs de formularios
- Extraer validaciones de DataAnnotations
- Identificar rutas desde Controllers MVC
- Mapear flujos de navegación

#### Para Servicio:
- Leer appsettings.json para configuración
- Identificar Worker/BackgroundService
- Buscar configuración de logging
- Documentar health checks si existen

#### Para CLI:
- Analizar parsing de argumentos (args, CommandLineParser, etc.)
- Listar comandos y subcomandos
- Extraer opciones/flags
- Documentar códigos de salida

### PASO 3: Generar documentación

Crear carpeta y archivos:

```
_duran/guias-uso/
├── GUIA_USO_[TIPO].md      # Documento principal
├── ejemplos-curl.sh         # Solo para API
└── ejemplos-postman.json    # Solo para API
```

### PASO 4: Usar plantillas del skill

Consultar el skill `user-documentation` para:
- Plantillas específicas por tipo de proyecto
- Formato de tablas de parámetros
- Estructura de ejemplos curl/Postman
- Checklist de completitud

---

## Output esperado

### Para API REST

```
📘 DOCUMENTACIÓN DE USO - API
═══════════════════════════════════════════════════

🔍 Analizando proyecto...

Tipo detectado: API REST
Controllers encontrados: 5
Endpoints totales: 23

📁 Archivos generados:
├── _duran/guias-uso/GUIA_USO_API.md
├── _duran/guias-uso/ejemplos-curl.sh
└── _duran/guias-uso/ejemplos-postman.json

📋 Contenido generado:
✅ Autenticación documentada
✅ 23 endpoints documentados
✅ Ejemplos curl para cada endpoint
✅ Colección Postman importable
✅ Códigos de error explicados
✅ 3 flujos principales documentados

💡 Revisa el archivo generado y ajusta:
   - Descripciones de endpoints
   - Ejemplos de valores reales
   - Flujos específicos de tu negocio
```

### Para Web MVC

```
📘 DOCUMENTACIÓN DE USO - WEB
═══════════════════════════════════════════════════

🔍 Analizando proyecto...

Tipo detectado: Web MVC/Razor
Pantallas encontradas: 12
Formularios: 8

📁 Archivos generados:
└── _duran/guias-uso/GUIA_USO_WEB.md

📋 Contenido generado:
✅ URL y acceso documentados
✅ 12 pantallas documentadas
✅ 8 formularios con validaciones
✅ Flujos de usuario principales
✅ Errores comunes y soluciones
✅ FAQ generado

💡 Revisa el archivo generado y ajusta:
   - Capturas de pantalla (opcional)
   - Roles y permisos específicos
   - FAQ con preguntas reales de usuarios
```

### Para Servicio

```
📘 DOCUMENTACIÓN DE USO - SERVICIO
═══════════════════════════════════════════════════

🔍 Analizando proyecto...

Tipo detectado: Worker Service (.NET)
Workers encontrados: 2

📁 Archivos generados:
└── _duran/guias-uso/GUIA_USO_SERVICIO.md

📋 Contenido generado:
✅ Instrucciones de instalación
✅ Configuración documentada (15 parámetros)
✅ Ubicación de logs
✅ Health checks
✅ Troubleshooting básico

💡 Revisa el archivo generado y ajusta:
   - Rutas de instalación reales
   - Valores de configuración de producción
   - Troubleshooting específico
```

### Para CLI

```
📘 DOCUMENTACIÓN DE USO - CLI
═══════════════════════════════════════════════════

🔍 Analizando proyecto...

Tipo detectado: Aplicación de Consola
Comandos encontrados: 5

📁 Archivos generados:
└── _duran/guias-uso/GUIA_USO_CLI.md

📋 Contenido generado:
✅ Uso básico documentado
✅ 5 comandos con opciones
✅ Ejemplos de uso
✅ Códigos de salida
✅ Opciones globales

💡 Revisa el archivo generado y ajusta:
   - Ejemplos con datos reales
   - Escenarios de uso comunes
```

---

## Skills Asociados

Este comando activa automáticamente el skill **user-documentation** que proporciona:

- Plantillas de documentación por tipo de proyecto
- Formato estándar de tablas
- Estructura de ejemplos curl/Postman
- Checklists de completitud

---

## Actualización de contexto

Después de generar la documentación, actualizar:

1. **FUNCIONALIDADES.md** - Añadir referencia a la guía de uso
2. **ESTADO_PROYECTO.json** - Registrar que existe documentación de usuario

---

## Notas

- La documentación generada es un **punto de partida**
- Requiere revisión humana para:
  - Descripciones de negocio específicas
  - Ejemplos con datos reales
  - Flujos de usuario adicionales
  - FAQ basado en preguntas reales
- Se integra con el sistema de contexto existente
- Complementa (no reemplaza) a `/documentar`

---

*Comando documentar-uso v3.7.0*
