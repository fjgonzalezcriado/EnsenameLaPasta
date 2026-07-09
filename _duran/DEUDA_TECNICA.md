# Deuda Tecnica - Enseñame la Pasta

> Registro de issues conocidos, hacks temporales y refactors pendientes.
> Actualizar tras descubrir/crear deuda. Marcar resuelta cuando se cierre.

---

## Estado actual (2026-05-26)

**No hay deuda tecnica todavia** — el proyecto no tiene codigo aun. Esta seccion se rellenara conforme se descubran issues durante el desarrollo.

---

## Categorias previstas

A medida que avance el desarrollo, registrar deuda en estas categorias:

| Codigo | Categoria | Ejemplo |
|---|---|---|
| `DT-ARQ-XXX` | Arquitectura | Acoplamiento entre capas, violacion de Clean Arch |
| `DT-PERF-XXX` | Rendimiento | Queries N+1, calculos repetidos, sin caching |
| `DT-SEC-XXX` | Seguridad | Validaciones faltantes, inputs sin sanear |
| `DT-TEST-XXX` | Testing | Cobertura baja, tests fragiles, sin tests integracion |
| `DT-DOC-XXX` | Documentacion | XML docs faltantes, README desactualizado |
| `DT-REF-XXX` | Refactor | Codigo duplicado, metodos largos, naming inconsistente |
| `DT-DEP-XXX` | Dependencias | Paquetes desactualizados, CVEs, deps no usadas |

---

## Plantilla para registrar nueva deuda

```markdown
### DT-XXX-001: Titulo breve

- **Categoria**: [ARQ/PERF/SEC/TEST/DOC/REF/DEP]
- **Prioridad**: [alta/media/baja]
- **Fecha deteccion**: YYYY-MM-DD
- **Detectado por**: stic.claude3
- **Estado**: [pendiente/en_progreso/resuelta/descartada]

**Descripcion**:
Que esta mal y por que es deuda.

**Impacto**:
Que pasa si no se arregla.

**Solucion propuesta**:
Como resolverlo.

**Archivos afectados**:
- `path/al/archivo.cs:linea`

**Referencias**:
- Commit/PR donde se introdujo (si aplica)
- Lecciones relacionadas en `_duran/LECCIONES.md`
```

---

## Problemas conocidos (no son deuda tecnica, son restricciones aceptadas)

Estos puntos NO se consideran deuda — son decisiones conscientes del MVP que se documentan para evitar que un futuro Claude los marque como "deuda a resolver":

1. **Sin autenticacion**: app personal, no expuesta. No requiere login.
2. **Sin Docker**: ejecucion solo con `dotnet run`. Docker se evalua si el proyecto crece.
3. **Sin pipeline CI/CD**: validacion local con `dotnet test`. CI se evalua si se publica el repo.
4. **SQLite, no SQL Server**: requisito del proyecto. Migraciones EF Core son portables si en el futuro se cambia.
5. **Datos de mercado aleatorios**: MVP intencional. APIs reales en roadmap Fase 2.
6. **Una sola estrategia (MA Crossover)**: deliberado. Mas estrategias en Fase 3.

---

**Ultima actualizacion**: 2026-05-26
**Actualizado por**: stic.claude3 (via /onboarding)
