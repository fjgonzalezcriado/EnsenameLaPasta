---
name: testing-patterns
description: >
  Generates unit and integration tests following Comillas conventions:
  xUnit, FluentAssertions, Moq, Testcontainers, and bUnit for Blazor.
  Includes test data builders, snapshot testing, and performance test patterns.
  USE FOR: writing unit tests, integration tests, xUnit patterns, Testcontainers,
  WebApplicationFactory, test naming, test coverage, crear tests,
  generar tests unitarios, tests integracion.
  DO NOT USE FOR: code review (use code-reviewer agent),
  security auditing (use security-audit),
  API contract testing (use postman-collection).
---

# Testing Patterns - Comillas

Este skill genera tests siguiendo estándares STIC. Incluye 8 patrones, 4 templates base y un arbol de decision para elegir el patron correcto.

---

## Cuando Usar Este Skill

1. **Tests unitarios** - Generar tests para servicios, entidades, validadores
2. **Tests de integracion** - Crear tests con WebApplicationFactory + Testcontainers
3. **Tests Blazor** - Componentes con bUnit (render, eventos, auth, JS interop)
4. **Configuracion** - Montar proyecto de tests desde cero
5. **Cobertura** - Analizar y mejorar cobertura de codigo
6. **Snapshot testing** - Verificar que salidas no cambian (Verify.NET)
7. **Contract testing** - Verificar contratos API (status, shape, backward compat)

---

## Arbol de Decision: ¿Que Patron Usar?

```
¿Que estas testeando?
│
├── Entidad de Dominio (Entity/Aggregate)
│   → Usar: Plantilla "Test de Entidad" en references/test-templates.md
│   → Pattern: patterns/builders.md para datos complejos
│
├── Value Object
│   → Usar: Plantilla "Test de Value Object" en references/test-templates.md
│   → Probar: constructor, igualdad, validaciones
│
├── Service / Handler (Application Layer)
│   → Usar: Plantilla "Test Unitario - Service" en references/test-templates.md
│   → Pattern: patterns/mocking-avanzado.md para Moq avanzado
│   → Template: templates/TestBase.cs.template (base class con AutoMocker)
│
├── Controller / Endpoint (API)
│   → Usar: patterns/api-contract-testing.md para contratos
│   → Template: templates/ApiTestBase.cs.template (WebApplicationFactory)
│
├── Repositorio / Infrastructure
│   → Usar: patterns/integration-fixtures.md (Testcontainers)
│   → Template: templates/IntegrationTestBase.cs.template
│
├── Componente Blazor (.razor)
│   → Usar: patterns/bunit-patterns.md
│   → Probar: render, eventos, forms, auth, JS interop
│
├── Datos de test complejos
│   ├── Granular (1 entidad) → patterns/builders.md (fluent builder)
│   └── Escenarios (multiples) → patterns/test-data-factory.md (factory)
│   → Template: templates/FakeBuilder.cs.template (Bogus)
│
├── Verificar que salida no cambia
│   → Usar: patterns/snapshot-testing.md (Verify.NET)
│   → Ideal para: respuestas JSON, mapeos DTO, configuraciones
│
└── Verificar rendimiento
    → Usar: patterns/performance-testing.md (BenchmarkDotNet + NBomber)
```

---

## Indice de Archivos del Skill

| Archivo | Proposito | Lineas |
|---------|-----------|--------|
| **SKILL.md** | Entrada principal, arbol de decision, convenciones | Este archivo |
| **references/test-templates.md** | Plantillas completas de tests (service, entidad, VO, integracion) | ~300 |
| **references/test-helpers.md** | Test Data Builder pattern y helpers | ~55 |
| **patterns/builders.md** | Builder pattern fluent para datos de test | ~380 |
| **patterns/mocking-avanzado.md** | Moq: setup, callbacks, secuencias, verificaciones | ~340 |
| **patterns/integration-fixtures.md** | WebApplicationFactory + Testcontainers + auth | ~430 |
| **patterns/snapshot-testing.md** | Verify.NET: snapshots, scrubbers, CI config | ~200 |
| **patterns/api-contract-testing.md** | Contract tests, Pact.NET, OpenAPI validation | ~250 |
| **patterns/performance-testing.md** | BenchmarkDotNet + NBomber load testing | ~45 |
| **patterns/test-data-factory.md** | Factory pattern centralizado para datos | ~210 |
| **patterns/bunit-patterns.md** | bUnit: render, eventos, forms, auth, JS interop | ~300 |
| **templates/TestBase.cs.template** | Base class unitaria con AutoMocker | ~40 |
| **templates/IntegrationTestBase.cs.template** | Base class integracion con Testcontainers | ~56 |
| **templates/ApiTestBase.cs.template** | Base class API con WebApplicationFactory | ~39 |
| **templates/FakeBuilder.cs.template** | Builder base con Bogus | ~35 |

---

## Activacion del Skill

### Por Comando

| Comando | Accion |
|---------|--------|
| `/test` | Ejecutar tests existentes + reportar |
| `/test --generate [clase]` | Generar tests para codigo existente |
| `/test --coverage` | Analisis de gaps de cobertura |

### Por Contexto (Automatico)

Claude activa este skill automaticamente cuando detecta:
- Usuario menciona "test", "pruebas", "cobertura", "bUnit"
- Se pide "crear tests para este servicio"
- Se menciona "xUnit", "FluentAssertions", "Moq", "Testcontainers"
- Se trabaja en carpeta `tests/` o archivos `*Tests.cs`
- Se pide "snapshot testing" o "contract testing"

---

## Stack de Testing

| Paquete | Version | Uso | Pattern |
|---------|---------|-----|---------|
| **xUnit** | 2.x | Framework de testing | Todos |
| **FluentAssertions** | 6.x | Aserciones legibles | Todos |
| **Moq** | 4.x | Mocking | mocking-avanzado.md |
| **Testcontainers** | 3.x | Tests de integracion | integration-fixtures.md |
| **Bogus** | 34.x | Generacion de datos | builders.md, FakeBuilder.cs |
| **bUnit** | 1.x | Tests de Blazor | bunit-patterns.md |
| **Verify.Xunit** | 26.x | Snapshot testing | snapshot-testing.md |
| **PactNet** | 5.x | Consumer-driven contracts | api-contract-testing.md |
| **BenchmarkDotNet** | 0.14.x | Benchmarks | performance-testing.md |
| **NBomber** | 6.x | Load testing | performance-testing.md |

---

## Convenciones de Nomenclatura

### Nombre de Tests

```
Método_Escenario_ResultadoEsperado

Ejemplos:
- GetById_BecaExiste_RetornaBecaDto
- GetById_BecaNoExiste_RetornaNull
- Create_DatosValidos_GuardaYRetorna
- Create_NombreVacio_LanzaValidationException
- Delete_ConSolicitudes_LanzaBusinessException
```

### Estructura de Carpetas

```
tests/
├── Comillas.MiApp.Domain.Tests/
│   ├── Entities/
│   │   └── BecaTests.cs
│   └── ValueObjects/
│       └── PeriodoTests.cs
├── Comillas.MiApp.Application.Tests/
│   ├── Services/
│   │   └── BecaServiceTests.cs
│   └── Validators/
│       └── CreateBecaRequestValidatorTests.cs
└── Comillas.MiApp.Integration.Tests/
    ├── Api/
    │   └── BecasControllerTests.cs
    └── Fixtures/
        └── WebApplicationFixture.cs
```

---

## Plantillas de Tests

> Plantillas completas de codigo en `references/test-templates.md`
> Incluye: test unitario de service, test de entidad, test de value object, test de integracion con WebApplicationFactory + Testcontainers.

> Helpers y builders en `references/test-helpers.md`
> Incluye: Test Data Builder pattern con fluent API.

---

## Cobertura Mínima

| Capa | Objetivo |
|------|----------|
| Domain | 90% |
| Application | 80% |
| Infrastructure | 60% |
| Presentation | 40% |

---

## Comandos

```bash
# Ejecutar todos los tests
dotnet test

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"

# Generar reporte
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage
```

---

*Skill testing-patterns v3.7.0*
