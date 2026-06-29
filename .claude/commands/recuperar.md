Recupera contexto después de emergencia (SOS/CP)

# Recuperación Post-Emergencia

## Contexto
Retomar trabajo después de un cierre de emergencia (/sos o /cp).

## Tareas a Ejecutar

### 1. Buscar Archivos de Emergencia

```powershell
# Buscar archivos SOS
Get-ChildItem "_duran/SOS_*.md" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
```

### 2. Si hay archivo SOS

Leer el archivo y mostrar:

```
📄 RECUPERANDO SESIÓN DE EMERGENCIA
═════════════════════════════════

📅 SOS guardado: {0}

[Contenido del archivo SOS]

¿Retomar desde este punto? (s/n)
```

### 3. Si hay info en ESTADO_PROYECTO.json

Leer sección `emergencia`:

```
📄 RECUPERANDO DESDE ESTADO
═════════════════════════════════

📅 Emergencia: {0}
📋 Evolutivo: {0}
📝 Última acción: {0}

¿Continuar? (s/n)
```

### 4. Si el usuario pegó texto de /cp

Detectar si el input contiene "RECUPERAR_":

```
📋 DATOS DE RECUPERACIÓN DETECTADOS

Procesando información pegada...

- Evolutivo: {0}
- Haciendo: {0}
- Archivos: {0}
- Próximo: {0}

¿Es correcto? (s/n)
```

### 5. Restaurar Contexto

- Actualizar `ESTADO_PROYECTO.json` con info recuperada
- Marcar archivo SOS como procesado (renombrar a `SOS_*.md.recovered`)

### 6. Limpiar

```
🧹 ¿Eliminar archivo de emergencia? (s/n)
```

### 7. Continuar

```
✅ RECUPERACIÓN COMPLETA
═════════════════════════════════

📋 Evolutivo: {0}
🎯 Retomando: {0}

¿Comenzamos?
```

### 8. Si no hay nada que recuperar

```
🔭 No se encontraron datos de emergencia.

Opciones:
1. /continuar - Retomar sesión normal
2. /estado - Ver estado del proyecto
3. Pegar texto de /cp manualmente
```
