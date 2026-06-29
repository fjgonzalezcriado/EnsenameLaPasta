---
description: Finaliza evolutivo actualizando spec, FUNCIONALIDADES.md y _duran/ESTADO_PROYECTO.json
---

Finaliza evolutivo actualizando spec, FUNCIONALIDADES.md y JSON

# /finalizar-evolutivo - Cerrar evolutivo

> ⚠️ **CRITICAL**: CRITICAL: Este comando DEBE actualizar TODOS los archivos relacionados:
> - _duran/specs/[CODIGO].md → Marcar como completado + resumen
> - _duran/FUNCIONALIDADES.md → Mover a "Completados"
> - _duran/ESTADO_PROYECTO.json → Mover de `enProgreso` a `completados`

## Parámetro Recibido
- **ID del evolutivo**: $ARGUMENTS

Si no se proporciona ID, leer `_duran/ESTADO_PROYECTO.json` y mostrar evolutivos activos.

---

## FLUJO DE EJECUCIÓN

```
1. Identificar evolutivo (por ID o mostrar lista)
2. Leer spec completo: _duran/specs/[CODIGO].md
3. Verificar checklist (tareas completadas/pendientes)
4. Recopilar información de cierre
5. ⚠️ ACTUALIZAR INMEDIATAMENTE:
   • _duran/specs/[CODIGO].md (cerrar), _duran/FUNCIONALIDADES.md (mover a Completados), _duran/ESTADO_PROYECTO.json (mover array)
6. Mostrar resumen de cierre
```

---

## PASO 1: Identificar Evolutivo

**Si se proporcionó ID:**
```
Buscar en _duran/ESTADO_PROYECTO.json → evolutivos.enProgreso
Buscar en _duran/specs/[ID].md
```

**Si NO se proporcionó ID:**
```
📋 EVOLUTIVOS EN PROGRESO:

1. [HV-01] Permitir más formatos de fecha
   Inicio: 2026-01-23 | Progreso: 60%

2. [EVT-047] Módulo de gestión de becas
   Inicio: 2026-01-20 | Progreso: 80%

¿Cuál deseas finalizar? (indica el número o código)
```

---

## PASO 2: Leer Spec Completo

```
Leer: _duran/specs/[CODIGO].md
```

Extraer::
- Descripción y objetivo
- Criterios de aceptación (checklist)
- Archivos afectados
- Historial de sesiones

---

## PASO 3: Verificar Completitud

Mostrar estado del checklist::

```
📋 CHECKLIST DE [CÓDIGO]: [Nombre]
────────────────────────────────────

✅ Completadas: [N]
   • Criterio 1
   • Criterio 2

⏳ Pendientes: [M]
   • Criterio 3
   • Criterio 4
```

**Si hay tareas pendientes:**

```
⚠️ Hay [M] tareas pendientes. ¿Qué deseas hacer?

1. Marcarlas como completadas (ya están hechas)
2. Marcarlas como "No aplica" / "Descartadas"
3. Cancelar y seguir trabajando

Responde 1, 2 o 3:
```

---

## PASO 4: Recopilar Información de Cierre

**Preguntar al usuario:**

```
📝 CIERRE DEL EVOLUTIVO

Para documentar correctamente el cierre, responde brevemente:

1. **¿Se cumplió el objetivo?** (Sí / Parcialmente / No)
2. **¿Hubo cambios respecto al plan original?** (breve descripción)
3. **¿Alguna lección aprendida?**
4. **¿Quedó deuda técnica nueva?** (Si/No, cuál)
```

**ESPERAR RESPUESTA**

---

## PASO 5: ACTUALIZAR ARCHIVOS INMEDIATAMENTE

> **CRITICAL**: CRITICAL: Ejecutar TODOS estos pasos antes de mostrar confirmación.

### 5.1 Actualizar _duran/specs/[CODIGO].md

Añadir sección de cierre al final::

```markdown
---

## ✅ CIERRE DEL EVOLUTIVO

- **Estado**: ✅ Completado
- **Fecha cierre**: [FECHA_ACTUAL]
- **Duración total**: [N] días
- **Resultado**: [Cumplido | Parcial | No cumplido]

### Resumen de Implementación
[Breve descripción de lo implementado]

### Cambios vs Plan Original
[Lo que cambió respecto al plan inicial, o "Ninguno"]

### Criterios de Aceptación - Estado Final
- [x] Criterio 1
- [x] Criterio 2
- [x] Criterio 3 (No aplicó)

### Archivos Modificados (final)
- `03_Desarrollo/[ruta]/archivo1.cs`
- `03_Desarrollo/[ruta]/archivo2.cs`

### Lecciones Aprendidas
[Lo que se aprendió]

### Deuda Técnica Generada
[Nueva deuda técnica, o "Ninguna"]
```

### 5.2 Actualizar _duran/FUNCIONALIDADES.md

**MOVER de `## 🟡 En Progreso` a `## ✅ Completados`:**:

```markdown
## ✅ Completados

#### [CÓDIGO] [Nombre] ✅
- **Estado**: ✅ Completado
- **Período**: [fecha inicio] → [FECHA_ACTUAL]
- **Duración**: [N] días
- **Resultado**: [Cumplido | Parcial]
- **Spec**: `_duran/specs/[CODIGO].md`
```

**ELIMINAR la entrada de la sección `## 🟡 En Progreso`.**

### 5.3 Actualizar _duran/ESTADO_PROYECTO.json

**MOVER de `evolutivos.enProgreso` a `evolutivos.completados`:**:

```json
{
  "evolutivos": {
    "enProgreso": [
      // ELIMINAR de aquí
    ],
    "completados": [
      // AÑADIR aquí:
      {
        "id": "[CÓDIGO]",
        "nombre": "[Nombre]",
        "inicio": "[fecha inicio]",
        "fin": "[FECHA_ACTUAL]",
        "duracionDias": [N],
        "resultado": "[cumplido|parcial|no_cumplido]",
        "specFile": "_duran/specs/[CODIGO].md"
      }
    ]
  },
  "ultimaSesion": {
    "fecha": "[FECHA_ACTUAL]",
    "evolutivoActivo": null,
    "accion": "Finalizado [CÓDIGO]"
  }
}
```

---

## PASO 6: INTEGRACIÓN GIT

> Después de actualizar los archivos, ofrecer opciones de Git.

### Detectar Estado Git

```bash
# Rama actual
git branch --show-current

# Cambios sin commit
git status --porcelain

# Commits pendientes de push
git log origin/$(git branch --show-current)..HEAD --oneline 2>/dev/null
```

### Nivel BÁSICO:

```
💡 SUGERENCIAS GIT:
┌────────────────────────────────────────────────────────────┐
│ Si tienes cambios pendientes:                             │
│   git add -A                                              │
│   git commit -m "[HV-05] Evolutivo completado"            │
│                                                           │
│ Para publicar la rama:                                    │
│   git push origin feature/HV-05                           │
│                                                           │
│ Crear Pull Request:                                       │
│   https://github.com/org/repo/compare/feature/HV-05       │
│                                                           │
│ Volver a main:                                            │
│   git checkout main                                       │
└────────────────────────────────────────────────────────────┘
```

### Nivel MEDIO (Recomendado):

```
🎯 FINALIZAR EVOLUTIVO: HV-05

El evolutivo está listo para cerrar.

Rama actual: feature/HV-05
Commits pendientes de push: 3
Cambios sin commit: 2 archivos

¿Qué deseas hacer?
1. Commit final + Push + Marcar completado
2. Solo push (ya hice commit) + Marcar completado
3. Solo marcar completado (sin operaciones Git)
4. Cancelar

Opción (1/2/3/4):
```

Si elige 1::
```
📝 COMMIT FINAL

Mensaje sugerido: [HV-05] Evolutivo completado - Implementar paginación

¿Usar este mensaje o escribir otro? (s/otro)
```

Luego::
```
✅ Ejecutando: git add -A
✅ Ejecutando: git commit -m "[HV-05] Evolutivo completado - Implementar paginación"
✅ Ejecutando: git push origin feature/HV-05
✅ Push completado

🔗 Crear Pull Request:
   https://github.com/org/repo/compare/feature/HV-05

¿Volver a rama main? (s/n)
```

Si responde "s"::
```
✅ Ejecutando: git checkout main
✅ Cambiado a rama main
```

### Nivel ALTO:

```
🤖 ACCIONES GIT AUTOMÁTICAS:
├── ✅ Detectados 2 archivos sin commit
├── ✅ git add -A
├── ✅ git commit -m "[HV-05] Evolutivo completado"
├── ✅ git push origin feature/HV-05
├── ✅ git checkout main
└── ✅ Cambiado a rama main

🔗 Pull Request:
   https://github.com/org/repo/compare/feature/HV-05
```

### Mover Spec a Completados (opcional)

```
📁 ¿Mover spec a carpeta de completados?

De: _duran/specs/HV-05.md
A: _duran/specs/completados/HV-05.md

(s/n)
```

---

## PASO 7: Confirmación

### 7.1 Confirmación SIN Jira (evolutivo local HV-XX o texto libre)

```
╔═══════════════════════════════════════════════════════════╗
║                  ✅ EVOLUTIVO FINALIZADO                          ║
╚═══════════════════════════════════════════════════════════╝

📋 [CÓDIGO]: [Nombre]
📅 Período: [inicio] → [fin]
⏱️ Duración: [N] días
🎯 Resultado: [Cumplido | Parcial | No cumplido]

┌────────────────────────────────────────────────────────────┐
│ 📂 ARCHIVOS ACTUALIZADOS:                                       │
├────────────────────────────────────────────────────────────┤
│ ✅ _duran/specs/[CODIGO].md → Cerrado con resumen       │
│ ✅ _duran/FUNCIONALIDADES.md → Movido a "Completados"    │
│ ✅ _duran/ESTADO_PROYECTO.json → Actualizado               │
└────────────────────────────────────────────────────────────┘

📊 Resumen:
   • Criterios cumplidos: [N]/[M]
   • Archivos modificados: [X]
   • Deuda técnica nueva: [Sí/No]

💡 Próximos pasos sugeridos::
   • /nuevo-evolutivo "CÓDIGO: Descripción" → Iniciar otro evolutivo
   • /estado → Ver estado general del proyecto
```

### 7.2 Confirmación CON Jira (evolutivo vinculado a Jira)

> **Detectar: Si el código del evolutivo coincide con patrón Jira (`^[A-Z]{2,10}-[0-9]{1,6}$`) Y NO es un evolutivo local (`HV-XX`), mostrar esta versión con recordatorio de Jira.**

```
╔═══════════════════════════════════════════════════════════╗
║                  ✅ EVOLUTIVO FINALIZADO                          ║
╚═══════════════════════════════════════════════════════════╝

📋 [CÓDIGO]: [Nombre]
📅 Período: [inicio] → [fin]
⏱️ Duración: [N] días
🎯 Resultado: [Cumplido | Parcial | No cumplido]

┌────────────────────────────────────────────────────────────┐
│ 📂 ARCHIVOS ACTUALIZADOS:                                       │
├────────────────────────────────────────────────────────────┤
│ ✅ _duran/specs/[CODIGO].md → Cerrado con resumen       │
│ ✅ _duran/FUNCIONALIDADES.md → Movido a "Completados"    │
│ ✅ _duran/ESTADO_PROYECTO.json → Actualizado               │
└────────────────────────────────────────────────────────────┘

📊 Resumen:
   • Criterios cumplidos: [N]/[M]
   • Archivos modificados: [X]
   • Deuda técnica nueva: [Sí/No]

┌────────────────────────────────────────────────────────────┐
│ 🔗 ACCIÓN MANUAL REQUERIDA EN JIRA:                             │
├────────────────────────────────────────────────────────────┤
│                                                                 │
│  El evolutivo [CÓDIGO] está vinculado a Jira.                   │
│                                                                 │
│  ⚠️ Recuerda actualizar el estado del ticket en Jira:           │
│                                                                 │
│  📌 URL: [URL_JIRA]/browse/[CÓDIGO]                             │
│                                                                 │
│  Pasos sugeridos:                                               │
│  1. Abrir el ticket [CÓDIGO] en Jira                            │
│  2. Cambiar estado a "Done" / "Cerrado" / "Completado"          │
│  3. Añadir comentario con resumen del trabajo realizado         │
│  4. Verificar que las horas están registradas                   │
│                                                                 │
└────────────────────────────────────────────────────────────┘

💡 Próximos pasos sugeridos::
   • /nuevo-evolutivo "CÓDIGO: Descripción" → Iniciar otro evolutivo
   • /estado → Ver estado general del proyecto
```

### Lógica de detección de Jira

```
# Leer configuración Jira
Leer: _duran/ESTADO_PROYECTO.json → jira

# Si Jira está habilitado y el código coincide con patrón Jira
SI jira.habilitado = true
   Y codigo matches "^[A-Z]{2,10}-[0-9]{1,6}$"
   Y codigo NO empieza con "HV-"
ENTONCES
   Mostrar confirmación 7.2 (CON recordatorio Jira)
   Usar URL: {jira.url}/browse/{codigo}
SINO
   Mostrar confirmación 7.1 (SIN Jira)
```

---

## ⚠️ CRITICAL REMINDERS

### SIEMPRE actualizar estos 3 archivos::

| Archivo | Acción |
|---------|--------|
| `_duran/specs/[CODIGO].md` | Añadir sección "CIERRE DEL EVOLUTIVO" |
| `_duran/FUNCIONALIDADES.md` | MOVER de "En Progreso" a "Completados" |
| `_duran/ESTADO_PROYECTO.json` | MOVER en arrays + limpiar evolutivoActivo |

### NUNCA::

- ❌ Finalizar sin actualizar los 3 archivos
- ❌ Dejar el evolutivo en "En Progreso" después de finalizar
- ❌ Mostrar confirmación sin haber escrito a archivos

### VERIFICACIÓN::

Antes de mostrar confirmación, verificar::
```powershell
# El spec debe tener la sección de cierre
Select-String "CIERRE DEL EVOLUTIVO" "_duran/specs/[CODIGO].md"
```
