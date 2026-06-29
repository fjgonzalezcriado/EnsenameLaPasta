# Antipatrones de Branching

> Skill: git-best-practices | Version: 3.7.0
> Documento fuente: `Guias/reglas-externas/branching-strategies-v2-guia-completa.html`

Errores comunes en estrategias de ramificacion y como evitarlos.

---

## Los 9 Antipatrones

### 1. Long-Lived Feature Branches

**Problema**: Ramas feature que viven semanas o meses sin mergear.

```
main ──────●────●────●────●────●────●────●────●──── (avanza)
            \
             ●──●──●──●──●──●──●──●──●──●──●        (3 semanas, merge hell)
```

**Consecuencias**:
- Merge conflicts masivos
- Codigo divergente, tests desactualizados
- Integracion tardia = bugs tardios

**Solucion**:
- Feature branches < 3 dias (ideal < 1 dia)
- Stacked PRs para features grandes
- Feature flags para codigo incompleto
- Mergear a main frecuentemente (al menos diario)

---

### 2. Ramas Zombie

**Problema**: Ramas que nadie elimina despues del merge.

```
main ──────●────●────●────●────
            \  /
             ●         ← rama mergeada pero no eliminada
feature/HV-12  ← sigue existiendo sin actividad
feature/HV-08  ← 2 meses sin actividad
bugfix/old     ← sin actividad desde enero
```

**Consecuencias**:
- Confusion sobre que esta activo
- Repositorio desordenado
- Builds innecesarios si hay triggers por rama

**Solucion**:
- Configurar auto-delete de ramas tras merge en Azure DevOps/GitHub
- Revision mensual de ramas inactivas
- Convencion: borrar rama inmediatamente despues del merge

---

### 3. Cherry-Pick Inverso

**Problema**: Aplicar fixes directamente a release branch sin pasar por main.

```
main ────────────────────●──── (¡NO tiene el fix!)

release/v2.3 ─────●───── (fix aplicado aqui primero)
```

**Consecuencias**:
- Fix se pierde en proxima release
- Regresion al crear release/v2.4 desde main
- Historial inconsistente

**Solucion (Release Flow)**:
1. Fix va a `main` primero (via PR)
2. Cherry-pick de `main` a `release/*`
3. Nunca al reves

---

### 4. Merge a Deshoras (Integration Hell)

**Problema**: Hacer merge solo al final del sprint o milestone.

**Consecuencias**:
- Acumulacion de conflictos
- Sesiones de merge de horas
- Bugs descubiertos tarde

**Solucion**:
- Integrar a main al menos una vez al dia
- CI que ejecuta en cada push
- PRs pequenos y frecuentes

---

### 5. Ramas por Entorno (sin necesidad)

**Problema**: Mantener ramas `dev`, `staging`, `production` cuando un pipeline multi-stage cumple el mismo proposito.

```
dev        ──●──●──●──●──●──●──●──── (¿quien mergea que a donde?)
staging    ──────●────────●──────────
production ──────────────────●───────
```

**Consecuencias**:
- Merges manuales entre ramas = fuente de errores
- No esta claro que codigo esta en que entorno
- Complejidad innecesaria

**Solucion**:
- Usar pipeline multi-stage de Azure Pipelines
- Una rama (`main`) con deploy progresivo: Dev → Demo → Pro
- Las ramas representan *trabajo*, los entornos se gestionan con *pipelines*

---

### 6. GitFlow para Webapps

**Problema**: Usar GitFlow (con `develop`, `release/*`, `hotfix/*`) para aplicaciones web con deploy continuo.

> "If your team is doing continuous delivery of software, I would suggest to adopt a much simpler workflow instead of trying to shoehorn git-flow into your team."
> — Vincent Driessen (creador de GitFlow), 2020

**Consecuencias**:
- Overhead de ramas innecesario
- Merges redundantes (feature → develop → release → main)
- Ralentiza deploys

**Solucion**:
- GitHub Flow para webapps (90% de proyectos Comillas)
- Release Flow si necesitas releases versionadas
- GitFlow solo para software empaquetado con multiples versiones en produccion

---

### 7. PRs Gigantes

**Problema**: Pull Requests de 1000+ lineas que nadie puede revisar efectivamente.

**Consecuencias**:
- Reviews superficiales ("LGTM" sin leer)
- Bugs que se cuelan
- Tiempo de review excesivo

**Solucion**:
- PRs de < 400 lineas (ideal < 200)
- Stacked PRs para features grandes
- Descomponer: modelo → API → UI (3 PRs encadenados)

---

### 8. Push Directo a Main

**Problema**: Desarrolladores hacen push directamente a main sin PR ni review.

**Consecuencias**:
- Sin code review = calidad variable
- Sin build validation = posibles regresiones
- Sin trazabilidad de quien aprobo que

**Solucion**:
- Branch policies obligatorias en main
- Build validation automatica
- Excepcion controlada: Ship/Show/Ask para cambios triviales (typos, deps minor) solo si el equipo tiene madurez

---

### 9. Merge Strategy Inconsistente

**Problema**: Mezclar squash merges, regular merges y rebases sin criterio.

```
main ──M──S──M──R──S──M──S──R──── (historial caotico)
       │  │  │  │  │  │  │  │
       merge squash merge rebase...
```

**Consecuencias**:
- Historial dificil de leer
- `git bisect` se complica
- Confusion del equipo

**Solucion**:
- Elegir UNA estrategia y forzarla via branch policies
- Recomendacion Comillas: **Squash merge** para GitHub Flow
- Registrar en `_duran/ESTADO_PROYECTO.json` campo `mergeStrategy`

---

## Quick Checklist

Antes de establecer la estrategia de branching del proyecto, verificar:

- [ ] **Estrategia elegida** esta documentada en `ESTADO_PROYECTO.json`
- [ ] **Branch policies** configuradas en Azure DevOps/GitHub
- [ ] **Merge strategy** unica y forzada via policies
- [ ] **Auto-delete** de ramas tras merge habilitado
- [ ] **Build validation** obligatoria en main
- [ ] **Feature branches** con vida < 3 dias
- [ ] **PRs** de tamano razonable (< 400 lineas)
- [ ] **Cherry-pick direction** clara: main → release (nunca al reves)
- [ ] **Pipeline** refleja la estrategia (no ramas por entorno innecesarias)
- [ ] **Equipo** entiende y aplica la estrategia elegida

---

## Conexion con Branch Policies

Los antipatrones 2, 7, 8 y 9 se previenen con branch policies correctas:

| Antipatron | Policy que lo previene |
|------------|----------------------|
| Ramas zombie | Auto-delete after merge |
| PRs gigantes | Limite de archivos cambiados (revisor manual) |
| Push directo | Minimum approvers >= 1 |
| Merge inconsistente | Limit merge types (solo squash) |
| Sin build validation | Build validation required |

Ver `patterns/azure-devops-branching.md` para configuracion detallada de policies.

---

*Checklist v3.7.0 — 9 antipatrones, quick checklist, branch policies*
