---
globs:
  - "**/*.vspscc"
  - "**/*.vssscc"
  - "**/\$tf/**"
  - "**/*.tpattributes"
---

# Reglas para TFVC (Team Foundation Version Control)

> ⚠️ **PROYECTO EN TFVC DETECTADO**
> Este proyecto usa Team Foundation Version Control en lugar de Git.
> Algunos comandos de control de versiones funcionan de forma diferente.

---

## DETECCIÓN DE TFVC

Este archivo se activa cuando Claude detecta:
- Archivos `.vspscc` o `.vssscc` (bindings de Source Control)
- Carpeta `$tf` (metadata de TFS)
- Archivos `.tpattributes`

---

## COMANDOS ADAPTADOS PARA TFVC

### /commit en TFVC

En lugar de ejecutar comandos Git automáticamente, Claude debe:

1. **Generar el mensaje de commit** siguiendo Conventional Commits
2. **Mostrar instrucciones manuales** para el desarrollador:

```
📋 CHECKIN EN TFVC

Tu mensaje de commit generado:
feat(HV-18): añadir filtro de becas por fecha y estado

Para hacer checkin, ejecuta en Developer Command Prompt:

  tf checkin /comment:"feat(HV-18): añadir filtro de becas por fecha y estado" /noprompt

O desde Visual Studio:
  1. Team Explorer → Pending Changes
  2. Pegar el mensaje en Comment
  3. Click en "Check In"
```

### /sync en TFVC

En lugar de `git pull/push`, mostrar:

```
📋 SINCRONIZAR EN TFVC

Para obtener últimos cambios:
  tf get /recursive

Para subir tus cambios:
  tf checkin /comment:"mensaje" /noprompt

Desde Visual Studio:
  1. Team Explorer → Pending Changes
  2. "Get Latest" para obtener cambios
  3. "Check In" para subir
```

### Shelvesets (equivalente a git stash)

```
📋 SHELVESETS EN TFVC

Guardar cambios temporalmente (como git stash):
  tf shelve "nombre-shelveset" /noprompt

Recuperar cambios:
  tf unshelve "nombre-shelveset"

Listar shelvesets:
  tf shelvesets /owner:*
```

---

## FUNCIONALIDADES COMPLETAS EN TFVC

Estos comandos funcionan **exactamente igual** que en Git:

| Comando | Funcionalidad | Estado |
|---------|---------------|--------|
| `/onboarding` | Inicializar proyecto con CLAUDE.md y _duran/ | ✅ 100% |
| `/analizar` | Escanear código existente | ✅ 100% |
| `/nuevo-evolutivo` | Crear especificación de tarea | ✅ 100%* |
| `/estado` | Ver progreso del evolutivo | ✅ 100% |
| `/pausar` | Pausar evolutivo actual | ✅ 100% |
| `/continuar` | Retomar evolutivo | ✅ 100% |
| `/finalizar-evolutivo` | Cerrar y documentar | ✅ 100% |
| `/test` | Generar tests unitarios | ✅ 100% |
| `/revision` | Code review | ✅ 100% |
| `/documentar` | Generar documentación | ✅ 100% |
| `/equipo` | Ver estado del equipo | ✅ 100% |
| `/tomar` | Asignarme evolutivo | ✅ 100% |
| `/liberar` | Devolver evolutivo | ✅ 100% |
| `/prepara-entrega` | Checklist pre-entrega | ✅ 100% |
| `/sos` | Ayuda rápida | ✅ 100% |

*`/nuevo-evolutivo`: No crea ramas automáticamente en TFVC. El desarrollador debe crear la rama manualmente si es necesario.

---

## LIMITACIONES EN TFVC

### Gestión de Ramas

TFVC tiene un modelo de ramas diferente a Git:
- Las ramas son **carpetas** en el servidor (ej: `$/Proyecto/branches/feature-HV18`)
- No hay concepto de "rama local"
- Los merges son operaciones del servidor

**Recomendación:** Para evolutivos en TFVC, trabajar en la rama principal o crear rama manualmente:

```
tf branch $/Proyecto/Main $/Proyecto/branches/HV-18-filtro-becas
tf get $/Proyecto/branches/HV-18-filtro-becas /recursive
```

### Comandos Git deshabilitados

Los siguientes comandos mostrarán instrucciones manuales en lugar de ejecutarse:

| Comando | Alternativa TFVC |
|---------|------------------|
| `git commit` | `tf checkin` |
| `git push` | `tf checkin` (automático) |
| `git pull` | `tf get /recursive` |
| `git stash` | `tf shelve` |
| `git branch` | `tf branch` (en servidor) |
| `git merge` | `tf merge` |

---

## MIGRACIÓN A GIT

> 💡 **Recomendación:** Planificar migración a Git para aprovechar el 100% de las funcionalidades del paquete STIC.IA.

Beneficios de Git sobre TFVC:
- Ramas locales instantáneas
- Commits atómicos y rápidos
- Mejor integración con CI/CD moderno
- Comandos automáticos del paquete STIC.IA

---

## MENSAJE INFORMATIVO

Cuando Claude detecte TFVC, debe mostrar este mensaje al inicio de la sesión:

```
╔══════════════════════════════════════════════════════════════════╗
║  ⚠️  PROYECTO TFVC DETECTADO                                     ║
╠══════════════════════════════════════════════════════════════════╣
║  Los comandos de contexto y desarrollo funcionan normalmente.    ║
║  Los comandos /commit y /sync mostrarán instrucciones manuales.  ║
║                                                                  ║
║  Comandos disponibles: /onboarding, /analizar, /nuevo-evolutivo, ║
║  /estado, /test, /revision, /documentar, /equipo, y más.         ║
║                                                                  ║
║  💡 Considera migrar a Git para funcionalidad completa.          ║
╚══════════════════════════════════════════════════════════════════╝
```
