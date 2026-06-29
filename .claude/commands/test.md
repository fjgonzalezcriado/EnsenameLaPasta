Ejecuta tests con cobertura, genera reportes y analiza gaps de testing

# /test - Ejecutar y Generar Tests

## Extended Thinking Mode

**think hard** - Analiza casos edge y genera tests exhaustivos cuando se soliciten.

---

## 1. Contexto del Proyecto

### 1.1 Detectar VCS y Solucion

```
DETECTAR tipo de VCS:
  - Buscar carpeta .git/ → Git
  - Buscar archivos .vspscc/.vssscc → TFVC
  - Ninguno → Sin VCS

DETECTAR multi-solucion:
  - Leer _duran/ESTADO_PROYECTO.json → soluciones.multiSolucion
  - Si multiSolucion == true:
    → Mostrar selector de solucion
    → Usar solucionActiva como default
  - Si multiSolucion == false:
    → Auto-detectar .sln/.slnx en 03_Desarrollo/
```

### 1.2 Ubicacion de Tests

Location_note

```
MiProyecto/
├── _duran/                         ← Metricas de tests
├── 03_Desarrollo/                  ← CODIGO Y TESTS
│   ├── MiSolucion.sln|.slnx       ← .slnx solo .NET 8+
│   ├── Comillas.MiApp.Domain/
│   ├── Comillas.MiApp.Application/
│   ├── Comillas.MiApp.Tests/           ← Tests unitarios
│   ├── Comillas.MiApp.Integration.Tests/ ← Tests integracion
│   └── Comillas.MiApp.Blazor.Tests/    ← Tests bUnit (si Blazor)
└── 04_Pruebas/                     ← Documentacion de pruebas
    ├── planes/
    ├── resultados/
    └── reportes/
```

---

## 2. Modos de Ejecucion

Modes_title

### 2.1 Modo Ejecutar (default: `--run`)

Ejecutar la suite de tests existente y mostrar resultados con cobertura.

### 2.2 Modo Generar (`--generate`)

Generar tests nuevos para codigo existente que no tiene tests.

### 2.3 Modo Analizar Cobertura (`--coverage`)

Analizar gaps de cobertura sin ejecutar tests, identificando clases/metodos sin tests.

### 2.4 Modo Filtrado (`--filter`)

Ejecutar tests filtrados por clase, metodo, categoria o proyecto.

```
Ejemplos de uso:
  /test                             → Ejecutar todos los tests
  /test --generate BecaService      → Generar tests para BecaService
  /test --coverage                  → Analizar gaps de cobertura
  /test --filter "ClassName"        → Filtrar tests por clase
  /test --project Tests.Unit        → Solo proyecto especifico
  /test --category integration      → Solo tests de integracion
```

---

## 3. MODO EJECUTAR: Flujo de 10 pasos

### Paso 1: Detectar Framework de Tests

```powershell
# Buscar proyectos de test en 03_Desarrollo/
$testProjects = Get-ChildItem -Path "03_Desarrollo" -Recurse -Filter "*.csproj" |
    Where-Object { $_.Name -match "Test|Tests|Spec" }

# Detectar framework en cada .csproj:
# - xUnit       → <PackageReference Include="xunit" />
# - NUnit       → <PackageReference Include="NUnit" />
# - MSTest      → <PackageReference Include="MSTest.TestFramework" />
# - bUnit       → <PackageReference Include="bunit" />
# - Testcontainers → <PackageReference Include="Testcontainers" />
```

### Paso 2: Mostrar Tests Disponibles

```
TEST DETECTADOS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Projects_header

Proyecto                              Framework    Tests
────────────────────────────────────────────────────────
03_Desarrollo/Comillas.MiApp.Tests    xUnit+Moq    ~[N]
03_Desarrollo/Comillas.MiApp.IT       xUnit+TC     ~[N]
03_Desarrollo/Comillas.MiApp.Blazor   bUnit        ~[N]

Total: [N] proyectos de test, ~[N] tests

Ejecutar: [T]odos / [U]nitarios / [I]ntegracion / [S]eleccionar
```

### Paso 3: Verificar Prerequisitos

Antes de ejecutar, verificar:
- Build compila sin errores
- Docker running si hay Testcontainers (tests de integracion)
- Connection strings disponibles si hay tests de BD

```powershell
# Verificar build
Push-Location "03_Desarrollo"
$buildResult = dotnet build --no-restore --verbosity quiet 2>&1
if ($LASTEXITCODE -ne 0) {
    # Mostrar errores de build y detener
}
Pop-Location
```

### Paso 4: Ejecutar Tests

```powershell
# Buscar solucion
$sln = Get-ChildItem -Path "03_Desarrollo" -Filter "*.slnx" | Select-Object -First 1
if (-not $sln) { $sln = Get-ChildItem -Path "03_Desarrollo" -Filter "*.sln" | Select-Object -First 1 }

# Ejecutar con cobertura
dotnet test $sln.FullName `
    --collect:"XPlat Code Coverage" `
    --results-directory "04_Pruebas/resultados" `
    --logger "console;verbosity=normal" `
    --settings "03_Desarrollo/runsettings.xml" 2>$null

# Con filtro (si se especifico)
dotnet test $sln.FullName `
    --filter "FullyQualifiedName~BecaServiceTests" `
    --collect:"XPlat Code Coverage"

# Solo proyecto especifico
dotnet test "03_Desarrollo/Comillas.MiApp.Tests/Comillas.MiApp.Tests.csproj" `
    --collect:"XPlat Code Coverage"
```

### Paso 5: Mostrar Resultados

```
RESULTADOS DE TESTS
━━━━━━━━━━━━━━━━━━━━━

  Pasaron:  [N]
  Fallaron: [N]
  Saltados: [N]

  Tiempo: [X]s

  Cobertura Global: [X]%

  Cobertura por Capa:
  ┌──────────────────────┬───────────┬──────────┬──────────┐
  │ Capa                 │ Cobertura │ Objetivo │ Estado   │
  ├──────────────────────┼───────────┼──────────┼──────────┤
  │ Domain               │ [X]%      │ 90%      │ [OK/WARN]│
  │ Application          │ [X]%      │ 80%      │ [OK/WARN]│
  │ Infrastructure       │ [X]%      │ 60%      │ [OK/WARN]│
  │ Presentation/Web     │ [X]%      │ 40%      │ [OK/WARN]│
  └──────────────────────┴───────────┴──────────┴──────────┘

  Codigo testeado: 03_Desarrollo/
  Resultados en:   04_Pruebas/resultados/
```

### Paso 6: Detalle de Tests Fallidos

```
TESTS FALLIDOS
━━━━━━━━━━━━━━━━

1. BecaServiceTests.Create_NombreVacio_LanzaValidationException
   Mensaje: Expected exception of type ValidationException but no exception was thrown
   Ubicacion: 03_Desarrollo/Comillas.MiApp.Tests/Services/BecaServiceTests.cs:45
   Sugerencia: Verificar que el servicio valida el nombre antes de crear

2. PeriodoTests.Constructor_FechaFinAnterior_LanzaExcepcion
   Mensaje: Assert.Throws() failed - no exception thrown
   Ubicacion: 03_Desarrollo/Comillas.MiApp.Tests/ValueObjects/PeriodoTests.cs:28
   Sugerencia: Verificar validacion en constructor de Periodo

Ver detalle: (numero) | Corregir: (c) | Continuar: (Enter)
```

### Paso 7: Analisis de Cobertura Gaps

```
GAPS DE COBERTURA DETECTADOS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Clases sin tests (ordenadas por criticidad):
  [ALTA]  BecaService.cs         → 12 metodos publicos, 0 tests
  [ALTA]  EstudianteService.cs   → 8 metodos publicos, 0 tests
  [MEDIA] NotificacionService.cs → 5 metodos publicos, 2 tests (40%)
  [BAJA]  MappingProfile.cs      → 3 metodos, 0 tests

Metodos sin cobertura en clases testeadas:
  BecaController.Update()       → Sin test de 404
  BecaController.Delete()       → Sin test de autorizacion
  Periodo.SeSolapa()            → Sin test

Generar tests para gaps? (s/n/seleccionar)
```

### Paso 8: Actualizar Estado

Actualizar `_duran/ESTADO_PROYECTO.json`:

```json
{
  "metricas": {
    "tests": {
      "total": "[N]",
      "pasaron": "[N]",
      "fallaron": "[N]",
      "saltados": "[N]",
      "cobertura_global": "[X]",
      "cobertura_por_capa": {
        "domain": "[X]",
        "application": "[X]",
        "infrastructure": "[X]",
        "presentation": "[X]"
      },
      "frameworks": ["xUnit", "bUnit"],
      "proyectos_test": "[N]",
      "ultimaEjecucion": "[FECHA_HORA]",
      "rutaTests": "03_Desarrollo",
      "rutaResultados": "04_Pruebas/resultados"
    }
  }
}
```

### Paso 9: Generar Reporte en 04_Pruebas/

```markdown
# Reporte de Tests - [FECHA]

## Resumen Ejecutivo
- **Total**: [N] tests en [N] proyectos
- **Resultado**: [PASARON] / [FALLARON] / [SALTADOS]
- **Cobertura Global**: [X]%
- **Tiempo Total**: [X]s

## Cobertura por Proyecto
| Proyecto | Tests | Pasaron | Fallaron | Cobertura |
|----------|-------|---------|----------|-----------|
| Comillas.MiApp.Tests | [N] | [N] | [N] | [X]% |
| Comillas.MiApp.IntegrationTests | [N] | [N] | [N] | [X]% |

## Cobertura por Capa
| Capa | Lineas | Cubiertos | Cobertura | Objetivo | Estado |
|------|--------|-----------|-----------|----------|--------|
| Domain | [N] | [N] | [X]% | 90% | [OK/WARN] |
| Application | [N] | [N] | [X]% | 80% | [OK/WARN] |
| Infrastructure | [N] | [N] | [X]% | 60% | [OK/WARN] |

## Tests Fallidos
[Detalle si los hay]

## Gaps de Cobertura
[Lista de clases/metodos sin tests]

## Recomendaciones
[Sugerencias basadas en gaps detectados]

---
*Generado por /test - STIC.IA v3.7.0*
```

Guardar en `04_Pruebas/resultados/reporte_tests_[FECHA].md`

### Paso 10: Resumen Final

```
RESUMEN DE TESTS
━━━━━━━━━━━━━━━━━━

Estado:    [ OK | FALLANDO]
Total:     [N] tests
Cobertura: [X]%
Tiempo:    [X]s
Reporte:   04_Pruebas/resultados/reporte_tests_[FECHA].md

Next_steps
  - /commit           → Si todo OK, hacer commit
  - /test --generate  → Generar tests para gaps
  - /revision         → Code review antes de commit

  testing-patterns skill:
  - patterns/builders.md           → Test data builders
  - patterns/mocking-avanzado.md   → Mocking avanzado con Moq
  - patterns/integration-fixtures.md → WebApplicationFactory + Testcontainers
```

---

## 4. MODO GENERAR: Flujo

Cuando se ejecuta `/test --generate [archivo|clase]`:

### 4.1 Analizar Codigo Fuente

```
Leer el archivo/clase objetivo en 03_Desarrollo/
Identificar:
  - Metodos publicos
  - Dependencias inyectadas (constructor)
  - Validaciones existentes
  - Excepciones posibles
  - Value Objects y entidades
  - Flujos happy path y edge cases
```

### 4.2 Seleccionar Patron de Test

Usar el **arbol de decision del skill testing-patterns**:

```
¿Que estas testeando?
├── Entidad de Dominio    → Patron: Domain entity test (SKILL.md)
├── Value Object          → Patron: Value object test (SKILL.md)
├── Service/Handler       → Patron: Service test + mocking-avanzado.md
├── Controller/Endpoint   → Patron: API test + ApiTestBase.cs.template
├── Repositorio           → Patron: Integration test + integration-fixtures.md
├── Componente Blazor     → Patron: bUnit test (bunit-patterns.md)
└── Datos complejos       → Patron: Builder/Factory (builders.md / test-data-factory.md)
```

### 4.3 Generar Tests con Convenciones

Aplicar SIEMPRE las convenciones de `tests.md`:
- Nomenclatura: `Metodo_Escenario_ResultadoEsperado`
- Patron AAA: Arrange, Act, Assert (comentar cada seccion)
- System Under Test: variable `_sut`
- Mocking: Moq con `Mock<IInterface>`
- Assertions: FluentAssertions (`Should()`)
- Async: `CancellationToken` en todos los metodos async

### 4.4 Template Base

```csharp
// 03_Desarrollo/Comillas.MiApp.Tests/[Carpeta]/[Clase]Tests.cs
using Xunit;
using Moq;
using FluentAssertions;

namespace Comillas.MiApp.Tests.[Carpeta];

public class [Clase]Tests
{
    // Mocks de dependencias
    private readonly Mock<I[Dependencia1]> _[dep1]Mock;
    private readonly Mock<ILogger<[Clase]>> _loggerMock;
    private readonly [Clase] _sut; // System Under Test

    public [Clase]Tests()
    {
        _[dep1]Mock = new Mock<I[Dependencia1]>();
        _loggerMock = new Mock<ILogger<[Clase]>>();
        _sut = new [Clase](_[dep1]Mock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task [Metodo]_[EscenarioPositivo]_[ResultadoEsperado]()
    {
        // Arrange
        // ... setup mocks y datos

        // Act
        var result = await _sut.[Metodo]Async(...);

        // Assert
        result.Should().NotBeNull();
        _[dep1]Mock.Verify(x => x.[Metodo](...), Times.Once);
    }

    [Fact]
    public async Task [Metodo]_[EscenarioNegativo]_[LanzaExcepcion]()
    {
        // Arrange
        // ... setup para fallo

        // Act
        var act = () => _sut.[Metodo]Async(...);

        // Assert
        await act.Should().ThrowAsync<[TipoExcepcion]>()
            .WithMessage("*[texto esperado]*");
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("valor valido", true)]
    public async Task [Metodo]_[VariosEscenarios]_[ResultadoVariable](
        string input, bool expectedValid)
    {
        // Arrange + Act + Assert parametrizado
    }
}
```

---

## 5. Umbrales de Cobertura Configurables

Leer umbrales de `_duran/ESTADO_PROYECTO.json`:

```json
{
  "metricas": {
    "umbrales_cobertura": {
      "domain": 90,
      "application": 80,
      "infrastructure": 60,
      "presentation": 40,
      "global_minimo": 70
    }
  }
}
```

Si no estan configurados, usar los defaults de `CLAUDE_BASE_COMILLAS.md`:
- Domain: 90%, Application: 80%, Infrastructure: 60%, Presentation: 40%

---

## 6. Integracion con Skill y Regla

### Tabla de Referencia Cruzada

| Necesitas... | Recurso | Ubicacion |
|---|---|---|
| Ejecutar tests existentes | **Este comando** (`/test`) | `.claude/commands/test.md` |
| Convenciones y nomenclatura | **Regla tests.md** | `.claude/rules/tests.md` |
| Patron de test unitario | **SKILL.md** seccion "Plantillas" | `.claude/skills/testing-patterns/SKILL.md` |
| Builder pattern (datos) | **builders.md** | `.claude/skills/testing-patterns/patterns/builders.md` |
| Mocking avanzado (Moq) | **mocking-avanzado.md** | `.claude/skills/testing-patterns/patterns/mocking-avanzado.md` |
| Tests de integracion | **integration-fixtures.md** | `.claude/skills/testing-patterns/patterns/integration-fixtures.md` |
| Snapshot testing (Verify) | **snapshot-testing.md** | `.claude/skills/testing-patterns/patterns/snapshot-testing.md` |
| Contract testing (API) | **api-contract-testing.md** | `.claude/skills/testing-patterns/patterns/api-contract-testing.md` |
| Performance testing | **performance-testing.md** | `.claude/skills/testing-patterns/patterns/performance-testing.md` |
| Factory pattern (datos) | **test-data-factory.md** | `.claude/skills/testing-patterns/patterns/test-data-factory.md` |
| Tests Blazor (bUnit) | **bunit-patterns.md** | `.claude/skills/testing-patterns/patterns/bunit-patterns.md` |
| Base class unitaria | **TestBase.cs.template** | `.claude/skills/testing-patterns/templates/TestBase.cs.template` |
| Base class integracion | **IntegrationTestBase.cs.template** | `.claude/skills/testing-patterns/templates/IntegrationTestBase.cs.template` |
| Base class API | **ApiTestBase.cs.template** | `.claude/skills/testing-patterns/templates/ApiTestBase.cs.template` |
| Builder base con Bogus | **FakeBuilder.cs.template** | `.claude/skills/testing-patterns/templates/FakeBuilder.cs.template` |

---

## 7. Diagrama de Flujo

```
┌──────────────────────────────────────────────────────────────┐
│                       /test                                   │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  PASO 1: Detectar VCS + multi-solucion                      │
│     │                                                        │
│  PASO 2: Detectar framework (xUnit/NUnit/MSTest/bUnit)      │
│     │                                                        │
│  PASO 3: Verificar prerequisitos (build, Docker)            │
│     │                                                        │
│  ┌──┴───────────────────────────────────────────────┐       │
│  │  ¿Modo?                                          │       │
│  ├──────────────────────────────────────────────────┤       │
│  │  --run (default)  │  --generate  │  --coverage   │       │
│  │       │           │      │       │      │        │       │
│  │  PASO 4: dotnet   │ Analizar    │ Escanear     │       │
│  │  test + coverage  │ codigo      │ clases sin   │       │
│  │       │           │ fuente      │ tests         │       │
│  │  PASO 5: Show     │      │      │      │        │       │
│  │  resultados       │ Seleccionar │ Mostrar      │       │
│  │       │           │ patron      │ gaps          │       │
│  │  PASO 6: Detalle  │      │      │      │        │       │
│  │  fallidos         │ Generar     │ Sugerir      │       │
│  │       │           │ tests       │ prioridades   │       │
│  └──┬───────────────────────────────────────────────┘       │
│     │                                                        │
│  PASO 7: Analisis de gaps                                   │
│     │                                                        │
│  PASO 8: Actualizar _duran/ESTADO_PROYECTO.json             │
│     │                                                        │
│  PASO 9: Generar reporte en 04_Pruebas/resultados/          │
│     │                                                        │
│  PASO 10: Resumen + proximos pasos + skill refs             │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## 8. Tabla Resumen del Comando

| Aspecto | Valor |
|---|---|
| **Comando** | `/test` |
| **Version** | v3.7.0 |
| **Modos** | `--run` (default), `--generate`, `--coverage`, `--filter` |
| **Parametros** | `--project`, `--category`, `--filter`, `--no-report` |
| **VCS** | Git + TFVC |
| **Multi-solucion** | Si (selector interactivo) |
| **Framework** | xUnit, NUnit, MSTest, bUnit |
| **Cobertura** | XPlat Code Coverage + umbrales configurables |
| **Reportes** | `04_Pruebas/resultados/reporte_tests_[FECHA].md` |
| **Actualiza** | `_duran/ESTADO_PROYECTO.json` → metricas.tests |
| **Skill relacionado** | `testing-patterns` (13 archivos, 7 patrones, 4 templates) |
| **Regla relacionada** | `tests.md` (convenciones AAA, nomenclatura, cobertura) |

---

## 9. Recordatorios Criticos

### NUNCA
- Ejecutar tests sin verificar que el build compila
- Ignorar tests fallidos sin analizar la causa
- Generar tests que dependan de orden de ejecucion
- Hardcodear datos sensibles en tests (connection strings, passwords)
- Usar `Thread.Sleep` en tests (usar `Task.Delay` o `Polly`)

### SIEMPRE
- Verificar build antes de ejecutar tests
- Usar patron AAA con comentarios en cada seccion
- Generar tests con nomenclatura `Metodo_Escenario_Resultado`
- Incluir tests de happy path Y edge cases
- Usar `CancellationToken` en metodos async
- Actualizar metricas en `_duran/ESTADO_PROYECTO.json`
- Consultar **testing-patterns** skill para patrones avanzados

---

*Comando /test v3.7.0 - STIC.IA*
