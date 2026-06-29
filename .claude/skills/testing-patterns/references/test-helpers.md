# Helpers y Builders para Tests

> Patrones de Test Data Builder para crear datos de test de forma fluida.
> Incluye: builder pattern, uso con Bogus para datos aleatorios.

---

## Test Data Builder

```csharp
public class BecaBuilder
{
    private string _nombre = "Beca Test";
    private decimal _importe = 5000m;
    private EstadoBeca _estado = EstadoBeca.Borrador;

    public BecaBuilder ConNombre(string nombre)
    {
        _nombre = nombre;
        return this;
    }

    public BecaBuilder ConImporte(decimal importe)
    {
        _importe = importe;
        return this;
    }

    public BecaBuilder Publicada()
    {
        _estado = EstadoBeca.Publicada;
        return this;
    }

    public Beca Build()
    {
        var periodo = new Periodo(
            DateTime.Today.AddDays(1),
            DateTime.Today.AddMonths(6));
        var beca = Beca.Crear("BECA-" + Guid.NewGuid().ToString()[..8], _nombre, _importe, periodo);

        if (_estado == EstadoBeca.Publicada)
            beca.Publicar();

        return beca;
    }
}

// Uso
var beca = new BecaBuilder()
    .ConNombre("Beca Excelencia")
    .ConImporte(10000m)
    .Publicada()
    .Build();
```

---

> Para builders mas avanzados con Bogus, ver `patterns/builders.md`.
> Para factories centralizadas, ver `patterns/test-data-factory.md`.
