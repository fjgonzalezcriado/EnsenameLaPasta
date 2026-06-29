# /analisis-arquitectura - Auditoria arquitectonica formal

Genera un analisis arquitectonico exhaustivo del proyecto con entregables formales versionados (Markdown + HTML).

---

## REGLAS CRITICAS (LEER ANTES DE HACER NADA)

> **REGLA 1 - SIN SUBAGENTES**: NO lanzar Agent, Explore, ni subagentes de ningun tipo.
> TODO el trabajo se hace en el contexto principal: lectura de archivos, analisis y escritura.
> Los archivos del skill `.claude/skills/analisis-arquitectura/` se leen con Read, no se invocan.

> **REGLA 2 - RUTA DE SALIDA**: Los archivos se generan SIEMPRE en `06_Documentacion/`.
> NUNCA en `01_Diseno/`, NUNCA en raiz, NUNCA en `Arquitectura/`, NUNCA en otra carpeta.
> Si `06_Documentacion/` no existe, crearla.

> **REGLA 3 - NOMBRES CON VERSION Y FECHA**: Los nombres de archivo SIEMPRE incluyen version y fecha.
> Formato: `ANALISIS_ARQUITECTURA_v{version}_{fecha}.md` donde fecha = YYYY-MM-DD.
> NUNCA generar `ANALISIS_ARQUITECTURA.md` sin version ni fecha.

> **REGLA 4 - MULTI-SOLUCION**: Si hay 2+ archivos .sln/.slnx, PREGUNTAR al usuario cual analizar
> ANTES de empezar el analisis. No analizar todas sin preguntar.

---

## Parametros

- `$ARGUMENTS` - Parametros opcionales: `--tipo ca|general|auto`, `--version X.X`, `--solucion nombre.sln`, `--todas`

---

## Instrucciones para Claude

### 1. Detectar soluciones y preguntar

**PRIMERO**, antes de leer ningun archivo de codigo:

1. Buscar archivos .sln y .slnx en el proyecto (Glob `**/*.sln`, `**/*.slnx`)
2. Leer `_duran/ESTADO_PROYECTO.json` → `soluciones.multiSolucion`

**Si hay 2+ soluciones** (o multiSolucion == true):

```
PREGUNTAR al usuario (no continuar sin respuesta):

"Se han detectado N soluciones:
 [1] Solucion1.sln (.NET X)
 [2] Solucion2.sln (.NET X)
 [3] Todas (genera un informe por solucion + comparativa)
 ¿Cual analizar? (1/2/3)"
```

**Si hay 1 solucion**: continuar sin preguntar.

Parsear argumentos de `$ARGUMENTS`:
- `--tipo ca` → Forzar analisis Clean Architecture
- `--tipo general` → Forzar analisis arquitectura general
- `--tipo auto` o sin parametro → Auto-detectar
- `--version X.X` → Version del documento (default: 1.0)
- `--solucion nombre.sln` → Forzar una solucion concreta (salta la pregunta)
- `--todas` → Analizar todas (salta la pregunta)

### 2. Recopilar especificacion del proyecto

Leer datos de multiples fuentes para auto-rellenar la especificacion:

| Campo | Fuente principal | Fuente alternativa |
|-------|-----------------|-------------------|
| NOMBRE | `_duran/ESTADO_PROYECTO.json → proyecto.nombre` | CLAUDE.md |
| FRAMEWORK | `<TargetFramework>` en .csproj | ESTADO_PROYECTO.json |
| COMPOSICION | Analisis de proyectos en .sln/.slnx | ESTADO_PROYECTO.json → soluciones |
| BASE DE DATOS | Connection strings en appsettings.json | ESTADO_PROYECTO.json |
| AUTENTICACION | Deteccion en Program.cs/Startup.cs | ESTRUCTURA_TECNICA.md |

**Mostrar la especificacion recopilada al usuario y pedir confirmacion antes de continuar.**

**Auto-deteccion de Clean Architecture** (por solucion):

```
SI la solucion tiene:
  - Proyectos con sufijo .Domain, .Application, .Infrastructure
  - Y las dependencias siguen el patron CA
ENTONCES → tipo = "ca"
SINO → tipo = "general"
```

### 3. Ejecutar analisis (EN CONTEXTO PRINCIPAL)

> **RECORDATORIO**: NO lanzar Agent ni Explore. Leer archivos directamente con Read/Glob/Grep.

**Paso 3a**: Leer el prompt de analisis:

- Si tipo = "ca" → Leer `.claude/skills/analisis-arquitectura/prompts/prompt-clean-architecture.md`
- Si tipo = "general" → Leer `.claude/skills/analisis-arquitectura/prompts/prompt-general.md`

**Paso 3b**: Leer los recursos auxiliares:

- `checklists/clean-architecture.md` (si ca) o `checklists/migration-viability.md` (si general)
- `patterns/design-pattern-catalog.md`
- `patterns/dependency-analysis.md`

**Paso 3c**: Ejecutar el analisis siguiendo las secciones obligatorias del prompt.

El analisis debe ser exhaustivo:
- Leer archivos .cs, .csproj, Program.cs, Startup.cs, appsettings.json **de la solucion seleccionada**
- Analizar dependencias reales entre proyectos (ProjectReference en .csproj)
- Contar metricas cuantitativas reales (no estimar)
- Detectar anti-patrones y violaciones
- Generar diagramas Mermaid con datos reales

**Multi-solucion modo "Todas"**: Repetir pasos 3a-3c para cada solucion.

### 4. Generar entregables

> **RECORDATORIO**: Archivos en `06_Documentacion/` con version y fecha en el nombre.

#### Variables

- `{version}` = parametro --version o "1.0"
- `{fecha}` = fecha actual YYYY-MM-DD (ej: 2026-04-06)
- `{NombreSolucion}` = nombre del .sln sin extension (ej: GestionIntercambio, Ewp)
- `{{STIC_VERSION}}` = leer de `_duran/ESTADO_PROYECTO.json` → `ecosistema.version` (ej: "3.8.0"). Si no existe, usar "desconocida"

#### Caso A: Solucion unica

```
06_Documentacion/ANALISIS_ARQUITECTURA_v{version}_{fecha}.md
06_Documentacion/ANALISIS_ARQUITECTURA_v{version}_{fecha}.html
```

Ejemplo: `06_Documentacion/ANALISIS_ARQUITECTURA_v1.0_2026-04-06.md`

#### Caso B: Multi-solucion (modo "Todas")

Un par de archivos **por cada solucion** + una comparativa:

```
06_Documentacion/ANALISIS_ARQUITECTURA_{NombreSolucion1}_v{version}_{fecha}.md
06_Documentacion/ANALISIS_ARQUITECTURA_{NombreSolucion1}_v{version}_{fecha}.html
06_Documentacion/ANALISIS_ARQUITECTURA_{NombreSolucion2}_v{version}_{fecha}.md
06_Documentacion/ANALISIS_ARQUITECTURA_{NombreSolucion2}_v{version}_{fecha}.html
06_Documentacion/COMPARATIVA_ARQUITECTURA_v{version}_{fecha}.md
06_Documentacion/COMPARATIVA_ARQUITECTURA_v{version}_{fecha}.html
```

Ejemplo:
```
06_Documentacion/ANALISIS_ARQUITECTURA_GestionIntercambio_v1.0_2026-04-06.md
06_Documentacion/ANALISIS_ARQUITECTURA_Ewp_v1.0_2026-04-06.md
06_Documentacion/COMPARATIVA_ARQUITECTURA_v1.0_2026-04-06.md
```

**Cada informe por solucion** es independiente y completo:
- TODAS las secciones del prompt
- Solo los .csproj referenciados en esa .sln
- Su propia puntuacion y plan de accion

**Comparativa** (solo en modo multi-solucion):
- Tabla resumen: Solucion | Framework | Tipo Arq. | Puntuacion | Hallazgos criticos
- Dependencias compartidas (NuGets, BD, tablas)
- Patrones comunes vs divergentes
- Plan de accion consolidado

#### Formato

**Markdown**: Leer `.claude/skills/analisis-arquitectura/templates/ANALISIS_ARQUITECTURA.md.template` y rellenar con datos reales.

**HTML**: Leer `.claude/skills/analisis-arquitectura/templates/ANALISIS_ARQUITECTURA.html.template` y rellenar los placeholders `{{SECCION_NOMBRE}}`.

### 5. Registrar en DURAN

Actualizar `_duran/HISTORIAL_CAMBIOS.md`:

```markdown
### [YYYY-MM-DD] Analisis Arquitectonico v{version}
- Tipo: {ca|general}
- Puntuacion: X.X/10
- Hallazgos criticos: N
- Archivos: 06_Documentacion/ANALISIS_ARQUITECTURA_v{version}_{fecha}.md/.html
```

### 6. Mostrar resumen

```
╔══════════════════════════════════════════════════════════════╗
║  ANALISIS ARQUITECTONICO COMPLETADO                         ║
╠══════════════════════════════════════════════════════════════╣
║  Solucion(es): {nombre(s) .sln analizados}                  ║
║  Tipo: {Clean Architecture | Arquitectura General}          ║
║  Puntuacion: X.X / 10                                       ║
║  Hallazgos criticos: N                                       ║
╠══════════════════════════════════════════════════════════════╣
║  Archivos generados:                                         ║
║  📄 06_Documentacion/ANALISIS_ARQUITECTURA_v{v}_{f}.md      ║
║  🌐 06_Documentacion/ANALISIS_ARQUITECTURA_v{v}_{f}.html    ║
╠══════════════════════════════════════════════════════════════╣
║  Para un analisis completo, ejecutar tambien:                ║
║  • /analizar         → 9 fases (seguridad, tests, deuda)    ║
║  • security-auditor  → OWASP Top 10, secrets, STRIDE        ║
║  • /verify           → Pipeline pre-commit 7 fases          ║
╚══════════════════════════════════════════════════════════════╝
```

> **Nota**: Este comando genera un informe arquitectonico enfocado en estructura,
> capas, patrones y dependencias. Para auditorias de **seguridad** (OWASP, secrets),
> **testing** (cobertura, calidad), **observabilidad** (Serilog, OpenTelemetry) o
> **resiliencia** (Polly, Health Checks), usar `/analizar` o los agents especializados.

---

## Ejemplos de uso

```
/analisis-arquitectura                              # Auto-detecta todo, version 1.0
/analisis-arquitectura --tipo ca                    # Forzar Clean Architecture
/analisis-arquitectura --tipo general --version 2.0 # General, version 2.0
/analisis-arquitectura --solucion MiApp.sln         # Solo esa solucion
/analisis-arquitectura --todas                      # Todas + comparativa
```

---

*Comando v3.8.1 - Auditoria arquitectonica formal con entregables MD+HTML + guia analisis complementarios*
