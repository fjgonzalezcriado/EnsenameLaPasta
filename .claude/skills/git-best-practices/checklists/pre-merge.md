# Checklist: Pre-Merge

> Skill: git-best-practices | Version: 3.5.0

Verificaciones antes de aprobar y mergear un Pull Request.

---

## Verificaciones Automaticas (CI)

- [ ] Build compila sin errores
- [ ] Todos los tests pasan (unit + integration)
- [ ] Sin vulnerabilidades criticas (dotnet audit)
- [ ] Cobertura de tests >= umbral definido
- [ ] Lint/format sin warnings nuevos

---

## Verificaciones de Codigo (Reviewer)

### Funcionalidad
- [ ] El codigo hace lo que el PR describe
- [ ] Los edge cases estan manejados
- [ ] El error handling es apropiado
- [ ] No hay regresiones visibles

### Calidad
- [ ] Sigue los patrones del proyecto (ver CLAUDE_BASE_COMILLAS.md)
- [ ] Nombres descriptivos (variables, metodos, clases)
- [ ] Sin codigo duplicado evitable
- [ ] Complejidad razonable
- [ ] Sin TODOs sin ticket asociado

### Seguridad
- [ ] Sin secrets hardcodeados
- [ ] Inputs validados (FluentValidation)
- [ ] Queries parametrizadas (no concatenacion SQL)
- [ ] Autorizacion verificada en endpoints nuevos

### Tests
- [ ] Tests unitarios para logica nueva
- [ ] Tests de integracion si hay cambios de BD o APIs
- [ ] Test names siguen convencion `Metodo_Escenario_Resultado`

---

## Verificaciones de Git

- [ ] Commits siguen Conventional Commits
- [ ] Branch actualizada con main (sin conflictos)
- [ ] No hay merge commits innecesarios
- [ ] Commits firmados (si la politica lo requiere)
- [ ] PR titulo descriptivo y conciso

---

## Verificaciones Post-Merge

- [ ] Branch feature borrada
- [ ] Pipeline de deploy ejecuta correctamente (si aplica)
- [ ] No hay degradacion de rendimiento
- [ ] Evolutivo/ticket actualizado

---

*Checklist v3.7.0*
