# Checklist: Preparacion para Git 3.0

> Skill: git-best-practices | Version: 3.5.0

Git 3.0 esta previsto para finales de 2026. Estos son los cambios que preparar.

---

## Cambios Confirmados en Git 3.0

### 1. SHA-256 como Default

**Que cambia:** Los nuevos repositorios usaran SHA-256 en lugar de SHA-1 por defecto.

- [ ] Verificar que herramientas y scripts no dependen del formato SHA-1 (40 chars hex)
- [ ] SHA-256 produce hashes de 64 chars hex
- [ ] Los repositorios existentes NO se rompen (interoperabilidad SHA-1/SHA-256)
- [ ] Planificar migracion de repositorios existentes (no urgente, hay interop)

```bash
# Crear repo con SHA-256 (ya disponible en Git 2.52)
git init --object-format=sha256

# Verificar formato de un repo
git rev-parse --show-object-format
```

### 2. Rust como Dependencia Obligatoria

**Que cambia:** Git 3.0 requiere Rust para compilar. Partes criticas se reescriben en Rust.

- [ ] Verificar que entornos de build tienen Rust disponible
- [ ] Actualizar Docker images de CI/CD para incluir Rust toolchain
- [ ] No afecta al USO de Git, solo a la COMPILACION desde fuente
- [ ] Las distribuciones (apt, brew, winget) manejaran esto transparentemente

### 3. Default Branch: main

**Que cambia:** `git init` creara `main` por defecto (ya configurado en Git 2.28+).

- [ ] Verificar que todos los repos usan `main` (no `master`)
- [ ] Actualizar scripts que referencien `master` hardcodeado
- [ ] Actualizar documentacion que mencione `master`

```bash
# Verificar configuracion actual
git config --global init.defaultBranch
# Debe ser: main

# Cambiar si no esta configurado
git config --global init.defaultBranch main
```

---

## Cambios a Monitorizar

### Reftable Backend

Git 3.0 puede cambiar el backend de almacenamiento de referencias.

- [ ] Reftable mejora rendimiento en repos con muchas ramas/tags
- [ ] Compatible hacia atras
- [ ] No requiere accion inmediata

### Deprecaciones

- [ ] `git checkout` para cambio de ramas → usar `git switch`
- [ ] `git checkout` para restaurar archivos → usar `git restore`
- [ ] `git stash save` → usar `git stash push`

```bash
# Comandos modernos (ya disponibles)
git switch feature/HV-18       # En lugar de: git checkout feature/HV-18
git switch -c nueva-rama        # En lugar de: git checkout -b nueva-rama
git restore archivo.cs          # En lugar de: git checkout -- archivo.cs
git stash push -m "descripcion" # En lugar de: git stash save "descripcion"
```

---

## LTS de Git 2.x

La ultima version de Git antes de 3.0 sera LTS con:
- Bug fixes por 4 ciclos de release
- Security fixes por 6 ciclos de release

- [ ] Planificar ventana de migracion a Git 3.0 (no urgente)
- [ ] Mantener Git actualizado durante la transicion

---

## Timeline Estimado

| Fecha | Evento |
|-------|--------|
| Nov 2025 | Git 2.52 (preparaciones 3.0) |
| 2026 Q1-Q2 | Git 2.53-2.54 (Rust por defecto) |
| 2026 Q3-Q4 | **Git 3.0** (SHA-256 default, breaking changes) |
| 2026 Q4+ | Git 2.x LTS (soporte extendido) |

---

*Checklist v3.7.0*
