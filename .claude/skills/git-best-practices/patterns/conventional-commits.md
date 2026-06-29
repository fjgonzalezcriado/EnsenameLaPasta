# Conventional Commits - Guia Comillas

> Skill: git-best-practices | Version: 3.5.0

Estandar de mensajes de commit obligatorio para todos los proyectos Comillas.

---

## Formato

```
<tipo>(<ambito>): <descripcion>

[cuerpo opcional]

[notas al pie opcionales]
```

---

## Tipos

| Tipo | Descripcion | Ejemplo | Changelog |
|------|-------------|---------|-----------|
| `feat` | Nueva funcionalidad | `feat(becas): añadir filtro por estado` | Si |
| `fix` | Correccion de bug | `fix(auth): corregir expiracion de token` | Si |
| `docs` | Solo documentacion | `docs(readme): actualizar instrucciones` | No |
| `style` | Formato (no cambia codigo) | `style: aplicar formato dotnet-format` | No |
| `refactor` | Reestructuracion sin cambio funcional | `refactor(repo): extraer metodo comun` | No |
| `perf` | Mejora de rendimiento | `perf(query): optimizar consulta paginada` | Si |
| `test` | Tests | `test(becas): añadir tests de servicio` | No |
| `chore` | Mantenimiento | `chore(deps): actualizar EF Core a 10.0.1` | No |
| `ci` | CI/CD | `ci: añadir workflow de build .NET 10` | No |
| `build` | Sistema de build | `build: migrar a Central Package Management` | No |
| `revert` | Reversion | `revert: feat(becas): filtro por estado` | Si |

---

## Ambito (Scope)

El ambito indica el modulo o area afectada:

```bash
# Modulos funcionales
feat(becas): ...
fix(solicitudes): ...
feat(matriculas): ...

# Capas de arquitectura
refactor(domain): ...
fix(infrastructure): ...
test(application): ...

# Areas transversales
chore(deps): ...
ci(pipeline): ...
docs(api): ...
```

---

## Breaking Changes

```bash
# Opcion 1: Con "!" despues del tipo
feat(api)!: cambiar formato de respuesta paginada

# Opcion 2: Con BREAKING CHANGE en el footer
feat(api): cambiar formato de respuesta paginada

BREAKING CHANGE: PaginatedResult ahora usa TotalPages en lugar de TotalCount.
Los clientes deben actualizar sus modelos de respuesta.
```

---

## Ejemplos Comillas

### Commit simple

```
feat(becas): añadir filtro de becas por estado
```

### Commit con cuerpo

```
fix(solicitudes): corregir validacion de fecha limite

La fecha limite se comparaba sin tener en cuenta la zona horaria,
lo que causaba rechazos incorrectos en solicitudes del ultimo dia.

Closes HV-23
```

### Commit con breaking change

```
feat(api)!: migrar respuestas paginadas a nuevo formato

BREAKING CHANGE: El campo "totalElements" se renombra a "totalItems"
y "pageSize" a "itemsPerPage" en todas las respuestas paginadas.

Refs: HV-30
```

### Commit con multiples referencias

```
fix(auth): corregir refresh de token Azure AD

El middleware de autenticacion no renovaba el token cuando expiraba
durante una peticion larga (>5min).

Fixes: HV-25
See also: HV-22, HV-24
Co-authored-by: Juan Garcia <jgarcia@comillas.edu>
```

---

## Validacion Automatica

### Git Hook commit-msg

Ver `templates/commit-msg-hook.sh.template` para hook que valida el formato.

### Regex de Validacion

```regex
^(feat|fix|docs|style|refactor|perf|test|chore|ci|build|revert)(\(.+\))?(!)?: .{1,100}$
```

### Errores Comunes

| Error | Correcto |
|-------|----------|
| `Fixed bug` | `fix(becas): corregir calculo de importe` |
| `feat: Added new feature` | `feat(becas): añadir filtro por estado` |
| `WIP` | No hacer commit de trabajo en progreso |
| `update` | `chore(deps): actualizar paquetes NuGet` |
| `misc changes` | Dividir en commits especificos |

---

## Integracion con Jira/Azure DevOps

```bash
# Referenciar ticket en el footer
feat(becas): implementar exportacion Excel

Refs: STIC-123
# o
Closes: HV-18

# Para Azure DevOps Work Items
feat(becas): implementar exportacion Excel

AB#1234
```

---

*Pattern v3.7.0*
