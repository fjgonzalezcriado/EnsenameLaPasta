# Estrategias de Ramificacion

> Skill: git-best-practices | Version: 3.7.0
> Documento fuente: `Guias/reglas-externas/branching-strategies-v2-guia-completa.html`

Comparativa de 12 modelos de ramificacion con recomendaciones para proyectos Comillas.

---

## Estrategias Principales

### 1. GitHub Flow (Default Comillas)

```
main ─────●────●─────────●────●──── (produccion)
           \  /           \  /
            ●──●──●        ●──●     (feature branches)
```

**Principios:**
- `main` siempre desplegable
- Feature branches con nombre descriptivo
- Pull Requests obligatorios con code review
- Deploy desde main tras merge

**Cuando usar:**
- Equipos de 1-6 desarrolladores
- APIs REST, portales web, servicios
- Despliegues semanales o continuos
- Un unico entorno de produccion

**Merge strategy:** Squash merge (1 commit por feature)

---

### 2. Trunk-Based Development

```
main ─────●────●────●────●────●────●──── (produccion)
           \  /      \  /      \  /
            ●         ●         ●        (ramas cortas, <1 dia)
```

**Principios:**
- Rama `main` siempre desplegable
- Feature branches de vida corta (horas, maximo 1-2 dias)
- Integracion continua real: merge a main varias veces al dia
- **Feature flags** para codigo incompleto (`Microsoft.FeatureManagement` en .NET 10)

**Cuando usar:**
- Equipos con CI/CD maduro y buena cobertura de tests
- Despliegues frecuentes (diarios o mas)
- Equipos pequenos-medianos (2-8 devs) con confianza alta

**Requisitos:** CI/CD robusto, feature flags, tests automatizados con alta cobertura

---

### 3. Release Flow (Microsoft)

```
main ─────●────●────●────●────●──── (desarrollo continuo)
           \        \        \
            ●        ●        ●      (release/v1.2, release/v1.3...)
```

**Principios:**
- Desarrollo en `main` (similar a Trunk-Based)
- Release branches solo para estabilizar antes de produccion
- Cherry-pick de fixes: **siempre main primero**, luego a release branch
- Usado internamente por el equipo de Azure DevOps

**Cuando usar:**
- Proyectos con releases versionadas (GestorLicencias, EuPeace, ErpSync)
- Necesidad de estabilizar una version antes del pase a produccion
- Soporte de multiples versiones en produccion (LTS)

**Merge strategy:** No-fast-forward para trazabilidad, cherry-pick para hotfixes

---

### 4. OneFlow

```
main ─────●────●────●────●────●──── (desarrollo + produccion)
                \        /
                 ●──●──●             (release/v1.2 temporal)
```

**Principios:**
- Una sola rama permanente (`main`)
- Release branches temporales que se mergean de vuelta y se eliminan
- Tags para marcar versiones
- Simplificacion de GitFlow manteniendo releases

**Cuando usar:**
- SDK, librerias publicas, paquetes NuGet
- Necesitas releases pero GitFlow es excesivo
- Equipos medianos (3-8 devs)

---

### 5. Developer Flow (GitLab Flow simplificado)

```
main    ──●──●──●──●──●──●──●──── (integracion)
                │        │
                ↓        ↓
release ────────●────────●──────── (produccion)
```

**Principios:**
- `main` como rama de integracion (no es produccion directamente)
- `release` (o `production`) como rama de produccion
- Features → main via PR → release via merge programado
- QA manual en main antes de promover a release

**Cuando usar:**
- Monolitos con QA manual obligatoria
- Deploy escalonado con aprobacion
- Equipos que necesitan buffer entre desarrollo y produccion

---

### 6. GitLab Flow

```
main       ──●──●──●──●──●──●──●──── (desarrollo)
                  │        │
                  ↓        ↓ merge
staging    ──────●────────●──────── (QA/staging)
                        │
                        ↓ merge
production ─────────────●──────── (produccion)
```

**Principios:**
- Ramas que reflejan entornos (staging, production)
- Upstream-first: fixes van a main y se propagan "aguas abajo"
- Merge entre ramas = promover codigo entre entornos
- Mas formal que Environment Branching

**Cuando usar:**
- Proyectos con multiples entornos y QA manual por etapa
- Requisitos regulatorios de trazabilidad
- Equipos que necesitan visibilidad de que hay en cada entorno

**Advertencia:** En la practica, pipelines multi-stage de Azure Pipelines logran lo mismo sin ramas extra.

---

### 7. GitFlow (Solo para releases complejos)

```
main ────────────●──────────────●──── (produccion)
                 ↑              ↑
release ─────●──●───         ●──●──── (release branches)
             ↑               ↑
develop ──●──●──●──●──●──●──●──●──── (integracion)
           \  /     \  /
            ●        ●               (feature branches)
```

**Principios:**
- `main` = produccion, `develop` = integracion
- Feature branches desde develop
- Release branches para estabilizar
- Hotfix branches desde main

**Cuando usar:**
- Software empaquetado con ciclos de release largos (desktop, instalable)
- Multiples versiones en produccion simultaneas
- Equipos grandes (>10 devs) con coordinacion compleja

**Advertencia:**
> GitFlow anade complejidad significativa. El propio creador (Vincent Driessen, 2020)
> desaconseja GitFlow para webapps con deploy continuo. Para la mayoria de proyectos
> Comillas, GitHub Flow o Release Flow son suficientes. Solo usar GitFlow si hay
> requisitos explicitos de multiples versiones en produccion y ciclos largos.

---

### 8. GitHub Flow Simplificado (Solo Developer)

```
main  ──●──●──────●──●──────────●──●──  → demo (auto) → prod (manual)
         ╲      ╱     ╲          ╱
          ●──●─╱       ●──●──●─╱
          feature/a    feature/b
             ↑                ↑
       merge --no-ff    merge --no-ff
       (revertible)     (revertible)
```

**Principios:**
- Solo `main` + feature branches
- Sin Pull Requests formales (merge local con `--no-ff`)
- Build validation como unica branch policy (sin reviewers)
- Pipeline multi-stage: main → demo (auto) → prod (manual)
- Auto-delete de ramas tras merge

**Cuando usar:**
- Desarrollador unico que tambien despliega
- Proyectos internos pequenos sin equipo de review
- Aplicaciones con CI/CD pero sin necesidad de code review formal

**Workflow diario:**

```bash
# 1. Crear rama para la tarea
git checkout -b feature/HV-18-filtro-becas main

# 2. Desarrollar con commits atomicos
git commit -m "feat(becas): añadir servicio de filtrado"
git commit -m "feat(becas): implementar endpoint filtros"
git commit -m "test(becas): tests unitarios filtrado"

# 3. Merge local con --no-ff (mantiene trazabilidad)
git checkout main
git merge --no-ff feature/HV-18-filtro-becas
# Genera commit de merge → revertible con un solo git revert

# 4. Push a main → pipeline se dispara automaticamente
git push origin main

# 5. Borrar rama feature
git branch -d feature/HV-18-filtro-becas
```

**¿Por que `--no-ff` en lugar de squash?**

| Aspecto | `--no-ff` (recomendado) | Squash merge |
|---------|-------------------------|--------------|
| Historial | Commits individuales visibles | 1 commit por feature |
| Revertir | `git revert -m1 <merge>` → limpio | Revert manual, cherry-picks |
| Bisect | `git bisect` funciona en cada commit | Solo a nivel de feature |
| Para 1 dev | Ideal: ve todo su trabajo | Pierde detalle |

**Variante con release branches:**

```
main     ──●──●──────●──●──●──── (desarrollo continuo)
                          \
                           ●── release/v2.3 → demo → prod
                           ↑
                      cherry-pick desde main
```

Para proyectos que necesitan estabilizar una version antes de desplegar a produccion, se puede combinar con release branches temporales (misma mecanica que Release Flow pero sin PRs).

---

### 9. Developer Branch (Rama Personal Remota)

```
dev.claudio (remoto)
    │
    ├──●── 20260324-DT-005-fix-xyz ──●──●── merge → dev.claudio
    │
    ├──●── 20260324-HV-18-filtro-becas ──●── merge → dev.claudio
    │
    └── git push origin dev.claudio  ← unico push
```

**Principios:**
- Rama personal remota (`dev.{usuario}`) como punto de integracion
- Features en ramas locales — NUNCA se pushean al remoto
- Nomenclatura temporal: `yyyyMMdd-{tipo}-{codigo}-{descripcion}`
- Tipo de tarea explicito en nombre de rama (DT, HV, BUG, etc.)
- Plain merge (sin `--no-ff` por defecto, configurable)
- Confirmar con usuario antes de merge y push

**Cuando usar:**
- Desarrollador unico sin CI/CD formal
- Proyectos con rama personal remota como base
- Equipos que prefieren trazabilidad temporal en ramas
- Contextos donde las features no deben publicarse al remoto

**Workflow diario:**

```bash
# 1. Posicionarse en rama base personal
git checkout dev.claudio
git pull origin dev.claudio

# 2. Crear rama local con fecha y tipo
git checkout -b 20260406-HV-18-filtro-becas

# 3. Desarrollar con commits parciales
git commit -m "feat(becas): añadir servicio de filtrado"
git commit -m "test(becas): tests unitarios filtrado"

# 4. Compilar y verificar
dotnet build && dotnet test

# 5. Merge a rama base (CONFIRMAR con usuario)
git checkout dev.claudio
git merge 20260406-HV-18-filtro-becas

# 6. Push rama base (CONFIRMAR con usuario)
git push origin dev.claudio

# 7. Mantener rama local como historico o borrar
git branch -d 20260406-HV-18-filtro-becas
```

**Nomenclatura configurable:**

| Componente | Significado | Ejemplo |
|-----------|-------------|---------|
| `yyyyMMdd` | Fecha de inicio de la tarea | `20260406` |
| `{tipo}` | Tipo de tarea (configurable) | `DT`, `HV`, `BUG`, `REF` |
| `{codigo}` | Codigo evolutivo o numero | `005`, `18` |
| `{descripcion}` | Descripcion breve kebab-case | `filtro-becas` |

**Comparacion: Developer Branch vs GitHub Flow simplificado:**

| Aspecto | Developer Branch | GitHub Flow simplificado |
|---------|-----------------|--------------------------|
| Rama base | `dev.{usuario}` (personal) | `main` |
| Features se pushean | No | No |
| Merge | plain merge | `--no-ff` |
| Nomenclatura | Temporal (fecha + tipo) | Semantica (`feature/...`) |
| Tipo tarea en rama | Si (DT/HV/...) | No |
| CI/CD | Opcional | Recomendado |

---

**Comparacion: GitHub Flow vs GitHub Flow simplificado:**

| Aspecto | GitHub Flow | GitHub Flow simplificado |
|---------|-------------|--------------------------|
| Pull Requests | Obligatorios | No (merge local) |
| Code review | Minimo 1 reviewer | No (solo developer) |
| Branch policies | Build + reviewers | Solo build validation |
| Merge strategy | Squash merge | `--no-ff` merge |
| Safety net | PR + CI + review | CI + `--no-ff` (revertible) |
| Release branches | No | Opcional (variante) |
| Equipo | 1-6 devs | 1 dev |
| Complejidad | Baja | Minima |

**Azure DevOps config:**
- Branch policies en `main`: Solo build validation (sin minimum reviewers)
- Pipeline multi-stage con `condition: or(main, release/*)` para despliegue a produccion
- Auto-delete de ramas tras merge habilitado
- Ver `patterns/azure-devops-branching.md` para pipeline YAML concreto

---

## Tecnicas Complementarias (Overlays)

Estas tecnicas se superponen sobre cualquier estrategia base:

### 9. Ship/Show/Ask

Clasifica cada cambio segun su riesgo:

| Nivel | Accion | Ejemplo |
|-------|--------|---------|
| **Ship** | Merge directo sin PR | Fix typo, update dependency minor |
| **Show** | Merge, abrir PR informativo (post-merge) | Refactor local, test nuevo |
| **Ask** | PR clasico con review obligatorio | Feature nueva, cambio de API, seguridad |

**Requiere:** Madurez del equipo, CI/CD robusto. No usar en entornos regulados sin adaptacion.

### 10. Stacked PRs

Descomponer features grandes en PRs pequenos encadenados:

```
main
 └── feature/upvotes/data-model      (PR #1 → main)
      └── feature/upvotes/api        (PR #2 → PR #1)
           └── feature/upvotes/ui    (PR #3 → PR #2)
```

**Herramientas:** Graphite (GitHub), Aviator (GitHub), `git --update-refs` (Git 2.38+), Azure DevOps (PR contra PR soportado pero sin restacking automatico).

**Cuando usarlo:** Features grandes (backend + API + frontend), refactorizaciones en pasos.
**Cuando NO:** Features pequenas (<300 lineas). La complejidad del stack no compensa.

### 11. Forking Workflow

Cada desarrollador tiene su propio fork. PRs van del fork al repo central.

**Cuando usar:** Contribuidores externos sin acceso al repo, proyectos open source.
**En Comillas:** Raro. Solo para colaboraciones inter-departamentales sin acceso compartido.

### 12. Environment Branching

Una rama por entorno (`dev`, `staging`, `production`). Merge entre ramas = promover.

**Advertencia:** Superado por pipelines multi-stage de Azure Pipelines. GitLab Flow es la version formalizada. Evitar en proyectos nuevos.

### 13. Cherry-Pick (operacion tactica, NO estrategia)

`git cherry-pick` **NO es** una estrategia de branching ni una `mergeStrategy`. Es una **operacion tactica** que se aplica encima de cualquier estrategia base cuando hace falta trasladar UN commit especifico entre ramas (en vez de mezclar la rama entera).

**Casos validos en Comillas:**

| Caso | Patron | Estrategia base aplicable |
|---|---|---|
| **Hotfix backport** | Fix en `main` → cherry-pick a `release/v3.x` | GitFlow, OneFlow, Release Flow |
| **Promocion selectiva** | Commit aprobado en `main` → cherry-pick a `pre` o `production` | GitLab Flow |
| **Hotfix paralelo** | Fix urgente aplicado a varias release branches simultaneas | GitFlow, Release Flow |
| **Rescate de commit perdido** | Recuperar 1 commit de una rama abandonada o rebaseada | Cualquiera |

**Comandos basicos:**

```bash
# Cherry-pick de un commit
git checkout release/v3.0
git cherry-pick <hash-en-main>

# Cherry-pick de varios commits (range)
git cherry-pick <hash-start>..<hash-end>

# Cherry-pick sin commitear (deja staged para revisar)
git cherry-pick -n <hash>

# Cherry-pick con tracking del origen (recomendado para auditoria)
git cherry-pick -x <hash>
# Anade footer "(cherry picked from commit <hash>)" automaticamente
```

**Anti-patron: cherry-pick inverso**

NO hacer el fix directamente en la rama de release y luego cherry-pick a `main`. El orden correcto siempre es: **fix primero en `main`** (o `develop` en GitFlow), VALIDAR ahi, y luego cherry-pick **hacia atras** a las release branches activas.

Razon: si el fix se hace primero en release/v3.0 y se cherry-pickea a main, es facil que se olvide y futuras releases (v3.1, v4.0) no incluyan el fix. Convirtiendo el bug en regresion permanente.

**Convencion de mensaje:**

Usar `git cherry-pick -x <hash>` (con `-x`) para que el commit destino lleve automaticamente:
```
fix(becas): corregir validacion de fecha

(cherry picked from commit abc1234...)
```
Asi la auditoria via `git log` permite rastrear de donde viene cada fix.

**Cuando NO usar cherry-pick:**
- Para mezclar una rama feature entera → usar merge / rebase / squash segun mergeStrategy.
- Para "rebobinar" historia rota → usar `git revert` (preserva historia) o `git reset` (con cuidado).
- En GitHub Flow puro o Trunk-Based → no hay release branches, no necesitas cherry-pick.

**Relacion con `/branching`:**

Cherry-pick **NO es configurable** en `_duran/ESTADO_PROYECTO.json` porque es operacion tactica, no estructural. `/branching` no lo gestiona. La decision de cuando usar cherry-pick es del desarrollador en base a esta documentacion.

---

## Arbol de Decision

```
¿Eres desarrollador unico sin equipo de review?
├── SI ──────────────────────────────── GitHub Flow simplificado
└── NO
    ¿Despliegas varias veces al dia con CI/CD maduro?
    ├── SI, equipo senior ────────────── Trunk-Based (+ Ship/Show/Ask)
    ├── SI, quiero code review en PRs ── GitHub Flow
    └── NO
        ¿Necesitas staging con QA manual?
        ├── SI, ramas por entorno ────── GitLab Flow
        ├── SI, pipelines multi-stage ── Developer Flow o GitHub Flow + stages
        └── NO
            ¿Multiples versiones en produccion?
            ├── SI, releases planificadas  Release Flow
            ├── SI, simplificar GitFlow ── OneFlow
            ├── SI, ciclos largos ──────── GitFlow
            └── NO ESTOY SEGURO ─────────  GitHub Flow (default seguro)

¿PRs demasiado grandes? → Stacked PRs sobre tu estrategia actual
¿Contribuidores externos? → Forking Workflow combinado con estrategia interna
```

---

## Matriz de Decision

| Criterio | Trunk | GitHub Flow | GH Flow Simp. | Release Flow | OneFlow | Developer | GitLab | GitFlow |
|----------|-------|-------------|---------------|-------------|---------|-----------|--------|---------|
| Tamano equipo | 2-8 | **1-6** | **1** | 5+ | 3-8 | 4-10 | 5-15 | 8+ |
| Frecuencia deploy | Diaria+ | Semanal+ | Semanal+ | Variable | Variable | Semanal | Semanal | Mensual+ |
| Complejidad | Baja | **Baja** | **Minima** | Media | Media | Media | Media-Alta | Alta |
| CI/CD requerido | Maduro | Basico | Basico | Medio | Basico | Medio | Medio | Basico |
| Multiples versiones | No | No | No* | **Si** | Si | No | No | Si |
| Feature flags | Obligatorio | Opcional | No | Opcional | No | No | No | No |
| **Encaje Azure DevOps** | Bueno | **Excelente** | **Excelente** | **Excelente** | Muy bueno | Bueno | Bueno | Con friccion |

*GitHub Flow simplificado soporta releases opcionales con variante cherry-pick.

### Matriz por tipo de proyecto Comillas

| Tipo de proyecto | Recomendada | Alternativa |
|------------------|-------------|-------------|
| API REST / Portal web | **GitHub Flow** | Trunk-Based |
| App con releases planificadas | **Release Flow** | OneFlow |
| Microservicios | Trunk-Based / GitHub Flow | — |
| Monolito con QA manual | Developer Flow | GitLab Flow |
| SDK / Libreria NuGet | OneFlow | GitFlow |
| Sincronizacion / ETL | GitHub Flow | Release Flow |
| Desarrollador unico que despliega | **GitHub Flow simplificado** | Release Flow (si versiones) |

---

## Recomendacion Comillas

### APIs y portales web → GitHub Flow

Main desplegable, PRs como gate, CI/CD directo. Considerar Ship/Show/Ask para cambios triviales si el equipo tiene madurez.

### Proyectos versionados → Release Flow

GestorLicencias, EuPeace, ErpSync. Desarrollo fluido en main, release branches solo para estabilizar, cherry-picks para hotfixes. Stacked PRs para features complejas.

### Desarrollador unico que tambien despliega → GitHub Flow simplificado

Solo `main` + feature branches. Sin PRs formales. Pipeline CI/CD: main → demo (auto) → prod (manual). Build validation en main como unica policy. Ver `patterns/azure-devops-branching.md` para pipeline concreto.

### Desarrollador unico con rama personal remota → Developer Branch

Rama personal (`dev.{usuario}`) como punto de integracion. Features locales con nomenclatura temporal (`yyyyMMdd-{tipo}-{codigo}-{descripcion}`). Sin CI/CD formal. Ideal para proyectos internos sin pipeline automatizado.

---

## Conexion con Metricas DORA

Las metricas DORA (Accelerate, Nicole Forsgren) demuestran que equipos de alto rendimiento usan flujos simples:

| Metrica DORA | Flujos simples (Trunk/GitHub) | Flujos complejos (GitFlow) |
|--------------|------------------------------|----------------------------|
| Deployment Frequency | Diaria-semanal | Mensual-trimestral |
| Lead Time for Changes | Horas-dias | Semanas-meses |
| Change Failure Rate | 0-15% | 31-45% |
| MTTR | Minutos-horas | Dias-semanas |

**Implicacion:** Preferir GitHub Flow o Trunk-Based salvo que haya requisitos explicitos que justifiquen mayor complejidad.

---

## Persistencia de la Decision

La estrategia elegida se registra en `_duran/ESTADO_PROYECTO.json` seccion `branching`:

```json
"branching": {
    "estrategia": "github-flow",
    "ramaBase": "main",
    "mergeStrategy": "squash",
    "convencionRamas": "feature/{codigo}-{descripcion}",
    "pushFeatures": true,
    "tiposTarea": null,
    "releasesBranches": false,
    "flujoDespliegue": "main → demo (auto) → prod (manual)"
}
```

Claude usa esta configuracion en `/commit`, `/git-sync` y `/liberar` para aplicar la estrategia correcta.

**Nomenclatura configurable** — el campo `convencionRamas` acepta distintos patrones:

| Patron | Ejemplo | Cuando usar |
|--------|---------|-------------|
| `feature/{codigo}-{descripcion}` | `feature/HV-18-filtro-becas` | Default, equipos con PRs |
| `yyyyMMdd-{tipo}-{codigo}-{descripcion}` | `20260406-DT-005-fix-xyz` | Developer Branch, trazabilidad temporal |
| `yyyyMMdd-{tipo}/{codigo}-{descripcion}` | `20260406-HV/HV-18-filtro` | Combinada |
| `custom` | Lo que defina el equipo | Nomenclatura propia |

**Rama base configurable** — `ramaBase` acepta cualquier valor: `main`, `master`, `develop`, `dev.{usuario}`.

**Tipos de tarea** — si `convencionRamas` contiene `{tipo}`, el campo `tiposTarea` define los tipos disponibles.

---

## Convencion de Ramas Comillas

### Nomenclatura Semantica (default)

```
# Ramas principales
main              # Produccion (protegida)
develop           # Integracion (solo si GitFlow)

# Ramas de trabajo
feature/HV-XX-descripcion    # Nuevas funcionalidades
bugfix/HV-XX-descripcion     # Correcciones de bugs
hotfix/HV-XX-descripcion     # Fixes urgentes en produccion
release/vX.Y.Z               # Preparacion de release (si aplica)
chore/descripcion             # Tareas de mantenimiento

# Ejemplos
feature/HV-18-filtro-becas-por-estado
bugfix/HV-23-fecha-solicitud-null
hotfix/HV-25-error-login-produccion
release/v2.3
```

### Nomenclatura Temporal (developer-branch y otros)

```
# Rama base personal
dev.{usuario}     # Rama personal remota (ej: dev.claudio)

# Ramas de trabajo (LOCALES, nunca se pushean)
yyyyMMdd-{tipo}-{codigo}-{descripcion}

# Tipos de tarea (configurables en ESTADO_PROYECTO.json)
DT  = Deuda tecnica
HV  = Evolutivo
BUG = Correccion
REF = Refactoring

# Ejemplos
20260406-DT-005-fix-validacion-email
20260406-HV-18-filtro-becas-por-estado
20260407-BUG-003-null-ref-solicitud
```

> La nomenclatura se configura en `configuracion.branching.convencionRamas` de ESTADO_PROYECTO.json. Claude la lee automaticamente en `/commit`, `/git-sync` y `/nuevo-evolutivo`.

---

*Pattern v3.7.0 — 12 estrategias, decision tree, metricas DORA*
