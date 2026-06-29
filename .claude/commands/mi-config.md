Configura preferencias personales del desarrollador (nivel Git, etc.)

## Descripción
Descripción

## Uso
```
/mi-config                     → Ver configuración actual
/mi-config nivel basico        → Cambiar nivel de integración Git
/mi-config nivel medio         → Cambiar nivel de integración Git
/mi-config nivel alto          → Cambiar nivel de integración Git
/mi-config crear               → Crear archivo de configuración personal
/mi-config reset               → Restaurar valores por defecto
```

## Instrucciones para Claude

Read state

### 1. Verificar si Existe Configuración Personal

```javascript
const userConfigPath = ".claude/user-config.json";
const templatePath = ".claude/user-config.template.json";
const existeConfig = existeArchivo(userConfigPath);
```

### 2. Flujo según Subcomando

#### /mi-config (sin argumentos) - Ver configuración

```
👤 MI CONFIGURACIÓN PERSONAL
═══════════════════════════════════════════════════════════════════════════════

📁 Archivo: .claude/user-config.json
📄 Estado: ✅ Existe (o ❌ No existe)

┌─────────────────────────────────────────────────────────────────────────────┐
│ PREFERENCIAS ACTUALES                                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ 👤 Usuario:              jgarcia                                            │
│ 📧 Email:                jgarcia@comillas.edu                               │
│                                                                             │
│ 🌿 Nivel Git:            medio ⭐ (recomendado)                             │
│ 📤 Confirmar Push:       ✅ Sí                                              │
│ 🏠 Auto checkout main:   ✅ Sí                                              │
│ 💡 Mostrar sugerencias:  ✅ Sí                                              │
│ 📝 Formato commit:       [{codigo}] {tipo}: {mensaje}                       │
│                                                                             │
│ 📢 Verbosidad:           normal                                             │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

📊 CONFIGURACIÓN DEL PROYECTO (default si no tienes user-config.json):
   • Nivel Git: medio
   • Rama base: main

💡 COMANDOS:
   • /mi-config nivel alto     → Cambiar nivel de integración
   • /mi-config crear          → Crear configuración personal
   • /mi-config reset          → Restaurar valores por defecto
```

#### No tienes configuración personal:

```
👤 MI CONFIGURACIÓN PERSONAL
═══════════════════════════════════════════════════════════════════════════════

❌ No tienes configuración personal

Actualmente usas la configuración del PROYECTO:
   • Nivel Git: medio
   • Esto aplica a TODOS los desarrolladores

¿Quieres crear tu configuración personal?

Esto te permite:
   ✓ Elegir tu propio nivel de integración Git
   ✓ Personalizar el formato de commits
   ✓ Configurar notificaciones a tu gusto
   ✓ Sin afectar a tus compañeros

Usa: /mi-config crear
```

#### `/mi-config crear` - /mi-config crear               → Crear archivo de configuración personal

```javascript
// Detecté tu información de Git:
const usuarioGit = ejecutar("git config user.name").trim();
const emailGit = ejecutar("git config user.email").trim();
```

```
📝 CREAR CONFIGURACIÓN PERSONAL
═══════════════════════════════════════════════════════════════════════════════

Detecté tu información de Git:
   • Usuario: {usuarioGit}
   • Email: {emailGit}

Nivel de integración Git:
   1. 💡 básico  - Solo sugiere comandos (máximo control)
   2. ⚡ medio   - Pregunta antes de ejecutar (recomendado) ⭐
   3. 🤖 alto    - Ejecuta automáticamente (máxima velocidad)

¿Qué nivel prefieres? (1/2/3):
```

After choosing

```javascript
// Create file with config
const config = {
  usuario: {
    nombre: usuarioGit,
    email: emailGit
  },
  preferencias: {
    nivelIntegracionGit: nivelElegido,  // "basico" | "medio" | "alto"
    confirmarPush: true,
    autoCheckoutMain: true,
    mostrarSugerenciasGit: true,
    formatoCommit: "[{codigo}] {tipo}: {mensaje}"
  },
  notificaciones: {
    mostrarAlertas: true,
    mostrarTips: true,
    verbosidad: "normal"
  }
};

guardarArchivo(".claude/user-config.json", config);
```

```
✅ CONFIGURACIÓN CREADA
═══════════════════════════════════════════════════════════════════════════════

📁 Archivo: .claude/user-config.json
🌿 Nivel Git: {nivelElegido}
👤 Usuario: {usuarioGit}

⚠️ IMPORTANTE: Este archivo está en .gitignore
   → Tu configuración es SOLO TUYA
   → No afecta a tus compañeros
   → No se subirá al repositorio

💡 Puedes cambiar el nivel en cualquier momento:
   /mi-config nivel basico|medio|alto
```

#### `/mi-config nivel {nivel}` - CAMBIAR NIVEL DE INTEGRACIÓN GIT

```
Usuario: /mi-config nivel alto

Claude: 🌿 CAMBIAR NIVEL DE INTEGRACIÓN GIT
        ═══════════════════════════════════════════════════════════════════════════════

        Nivel actual: medio
        Nivel nuevo:  alto

        Comportamiento del nivel ALTO:
        ├── git checkout -b  → 🤖 Automático
        ├── git add/commit   → 🤖 Automático
        ├── git push         → 🤖 Automático (con confirmación si confirmarPush=true)
        └── git checkout     → 🤖 Automático

        ⚠️ Con nivel alto, Claude ejecutará comandos Git sin preguntar.

        ¿Confirmar cambio a nivel alto? (s/n)

Usuario: s

Claude: ✅ Nivel cambiado a: alto

        Tu nueva configuración:
        • Nivel Git: alto 🤖
        • Confirmar Push: ✅ (siempre preguntará antes de push)
```

#### `/mi-config reset` - RESTAURAR CONFIGURACIÓN

```
⚠️ RESTAURAR CONFIGURACIÓN

Esto eliminará tu archivo .claude/user-config.json y volverás a usar la configuración del proyecto.

¿Confirmar? (s/n)
```

If confirms:
```
✅ Configuración personal eliminada
   Ahora usas la configuración del proyecto (nivel: medio)
```

### 3. Lógica de Prioridad en Otros Comandos

Los comandos `/nuevo-evolutivo`, `/commit`, `/continuar`, `/finalizar-evolutivo` deben leer así:

```javascript
function obtenerNivelGit() {
  // 1. Try read personal config
  try {
    const userConfig = leerArchivo(".claude/user-config.json");
    if (userConfig?.preferencias?.nivelIntegracionGit) {
      return userConfig.preferencias.nivelIntegracionGit;
    }
  } catch (e) {
    // Personal config not exists
  }

  // 2. Read project config
  try {
    const estado = leerArchivo("_duran/ESTADO_PROYECTO.json");
    if (estado?.configuracion?.nivelIntegracionGit) {
      return estado.configuracion.nivelIntegracionGit;
    }
  } catch (e) {
    // Project config not exists
  }

  // 3. Default
  return "medio";
}
```

### 4. Mostrar Origen de Configuración

Cuando se usa configuración, indicar de dónde viene:

```
Nivel Git: medio (personal)     ← Viene de user-config.json
Nivel Git: medio (proyecto)     ← Viene de ESTADO_PROYECTO.json
Nivel Git: medio (default)      ← Valor por defecto
```

### 5. Tabla de Opciones Configurables

| Opción | Valores | Default | Descripción |
|--------|---------|---------|-------------|
| `nivelIntegracionGit` | basico, medio, alto | medio | Nivel de automatización Git |
| `confirmarPush` | true, false | true | Siempre confirmar antes de push |
| `autoCheckoutMain` | true, false | true | Volver a main al finalizar |
| `mostrarSugerenciasGit` | true, false | true | Mostrar comandos sugeridos |
| `formatoCommit` | string | `[{codigo}] {tipo}: {mensaje}` | Formato del mensaje |
| `verbosidad` | minimal, normal, verbose | normal | Cantidad de información |

## Archivos Relacionados

- .claude/user-config.json - Configuración personal (NO se commitea)
- .claude/user-config.template.json - Plantilla de referencia
- _duran/ESTADO_PROYECTO.json - Configuración del proyecto (compartida)
- .gitignore - Excluye user-config.json

## Notas

- La configuración personal SIEMPRE tiene prioridad sobre la del proyecto
- El archivo user-config.json está en .gitignore, no se sube al repo
- Cada desarrollador puede tener un nivel diferente
- Si no existe configuración personal, se usa la del proyecto
- Si no existe ninguna, se usa "medio" como default
