Crear nuevo proyecto .NET desde cero con wizard interactivo (estándares Comillas)

# Crear Nuevo Proyecto

> 🏗️ **OBJETIVO**: Crear un proyecto .NET nuevo siguiendo los estándares STIC de Comillas,
> con nomenclatura `Comillas.NombreProyecto.*` y estructura en `03_Desarrollo/`.

## Modos de Uso

| Modo | Comando | Descripción |
|------|---------|-------------|
| **Wizard** | `/nuevo-proyecto` | Interactivo paso a paso (recomendado) |
| **Rápido** | `/nuevo-proyecto MiApi --quick` | Con valores por defecto |
| **Descripción** | `/nuevo-proyecto "Sistema de gestión de becas"` | Inferir configuración |

---

## 📋 REGLAS DE NOMENCLATURA (CLAUDE_BASE_COMILLAS)

### Convención de nombres

```
Comillas.<NombreProyecto>.<Capa>

Ejemplos:
├── Comillas.GestionBecas.sln    ← o .slnx en .NET 8+
├── Comillas.GestionBecas.Api
├── Comillas.GestionBecas.Web
├── Comillas.GestionBecas.Application
├── Comillas.GestionBecas.Domain
├── Comillas.GestionBecas.Infrastructure
├── Comillas.GestionBecas.Worker
└── Comillas.GestionBecas.Tests
```

### Formatos de Solución (.sln vs .slnx)

| Formato | Compatibilidad | Descripción |
|---------|----------------|-------------|
| **.sln** | .NET 4.x, 7, 8, 9, 10 | Formato tradicional, compatibilidad máxima |
| **.slnx** | .NET 8+ (VS 2022 17.10+) | Formato XML limpio, recomendado para nuevos proyectos |

```
┌─────────────────────────────────────────────────────────────────┐
│ MATRIZ DE COMPATIBILIDAD - FORMATO DE SOLUCIÓN                  │
├─────────────────────────────────────────────────────────────────┤
│ .NET 4.x  │ .NET 7  │ .NET 8  │ .NET 9  │ .NET 10 │ Formato    │
├───────────┼─────────┼─────────┼─────────┼─────────┼────────────┤
│    ✅     │   ✅    │   ✅    │   ✅    │   ✅    │ .sln       │
│    ❌     │   ❌    │   ✅    │   ✅    │   ✅    │ .slnx      │
└─────────────────────────────────────────────────────────────────┘
```

**Ventajas de .slnx:**
- Formato XML legible y editable manualmente
- Sin GUIDs complejos ni secciones difíciles de entender
- Mejor integración con control de versiones (menos conflictos)
- Estructura declarativa más limpia

### Detección inteligente del nombre

| Usuario escribe | Se interpreta como | Resultado |
|-----------------|-------------------|-----------|
| `GestionBecas` | Nombre sin prefijo | `Comillas.GestionBecas.*` |
| `Comillas.GestionBecas` | Ya tiene prefijo | `Comillas.GestionBecas.*` |
| `comillas.gestionbecas` | Minúsculas | `Comillas.GestionBecas.*` |
| `COMILLAS.GESTIONBECAS` | Mayúsculas | `Comillas.GestionBecas.*` |

---

## 📁 ESTRUCTURA DE ARCHIVOS CLAUDE.md

### Jerarquía de CLAUDE.md

```
MiProyecto/                          ← Raíz del proyecto STIC
├── CLAUDE.md                        ← 🔵 PRINCIPAL (generado por plantilla)
│   │                                   Referencias:
│   │                                   @.claude/CLAUDE_BASE_COMILLAS.md
│   │                                   @_duran/ESTADO_PROYECTO.json
│   │
├── .claude/
│   └── CLAUDE_BASE_COMILLAS.md      ← Estándares base Comillas
│
├── _duran/                       ← Estado del proyecto
│
└── 03_Desarrollo/
    └── Comillas.GestionBecas/       ← Carpeta de la solución
        └── CLAUDE.md                ← 🟢 ESPECÍFICO de la solución (opcional)
                                        Solo si hay configuración específica
                                        que no aplica al proyecto general
```

### ¿Cuándo crear CLAUDE.md en la carpeta de la solución?

| Escenario | ¿Crear CLAUDE.md en 03_Desarrollo/? |
|-----------|-------------------------------------|
| Proyecto nuevo con `/nuevo-proyecto` | ❌ No necesario (usa el principal) |
| Proyecto existente migrado | ✅ Sí, para documentar particularidades |
| Múltiples soluciones en 03_Desarrollo | ✅ Sí, uno por cada solución |
| Configuración específica de build | ✅ Sí, para comandos específicos |

### Contenido del CLAUDE.md específico (si se crea)

```markdown
# CLAUDE.md - Comillas.GestionBecas

> Este archivo complementa el CLAUDE.md principal del proyecto.
> Para estándares generales, ver: @../CLAUDE.md

## Configuración específica de esta solución
[Particularidades de build, deploy, etc.]

## Referencias
@../../CLAUDE.md
@../../.claude/CLAUDE_BASE_COMILLAS.md
```

---

## 🧙‍♂️ MODO WIZARD (Interactivo)

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║                                                                               ║
║     🏗️  CREAR NUEVO PROYECTO - WIZARD INTERACTIVO                            ║
║                                                                               ║
║     Universidad Pontificia Comillas - STIC                                    ║
║     Siguiendo estándares de CLAUDE_BASE_COMILLAS                              ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝
```

---

### 🚨 INSTRUCCIONES OBLIGATORIAS PARA CLAUDE

**FLUJO SECUENCIAL OBLIGATORIO - NO SALTAR PASOS:**

```
PASO 1  → Nombre y descripción del proyecto
PASO 1b → Contexto del proyecto (PREGUNTAR OPCIÓN, ESPERAR RESPUESTA)
PASO 2  → Tipos de aplicación
PASO 2b → Configuración Web (solo si eligió Web MVC)
PASO 3  → Base de datos
PASO 4  → Autenticación
PASO 5  → Arquitectura
PASO 6  → Integraciones
PASO 6b → Formato de solución (solo si .NET 8+)
PASO 7  → Testing
PASO 8  → Resumen y confirmación
```

**REGLAS CRÍTICAS:**
1. **CADA PASO** debe mostrarse y esperar respuesta del usuario
2. **NO ASUMIR** opciones por defecto - SIEMPRE preguntar
3. **NO SALTAR** al siguiente paso sin completar el actual
4. En **PASO 1b**: MOSTRAR las 4 opciones y ESPERAR elección
5. Si usuario elige **Documento/Spec**: PEDIR el documento ANTES de continuar

---

### 📝 PASO 1: Información Básica

```
▶ PASO 1/8: INFORMACIÓN BÁSICA
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📌 Nombre del proyecto (sin prefijo "Comillas.", se añade automáticamente):
   > _____________
   
   Ejemplos: GestionBecas, PortalAlumnos, SincronizadorHR
   
   ⚠️ Si escribes "Comillas.GestionBecas", se detectará automáticamente
      y no se duplicará el prefijo.

   Resultado: Comillas.[nombre].sln

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📋 Descripción funcional del proyecto (2-3 líneas):
   > _____________________________________________________________
   > _____________________________________________________________
   > _____________________________________________________________
   
   Ejemplo: 
   "Sistema para gestionar las solicitudes de becas de estudiantes.
   Permite a los alumnos solicitar becas y al personal de Comillas
   revisar, aprobar o rechazar las solicitudes."

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📁 Ubicación (siempre en 03_Desarrollo/):
   
   Se creará en: 03_Desarrollo/Comillas.[nombre]/
   
   [1] ✅ Confirmar ubicación
   [2] 📂 Ya tengo la carpeta creada (especificar ruta)

   > _
```

---

### 📋 PASO 1b: Contexto del Proyecto (Recomendado)

> **🚨 OBLIGATORIO PARA CLAUDE**: Este paso DEBE ejecutarse SIEMPRE después del Paso 1.
> Claude DEBE preguntar al usuario cómo quiere proporcionar el contexto y ESPERAR su respuesta.
> NO saltar este paso. NO asumir una opción. PREGUNTAR y ESPERAR.

**CLAUDE DEBE MOSTRAR ESTO Y ESPERAR RESPUESTA:**

```
▶ PASO 1b/8: CONTEXTO DEL PROYECTO
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

💡 Cuanta más información proporciones, mejor podré ayudarte durante
   todo el ciclo de vida del proyecto.

¿Cómo quieres proporcionar el contexto del proyecto?

   [1] 📝 Descripción guiada (te hago preguntas estructuradas)
   [2] 📄 Documento/Spec (pegar contenido o indicar ruta de archivo)
   [3] 💬 Conversación libre (cuéntame y yo extraigo la información)
   [4] ⏭️  Saltar (configurar después con /onboarding)

   > _
```

**🚨 CLAUDE: ESPERA la respuesta del usuario antes de continuar. NO asumas ninguna opción.**

---

#### Opción [1]: Descripción Guiada

**SOLO si el usuario elige explícitamente la opción 1**, Claude hace las siguientes preguntas:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📋 CONTEXTO FUNCIONAL
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1️⃣ PROPÓSITO Y ALCANCE

   ¿Cuál es el objetivo principal del sistema?
   > _____________________________________________________________
   > _____________________________________________________________

   ¿Qué problema resuelve?
   > _____________________________________________________________

   ¿Qué queda FUERA del alcance? (igual de importante)
   > _____________________________________________________________

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

2️⃣ USUARIOS Y ROLES

   ¿Quiénes usarán el sistema? (describir cada rol)

   Rol 1: _________________
   - Qué puede hacer: ________________________________________
   - Ejemplo: Administrador - gestiona usuarios y configuración

   Rol 2: _________________
   - Qué puede hacer: ________________________________________

   Rol 3: _________________
   - Qué puede hacer: ________________________________________

   (añadir más si es necesario)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

3️⃣ ENTIDADES PRINCIPALES

   ¿Cuáles son las entidades/conceptos principales del dominio?

   Entidad 1: _________________
   - Atributos clave: ________________________________________
   - Relaciones: _____________________________________________

   Entidad 2: _________________
   - Atributos clave: ________________________________________
   - Relaciones: _____________________________________________

   Entidad 3: _________________
   - Atributos clave: ________________________________________
   - Relaciones: _____________________________________________

   Ejemplo para "Gestor de Licencias":
   - Licencia: tipo, proveedor, fecha_expiracion, coste, estado
   - Solicitud: solicitante, licencia_solicitada, justificacion, estado
   - Usuario: nombre, departamento, rol, licencias_asignadas

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

4️⃣ FLUJOS DE NEGOCIO PRINCIPALES

   Describe los flujos/procesos principales (happy path):

   Flujo 1: _________________
   Pasos:
   1. ________________________________________________________
   2. ________________________________________________________
   3. ________________________________________________________

   Flujo 2: _________________
   Pasos:
   1. ________________________________________________________
   2. ________________________________________________________

   Ejemplo para "Gestor de Licencias":
   Flujo: Solicitud de licencia
   1. Usuario solicita licencia indicando tipo y justificación
   2. Jefe de departamento recibe notificación y aprueba/rechaza
   3. Si aprobada, Administrador asigna licencia disponible
   4. Usuario recibe notificación con datos de acceso

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

5️⃣ REGLAS DE NEGOCIO ESPECIALES

   ¿Hay reglas o validaciones importantes que el sistema debe cumplir?

   - Regla 1: ________________________________________________
   - Regla 2: ________________________________________________
   - Regla 3: ________________________________________________

   Ejemplo:
   - Una licencia no puede asignarse si está expirada
   - Un usuario no puede tener más de 3 solicitudes pendientes
   - Las licencias de más de 500€ requieren aprobación de dirección

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

6️⃣ INTEGRACIONES CONOCIDAS

   ¿Con qué sistemas externos debe integrarse?

   [ ] Banner (académico)
   [ ] Oracle HCM (RRHH)
   [ ] SAP
   [ ] Active Directory / Azure AD
   [ ] Correo electrónico (notificaciones)
   [ ] SharePoint
   [ ] Otro: _________________________________________________

   Detalles de integración (si los conoces):
   > _____________________________________________________________

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

7️⃣ REQUISITOS NO FUNCIONALES

   Rendimiento:
   [ ] Usuarios concurrentes esperados: _______
   [ ] Tiempo de respuesta crítico: _______
   [ ] Volumen de datos esperado: _______

   Disponibilidad:
   [ ] Horario de uso: 24/7 | Horario laboral | Otro: _______
   [ ] Criticidad: Alta | Media | Baja

   Seguridad adicional:
   [ ] Datos sensibles (RGPD): Sí | No
   [ ] Auditoría de acciones: Sí | No
   [ ] Cifrado especial: _______

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

8️⃣ INFORMACIÓN ADICIONAL

   ¿Hay algo más que deba saber sobre el proyecto?
   (contexto histórico, restricciones, preferencias técnicas, etc.)

   > _____________________________________________________________
   > _____________________________________________________________
   > _____________________________________________________________
```

---

#### Opción [2]: Documento/Spec

**🚨 SOLO si el usuario elige explícitamente la opción 2, Claude DEBE:**
1. Mostrar las sub-opciones (A, B, C)
2. ESPERAR a que el usuario elija
3. Pedir el documento/ruta/URL según la elección
4. NO continuar al siguiente paso hasta recibir y procesar el documento

**CLAUDE DEBE MOSTRAR ESTO:**

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📄 IMPORTAR DOCUMENTO DE ESPECIFICACIÓN
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

¿Cómo quieres proporcionar el documento?

   [A] 📋 Pegar el contenido aquí (texto, markdown, etc.)
   [B] 📁 Indicar ruta de archivo (Word, PDF, MD, TXT)
   [C] 🔗 URL (Confluence, SharePoint, etc.)

   > _
```

**🚨 CLAUDE: ESPERA la respuesta del usuario. NO continúes sin ella.**

**Si elige [A] Pegar contenido - CLAUDE DEBE DECIR:**
```
   Pega el contenido del documento a continuación.
   Cuando termines de pegar, escribe "FIN" en una línea sola.

   > [Esperando contenido del usuario...]
```

**🚨 CLAUDE: ESPERA a que el usuario pegue el contenido y escriba FIN.**

**Si elige [B] Ruta de archivo - CLAUDE DEBE DECIR:**
```
   Indica la ruta del archivo:
   > _____________________________________________________________

   Formatos soportados: .docx, .pdf, .md, .txt, .html
```

**🚨 CLAUDE: ESPERA la ruta, luego LEE el archivo con la herramienta Read.**

**Si elige [C] URL - CLAUDE DEBE DECIR:**
```
   Indica la URL del documento:
   > _____________________________________________________________
```

**🚨 CLAUDE: ESPERA la URL, luego usa WebFetch para obtener el contenido.**

**DESPUÉS de recibir el documento, Claude DEBE:**
1. Analizar el contenido
2. Extraer: entidades, roles, flujos, integraciones, requisitos
3. Mostrar resumen para confirmación
4. SOLO ENTONCES continuar al paso 2

Claude debe analizar el documento y extraer:
- Entidades mencionadas
- Roles de usuario
- Flujos de negocio
- Integraciones
- Requisitos técnicos

Y mostrar un resumen para confirmación:
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📊 RESUMEN EXTRAÍDO DEL DOCUMENTO
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

He identificado la siguiente información:

ENTIDADES DETECTADAS:
  • Licencia (tipo, proveedor, coste, estado)
  • Solicitud (solicitante, justificación, estado)
  • Usuario (nombre, departamento, rol)

ROLES DETECTADOS:
  • Administrador - gestión completa
  • Jefe Departamento - aprobación solicitudes
  • Usuario - solicitar licencias

FLUJOS DETECTADOS:
  • Solicitud de licencia nueva
  • Renovación de licencia
  • Baja de licencia

INTEGRACIONES DETECTADAS:
  • Active Directory (autenticación)
  • Correo (notificaciones)

¿Es correcta esta interpretación?
   [S] Sí, continuar
   [M] Modificar/añadir información
   [R] Reintentar con otro documento

   > _
```

---

#### Opción [3]: Conversación Libre

Si el usuario elige la opción 3:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
💬 CUÉNTAME SOBRE EL PROYECTO
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Descríbeme el proyecto con tus propias palabras.
Cuéntame qué necesitas, quién lo usará, qué debe hacer el sistema...

No te preocupes por el formato, yo extraeré la información relevante.

> _____________________________________________________________
> _____________________________________________________________
> _____________________________________________________________
> _____________________________________________________________
> _____________________________________________________________

(Escribe todo lo que consideres relevante. Cuando termines,
escribe "LISTO" en una línea sola)
```

Después de que el usuario escriba, Claude analiza y muestra:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔍 ANÁLISIS DE TU DESCRIPCIÓN
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

De tu descripción he entendido:

PROPÓSITO:
  [Resumen del objetivo del sistema]

USUARIOS/ROLES:
  • [Rol 1]: [descripción]
  • [Rol 2]: [descripción]

ENTIDADES PRINCIPALES:
  • [Entidad 1]: [atributos identificados]
  • [Entidad 2]: [atributos identificados]

FLUJOS CLAVE:
  1. [Flujo 1]
  2. [Flujo 2]

INTEGRACIONES:
  • [Sistema 1]
  • [Sistema 2]

DUDAS QUE TENGO:
  ❓ [Pregunta 1 para clarificar]
  ❓ [Pregunta 2 para clarificar]

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

¿Quieres responder mis dudas o continuar con esta información?
   [R] Responder dudas
   [C] Continuar (está bien así)
   [A] Añadir más información

   > _
```

---

#### Uso del Contexto Capturado

El contexto capturado en el Paso 1b se utiliza para:

1. **Pre-seleccionar opciones** en pasos siguientes:
   - Si menciona "API para móviles" → sugiere API REST
   - Si menciona "dashboard interno" → sugiere Web MVC
   - Si menciona "proceso nocturno" → sugiere Worker Service

2. **Generar estructura de dominio** inicial:
   - Crear carpetas por entidad en Domain/
   - Generar clases base de entidades
   - Crear DTOs iniciales

3. **Configurar _duran/** con información rica:
   - FUNCIONALIDADES.md pre-poblado
   - DEPENDENCIAS.md con integraciones
   - Specs iniciales en _duran/specs/

4. **Mejorar el CLAUDE.md** generado:
   - Sección de dominio con entidades
   - Reglas de negocio documentadas
   - Flujos principales descritos

5. **Asistencia durante desarrollo**:
   - Claude conoce el contexto completo
   - Puede sugerir implementaciones coherentes
   - Detecta inconsistencias con los requisitos

---

### 📦 PASO 2: Tipos de Aplicación

```
▶ PASO 2/8: TIPOS DE APLICACIÓN
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📦 ¿Qué tipo(s) de aplicación tendrá la solución?
   (Puedes seleccionar VARIOS con coma: 1,2,5)

   [1] 🌐 API REST                                   ← Más común
       Comillas.[nombre].Api
       
   [2] 🖥️  Web MVC/Razor
       Comillas.[nombre].Web
       
   [3] 📱 Blazor Server/WASM
       Comillas.[nombre].Blazor
       
   [4] ⚙️  Aplicación de Consola
       Comillas.[nombre].Console
       
   [5] 🔄 Worker Service (jobs, sincronizaciones)
       Comillas.[nombre].Worker
       
   [6] 📚 Library (paquete NuGet)
       Comillas.[nombre].Core

   > _

   💡 Ejemplo: "1,5" crea API + Worker en la misma solución

   Cada tipo seleccionado generará su proyecto con nomenclatura:
   Comillas.[nombre].[Tipo]
```

---

### 🎨 PASO 2b: Configuración Web (Solo si eligió Web MVC/Razor)

> **NOTA**: Este paso solo se muestra si el usuario seleccionó la opción [2] Web MVC/Razor en el paso anterior.

```
▶ PASO 2b/8: CONFIGURACIÓN WEB
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🎨 ¿Generar layout con estilos Comillas?

   [S] ✅ Sí - Genera _Layout.cshtml con colores corporativos
       Incluye:
       ├── Views/Shared/_Layout.cshtml (estructura Comillas)
       ├── wwwroot/css/comillas.css (variables de color)
       ├── Navbar responsive con logo Comillas
       ├── Footer estándar institucional
       └── Cumplimiento WCAG 2.1 AA

   [N] ❌ No - Layout básico de plantilla .NET
       Se puede añadir después con el skill webapp-layout

   > _
```

**Si el usuario elige [S] (Sí):**

Claude debe **activar el skill `webapp-layout`** para generar los archivos con estilos Comillas.

El skill generará:

```
src/Comillas.[nombre].Web/
├── Views/
│   └── Shared/
│       ├── _Layout.cshtml          ← Layout principal Comillas
│       ├── _LayoutFooter.cshtml    ← Footer institucional
│       └── _LayoutNav.cshtml       ← Navbar responsive
├── wwwroot/
│   ├── css/
│   │   └── comillas.css            ← Variables CSS corporativas
│   └── images/
│       └── logo-comillas.svg       ← Placeholder para logo
└── appsettings.json                ← Configuración de branding
```

**Colores corporativos incluidos:**
```css
:root {
    --comillas-azul: #003366;
    --comillas-azul-claro: #0066cc;
    --comillas-azul-hover: #004d99;
    --comillas-gris: #666666;
    --comillas-gris-claro: #f5f5f5;
}
```

---

### 🗄️ PASO 3: Base de Datos

```
▶ PASO 3/8: BASE DE DATOS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🗄️ ¿Qué base de datos utilizará?

   [1] 🔷 SQL Server on-premise Comillas            ← Más común
   [2] ☁️  Azure SQL Database
   [3] 🐘 PostgreSQL
   [4] 🍃 MongoDB
   [5] 🔥 CosmosDB (Azure)
   [6] ❌ Sin base de datos

   > _
```

**Si selecciona opción 1-5 (con BD):**

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🔐 ¿Cómo se gestionarán las credenciales de BD?

   [A] 🔐 Azure Key Vault (recomendado para producción)
   [B] 📝 Connection string en appsettings (desarrollo)
   [C] 🔄 Ambos (Key Vault en PRO, appsettings en DEV)

   > _
```

**Si selecciona [A] Azure Key Vault:**

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🔐 CONFIGURACIÓN DE AZURE KEY VAULT

   Nombre del Key Vault (sin .vault.azure.net):
   > _____________
   
   Ejemplos: kv-comillas-dev, kv-comillas-pro, kv-gestionbecas
   
   URL resultante: https://[nombre].vault.azure.net/

   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

   Nombre del secreto para connection string:
   > _____________
   
   Ejemplo: ConnectionStrings--DefaultConnection
   
   💡 El secreto debe existir en Key Vault o se creará después
```

**Si selecciona [B] o [C] Connection string:**

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📝 CONNECTION STRING (para desarrollo)

   Servidor de BD:
   > _____________
   
   Ejemplos Comillas: DVABORON, DVABORONDES, DVADESA01
   
   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

   Nombre de la base de datos:
   > _____________
   
   Se usará: Comillas_[nombre] (ejemplo: Comillas_GestionBecas)
   ¿Usar este nombre? [S/N]: _
   
   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

   Tipo de autenticación SQL:
   [1] Windows Authentication (Trusted_Connection=True)  ← Recomendado
   [2] SQL Authentication (usuario/password)
   
   > _
```

---

### 🔐 PASO 4: Autenticación (Azure AD)

```
▶ PASO 4/8: AUTENTICACIÓN
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🔐 ¿Qué tipo de autenticación necesita?

   [1] 🔷 Azure AD (Entra ID) - SSO Comillas        ← 99% de los casos
   [2] 🔷 Azure AD + Roles desde BD (híbrido)
   [3] 🎫 JWT Bearer (sin Azure AD)
   [4] 🔓 Sin autenticación

   > _
```

**Si selecciona [1] o [2] Azure AD:**

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🔷 CONFIGURACIÓN DE AZURE AD (Entra ID)

   ⚠️ Necesitarás crear App Registrations en Azure Portal
      para cada tipo de aplicación (API, Web, etc.)

   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

   📋 DATOS DEL TENANT (comunes a todas las apps):
   
   Tenant ID (Directory ID):
   > ________-____-____-____-____________
   
   Ejemplo: 12345678-1234-1234-1234-123456789012
   
   Domain:
   > _____________
   
   Ejemplo: comillas.edu, upcomillas.onmicrosoft.com
```

**Para cada tipo de aplicación seleccionado (API, Web, etc.):**

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🌐 APP REGISTRATION: Comillas.GestionBecas.Api

   ¿Ya tienes el App Registration creado?
   [S] Sí, tengo los datos
   [N] No, lo crearé después (dejar placeholders)
   
   > _
```

**Si responde [S]:**

```
   Client ID (Application ID) para API:
   > ________-____-____-____-____________
   
   Client Secret (si aplica, o dejar vacío para Managed Identity):
   > _____________
   
   Scopes/Audience:
   > api://Comillas.GestionBecas/.default
   
   💡 Se recomienda usar Managed Identity en Azure en lugar de secrets
```

**Si responde [N]:**

```
   ✅ Se generarán placeholders en appsettings.json
   
   📋 PASOS PARA CREAR APP REGISTRATION:
   
   1. Ir a Azure Portal > Azure Active Directory
   2. App Registrations > New Registration
   3. Nombre: Comillas.GestionBecas.Api
   4. Supported account types: Single tenant
   5. Redirect URI: (según tipo de app)
      - API: No necesita
      - Web: https://localhost:xxxx/signin-oidc
   6. Copiar Application (client) ID
   7. Certificates & secrets > New client secret
```

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🖥️ APP REGISTRATION: Comillas.GestionBecas.Web

   Client ID (Application ID) para Web:
   > ________-____-____-____-____________
   
   Client Secret:
   > _____________
   
   Callback Path:
   > /signin-oidc      (default)
```

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

👥 ¿Quiénes serán los usuarios?

   [A] 👨‍🎓 Comunidad Comillas (PAS/PDI/Alumnos)
   [B] 👨‍💼 Solo empleados (PAS/PDI)
   [C] 🔧 Solo técnicos STIC
   [D] 🌍 Externos (partners, proveedores)
   [E] 🤖 Aplicación/Servicio (sin usuarios humanos)

   > _

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🛡️ ¿Autorización basada en roles?

   [1] ✅ Roles desde grupos de Azure AD
   [2] ✅ Roles desde base de datos
   [3] ✅ Híbrido (AD + BD)
   [4] ❌ Sin roles

   > _
```

**Si selecciona roles desde Azure AD:**

```
   📋 GRUPOS DE AZURE AD PARA ROLES:
   
   Group ID para Administradores:
   > ________-____-____-____-____________
   (Dejar vacío si no tienes el ID aún)
   
   Group ID para Usuarios:
   > ________-____-____-____-____________
   
   💡 Puedes configurar estos IDs después en appsettings.json
```

---

### 🏗️ PASO 5: Arquitectura y Patrones

> **NOTA**: Ver documentación completa en `Documentos_Base/01_Estructura_Tecnica/GUIA_ARQUITECTURA.md`

```
▶ PASO 5/8: ARQUITECTURA Y PATRONES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🏗️ Selecciona el PERFIL que mejor se adapte a tu proyecto:

┌─────────────────────────────────────────────────────────────────┐
│  [1] 📦 BÁSICO - Services + Repository                          │
│      Para: CRUD, gestores simples, prototipos, MVPs             │
│      Capas: Api → Services → Repository → BD                    │
│      Complejidad: ⭐ (4-6 archivos/entidad)                      │
│                                                                 │
│      Proyectos:                                                 │
│      ├── Comillas.[nombre].Api                                  │
│      ├── Comillas.[nombre].Core                                 │
│      └── Comillas.[nombre].Tests                                │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  [2] 🧅 ESTÁNDAR - Clean Architecture + Mediator    ← RECOMENDADO │
│      Para: APIs REST, aplicaciones de negocio típicas           │
│      Capas: Api → Application (Handlers) → Domain → Infra       │
│      Complejidad: ⭐⭐ (8-12 archivos/entidad)                    │
│      Incluye: Mediator Pattern + Repository + FluentValidation  │
│                                                                 │
│      Proyectos:                                                 │
│      ├── Comillas.[nombre].Api                                  │
│      ├── Comillas.[nombre].Application                          │
│      ├── Comillas.[nombre].Domain                               │
│      ├── Comillas.[nombre].Infrastructure                       │
│      └── Comillas.[nombre].Tests                                │
│                                                                 │
│      ⚠️ Usa Mediator (MIT) en lugar de MediatR (comercial v12+) │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  [3] 🔷 AVANZADO - Clean Architecture + CQRS + Mediator         │
│      Para: Dominios complejos, alta escala, event sourcing      │
│      Capas: Api → Commands/Queries → Domain → Infra             │
│      Complejidad: ⭐⭐⭐ (15-20 archivos/entidad)                  │
│      Incluye: CQRS + Mediator + Repository + Domain Events      │
│                                                                 │
│      Proyectos:                                                 │
│      ├── Comillas.[nombre].Api                                  │
│      ├── Comillas.[nombre].Application                          │
│      │   ├── Commands/                                          │
│      │   └── Queries/                                           │
│      ├── Comillas.[nombre].Domain                               │
│      ├── Comillas.[nombre].Infrastructure                       │
│      └── Comillas.[nombre].Tests                                │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  [4] ⚙️  PERSONALIZADO - Elegir componentes manualmente          │
│      Para: Requisitos específicos, migraciones, legacy          │
└─────────────────────────────────────────────────────────────────┘

   > _
```

**Si selecciona [4] PERSONALIZADO:**

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⚙️ CONFIGURACIÓN PERSONALIZADA
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. ARQUITECTURA BASE:
   [A] 🧅 Clean Architecture (4 capas)
   [B] 📁 N-Capas Tradicional (3 capas)
   [C] 🍰 Vertical Slices (por features)
   [D] 📦 Minimal (1 proyecto)
   [E] 🔷 Hexagonal (Ports & Adapters)

   > _

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

2. PATRÓN DE COMUNICACIÓN:
   [1] 📞 Inyección directa (Services)        ← Más simple
   [2] 📨 Mediator Pattern (Handlers)         ← Desacoplado

   > _

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

3. PATRÓN DE DATOS:
   [a] 🔄 Repository + Unit of Work           ← Recomendado
   [b] 🔄 Repository simple
   [c] 💾 DbContext directo (no recomendado para proyectos grandes)

   > _

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

4. SEPARACIÓN LECTURA/ESCRITURA:
   [S] Sin separación (mismo modelo)          ← Más simple
   [C] CQRS (modelos separados Read/Write)

   > _

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

5. PATRONES ADICIONALES (seleccionar con coma, o N para ninguno):
   [1] 📋 Specification Pattern
   [2] 📢 Domain Events
   [3] 📤 Outbox Pattern (para eventos distribuidos)
   [N] Ninguno adicional

   > _
```

**GUÍA DE SELECCIÓN RÁPIDA:**

```
┌─────────────────────────────────────────────────────────────────────┐
│                    GUÍA DE SELECCIÓN DE PERFIL                      │
├─────────────────────┬───────────┬───────────┬───────────────────────┤
│     Criterio        │  BÁSICO   │ ESTÁNDAR  │      AVANZADO         │
├─────────────────────┼───────────┼───────────┼───────────────────────┤
│ Operaciones CRUD    │   >80%    │  50-80%   │       <50%            │
│ Lógica de negocio   │   Poca    │  Moderada │      Compleja         │
│ Tamaño equipo       │   1-2     │    2-5    │        5+             │
│ Duración proyecto   │  <6 meses │ 6-18 meses│     >18 meses         │
│ Integraciones       │   0-2     │    2-5    │        5+             │
│ Testing requerido   │  Básico   │   Medio   │     Exhaustivo        │
│ Experiencia equipo  │  Junior   │   Mixto   │      Senior           │
├─────────────────────┼───────────┼───────────┼───────────────────────┤
│ Setup inicial       │  1 hora   │  2-3 horas│     4-8 horas         │
│ Curva aprendizaje   │  1 día    │  3-5 días │    1-2 semanas        │
└─────────────────────┴───────────┴───────────┴───────────────────────┘
```

---

### ☁️ PASO 6: Integraciones

```
▶ PASO 6/8: INTEGRACIONES Y SERVICIOS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

☁️ Servicios de Azure (seleccionar con coma, o N para ninguno):

   ALMACENAMIENTO:
   [1] 📦 Azure Blob Storage
   [2] 📁 Azure File Share
   
   MENSAJERÍA:
   [3] 📬 Azure Service Bus
   [4] 📡 Azure SignalR
   
   MONITORIZACIÓN:
   [5] 🔍 Application Insights                      ← Recomendado
   
   CACHÉ:
   [6] 🚀 Azure Redis Cache
   
   IA:
   [7] 🤖 Azure OpenAI

   [N] ❌ Ninguno

   > _
```

**Si selecciona Blob Storage:**

```
   📦 AZURE BLOB STORAGE
   
   Nombre de la Storage Account:
   > _____________
   
   Nombre del contenedor principal:
   > documentos      (default)
```

**Si selecciona Application Insights:**

```
   🔍 APPLICATION INSIGHTS
   
   ¿Tienes el Connection String de App Insights?
   [S] Sí
   [N] No, lo configuraré después
   
   > _
```

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🔗 Integraciones con sistemas Comillas (seleccionar con coma):

   [A] 📚 Banner (ERP académico)
   [B] 👥 Oracle HCM Cloud (RRHH)
   [C] 🎓 Sigma (gestión académica legacy)
   [D] 📧 Microsoft 365 / Exchange
   [E] 📂 SharePoint
   [N] ❌ Sin integraciones Comillas

   > _
```

---

### 📄 PASO 6b: Formato de Solución (Solo .NET 8+)

> **NOTA**: Este paso solo se muestra si el proyecto usa .NET 8 o superior.

```
▶ PASO 6b/8: FORMATO DE SOLUCIÓN
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📄 ¿Qué formato de solución desea usar?

   [1] 📦 .sln (Formato tradicional)                  ← Compatibilidad máxima
       • Compatible con todas las versiones de .NET
       • Formato usado históricamente

   [2] 📋 .slnx (Nuevo formato XML)                   ← Recomendado para nuevos proyectos
       • Solo compatible con .NET 8+ (VS 2022 17.10+)
       • Formato XML limpio y legible
       • Sin GUIDs complejos
       • Menos conflictos en control de versiones

   > _

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 MATRIZ DE COMPATIBILIDAD

┌─────────────────────────────────────────────────────────────────┐
│ .NET 4.x  │ .NET 7  │ .NET 8  │ .NET 9  │ .NET 10 │ Formato    │
├───────────┼─────────┼─────────┼─────────┼─────────┼────────────┤
│    ✅     │   ✅    │   ✅    │   ✅    │   ✅    │ .sln       │
│    ❌     │   ❌    │   ✅    │   ✅    │   ✅    │ .slnx      │
└─────────────────────────────────────────────────────────────────┘

💡 Si el equipo usa VS 2022 17.10+ y no necesita compatibilidad
   con .NET 7 o anterior, se recomienda .slnx.
```

---

### 🧪 PASO 7: Testing

```
▶ PASO 7/8: CONFIGURACIÓN DE TESTS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🧪 ¿Qué proyectos de test crear?

   [1] ✅ Tests unitarios (Comillas.[nombre].UnitTests)
   [2] ✅ Tests de integración (Comillas.[nombre].IntegrationTests)
   [3] ✅ Ambos                                      ← Recomendado
   [4] ❌ Sin tests (no recomendado)

   > _

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📚 Framework de testing:

   [1] xUnit                                        ← Recomendado
   [2] NUnit
   [3] MSTest

   > _
```

---

### 📋 PASO 8: Resumen y Confirmación

```
▶ PASO 8/8: RESUMEN Y CONFIRMACIÓN
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📦 CONFIGURACIÓN DEL PROYECTO
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  📌 Nombre:        Comillas.GestionBecas                        │
│  📋 Descripción:   Sistema para gestionar las solicitudes de    │
│                    becas de estudiantes de Comillas             │
│  📁 Ubicación:     03_Desarrollo/Comillas.GestionBecas/         │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  📦 PROYECTOS A CREAR                                           │
│  ├── Comillas.GestionBecas.sln (o .slnx si NET 8+)             │
│  ├── Comillas.GestionBecas.Api                                 │
│  ├── Comillas.GestionBecas.Web                                 │
│  ├── Comillas.GestionBecas.Application                         │
│  ├── Comillas.GestionBecas.Domain                              │
│  ├── Comillas.GestionBecas.Infrastructure                      │
│  ├── Comillas.GestionBecas.UnitTests                           │
│  └── Comillas.GestionBecas.IntegrationTests                    │
├─────────────────────────────────────────────────────────────────┤
│  🗄️ BASE DE DATOS                                               │
│  ├── Motor:        SQL Server                                  │
│  ├── Servidor:     DVABORONDES                                 │
│  ├── Base datos:   Comillas_GestionBecas                       │
│  ├── Auth:         Windows Authentication                      │
│  └── Secretos:     Azure Key Vault (kv-comillas-dev)           │
├─────────────────────────────────────────────────────────────────┤
│  🔐 AZURE AD                                                    │
│  ├── Tenant ID:    12345678-1234-1234-1234-123456789012        │
│  ├── Domain:       comillas.edu                                │
│  ├── API Client:   xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx        │
│  ├── Web Client:   xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx        │
│  └── Roles:        Desde grupos Azure AD                       │
├─────────────────────────────────────────────────────────────────┤
│  🎨 DISEÑO WEB (si aplica)                                      │
│  ├── Layout Comillas: ✅ Sí / ❌ No                              │
│  ├── _Layout.cshtml con colores corporativos                   │
│  ├── comillas.css con variables                                │
│  └── WCAG 2.1 AA: ✅                                            │
├─────────────────────────────────────────────────────────────────┤
│  ☁️ INTEGRACIONES                                               │
│  ├── Azure Key Vault: kv-comillas-dev                          │
│  ├── Application Insights: ✅                                   │
│  ├── Banner: ✅                                                 │
│  └── Oracle HCM: ✅                                             │
└─────────────────────────────────────────────────────────────────┘

📁 ESTRUCTURA A CREAR EN 03_Desarrollo/

Comillas.GestionBecas/
├── Comillas.GestionBecas.sln
├── src/
│   ├── Comillas.GestionBecas.Api/
│   ├── Comillas.GestionBecas.Web/
│   ├── Comillas.GestionBecas.Application/
│   ├── Comillas.GestionBecas.Domain/
│   └── Comillas.GestionBecas.Infrastructure/
├── tests/
│   ├── Comillas.GestionBecas.UnitTests/
│   └── Comillas.GestionBecas.IntegrationTests/
└── README.md

⚠️ NOTA: NO se crea CLAUDE.md aquí.
   Se usa el CLAUDE.md principal de la raíz del proyecto.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

⚠️ ¿Confirmar creación del proyecto?

   [S] ✅ Sí, crear proyecto
   [M] 📝 Modificar algún paso
   [G] 💾 Guardar configuración como plantilla
   [C] ❌ Cancelar

   > _
```

---

## 🚀 EJECUCIÓN DE CREACIÓN

### Fase 1: Crear Estructura

```powershell
# Variables del wizard
$nombreBase = "GestionBecas"
$nombreCompleto = "Comillas.$nombreBase"

# Crear en 03_Desarrollo
cd 03_Desarrollo
mkdir $nombreCompleto
cd $nombreCompleto

# Crear solución (usar .slnx si el usuario lo seleccionó y es .NET 8+)
# Formato por defecto: .sln (compatibilidad máxima)
$formatoSolucion = "sln"  # Cambiar a "slnx" si usuario seleccionó .slnx

if ($formatoSolucion -eq "slnx") {
    # .slnx requiere creación manual o conversión posterior
    # Crear .sln primero y convertir
    dotnet new sln -n $nombreCompleto
    Write-Host "💡 Para convertir a .slnx: Abrir en VS 2022 17.10+ → Guardar como → .slnx"
} else {
    dotnet new sln -n $nombreCompleto
}

# Crear proyectos con nomenclatura Comillas.*
dotnet new webapi -n "$nombreCompleto.Api" -o "src/$nombreCompleto.Api"
dotnet new webapp -n "$nombreCompleto.Web" -o "src/$nombreCompleto.Web"
dotnet new classlib -n "$nombreCompleto.Application" -o "src/$nombreCompleto.Application"
dotnet new classlib -n "$nombreCompleto.Domain" -o "src/$nombreCompleto.Domain"
dotnet new classlib -n "$nombreCompleto.Infrastructure" -o "src/$nombreCompleto.Infrastructure"
dotnet new xunit -n "$nombreCompleto.UnitTests" -o "tests/$nombreCompleto.UnitTests"
dotnet new xunit -n "$nombreCompleto.IntegrationTests" -o "tests/$nombreCompleto.IntegrationTests"

# Añadir a solución
dotnet sln add (Get-ChildItem -Recurse *.csproj)
```

### Fase 2: Generar appsettings.json (con valores del wizard)

**appsettings.json generado automáticamente:**

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "comillas.edu",
    "TenantId": "12345678-1234-1234-1234-123456789012",
    "ClientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "ClientSecret": "",
    "Scopes": "api://Comillas.GestionBecas/.default",
    "CallbackPath": "/signin-oidc",
    "Roles": {
      "AdminGroupId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
      "UserGroupId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=DVABORONDES;Database=Comillas_GestionBecas;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "KeyVault": {
    "VaultUri": "https://kv-comillas-dev.vault.azure.net/",
    "SecretNames": {
      "DbConnectionString": "ConnectionStrings--DefaultConnection"
    }
  },
  "ApplicationInsights": {
    "ConnectionString": ""
  },
  "BlobStorage": {
    "AccountName": "",
    "ContainerName": "documentos"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "ApplicationInsights" }
    ]
  },
  "AllowedHosts": "*"
}
```

**appsettings.Development.json:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=DVABORONDES;Database=Comillas_GestionBecas;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "KeyVault": {
    "Enabled": false
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

### Fase 3: Actualizar CLAUDE.md principal

Añadir al CLAUDE.md principal del proyecto:

```markdown
## Soluciones en 03_Desarrollo/

### Comillas.GestionBecas

**Descripción:** Sistema para gestionar las solicitudes de becas de estudiantes.

**Estructura:**
- `Comillas.GestionBecas.Api` - API REST principal
- `Comillas.GestionBecas.Web` - Portal web
- `Comillas.GestionBecas.Application` - Casos de uso
- `Comillas.GestionBecas.Domain` - Entidades de dominio
- `Comillas.GestionBecas.Infrastructure` - EF Core, Azure

**Configuración:**
- Azure AD: Tenant comillas.edu
- BD: SQL Server (DVABORONDES)
- Key Vault: kv-comillas-dev
```

### Fase 4: Actualizar _duran/ESTADO_PROYECTO.json

```json
{
  "proyecto": {
    "nombre": "Comillas.GestionBecas",
    "version": "0.1.0",
    "estado": "en_desarrollo",
    "creado": "2026-01-27",
    "descripcion": "Sistema para gestionar las solicitudes de becas de estudiantes"
  },
  "soluciones": [
    {
      "nombre": "Comillas.GestionBecas",
      "ubicacion": "03_Desarrollo/Comillas.GestionBecas/",
      "proyectos": [
        "Comillas.GestionBecas.Api",
        "Comillas.GestionBecas.Web",
        "Comillas.GestionBecas.Application",
        "Comillas.GestionBecas.Domain",
        "Comillas.GestionBecas.Infrastructure"
      ]
    }
  ],
  "tecnologias": {
    "framework": ".NET 9.0",
    "lenguaje": "C# 13",
    "base_datos": "SQL Server",
    "autenticacion": "Azure AD (Entra ID)",
    "orm": "Entity Framework Core 9.0"
  },
  "azure": {
    "tenant_id": "12345678-1234-1234-1234-123456789012",
    "key_vault": "kv-comillas-dev",
    "app_insights": true
  }
}
```

---

## 📄 ARCHIVOS GENERADOS AUTOMÁTICAMENTE

| Archivo | Contenido |
|---------|-----------|
| `appsettings.json` | ✅ Con todos los valores del wizard |
| `appsettings.Development.json` | ✅ Configuración de desarrollo |
| `Program.cs` | ✅ Con Azure AD, Serilog, DI configurado |
| `DependencyInjection.cs` | ✅ Por cada capa |
| `README.md` | ✅ Documentación del proyecto |
| `CLAUDE.md` principal | ✅ Actualizado con nueva solución |
| `_duran/ESTADO_PROYECTO.json` | ✅ Actualizado |

---

## 📊 RESUMEN FINAL

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║  ✅ PROYECTO CREADO EXITOSAMENTE                                              ║
╚═══════════════════════════════════════════════════════════════════════════════╝

📦 Comillas.GestionBecas
├── Ubicación: 03_Desarrollo/Comillas.GestionBecas/
├── Proyectos: 7 (5 src + 2 tests)
├── Arquitectura: Clean Architecture + CQRS
└── Auth: Azure AD (Entra ID)

📄 ARCHIVOS CONFIGURADOS
✅ appsettings.json (con valores del wizard)
✅ appsettings.Development.json
✅ Program.cs (Azure AD + Serilog + DI)
✅ CLAUDE.md principal actualizado
✅ _duran/ESTADO_PROYECTO.json actualizado

⚠️ PENDIENTE DE CONFIGURAR
□ Crear App Registrations en Azure Portal (si no existen)
□ Crear base de datos Comillas_GestionBecas
□ Configurar secretos en Key Vault
□ Verificar permisos en Azure AD

🚀 PRÓXIMOS PASOS
1. /analizar                    ← Análisis inicial
2. /nuevo-evolutivo "HV-01"     ← Primera funcionalidad
3. dotnet run                   ← Ejecutar

📧 Soporte: soporte.stic@comillas.edu
```

---

## 💾 PLANTILLAS PREDEFINIDAS

```bash
/nuevo-proyecto --template api-comillas      # API estándar Comillas
/nuevo-proyecto --template fullstack         # API + Web + Worker
/nuevo-proyecto --template microservice      # Microservicio minimal
/nuevo-proyecto --template sync-service      # Worker de sincronización
```
