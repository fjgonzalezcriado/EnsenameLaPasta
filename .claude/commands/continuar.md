Retoma trabajo cargando spec completo y estado desde _duran/

# Continuación de Trabajo

> Este comando carga TODO el contexto necesario para retomar donde lo dejaste:
> - Spec completo del evolutivo activo (o el especificado)
> - Estado de la última sesión
> - Checklist y progreso
> - Próximo paso guardado

## Sintaxis

```bash
# Si hay un solo evolutivo en progreso → lo retoma automáticamente
/continuar

# Si hay múltiples evolutivos → especificar cuál retomar
/continuar HV-01

# También sirve para CAMBIAR de evolutivo activo
/continuar HV-02    # Cambia de HV-01 a HV-02 sin pausar formalmente
```

## Parámetro Recibido
- **Argumentos: $ARGUMENTS (opcional - código del evolutivo)**

## Contexto
Nueva sesión de Claude Code. Cargar contexto completo desde `_duran/` y preparar para continuar.

---

## FLUJO DE EJECUCIÓN

```
1. Parsear argumentos → ¿se especificó código?
2. Leer ESTADO_PROYECTO.json → evolutivos en progreso
3. DETERMINAR evolutivo a retomar:
   a. Si se especificó código → usar ese
   b. Si hay un solo evolutivo → usar ese
   c. Si hay múltiples → PREGUNTAR cuál
4. ACTUALIZAR evolutivoActivo en JSON
5. Leer specs/[CODIGO].md → especificaciones COMPLETAS
6. Leer FUNCIONALIDADES.md → estado general
7. Verificar cambios externos (git status)
8. Mostrar resumen completo
9. Preguntar: "¿Continuamos con [próximo paso]?"
```

---

## PASO 1: Parsear Argumentos

```
Si $ARGUMENTS contiene código (ej: "HV-01"):
   → codigoEspecificado = "HV-01"
Si no:
   → codigoEspecificado = null
```

---

## PASO 2: Cargar Estado del Proyecto

```
Leer: _duran/ESTADO_PROYECTO.json
```

Extraer:
```json
{
  "evolutivoActivo": "HV-02",
  "ultimaSesion": {
    "fecha": "...",
    "evolutivoActivo": "[CÓDIGO]",
    "proximoPaso": "...",
    "resumenSesion": "...",
    "archivosModificados": [...],
    "notas": "..."
  },
  "evolutivos": {
    "enProgreso": [
      { "id": "HV-01", ... },
      { "id": "HV-02", ... }
    ]
  }
}
```

---

## PASO 3: ⭐ DETERMINAR EVOLUTIVO A RETOMAR

### Caso A: Se especificó código

```
Si codigoEspecificado != null:
   Verificar que existe en evolutivos.enProgreso
   Si existe → evolutivoARetomar = codigoEspecificado
   Si no existe → ERROR: "Evolutivo [CÓDIGO] no encontrado en progreso"
```

### Caso B: Un solo evolutivo en progreso

```
Si evolutivos.enProgreso.length == 1:
   evolutivoARetomar = evolutivos.enProgreso[0].id
   Retomando único evolutivo en progreso: {0}
```

### Caso C: Múltiples evolutivos en progreso

```
Si evolutivos.enProgreso.length > 1 Y codigoEspecificado == null:
   PREGUNTAR al usuario:

   ╔═══════════════════════════════════════════════════════════╗
   ║              📋 MÚLTIPLES EVOLUTIVOS EN PROGRESO                ║
   ╚═══════════════════════════════════════════════════════════╝

   Hay {0} evolutivos en progreso. ¿Cuál quieres retomar?

   → HV-01: Validación de DNI en formulario
     Última sesión: {0} | Progreso: {1}% | Próximo paso: {0}

     HV-02: Agregar campos fecha/usuario a CourseDTO
     Última sesión: {0} | Progreso: {1}% | Próximo paso: {0}

   Indica el código (ej: HV-01) o escribe el número:

   ESPERAR RESPUESTA DEL USUARIO
```

### Caso D: Sin evolutivos en progreso

```
Si evolutivos.enProgreso.length == 0:
   → Ir a sección "Sin Evolutivo Activo"
```

---

## PASO 4: ⚠️ ACTUALIZAR evolutivoActivo

> **CRITICAL**: CRITICAL: Antes de continuar, actualizar el JSON para reflejar cuál es el evolutivo activo.

```json
// Actualizar _duran/ESTADO_PROYECTO.json
{
  "evolutivoActivo": "[CÓDIGO_SELECCIONADO]",    ← ACTUALIZAR
  ...
}
```

Esto permite que `/estado` y `/pausar` sepan cuál es el evolutivo activo actual.

---

## PASO 4.5: 🌿 VERIFICAR RAMA GIT

> Detectar si el usuario está en la rama correcta para el evolutivo.

### Detectar Rama Actual

```bash
git branch --show-current
```

### Obtener Rama del Evolutivo

```javascript
const evolutivo = estado.evolutivos.enProgreso.find(e => e.codigo === codigoSeleccionado);
const ramaEvolutivo = evolutivo?.rama;  // Ej: "feature/HV-05"
const ramaActual = ejecutar("git branch --show-current").trim();  // Ej: "main"
```

### Si las Ramas No Coinciden

```javascript
const nivel = estado.configuracion?.nivelIntegracionGit || "medio";
```

#### Nivel BÁSICO:

```
🌿 NOTA SOBRE RAMA GIT

Evolutivo: {0}
Rama del evolutivo: {0}
Tu rama actual: {0}

💡 SUGERENCIA:
┌──────────────────────────────────────────────────────────┐
│ Para cambiar a la rama del evolutivo:          │
│                                                          │
│   git checkout feature/HV-05                             │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

#### Nivel MEDIO (Recomendado):

```
🌿 RAMA DIFERENTE DETECTADA

Evolutivo: {0}
Rama del evolutivo: {0}
Tu rama actual: {0}

¿Cambiar a {0}? (s/n)
```

Si responde "s":
```
✅ Ejecutando: git checkout {0}
✅ Cambiado a rama {0}
```

Si responde "n":
```
⚠️ Continuando en rama {0}

Nota: Los cambios que hagas estarán en {0}, no en {1}
```

#### Nivel ALTO:

```
🤖 CAMBIO AUTOMÁTICO DE RAMA

✅ Detectado: estabas en {0}
✅ Ejecutando: git checkout {0}
✅ Cambiado a rama {0}
```

### Si No Existe la Rama

Si el evolutivo tiene rama asignada pero no existe localmente:

```
🌿 RAMA NO EXISTE LOCALMENTE

Evolutivo: {0}
Rama esperada: {0}

Opciones:
1. Crear rama local: git checkout -b {0}
2. Buscar en remoto: git fetch && git checkout {0}
3. Continuar sin cambiar de rama

¿Qué deseas hacer? (1/2/3)
```

### Si el Evolutivo No Tiene Rama Asignada

Si el evolutivo fue creado antes de la integración Git:

```
ℹ️ SIN RAMA ASIGNADA

El evolutivo {0} no tiene rama Git asignada.

¿Quieres asignar una rama ahora?
1. Crear feature/{0} (recomendado)
2. Usar rama actual ({0})
3. Continuar sin rama

Opción (1/2/3):
```

---

## PASO 5: ⚠️ CARGAR SPEC COMPLETO DEL EVOLUTIVO

> **CRITICAL**: CRITICAL: Leer el spec completo para tener todo el contexto.

```
Leer: _duran/specs/[CÓDIGO].md
```

Extraer:
- **Descripción completa del evolutivo**
- **Objetivo que se busca lograr**
- **Especificaciones técnicas: endpoints, SPs, validaciones**
- **Criterios de aceptación: checklist con estado**
- **Archivos afectados: estimación inicial**
- **Historial de sesiones: todo lo trabajado anteriormente**
- **Notas de implementación: decisiones tomadas**

---

## PASO 6: Verificar Cambios Externos

```powershell
# Ver si hay cambios desde la última sesión
git status --short

# Ver commits recientes (por si alguien más trabajó)
git log --oneline -5
```

---

## PASO 7: Mostrar Resumen Completo

```
╔═══════════════════════════════════════════════════════════════╗
║                    🔄 RETOMANDO SESIÓN                            ║
╚═══════════════════════════════════════════════════════════════╝

📅 Última sesión: {0}
📁 Código en: 03_Desarrollo/

┌──────────────────────────────────────────────────────────────────┐
│ 📋 EVOLUTIVO ACTIVO                                             │
├──────────────────────────────────────────────────────────────────┤
│ [CÓDIGO]: [Nombre del evolutivo]                                │
│                                                                  │
│ 📊 Progreso: {0}% ({1}/{2} criterios)               │
│ 🎯 Prioridad: {0}                                        │
│ 📄 Spec: _duran/specs/{0}.md                           │
└──────────────────────────────────────────────────────────────────┘

📝 DESCRIPCIÓN:
   [Descripción del evolutivo desde el spec]

🎯 OBJETIVO:
   [Objetivo que se busca lograr]

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📋 CRITERIOS DE ACEPTACIÓN:
━━━━━━━━━━━━━━━━━━━━━━

✅ Completados::
   • [Criterio 1]
   • [Criterio 2]

⏳ Pendientes::
   • [Criterio 3]
   • [Criterio 4]

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📂 ARCHIVOS DE LA ÚLTIMA SESIÓN:
   • 03_Desarrollo/[Proyecto]/archivo1.cs
   • 03_Desarrollo/[Proyecto]/archivo2.cs

📝 RESUMEN ÚLTIMA SESIÓN:
   "[Lo que se logró en la sesión anterior]"

🎯 PRÓXIMO PASO GUARDADO:
   "[El próximo paso que se guardó con /pausar]"

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📜 HISTORIAL DE SESIONES:
   • [fecha1]: [nota1]
   • [fecha2]: [nota2]
   • [fecha3]: [nota3]
```

**Si hay otros evolutivos en progreso (mostrar al final):**

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📋 OTROS EVOLUTIVOS EN PROGRESO ({0}):
   • HV-01: Validación de DNI... (60%)
   • HV-03: Mejora de rendimiento... (20%)

💡 Para cambiar: /continuar {0}
```

---

## PASO 8: Verificar Cambios Externos

**Si hay cambios no commiteados:**
```
⚠️ CAMBIOS DETECTADOS DESDE LA ÚLTIMA SESIÓN:

 M 03_Desarrollo/[Proyecto]/archivo1.cs
 M 03_Desarrollo/[Proyecto]/archivo2.cs

💡 Estos archivos tienen cambios pendientes de commit.
```

**Si hubo commits de otros:**
```
ℹ️ COMMITS RECIENTES (por si alguien más trabajó):

abc1234 - feat: añadido endpoint X (Juan, hace 2 días)
def5678 - fix: corregido bug Y (María, hace 3 días)
```

---

## PASO 9: ¿CÓMO CONTINUAMOS?

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🚀 ¿CÓMO CONTINUAMOS?

El próximo paso guardado es:
   "[próximo paso]"

Opciones:
1. Continuar con ese paso
2. Indicar otro punto de partida
3. Ver especificaciones técnicas completas del spec
4. Ver estado general del proyecto (/estado)

¿Qué prefieres?
```

---

## CASOS ESPECIALES

### NO HAY EVOLUTIVO ACTIVO

```
ℹ️ NO HAY EVOLUTIVO ACTIVO

Última sesión: {0}
Notas guardadas: "{0}"

💡 Opciones:
   • /nuevo-evolutivo "CÓDIGO: Descripción" → Iniciar nuevo evolutivo
   • /estado → Ver estado general del proyecto
   • /analizar → Analizar el proyecto
```

### EVOLUTIVO SIN SPEC

```
⚠️ EVOLUTIVO SIN SPEC

El evolutivo {0} está en progreso pero no tiene archivo spec.

Creando spec desde la información disponible...
[Crear _duran/specs/[CODIGO].md con la info de FUNCIONALIDADES.md]

✅ Spec creado: _duran/specs/{0}.md
```

### ¡BIENVENIDO AL PROYECTO!

```
👋 ¡BIENVENIDO AL PROYECTO!

No hay sesiones anteriores registradas.

💡 Comandos sugeridos:
   • /analizar → Analizar estructura del proyecto
   • /nuevo-evolutivo "CÓDIGO: Descripción" → Iniciar evolutivo
   • /estado → Ver configuración del proyecto
```

---

## ⚠️ CRITICAL REMINDERS

### SIEMPRE leer estos archivos:

| Archivo | Información |
|---------|-------------|
| `ESTADO_PROYECTO.json` | Última sesión, evolutivo activo |
| `FUNCIONALIDADES.md` | Estado de evolutivos |
| `specs/[CODIGO].md` | **SPEC COMPLETO con toda la info** |

### MOSTRAR del spec:

- ✅ Descripción y objetivo
- ✅ Criterios de aceptación (checklist)
- ✅ Especificaciones técnicas (si las hay)
- ✅ Historial de sesiones
- ✅ Próximo paso guardado

### NUNCA:

- ❌ Mostrar solo el nombre del evolutivo sin contexto
- ❌ Ignorar el archivo spec
- ❌ Empezar a trabajar sin mostrar el estado completo
- ❌ Asumir cuál evolutivo retomar si hay múltiples

### MÚLTIPLES EVOLUTIVOS:

- ✅ Si se especifica código → usar ese directamente
- ✅ Si hay un solo evolutivo → usarlo automáticamente
- ✅ Si hay múltiples sin código → PREGUNTAR cuál
- ✅ SIEMPRE actualizar `evolutivoActivo` en JSON al seleccionar
- ✅ Mostrar otros evolutivos en progreso al final del resumen
