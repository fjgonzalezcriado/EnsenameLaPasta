Permite asignar un evolutivo a otro desarrollador del equipo

## Descripción
Permite al jefe de proyecto o a cualquier miembro asignar un evolutivo a otro desarrollador del equipo

## Uso
```
/asignar HV-XX @usuario
/asignar HV-05 @mlopez
/asignar HV-02 @jgarcia
```

## Instrucciones para Claude

Al recibir `/asignar {codigo} @{usuario}`, debes:

### 1. Validaciones Iniciales

```javascript
// 1. Leer estado
const estado = leerArchivo("_duran/ESTADO_PROYECTO.json");

// 2. Extraer parámetros
const codigo = extraerCodigo(input);     // "HV-05"
const usuario = extraerUsuario(input);   // "mlopez" (sin @)

// 3. Verificar que el usuario destino existe en el equipo
const miembroDestino = estado.equipo?.miembros?.find(m => m.usuario === usuario);
if (!miembroDestino) {
  // Error: usuario no está en el equipo
}

// 4. Buscar el evolutivo (en pendientes o enProgreso)
let evolutivo = estado.evolutivos.pendientes.find(e => e.codigo === codigo);
let origen = "pendientes";

if (!evolutivo) {
  evolutivo = estado.evolutivos.enProgreso.find(e => e.codigo === codigo);
  origen = "enProgreso";
}

if (!evolutivo) {
  // Error: evolutivo no existe o ya está completado
}
```

### 2. Flujo Principal

```
Usuario: /asignar HV-05 @mlopez

Claude: 📋 ASIGNAR EVOLUTIVO: HV-05

        Titulo: Implementar paginación en GetAll
        Estado actual: pendiente
        Asignado actual: (nadie)

        👤 Se asignará a: mlopez (María López)

        ¿Confirmar asignación? (s/n)

Usuario: s

Claude: ✅ Evolutivo {0} asignado a: {1}
        ✅ Estado cambiado a: en_progreso
        ✅ Rama asociada: feature/HV-05
        ✅ ESTADO_PROYECTO.json actualizado

        📧 Notificación pendiente para {0}:
           "Se te ha asignado el evolutivo {0}"

        El desarrollador deberá ejecutar::
           /continuar HV-05
```

### 3. Reasignación (ya tenía asignado)

```
Usuario: /asignar HV-03 @jgarcia

Claude: ⚠️ REASIGNACIÓN DE EVOLUTIVO

        Evolutivo: HV-03 - Refactor servicio pagos

        Asignado actual: mlopez (María López)
        Nuevo asignado:  jgarcia (Juan García)

        Estado: en_progreso
        Rama: feature/HV-03

        ¿Confirmar reasignación? (s/n)

Usuario: s

Claude: ✅ Evolutivo {0} reasignado de {1} a {2}
        ✅ ESTADO_PROYECTO.json actualizado

        ⚠️ IMPORTANTE:
        • {0} debe hacer commit/push de su trabajo actual
        • {0} debe hacer pull de {1}

        📧 Notificaciones pendientes:
           → mlopez: "{0} ha sido reasignado a {1}"
           → jgarcia: "Se te ha asignado {0} (reasignado)"
```

### 4. Actualizar ESTADO_PROYECTO.json

```javascript
// Si viene de pendientes, mover a enProgreso
if (origen === "pendientes") {
  estado.evolutivos.pendientes = estado.evolutivos.pendientes.filter(e => e.codigo !== codigo);

  evolutivo.estado = "en_progreso";
  evolutivo.asignadoA = usuario;
  evolutivo.rama = `feature/${codigo}`;
  evolutivo.fechaInicio = fechaHoy();

  estado.evolutivos.enProgreso.push(evolutivo);
}

// Si ya estaba en progreso, solo actualizar asignado
if (origen === "enProgreso") {
  const idx = estado.evolutivos.enProgreso.findIndex(e => e.codigo === codigo);
  estado.evolutivos.enProgreso[idx].asignadoA = usuario;
  // Mantener la rama existente
}

// Guardar
guardarArchivo("_duran/ESTADO_PROYECTO.json", estado);
```

### 5. Casos de Error

#### ERROR: Usuario no encontrado:
```
❌ ERROR: Usuario no encontrado

El usuario "{0}" no está en el equipo del proyecto

👥 Miembros disponibles:
   • jgarcia (Juan García) - desarrollador
   • mlopez (María López) - desarrollador
   • aruiz (Ana Ruiz) - jefe_proyecto

Uso: /asignar HV-05 @jgarcia
```

#### ERROR: Evolutivo no encontrado:
```
❌ ERROR: Evolutivo no encontrado

El evolutivo {0} no existe

📋 Evolutivos disponibles para asignar:

   Pendientes:
   • HV-02: Validación ISO 8601 fechas
   • HV-04: Endpoint bulk insert

   En progreso (reasignar):
   • HV-01: Campos auditoría (asignado: jgarcia)
```

#### ERROR: Evolutivo ya completado:
```
❌ ERROR: Evolutivo ya completado

El evolutivo {0} ya está completado y no puede reasignarse

Fecha completado: 2026-01-18
Completado por: jgarcia

Si necesitas reabrir este evolutivo, usa::
   /reabrir HV-00
```

#### FALTA USUARIO:
```
⚠️ FALTA USUARIO

Uso correcto: /asignar HV-XX @usuario

Ejemplo: /asignar HV-05 @mlopez

👥 Miembros disponibles:
   • jgarcia
   • mlopez
   • aruiz
```

### 6. Sin Parámetros (ayuda)

```
Usuario: /asignar

Claude: 📋 ASIGNAR EVOLUTIVO

        Uso: /asignar HV-XX @usuario

        Ejemplos:
           /asignar HV-05 @mlopez
           /asignar HV-02 @jgarcia

        📋 Evolutivos disponibles:

        Pendientes (sin asignar):
        ┌──────────┬──────────────────────────────────────┐
        │ Código   │ Titulo                           │
        ├──────────┼──────────────────────────────────────┤
        │ HV-02    │ Validación ISO 8601 fechas       │
        │ HV-04    │ Endpoint bulk insert             │
        └──────────┴──────────────────────────────────────┘

        En progreso (reasignar):
        ┌──────────┬─────────────────────────┬───────────┐
        │ Código   │ Titulo                  │ Asignado a  │
        ├──────────┼─────────────────────────┼───────────┤
        │ HV-01    │ Campos auditoría        │ jgarcia   │
        │ HV-03    │ Refactor pagos          │ mlopez    │
        └──────────┴─────────────────────────┴───────────┘

        👥 Miembros:
           @jgarcia, @mlopez, @aruiz
```

### 7. Asignación Masiva (opcional, avanzado)

```
Usuario: /asignar HV-05,HV-06,HV-07 @mlopez

Claude: 📋 ASIGNACIÓN MÚLTIPLE

        Se asignarán {0} evolutivos a {1}:

        • HV-05: Implementar paginación
        • HV-06: Cache de consultas
        • HV-07: Logs estructurados

        ¿Confirmar asignación de los {0} evolutivos? (s/n)
```

## Archivos Relacionados

- `_duran/ESTADO_PROYECTO.json` - Se actualiza
- `/equipo` - Ver estado del equipo
- `/tomar` - Autoasignarse evolutivo
- `/liberar` - Liberar evolutivo

## Notas

- Cualquier miembro puede asignar (no solo jefe_proyecto)
- Se puede reasignar evolutivos en progreso
- La rama Git no se crea automáticamente, el desarrollador debe hacerlo con /continuar
- Se recomienda notificar al desarrollador por otro medio (email, chat)
