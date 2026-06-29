# Checklist: Setup de Repositorio

> Skill: git-best-practices | Version: 3.5.0

Checklist para configurar correctamente un repositorio Git nuevo.

---

## 1. Inicializacion

- [ ] `git init` con `--initial-branch=main`
- [ ] `.gitignore` apropiado para el stack (ver `patterns/gitignore-patterns.md`)
- [ ] `.gitattributes` con line endings y LFS (ver `patterns/gitattributes-config.md`)
- [ ] `.editorconfig` para consistencia entre editores
- [ ] `README.md` con descripcion, setup, contribucion

---

## 2. Configuracion de Firmado

- [ ] SSH signing configurado (ver `patterns/ssh-signing.md`)
- [ ] `commit.gpgsign = true` (global o local)
- [ ] `tag.gpgsign = true`
- [ ] Clave publica registrada en GitHub/GitLab como "Signing Key"
- [ ] `allowed_signers` configurado para verificacion local

---

## 3. Branch Protection (GitHub)

- [ ] Branch `main` protegida
- [ ] Require pull request antes de merge
- [ ] Require al menos 1 aprobacion
- [ ] Dismiss stale reviews al push nuevo
- [ ] Require status checks (CI) antes de merge
- [ ] Require branches up to date antes de merge
- [ ] No permitir force push a main
- [ ] No permitir delete de main
- [ ] Preferir GitHub Rulesets sobre Branch Protection Rules legacy

### GitHub Rulesets (Recomendado 2025+)

```
Repository Settings > Rules > Rulesets > New ruleset

Nombre: Proteccion Main
Target: main
Rules:
  [x] Restrict deletions
  [x] Require a pull request before merging
      - Required approvals: 1
      - Dismiss stale reviews
  [x] Require status checks to pass
      - build
      - test
  [x] Block force pushes
  [x] Require signed commits (opcional)
```

---

## 4. Templates

- [ ] `.github/pull_request_template.md` con checklist de PR
- [ ] `.github/ISSUE_TEMPLATE/` con plantillas de bug/feature
- [ ] `.github/CODEOWNERS` si hay multiples equipos

### PR Template sugerido

```markdown
## Descripcion
[Descripcion del cambio]

## Tipo de cambio
- [ ] Bug fix
- [ ] Nueva funcionalidad
- [ ] Breaking change
- [ ] Documentacion

## Checklist
- [ ] Tests añadidos/actualizados
- [ ] Documentacion actualizada
- [ ] Build pasa correctamente
- [ ] Sin warnings nuevos
```

---

## 5. CI/CD

- [ ] Workflow de build + test en PR
- [ ] Workflow de deploy en merge a main (o manual)
- [ ] Secretos en GitHub Secrets (no en codigo)
- [ ] Branch-specific workflows con path filters

---

## 6. Git Hooks (Opcional)

- [ ] `commit-msg`: Validar Conventional Commits
- [ ] `pre-commit`: Build rapido o lint
- [ ] `pre-push`: Tests
- [ ] Usar Husky (Node) o scripts PS para instalacion automatica

---

## 7. Configuracion de Equipo

```bash
# Cada desarrollador debe ejecutar:
git config --global user.name "Nombre Apellido"
git config --global user.email "usuario@comillas.edu"
git config --global init.defaultBranch main
git config --global pull.rebase true
git config --global fetch.prune true
git config --global diff.colorMoved zebra
```

---

*Checklist v3.7.0*
