---
globs:
  - "tests/**/*.cs"
  - "**/*.Tests/**/*.cs"
  - "**/*.Test/**/*.cs"
  - "**/Tests/**/*.cs"
  - "**/*Tests.cs"
  - "**/*Test.cs"
---

# Reglas para Pruebas Unitarias

> Este archivo aplica cuando Claude trabaja con tests.
> **Detecta automáticamente** el framework de testing del proyecto.

---

## DETECCIÓN DE FRAMEWORK

```xml
<PackageReference Include="xunit" Version="2.*" />              <!-- xUnit (recomendado) -->
<PackageReference Include="NUnit" Version="4.*" />              <!-- NUnit -->
<PackageReference Include="MSTest.TestFramework" Version="3.*" /> <!-- MSTest -->
```

---

## PARTE 1: NOMENCLATURA

### Convención de Nombres

```
Método_Escenario_ResultadoEsperado

Ejemplos:
- GetById_BecaExiste_RetornaBeca
- GetById_BecaNoExiste_RetornaNull
- Create_DatosValidos_GuardaEnBaseDatos
- Create_NombreVacio_LanzaValidationException
- Delete_BecaConSolicitudes_LanzaBusinessException
```

### Estructura de Carpetas

```
tests/
├── Comillas.MiApp.Domain.Tests/
│   ├── Entities/
│   │   ├── BecaTests.cs
│   │   └── SolicitudTests.cs
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

## PARTE 2: PATRÓN AAA (Arrange-Act-Assert)

### Ejemplo Completo - xUnit + FluentAssertions

```csharp
public class BecaServiceTests
{
    private readonly Mock<IBecaRepository> _repositoryMock;
    private readonly Mock<ILogger<BecaService>> _loggerMock;
    private readonly BecaService _sut; // System Under Test

    public BecaServiceTests()
    {
        _repositoryMock = new Mock<IBecaRepository>();
        _loggerMock = new Mock<ILogger<BecaService>>();
        _sut = new BecaService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetById_BecaExiste_RetornaBecaDto()
    {
        // Arrange
        var becaId = 1;
        var beca = new Beca 
        { 
            Id = becaId, 
            Nombre = "Beca Test", 
            Importe = 5000m 
        };
        
        _repositoryMock
            .Setup(r => r.GetByIdAsync(becaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(beca);

        // Act
        var result = await _sut.GetByIdAsync(becaId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(becaId);
        result.Nombre.Should().Be("Beca Test");
        result.Importe.Should().Be(5000m);
        
        _repositoryMock.Verify(
            r => r.GetByIdAsync(becaId, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetById_BecaNoExiste_RetornaNull()
    {
        // Arrange
        var becaId = 999;
        _repositoryMock
            .Setup(r => r.GetByIdAsync(becaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Beca?)null);

        // Act
        var result = await _sut.GetByIdAsync(becaId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Create_DatosValidos_GuardaYRetornaBeca()
    {
        // Arrange
        var request = new CreateBecaRequest
        {
            Nombre = "Nueva Beca",
            Importe = 3000m,
            FechaInicio = DateTime.Today.AddDays(1),
            FechaFin = DateTime.Today.AddMonths(6)
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Beca>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Beca b, CancellationToken _) => 
            {
                b.Id = 1;
                return b;
            });

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Nombre.Should().Be("Nueva Beca");
        
        _repositoryMock.Verify(
            r => r.AddAsync(
                It.Is<Beca>(b => b.Nombre == "Nueva Beca" && b.Importe == 3000m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Create_NombreInvalido_LanzaValidationException(string? nombre)
    {
        // Arrange
        var request = new CreateBecaRequest { Nombre = nombre!, Importe = 1000m };

        // Act
        var act = () => _sut.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*nombre*");
    }
}
```

---

## PARTE 3: MOCKING CON MOQ

### Setup Básico

```csharp
// Retornar valor
_mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
    .ReturnsAsync(new Beca { Id = 1 });

// Retornar según parámetro
_mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync((int id, CancellationToken _) => new Beca { Id = id });

// Lanzar excepción
_mockRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
    .ThrowsAsync(new NotFoundException("Beca no encontrada"));

// Callback para inspeccionar
_mockRepo.Setup(r => r.AddAsync(It.IsAny<Beca>(), It.IsAny<CancellationToken>()))
    .Callback<Beca, CancellationToken>((b, _) => becaGuardada = b)
    .ReturnsAsync((Beca b, CancellationToken _) => b);
```

### Verificaciones

```csharp
// Verificar que se llamó
_mockRepo.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);

// Verificar que NO se llamó
_mockRepo.Verify(r => r.DeleteAsync(It.IsAny<Beca>(), It.IsAny<CancellationToken>()), Times.Never);

// Verificar con condición
_mockRepo.Verify(
    r => r.AddAsync(
        It.Is<Beca>(b => b.Nombre == "Test" && b.Importe > 0),
        It.IsAny<CancellationToken>()),
    Times.Once);
```

---

## PARTE 4: FLUENT ASSERTIONS

### Aserciones Comunes

```csharp
// Valores
result.Should().Be(expected);
result.Should().NotBe(unexpected);
result.Should().BeNull();
result.Should().NotBeNull();

// Strings
nombre.Should().Be("Test");
nombre.Should().StartWith("Beca");
nombre.Should().Contain("excelencia");
nombre.Should().BeEmpty();
nombre.Should().HaveLength(10);

// Números
importe.Should().Be(5000m);
importe.Should().BeGreaterThan(0);
importe.Should().BeInRange(1000, 10000);
importe.Should().BeApproximately(5000m, 0.01m);

// Colecciones
becas.Should().NotBeEmpty();
becas.Should().HaveCount(5);
becas.Should().Contain(b => b.Nombre == "Test");
becas.Should().BeInAscendingOrder(b => b.Nombre);
becas.Should().OnlyContain(b => b.Estado == EstadoBeca.Activa);

// Excepciones
var act = () => _sut.Delete(999);
await act.Should().ThrowAsync<NotFoundException>();
await act.Should().ThrowAsync<ValidationException>()
    .WithMessage("*obligatorio*");

// Objetos
beca.Should().BeEquivalentTo(expected, options => 
    options.Excluding(b => b.Id)
           .Excluding(b => b.CreatedAt));

// Tiempo de ejecución
var act = () => _sut.ProcesarAsync();
await act.Should().CompleteWithinAsync(TimeSpan.FromSeconds(5));
```

---

## PARTE 5: TESTS DE INTEGRACIÓN - .NET 10

### WebApplicationFactory

```csharp
public class WebApplicationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private MsSqlContainer _sqlContainer = null!;

    public async Task InitializeAsync()
    {
        // Testcontainers - SQL Server en Docker
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
        
        await _sqlContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Reemplazar DbContext con conexión a contenedor
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(_sqlContainer.GetConnectionString()));
        });
    }

    public new async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }
}

[Collection("Integration")]
public class BecasControllerTests : IClassFixture<WebApplicationFixture>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFixture _factory;

    public BecasControllerTests(WebApplicationFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/becas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PaginatedResult<BecaDto>>();
        content.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateBecaRequest
        {
            Nombre = "Beca Test Integration",
            Importe = 5000m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/becas", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }
}
```

---

## PARTE 6: TESTS DE DOMINIO

### Testing Value Objects

```csharp
public class PeriodoTests
{
    [Fact]
    public void Constructor_FechasValidas_CreaPeriodo()
    {
        // Arrange
        var inicio = new DateTime(2025, 1, 1);
        var fin = new DateTime(2025, 12, 31);

        // Act
        var periodo = new Periodo(inicio, fin);

        // Assert
        periodo.FechaInicio.Should().Be(inicio);
        periodo.FechaFin.Should().Be(fin);
    }

    [Fact]
    public void Constructor_FechaFinAnteriorAInicio_LanzaExcepcion()
    {
        // Arrange
        var inicio = new DateTime(2025, 12, 31);
        var fin = new DateTime(2025, 1, 1);

        // Act
        var act = () => new Periodo(inicio, fin);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*fecha fin*anterior*");
    }

    [Theory]
    [InlineData("2025-06-15", true)]  // Dentro del periodo
    [InlineData("2024-12-31", false)] // Antes
    [InlineData("2026-01-01", false)] // Después
    public void ContieneEn_VariasFechas_RetornaEsperado(string fechaStr, bool esperado)
    {
        // Arrange
        var periodo = new Periodo(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 12, 31));
        var fecha = DateTime.Parse(fechaStr);

        // Act
        var result = periodo.Contiene(fecha);

        // Assert
        result.Should().Be(esperado);
    }
}
```

---

## PARTE 7: COBERTURA

### Cobertura Mínima Recomendada

| Capa | Cobertura mínima |
|------|------------------|
| Domain | 90% |
| Application | 80% |
| Infrastructure | 60% |
| Presentation | 40% |

### Ejecutar con Cobertura

```bash
# .NET 10 con coverlet
dotnet test --collect:"XPlat Code Coverage"

# Generar reporte HTML
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report
```

---

## CHECKLIST

### Cada test debe
- [ ] Nombre descriptivo (Método_Escenario_Resultado)
- [ ] Seguir patrón AAA
- [ ] Probar UN solo comportamiento
- [ ] Ser independiente de otros tests
- [ ] Ejecutar rápido (<1 segundo unitarios)

### Proyecto de tests debe
- [ ] xUnit + FluentAssertions + Moq
- [ ] Estructura espejo del proyecto principal
- [ ] Tests de integración separados
- [ ] Cobertura mínima por capa

---

*Regla condicional v3.7.0 - Multi-versión .NET*
