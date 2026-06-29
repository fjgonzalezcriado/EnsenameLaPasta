---
name: user-documentation
description: >
  Generates end-user documentation: usage guides, FAQ pages, API consumer
  guides, user flow diagrams, screenshot-annotated walkthroughs, and
  Postman collection guides.
  USE FOR: user guides, FAQ pages, help documentation, user flow diagrams,
  release notes for users, guia de usuario, documentacion de uso, FAQ,
  manual usuario.
  DO NOT USE FOR: technical/developer documentation (use documentacion-tecnica),
  API documentation (use documentacion-tecnica), code generation
  (use generador-crud).
---

# Documentación de Usuario - Comillas

Este skill genera documentación funcional orientada a usuarios finales, QA y Product Owners.

---

## Activación del Skill

### Por Comando

| Comando | Acción |
|---------|--------|
| `/documentar-uso` | Activa el skill (auto-detecta tipo de proyecto) |
| `/documentar-uso api` | Fuerza documentación de API REST |
| `/documentar-uso web` | Fuerza documentación de aplicación web |
| `/documentar-uso servicio` | Fuerza documentación de servicio Windows |
| `/documentar-uso cli` | Fuerza documentación de aplicación de consola |

### Por Contexto (Automático)

Claude activa este skill automáticamente cuando detecta:
- Usuario menciona "guía de uso", "manual de usuario", "documentar uso"
- Se pide generar ejemplos curl o Postman
- Se solicitan flujos de usuario o documentación funcional
- Se pregunta "cómo usar" la API o aplicación

---

## Cuándo Usar Este Skill

| Situación | Este Skill | documentacion-tecnica |
|-----------|------------|----------------------|
| Manual de usuario | ✅ | ❌ |
| Guía de API para consumidores | ✅ | ❌ |
| Arquitectura y código | ❌ | ✅ |
| README técnico | ❌ | ✅ |
| Ejemplos curl/Postman | ✅ | ❌ |
| ADRs | ❌ | ✅ |
| Flujos de usuario | ✅ | ❌ |

---

## Detección Automática de Tipo de Proyecto

| Tipo proyecto | Indicadores | Documentación generada |
|---------------|-------------|------------------------|
| **API REST** | Controllers con `[ApiController]`, Swagger | Endpoints, params, ejemplos curl/Postman |
| **Web MVC/Razor** | Views, Razor Pages, Controllers MVC | Pantallas, flujos, formularios |
| **Servicio Windows** | ServiceBase, Worker Service | Config, parámetros, logs |
| **Librería/NuGet** | Sin punto de entrada, solo clases | Métodos públicos, quick start |
| **Consola** | Program.cs con args | Argumentos, flags, ejemplos CLI |
| **Blazor** | Components .razor | Componentes, interacciones |

### Cómo Detectar el Tipo

```
1. Buscar Controllers con [ApiController] → API REST
2. Buscar carpeta Views/ o Pages/ → Web MVC/Razor
3. Buscar archivos .razor → Blazor
4. Buscar ServiceBase o IHostedService → Servicio
5. Buscar Program.cs con args parsing → Consola
6. Si solo hay clases públicas → Librería
```

---

## Plantillas por Tipo de Proyecto

### Guía de Consumidor API
> Plantilla completa en `references/api-consumer-guide.md`
> Incluye: autenticación, endpoints, códigos de estado, flujos comunes, paginación, contacto.

**Ubicación de salida:** `_duran/guias-uso/GUIA_USO_API.md`

### Guía de Usuario Web (MVC/Razor)
> Plantilla completa en `references/user-guide-template.md`
> Incluye: acceso, pantallas con formularios, flujos de usuario, FAQ, soporte.

**Ubicación de salida:** `_duran/guias-uso/GUIA_USO_WEB.md`

### Guía de Operaciones (Servicio/Worker)
> Plantilla completa en `references/operations-guide.md`
> Incluye: instalación, configuración appsettings, logs, monitorización, troubleshooting.

**Ubicación de salida:** `_duran/guias-uso/GUIA_USO_SERVICIO.md`

### Guía de Aplicación CLI
> Plantilla completa en `references/cli-guide.md`
> Incluye: uso básico, comandos, opciones globales, códigos de salida, ejemplos, colección Postman.

**Ubicación de salida:** `_duran/guias-uso/GUIA_USO_CLI.md`

---

## Extracción de Información

### De Controllers API

```csharp
// Buscar atributos
[HttpGet]           → Método GET
[HttpPost]          → Método POST
[Route("api/...")]  → Ruta
[Authorize]         → Requiere auth
[FromQuery]         → Parámetro query
[FromBody]          → Parámetro body
[FromRoute]         → Parámetro ruta
[Required]          → Campo requerido
[MaxLength(n)]      → Longitud máxima
```

### De Formularios Web

```csharp
// En ViewModels/DTOs
[Required]          → Campo obligatorio
[EmailAddress]      → Formato email
[Range(min, max)]   → Rango numérico
[StringLength(max)] → Longitud texto
[Display(Name="X")] → Label mostrado
```

---

## Checklist de Documentación de Usuario

### API
- [ ] Autenticación documentada
- [ ] Todos los endpoints listados
- [ ] Parámetros con ejemplos
- [ ] Códigos de error explicados
- [ ] Ejemplos curl generados
- [ ] Flujos principales documentados

### Web
- [ ] URL de acceso
- [ ] Roles y permisos
- [ ] Pantallas principales
- [ ] Formularios con validaciones
- [ ] Flujos de usuario
- [ ] FAQ con errores comunes

### Servicio
- [ ] Instrucciones de instalación
- [ ] Configuración documentada
- [ ] Ubicación de logs
- [ ] Troubleshooting básico

### CLI
- [ ] Todos los comandos
- [ ] Opciones y argumentos
- [ ] Ejemplos de uso
- [ ] Códigos de salida

---

## Integración con /documentar-uso

Este skill se activa automáticamente con el comando `/documentar-uso`:

```bash
/documentar-uso           # Auto-detecta tipo y genera guía
/documentar-uso api       # Fuerza documentación de API
/documentar-uso web       # Fuerza documentación web
/documentar-uso servicio  # Fuerza documentación de servicio
/documentar-uso cli       # Fuerza documentación CLI
```

---

*Skill user-documentation v3.7.0*
