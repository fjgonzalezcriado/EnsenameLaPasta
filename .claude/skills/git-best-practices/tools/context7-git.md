# Integracion con Context7 para Git

> Skill: git-best-practices | Version: 3.5.0

Como usar el servidor MCP Context7 para consultar documentacion Git actualizada.

> Ver tambien: `tools/github-actions-patterns.md`, `patterns/ssh-signing.md`

---

## Proposito

El servidor `context7` se usa para consultar documentacion actualizada sobre:

- Sintaxis y opciones de comandos Git recientes (2.48+)
- Documentacion de GitHub Actions y workflows
- Configuracion de GitHub Rulesets y branch policies
- Documentacion de Azure DevOps Git
- Librerias de Git hooks (Husky, lint-staged, commitlint)

---

## Cuando Usar Context7

| Situacion | Accion |
|-----------|--------|
| Comando Git con opcion desconocida | Consultar docs de Git |
| GitHub Actions syntax | Consultar docs de GitHub Actions |
| Husky/lint-staged config | Consultar docs del paquete |
| Azure DevOps pipelines | Consultar docs de Azure Pipelines |

---

## Flujo de Trabajo

```
1. Usuario pregunta sobre configuracion Git/CI
   |
2. Resolver libreria en context7
   resolve-library-id("git") o
   resolve-library-id("github actions") o
   resolve-library-id("husky")
   |
3. Consultar documentacion especifica
   get-library-docs(id, topic="branching")
   get-library-docs(id, topic="reusable workflows")
   |
4. Aplicar respuesta con informacion verificada
```

---

## Ejemplos de Consulta

```
# Configuracion de Git hooks
resolve-library-id("husky")
get-library-docs(id, topic="hooks configuration")

# GitHub Actions reusable workflows
resolve-library-id("github actions")
get-library-docs(id, topic="reusable workflows")

# Git sparse checkout
resolve-library-id("git")
get-library-docs(id, topic="sparse checkout")
```

---

## Reglas Operativas

- Consultar Context7 cuando la informacion pueda haber cambiado desde la ultima version conocida
- Preferir documentacion de Context7 sobre conocimiento interno para versiones especificas
- Si Context7 no tiene la libreria, usar el conocimiento interno del skill
- No inventar opciones de comandos: verificar contra documentacion

---

*Pattern v3.7.0*
