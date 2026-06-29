---
description: Crea commit con conventional commits y verificaciones pre-commit
---

Crea commit con conventional commits y verificaciones pre-commit

# Commit/Checkin con Verificaciones

> Skill ref
> **Skill**: `git-best-practices` — Skill ref detail

---

## 1. Detect vcs

```powershell
# Detectar Git o TFVC
if (Test-Path ".git") {
    $vcs = "git"
    Write-Host "🔀 Git detectado"
} elseif ((Test-Path "*.vspscc") -or (Test-Path "$tf")) {
    $vcs = "tfvc"
    Write-Host "🔀 TFVC detectado"
} else {
    Write-Host "⚠️ No vcs"
    # Continuar sin VCS - solo generar mensaje
}
```

---

## 2. Flow git

### 2.1 Show pending changes

```bash
git status --short
git diff --stat
```

### 2.2 Selective staging

> ⚠️ **Never git add all**
> `git add -A` Git add all risk

```bash
# ✅ Staging selectivo - solo archivos relevantes
git add src/Controllers/BecasController.cs
git add src/Services/BecaService.cs
git add tests/BecaServiceTests.cs

# ✅ Staging por directorio
git add src/Features/Becas/

# ✅ Staging interactivo (revisar cambio por cambio)
git add -p

# ❌ NUNCA: git add -A (puede incluir .env, secrets, binarios)
# ❌ NUNCA: git add . (mismo problema)
```

Staging behavior:

| Nivel de integracion | Staging action |
|-------|--------|
| **Level basic** | Staging basic |
| **Level medium** | Staging medium |
| **Level high** | Staging high |

### 2.3 Secrets scan

> 🔒 **Mandatory before commit**

```bash
# Escanear archivos staged por patterns de secrets
git diff --cached --name-only | while read file; do
    # Buscar patterns peligrosos
    grep -nE "(password|secret|api[_-]?key|connectionstring|token)\s*[:=]" "$file" 2>/dev/null
done
```

Secrets patterns:

| Pattern | Riesgo | Ejemplo |
|---------|--------|---------|
| `password\s*[:=]` | 🔴 Critico | `password = "abc123"` |
| `(api[_-]?key\|secret)\s*[:=]` | 🔴 Critico | `apiKey = "sk-..."` |
| `connectionstring.*password` | 🔴 Critico | `Server=prod;Password=...` |
| `-----BEGIN.*PRIVATE KEY` | 🔴 Critico | Private key |
| `Bearer [a-zA-Z0-9._-]{20,}` | 🟠 Alto | Hardcoded token |

Secrets action:
```
🔴 SECRETS DETECTADOS - COMMIT BLOQUEADO

Secrets found in:
  • src/appsettings.json:14 → password = "..."
  • src/Services/ApiClient.cs:8 → apiKey = "sk-..."

Secrets options:
  [1] Secrets exclude
  [2] Secrets review
  [3] Secrets abort
```

### 2.4 Pre commit checks

```powershell
# 1. Verificar compilación (en 03_Desarrollo/)
Push-Location "03_Desarrollo"
dotnet build --no-restore --verbosity quiet
Pop-Location

# 2. Invocar hooks existentes si están configurados
# Ver skill git-best-practices → tools/git-hooks-automation.md
```

Hooks check:
```
💡 No hooks detected

Hooks recommendation:
  - pre-commit: build + format check
  - commit-msg: Conventional Commits validation

Hooks install suggestion:
  → Ver skill git-best-practices → tools/git-hooks-automation.md
  → O ejecutar: .claude/skills/git-best-practices/templates/commit-msg-hook.sh.template
```

### 2.5 Build message

Build message intro:

```
📝 CONVENTIONAL COMMITS (v1.0)

Tipo:
  1. feat      - Type feat
  2. fix       - Type fix
  3. refactor  - Type refactor
  4. docs      - Type docs
  5. test      - Type test
  6. chore     - Type chore
  7. perf      - Type perf
  8. style     - Type style
  9. ci        - Type ci
  10. build    - Type build
  11. revert   - Type revert

Ambito (Opcional): api, auth, becas, domain, infrastructure, deps, ci...
Short description:
Breaking change (s/n):
```

### 2.6 Message validation

> Validate before commit

```regex
^(feat|fix|docs|style|refactor|perf|test|chore|ci|build|revert)(\(.+\))?(!)?: .{1,100}$
```

Validation examples:

| Mensaje | Valido | Razon |
|---------|--------|---------|
| `feat(becas): añadir filtro por estado` | ✅ | Valid format |
| `feat(api)!: cambiar formato respuesta` | ✅ | Breaking format |
| `Fixed bug` | ❌ | No type |
| `feat: a` | ❌ | Too short |
| `update` | ❌ | No conventional |

### 2.7 Breaking changes

Breaking changes intro:

```bash
# Opcion 1: Con "!" despues del tipo
feat(api)!: cambiar formato de respuesta paginada

# Opcion 2: Con BREAKING CHANGE en el footer
feat(api): cambiar formato de respuesta paginada

BREAKING CHANGE: PaginatedResult ahora usa TotalPages en lugar de TotalCount.
Los clientes deben actualizar sus modelos de respuesta.
```

### 2.8 Footer references

Footer intro:

```bash
# Cerrar ticket
feat(becas): implementar exportacion Excel

Closes: HV-18

# Azure DevOps Work Items
feat(becas): implementar exportacion Excel

AB#1234

# Multiples referencias
fix(auth): corregir refresh de token Azure AD

Fixes: HV-25
See also: HV-22, HV-24
Co-authored-by: Juan Garcia <jgarcia@comillas.edu>
```

### 2.9 Associate evolutivo

Associate intro:

> Leer `configuracion.branching.convencionRamas` de `_duran/ESTADO_PROYECTO.json` para determinar el formato del prefijo.

**Nomenclatura semantica** (`feature/{codigo}-{descripcion}`):
```
Message result: [HV-05] feat(api): Agregar paginación
```

**Nomenclatura temporal** (`yyyyMMdd-{tipo}-{codigo}-{descripcion}`):
```
Rama actual: 20260406-HV-18-filtro-becas
Mensaje: [20260406-HV-18] feat(becas): Agregar filtro por estado
```

Si `convencionRamas` contiene `{tipo}`, el prefijo del commit incluye la fecha y tipo de la rama actual.

### 2.10 Signing reminder

Signing intro:

```
🔐 Signing detected

Signing method: SSH (Git 2.34+)
Signing key: ~/.ssh/id_ed25519.pub

Commit will be signed
→ Ver skill git-best-practices → patterns/ssh-signing.md
```

### 2.11 Execute commit

Execute by level:

| Nivel de integracion | Comportamiento |
|-------|--------|
| **Level basic** | Behavior basic |
| **Level medium** | Behavior medium |
| **Level high** | Behavior high |

```bash
# Staging selectivo (ya realizado en 2.2)
git commit -m "[HV-05] feat(api): Agregar paginación

Closes: HV-05"
```

---

## 3. Flow tfvc

> ⚠️ **Tfvc detected**: Tfvc note

### 3.1 Tfvc show changes

```
📋 Tfvc pending changes
━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Tfvc view changes:
  tf status /recursive

Tfvc or vs:
  Team Explorer → Pending Changes
```

### 3.2 Tfvc generate message

Tfvc generate intro:

```
📝 Tfvc message generated
━━━━━━━━━━━━━━━━━━

[HV-05] feat(api): Agregar paginación a endpoint de becas

Evolutivo: HV-05
Tipo: feat
Ambito: api
```

### 3.3 Tfvc checkin instructions

```
╔══════════════════════════════════════════════════════════════════════╗
║  📋 CHECKIN EN TFVC                                                  ║
╠══════════════════════════════════════════════════════════════════════╣
║                                                                      ║
║  Tfvc option1:                              ║
║  ─────────────────────────────────────────────                       ║
║  tf checkin /comment:"[HV-05] feat(api): Agregar paginación" /noprompt
║                                                                      ║
║  Tfvc option2:                              ║
║  ─────────────────────────────────────────────                       ║
║  1. Abrir Team Explorer → Pending Changes                            ║
║  2. En "Comment", pegar:                                             ║
║     [HV-05] feat(api): Agregar paginación                           ║
║  3. Verificar archivos incluidos                                     ║
║  4. Click en "Check In"                                              ║
║                                                                      ║
║  Tfvc option3:                              ║
║  ─────────────────────────────────────────────                       ║
║  tf checkin /comment:"[HV-05] feat(api): ..." /associate:12345      ║
║                                                                      ║
╚══════════════════════════════════════════════════════════════════════╝
```

### 3.4 Tfvc shelveset tip

```
💡 TIP: Tfvc shelveset intro

Tfvc shelveset create (como git stash):
  tf shelve "WIP-HV-05" /noprompt

Tfvc shelveset recover:
  tf unshelve "WIP-HV-05"

Tfvc shelveset list:
  tf shelvesets /owner:$env:USERNAME
```

---

## 4. Update context

Update context intro `_duran/HISTORIAL_CAMBIOS.md`:

```markdown
### [FECHA] - feat(api): Agregar paginación
- **Commit/Changeset**: [hash o pendiente de checkin]
- **Evolutivo**: HV-05
- **Archivos**: 5 archivos modificados
- **Firmado**: ✅ SSH / ❌ Sin firmar

**Cambios:**
- `BecasController.cs` - Añadido parámetros de paginación
- `GetBecasQuery.cs` - Implementado paginación
```

---

## 5. Confirmation

### Git:
```
✅ COMMIT EXITOSO

📝 Mensaje: [HV-05] feat(api): Agregar paginación
🔢 Hash: abc1234
📁 Archivos: 5 (staged selectivamente)
📋 Evolutivo: HV-05
🔐 Firmado: ✅ SSH
🔍 Secrets scan: ✅ No secrets found
✅ Validacion: Conventional Commits OK

💡 Siguiente paso: /git-sync Or manual git push
```

### TFVC:
```
✅ Tfvc message ready

📝 Mensaje: [HV-05] feat(api): Agregar paginación
📁 Pending files: 5
📋 Evolutivo: HV-05
🔍 Secrets scan: ✅ No secrets found
✅ Validacion: Conventional Commits OK

⚠️ Tfvc reminder
   Tfvc or command: tf checkin /comment:"..." /noprompt

💡 Siguiente paso: Tfvc verify
```

---

## 6. Vcs comparison

| Accion | Git | TFVC |
|--------|-----|------|
| View changes | `git status` | `tf status` |
| Stage files | `git add <files>` | Automatic |
| Commit/Checkin | `git commit -m "..."` | `tf checkin /comment:"..."` |
| Push | `git push` | Included in checkin |
| Stash | `git stash` | `tf shelve` |

---

## 7. Configuracion

Config intro `_duran/ESTADO_PROYECTO.json`:

| Nivel de integracion | Crea commit con conventional commits y verificaciones pre-commit |
|-------|-------------|
| **basico** | Config basic |
| **medio** | Config medium |
| **alto** | Config high |

> Tfvc always basic

---

## 8. Conventional commits ref

### All types

| Tipo | Crea commit con conventional commits y verificaciones pre-commit | Ejemplo | Changelog |
|------|-------------|---------|-----------|
| `feat` | Type feat | `feat(becas): añadir filtro por estado` | ✅ |
| `fix` | Type fix | `fix(auth): corregir expiración de token` | ✅ |
| `docs` | Type docs | `docs(readme): actualizar instrucciones` | ❌ |
| `style` | Type style | `style: aplicar formato dotnet-format` | ❌ |
| `refactor` | Type refactor | `refactor(repo): extraer método común` | ❌ |
| `perf` | Type perf | `perf(query): optimizar consulta paginada` | ✅ |
| `test` | Type test | `test(becas): añadir tests de servicio` | ❌ |
| `chore` | Type chore | `chore(deps): actualizar EF Core a 10.0.1` | ❌ |
| `ci` | Type ci | `ci: añadir workflow de build .NET 10` | ❌ |
| `build` | Type build | `build: migrar a Central Package Management` | ❌ |
| `revert` | Type revert | `revert: feat(becas): filtro por estado` | ✅ |

### Message format

```
[CÓDIGO] tipo(ámbito): descripción breve

Cuerpo opcional con más detalles sobre el cambio.

BREAKING CHANGE: descripción del breaking change (si aplica)
Closes: HV-XX
AB#1234
Co-authored-by: Nombre <email>
```

---

## ⚠️ Critical reminders

### Siempre

- ✅ Always detect vcs
- ✅ Always scan secrets (2.3)
- ✅ Always selective staging (2.2)
- ✅ Always validate message (2.6)
- ✅ Always build (2.4)
- ✅ Always evolutivo
- ✅ Always conventional commits
- ✅ Always update historial

### Nunca

- ❌ `git add -A` / `git add .` — Never git add all reason
- ❌ Never git in tfvc
- ❌ Never commit without build
- ❌ Never generic messages ("cambios", "updates", "WIP")
- ❌ Never commit secrets
- ❌ Never skip validation

### Skill integration

Skill integration intro `git-best-practices`:

| Recurso | Uso |
|---------|-----|
| `patterns/conventional-commits.md` | Skill cc |
| `patterns/ssh-signing.md` | Skill signing |
| `tools/git-hooks-automation.md` | Skill hooks |
| `templates/commit-msg-hook.sh.template` | Skill hook template |
| `checklists/pre-merge.md` | Skill pre merge |

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
│ 2. git status    │
│    git diff      │
└────────┬────────┘
         ▼
┌─────────────────┐
│ 3. Staging       │
│    SELECTIVO     │ ← ❌ NUNCA git add -A
└────────┬────────┘
         ▼
┌─────────────────┐
│ 4. Scan SECRETS  │ → 🔴 Bloqueado si detecta
└────────┬────────┘
         ▼
┌─────────────────┐
│ 5. dotnet build  │ → ❌ Abort si falla
└────────┬────────┘
         ▼
┌─────────────────┐
│ 6. Construir     │
│    mensaje CC    │ → Validar regex
│    (11 tipos)    │
└────────┬────────┘
         ▼
┌─────────────────┐
│ 7. Asociar       │
│    evolutivo     │ → [HV-XX] prefix
└────────┬────────┘
         ▼
┌─────────────────┐
│ 8. git commit    │ → Con firma si configurada
│    (según nivel) │
└────────┬────────┘
         ▼
┌─────────────────┐
│ 9. Actualizar    │
│    HISTORIAL     │
└────────┬────────┘
         ▼
┌─────────────────┐
│ 10. Confirmación │ → 💡 Sugerir /git-sync
└─────────────────┘
```
