---
description: Inicia evolutivo y lo documenta INMEDIATAMENTE en _duran/specs/
argument-hint: "[--spec \"descripcion breve\"]"
---

Inicia evolutivo y lo documenta INMEDIATAMENTE en _duran/

# Inicio de Nuevo Evolutivo

> ⚠️ **CRITICAL**: Este comando DEBE escribir a los archivos INMEDIATAMENTE después de:
> 1. Leer especificaciones (si --spec)
> 2. Recibir respuesta del usuario (si sin --spec)
> 
> **NUNCA** dejar la información solo en el chat. **SIEMPRE** persistir a archivos.

## Sintaxis

```bash
/nuevo-evolutivo "CÓDIGO: Descripción breve" [--spec ruta/especificaciones.md]
```

### Códigos Soportados

| Formato | Ejemplo | Uso |
|---------|---------|-----|
| **HV-XX** | `HV-25: Filtro fechas` | Evolutivos locales (sin Jira) |
| **JIRA-XXX** | `STIC-456: Filtro fechas` | Tickets de Jira (recomendado si usas Jira) |
| **Cualquier código** | `EVT-2024-047: Mejora` | Formato libre |

> 💡 **Integración con Jira**: Si usas el código del ticket de Jira (ej: `STIC-456`),
> los commits, ramas y specs usarán ese código, facilitando la trazabilidad.
> Buscar "STIC-456" en Jira mostrará los commits relacionados.

## Parámetro Recibido
- **Argumentos**: $ARGUMENTS

Si no se proporciona nombre, solicitar al usuario antes de continuar.

---

## FLUJO DE EJECUCIÓN

### FLUJO A: Con --spec (especificaciones en archivo)

```
1. Parsear argumentos → extraer código y ruta --spec
2. Leer archivo de especificaciones
3. Extraer: endpoints, SPs, validaciones, criterios de aceptación
4. ⚠️ ESCRIBIR INMEDIATAMENTE a:
   • _duran/FUNCIONALIDADES.md
   • _duran/ESTADO_PROYECTO.json
   • _duran/specs/[CODIGO].md (si no existe, crear resumen)
5. Mostrar confirmación
6. Preguntar: "¿Comenzamos con [primera tarea]?"
```

### FLUJO B: Sin --spec (información vía chat)

```
1. Parsear argumentos → extraer código y descripción
2. ⭐ EVALUAR COMPLEJIDAD del título/descripción
3. Si parece complejo → Preguntar si tiene spec o más detalles
4. ESPERAR respuesta del usuario
5. Recopilar detalles adicionales si es necesario
6. ⚠️ ESCRIBIR INMEDIATAMENTE a:
   • _duran/FUNCIONALIDADES.md
   • _duran/ESTADO_PROYECTO.json
   • _duran/specs/[CODIGO].md (crear spec desde la conversación)
7. Mostrar confirmación
8. Preguntar: "¿Comenzamos?"
```

---

## PASO 1: Parsear Argumentos

Detectar:
- **Código**: Ej. "HV-01", "EVT-2024-047", "STIC-456"
- **Descripción**: Texto después del código
- **--spec**: Ruta al archivo de especificaciones (opcional)

---

## PASO 1.5: DETECTAR VINCULACIÓN JIRA

> **Propósito**: Detectar si el código corresponde a un ticket de Jira y mostrar recordatorios.
> **Fase**: 0 (manual) - Solo recordatorios, sin automatización.

### 1.5.1 Patrón de detección

Un código se considera **potencialmente de Jira** si:
- Formato: `LETRAS-NÚMEROS` (ej: `STIC-456`, `ACAD-123`, `RRHH-789`)
- Entre 2-10 letras mayúsculas seguidas de guión y 1-6 números
- Regex: `^[A-Z]{2,10}-[0-9]{1,6}$`

**NO se considera Jira:**
- `HV-XX` → Evolutivo local (patrón interno)
- Texto libre sin guión → Evolutivo local
- Guión bajo o espacios → Evolutivo local

### 1.5.2 Leer configuración Jira actual

```javascript
const jiraConfig = leerArchivo("_duran/ESTADO_PROYECTO.json").jira;
// {
//   habilitado: false,
//   proyectoKey: null,
//   url: null
// }
```

### 1.5.3 Lógica de decisión

```
┌─────────────────────────────────────────────────────────────┐
│  DETECTAR VINCULACIÓN JIRA                                   │
└─────────────────────────────────────────────────────────────┘

SI código es "HV-XX" (patrón interno):
    → Evolutivo LOCAL (sin Jira)
    → Continuar normal

SI código NO coincide con patrón LETRAS-NÚMEROS:
    → Evolutivo LOCAL (sin Jira)
    → Continuar normal

SI código coincide con patrón LETRAS-NÚMEROS:
    │
    ├─ SI jira.habilitado = true Y código empieza con jira.proyectoKey:
    │     → Evolutivo VINCULADO A JIRA
    │     → Guardar jiraTicket = código
    │     → Mostrar recordatorios al crear y finalizar
    │
    ├─ SI jira.habilitado = true Y código NO empieza con jira.proyectoKey:
    │     → Evolutivo LOCAL (otro proyecto, no configurado)
    │     → Continuar normal
    │
    └─ SI jira.habilitado = false:
          → PREGUNTAR al usuario (primera vez)
          → Si confirma Jira: guardar configuración
          → Si no: continuar como local
```

### 1.5.4 Preguntar configuración (solo si jira.habilitado = false)

**Mostrar SOLO si:**
- `jira.habilitado = false`
- Y código coincide con patrón Jira (no es HV-XX)

```
╔══════════════════════════════════════════════════════════════════╗
║  📋 TICKET JIRA DETECTADO                                         ║
╚══════════════════════════════════════════════════════════════════╝

El código "[CODIGO]" parece un ticket de Jira.

¿Este proyecto usa Jira para gestión de tareas?

  1. ✅ Sí, proyecto [PROYECTO_KEY] en comillas.atlassian.net (Recomendado)
  2. 🔧 Sí, pero con otra URL de Jira
  3. ❌ No, es solo un código interno (no vincular a Jira)

Opción (1/2/3):
```

Donde `[PROYECTO_KEY]` se extrae del código (ej: "STIC" de "STIC-456").

### 1.5.5 Procesar respuesta del usuario

**Si opción 1 (Jira en comillas.atlassian.net):**

```javascript
// Extraer proyectoKey del código
const proyectoKey = codigo.split('-')[0];  // "STIC" de "STIC-456"

// Actualizar ESTADO_PROYECTO.json
jiraConfig.habilitado = true;
jiraConfig.proyectoKey = proyectoKey;
jiraConfig.url = "https://comillas.atlassian.net";

guardarArchivo("_duran/ESTADO_PROYECTO.json", estado);
```

Confirmación:
```
✅ Configuración Jira guardada:
   Proyecto: [PROYECTO_KEY]
   URL: https://comillas.atlassian.net

   Próximos evolutivos [PROYECTO_KEY]-XXX se vincularán automáticamente.
```

**Si opción 2 (otra URL):**

```
Introduce la URL de tu instancia de Jira:
> https://mi-empresa.atlassian.net

✅ Configuración Jira guardada:
   Proyecto: [PROYECTO_KEY]
   URL: https://mi-empresa.atlassian.net
```

**Si opción 3 (no es Jira):**

```
✅ Entendido. El evolutivo se creará como local.
   No se volverá a preguntar para códigos similares a menos que configures Jira manualmente.
```

Nota: En este caso NO se habilita Jira, el evolutivo continúa como local.

### 1.5.6 Variable para el resto del flujo

Después de este paso, establecer:

```javascript
// Variable que se usará en pasos posteriores
const esJira = jiraConfig.habilitado &&
               codigo.startsWith(jiraConfig.proyectoKey + "-");

const jiraUrl = esJira ? `${jiraConfig.url}/browse/${codigo}` : null;
```

---

## PASO 2: Leer Estado Actual

```
Leer: _duran/ESTADO_PROYECTO.json
Leer: _duran/FUNCIONALIDADES.md
```

Obtener:
- Último ID de evolutivo usado
- Evolutivos en progreso existentes

---

## PASO 2.5: ⭐ EVALUAR COMPLEJIDAD (solo sin --spec)

> Este paso ayuda a no empezar con información incompleta.

### Indicadores de Evolutivo COMPLEJO (requiere más detalles):

| Indicador | Ejemplo | Por qué |
|-----------|---------|---------|
| **Múltiples elementos** (3+) | "Agregar campos fecha, usuario, estado..." | Cada campo puede tener tipos, nullable, defaults |
| **Palabras clave técnicas** | "API", "endpoint", "SP", "integración" | Implica contratos, parámetros, validaciones |
| **Título largo** (>60 caracteres) | "Agregar campos con fecha de creación / modificación / Usuario..." | Complejidad implícita |
| **Múltiples capas** | "DTO", "BD", "mapping", "validación" | Afecta varios niveles |
| **Ambigüedad** | "Mejorar el módulo de..." | No está claro el alcance |

### Indicadores de Evolutivo SIMPLE (puede empezar directo):

| Indicador | Ejemplo |
|-----------|---------|
| **Acción específica y clara** | "Fix typo en mensaje de error" |
| **Un solo elemento** | "Añadir campo Email a tabla Alumnos" |
| **Título corto y preciso** (<40 chars) | "Corregir validación DNI" |
| **Bug con contexto** | "NullReferenceException en GetById" |

### Acción según complejidad:

**SI ES COMPLEJO → Preguntar:**

```
📋 NUEVO EVOLUTIVO: [CÓDIGO]
"[Descripción del usuario]"

🤔 Este evolutivo parece involucrar varios elementos:
   • [elemento detectado 1]
   • [elemento detectado 2]
   • [elemento detectado 3]

Para asegurar una implementación correcta, necesito más detalles:

1. **¿Tienes un archivo de especificaciones?**
   → Indica la ruta: `_duran/specs/[CODIGO].md` o similar

2. **¿O prefieres darme los detalles aquí?** Necesitaría saber:
   • Tipos de datos de cada campo (string, DateTime, int...)
   • ¿Son nullable? ¿Tienen valores por defecto?
   • ¿Afecta solo al DTO o también a BD/Entidad?
   • ¿Hay validaciones específicas?
   • ¿Criterios de aceptación?

3. **"listo"** - Si el título ya tiene toda la información necesaria

¿Qué prefieres?
```

**SI ES SIMPLE → Confirmar y preguntar mínimo:**

```
📋 NUEVO EVOLUTIVO: [CÓDIGO]
"[Descripción del usuario]"

✅ El alcance parece claro. Solo necesito confirmar:

• **Prioridad**: 🔴 Alta | 🟠 Media | 🟡 Baja
• **¿Algún detalle adicional?** (o "listo" para comenzar)
```

**ESPERAR RESPUESTA DEL USUARIO**

---

## PASO 3A: Si tiene --spec

1. **Leer archivo de especificaciones completo**
2. **Extraer información estructurada:**

| Elemento | Buscar |
|----------|--------|
| Endpoints | URLs, rutas API, métodos HTTP |
| Stored Procedures | Nombres de SPs, parámetros |
| Validaciones | Reglas de negocio, restricciones |
| Modelo de datos | Tablas, campos, relaciones |
| Criterios de aceptación | Lista de condiciones para completar |
| Dependencias | Servicios externos, APIs |

3. **⚠️ ESCRIBIR INMEDIATAMENTE** (ir a PASO 4)

## PASO 3B: Si NO tiene --spec

> El PASO 2.5 ya habrá preguntado según la complejidad.

### Si el usuario proporcionó ruta a spec:

```
→ Leer el archivo indicado
→ Continuar como FLUJO A (PASO 3A)
```

### Si el usuario proporcionó detalles en el chat:

Extraer de la respuesta:
- Tipos de datos
- Nullable / defaults
- Capas afectadas (DTO, Entidad, BD)
- Validaciones
- Criterios de aceptación

**→ ESCRIBIR INMEDIATAMENTE** (ir a PASO 4)

### Si el usuario dijo "listo":

Usar la información del título original.
Generar criterios de aceptación básicos.

**→ ESCRIBIR INMEDIATAMENTE** (ir a PASO 4)

---

## PASO 4: ⚠️ ESCRIBIR INMEDIATAMENTE A ARCHIVOS

> **CRITICAL**: Este paso es OBLIGATORIO. No continuar sin ejecutarlo.

### 4.1 Crear/Actualizar _duran/specs/[CODIGO].md

```markdown
# [CÓDIGO]: [Nombre del evolutivo]

## Información General
- **Código**: [CÓDIGO]
- **Fecha inicio**: [FECHA_ACTUAL]
- **Estado**: 🟡 En progreso
- **Prioridad**: [🔴|🟠|🟡]

## Descripción
[Descripción detallada del evolutivo]

## Objetivo
[Qué problema resuelve / qué valor aporta]

## Especificaciones Técnicas

### Endpoints / Funcionalidades
[Lista de endpoints o funcionalidades a implementar]

### Stored Procedures
[SPs a crear o modificar]

### Validaciones de Negocio
[Reglas de validación]

### Modelo de Datos
[Tablas/campos afectados]

## Criterios de Aceptación
- [ ] Criterio 1
- [ ] Criterio 2
- [ ] Criterio 3

## Archivos Afectados (estimación)
- `03_Desarrollo/[ruta]/archivo1.cs`
- `03_Desarrollo/[ruta]/archivo2.cs`

## Notas de Implementación
[Notas técnicas, decisiones, consideraciones]

## Historial de Sesiones
- [FECHA_ACTUAL]: Inicio del evolutivo
```

### 4.2 Actualizar _duran/FUNCIONALIDADES.md

Buscar sección `## 🟡 En Progreso` (crearla si no existe).

Añadir:

```markdown
#### [CÓDIGO] [Nombre del evolutivo]
- **Estado**: 🟡 En progreso
- **Prioridad**: [🔴|🟠|🟡]
- **Inicio**: [FECHA_ACTUAL]
- **Última sesión**: [FECHA_ACTUAL]
- **Spec**: `_duran/specs/[CODIGO].md`

**Checklist:**
- [ ] Análisis completado
- [ ] Implementación backend
- [ ] Tests
- [ ] Documentación

**Próximo paso:** [primera tarea identificada]
```

### 4.3 Actualizar _duran/ESTADO_PROYECTO.json

**PRIMERO**: Actualizar el campo `evolutivoActivo` para marcar este como el activo:

```json
{
  "evolutivoActivo": "[CÓDIGO]",    ← NUEVO EVOLUTIVO ES EL ACTIVO
  ...
}
```

**SEGUNDO**: Añadir al array `evolutivos.enProgreso`:

```json
{
  "id": "[CÓDIGO]",
  "nombre": "[Nombre]",
  "inicio": "[FECHA_ACTUAL]",
  "ultimaSesion": "[FECHA_ACTUAL]",
  "progreso": 0,
  "prioridad": "[alta|media|baja]",
  "specFile": "_duran/specs/[CODIGO].md",
  "rutaCodigo": "03_Desarrollo"
}
```

Si el campo `evolutivos` no existe, crearlo:

```json
{
  "evolutivoActivo": "[CÓDIGO]",
  "evolutivos": {
    "enProgreso": [],
    "completados": []
  }
}
```

---

## PASO 5: Confirmación

Mostrar al usuario (con o sin Jira según `esJira`):

### 5.1 Confirmación SIN Jira (evolutivo local)

```
╔══════════════════════════════════════════════════════════════════╗
║                    ✅ EVOLUTIVO CREADO                            ║
╚══════════════════════════════════════════════════════════════════╝

📋 [CÓDIGO]: [Nombre]
📅 Inicio: [FECHA_ACTUAL]
🎯 Prioridad: [emoji]
📁 Código en: 03_Desarrollo/

┌─────────────────────────────────────────────────────────────────┐
│ 📝 DOCUMENTADO EN:                                              │
├─────────────────────────────────────────────────────────────────┤
│ ✅ _duran/specs/[CODIGO].md        ← Especificaciones        │
│ ✅ _duran/FUNCIONALIDADES.md       ← En sección "En Progreso"│
│ ✅ _duran/ESTADO_PROYECTO.json     ← evolutivos.enProgreso   │
└─────────────────────────────────────────────────────────────────┘

📊 Checklist: [N] tareas pendientes
🎯 Próximo paso: [primera tarea]

💡 Comandos útiles:
   • /pausar         → Al terminar la sesión
   • /estado         → Ver progreso actual
   • /continuar      → Retomar trabajo
   • /finalizar-evolutivo [CÓDIGO] → Al completar
```

### 5.2 Confirmación CON Jira (evolutivo vinculado)

```
╔══════════════════════════════════════════════════════════════════╗
║                    ✅ EVOLUTIVO CREADO                            ║
╚══════════════════════════════════════════════════════════════════╝

📋 [CÓDIGO]: [Nombre]
📅 Inicio: [FECHA_ACTUAL]
🎯 Prioridad: [emoji]
📁 Código en: 03_Desarrollo/

┌─────────────────────────────────────────────────────────────────┐
│ 📝 DOCUMENTADO EN:                                              │
├─────────────────────────────────────────────────────────────────┤
│ ✅ _duran/specs/[CODIGO].md        ← Especificaciones        │
│ ✅ _duran/FUNCIONALIDADES.md       ← En sección "En Progreso"│
│ ✅ _duran/ESTADO_PROYECTO.json     ← evolutivos.enProgreso   │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 🔗 VINCULADO A JIRA                                             │
├─────────────────────────────────────────────────────────────────┤
│ 📎 [jiraUrl]                                                    │
│                                                                 │
│ ⚠️  ACCIÓN MANUAL REQUERIDA:                                    │
│     Abre Jira y cambia el estado:  To Do → In Progress          │
└─────────────────────────────────────────────────────────────────┘

📊 Checklist: [N] tareas pendientes
🎯 Próximo paso: [primera tarea]

💡 Comandos útiles:
   • /pausar         → Al terminar la sesión
   • /estado         → Ver progreso actual
   • /continuar      → Retomar trabajo
   • /finalizar-evolutivo [CÓDIGO] → Al completar (recuerda cerrar en Jira)
```

---

## PASO 6: Siguiente Acción

**Preguntar:**

```
¿Comenzamos con: [primera tarea identificada]?

O si prefieres, indícame por dónde quieres empezar.
```

---

## ⚠️ CRITICAL REMINDERS

### 1. ESCRITURA OBLIGATORIA

**SIEMPRE** escribir a estos 3 archivos antes de mostrar confirmación:

| Archivo | Acción |
|---------|--------|
| `_duran/specs/[CODIGO].md` | CREAR con especificaciones completas |
| `_duran/FUNCIONALIDADES.md` | AÑADIR a sección "En Progreso" |
| `_duran/ESTADO_PROYECTO.json` | AÑADIR a array `evolutivos.enProgreso` |

### 2. CON --spec

```
Leer spec → Extraer info → ESCRIBIR ARCHIVOS → Confirmar
```

**NO** preguntar más detalles si el spec tiene la información.

### 3. SIN --spec

```
Preguntar detalles → ESPERAR respuesta → ESCRIBIR ARCHIVOS → Confirmar
```

**IMPORTANTE**: Después de que el usuario responda, ESCRIBIR INMEDIATAMENTE.
No hacer más preguntas antes de escribir.

### 4. NUNCA

- ❌ Dejar información solo en el chat
- ❌ Esperar a `/finalizar-evolutivo` para documentar
- ❌ Mostrar confirmación sin haber escrito a archivos
- ❌ Preguntar "¿quieres que lo documente?" - SIEMPRE documentar

### 5. VERIFICACIÓN

Antes de mostrar confirmación, verificar que los archivos existen:

```powershell
Test-Path "_duran/specs/[CODIGO].md"  # Debe ser True
```

---

## Estructura de Carpetas (referencia)

```
MiProyecto/
├── _duran/
│   ├── specs/
│   │   └── [CODIGO].md         ← SE CREA/ACTUALIZA
│   ├── FUNCIONALIDADES.md      ← SE ACTUALIZA
│   ├── ESTADO_PROYECTO.json    ← SE ACTUALIZA
│   └── ...
├── 03_Desarrollo/              ← CÓDIGO FUENTE
└── ...
```

---

## Integración con Visual Studio

Después de crear el evolutivo, ejecutar para añadir spec a Solution Folders:

```powershell
.\.claude\commands\integracion-vs.ps1
```

---

## Ejemplos de Evaluación de Complejidad

### Ejemplo 1: COMPLEJO - Múltiples campos

**Input:**
```
/nuevo-evolutivo "HV-02: Agregar campos fecha creación/modificación y usuario a CourseDTO"
```

**Claude detecta:**
- 4+ campos mencionados (fecha creación, fecha modificación, usuario creación, usuario modificación)
- Palabra clave "DTO" → puede afectar más capas
- Título >60 caracteres

**Claude pregunta:**
```
📋 NUEVO EVOLUTIVO: HV-02
"Agregar campos fecha creación/modificación y usuario a CourseDTO"

🤔 Este evolutivo parece involucrar varios elementos:
   • 4 campos nuevos (fechas y usuarios)
   • Modificación de DTO (¿también entidad/BD?)
   • Posibles valores por defecto

Para asegurar una implementación correcta:

1. ¿Tienes spec? → Indica ruta
2. ¿Detalles aquí? → Tipos, nullable, capas afectadas
3. "listo" → Si el título es suficiente
```

### Ejemplo 2: SIMPLE - Acción clara

**Input:**
```
/nuevo-evolutivo "HV-03: Fix NullReferenceException en GetCourseById"
```

**Claude detecta:**
- Acción específica (fix)
- Error concreto (NullReferenceException)
- Método específico (GetCourseById)

**Claude responde:**
```
📋 NUEVO EVOLUTIVO: HV-03
"Fix NullReferenceException en GetCourseById"

✅ El alcance está claro.

• **Prioridad**: 🔴 Alta | 🟠 Media | 🟡 Baja
• **¿Algún detalle adicional?** (o "listo" para comenzar)
```

### Ejemplo 3: COMPLEJO - API/Endpoint

**Input:**
```
/nuevo-evolutivo "EVT-050: Nuevo endpoint POST /api/becas/validar"
```

**Claude detecta:**
- Palabra clave "endpoint"
- Método HTTP específico (POST)
- No especifica: body, response, validaciones, errores

**Claude pregunta:**
```
📋 NUEVO EVOLUTIVO: EVT-050
"Nuevo endpoint POST /api/becas/validar"

🤔 Para implementar este endpoint necesito saber:
   • ¿Qué recibe en el body? (DTO de entrada)
   • ¿Qué devuelve? (DTO de salida, códigos HTTP)
   • ¿Qué validaciones aplica?
   • ¿Llama a algún SP o servicio externo?

1. ¿Tienes spec? → Indica ruta
2. ¿Detalles aquí? → Responde lo anterior
3. "listo" → Usaré convenciones estándar
```

---


---

## PASO 7: INTEGRACIÓN GIT Y TRABAJO COLABORATIVO (3 OPCIONES)

> **IMPORTANTE**: Este paso se ejecuta DESPUÉS de escribir los archivos a `_duran/`.
> Presenta 3 opciones al usuario para decidir qué hacer con el evolutivo.

### 7.0 Leer Configuración (Personal > Proyecto > Default)

```javascript
// PRIORIDAD 1: Configuración personal
let nivel = "medio"; // default
let origen = "default";

try {
  const userConfig = leerArchivo(".claude/user-config.json");
  if (userConfig?.preferencias?.nivelIntegracionGit) {
    nivel = userConfig.preferencias.nivelIntegracionGit;
    origen = "personal";
  }
} catch (e) {
  // No existe config personal, intentar proyecto
  try {
    const estado = leerArchivo("_duran/ESTADO_PROYECTO.json");
    if (estado?.configuracion?.nivelIntegracionGit) {
      nivel = estado.configuracion.nivelIntegracionGit;
      origen = "proyecto";
    }
  } catch (e2) {
    // Usar default
  }
}

// Mostrar origen de la configuración
console.log(`🌿 Nivel Git: ${nivel} (${origen})`);
```

### 7.1 Detectar Usuario Git

```bash
# Obtener usuario actual
git config user.name
```

### 7.2 PRESENTAR 3 OPCIONES AL USUARIO

> ⚠️ **OBLIGATORIO**: Siempre mostrar estas 3 opciones después de crear el evolutivo.

```
╔══════════════════════════════════════════════════════════════════════╗
║                    ✅ EVOLUTIVO CREADO                                ║
╠══════════════════════════════════════════════════════════════════════╣
║                                                                      ║
║  📋 [CÓDIGO]: [Título del evolutivo]                                 ║
║  📄 Spec: _duran/specs/[CODIGO].md                                ║
║                                                                      ║
╚══════════════════════════════════════════════════════════════════════╝

¿Qué deseas hacer?

  1. 🚀 Comenzar yo mismo
     → Asignarme el evolutivo
     → Elegir si crear rama o trabajar en la actual
     → Empezar a trabajar inmediatamente
     
  2. 👥 Asignar a otro desarrollador
     → Ver equipo disponible
     → Asignar a un compañero
     → El evolutivo quedará en su backlog
     
  3. 📋 Dejar en backlog (sin asignar)
     → Quedará como pendiente
     → Cualquiera podrá tomarlo con /tomar

Opción (1/2/3): 
```

### 7.3 OPCIÓN 1: Comenzar yo mismo

Si el usuario elige opción 1, **SIEMPRE preguntar sobre la rama** (independiente del nivel Git):

```
🚀 COMENZAR EVOLUTIVO: [CÓDIGO]

👤 Se asignará a: [usuario] (tú)
🌿 Rama actual: [rama_actual]

¿Crear rama para este evolutivo?
  s = Crear feature/[CÓDIGO] y cambiar a ella
  n = Trabajar en rama actual ([rama_actual])

(s/n): 
```

#### Si responde "s" (crear rama):

```javascript
// 1. Crear rama (Git o mostrar instrucciones TFVC)
if (esProyectoGit()) {
  ejecutar(`git checkout -b feature/${codigo}`);
}

// 2. Autoasignar el evolutivo
evolutivo.estado = "en_progreso";
evolutivo.asignadoA = usuarioGit;
evolutivo.fechaInicio = fechaHoy();
evolutivo.rama = `feature/${codigo}`;

// 3. Mover a enProgreso y establecer como activo
estado.evolutivos.enProgreso.push(evolutivo);
estado.evolutivoActivo = codigo;

// 4. Guardar
guardarArchivo("_duran/ESTADO_PROYECTO.json", estado);
```

Confirmación:
```
✅ git checkout -b feature/[CÓDIGO]
✅ Rama feature/[CÓDIGO] creada
✅ Evolutivo [CÓDIGO] asignado a: [usuario]
✅ Estado: en_progreso
✅ ESTADO_PROYECTO.json actualizado

📄 Spec: _duran/specs/[CODIGO].md

¡Listo! Puedes empezar a trabajar.
```

#### Si responde "n" (sin rama):

```javascript
// 1. Autoasignar el evolutivo SIN rama específica
evolutivo.estado = "en_progreso";
evolutivo.asignadoA = usuarioGit;
evolutivo.fechaInicio = fechaHoy();
evolutivo.rama = null;  // Sin rama específica

// 2. Mover a enProgreso y establecer como activo
estado.evolutivos.enProgreso.push(evolutivo);
estado.evolutivoActivo = codigo;

// 3. Guardar
guardarArchivo("_duran/ESTADO_PROYECTO.json", estado);
```

Confirmación:
```
✅ Evolutivo [CÓDIGO] asignado a: [usuario]
✅ Estado: en_progreso
✅ Trabajando en rama actual: [rama_actual]
✅ ESTADO_PROYECTO.json actualizado

📄 Spec: _duran/specs/[CODIGO].md

⚠️ IMPORTANTE: Sin rama específica
   Recuerda usar el prefijo [[CÓDIGO]] en tus commits para identificarlos.
   Ejemplo: git commit -m "[HV-18] feat: Añadir filtro de fechas"

¡Listo! Puedes empezar a trabajar.
```

#### Nota sobre TFVC

En proyectos TFVC (detectados por archivos .vspscc o carpeta $tf):
- Si elige "s", mostrar instrucciones manuales para crear rama en TFS
- Si elige "n", continuar normalmente sin rama

```
📋 PROYECTO TFVC DETECTADO

Para crear la rama en TFS, ejecuta manualmente:
  tf branch $/Proyecto/Main $/Proyecto/Dev/feature-[CÓDIGO]

O continúa sin rama específica (opción recomendada en TFVC).
```

### 7.4 OPCIÓN 2: Asignar a otro desarrollador

Si el usuario elige opción 2:

```
👥 ASIGNAR A OTRO DESARROLLADOR

Miembros del equipo:
┌─────────────┬──────────────────────┬─────────────────────────┐
│ Usuario     │ Nombre               │ Estado actual           │
├─────────────┼──────────────────────┼─────────────────────────┤
│ jgarcia     │ Juan García          │ 🟢 Libre                │
│ mlopez      │ María López          │ 🔵 HV-15 (en progreso)  │
│ pruiz       │ Pedro Ruiz           │ 🟢 Libre                │
│ aruiz       │ Ana Ruiz (JP)        │ -                       │
└─────────────┴──────────────────────┴─────────────────────────┘

🟢 = Libre    🔵 = Con evolutivo activo

¿A quién asignar? (@usuario o número): 
```

Si elige un usuario:

```javascript
// 1. Asignar al usuario seleccionado
evolutivo.estado = "en_progreso";
evolutivo.asignadoA = usuarioSeleccionado;
evolutivo.fechaInicio = fechaHoy();
evolutivo.rama = `feature/${codigo}`;
evolutivo.creadoPor = usuarioGit; // Quien lo creó

// 2. Mover a enProgreso
estado.evolutivos.enProgreso.push(evolutivo);

// 3. Guardar
guardarArchivo("_duran/ESTADO_PROYECTO.json", estado);
```

Confirmación:

```
✅ Evolutivo [CÓDIGO] asignado a: [usuarioDestino] ([NombreCompleto])
✅ Estado: en_progreso
✅ Rama asociada: feature/[CÓDIGO]
✅ ESTADO_PROYECTO.json actualizado

📧 Notificación para [usuarioDestino]:
   "Se te ha asignado el evolutivo [CÓDIGO]: [Título]"

💡 El desarrollador deberá ejecutar:
   /continuar [CÓDIGO]
```

### 7.5 OPCIÓN 3: Dejar en backlog (sin asignar)

Si el usuario elige opción 3:

```javascript
// 1. Crear como pendiente
evolutivo.estado = "pendiente";
evolutivo.asignadoA = null;
evolutivo.rama = null;
evolutivo.creadoPor = usuarioGit;

// 2. Añadir a pendientes
estado.evolutivos.pendientes.push(evolutivo);

// 3. Guardar
guardarArchivo("_duran/ESTADO_PROYECTO.json", estado);
```

Confirmación:

```
📋 EVOLUTIVO EN BACKLOG

✅ Evolutivo [CÓDIGO] creado como pendiente
✅ Sin asignación
✅ ESTADO_PROYECTO.json actualizado

📄 Spec disponible: _duran/specs/[CODIGO].md

💡 El evolutivo está disponible para que cualquiera lo tome:
   /tomar [CÓDIGO]

💡 O puedes asignarlo después:
   /asignar [CÓDIGO] @usuario
```

### 7.6 Resumen de Acciones por Opción

| Opción | Estado | AsignadoA | Rama | evolutivoActivo |
|--------|--------|-----------|------|-----------------|
| 1. Comenzar yo (con rama) | en_progreso | usuario actual | feature/XXX | Este código |
| 1. Comenzar yo (sin rama) | en_progreso | usuario actual | null | Este código |
| 2. Asignar otro | en_progreso | otro usuario | feature/XXX | (no se toca) |
| 3. Backlog | pendiente | null | null | (no se toca) |

### 7.7 Si el Usuario no está en el Equipo

Si el usuario Git no está registrado en `equipo.miembros`:

```
⚠️ USUARIO NO REGISTRADO EN EQUIPO

Tu usuario Git ([usuario]) no está en el equipo del proyecto.

Para registrarte, edita _duran/ESTADO_PROYECTO.json:

{
  "equipo": {
    "miembros": [
      {
        "usuario": "[tu_usuario]",
        "nombre": "[Tu Nombre]",
        "email": "[tu@email.com]",
        "rol": "desarrollador"
      }
    ]
  }
}

Mientras tanto, las opciones disponibles son:
  2. 👥 Asignar a otro desarrollador
  3. 📋 Dejar en backlog

Opción (2/3):
```

---

## PASO 8: CONFIRMACIÓN FINAL

Después de ejecutar cualquiera de las 3 opciones, mostrar resumen:

```
╔══════════════════════════════════════════════════════════════════╗
║                    ✅ PROCESO COMPLETADO                          ║
╚══════════════════════════════════════════════════════════════════╝

📋 Evolutivo: [CÓDIGO] - [Título]
📅 Creado: [FECHA]
🎯 Prioridad: [emoji] [prioridad]
👤 Asignado a: [usuario o "Sin asignar"]
🌿 Rama: [feature/XXX o "Pendiente"]
📝 Estado: [estado]

┌─────────────────────────────────────────────────────────────────┐
│ 📁 ARCHIVOS ACTUALIZADOS:                                       │
├─────────────────────────────────────────────────────────────────┤
│ ✅ _duran/specs/[CODIGO].md                                  │
│ ✅ _duran/FUNCIONALIDADES.md                                 │
│ ✅ _duran/ESTADO_PROYECTO.json                               │
└─────────────────────────────────────────────────────────────────┘

💡 Comandos útiles:
   • /equipo              → Ver estado del equipo
   • /continuar [CÓDIGO]  → Retomar evolutivo
   • /estado              → Ver progreso
```

---

## NOTAS IMPORTANTES

### Para Desarrolladores
- Si creas un evolutivo para ti mismo → Opción 1
- Si no estás seguro → Opción 3 (backlog) y luego /tomar

### Para Jefes de Proyecto / Líderes Técnicos
- Crear evolutivo y asignar directamente → Opción 2
- Crear varios evolutivos en backlog → Opción 3 repetida
- Luego asignar con → /asignar HV-XX @usuario

### Flujo Recomendado por Rol

| Rol | Flujo típico |
|-----|--------------|
| Desarrollador | Opción 1 (comenzar yo) o Opción 3 + /tomar |
| JP/LT | Opción 2 (asignar) o Opción 3 (backlog) |
| Analista | Opción 3 (backlog) para que devs tomen |
