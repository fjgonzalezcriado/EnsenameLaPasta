# /branching - Configurar / cambiar estrategia de ramificacion

Comando idempotente para revisar o modificar la **estrategia de branching y merge** del proyecto en `_duran/ESTADO_PROYECTO.json` sin tocar el resto de la configuracion.

Pensado para reconfigurar tras `/onboarding` cuando:
- Cambia el tamano del equipo (1 dev → varios, o al reves)
- Aparece o desaparece CI/CD
- El equipo decide cambiar de modelo (ej. de `trunk-based` a `github-flow`)
- Tras `/actualizar` aparecen estrategias nuevas que antes no existian

---

## Sintaxis

```
/branching                          # Modo interactivo: muestra config y pregunta cambios
/branching --show                   # Solo lectura (no escribe nada)
/branching --estrategia <nombre>    # Cambio directo a una estrategia concreta (+ defaults coherentes)
/branching --merge <nombre>         # Cambio SOLO de mergeStrategy (sin tocar estrategia ni resto)
/branching --reset                  # Restablece a defaults segun /onboarding rules
```

**Estrategias validas** (9):
`github-flow`, `github-flow-simplificado`, `release-flow`, `trunk-based`, `oneflow`, `gitflow`, `developer-flow`, `developer-branch`, `gitlab-flow`.

**mergeStrategy validos** (4):
`squash`, `no-ff`, `rebase`, `merge`.

---

## Instrucciones para Claude

### 1. Validar precondiciones

```bash
test -f _duran/ESTADO_PROYECTO.json || { echo "ERROR: ESTADO_PROYECTO.json no existe. Ejecuta /onboarding primero."; exit 1; }
jq -e '.configuracion.branching' _duran/ESTADO_PROYECTO.json > /dev/null || { echo "ERROR: seccion branching no existe. Ejecuta /onboarding."; exit 1; }
```

### 2. Modo `--show` (solo lectura)

Mostrar la config actual con formato tabla:

```
ESTRATEGIA DE BRANCHING ACTUAL

  Estrategia:           github-flow
  Rama base:            main
  Merge strategy:       squash
  Convencion ramas:     feature/{codigo}-{descripcion}
  Push features:        true
  Tipos tarea:          (no aplica con nomenclatura semantica)

  Branch policies:
    Reviewers minimo:   1
    Build validation:   true
    Linked work items:  false
    Comment resolution: true

  Flujo despliegue:     main → demo (auto) → prod (manual)
  Estrategias disponibles en este proyecto: 9
```

Salir sin escribir nada.

### 3. Modo `--estrategia <nombre>`

a) Validar que `<nombre>` esta en `_estrategias_disponibles`. Si no, listar las 9 validas y salir.

b) Mostrar la config actual (snapshot pre-cambio) y la propuesta (snapshot post-cambio) en formato diff lado a lado.

c) Calcular **defaults coherentes** segun la estrategia elegida usando esta tabla:

| Estrategia | mergeStrategy | ramaBase | convencionRamas | pushFeatures | reviewersMinimo | buildValidation |
|---|---|---|---|---|---|---|
| `github-flow` | `squash` | `main` | `feature/{codigo}-{descripcion}` | `true` | `1` | `true` |
| `github-flow-simplificado` | `no-ff` | `main` | `feature/{codigo}-{descripcion}` | `false` | `0` | `true` |
| `release-flow` | `squash` | `main` | `feature/{codigo}-{descripcion}` | `1` | `true` | `true` |
| `trunk-based` | `squash` | `main` | `feature/{codigo}-{descripcion}` (corta-vida <1d) | `true` | `1` | `true` |
| `oneflow` | `no-ff` | `main` | `feature/{codigo}-{descripcion}` | `true` | `1` | `true` |
| `gitflow` | `no-ff` | `develop` | `feature/{codigo}-{descripcion}` | `true` | `1` | `true` |
| `developer-flow` | `squash` | `main` | `feature/{codigo}-{descripcion}` | `true` | `1` | `true` |
| `developer-branch` | `merge` | `dev.{usuario}` | `yyyyMMdd-{tipo}-{codigo}-{descripcion}` | `false` | `0` | `false` |
| `gitlab-flow` | `no-ff` | `main` | `feature/{codigo}-{descripcion}` | `true` | `1` | `true` |

**Reglas adicionales**:
- Si la estrategia es `gitflow`: pedir confirmar/elegir nombre de `ramaDevelop` (default `develop`) y poner `releasesBranches: true`.
- Si la estrategia es `developer-branch`: preguntar el usuario para construir `dev.{usuario}` (lee de `git config user.name` o de `equipo.miembros[0].usuario`).
- Si la estrategia usa nomenclatura temporal (`developer-branch`): poner `tiposTarea` por defecto si esta `null`: `{"DT":"Deuda tecnica","HV":"Evolutivo","BUG":"Correccion","REF":"Refactoring"}`.

d) Mostrar al usuario la **propuesta completa** y pedir confirmacion `[s/N]`. Si confirma, escribir con `jq` preservando el resto del JSON.

```bash
jq --arg estrategia "$NUEVA" \
   --arg mergeStrategy "$MERGE" \
   --arg ramaBase "$RAMA" \
   --arg convencionRamas "$CONV" \
   --argjson pushFeatures $PUSH \
   --argjson reviewersMinimo $REV \
   '.configuracion.branching.estrategia = $estrategia
    | .configuracion.branching.mergeStrategy = $mergeStrategy
    | .configuracion.branching.ramaBase = $ramaBase
    | .configuracion.branching.convencionRamas = $convencionRamas
    | .configuracion.branching.pushFeatures = $pushFeatures
    | .configuracion.branching.branchPolicies.reviewersMinimo = $reviewersMinimo' \
   _duran/ESTADO_PROYECTO.json > _duran/ESTADO_PROYECTO.json.tmp \
   && mv _duran/ESTADO_PROYECTO.json.tmp _duran/ESTADO_PROYECTO.json
```

**IMPORTANTE**: Preservar UTF-8 sin BOM (el hook `json-bom-guard.ps1` del constructor bloqueara si se anade BOM).

e) Anadir entrada al historial de cambios:

```bash
# Anadir nota en _duran/LECCIONES.md o _duran/HISTORIAL_CAMBIOS.md
echo "$(date +%Y-%m-%d): /branching - estrategia $ANTIGUA -> $NUEVA" >> _duran/HISTORIAL_CAMBIOS.md
```

f) Mostrar resumen:

```
[OK] Estrategia cambiada: github-flow → developer-branch
[OK] mergeStrategy: squash → merge
[OK] ramaBase: main → dev.claudio
[OK] convencionRamas: feature/{codigo}-{descripcion} → yyyyMMdd-{tipo}-{codigo}-{descripcion}
[OK] pushFeatures: true → false
[OK] tiposTarea: null → {DT, HV, BUG, REF}

Cambios escritos en _duran/ESTADO_PROYECTO.json.
Proximo /commit, /git-sync y /liberar usaran la nueva estrategia.
```

### 3.bis. Modo `--merge <nombre>` (cambio aislado de mergeStrategy)

Cambia **solo** el campo `configuracion.branching.mergeStrategy` sin tocar `estrategia` ni el resto de campos. Util cuando la estrategia es correcta pero el equipo quiere ajustar el tipo de merge (ej. de `squash` a `no-ff` manteniendo `github-flow`).

a) Validar que `<nombre>` esta en `["squash", "no-ff", "rebase", "merge"]`. Si no, salir con error.

b) Comprobar coherencia con la `estrategia` actual y avisar (no bloquear):

| Estrategia | mergeStrategy esperada | Si difiere |
|---|---|---|
| `github-flow` | `squash` | Warning: "github-flow normalmente usa squash. ¿Confirmas?" |
| `github-flow-simplificado` | `no-ff` | Warning: "github-flow-simplificado usa no-ff para preservar trazabilidad local. ¿Confirmas?" |
| `developer-branch` | `merge` (plain) | Warning: "developer-branch usa plain merge. ¿Confirmas?" |
| `gitflow` / `oneflow` / `gitlab-flow` | `no-ff` | Warning: "Esta estrategia normalmente usa no-ff. ¿Confirmas?" |

El warning **NO bloquea** — solo informa. El usuario puede confirmar con `[s/N]` y aplicar de todas formas (caso valido: equipo que prefiere rebase universal en cualquier estrategia).

c) Mostrar diff antes/despues y pedir confirmacion `[s/N]`.

d) Escribir con `jq` preservando el resto del JSON:

```bash
jq --arg merge "$NUEVA_MERGE" \
   '.configuracion.branching.mergeStrategy = $merge' \
   _duran/ESTADO_PROYECTO.json > _duran/ESTADO_PROYECTO.json.tmp \
   && mv _duran/ESTADO_PROYECTO.json.tmp _duran/ESTADO_PROYECTO.json
```

e) Anadir entrada al historial:

```bash
echo "$(date +%Y-%m-%d): /branching --merge - mergeStrategy $ANTIGUA -> $NUEVA" >> _duran/HISTORIAL_CAMBIOS.md
```

f) Resumen:

```
[OK] mergeStrategy cambiada: squash → no-ff
[INFO] estrategia sigue siendo: github-flow (sin cambios)
Cambios escritos en _duran/ESTADO_PROYECTO.json.
```

### 4. Modo interactivo (default, sin args)

a) Mostrar la config actual (como `--show`).
b) Mostrar el menu:

```
¿Que quieres hacer?
  1. Cambiar estrategia (lista de 9)
  2. Cambiar SOLO mergeStrategy (squash/no-ff/rebase/merge)
  3. Cambiar SOLO convencionRamas (semantica/temporal/combinada/custom)
  4. Cambiar SOLO ramaBase
  5. Cambiar branchPolicies (reviewersMinimo, buildValidation, ...)
  6. Restablecer a defaults coherentes con la estrategia actual
  Q. Salir sin cambios
```

c) Procesar opcion elegida con el mismo flujo del paso 3.

### 5. Modo `--reset`

Lee la `estrategia` actual y aplica todos los defaults coherentes de la tabla del paso 3.c. Util cuando los campos derivados (mergeStrategy, convencionRamas...) se han desalineado.

---

## Coherencia con otros comandos

Tras cambiar la estrategia, los siguientes comandos usaran la nueva config automaticamente (leen `configuracion.branching` en cada invocacion):

- `/commit` - aplica `mergeStrategy` y prefijo de rama
- `/git-sync` - sincroniza segun la nueva estrategia
- `/liberar` - cierra ramas siguiendo la nomenclatura
- `/nuevo-evolutivo` - crea ramas con la nueva convencion
- Regla condicional `.claude/rules/developer-branch.md` se activa/desactiva automaticamente

---

## Cuando NO usar /branching

- **Primera configuracion del proyecto**: usar `/onboarding` (cubre branching + 7 secciones mas).
- **Auditar coherencia sin cambiar nada**: usar `/analizar` o `/branching --show`.
- **Resolver merge conflicts concretos**: usar `/commit` o herramientas Git directas.

---

## Ejemplos

```
/branching --show
# Solo muestra la config actual, sin tocar nada.

/branching --estrategia gitlab-flow
# Cambio directo. Pide confirmacion antes de escribir.

/branching
# Modo interactivo con menu.

/branching --reset
# Reaplica defaults coherentes a la estrategia actual (limpia drift).
```

---

## Anti-patrones a evitar

- **NO mezclar varios cambios sin diff explicito**: si cambias `estrategia`, mostrar TODOS los campos derivados que tambien cambian (no solo `estrategia`).
- **NO escribir sin confirmacion** salvo `--reset` cuando el usuario lo pidio explicitamente.
- **NO duplicar la logica de `/onboarding`**: solo gestiona la seccion `branching`, no toca `equipo`, `infraestructura`, `jira`, etc.
- **NO romper el JSON**: validar con `jq -e .` despues de escribir.

---

*Comando STIC.IA v3.9.0+. Origen: bloque de mejora reconfigurar-estrategia post-onboarding (2026-05-13).*
