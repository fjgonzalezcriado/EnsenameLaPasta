# Automatizacion de Git Hooks

> Skill: git-best-practices | Version: 3.5.0

Pre-commit, commit-msg y pre-push hooks para proyectos .NET y Node.

---

## Git Hooks Disponibles

| Hook | Momento | Uso recomendado |
|------|---------|-----------------|
| `pre-commit` | Antes de crear commit | Build rapido, lint, format |
| `commit-msg` | Tras escribir mensaje | Validar Conventional Commits |
| `pre-push` | Antes de push | Tests, analisis de seguridad |
| `prepare-commit-msg` | Al preparar mensaje | Auto-completar prefijo |
| `post-merge` | Tras merge/pull | Restaurar dependencias |

---

## Opcion 1: Scripts PowerShell (Proyectos .NET)

### Instalacion

```powershell
# install-hooks.ps1
#Requires -Version 5.1

$hooksDir = Join-Path (git rev-parse --git-dir) "hooks"

# Pre-commit: build rapido
$preCommit = @'
#!/bin/sh
echo "Pre-commit: Building..."
dotnet build --no-restore --verbosity quiet
if [ $? -ne 0 ]; then
    echo "ERROR: Build failed. Commit aborted."
    exit 1
fi
'@

# Commit-msg: validar Conventional Commits
$commitMsg = @'
#!/bin/sh
commit_msg=$(cat "$1")
pattern="^(feat|fix|docs|style|refactor|perf|test|chore|ci|build|revert)(\(.+\))?(!)?: .{1,100}$"

if ! echo "$commit_msg" | head -1 | grep -qE "$pattern"; then
    echo "ERROR: Commit message does not follow Conventional Commits."
    echo "Format: type(scope): description"
    echo "Types: feat, fix, docs, style, refactor, perf, test, chore, ci, build, revert"
    echo ""
    echo "Your message: $commit_msg"
    exit 1
fi
'@

Set-Content -Path (Join-Path $hooksDir "pre-commit") -Value $preCommit -NoNewline
Set-Content -Path (Join-Path $hooksDir "commit-msg") -Value $commitMsg -NoNewline

Write-Host "Git hooks installed successfully." -ForegroundColor Green
```

---

## Opcion 2: Husky + lint-staged (Proyectos Node/Full-stack)

### Instalacion

```bash
# Instalar Husky
npm install --save-dev husky lint-staged

# Inicializar Husky
npx husky init

# Pre-commit con lint-staged
echo "npx lint-staged" > .husky/pre-commit

# Commit-msg con commitlint
npm install --save-dev @commitlint/cli @commitlint/config-conventional
echo "npx --no -- commitlint --edit \$1" > .husky/commit-msg
```

### Configuracion lint-staged (package.json)

```json
{
  "lint-staged": {
    "*.{ts,tsx}": [
      "eslint --fix",
      "prettier --write"
    ],
    "*.{css,scss}": [
      "prettier --write"
    ],
    "*.cs": [
      "dotnet format --include"
    ]
  }
}
```

### Configuracion commitlint

```javascript
// commitlint.config.js
module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'scope-enum': [2, 'always', [
      'becas', 'solicitudes', 'auth', 'api',
      'domain', 'infrastructure', 'deps', 'ci'
    ]],
    'subject-max-length': [2, 'always', 100],
  },
};
```

---

## Opcion 3: dotnet-format como Pre-commit (.NET)

```bash
# Instalar dotnet-format (incluido en .NET SDK)
dotnet tool install -g dotnet-format

# Hook pre-commit para .NET
#!/bin/sh
# Pre-commit: format check
staged_cs=$(git diff --cached --name-only --diff-filter=ACM | grep '\.cs$')

if [ -n "$staged_cs" ]; then
    echo "Checking format..."
    dotnet format --verify-no-changes --include $staged_cs
    if [ $? -ne 0 ]; then
        echo "ERROR: Code format issues found. Run 'dotnet format' to fix."
        exit 1
    fi
fi
```

---

## Bypass de Hooks (Solo emergencias)

```bash
# Saltar hooks (usar con precaucion)
git commit --no-verify -m "hotfix: emergencia en produccion"
git push --no-verify

# IMPORTANTE: Solo para emergencias reales
# Los hooks estan para proteger la calidad del codigo
```

---

## Post-merge Hook (Restaurar dependencias)

```bash
#!/bin/sh
# .git/hooks/post-merge
# Restaurar dependencias tras pull/merge

changed_files=$(git diff-tree -r --name-only --no-commit-id ORIG_HEAD HEAD)

# .NET: restaurar NuGet si cambiaron .csproj
if echo "$changed_files" | grep -q '\.csproj$'; then
    echo "Post-merge: Restoring NuGet packages..."
    dotnet restore
fi

# Node: reinstalar si cambio package-lock.json
if echo "$changed_files" | grep -q 'package-lock.json'; then
    echo "Post-merge: Installing npm packages..."
    npm ci
fi
```

---

*Pattern v3.7.0*
