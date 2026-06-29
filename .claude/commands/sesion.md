---
description: Actualiza _duran/SESION_ACTUAL.md con un resumen rápido de la sesión actual
---

Actualiza _duran/SESION_ACTUAL.md con un resumen rápido de la sesión actual

Actualiza el archivo `_duran/SESION_ACTUAL.md` con un resumen rápido de la sesión actual.

---

## Cuándo usar

- **Al final de cada sesión de trabajo**
- **Antes de cerrar Claude Code**
- **Cuando cambies de tarea y quieras guardar contexto**
- **Antes de que otro usuario continúe el trabajo**

---

## Instrucciones para Claude

Al ejecutar `/sesion`, Claude debe:

### 1. Analizar la sesión actual

Revisar la conversación y extraer:
- Qué se trabajó (resumen breve)
- Archivos modificados/creados
- Decisiones importantes
- Tareas completadas
- Tareas pendientes
- Evolutivo activo (si hay)
- Lecciones aprendidas (patrones útiles, errores corregidos, particularidades descubiertas)

### 2. Leer el archivo actual

Leer `_duran/SESION_ACTUAL.md` para:
- Obtener el historial reciente existente
- Preservar información que sigue siendo válida

### 3. Actualizar SESION_ACTUAL.md

Actualizar el archivo manteniendo la estructura pero con información actualizada:

- **Fecha: Fecha de hoy**
- **Usuario: Detectar de contexto o preguntar**
- **Evolutivo activo: Del ESTADO_PROYECTO.json o "ninguno"**
- **Resumen: Lo trabajado en esta sesión**
- **Archivos: Lista de archivos tocados**
- **Decisiones: Decisiones importantes tomadas**
- **Pendientes: Tareas que quedan por hacer**
- **Historial: Añadir entrada al historial reciente (máximo 5 entradas)**

### 3b. Actualizar LECCIONES.md (si aplica)

Si durante la sesión se descubrió algo relevante, actualizar `_duran/LECCIONES.md`:

- **Patrón útil → Añadir en sección "Patrones del Proyecto"**
- **Error corregido no trivial → Añadir en sección "Errores Corregidos"**
- **Particularidad técnica → Añadir en sección "Particularidades Técnicas"**
- **Preferencia del equipo → Añadir en sección "Preferencias del Equipo"**
- **Actualizar Top 5 si la nueva lección es más relevante que las existentes**

> Solo añadir lecciones que aporten valor para futuras sesiones. No documentar lo trivial.

### 4. Confirmar al usuario

```
╔═══════════════════════════════════════════════════════════╗
║  💾 SESIÓN GUARDADA                                        ║
╚═══════════════════════════════════════════════════════════╝

Archivo: _duran/SESION_ACTUAL.md

Resumen guardado:
- Fecha: [YYYY-MM-DD]
- Evolutivo: [HV-XX o ninguno]
- Archivos tocados: [N]
- Tareas completadas: [N]
- Pendientes: [N]
- Lecciones: [N nuevas o "sin cambios"]

La próxima sesión comenzará con este contexto cargado.
```

---

## Diferencia con /acta

| Aspecto | /sesion | /acta (si existe) |
|---------|---------|-------------------|
| **Propósito** | Contexto rápido para continuar | Documentación completa |
| **Archivo** | `_duran/SESION_ACTUAL.md` | Actas detalladas |
| **Detalle** | Resumen ejecutivo | Detalle completo |
| **Cuándo** | Siempre al terminar | Sesiones significativas |
| **Acumulativo** | Sobrescribe anterior | Crea nuevo archivo |

---

## Parámetros opcionales

- `/sesion` - Actualización normal
- `/sesion quick` - Solo actualiza fecha y pendientes
- `/sesion handoff "nombre"` - Indica que otro usuario continuará

---

## Ejemplo de uso

```
Usuario: /sesion

Claude:
╔═══════════════════════════════════════════════════════════╗
║  💾 SESIÓN GUARDADA                                        ║
╚═══════════════════════════════════════════════════════════╝

Archivo: _duran/SESION_ACTUAL.md

Resumen:
- Evolutivo: HV-15
- Trabajado: Implementación de filtros en BecasController
- Archivos: 3 modificados
- Completado: Endpoint GET con filtros
- Pendientes: Tests unitarios, documentación

¿Necesitas algo más antes de cerrar?
```

---

## Integración con otros comandos

- `/pausar` → Debería ejecutar `/sesion` automáticamente
- `/finalizar-evolutivo` → Debería ejecutar `/sesion` automáticamente
- `/continuar` → Lee `SESION_ACTUAL.md` y `LECCIONES.md` para recuperar contexto
- `_duran/LECCIONES.md` → Se actualiza en paso 3b si hay lecciones nuevas

---

## Flujo recomendado

```
Al terminar de trabajar:
1. Guardar archivos pendientes
2. Ejecutar /sesion
3. (Opcional) Ejecutar /acta si fue sesión significativa
4. Cerrar Claude Code

Al empezar nueva sesión:
1. Claude lee CLAUDE.md → importa SESION_ACTUAL.md
2. Tienes contexto de dónde dejaste
3. Ejecutar /continuar si hay evolutivo pausado
```
