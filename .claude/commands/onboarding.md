---
description: Wizard guiado de 8 fases para contextualizar proyecto existente + integración en Visual Studio
argument-hint: "[--section <branching|equipo|infraestructura|...>]"
---

Wizard guiado de 8 fases para contextualizar proyecto existente + integración en Visual Studio

---

## Sintaxis

```
/onboarding                          # Wizard completo (8 fases)
/onboarding --section branching      # Solo re-ejecuta la fase de branching (pasos 8-14)
/onboarding --section <fase>         # Re-ejecuta solo la fase indicada sin tocar el resto
```

**Secciones disponibles para re-ejecución parcial** (idempotentes, no sobrescriben otras secciones):
- `informacion_general`, `stack_tecnologico`, `estructura_codigo`, `base_datos`, `integraciones`, `infraestructura_despliegue`, `autenticacion`, `funcionalidades`, `dependencias`, `tests_existentes`, `documentacion_existente`, `equipo`, `problemas_conocidos`, **`branching`** (pasos 8-14).

> **Para SOLO cambiar branching** sin tocar el resto: usar `/branching` (comando dedicado, más rápido y con validación de coherencia). `/onboarding --section branching` es equivalente pero pasa por el wizard.

---

# 🧠 Extended Thinking Mode

**think hard** - Analiza cuidadosamente la estructura del proyecto antes de configurar.

---

# ⚠️ INSTRUCCIONES CRÍTICAS PARA CLAUDE CODE ⚠️

## REGLAS QUE DEBES SEGUIR

0. **DETECTAR FLAG `--section <nombre>`** al inicio. Si está presente:
   - Validar que `<nombre>` está en la lista de secciones disponibles (informacion_general, stack_tecnologico, estructura_codigo, base_datos, integraciones, infraestructura_despliegue, autenticacion, funcionalidades, dependencias, tests_existentes, documentacion_existente, equipo, problemas_conocidos, **branching**).
   - **SALTAR todas las otras fases**. Re-ejecutar SOLO la fase indicada.
   - Para `--section branching`: ejecutar SOLO los pasos 8-14 (estrategia, mergeStrategy, convencionRamas, ramaBase, tiposTarea, pushFeatures, branchPolicies). Preservar el resto de `_duran/ESTADO_PROYECTO.json` intacto (leer JSON, modificar SOLO la rama `configuracion.branching`, escribir de vuelta sin BOM).
   - **NO ejecutar el script PowerShell de la Fase Final** en modo `--section` (no es onboarding completo, es reconfiguración parcial).
   - **NO modificar `onboarding.pasos_completados`** en modo `--section` (la fase ya estaba completada en el onboarding original).
   - Mensaje final: `[OK] Sección {nombre} re-configurada. Resto del onboarding intacto.`
1. **HACER CADA PREGUNTA** y esperar respuesta del usuario
2. **NO SALTARSE NINGUNA PREGUNTA** - si el usuario no sabe, guardar "Por definir"
3. **CREAR LOS ARCHIVOS** listados en cada fase si no existen
4. **AL FINALIZAR LAS 8 FASES: EJECUTAR EL SCRIPT POWERSHELL** de la Fase Final (solo si NO se pasó `--section`)
5. **VERIFICAR EL CHECKLIST** antes de marcar como completado
6. **SEMÁNTICA DE `pasos_completados`** - Los campos booleanos en `onboarding.pasos_completados` del JSON tienen DOS tipos:
   - **Campos de FASE** (`informacion_general`, `stack_tecnologico`, `estructura_codigo`, `base_datos`, `integraciones`, `infraestructura_despliegue`, `funcionalidades`, `dependencias`, `equipo`, `problemas_conocidos`): Se ponen a `true` cuando la fase se completa (las preguntas fueron respondidas).
   - **Campos de EXISTENCIA** (`tests_existentes`, `documentacion_existente`, `autenticacion`): Se ponen a `true` SOLO si el recurso EXISTE en el proyecto. Si el usuario dice "no hay tests" → `tests_existentes: false`. Si dice "no hay autenticación" → `autenticacion: false`. **NO confundir "pregunta respondida" con "recurso existe".**

## INDICADORES VISUALES

| Estado | Emoji | Uso |
|--------|-------|-----|
| Completado | ✅ | Fase/pregunta completada |
| Pendiente | ⬜ | Pregunta por hacer |
| Crítico | ⚠️ | NO SALTARSE |
| Error | ❌ | Problema detectado |

---

# Comando: /onboarding

## Pre-requisitos

```
═══════════════════════════════════════════════════════════════════
                      🔍 VERIFICANDO PRE-REQUISITOS
═══════════════════════════════════════════════════════════════════

┌─────────────────────────────────────────────────────────────────┐
│ 📄 CLAUDE.md           [✅ Existe | ❌ Ejecutar /init]          │
│ 📁 03_Desarrollo/      [✅ Existe | ❌ Ejecutar arranque.ps1]   │
│ 📋 ESTADO_PROYECTO     [✅ Existe | 🆕 Se creará]               │
│ 📦 Solución            [.sln | .slnx (NET 8+)]                  │
└─────────────────────────────────────────────────────────────────┘
```

> **Nota**: Claude detecta tanto `.sln` como `.slnx` (formato XML disponible en .NET 8+ / VS 2022 17.10+)

---

## FASE 0: Verificar @imports

Verificar CLAUDE.md. Si no tiene los @imports, AÑADIRLOS al principio:

```markdown
@import .claude/CLAUDE_BASE_COMILLAS.md
@import _duran/ESTADO_PROYECTO.json
```

---

## FASE 1: Información General del Proyecto

```
═══════════════════════════════════════════════════════════════════
                 📋 FASE 1: INFORMACIÓN GENERAL (1/8)
═══════════════════════════════════════════════════════════════════

Progreso: ██░░░░░░░░  12%

┌─────────────────────────────────────────────────────────────────┐
│ 🏢 DATOS DEL PROYECTO                                           │
├─────────────────────────────────────────────────────────────────┤
│ ⬜ 1. ¿Nombre del proyecto?                                     │
│ ⬜ 2. ¿Descripción breve (1-2 líneas)?                          │
│ ⬜ 3. ¿Quién es el OWNER? (área/departamento)                   │
│ ⬜ 4. ¿Fecha aproximada de inicio?                              │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 👥 ROLES DEL PROYECTO                                           │
├─────────────────────────────────────────────────────────────────┤
│ ⚠️ 5. ¿Quién es el JEFE DE PROYECTO (JP)?                       │
│ ⚠️ 6. ¿Quién es el LÍDER TÉCNICO (LT)?                          │
│ ⬜ 7. ¿Quién es el RESPONSABLE TÉCNICO (RT)?                    │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 🔗 REPOSITORIO                                                  │
├─────────────────────────────────────────────────────────────────┤
│ ⬜ 8. ¿Tipo VCS? (Git/TFS) - Detectar automáticamente           │
│ ⬜ 9. ¿URL o nombre del repositorio?                            │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ ⚡ CRITICIDAD Y PLAZOS                                          │
├─────────────────────────────────────────────────────────────────┤
│ ⬜ 10. ¿Criticidad para el negocio? (alta/media/baja)           │
│       Alta = caída afecta a usuarios/operaciones                │
│       Media = impacto limitado, workaround posible              │
│       Baja = herramienta interna no crítica                     │
│ ⬜ 11. ¿Tiene SLA? (ej: 99.9%) ¿Horario mantenimiento?         │
│ ⬜ 12. ¿Hay deadline próximo o freeze periods?                  │
│ ⬜ 13. ¿Ciclo de releases? (continuo/semanal/quincenal/sprint)  │
└─────────────────────────────────────────────────────────────────┘
```

**GUARDAR en** `_duran/ESTADO_PROYECTO.json` → secciones `proyecto`, `criticidad`, `calendario`

---

## FASE 2: Stack Tecnológico

```
═══════════════════════════════════════════════════════════════════
                  🔧 FASE 2: STACK TECNOLÓGICO (2/8)
═══════════════════════════════════════════════════════════════════

Progreso: ████░░░░░░  25%

┌─────────────────────────────────────────────────────────────────┐
│ 🔍 DETECCIÓN AUTOMÁTICA (leer .csproj y código en 03_Desarrollo/)│
├─────────────────────────────────────────────────────────────────┤
│ Detectar en .csproj: TargetFramework, PackageReferences, Sdk    │
│                                                                  │
│ Detectar en código (buscar en Program.cs, Startup.cs):           │
│  - AddMicrosoftIdentityWebApp → Azure AD / Entra ID             │
│  - AddAuthentication + AddJwtBearer → JWT                        │
│  - AddAuthentication + AddCookie → Cookie Auth                   │
│  - UseWindowsAuthentication → Windows Auth                       │
│  - [Authorize] sin config → Revisar tipo                         │
│  - Ninguno de los anteriores → Sin autenticación                 │
│                                                                  │
│ Detectar NuGet feeds privados:                                   │
│  - Buscar nuget.config con <packageSources> custom               │
│  - Buscar Directory.Packages.props (CPM)                         │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 🔧 CONFIRMAR STACK                                              │
├─────────────────────────────────────────────────────────────────┤
│ ⬜ 1. Framework .NET: [detectado] ¿Correcto?                    │
│ ⬜ 2. Tipo proyecto: [detectado] ¿Correcto?                     │
│ ⬜ 3. ORM: [detectado] ¿Correcto?                               │
│ ⬜ 4. ¿Frontend? (Razor/Angular/React/Blazor/Ninguno)           │
│ ⬜ 5. ¿Base de datos? (SQL Server/Oracle/PostgreSQL)            │
│ ⬜ 6. ¿Formato salida? (JSON/XML/HTML)                          │
└─────────────────────────────────────────────────────────────────┘
```

**GUARDAR en** `_duran/ESTADO_PROYECTO.json` → sección `stack`
**CREAR/ACTUALIZAR** `_duran/DEPENDENCIAS.md`

---

## FASE 3: Estructura del Código

```
═══════════════════════════════════════════════════════════════════
                📁 FASE 3: ESTRUCTURA DEL CÓDIGO (3/8)
═══════════════════════════════════════════════════════════════════

Progreso: ██████░░░░  37%

┌─────────────────────────────────────────────────────────────────┐
│ 🔍 DETECCIÓN AUTOMÁTICA DE ARQUITECTURA                         │
├─────────────────────────────────────────────────────────────────┤
│ Claude debe buscar en la estructura de carpetas:                 │
│  - Domain/, Application/, Infrastructure/ → Clean Architecture  │
│  - BLL/, DAL/, Models/ → N-Capas                                │
│  - Features/ con Commands/ y Queries/ → Vertical Slices + CQRS  │
│  - Controllers/ + Services/ + Data/ → MVC tradicional            │
│  - Múltiples .sln o docker-compose → Microservicios              │
│                                                                  │
│ Presentar lo detectado y CONFIRMAR con el usuario.               │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 📁 ARQUITECTURA                                                 │
├─────────────────────────────────────────────────────────────────┤
│ ⬜ 1. Patrón detectado: [auto] ¿Correcto?                       │
│      (N-Capas/Clean/Hexagonal/MVC/CQRS/Microservicios)          │
│ ⬜ 2. ¿Convenciones de naming específicas?                      │
│ ⬜ 3. ¿Carpeta principal del código de negocio?                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## FASE 4: Base de Datos

```
═══════════════════════════════════════════════════════════════════
                    💾 FASE 4: BASE DE DATOS (4/8)
═══════════════════════════════════════════════════════════════════

Progreso: ████████░░  50%

┌─────────────────────────────────────────────────────────────────┐
│ 💾 CONFIGURACIÓN DE BD                                          │
├─────────────────────────────────────────────────────────────────┤
│ ⬜ 1. ¿Nombre de la base de datos?                              │
│ ⬜ 2. ¿Servidor de BD? (desarrollo)                             │
│ ⬜ 3. ¿Cuántas tablas principales? (aprox)                      │
│ ⬜ 4. ¿Usa Stored Procedures? [S/N] ¿Cuántos?                   │
│ ⬜ 5. ¿Sistema de migraciones?                                  │
│      (EF Migrations/Scripts manuales/Flyway/Ninguno)            │
└─────────────────────────────────────────────────────────────────┘
```

**GUARDAR en** `_duran/ESTADO_PROYECTO.json` → sección `acceso_bd`

---

## FASE 5a: Integraciones Externas

```
═══════════════════════════════════════════════════════════════════
              🔗 FASE 5a: INTEGRACIONES EXTERNAS (5/8)
═══════════════════════════════════════════════════════════════════

Progreso: ██████████░  55%

┌─────────────────────────────────────────────────────────────────┐
│ 🔗 SISTEMAS EXTERNOS                                            │
├─────────────────────────────────────────────────────────────────┤
│ ⬜ 1. ¿Consume APIs externas? [S/N] ¿Cuáles?                    │
│ ⬜ 2. ¿Expone APIs? [S/N] ¿Tipo? (REST/SOAP/GraphQL)            │
│ ⬜ 3. ¿Sistema de autenticación? (auto-detectado en Fase 2)     │
│      Confirmar: [detectado] ¿Correcto?                          │
│      (Azure AD/ADFS/JWT/Certificados/Basic Auth/Ninguno)        │
│ ⬜ 4. ¿Integraciones con otros sistemas de Comillas?            │
│      (Banner, Oracle HCM, Sigma, SharePoint, M365...)           │
│ ⬜ 5. ¿Otros proyectos dependen de este? ¿Este depende de otros?│
└─────────────────────────────────────────────────────────────────┘
```

**GUARDAR en** `_duran/ESTADO_PROYECTO.json`:

- Sección `integraciones` con los datos recogidos
- **`onboarding.pasos_completados.integraciones`**: `true` (la fase fue completada)
- **`onboarding.pasos_completados.autenticacion`**: `true` SOLO si el usuario indica que tiene un sistema de autenticación (Azure AD, ADFS, JWT, etc.). Si responde "Ninguno" → `false`
- **`criticidad.dependencias_inter_proyecto`**: lista de dependencias si las hay

**ACTUALIZAR** `_duran/DEPENDENCIAS.md`

---

## FASE 5b: Infraestructura de Despliegue

```
═══════════════════════════════════════════════════════════════════
           🖥️ FASE 5b: INFRAESTRUCTURA DE DESPLIEGUE (5/8)
═══════════════════════════════════════════════════════════════════

Progreso: ████████████░  62%

┌─────────────────────────────────────────────────────────────────┐
│ 🖥️ ENTORNOS DE DESPLIEGUE                                       │
├─────────────────────────────────────────────────────────────────┤
│ ⬜ 1. ¿Cuántos entornos tiene? (Dev/Pre/Pro)                    │
│ ⬜ 2. Para CADA entorno:                                        │
│      - Nombre del servidor (ej: trapster01, stryfe01)            │
│      - URL de acceso (ej: devwww.comillas.edu)                   │
│      - ¿Deploy automático o manual?                              │
│ ⬜ 3. ¿Hay balanceo de carga en producción? [S/N]               │
│      Si sí: ¿Cuántos nodos? ¿Nombres?                           │
│ ⬜ 4. ¿Es aplicación web pública o servicio interno?             │
│      Público → servidores tipo Stryfe                            │
│      Interno → servidores tipo Storm                             │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 🔄 CI/CD Y PIPELINE                                             │
├─────────────────────────────────────────────────────────────────┤
│ ⬜ 5. ¿Plataforma CI/CD? (Azure Pipelines/manual/otra)          │
│ ⬜ 6. ¿Existe pipeline YAML? [S/N]                              │
│      Si sí: buscar azure-pipelines.yml en el repo               │
│ ⬜ 7. ¿Deployment Groups configurados? [S/N]                    │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 🌿 ESTRATEGIA DE BRANCHING                                      │
├─────────────────────────────────────────────────────────────────┤
│ ⬜ 8. ¿Cuántos desarrolladores trabajan en el repositorio?       │
│      (1 / 2-5 / 6+)                                             │
│ ⬜ 9. ¿Estrategia de ramificación?                              │
│                                                                  │
│      📌 RECOMENDACIÓN AUTOMÁTICA (según tamaño equipo):          │
│      • 1 dev + sin CI/CD → developer-branch (rama personal)     │
│      • 1 dev + con CI/CD → github-flow-simplificado (--no-ff)   │
│      • 2-5 devs → github-flow (PRs + squash merge)              │
│      • 6+ devs → release-flow o gitflow                         │
│                                                                  │
│      Opciones disponibles (9 estrategias principales):           │
│      ┌────────────────────────┬──────────────────────────────┐  │
│      │ github-flow            │ PRs + squash merge (default) │  │
│      │ github-flow-simplif.   │ Solo dev, --no-ff local      │  │
│      │ developer-branch       │ Rama personal + feats locales│  │
│      │ release-flow           │ Ramas release/* + PRs (MS)   │  │
│      │ trunk-based            │ Commits directos a main      │  │
│      │ oneflow                │ Una rama + release tags       │  │
│      │ gitflow                │ develop + release + hotfix    │  │
│      │ developer-flow         │ GitLab Flow simplificado     │  │
│      │ gitlab-flow            │ main + env branches (pre/pro)│  │
│      └────────────────────────┴──────────────────────────────┘  │
│      💡 Overlays orthogonales (combinar con la estrategia base):│
│         Ship/Show/Ask, Stacked PRs, Forking, Environment branch │
│                                                                  │
│ ⬜ 10. ¿Tipo de merge preferido?                                 │
│      • squash (historial limpio, 1 commit por feature)           │
│      • no-ff (preserva commits individuales, revertible)         │
│      • rebase (historial lineal, sin merge commits)              │
│      • merge (plain merge, sin flags - developer-branch)         │
│                                                                  │
│      💡 github-flow-simplificado usa --no-ff por defecto         │
│      💡 github-flow usa squash por defecto                       │
│      💡 developer-branch usa merge (plain) por defecto           │
│                                                                  │
│ ⬜ 11. ¿Nomenclatura de ramas?                                   │
│      • Semántica: feature/{codigo}-{descripcion} (default)       │
│      • Temporal: yyyyMMdd-{tipo}-{codigo}-{descripcion}          │
│      • Combinada: yyyyMMdd-{tipo}/{codigo}-{descripcion}         │
│      • Custom: definir formato personalizado                     │
│                                                                  │
│      💡 developer-branch recomienda nomenclatura temporal         │
│                                                                  │
│ ⬜ 12. ¿Rama base del proyecto?                                  │
│      • main (default)                                            │
│      • master                                                    │
│      • develop                                                   │
│      • dev.{usuario} (rama personal remota)                      │
│                                                                  │
│      💡 developer-branch usa dev.{usuario} como rama base         │
│                                                                  │
│ ⬜ 13. (Solo si nomenclatura temporal) ¿Tipos de tarea?          │
│      Ejemplo: DT=Deuda técnica, HV=Evolutivo, BUG=Corrección    │
│      (dejar vacío si no aplica)                                  │
│                                                                  │
│ ⬜ 14. ¿Se pushean las ramas feature al remoto? [S/N]            │
│      💡 developer-branch: No (features solo locales)              │
│      💡 github-flow: Sí (para PRs)                                │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 🔐 APROBADORES DE DEPLOY                                        │
├─────────────────────────────────────────────────────────────────┤
│ ⬜ 15. ¿Quién aprueba deploy a Pre? (nombre/rol o "nadie")      │
│ ⬜ 16. ¿Quién aprueba deploy a Pro? (nombre/rol)                │
│      ⚠️ Producción SIEMPRE requiere aprobador                    │
└─────────────────────────────────────────────────────────────────┘
```

**GUARDAR en** `_duran/ESTADO_PROYECTO.json`:

- Sección `infraestructura.entornos` con los datos de cada entorno
- Sección `infraestructura.cicd` con plataforma y estado del pipeline
- Sección `infraestructura.aprobadores` con los aprobadores por entorno
- Sección `configuracion.branching` con la estrategia elegida:
  - `branching.estrategia`: valor elegido (ej: `"github-flow-simplificado"`, `"developer-branch"`)
  - `branching.ramaBase`: rama base elegida (ej: `"main"`, `"dev.claudio"`)
  - `branching.mergeStrategy`: tipo de merge (ej: `"no-ff"`, `"squash"`, `"rebase"`, `"merge"`)
  - `branching.convencionRamas`: nomenclatura elegida (ej: `"feature/{codigo}-{descripcion}"`, `"yyyyMMdd-{tipo}-{codigo}-{descripcion}"`)
  - `branching.pushFeatures`: `true` si features se pushean, `false` si solo locales
  - `branching.tiposTarea`: objeto con tipos (ej: `{"DT":"Deuda técnica","HV":"Evolutivo"}`) o `null`
  - `branching.branchPolicies.reviewersMinimo`: `0` si github-flow-simplificado/developer-branch, `1` si github-flow
  - `branching.branchPolicies.buildValidation`: `true` siempre (excepto developer-branch sin pipeline: `false`)
- **`onboarding.pasos_completados.infraestructura_despliegue`**: `true` (la fase fue completada)

> **NOTA**: Si el usuario no conoce los detalles de infraestructura, guardar lo que sepa y marcar el resto como `null`. Se puede completar después con `/setup`.
> **NOTA**: Si el equipo es de 1 persona (detectado en pregunta 8):
> - Con pipeline CI/CD → recomendar `github-flow-simplificado` con `mergeStrategy: "no-ff"`
> - Sin pipeline CI/CD y rama `dev.{nombre}` detectada → recomendar `developer-branch` con `mergeStrategy: "merge"`, `pushFeatures: false`, `convencionRamas: "yyyyMMdd-{tipo}-{codigo}-{descripcion}"`

---

## FASE 6: Funcionalidades

```
═══════════════════════════════════════════════════════════════════
                   ⚙️ FASE 6: FUNCIONALIDADES (6/8)
═══════════════════════════════════════════════════════════════════

Progreso: ████████████░  75%

┌─────────────────────────────────────────────────────────────────┐
│ ⚙️ PROPÓSITO Y MÓDULOS                                          │
├─────────────────────────────────────────────────────────────────┤
│ ⬜ 1. ¿Propósito principal de la aplicación? (2-3 líneas)       │
│ ⬜ 2. ¿Módulos principales? (separados por coma)                │
│ ⬜ 3. ¿Funcionalidades críticas que no pueden fallar?           │
│ ⬜ 4. ¿Funcionalidades en desarrollo o planificadas?            │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 📚 GLOSARIO DEL DOMINIO                                         │
├─────────────────────────────────────────────────────────────────┤
│ ⬜ 5. ¿Términos de negocio específicos? (3-5 términos clave)    │
│      Ejemplo: "Beca = ayuda económica a estudiantes"            │
│      "Convocatoria = periodo de solicitud con fechas"           │
│      "Adjudicación = proceso de asignar becas"                  │
│                                                                 │
│      💡 Ayuda a Claude a entender el vocabulario del proyecto   │
└─────────────────────────────────────────────────────────────────┘
```

**CREAR/ACTUALIZAR** `_duran/FUNCIONALIDADES.md`
**ACTUALIZAR** `CLAUDE.md` → sección `## Glosario del Dominio`

---

## FASE 7: Tests y Documentación

```
═══════════════════════════════════════════════════════════════════
                 🧪 FASE 7: TESTS Y DOCUMENTACIÓN (7/8)
═══════════════════════════════════════════════════════════════════

Progreso: ██████████████░  87%

┌─────────────────────────────────────────────────────────────────┐
│ 🧪 TESTING                                                      │
├─────────────────────────────────────────────────────────────────┤
│ ⬜ 1. ¿Hay tests unitarios? [S/N] ¿Framework?                   │
│ ⬜ 2. ¿Cobertura de tests actual?                               │
│ ⬜ 3. ¿Hay tests de integración? [S/N]                          │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 📚 DOCUMENTACIÓN                                                │
├─────────────────────────────────────────────────────────────────┤
│ ⬜ 4. ¿Existe documentación técnica? [S/N] ¿Dónde?              │
│ ⬜ 5. ¿Existe documentación funcional? [S/N] ¿Dónde?            │
└─────────────────────────────────────────────────────────────────┘
```

**GUARDAR en** `_duran/ESTADO_PROYECTO.json`:

- **`metricas.cobertura_tests`**: valor indicado por el usuario (o `null` si no sabe)
- **`onboarding.pasos_completados.tests_existentes`**: `true` SOLO si el usuario confirma que SÍ hay tests unitarios o de integración. Si responde "no" a las preguntas 1 y 3 → `false`
- **`onboarding.pasos_completados.documentacion_existente`**: `true` SOLO si el usuario confirma que SÍ existe documentación técnica o funcional. Si responde "no" a las preguntas 4 y 5 → `false`

> ⚠️ **IMPORTANTE**: Estos campos indican si el proyecto TIENE tests/documentación, NO si la pregunta fue respondida. "No hay tests" = `false`, no `true`.

---

## FASE 8: Equipo y Problemas Conocidos

```
═══════════════════════════════════════════════════════════════════
                 👥 FASE 8: EQUIPO Y PROBLEMAS (8/8)
═══════════════════════════════════════════════════════════════════

Progreso: ████████████████░  95%

⚠️ PREGUNTAS CRÍTICAS - NO SALTARSE NINGUNA

┌─────────────────────────────────────────────────────────────────┐
│ 👥 EQUIPO DE DESARROLLO                                         │
├─────────────────────────────────────────────────────────────────┤
│ ⚠️ 1. ¿Desarrolladores del equipo? (nombres, o "Solo yo")       │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 📞 CONTACTOS DE ESCALADO                                        │
├─────────────────────────────────────────────────────────────────┤
│ ⚠️ 2. ¿Contacto escalado TÉCNICO? (problemas técnicos graves)   │
│ ⚠️ 3. ¿Contacto escalado FUNCIONAL? (reglas de negocio)         │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 🐛 PROBLEMAS CONOCIDOS                                          │
├─────────────────────────────────────────────────────────────────┤
│ ⚠️ 4. ¿Bugs conocidos pendientes?                               │
│ ⚠️ 5. ¿Deuda técnica identificada?                              │
│ ⬜ 6. ¿Problemas de rendimiento conocidos?                      │
│ ⬜ 7. ¿Código legacy que necesite atención?                     │
└─────────────────────────────────────────────────────────────────┘
```

**GUARDAR en** `_duran/ESTADO_PROYECTO.json` → sección `equipo`
**CREAR** `_duran/DEUDA_TECNICA.md` con las respuestas 4-7

---

## FASE 9 (opcional): Registro en MCP Server STIC.IA

```
═══════════════════════════════════════════════════════════════════
              🌐 FASE 9: HUB CENTRAL STIC.IA (opcional)
═══════════════════════════════════════════════════════════════════

¿Conectar este proyecto al hub central STIC.IA Comillas?

Beneficios:
  ✅ Aparece en el inventario ejecutivo (dashboard Direccion)
  ✅ Recibe avisos de versiones nuevas STIC.IA
  ✅ Knowledge sharing: tus decisiones/lecciones disponibles para
     otros proyectos consumidores (anonimo)

Privacidad (privacy.html):
  - Identificacion: solo alias git (NO email)
  - Telemetria: hash del prompt, NUNCA contenido
  - Retencion: 90d eventos / 12m heartbeats
  - Right to be forgotten: /mcp-forget en cualquier momento

¿Registrar ahora?
  [1] Si, registrar SIN telemetria de agents (solo inventario)
  [2] Si, registrar CON telemetria de agents (opt-in completo)
  [3] No, decidir mas tarde (puedes usar /mcp-register cuando quieras)
```

**Si el usuario elige 1 o 2**: ejecutar el comando `/mcp-register` con el modo elegido.
**Si elige 3**: dejar `mcpSync.habilitado = false` y continuar.

> NO bloquear el onboarding si el servidor MCP no responde. Mostrar warning y continuar.

---

## 🔴 FASE FINAL: Integración en Visual Studio

```
═══════════════════════════════════════════════════════════════════
             🔧 FASE FINAL: INTEGRACIÓN VISUAL STUDIO
═══════════════════════════════════════════════════════════════════

Progreso: ██████████████████  100%
```

### ⚠️ CLAUDE CODE: EJECUTAR OBLIGATORIAMENTE

**Si existe un archivo .sln o .slnx en 03_Desarrollo/, EJECUTAR este comando:**

```powershell
.\.claude\commands\integracion-vs.ps1
```

**IMPORTANTE:** 
- Este paso es OBLIGATORIO, no opcional
- Si el comando falla, informar al usuario del error
- Si no hay .sln, saltar este paso e informar al usuario

El script `integracion-vs.ps1` añade 8 Solution Folders al .sln:
- Contexto Claude (CLAUDE.md, ESTADO_PROYECTO.json, etc.)
- Especificaciones (_duran/specs/)
- Diagramas (01_Diseno/Arquitectura/)
- Gestion (00_Gestion/)
- Pruebas (04_Pruebas/)
- CI-CD (05_CICD/)
- Documentacion (06_Documentacion/)
- UAP (07_UAP/)

### DESPUÉS DE LA INTEGRACIÓN, MOSTRAR:

```
═══════════════════════════════════════════════════════════════════
                     ✅ ONBOARDING COMPLETADO
═══════════════════════════════════════════════════════════════════

┌─────────────────────────────────────────────────────────────────┐
│ 📋 PROYECTO                                                     │
├─────────────────────────────────────────────────────────────────┤
│ Nombre:       [nombre]                                          │
│ Descripción:  [descripción]                                     │
│ Owner:        [owner]                                           │
│ Inicio:       [fecha_inicio]                                    │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 👥 EQUIPO                                                       │
├─────────────────────────────────────────────────────────────────┤
│ 👨‍💼 Jefe Proyecto:       [jefe_proyecto]                         │
│ 👨‍💻 Líder Técnico:        [lider_tecnico]                        │
│ 🔧 Responsable Técnico:  [responsable_tecnico]                  │
│ 👷 Desarrolladores:      [desarrolladores]                      │
│                                                                 │
│ 📞 Escalado Técnico:     [contacto_tecnico]                     │
│ 📞 Escalado Funcional:   [contacto_funcional]                   │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 🔧 STACK TECNOLÓGICO                                            │
├─────────────────────────────────────────────────────────────────┤
│ Framework:    [.NET X.0]                                        │
│ Tipo:         [tipo proyecto]                                   │
│ ORM:          [orm]                                             │
│ Frontend:     [frontend]                                        │
│ Auth:         [Azure AD/JWT/Ninguno - auto-detectado]           │
│ BD:           [base datos]                                      │
│ Arquitectura: [patrón - auto-detectado]                         │
│ VCS:          [Git|TFS]                                         │
│ Código:       03_Desarrollo/                                    │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 🖥️ INFRAESTRUCTURA                                              │
├─────────────────────────────────────────────────────────────────┤
│ Criticidad:   [alta/media/baja]                                 │
│ Entornos:     [Dev → Pre → Pro]                                 │
│ Producción:   [servidor(es)] [balanceo: sí/no]                  │
│ CI/CD:        [Azure Pipelines/manual]                          │
│ Aprobador Pro: [nombre/rol]                                     │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 📂 INTEGRACIÓN VISUAL STUDIO                                    │
├─────────────────────────────────────────────────────────────────┤
│ ✅ Contexto Claude       (_duran/)                           │
│ ✅ Especificaciones      (_duran/specs/)                     │
│ ✅ Diagramas             (01_Diseno/Arquitectura/)              │
│ ✅ Gestion               (00_Gestion/)                          │
│ ✅ Pruebas               (04_Pruebas/)                          │
│ ✅ CI-CD                 (05_CICD/)                             │
│ ✅ Documentacion         (06_Documentacion/)                    │
│ ✅ UAP                   (07_UAP/)                              │
│                                                                 │
│ 💾 Backup: 03_Desarrollo/[nombre].sln.bak                       │
└─────────────────────────────────────────────────────────────────┘

📁 ARCHIVOS CREADOS/ACTUALIZADOS:
   ✅ _duran/ESTADO_PROYECTO.json
   ✅ _duran/FUNCIONALIDADES.md
   ✅ _duran/DEPENDENCIAS.md
   ✅ _duran/DEUDA_TECNICA.md
   ✅ 03_Desarrollo/[nombre].sln (8 Solution Folders añadidos)

═══════════════════════════════════════════════════════════════════
🎯 PRÓXIMOS PASOS:
   1. Abrir Visual Studio para ver las nuevas carpetas
   2. /analizar - Análisis profundo de arquitectura
   3. /estado - Ver dashboard del proyecto
   4. /nuevo-evolutivo - Comenzar trabajo
═══════════════════════════════════════════════════════════════════
```

---

## ✅ CHECKLIST FINAL

**Claude Code: Verificar ANTES de finalizar:**

```
┌─────────────────────────────────────────────────────────────────┐
│ ✅ CHECKLIST ONBOARDING                                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ DATOS RECOGIDOS:                                                │
│ [ ] Nombre, descripción, owner, fecha                           │
│ [⚠️] Jefe Proyecto, Líder Técnico, Responsable Técnico          │
│ [ ] Criticidad, SLA, calendario                                 │
│ [ ] Stack tecnológico completo                                  │
│ [ ] Autenticación (auto-detectada y confirmada)                 │
│ [ ] Arquitectura (auto-detectada y confirmada)                  │
│ [ ] Base de datos configurada                                   │
│ [ ] Integraciones externas documentadas                         │
│ [ ] Infraestructura de despliegue (entornos, servidores, CI/CD) │
│ [ ] Aprobadores de deploy por entorno                           │
│ [ ] Funcionalidades principales                                 │
│ [ ] Glosario del dominio (términos de negocio)                  │
│ [⚠️] Equipo y desarrolladores                                   │
│ [⚠️] Contactos de escalado (técnico y funcional)                │
│ [⚠️] Deuda técnica y bugs conocidos                             │
│                                                                 │
│ ARCHIVOS CREADOS:                                               │
│ [ ] _duran/ESTADO_PROYECTO.json actualizado                  │
│ [ ] _duran/FUNCIONALIDADES.md creado                         │
│ [ ] _duran/DEPENDENCIAS.md creado                            │
│ [ ] _duran/DEUDA_TECNICA.md creado                           │
│                                                                 │
│ INTEGRACIÓN VS:                                                 │
│ [ ] Script PowerShell EJECUTADO                                 │
│ [ ] 8 Solution Folders añadidos al .sln                         │
│ [ ] Backup del .sln creado                                      │
│                                                                 │
│ ⚠️ = Campos críticos - PREGUNTAR SIEMPRE                        │
└─────────────────────────────────────────────────────────────────┘
```

**Si falta algo del checklist, COMPLETARLO antes de finalizar.**
