---
description: Muestra estado completo del proyecto incluyendo evolutivos y specs
---

Muestra estado completo del proyecto incluyendo evolutivos y specs

# Comando: /estado

> Muestra el estado actual del proyecto, evolutivos activos y su progreso.

---

## FLUJO DE EJECUCIÓN

```
1. Leer ESTADO_PROYECTO.json → configuración + última sesión + evolutivoActivo
2. Leer FUNCIONALIDADES.md → evolutivos en progreso y completados
3. Leer specs/*.md → detalles de cada evolutivo activo
4. Detectar estado de git
5. Mostrar resumen completo (marcando evolutivo activo con →)
```

---

## ARCHIVOS A LEER

| Archivo | Información |
|---------|-------------|
| `_duran/ESTADO_PROYECTO.json` | Config proyecto, última sesión, **evolutivoActivo** |
| `_duran/FUNCIONALIDADES.md` | Lista de evolutivos y su estado |
| `_duran/specs/*.md` | Specs de evolutivos activos |
| `_duran/DEUDA_TECNICA.md` | Deuda técnica pendiente (si existe) |

---

## OUTPUT ESPERADO

```
╔══════════════════════════════════════════════════════════════════╗
║                    📊 ESTADO DEL PROYECTO                         ║
╚══════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────┐
│ 📁 INFORMACIÓN GENERAL                                          │
├─────────────────────────────────────────────────────────────────┤
│ Proyecto: [nombre]                                              │
│ Versión: [version_actual]                                       │
│ Código en: 03_Desarrollo/                                       │
│ Última sesión: [fecha]                                          │
│ Evolutivo activo: [CÓDIGO] ← desde evolutivoActivo del JSON     │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 👥 EQUIPO                                                       │
├─────────────────────────────────────────────────────────────────┤
│ Owner: [owner]                                                  │
│ Jefe de Proyecto: [jefe_proyecto]                               │
│ Responsable Técnico: [responsable_tecnico]                      │
└─────────────────────────────────────────────────────────────────┘

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🟡 EVOLUTIVOS EN PROGRESO: [N]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

┌─────────────────────────────────────────────────────────────────┐
│ → [CÓDIGO-1]: [Nombre del evolutivo 1]          ⬅️ ACTIVO       │
│    ├─ Progreso: ████████░░ 80% ([4]/[5] criterios)             │
│    ├─ Prioridad: 🔴 Alta                                        │
│    ├─ Inicio: [fecha]                                           │
│    ├─ Última sesión: [fecha]                                    │
│    ├─ Spec: _duran/specs/[CODIGO-1].md                       │
│    └─ Próximo paso: [próximo paso guardado]                     │
├─────────────────────────────────────────────────────────────────┤
│   [CÓDIGO-2]: [Nombre del evolutivo 2]                          │
│    ├─ Progreso: ████░░░░░░ 40% ([2]/[5] criterios)             │
│    ├─ Prioridad: 🟠 Media                                       │
│    ├─ Inicio: [fecha]                                           │
│    ├─ Última sesión: [fecha]                                    │
│    ├─ Spec: _duran/specs/[CODIGO-2].md                       │
│    └─ Próximo paso: [próximo paso guardado]                     │
└─────────────────────────────────────────────────────────────────┘

💡 Para cambiar de evolutivo: /continuar [CÓDIGO]

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ EVOLUTIVOS COMPLETADOS (últimos 5): [M]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

│ [CÓDIGO-A] [Nombre] ✅ (completado [fecha], [N] días)
│ [CÓDIGO-B] [Nombre] ✅ (completado [fecha], [N] días)
│ [CÓDIGO-C] [Nombre] ✅ (completado [fecha], [N] días)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📋 SPECS DISPONIBLES:
━━━━━━━━━━━━━━━━━━━━━

│ _duran/specs/
│ ├─ [CODIGO-1].md (🟡 en progreso)
│ ├─ [CODIGO-2].md (🟡 en progreso)
│ ├─ [CODIGO-A].md (✅ completado)
│ └─ [CODIGO-B].md (✅ completado)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

⚠️ DEUDA TÉCNICA:
━━━━━━━━━━━━━━━━━

│ 🔴 Crítica: [N] items
│ 🟠 Importante: [M] items
│ 🟡 Menor: [P] items
│
│ Ver detalle: _duran/DEUDA_TECNICA.md

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 GIT STATUS:
━━━━━━━━━━━━━━

│ Branch: [branch_actual]
│ Cambios sin commit: [N] archivos
│ Último commit: [hash] - [mensaje] ([fecha])

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

💡 COMANDOS ÚTILES:
━━━━━━━━━━━━━━━━━━━

│ /continuar                    → Retomar evolutivo activo
│ /continuar [CÓDIGO]           → Cambiar a otro evolutivo
│ /nuevo-evolutivo "COD: Desc"  → Iniciar nuevo evolutivo
│ /finalizar-evolutivo [COD]    → Cerrar evolutivo
│ /pausar                       → Guardar estado antes de salir
│ /analizar                     → Analizar código del proyecto
```

---

## DETALLE DE CADA SECCIÓN

### 1. Información General
Desde `ESTADO_PROYECTO.json`:
- Nombre del proyecto
- Versión actual
- Modo (desarrollo/mantenimiento/soporte)
- Última sesión

### 2. Evolutivos en Progreso
Desde `FUNCIONALIDADES.md` + `specs/*.md`:
- Lista de evolutivos activos
- Progreso basado en criterios de aceptación
- Prioridad
- Próximo paso (del spec)

### 3. Evolutivos Completados
Desde `FUNCIONALIDADES.md`:
- Últimos 5 evolutivos cerrados
- Fecha de cierre y duración

### 4. Specs Disponibles
Listar archivos en `_duran/specs/`:
- Indicar estado (en progreso / completado)

### 5. Deuda Técnica
Desde `DEUDA_TECNICA.md` (si existe):
- Conteo por severidad
- Referencia al archivo

### 6. Git Status
Ejecutar:
```powershell
git branch --show-current
git status --short | Measure-Object
git log -1 --oneline
```

---

## CASOS ESPECIALES

### Sin Evolutivos Activos

```
ℹ️ No hay evolutivos en progreso.

💡 Para iniciar uno:
   /nuevo-evolutivo "CÓDIGO: Descripción"
```

### Sin Specs

```
⚠️ No hay specs en _duran/specs/

Los evolutivos se documentan en FUNCIONALIDADES.md pero sin spec detallado.

💡 Recomendación: Usar /nuevo-evolutivo para crear specs automáticamente.
```

### Proyecto Nuevo (sin estado)

```
👋 PROYECTO SIN ESTADO INICIAL

No se encontró _duran/ESTADO_PROYECTO.json

💡 Comandos sugeridos:
   • /analizar → Analizar estructura del proyecto
   • /setup → Configurar proyecto inicial
```

---

## ⚠️ REMINDERS

### SIEMPRE mostrar:

- ✅ Evolutivos en progreso con progreso real
- ✅ Próximo paso de cada evolutivo (desde spec)
- ✅ Lista de specs disponibles
- ✅ Estado de git

### CALCULAR progreso desde:

```
Progreso = (criterios completados / total criterios) * 100

Leer de specs/[CODIGO].md:
- Contar líneas con "- [x]" → completados
- Contar líneas con "- [ ]" → pendientes
```
