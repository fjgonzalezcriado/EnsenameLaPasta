# Patrón Builder para Tests

> Skill: testing-patterns
> Versión: 2.8.0

---

## Descripción

El patrón Builder permite construir objetos de test de forma fluida y expresiva,
evitando constructores con muchos parámetros y haciendo los tests más legibles.

---

## Builder Genérico

```csharp
public abstract class Builder<T, TBuilder> where TBuilder : Builder<T, TBuilder>
{
    protected abstract T Build();

    public static implicit operator T(Builder<T, TBuilder> builder) => builder.Build();
}
```

---

## Builder para Entidad Beca

```csharp
public class BecaBuilder : Builder<Beca, BecaBuilder>
{
    private int _id = 1;
    private string _codigo = "BECA-001";
    private string _nombre = "Beca de Prueba";
    private string? _descripcion;
    private decimal _importe = 5000m;
    private EstadoBeca _estado = EstadoBeca.Borrador;
    private DateTime _fechaInicio = DateTime.Today;
    private DateTime _fechaFin = DateTime.Today.AddMonths(6);
    private List<Solicitud> _solicitudes = [];

    public BecaBuilder ConId(int id)
    {
        _id = id;
        return this;
    }

    public BecaBuilder ConCodigo(string codigo)
    {
        _codigo = codigo;
        return this;
    }

    public BecaBuilder ConNombre(string nombre)
    {
        _nombre = nombre;
        return this;
    }

    public BecaBuilder ConDescripcion(string descripcion)
    {
        _descripcion = descripcion;
        return this;
    }

    public BecaBuilder ConImporte(decimal importe)
    {
        _importe = importe;
        return this;
    }

    public BecaBuilder ConEstado(EstadoBeca estado)
    {
        _estado = estado;
        return this;
    }

    public BecaBuilder Publicada()
    {
        _estado = EstadoBeca.Publicada;
        return this;
    }

    public BecaBuilder Cerrada()
    {
        _estado = EstadoBeca.Cerrada;
        return this;
    }

    public BecaBuilder ConPeriodo(DateTime inicio, DateTime fin)
    {
        _fechaInicio = inicio;
        _fechaFin = fin;
        return this;
    }

    public BecaBuilder ConSolicitud(Solicitud solicitud)
    {
        _solicitudes.Add(solicitud);
        return this;
    }

    public BecaBuilder ConSolicitudes(int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            _solicitudes.Add(new SolicitudBuilder()
                .ConBecaId(_id)
                .ConEstudianteId(i + 1)
                .Build());
        }
        return this;
    }

    protected override Beca Build()
    {
        var beca = Beca.Crear(_codigo, _nombre, _importe,
            new Periodo(_fechaInicio, _fechaFin));

        // Usar reflection para setear Id y otros campos privados en tests
        SetPrivateProperty(beca, nameof(Beca.Id), _id);
        SetPrivateProperty(beca, nameof(Beca.Descripcion), _descripcion);
        SetPrivateProperty(beca, nameof(Beca.Estado), _estado);

        foreach (var solicitud in _solicitudes)
        {
            // Agregar a colección privada
            var field = typeof(Beca).GetField("_solicitudes",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var list = (List<Solicitud>)field!.GetValue(beca)!;
            list.Add(solicitud);
        }

        return beca;
    }

    private static void SetPrivateProperty<TValue>(object obj, string propertyName, TValue value)
    {
        var property = obj.GetType().GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.Instance);

        if (property?.CanWrite == true)
        {
            property.SetValue(obj, value);
        }
        else
        {
            // Intentar con backing field
            var field = obj.GetType().GetField($"<{propertyName}>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
    }
}
```

---

## Builder para DTOs

```csharp
public class BecaDtoBuilder
{
    private int _id = 1;
    private string _codigo = "BECA-001";
    private string _nombre = "Beca de Prueba";
    private decimal _importe = 5000m;
    private string _estado = "Borrador";
    private DateTime _fechaInicio = DateTime.Today;
    private DateTime _fechaFin = DateTime.Today.AddMonths(6);

    public BecaDtoBuilder ConId(int id)
    {
        _id = id;
        return this;
    }

    public BecaDtoBuilder ConNombre(string nombre)
    {
        _nombre = nombre;
        return this;
    }

    public BecaDtoBuilder ConImporte(decimal importe)
    {
        _importe = importe;
        return this;
    }

    public BecaDtoBuilder Publicada()
    {
        _estado = "Publicada";
        return this;
    }

    public BecaDto Build() => new()
    {
        Id = _id,
        Codigo = _codigo,
        Nombre = _nombre,
        Importe = _importe,
        Estado = _estado,
        FechaInicio = _fechaInicio,
        FechaFin = _fechaFin
    };

    public static implicit operator BecaDto(BecaDtoBuilder builder) => builder.Build();
}
```

---

## Builder para Requests

```csharp
public class CreateBecaRequestBuilder
{
    private string _codigo = "BECA-001";
    private string _nombre = "Nueva Beca";
    private string? _descripcion;
    private decimal _importe = 5000m;
    private DateTime _fechaInicio = DateTime.Today.AddDays(1);
    private DateTime _fechaFin = DateTime.Today.AddMonths(6);

    public CreateBecaRequestBuilder ConCodigo(string codigo)
    {
        _codigo = codigo;
        return this;
    }

    public CreateBecaRequestBuilder ConNombre(string nombre)
    {
        _nombre = nombre;
        return this;
    }

    public CreateBecaRequestBuilder SinNombre()
    {
        _nombre = string.Empty;
        return this;
    }

    public CreateBecaRequestBuilder ConImporte(decimal importe)
    {
        _importe = importe;
        return this;
    }

    public CreateBecaRequestBuilder ConImporteInvalido()
    {
        _importe = -1000m;
        return this;
    }

    public CreateBecaRequestBuilder ConFechasInvalidas()
    {
        _fechaInicio = DateTime.Today.AddMonths(6);
        _fechaFin = DateTime.Today; // Fin antes que inicio
        return this;
    }

    public CreateBecaCommand Build() => new()
    {
        Codigo = _codigo,
        Nombre = _nombre,
        Descripcion = _descripcion,
        Importe = _importe,
        FechaInicio = _fechaInicio,
        FechaFin = _fechaFin
    };
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
        // Arrange - Uso fluido del builder
        var beca = new BecaBuilder()
            .ConId(1)
            .ConNombre("Beca Excelencia")
            .ConImporte(10000m)
            .Publicada()
            .Build();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(beca);

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Beca Excelencia");
    }

    [Fact]
    public async Task Create_DatosValidos_CreaYRetornaBeca()
    {
        // Arrange
        var request = new CreateBecaRequestBuilder()
            .ConNombre("Nueva Beca")
            .ConImporte(5000m)
            .Build();

        // Act & Assert
        // ...
    }

    [Fact]
    public async Task Delete_BecaConSolicitudes_LanzaExcepcion()
    {
        // Arrange
        var beca = new BecaBuilder()
            .ConId(1)
            .Publicada()
            .ConSolicitudes(5) // 5 solicitudes generadas
            .Build();

        // Act & Assert
        // ...
    }
}
```

---

## Factoría de Builders

```csharp
public static class TestDataFactory
{
    public static BecaBuilder Beca() => new();
    public static SolicitudBuilder Solicitud() => new();
    public static EstudianteBuilder Estudiante() => new();
    public static CreateBecaRequestBuilder CreateBecaRequest() => new();

    // Builders predefinidos para escenarios comunes
    public static Beca BecaPublicadaValida() => new BecaBuilder()
        .Publicada()
        .ConPeriodo(DateTime.Today, DateTime.Today.AddMonths(6))
        .Build();

    public static Beca BecaCerradaConSolicitudes() => new BecaBuilder()
        .Cerrada()
        .ConSolicitudes(10)
        .Build();
}

// Uso
var beca = TestDataFactory.BecaPublicadaValida();
var request = TestDataFactory.CreateBecaRequest().ConNombre("Mi Beca").Build();
```

---

## Ventajas del Patrón Builder

| Ventaja | Descripción |
|---------|-------------|
| **Legibilidad** | Tests autodocumentados |
| **Mantenibilidad** | Cambios centralizados |
| **Flexibilidad** | Fácil crear variaciones |
| **Reutilización** | Builders compartidos |
| **Defaults sensatos** | Valores por defecto válidos |

---

*Pattern v1.0 - testing-patterns skill*
