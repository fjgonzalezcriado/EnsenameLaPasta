---
name: git-best-practices
description: >
  Implements Git best practices for Comillas teams: 13 branching strategies,
  Conventional Commits, SSH/GPG signing, Azure DevOps branch policies and
  YAML pipelines, GitHub Rulesets, Git hooks automation, CI/CD pipelines,
  merge queues, feature flags, DORA metrics, monorepo patterns, and Git 3.0
  preparation. Configurable branch naming and base branch.
  USE FOR: branching strategies, commit conventions, merge policies, CI/CD
  pipelines, Git hooks, branch protection, monorepo patterns, estrategia
  branching, convenciones commit, politicas merge.
  DO NOT USE FOR: modifying application code, security auditing
  (use security-audit), deployment configuration (use azure-config).
allowed-tools: Read, Grep, Glob, Bash(git *)
context: fork
agent: Explore
---

# Mejores Practicas de Git - Comillas

> Basado en Git 2.52+ (2025-2026), Conventional Commits v1.0, GitHub Flow, Trunk-Based Development.
> Documento fuente: Arquitectura de Software STIC.

---

## Cuando Usar Este Skill

1. **Estrategia de ramas** - Decidir branching model (13 estrategias: GitHub Flow, GitHub Flow simplificado, Developer Branch, Trunk-Based, Release Flow, OneFlow, Developer Flow, GitLab Flow, GitFlow, Ship/Show/Ask, Stacked PRs, Forking, Environment Branching)
2. **Conventional Commits** - Estandarizar mensajes de commit
3. **Firmado de commits** - Configurar SSH/GPG signing
4. **Branch protection** - Configurar GitHub Rulesets o Azure DevOps policies
5. **Git hooks** - Pre-commit, pre-push, commit-msg automation
6. **CI/CD pipelines** - GitHub Actions, Azure DevOps Pipelines
7. **Azure DevOps branching** - Branch policies, YAML pipelines, merge queues
8. **Feature flags** - Microsoft.FeatureManagement, Azure App Configuration
9. **Branching antipatrones** - Long-lived branches, cherry-pick inverso, ramas zombie
10. **DORA metrics** - Conexion entre estrategia de branching y metricas de rendimiento
11. **Monorepo** - Estrategias para repositorios monoliticos
12. **Migracion Git** - De TFVC a Git, de GitFlow a Trunk-Based
13. **Performance** - Sparse checkout, partial clone, mantenimiento
14. **Preparacion Git 3.0** - SHA-256, reftable, cambios de compatibilidad

---

## Activacion del Skill

### Por Contexto (Automatico)

Claude activa este skill automaticamente cuando detecta:
- Usuario pregunta sobre "estrategia de ramas" o "branching model"
- Se solicita configurar "Conventional Commits" o "mensajes de commit"
- Se menciona "firmar commits" o "SSH signing" o "GPG signing"
- Se pregunta por "branch protection" o "GitHub Rulesets" o "branch policies"
- Se trabaja con `.gitignore`, `.gitattributes`, hooks o configuracion Git
- Se menciona "Git hooks", "pre-commit", "Husky" o "lint-staged"
- Se pide configurar "GitHub Actions" o "pipelines de CI/CD" para Git
- Se pregunta sobre "merge queue" o "merge queues"
- Se menciona "feature flags" o "feature toggles" en contexto de branching
- Se pregunta por "DORA metrics" o metricas de rendimiento de equipo
- Se pregunta por "antipatrones" o "antipatterns" de branching/ramas
- Se menciona "desarrollador unico", "solo developer" o "GitHub Flow simplificado"
- Se menciona "developer branch", "rama personal", "dev.{usuario}" o "rama personal remota"
- Se pregunta por "nomenclatura temporal", "nomenclatura configurable", "convencion de ramas" o "yyyyMMdd"
- Se menciona "pushFeatures", "tiposTarea" o "tipo de tarea en rama"
- Se pide configurar "Azure DevOps branch policies" o "YAML pipelines"
- Se pregunta sobre "monorepo" o "sparse checkout"
- Se solicita "migrar de TFVC" o "cambiar de GitFlow"
- Se pregunta sobre "Git rebase vs merge" o "estrategia de integracion"
- Se menciona "cherry-pick", "bisect", "reflog" o tecnicas avanzadas
- Se pregunta por "Git 3.0", "SHA-256" o "reftable"
- Se trabaja con `.github/workflows/` o archivos de CI/CD

---

## Principios Fundamentales

### Criterios de Decision

| Factor | Recomendacion Comillas |
|--------|----------------------|
| **Ramas** | GitHub Flow para equipos pequenos, Trunk-Based para CI/CD maduro |
| **Commits** | Conventional Commits obligatorio |
| **Firmado** | SSH signing (Git 2.34+) recomendado, GPG como alternativa |
| **Proteccion** | GitHub Rulesets (2025+) sobre Branch Protection Rules legacy |
| **Hooks** | Pre-commit: build + lint. Pre-push: tests |
| **CI/CD** | Reusable Workflows para organizacion, Composite Actions para tareas |
| **Monorepo** | Solo si hay dependencias fuertes entre proyectos |

### Flujo de Trabajo Recomendado

```
1. Crear rama desde main (feature/HV-XX-descripcion)
2. Commits atomicos con Conventional Commits
3. Push y abrir Pull Request
4. CI automatico: build + test + lint
5. Code review (minimo 1 aprobacion)
6. Squash merge a main (o merge commit segun politica)
7. Borrar rama feature
```

---

## Recursos del Skill

### strategies/ - Estrategias de Ramificacion

| Archivo | Descripcion |
|---------|-------------|
| `strategies/branching-models.md` | Comparativa de 13 estrategias con arbol de decision, nomenclatura configurable, DORA metrics, recomendaciones Comillas |
| `strategies/merge-strategies.md` | Rebase vs Merge vs Squash: cuando usar cada una |
| `strategies/release-management.md` | Versionado semantico, tags, release branches, changelogs |

### patterns/ - Patrones y Configuracion

| Archivo | Descripcion |
|---------|-------------|
| `patterns/conventional-commits.md` | Guia completa de Conventional Commits con ejemplos Comillas |
| `patterns/ssh-signing.md` | Configuracion de firmado SSH (Git 2.34+), verificacion local |
| `patterns/gitignore-patterns.md` | .gitignore optimizado por tipo de proyecto (.NET, Node, monorepo) |
| `patterns/gitattributes-config.md` | .gitattributes para LFS, line endings, diff drivers |
| `patterns/azure-devops-branching.md` | Azure DevOps: branch policies, YAML pipelines, merge queues, feature flags |
| `patterns/monorepo-patterns.md` | Estrategias monorepo: sparse checkout, CODEOWNERS, path filters |

### checklists/ - Checklists de Verificacion

| Archivo | Descripcion |
|---------|-------------|
| `checklists/repository-setup.md` | Setup inicial de repositorio: proteccion, hooks, CI, templates |
| `checklists/pre-merge.md` | Checklist pre-merge: tests, build, conflictos, commits limpios |
| `checklists/branching-antipatterns.md` | 9 antipatrones de branching con quick checklist y soluciones |
| `checklists/git3-readiness.md` | Preparacion para Git 3.0: SHA-256, Rust, default branch |

### tools/ - Herramientas y Automatizacion

| Archivo | Descripcion |
|---------|-------------|
| `tools/git-hooks-automation.md` | Pre-commit hooks: Husky, lint-staged, dotnet-format, scripts PS |
| `tools/github-actions-patterns.md` | Reusable Workflows, Composite Actions, matrices, secretos |
| `tools/context7-git.md` | Consulta de documentacion Git actualizada via Context7 |

### templates/ - Plantillas

| Archivo | Descripcion |
|---------|-------------|
| `templates/github-workflow.yml.template` | Workflow CI/CD para .NET 10 (build, test, deploy) |
| `templates/branch-ruleset.json.template` | GitHub Ruleset exportable con proteccion estandar Comillas |
| `templates/commit-msg-hook.sh.template` | Hook commit-msg para validar Conventional Commits |

---

## Metricas del Skill

| Metrica | Valor |
|---------|-------|
| Archivos totales | 20 |
| Subdirectorios | 5 |
| Git version referencia | 2.52+ (Nov 2025) |
| Estrategias documentadas | 13 (GitHub Flow, GitHub Flow simplificado, Developer Branch, Trunk-Based, Release Flow, OneFlow, Developer Flow, GitLab Flow, GitFlow, Ship/Show/Ask, Stacked PRs, Forking, Environment Branching) |
| Plataformas cubiertas | 3 (GitHub, Azure DevOps, GitLab) |
| DORA metrics | Conexion estrategia → rendimiento equipo |
| Antipatrones | 9 antipatrones con soluciones |
| Git 3.0 preparacion | SHA-256, Rust, default branch |

---

*Skill git-best-practices v3.7.0*
