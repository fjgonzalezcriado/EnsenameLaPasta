# Estrategias de Merge

> Skill: git-best-practices | Version: 3.5.0

Cuando usar rebase, merge commit o squash merge.

---

## Comparativa

| Estrategia | Historial | Cuando usar | Riesgo |
|------------|-----------|-------------|--------|
| **Merge commit** | Preserva toda la historia | Branches con multiples cambios logicos | Historial ruidoso |
| **Squash merge** | 1 commit por feature | Feature branches normales | Pierde commits intermedios |
| **Rebase** | Lineal, limpio | Actualizar branch con main | No usar en ramas compartidas |

---

## Recomendacion Comillas

### Para Pull Requests: Squash Merge (Default)

```bash
# GitHub: Configurar en Settings > General > Pull Requests
# [x] Allow squash merging (default)
# [x] Allow merge commits
# [ ] Allow rebase merging

# El mensaje resultante sigue Conventional Commits:
# feat(HV-18): añadir filtro de becas por estado (#42)
```

**Ventajas:**
- Un commit limpio por feature en main
- Historial legible y facil de navegar
- Facil de revertir (un commit = una feature)

### Para actualizar rama con main: Rebase

```bash
# Actualizar feature branch con cambios de main
git fetch origin
git rebase origin/main

# Si hay conflictos:
# 1. Resolver conflicto en el archivo
# 2. git add <archivos-resueltos>
# 3. git rebase --continue

# Forzar push (solo en TU rama, nunca en main)
git push --force-with-lease
```

**Reglas de seguridad:**
- NUNCA hacer rebase de `main` o `develop`
- NUNCA hacer `git push --force` (usar `--force-with-lease`)
- Solo hacer rebase en ramas propias que nadie mas usa

### Para hotfixes: Merge commit

```bash
# Hotfixes necesitan trazabilidad completa
git checkout main
git merge --no-ff hotfix/HV-25-error-login
git tag -a v1.2.1 -m "Hotfix: error login produccion"
```

---

## Resolucion de Conflictos

### Flujo recomendado

```bash
# 1. Actualizar main local
git fetch origin

# 2. Rebase sobre main
git rebase origin/main

# 3. Si hay conflictos, Git indica los archivos
# CONFLICT (content): Merge conflict in src/Services/BecaService.cs

# 4. Abrir archivo, buscar marcadores de conflicto
<<<<<<< HEAD (tu codigo)
var becas = await _repo.GetActivasAsync(ct);
=======
var becas = await _repo.GetAllAsync(filter, ct);
>>>>>>> origin/main (codigo de main)

# 5. Elegir la version correcta (o combinar)
var becas = await _repo.GetAllAsync(filter, ct);

# 6. Marcar como resuelto y continuar
git add src/Services/BecaService.cs
git rebase --continue
```

### Herramientas de merge

```bash
# Configurar herramienta visual
git config --global merge.tool vscode
git config --global mergetool.vscode.cmd 'code --wait --merge $REMOTE $LOCAL $BASE $MERGED'

# Visual Studio tiene merge integrado en Team Explorer
```

---

## Anti-patrones

| Anti-patron | Problema | Solucion |
|-------------|----------|----------|
| `git push --force` en main | Destruye historial de otros | Usar `--force-with-lease` en ramas propias |
| Merge de main a feature | Merge commits innecesarios | Usar rebase |
| Commits gigantes | Dificil review, dificil revert | Commits atomicos |
| No resolver conflictos | PRs bloqueados | Rebase frecuente |
| Rebase de ramas compartidas | Reescribe historia de otros | Solo rebase en ramas propias |

---

*Pattern v3.7.0*
