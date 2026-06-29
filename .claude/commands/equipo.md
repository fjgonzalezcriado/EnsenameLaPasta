Muestra el estado actual del equipo y los evolutivos asignados

## Descripción
Muestra el estado actual del equipo y los evolutivos asignados a cada miembro.

## Uso
```
/equipo
/equipo resumen
/equipo detalle
```

## Instrucciones para Claude

Leer ESTADO_PROYECTO.json

### 1. Leer ESTADO_PROYECTO.json

```javascript
// Leer ESTADO_PROYECTO.json
const estado = leerArchivo("_duran/ESTADO_PROYECTO.json");
const equipo = estado.equipo?.miembros || [];
const evolutivos = {
  pendientes: estado.evolutivos?.pendientes || [],
  enProgreso: estado.evolutivos?.enProgreso || [],
  completados: estado.evolutivos?.completados || []
};
```

### 2. Detectar Usuario Actual (si es posible)

```bash
# Intentar obtener usuario Git actual
git config user.name
git config user.email
```

### 3. Generar Vista de Equipo

Mostrar en formato visual:

```
📊 ESTADO DEL EQUIPO - {0}
═══════════════════════════════════════════════════════════════════════════════

👤 Usuario actual: {0} ({1})
🌿 Rama actual: {0}

───────────────────────────────────────────────────────────────────────────────

👥 MIEMBROS DEL EQUIPO
┌─────────────┬──────────────────────┬───────────────┬─────────────────────┐
│ Usuario     │ Nombre               │ Rol           │ Evolutivo Activo    │
├─────────────┼──────────────────────┼───────────────┼─────────────────────┤
│ jgarcia     │ Juan García          │ desarrollador │ HV-01 (en progreso) │
│ mlopez      │ María López          │ desarrollador │ HV-03 (en progreso) │
│ aruiz       │ Ana Ruiz             │ jefe_proyecto │ -                   │
└─────────────┴──────────────────────┴───────────────┴─────────────────────┘

───────────────────────────────────────────────────────────────────────────────

📋 EVOLUTIVOS

⏳ PENDIENTES (sin asignar)
┌──────────┬────────────────────────────────────┬───────────┬───────────────┐
│ Código   │ Título                           │ Prioridad │ Creado        │
├──────────┼────────────────────────────────────┼───────────┼───────────────┤
│ HV-02    │ Validación ISO 8601 fechas       │ alta      │ 2026-01-20    │
│ HV-04    │ Endpoint bulk insert             │ media     │ 2026-01-22    │
└──────────┴────────────────────────────────────┴───────────┴───────────────┘

🔵 EN PROGRESO
┌──────────┬────────────────────────────────────┬───────────────┬─────────────┐
│ Código   │ Título                           │ Asignado      │ Rama        │
├──────────┼────────────────────────────────────┼───────────────┼─────────────┤
│ HV-01    │ Campos auditoría CourseDTO       │ jgarcia       │ feature/HV-01│
│ HV-03    │ Refactor servicio pagos          │ mlopez        │ feature/HV-03│
└──────────┴────────────────────────────────────┴───────────────┴─────────────┘

✅ COMPLETADOS (últimos 5)
┌──────────┬────────────────────────────────────┬───────────────┬─────────────┐
│ Código   │ Título                           │ Completado por│ Fecha       │
├──────────┼────────────────────────────────────┼───────────────┼─────────────┤
│ HV-00    │ Setup inicial proyecto           │ jgarcia       │ 2026-01-18  │
└──────────┴────────────────────────────────────┴───────────────┴─────────────┘

───────────────────────────────────────────────────────────────────────────────

📈 RESUMEN
   • Total evolutivos: {0}
   • Pendientes: {0}
   • En progreso: {0}
   • Completados: {0}

💡 COMANDOS DISPONIBLES
   • /tomar HV-XX      → Asignarte un evolutivo pendiente
   • /asignar HV-XX @usuario → Asignar evolutivo a un compañero
   • /liberar          → Liberar tu evolutivo actual
   • /continuar HV-XX  → Continuar trabajando en un evolutivo

═══════════════════════════════════════════════════════════════════════════════
```

### 4. Casos especiales

#### EQUIPO NO CONFIGURADO:
```
⚠️ EQUIPO NO CONFIGURADO

No se han configurado miembros del equipo.

Para configurar, edita ESTADO_PROYECTO.json:

{
  "equipo": {
    "miembros": [
      {
        "usuario": "tu_usuario_git",
        "nombre": "Tu Nombre",
        "email": "email@comillas.edu",
        "rol": "desarrollador"
      }
    ]
  }
}
```

#### No hay evolutivos registrados.:
```
📋 No hay evolutivos registrados.

Usa /nuevo-evolutivo "HV-XX: Título" para crear uno.
```

### 5. Variantes del Comando

#### /equipo resumen
Muestra solo el conteo rápido:
```
📊 RESUMEN RÁPIDO
   👥 Miembros: {0}
   ⏳ Pendientes: {0}
   🔵 En progreso: {0} ({1})
   ✅ Completados: {0}
```

#### /equipo detalle
Muestra información completa incluyendo notas y fechas estimadas.

## Archivos Relacionados

- _duran/ESTADO_PROYECTO.json - Fuente de datos
- /nuevo-evolutivo - Crear evolutivos
- /tomar - Autoasignarse evolutivo
- /asignar - Asignar a otro miembro

## Notas

- El comando NO modifica ningún archivo, solo lee y muestra
- Intenta detectar usuario Git para resaltar sus evolutivos
- Los evolutivos del usuario actual se muestran destacados
