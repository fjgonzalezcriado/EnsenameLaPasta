# Gestion de Releases

> Skill: git-best-practices | Version: 3.5.0

Versionado semantico, tags, changelogs y gestion de releases.

---

## Versionado Semantico (SemVer 2.0)

```
MAJOR.MINOR.PATCH[-prerelease][+build]

Ejemplos:
  1.0.0          # Release inicial
  1.1.0          # Nueva funcionalidad retrocompatible
  1.1.1          # Fix de bug
  2.0.0          # Breaking change
  1.2.0-beta.1   # Pre-release beta
  1.2.0-rc.1     # Release candidate
```

### Reglas

| Incremento | Cuando | Ejemplo |
|------------|--------|---------|
| **MAJOR** | Breaking changes en API o comportamiento | Cambiar firma de metodo publico |
| **MINOR** | Nueva funcionalidad retrocompatible | Nuevo endpoint, nuevo parametro opcional |
| **PATCH** | Bug fix retrocompatible | Corregir error de calculo |

---

## Tags en Git

### Crear tag de release

```bash
# Tag anotado (recomendado) con firma SSH
git tag -a v1.2.0 -m "Release 1.2.0: filtro de becas por estado" --sign

# Tag anotado sin firma
git tag -a v1.2.0 -m "Release 1.2.0: filtro de becas por estado"

# Push del tag
git push origin v1.2.0

# Push de todos los tags pendientes
git push origin --tags
```

### Convencion de tags Comillas

```
v{MAJOR}.{MINOR}.{PATCH}

Ejemplos:
  v1.0.0    # Release inicial
  v1.1.0    # Nueva funcionalidad
  v1.1.1    # Hotfix
  v2.0.0    # Breaking change
```

### Listar tags

```bash
# Todos los tags
git tag -l

# Tags con patron
git tag -l "v1.*"

# Tags con detalle
git tag -l --format='%(tag) %(taggerdate:short) %(subject)' -n
```

---

## Changelog Automatico

### Desde Conventional Commits

```bash
# Los commits Conventional Commits permiten generar changelog automatico
# Tipos que aparecen en changelog:
#   feat: → "Features" (nuevas funcionalidades)
#   fix:  → "Bug Fixes" (correcciones)
#   perf: → "Performance" (mejoras de rendimiento)
#
# Tipos que NO aparecen:
#   docs:, style:, refactor:, test:, chore:, ci:

# Ejemplo de changelog generado:
# ## [1.2.0] - 2026-03-06
#
# ### Features
# - **becas**: añadir filtro por estado (HV-18) (#42)
# - **solicitudes**: validacion de fecha limite (#45)
#
# ### Bug Fixes
# - **auth**: corregir expiracion de token (HV-23) (#48)
```

### Formato de Changelog (CHANGELOG.md)

```markdown
# Changelog

Todos los cambios notables se documentan en este archivo.
Formato basado en [Keep a Changelog](https://keepachangelog.com/).

## [1.2.0] - 2026-03-06

### Agregado
- Filtro de becas por estado y fecha (#42)
- Exportacion a Excel de solicitudes (#45)

### Corregido
- Error en expiracion de token Azure AD (#48)
- Paginacion incorrecta en listado de becas (#50)

### Cambiado
- Actualizado Entity Framework Core a 10.0.1

## [1.1.0] - 2026-02-15
...
```

---

## Flujo de Release

### Release desde main (GitHub Flow)

```bash
# 1. Verificar que main esta limpio
git checkout main
git pull origin main

# 2. Ejecutar tests
dotnet test

# 3. Crear tag
git tag -a v1.2.0 -m "Release 1.2.0" --sign
git push origin v1.2.0

# 4. GitHub Action automatica crea release (ver templates/)
```

### Release con release branch (Release Flow)

```bash
# 1. Crear release branch
git checkout -b release/v1.2.0 main

# 2. Solo fixes en release branch
git cherry-pick <commit-fix>

# 3. Cuando esta listo
git tag -a v1.2.0 -m "Release 1.2.0" --sign
git push origin release/v1.2.0 --tags

# 4. Merge de vuelta a main
git checkout main
git merge release/v1.2.0
```

---

## Reversion de Release

```bash
# Opcion 1: Revert del merge commit
git revert -m 1 <merge-commit-hash>

# Opcion 2: Deploy de version anterior
git checkout v1.1.0
# deploy...

# Opcion 3: Hotfix
git checkout -b hotfix/HV-XX-descripcion main
# ... fix ...
git tag -a v1.2.1 -m "Hotfix"
```

---

*Pattern v3.7.0*
