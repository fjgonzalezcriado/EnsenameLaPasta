# Patrón Test Data Factory

> Skill: testing-patterns
> Versión: 2.8.0

---

## Descripción

El patrón Test Data Factory centraliza la creación de datos de prueba en una clase estática,
proporcionando métodos para generar entidades, DTOs y requests con valores por defecto válidos.
Complementa al patrón Builder ofreciendo accesos directos para escenarios comunes.

---

## Factory Base

```csharp
public static class TestDataFactory
{
    // ═══════════════════════════════════════════════════════════════
    // BUILDERS - Acceso fluido a builders específicos
    // ═══════════════════════════════════════════════════════════════

    public static BecaBuilder Beca() => new();
    public static SolicitudBuilder Solicitud() => new();
    public static EstudianteBuilder Estudiante() => new();
    public static CreateBecaRequestBuilder CreateBecaRequest() => new();

    // ═══════════════════════════════════════════════════════════════
    // ESCENARIOS PREDEFINIDOS - Entidades listas para usar
    // ═══════════════════════════════════════════════════════════════

    public static Beca BecaBorradorValida() => new BecaBuilder()
        .ConCodigo("BECA-TEST-001")
        .ConNombre("Beca de Prueba")
        .ConImporte(5000m)
        .ConPeriodo(DateTime.Today.AddDays(1), DateTime.Today.AddMonths(6))
        .Build();

    public static Beca BecaPublicadaValida() => new BecaBuilder()
        .ConCodigo("BECA-PUB-001")
        .Publicada()
        .ConPeriodo(DateTime.Today, DateTime.Today.AddMonths(6))
        .Build();

    public static Beca BecaCerradaConSolicitudes(int numSolicitudes = 10) => new BecaBuilder()
        .ConCodigo("BECA-CERR-001")
        .Cerrada()
        .ConSolicitudes(numSolicitudes)
        .Build();

    public static Estudiante EstudianteActivo() => new EstudianteBuilder()
        .ConNombre("Estudiante Test")
        .ConEmail("test@comillas.edu")
        .Activo()
        .Build();
}
```

---

## Escenarios de Listas

```csharp
public static class TestDataFactory
{
    // Listas para tests de paginación y filtrado

    public static List<Beca> ListaBecasMixtas(int cantidad = 20)
    {
        var becas = new List<Beca>();
        var estados = new[] { EstadoBeca.Borrador, EstadoBeca.Publicada, EstadoBeca.Cerrada };

        for (int i = 0; i < cantidad; i++)
        {
            becas.Add(new BecaBuilder()
                .ConId(i + 1)
                .ConCodigo($"BECA-{i + 1:D3}")
                .ConNombre($"Beca Test {i + 1}")
                .ConImporte(1000m + (i * 500m))
                .ConEstado(estados[i % estados.Length])
                .Build());
        }

        return becas;
    }

    public static List<BecaDto> ListaBecaDtos(int cantidad = 10)
    {
        return Enumerable.Range(1, cantidad)
            .Select(i => new BecaDtoBuilder()
                .ConId(i)
                .ConNombre($"Beca DTO {i}")
                .ConImporte(i * 1000m)
                .Build())
            .ToList();
    }
}
```

---

## Datos Inválidos para Tests Negativos

```csharp
public static class TestDataFactory
{
    // Requests inválidos para validar errores

    public static CreateBecaCommand RequestSinNombre() => new CreateBecaRequestBuilder()
        .SinNombre()
        .Build();

    public static CreateBecaCommand RequestConImporteNegativo() => new CreateBecaRequestBuilder()
        .ConImporteInvalido()
        .Build();

    public static CreateBecaCommand RequestConFechasInvalidas() => new CreateBecaRequestBuilder()
        .ConFechasInvalidas()
        .Build();

    // Colección de casos inválidos para Theory
    public static IEnumerable<object[]> RequestsInvalidos()
    {
        yield return new object[] { RequestSinNombre(), "nombre" };
        yield return new object[] { RequestConImporteNegativo(), "importe" };
        yield return new object[] { RequestConFechasInvalidas(), "fecha" };
    }
}
```

---

## Uso en Tests

```csharp
public class BecaServiceTests
{
    [Fact]
    public async Task GetById_BecaExiste_RetornaBecaDto()
    {
        // Arrange - Escenario predefinido
        var beca = TestDataFactory.BecaPublicadaValida();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(beca.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(beca);

        // Act
        var result = await _sut.GetByIdAsync(beca.Id);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_RetornaPaginado()
    {
        // Arrange - Lista predefinida
        var becas = TestDataFactory.ListaBecasMixtas(50);

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(becas);

        // Act
        var result = await _sut.GetPaginatedAsync(1, 10);

        // Assert
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(50);
    }

    [Theory]
    [MemberData(nameof(TestDataFactory.RequestsInvalidos), MemberType = typeof(TestDataFactory))]
    public async Task Create_DatosInvalidos_LanzaValidationException(
        CreateBecaCommand request, string campoEsperado)
    {
        // Act
        var act = () => _sut.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.ContainsKey(campoEsperado));
    }
}
```

---

## Factory vs Builder

| Aspecto | TestDataFactory | Builder |
|---------|-----------------|---------|
| **Uso** | Escenarios comunes predefinidos | Configuración granular |
| **Sintaxis** | `TestDataFactory.BecaPublicadaValida()` | `new BecaBuilder().Publicada().Build()` |
| **Personalización** | Baja (escenarios fijos) | Alta (método por propiedad) |
| **Mejor para** | Tests simples, datos por defecto | Tests que requieren variaciones |

Se recomienda usar ambos: Factory para escenarios comunes y Builder cuando se necesita
personalizar propiedades específicas del objeto de test.

---

*Pattern v1.0 - testing-patterns skill*
