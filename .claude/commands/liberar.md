Permite liberar (desasignarse de) el evolutivo actual para que otro lo tome

## Descripción
Permite a un desarrollador liberar (desasignarse de) su evolutivo actual para que otro miembro del equipo pueda tomarlo.

## Uso
```
/liberar
/liberar HV-05
```

## Instrucciones para Claude

Instrucciones para Claude

---

## Detect context title

```
Read json
Detectar usuario actual
Verify in team
Verify evolutivo
```

### Case atitle

```
Case averify exists
Case averify ownership
Case asuccess
```

### Case btitle

```
Case bauto detect
```

### Case ctitle

```
Case cmessage
```

---

## Update json title

```
Update json unassign
Update json move back pending
Update json remove active
```

---

## Git branch title

### Git level basic

```
✅ Confirm released: {usuario}
✅ Confirm saved in: ESTADO_PROYECTO.json

💡 SUGERENCIA:
┌──────────────────────────────────────────────────┐
│ Suggest manual branch   │
│   git checkout main                              │
│   git branch -D feature/HV-05                    │
└──────────────────────────────────────────────────┘
```

### Git level medium

```
¿Ask delete branch?
```

### Git level high

```
✅ Branch deleted auto
```

---

## Show summary title

```
╔══════════════════════════════════════════════════╗
║  📤 LIBERAR EVOLUTIVO   ║
╚══════════════════════════════════════════════════╝

📌 Show summary evolutivo: [CODE]
🔓 Show summary released: Show summary yes
🌿 Show summary branch: [feature/CODE o N/A]

📋 Show summary available

💡 Show summary commands:
   /tomar [CODE]  - Comando: /tomar
   /estado        - Estado del Proyecto
   /equipo        - Ver estado del equipo
```

---

## Confirm title

```
╔══════════════════════════════════════════════════╗
║  ✅ Confirm evolutivo released ║
╚══════════════════════════════════════════════════╝

🔓 Confirm now available
```

---

## Casos de Error

### Error no evolutivo

```
❌ Error no evolutivo

Error no evolutivo message

SUGERENCIA: /tomar [CODE]
```

### Error not owner

```
⚠️ Error not owner

Error not owner message

SUGERENCIA: Contactar usuario
```

### Error code not found

```
❌ Error code not found

Error code not found message

Error code not found help
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

- Reminders other allow reassign

---

## Archivos Relacionados

- `_duran/ESTADO_PROYECTO.json` - Se actualiza con /liberar
- `_duran/specs/[CODE].md` - Spec del evolutivo
- `/tomar` - Reassign evolutivo
- `/equipo` - Ver estado del equipo

---

## Notas

- Notes no delete
- Notes state change
- Notes branch optional
- Notes can retake
- Notes git integration depends
