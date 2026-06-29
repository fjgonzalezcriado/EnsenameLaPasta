---
description: Modo SOS de emergencia - captura estado actual del proyecto para recuperacion posterior
---

Muestra ayuda contextual segun el estado del proyecto

# 🚨 Modo SOS - Emergencia

## Contexto

Comando de emergencia para capturar el estado actual del proyecto y la sesion de trabajo cuando algo va mal y el desarrollador necesita pausar de forma abrupta sin perder contexto. Guarda toda la informacion relevante en un archivo de recuperacion que `/recuperar` puede leer despues para retomar el trabajo.

## Ejecucion Inmediata

### 1. Capturar estado actual

```
🚨 SOS — captura de emergencia
═══════════════════════════════
```

Claude debe detectar:
- Evolutivo activo (de `_duran/ESTADO_PROYECTO.json.evolutivoActivo`)
- Rama git actual
- Archivos modificados sin commit
- Ultima accion realizada en esta sesion
- Resumen breve de la conversacion (ultimos intercambios)

### 2. Guardar en JSON

Actualizar `_duran/ESTADO_PROYECTO.json` anadiendo bloque `emergencia`:

```json
{
  "emergencia": {
    "fecha": "[FECHA_HORA]",
    "tipo": "sos",
    "evolutivoActivo": "[detectado]",
    "ultimaAccion": "[lo que se estaba haciendo]",
    "contextoConversacion": "[resumen breve]"
  }
}
```

### 3. Crear archivo de recuperacion

Crear `_duran/SOS_[FECHA].md`:

```markdown
# 🚨 SOS - [FECHA_HORA]

## Estado en este momento

### Evolutivo activo
- ID: [EVO-XXX]
- Nombre: [nombre]
- Progreso: [X]%

### Ultima accion
[Descripcion de lo que se estaba haciendo]

### Conversacion relevante
[Resumen de los ultimos intercambios importantes]

### Archivos implicados
- archivo1.cs
- archivo2.cs

### Siguiente paso sugerido
[Lo que se deberia hacer al retomar]

## Para recuperar
Ejecutar: `/recuperar`
```

### 4. Confirmacion Ultra-Rapida

```
✅ SOS guardado correctamente

📄 _duran/SOS_[FECHA].md

Para recuperar, ejecuta: /recuperar
```
