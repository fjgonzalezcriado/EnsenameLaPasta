Guarda estado de sesión en spec, FUNCIONALIDADES.md y JSON

# Pausar Sesión de Trabajo

> ⚠️ **CRITICAL**: CRITICAL: Este comando DEBE actualizar TODOS los archivos relacionados:
> - `_duran/specs/[CODIGO].md` → Nota de sesión añadida
> - `_duran/FUNCIONALIDADES.md` → Última sesión actualizada + Próximo paso guardado
> - `_duran/ESTADO_PROYECTO.json` → Estado completo guardado

## Contexto
Guardar el estado actual antes de cerrar Claude Code para poder retomar después con /continuar.

---

## FLUJO DE EJECUCIÓN

```
1. Detectar evolutivo activo (desde JSON)
2. Leer spec actual: _duran/specs/[CODIGO].md
3. Preguntar: próximo paso, notas, archivos en progreso
4. ESPERAR respuesta del usuario
5. ⚠️ ACTUALIZAR INMEDIATAMENTE:
   • _duran/specs/[CODIGO].md (Nota de sesión añadida)
   • _duran/FUNCIONALIDADES.md (Última sesión actualizada)
   • _duran/ESTADO_PROYECTO.json (Estado completo guardado)
6. Mostrar confirmación + recordatorio de archivos sin commit
```

---

## PASO 1: Detectar Evolutivo Activo

```
Leer: _duran/ESTADO_PROYECTO.json
   → Buscar campo "evolutivoActivo"
   → Buscar "evolutivos.enProgreso"
```

### Caso A: Hay evolutivoActivo definido

```
Si evolutivoActivo != null:
   📋 Evolutivo activo: {0} - {1}
   → Continuar con ese evolutivo
```

### Caso B: No hay evolutivoActivo pero hay evolutivos en progreso

```
Si evolutivoActivo == null Y evolutivos.enProgreso.length > 0:

   Si evolutivos.enProgreso.length == 1:
      → Usar ese único evolutivo

   Si evolutivos.enProgreso.length > 1:
      ╔═══════════════════════════════════════════════════════════╗
      ║              📋 MÚLTIPLES EVOLUTIVOS EN PROGRESO                ║
      ╚═══════════════════════════════════════════════════════════╝

      ¿Cuál evolutivo quieres pausar?

      → HV-01: Validación de DNI... (60%)
        HV-02: Agregar campos fecha... (30%)

      Indica el código o "todos" para pausar todos:

      ESPERAR RESPUESTA DEL USUARIO
```

### Caso C: No hay evolutivos en progreso

```
Si evolutivos.enProgreso.length == 0:
   ℹ️ No hay evolutivo activo. Guardando estado general del proyecto.
```

---

## PASO 2: Leer Spec del Evolutivo (si existe)

```
Leer: _duran/specs/[CODIGO].md
```

Extraer:
- Checklist actual (tareas completadas/pendientes)
- Última nota de sesión
- Archivos afectados

---

## PASO 3: Recopilar Estado Actual

**Preguntar al usuario:**

```
⏸️ PAUSANDO SESIÓN

Para poder retomar correctamente, necesito saber:

1. **¿Cuál es el próximo paso pendiente?**
   (Lo primero que hay que hacer al retomar)

2. **¿Qué se logró en esta sesión?**
   (Breve resumen del avance)

3. **¿Hay algo importante a recordar?**
   (Contexto, decisiones, problemas encontrados)

4. **¿En qué archivos estabas trabajando?**
   (Si no los recuerdas, los detecto automáticamente)

Puedes responder en formato libre.
```

**ESPERAR RESPUESTA DEL USUARIO**

---

## PASO 4: Detectar Archivos Modificados

```powershell
# Detectar cambios no commiteados
git status --short

# Filtrar solo código
git status --short | Where-Object { $_ -match "03_Desarrollo/" }
```

---

## PASO 5: ⚠️ ACTUALIZAR ARCHIVOS INMEDIATAMENTE

> **CRITICAL**: CRITICAL: Ejecutar TODOS estos pasos antes de mostrar confirmación.

### 5.1 Actualizar _duran/specs/[CODIGO].md

Añadir en la sección `## Historial de Sesiones`:

```markdown
## Historial de Sesiones
- [FECHA_ANTERIOR]: [nota anterior]
- [FECHA_ACTUAL]: [resumen de lo logrado] | Próximo: [próximo paso]
```

Si hay checklist, actualizar tareas completadas:
```markdown
## Criterios de Aceptación
- [x] Criterio 1 (completado)
- [x] Criterio 2 (completado esta sesión)
- [ ] Criterio 3 (pendiente)
```

### 5.2 Actualizar _duran/FUNCIONALIDADES.md

En la sección del evolutivo activo:

```markdown
#### [CÓDIGO] [Nombre]
- **Estado**: 🟡 En progreso
- **Última sesión**: [FECHA_ACTUAL]    ← ACTUALIZAR
- **Spec**: `_duran/specs/[CODIGO].md`

**Checklist:**
- [x] Tarea 1
- [x] Tarea 2                          ← ACTUALIZAR si cambió
- [ ] Tarea 3

**Próximo paso:** [próximo paso]       ← ACTUALIZAR

**Notas de sesión:**
- [FECHA_ACTUAL]: [resumen] | Próximo: [paso]   ← AÑADIR
```

### 5.3 Actualizar _duran/ESTADO_PROYECTO.json

```json
{
  "ultimaSesion": {
    "fecha": "[FECHA_HORA_ACTUAL]",
    "evolutivoActivo": "[CÓDIGO]",
    "proximoPaso": "[lo que indicó el usuario]",
    "resumenSesion": "[lo que se logró]",
    "archivosModificados": [
      "03_Desarrollo/[Proyecto]/archivo1.cs",
      "03_Desarrollo/[Proyecto]/archivo2.cs"
    ],
    "notas": "[notas adicionales]",
    "archivosSinCommit": true
  },
  "evolutivos": {
    "enProgreso": [
      {
        "id": "[CÓDIGO]",
        "ultimaSesion": "[FECHA_ACTUAL]",
        "progreso": [X]
      }
    ]
  }
}
```

---

## PASO 6: SESIÓN PAUSADA

```
╔═══════════════════════════════════════════════════════════════╗
║                    ⏸️ SESIÓN PAUSADA                             ║
╚═══════════════════════════════════════════════════════════════╝

📅 Fecha: [FECHA_HORA_ACTUAL]
📋 Evolutivo: [CÓDIGO] - [Nombre]
📊 Progreso: ████████░░ [X]%

┌──────────────────────────────────────────────────────────────────┐
│ 📂 ESTADO GUARDADO EN:                                          │
├──────────────────────────────────────────────────────────────────┤
│ ✅ _duran/specs/[CODIGO].md      ← Nota de sesión añadida    │
│ ✅ _duran/FUNCIONALIDADES.md     ← Última sesión actualizada │
│ ✅ _duran/ESTADO_PROYECTO.json   ← Estado completo guardado  │
└──────────────────────────────────────────────────────────────────┘

🎯 Próximo paso guardado:
   "[próximo paso]"

📂 Archivos de esta sesión:
   • 03_Desarrollo/[ruta]/archivo1.cs
   • 03_Desarrollo/[ruta]/archivo2.cs
```

---

## PASO 7: Verificar Cambios Sin Commit

```
⚠️ ARCHIVOS MODIFICADOS SIN COMMIT:
━━━━━━━━━━━━━━━━━━━━━━━━━━━

 M 03_Desarrollo/[Proyecto]/archivo1.cs
 M 03_Desarrollo/[Proyecto]/archivo2.cs
 A 03_Desarrollo/[Proyecto]/archivo3.cs

💡 Recomendación: Hacer commit antes de cerrar
   git add .
   git commit -m "[CÓDIGO]: [descripción del avance]"
```

**Si no hay cambios sin commit:**
```
✅ Todos los cambios están commiteados
```

---

## PASO 8: PARA RETOMAR

```
💡 PARA RETOMAR:
━━━━━━━━━━━━━━

1. Abre Claude Code en este proyecto
2. Ejecuta: /continuar
3. Claude cargará automáticamente::
   • Evolutivo activo
   • Próximo paso
   • Contexto de la sesión

¡Hasta la próxima sesión! 👋
```

---

## ⚠️ CRITICAL REMINDERS

### SIEMPRE actualizar estos 3 archivos:

| Archivo | Acción |
|---------|--------|
| `_duran/specs/[CODIGO].md` | Nota de sesión añadida |
| `_duran/FUNCIONALIDADES.md` | Última sesión actualizada + Próximo paso guardado |
| `_duran/ESTADO_PROYECTO.json` | Estado completo guardado |

### NUNCA:

- ❌ Pausar sin actualizar los archivos
- ❌ Dejar el próximo paso solo en el chat
- ❌ Mostrar confirmación sin haber escrito a archivos

### SIN EVOLUTIVO ACTIVO:

Si no hay evolutivo en progreso, igual guardar en JSON:
```json
{
  "ultimaSesion": {
    "fecha": "[FECHA]",
    "evolutivoActivo": null,
    "notas": "[lo que se trabajó]"
  }
}
```
