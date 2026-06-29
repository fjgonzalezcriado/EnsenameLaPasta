> Skill: analisis-arquitectura | Version: 3.7.0

# Checklist de Viabilidad de Migracion a Clean Architecture

Este checklist se usa cuando el proyecto **NO sigue Clean Architecture** y se quiere evaluar
si merece la pena migrar. Forma parte de la seccion 10 del analisis de arquitectura general.

---

## Indice de Viabilidad (1-5)

Evaluar cada factor con puntuacion 1-5 y peso ponderado:

| Factor | Peso | Que evaluar | Puntuacion baja (1-2) | Puntuacion alta (4-5) |
|--------|------|-------------|----------------------|----------------------|
| **Tamano del proyecto** | 15% | Archivos .cs, complejidad ciclomatica | <20 archivos, logica simple | >100 archivos, multiples modulos |
| **Acoplamiento entre capas** | 20% | Dependencias circulares, new() directo, DbContext en controllers | Bajo acoplamiento, interfaces existentes | Alto acoplamiento, todo mezclado |
| **Logica de dominio clara** | 20% | Reglas de negocio identificables, entidades con comportamiento | Solo CRUD, sin logica | Reglas complejas, validaciones de negocio |
| **Cobertura de tests** | 10% | Proyectos de test, frameworks, % cobertura | >60% cobertura | 0% cobertura, sin tests |
| **Deuda tecnica acumulada** | 15% | Clases >500 lineas, metodos >50 lineas, code smells | Codigo limpio y mantenible | God classes, copy-paste, magic strings |
| **Beneficio vs esfuerzo** | 20% | Vida util del proyecto, frecuencia de cambios, equipo | Proyecto legacy estable, sin cambios | Proyecto activo, cambios frecuentes |

### Interpretacion del indice

| Rango | Veredicto | Accion |
|-------|-----------|--------|
| **4.0 - 5.0** | Migracion altamente recomendada | Planificar migracion completa |
| **3.0 - 3.9** | Recomendada con reservas | Evaluar caso por caso, migracion parcial |
| **2.0 - 2.9** | Migracion parcial posible | Aplicar principios CA sin reescritura completa |
| **1.0 - 1.9** | No recomendada | Mejorar dentro de la arquitectura actual |

---

## Estrategias de Migracion

Segun el contexto del proyecto, recomendar una o combinacion:

### Strangler Fig
- **Cuando**: Proyecto grande en produccion
- **Como**: Envolver funcionalidad existente gradualmente con nueva capa CA
- **Riesgo**: Medio - coexistencia temporal de dos arquitecturas
- **Duracion**: 3-6 meses tipico

### Branch by Abstraction
- **Cuando**: Acoplamiento moderado, interfaces parciales
- **Como**: Introducir interfaces donde no existen, luego mover implementaciones a capas CA
- **Riesgo**: Bajo - cambios incrementales
- **Duracion**: 2-4 meses tipico

### Big Bang (parcial)
- **Cuando**: Proyecto pequeno o modulos independientes
- **Como**: Reescribir modulo por modulo en estructura CA
- **Riesgo**: Alto - requiere tests de regresion completos
- **Duracion**: 1-2 sprints por modulo

### Bubble Context
- **Cuando**: Funcionalidad nueva aislable
- **Como**: Crear nuevo modulo CA que coexista con legacy
- **Riesgo**: Bajo - no toca codigo existente
- **Duracion**: 1 sprint para setup + desarrollo normal

---

## Plan de Migracion por Fases

Plantilla estandar para el plan:

| Fase | Sprint(s) | Tareas | Riesgo | Criterio de exito |
|------|-----------|--------|--------|-------------------|
| **0 - Preparacion** | 1 | Tests de caracterizacion, documentar comportamiento actual | Bajo | Tests pasan, comportamiento documentado |
| **1 - Domain** | 1-2 | Extraer entidades, value objects, interfaces de repositorio | Bajo | Domain compila sin dependencias externas |
| **2 - Application** | 1-2 | Crear servicios de aplicacion, mover logica de controllers | Medio | Controllers delgados, logica en Application |
| **3 - Infrastructure** | 1 | Mover implementaciones de repositorio, servicios externos | Medio | Infrastructure implementa interfaces de Domain |
| **4 - Limpieza** | 1 | Eliminar proyectos obsoletos, ajustar DI, documentar | Bajo | Solucion limpia, CI verde |

---

## Riesgos de Migracion

Evaluar cada riesgo:

| Riesgo | Probabilidad (1-5) | Impacto (1-5) | Mitigacion |
|--------|--------------------|--------------|-----------|
| Regresiones en funcionalidad existente | | | Tests de caracterizacion antes de migrar |
| Interrupcion de consumidores de la API | | | Mantener contratos, versionado API |
| Aumento temporal de complejidad | | | Migracion por fases, no big-bang |
| Curva de aprendizaje del equipo | | | Formacion previa, pair programming |
| Tiempo de migracion mayor al estimado | | | Buffer del 30%, criterios de parada |

---

## Preguntas para Contextualizar

Antes de evaluar viabilidad, considerar:

- [ ] Vida util esperada del proyecto (>2 anios justifica migracion)
- [ ] Frecuencia de cambios (cambios semanales vs mantenimiento puntual)
- [ ] Tamano del equipo y experiencia con CA
- [ ] Existencia de tests (red de seguridad para refactoring)
- [ ] Consumidores de APIs existentes (riesgo de rotura)
- [ ] Restricciones de tiempo (deadline cercano = no migrar ahora)
- [ ] Presupuesto disponible para la migracion

---

*Checklist v3.7.0 - Evaluacion de viabilidad de migracion a Clean Architecture*
