Permite autoasignarse un evolutivo pendiente (sin asignar)

## Descripción
Permite a un desarrollador autoasignarse un evolutivo que está pendiente (sin asignar).

## Uso
```
/tomar HV-XX
/tomar HV-05
```

## Instrucciones para Claude

Al recibir /tomar {codigo}, debes:

---

## Detect context title

```
1. Leer ESTADO_PROYECTO.json
2. Detectar usuario actual (git config)
3. Verificar que el usuario está en el equipo
4. Buscar el evolutivo pendiente
```

### Case atitle

```
Case aexample

- Case averify spec
- Case averify json
- Case averify assigned
```

### Case btitle

```
Si 1 pendiente: Case bsingle
Si múltiples: Case bmultiple
```

### Case ctitle

```
Case cmessage
```

---

## Read spec title

```
Read spec path
```

---

## Actualizar ESTADO_PROYECTO.json

```
- Update json assignment
- Update json move to in progress
```

---

## Git branch title

### Git level basic

```
✅ Confirm assigned to: {usuario}
✅ Confirm saved in: ESTADO_PROYECTO.json

💡 SUGERENCIA:
┌──────────────────────────────────────────────────┐
│ Ejecuta manualmente:   │
│   git checkout -b feature/HV-05                  │
└──────────────────────────────────────────────────┘
```

### Git level medium (Default)

```
¿Crear rama feature/{0}? (s/n)
```

### Git level high

```
✅ Rama {0} creada automáticamente
```

---

## Show summary title

```
╔══════════════════════════════════════════════════╗
║  📋 TOMAR EVOLUTIVO: {0}       ║
╚══════════════════════════════════════════════════╝

📌 Show summary evolutivo: [CODE]
🔤 Show summary title2: [Título]
⚡ Show summary priority: [alta/media/baja]
📅 Show summary deadline: [Fecha]
🌿 Show summary branch: [feature/CODE]

┌──────────────────────────────────────────────────┐
│ 📝 Show summary description           │
├──────────────────────────────────────────────────┤
│ [Descripción del evolutivo]                      │
└──────────────────────────────────────────────────┘

✅ Show summary acceptance criteria:
- [ ] Criterio 1
- [ ] Criterio 2
- [ ] Criterio 3

🔧 Show summary technical specs:
[Detalles técnicos si existen]

🎯 Show summary next step:
"[Próximo paso a ejecutar]"

💡 Show summary quick commands:
   /pausar    - Pausar Sesión de Trabajo
   /continuar - Continuación de Trabajo
   /estado    - Estado del Proyecto
```

---

## Confirm title

```
╔══════════════════════════════════════════════════╗
║  ✅ EVOLUTIVO ASIGNADO            ║
╚══════════════════════════════════════════════════╝

👤 Confirm assigned to: {usuario}
📁 Confirm saved in: _duran/ESTADO_PROYECTO.json
🌿 Confirm branch: feature/[CODE]

Confirm you can start
```

---

## Casos de Error

### NO HAY EVOLUTIVOS DISPONIBLES

```
❌ NO HAY EVOLUTIVOS DISPONIBLES

Todos los evolutivos están asignados o no hay pendientes.

SUGERENCIA: Usa /nuevo-evolutivo para crear un nuevo evolutivo
```

### EVOLUTIVO YA ASIGNADO

```
⚠️ EVOLUTIVO YA ASIGNADO

El evolutivo {0} está asignado a: {1} ({2})

Error already assigned question
1. Contactar usuario
2. Reasignar
3. Elegir otro

Opción (1/2/3):
```

### SPEC NO ENCONTRADO

```
❌ SPEC NO ENCONTRADO

Archivo no encontrado: _duran/specs/{0}.md

Créalo primero con: /nuevo-evolutivo
```

---

## Reminders title

### Reminders always

- Reminders always1
- Reminders always2
- Reminders always3
- Reminders always4

### Reminders never

- Reminders never1
- Reminders never2
- Reminders never3
- Reminders never4

### Reminders other

- Reminders other if multiple

---

## Archivos Relacionados

- `_duran/ESTADO_PROYECTO.json` - Se actualiza con /tomar
- `_duran/specs/[CODE].md` - Spec del evolutivo
- `/equipo` - Ver estado del equipo
- `/liberar` - Liberar evolutivo
- `/continuar` - Continuar evolutivo

---

## Notas

- Solo se pueden tomar evolutivos en estado "pendiente"
- El evolutivo se mueve de `pendientes` a `enProgreso`
- Se registra la fecha de inicio
- Se actualiza `evolutivoActivo` con el código tomado
- La integración Git depende del nivel configurado
