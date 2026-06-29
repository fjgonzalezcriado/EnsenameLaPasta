Sincroniza con repositorio remoto (pull + push)

# Sincronizar con Repositorio Remoto

> Skill ref
> **Skill**: `git-best-practices` — Skill ref detail

---

## 1. Detección de Control de Versiones

```powershell
if (Test-Path ".git") {
    $vcs = "git"
} elseif ((Test-Path "*.vspscc") -or (Test-Path "$tf")) {
    $vcs = "tfvc"
} else {
    Write-Host "⚠️ No se detectó sistema de control de versiones"
}
```

---

## 2. FLUJO PARA GIT

### 2.1 1. Verificar Estado Local

```bash
# Verificar si hay cambios sin commitear
git status --porcelain
```

If uncommitted changes:
```
⚠️ CAMBIOS SIN COMMITEAR

Tienes cambios locales no commiteados. ¿Qué deseas hacer?

1. Hacer commit primero (/commit)
2. Guardar en stash y continuar
3. Cancelar

Opción (1/2/3):
```

### 2.2 Pre merge checklist

> 🔍 **Pre merge intro**
> → See skill: `git-best-practices/checklists/pre-merge.md`

Pre merge auto checks:

| Verificacion | Comando | Bloqueante |
|--------|---------|-----------|
| Check build | `dotnet build --no-restore -q` | ✅ |
| Check tests | `dotnet test --no-build -q` | ⚠️ If configured |
| Check secrets | Pattern scan | ✅ |
| Check conflict markers | `grep -r "<<<<<<" src/` | ✅ |

```
🔍 PRE-MERGE CHECKLIST
━━━━━━━━━━━━━━━━━━━━━━

  ✅ Check build
  ✅ Check secrets
  ✅ Check conflict markers
  ⬜ Check tests (Skipped no config)

All checks passed
```

### 2.3 Merge strategy

> Merge strategy intro
> → See skill: `git-best-practices/strategies/merge-strategies.md`

> ⚠️ **Adaptable a estrategia**: Claude lee la estrategia de branching de ESTADO_PROYECTO.json para adaptar el comportamiento de merge
> → `_duran/ESTADO_PROYECTO.json` → `configuracion.branching.estrategia`

#### Estrategia por defecto: `github-flow` (PRs + squash)

| Branch type | Estrategia | Razon |
|--------|------------|--------|
| `feature/*` → Actualizar | `git pull --rebase` | Rebase reason |
| `feature/*` → `main` (PR) | Squash merge | Squash reason |
| `hotfix/*` → `main` | Merge commit | Hotfix reason |
| `release/*` → `main` | Merge commit | Release reason |

#### Estrategia simplificada: `github-flow-simplificado` (desarrollador individual, sin PRs)

| Branch type | Estrategia | Razon |
|--------|------------|--------|
| `feature/*` → Actualizar | `git pull --rebase` | Rebase reason |
| `feature/*` → `main` (merge local) | `git merge --no-ff` | Preserva commits individuales, revertible limpiamente con git revert -m1 |
| `hotfix/*` → `main` | `git merge --no-ff` | Hotfix reason |
| `release/*` → `main` | Merge commit | Release reason |

> 💡 **`github-flow-simplificado`**: Merge --no-ff en local en lugar de crear PR. Ideal para desarrollador individual.
> - No requiere PR — merge directo en local
> - Preserva commits individuales para mejor git bisect
> - Revertible limpiamente: git revert -m1 <merge-commit>

#### Estrategia Developer Branch: `developer-branch` (Rama personal remota con features locales)

> Leer nomenclatura de configuracion.branching.convencionRamas: `_duran/ESTADO_PROYECTO.json` → `configuracion.branching`

| Branch type | Estrategia | Razon |
|--------|------------|--------|
| `{ramaBase}` → Actualizar | `git pull origin {ramaBase}` | Rama base configurable (main, master, dev.{usuario}...) |
| `yyyyMMdd-{tipo}-nnn` → `{ramaBase}` | `git merge` (plain) | Flujo: crear rama local → desarrollar → merge a rama base → push |

> ⚠️ **Las ramas feature NUNCA se pushean al remoto**
> - Push de features deshabilitado
> - Confirmar con usuario antes de merge y push

**Flujo: crear rama local → desarrollar → merge a rama base → push:**

```
1. git checkout {ramaBase}              # Posicionarse en rama base
2. git pull origin {ramaBase}           # Actualizar
3. git checkout -b {yyyyMMdd-tipo-nnn}  # Crear rama local (NUNCA pushear)
4. # ... desarrollo + commits ...
5. git checkout {ramaBase}
6. git merge {rama-local}               # ⚠️ CONFIRMAR con usuario
7. git push origin {ramaBase}           # ⚠️ CONFIRMAR con usuario
8. git branch -d {rama-local}           # Limpieza opcional
```

> 💡 **Nomenclatura configurable**: Leer `convencionRamas` de ESTADO_PROYECTO.json.
> Si contiene `{tipo}`, leer `tiposTarea` para los códigos válidos (DT, HV, BUG...).

### 2.4 2. Obtener Cambios Remotos

```bash
# Fetch para ver si hay cambios
# NOTA: En developer-branch, fetch apunta a {ramaBase} (ej: dev.claudio), no a main
git fetch origin

# Ver diferencias
git log HEAD..origin/$(git branch --show-current) --oneline
```

```
📥 CAMBIOS REMOTOS DISPONIBLES

Rama: feature/HV-05
Commits nuevos: 3

  abc1234 - feat: Añadir validación (jperez, hace 2h)
  def5678 - fix: Corregir error (mlopez, hace 1h)
  ghi9012 - docs: Actualizar README (jgarcia, hace 30m)

¿Descargar e integrar? (s/n):
```

### 2.5 3. Integrar Cambios (Pull con Rebase)

```bash
# Rebase para historial limpio (branch tipo feature/*)
git pull --rebase origin $(git branch --show-current)
```

If conflicts:
```
⚠️ CONFLICTOS DETECTADOS

Archivos en conflicto:
  • src/Controllers/BecasController.cs
  • src/Services/BecaService.cs

Opciones:
1. Resolver manualmente (abre VS Code)
2. Aceptar cambios remotos (theirs)
3. Mantener mis cambios (ours)
4. Abortar rebase

Opción (1/2/3/4):
```

### 2.6 4. Subir Cambios Locales (Push)

```bash
# Push normal (en developer-branch: solo pushear ramaBase, NUNCA features)
git push origin $(git branch --show-current)

# Después de rebase con conflictos resueltos → force-with-lease (NUNCA --force)
# NOTA: En developer-branch con pushFeatures=false, NUNCA pushear ramas locales
git push --force-with-lease origin $(git branch --show-current)
```

> ⚠️ **Force with lease**
> Force with lease intro:
> - `--force-with-lease`: ✅ Force with lease safe
> - `--force`: ❌ Force dangerous
> → See skill: `git-best-practices/strategies/merge-strategies.md`

```
📤 SUBIENDO CAMBIOS

Rama: feature/HV-05
Commits a subir: 2

  xyz1234 - [HV-05] feat(api): Añadir paginación
  xyz5678 - [HV-05] test: Añadir tests de paginación

✅ Push completado
```

### 2.7 Post merge restore

> 🔄 **Post merge restore intro**

After pull detect:

```bash
# Detectar si package-lock.json o *.csproj cambiaron en los commits descargados
git diff HEAD@{1}..HEAD --name-only | grep -E "(\.csproj|packages\.lock\.json|package-lock\.json|yarn\.lock)"
```

If dependencies changed:

| File type | Restore command |
|---------|---------|
| `*.csproj` / `packages.lock.json` | `dotnet restore` |
| `package-lock.json` | `npm ci` |
| `yarn.lock` | `yarn install --frozen-lockfile` |

```
🔄 Dependency changes detected

Changed files:
  • src/Comillas.MiApp.Web/Comillas.MiApp.Web.csproj
  • src/Comillas.MiApp.Infrastructure/Comillas.MiApp.Infrastructure.csproj

Restoring: dotnet restore...
✅ Restore completed
```

### 2.8 5. Confirmación

```
✅ SINCRONIZACIÓN COMPLETADA

📥 Descargados: 3 commits
📤 Subidos: 2 commits
🔀 Rama: feature/HV-05
🔍 Pre merge checklist: ✅ All passed
🔄 Dependency restore: ✅ dotnet restore
⏱️ Última sincronización: [FECHA_HORA]

Estado: Al día con origin/{0}
```

---

## 3. FLUJO PARA TFVC

> ⚠️ **PROYECTO TFVC DETECTADO**

En TFVC no existe el concepto de push/pull como en Git. Las operaciones equivalentes son:

### 3.1 Obtener cambios (como git pull)

```
╔══════════════════════════════════════════════════════════════════════╗
║  📥 OBTENER CAMBIOS EN TFVC                          ║
╠══════════════════════════════════════════════════════════════════════╣
║                                                                      ║
║  OPCIÓN 1 - Línea de comandos:                           ║
║  ─────────────────────────────                                       ║
║  tf get /recursive                                                   ║
║                                                                      ║
║  OPCIÓN 2 - Visual Studio:                           ║
║  ─────────────────────────                                           ║
║  1. Team Explorer → Source Control Explorer                      ║
║  2. Click derecho en carpeta → "Get Latest Version"                      ║
║                                                                      ║
║  OPCIÓN 3 - Obtener versión específica:                           ║
║  ─────────────────────────────────────                              ║
║  tf get /version:C12345  (changeset específico)                 ║
║  tf get /version:D2026-01-24  (por fecha)                           ║
║                                                                      ║
╚══════════════════════════════════════════════════════════════════════╝
```

### 3.2 Subir cambios (como git push)

```
╔══════════════════════════════════════════════════════════════════════╗
║  📤 SUBIR CAMBIOS EN TFVC                         ║
╠══════════════════════════════════════════════════════════════════════╣
║                                                                      ║
║  En TFVC, el checkin sube automáticamente al servidor. No hay operación separada de "push".                             ║
║                                                                      ║
║  Para hacer checkin:                          ║
║  tf checkin /comment:"mensaje" /noprompt                            ║
║                                                                      ║
║  O usa el comando: /commit                       ║
║                                                                      ║
╚══════════════════════════════════════════════════════════════════════╝
```

### 3.3 Resolver conflictos en TFVC

```
╔══════════════════════════════════════════════════════════════════════╗
║  ⚠️ RESOLVER CONFLICTOS EN TFVC                    ║
╠══════════════════════════════════════════════════════════════════════╣
║                                                                      ║
║  Si hay conflictos al obtener cambios:                        ║
║                                                                      ║
║  OPCIÓN 1 - Visual Studio:                      ║
║  ─────────────────────────                                           ║
║  Team Explorer → Resolve Conflicts                  ║
║                                                                      ║
║  OPCIÓN 2 - Línea de comandos:                      ║
║  ─────────────────────────────                                       ║
║  tf resolve /auto:AcceptMerge    (merge automático)                    ║
║  tf resolve /auto:TakeTheirs     (aceptar servidor)                       ║
║  tf resolve /auto:KeepYours      (mantener local)                         ║
║                                                                      ║
╚══════════════════════════════════════════════════════════════════════╝
```

### 3.4 Ver historial

```
📋 VER HISTORIAL EN TFVC

tf history /recursive /noprompt /format:detailed

O en Visual Studio: Click derecho → View History
```

---

## 4. RESUMEN DE COMANDOS

| Acción | Git | TFVC |
|--------|-----|------|
| Obtener cambios | `git pull --rebase` | `tf get /recursive` |
| Subir cambios | `git push` | Included in checkin |
| Push after rebase | `git push --force-with-lease` | N/A |
| Ver historial | `git log` | `tf history` |
| Resolver conflictos | `git mergetool` | `tf resolve` |
| Descartar cambios | `git checkout -- .` | `tf undo /recursive` |
| Restore deps | `dotnet restore` / `npm ci` | `dotnet restore` / `npm ci` |

---

## 5. Configuracion

Config intro `_duran/ESTADO_PROYECTO.json`:

| Nivel de integracion | Comportamiento |
|-------|--------|
| **basico** | Config basic |
| **medio** | Config medium |
| **alto** | Config high |

> Tfvc always basic

---

## 6. Error cases

### No connection

```
❌ Connection error

Verify connection:
  • Check internet
  • Check credentials
  • Check remote url: git remote -v
```

### Branch not published

```
⚠️ Branch not published title

Branch not published msg: feature/HV-05

Publish branch:
  git push -u origin feature/HV-05

Publish now
```

### Rebase conflict

```
⚠️ Rebase conflict title

Rebase conflict msg

To continue after resolve:
  git rebase --continue

To abort:
  git rebase --abort

After rebase push:
  git push --force-with-lease origin $(git branch --show-current)
```

---

## ⚠️ Critical reminders

### Siempre

- ✅ Always check uncommitted
- ✅ Always rebase
- ✅ Always pre merge checklist (2.2)
- ✅ Always force with lease (2.6)
- ✅ Always post merge restore (2.7)
- ✅ Always show commits
- ✅ Always offer conflict options

### Nunca

- ❌ `git push --force` — Never force reason
- ❌ Never pull without warning
- ❌ Never auto resolve conflicts
- ❌ Never tfvc in git
- ❌ Never skip pre merge

### Skill integration

Skill integration intro `git-best-practices`:

| Recurso | Uso |
|---------|-----|
| `strategies/merge-strategies.md` | Skill merge |
| `strategies/branching-models.md` | Skill branching |
| `checklists/pre-merge.md` | Skill pre merge |
| `tools/git-hooks-automation.md` | Skill hooks |
| `patterns/ssh-signing.md` | Skill signing |

---

## Execution flow diagram

```
┌─────────────────┐
│ 1. Detectar VCS  │
└────────┬────────┘
         │
    ┌────┴────┐
    │  Git?   │
    └────┬────┘
    Yes  │  No → TFVC flow (sección 3)
         ▼
┌─────────────────┐
│ 2. git status    │ → Cambios? → Commit/Stash/Cancel
└────────┬────────┘
         ▼
┌─────────────────┐
│ 3. Pre-merge     │ → Build + Secrets + Conflict markers
│    CHECKLIST     │
└────────┬────────┘
         ▼
┌─────────────────┐
│ 4. git fetch     │ → Ver commits remotos
└────────┬────────┘
         ▼
┌─────────────────┐
│ 5. Merge strategy│ → Adaptar según tipo de rama
│    (rebase/merge)│
└────────┬────────┘
         ▼
┌─────────────────┐
│ 6. git pull      │ → Conflictos? → Resolver
│    --rebase      │
└────────┬────────┘
         ▼
┌─────────────────┐
│ 7. Post-merge    │ → ¿Cambió .csproj? → dotnet restore
│    RESTORE       │ → ¿Cambió package-lock? → npm ci
└────────┬────────┘
         ▼
┌─────────────────┐
│ 8. git push      │ → Normal o --force-with-lease
│    (según nivel) │
└────────┬────────┘
         ▼
┌─────────────────┐
│ 9. Confirmación  │ → Resumen completo
└─────────────────┘
```

---

## NOTAS

- En **Git**: Pull + Push son operaciones separadas
- En **TFVC**: Get obtiene cambios, Checkin sube (no hay push separado)
- El comando `/commit` ya maneja ambos VCS
- Este comando (`/git-sync`) muestra instrucciones adaptadas al VCS detectado
- Note pre merge
- Note restore
