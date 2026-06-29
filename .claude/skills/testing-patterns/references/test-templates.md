# Plantillas de Tests

> Plantillas completas de tests para diferentes capas y patrones.
> Incluye: test unitario de service, test de entidad, test de value object, test de integración.

---

## Test Unitario - Service

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
        var beca = new Beca { Id = becaId, Nombre = "Test", Importe = 5000m };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(becaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(beca);

        // Act
        var result = await _sut.GetByIdAsync(becaId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(becaId);
        result.Nombre.Should().Be("Test");

        _repositoryMock.Verify(
            r => r.GetByIdAsync(becaId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetById_BecaNoExiste_RetornaNull()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Beca?)null);

        // Act
        var result = await _sut.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
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

## Test de Entidad (Domain)

```csharp
public class BecaTests
{
    [Fact]
    public void Crear_DatosValidos_CreaBecaEnBorrador()
    {
        // Arrange
        var periodo = new Periodo(
            DateTime.Today.AddDays(1),
            DateTime.Today.AddMonths(6));

        // Act
        var beca = Beca.Crear("BECA-001", "Test", 5000m, periodo);

        // Assert
        beca.Should().NotBeNull();
        beca.Estado.Should().Be(EstadoBeca.Borrador);
        beca.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BecaCreadaEvent>();
    }

    [Fact]
    public void Publicar_EstadoBorrador_CambiaAPublicada()
    {
        // Arrange
        var beca = CrearBecaValida();

        // Act
        beca.Publicar();

        // Assert
        beca.Estado.Should().Be(EstadoBeca.Publicada);
    }

    [Fact]
    public void Publicar_YaPublicada_LanzaExcepcion()
    {
        // Arrange
        var beca = CrearBecaValida();
        beca.Publicar();

        // Act
        var act = () => beca.Publicar();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*borrador*");
    }

    private static Beca CrearBecaValida()
    {
        var periodo = new Periodo(
            DateTime.Today.AddDays(1),
            DateTime.Today.AddMonths(6));
        return Beca.Crear("BECA-001", "Test", 5000m, periodo);
    }
}
```

---

## Test de Value Object

```csharp
public class PeriodoTests
{
    [Fact]
    public void Constructor_FechasValidas_CreaPeriodo()
    {
        // Arrange
        var inicio = DateTime.Today;
        var fin = DateTime.Today.AddMonths(6);

        // Act
        var periodo = new Periodo(inicio, fin);

        // Assert
        periodo.FechaInicio.Should().Be(inicio);
        periodo.FechaFin.Should().Be(fin);
    }

    [Fact]
    public void Constructor_FechaFinAnterior_LanzaExcepcion()
    {
        // Arrange
        var inicio = DateTime.Today;
        var fin = DateTime.Today.AddDays(-1);

        // Act
        var act = () => new Periodo(inicio, fin);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*posterior*");
    }

    [Theory]
    [InlineData(0, true)]   // Hoy, dentro
    [InlineData(90, true)]  // Dentro
    [InlineData(-1, false)] // Antes
    [InlineData(200, false)] // Después
    public void Contiene_VariasFechas_RetornaEsperado(int diasDesdeInicio, bool esperado)
    {
        // Arrange
        var inicio = DateTime.Today;
        var fin = DateTime.Today.AddMonths(6);
        var periodo = new Periodo(inicio, fin);
        var fecha = inicio.AddDays(diasDesdeInicio);

        // Act
        var result = periodo.Contiene(fecha);

        // Assert
        result.Should().Be(esperado);
    }

    [Fact]
    public void Equals_MismoValor_SonIguales()
    {
        // Arrange
        var inicio = DateTime.Today;
        var fin = DateTime.Today.AddMonths(6);
        var periodo1 = new Periodo(inicio, fin);
        var periodo2 = new Periodo(inicio, fin);

        // Act & Assert
        periodo1.Should().Be(periodo2);
        (periodo1 == periodo2).Should().BeTrue();
    }
}
```

---

## Test de Integración

```csharp
public class WebApplicationFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private MsSqlContainer _sqlContainer = null!;

    public async Task InitializeAsync()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await _sqlContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Reemplazar DbContext
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

    public BecasControllerTests(WebApplicationFixture factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/becas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new { Nombre = "Test", Importe = 5000m };

        // Act
        var response = await _client.PostAsJsonAsync("/api/becas", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new { Nombre = "", Importe = -100m };

        // Act
        var response = await _client.PostAsJsonAsync("/api/becas", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```
