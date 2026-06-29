---
globs:
  - "**/Domain/**/*.cs"
  - "**/Entities/**/*.cs"
  - "**/ValueObjects/**/*.cs"
  - "**/Aggregates/**/*.cs"
  - "**/Events/**/*.cs"
  - "**/*Entity.cs"
  - "**/*Aggregate.cs"
---

# Reglas para Capa de Dominio

> Este archivo aplica cuando Claude trabaja con entidades, value objects y lógica de dominio.
> El dominio debe ser **agnóstico a la infraestructura**.

---

## PRINCIPIOS FUNDAMENTALES

| Principio | Descripción |
|-----------|-------------|
| **Agnóstico** | Sin dependencias de EF, HTTP, bases de datos |
| **Rico** | Lógica de negocio en entidades, no en servicios |
| **Inmutable** | Value Objects inmutables |
| **Validado** | Entidades siempre en estado válido |

---

## PARTE 1: ENTIDAD BASE

### Entity Base - .NET 10 (C# 13)

```csharp
public abstract class Entity : IEquatable<Entity>
{
    public int Id { get; protected set; }

    // Eventos de dominio
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    // Igualdad por ID
    public override bool Equals(object? obj)
    {
        return obj is Entity entity && Equals(entity);
    }

    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        if (Id == default || other.Id == default) return false;
        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity? left, Entity? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity? left, Entity? right)
    {
        return !Equals(left, right);
    }
}

// Entidad auditable
public abstract class AuditableEntity : Entity, IAuditable
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

// Soft delete
public interface ISoftDelete
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    string? DeletedBy { get; }
}
```

---

## PARTE 2: ENTIDAD DE DOMINIO

### Ejemplo: Beca

```csharp
public class Beca : AuditableEntity, ISoftDelete
{
    // ═══════════════════════════════════════════════════════════════
    // PROPIEDADES - Solo getters públicos, setters privados
    // ═══════════════════════════════════════════════════════════════
    
    public string Codigo { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public string? Descripcion { get; private set; }
    public decimal Importe { get; private set; }
    public Periodo Periodo { get; private set; } = null!;  // Value Object
    public EstadoBeca Estado { get; private set; }
    
    // Soft delete
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }

    // Navegación (solo lectura)
    private readonly List<Solicitud> _solicitudes = [];
    public IReadOnlyCollection<Solicitud> Solicitudes => _solicitudes.AsReadOnly();

    // ═══════════════════════════════════════════════════════════════
    // CONSTRUCTOR - Privado para forzar uso de factory
    // ═══════════════════════════════════════════════════════════════
    
    private Beca() { } // Para EF

    private Beca(string codigo, string nombre, decimal importe, Periodo periodo)
    {
        Codigo = codigo;
        Nombre = nombre;
        Importe = importe;
        Periodo = periodo;
        Estado = EstadoBeca.Borrador;
    }

    // ═══════════════════════════════════════════════════════════════
    // FACTORY - Punto único de creación con validación
    // ═══════════════════════════════════════════════════════════════
    
    public static Beca Crear(string codigo, string nombre, decimal importe, Periodo periodo)
    {
        // Validaciones de dominio
        if (string.IsNullOrWhiteSpace(codigo))
            throw new DomainException("El código es obligatorio");
        
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre es obligatorio");
        
        if (nombre.Length > 200)
            throw new DomainException("El nombre no puede exceder 200 caracteres");
        
        if (importe <= 0)
            throw new DomainException("El importe debe ser positivo");
        
        if (importe > 100_000)
            throw new DomainException("El importe máximo es 100.000€");

        var beca = new Beca(codigo.ToUpperInvariant(), nombre.Trim(), importe, periodo);
        
        // Evento de dominio
        beca.AddDomainEvent(new BecaCreadaEvent(beca.Id, beca.Codigo, beca.Nombre));
        
        return beca;
    }

    // ═══════════════════════════════════════════════════════════════
    // COMPORTAMIENTOS - Métodos con lógica de negocio
    // ═══════════════════════════════════════════════════════════════
    
    public void Publicar()
    {
        if (Estado != EstadoBeca.Borrador)
            throw new DomainException("Solo se pueden publicar becas en borrador");
        
        if (!Periodo.EsFuturo())
            throw new DomainException("No se puede publicar una beca con periodo pasado");

        Estado = EstadoBeca.Publicada;
        AddDomainEvent(new BecaPublicadaEvent(Id, Codigo));
    }

    public void Cerrar()
    {
        if (Estado != EstadoBeca.Publicada)
            throw new DomainException("Solo se pueden cerrar becas publicadas");

        Estado = EstadoBeca.Cerrada;
        AddDomainEvent(new BecaCerradaEvent(Id));
    }

    public void ActualizarImporte(decimal nuevoImporte)
    {
        if (Estado != EstadoBeca.Borrador)
            throw new DomainException("Solo se puede modificar el importe en borrador");
        
        if (nuevoImporte <= 0)
            throw new DomainException("El importe debe ser positivo");

        var importeAnterior = Importe;
        Importe = nuevoImporte;
        
        AddDomainEvent(new ImporteBecaModificadoEvent(Id, importeAnterior, nuevoImporte));
    }

    public void AgregarSolicitud(Solicitud solicitud)
    {
        if (Estado != EstadoBeca.Publicada)
            throw new DomainException("Solo se aceptan solicitudes en becas publicadas");
        
        if (!Periodo.Contiene(DateTime.Today))
            throw new DomainException("Fuera del periodo de solicitud");
        
        if (_solicitudes.Any(s => s.EstudianteId == solicitud.EstudianteId))
            throw new DomainException("El estudiante ya tiene una solicitud para esta beca");

        _solicitudes.Add(solicitud);
    }

    public void Eliminar(string usuario)
    {
        if (Estado == EstadoBeca.Publicada && _solicitudes.Any())
            throw new DomainException("No se puede eliminar una beca con solicitudes");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = usuario;
    }
}

// Enum de dominio
public enum EstadoBeca
{
    Borrador = 0,
    Publicada = 1,
    Cerrada = 2,
    Cancelada = 3
}
```

---

## PARTE 3: VALUE OBJECTS

### Value Object Base

```csharp
public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        return Equals((ValueObject)obj);
    }

    public bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }
}
```

### Ejemplo: Periodo

```csharp
public class Periodo : ValueObject
{
    public DateTime FechaInicio { get; }
    public DateTime FechaFin { get; }

    // Duración calculada
    public int DiasTotal => (FechaFin - FechaInicio).Days;

    private Periodo() { } // Para EF

    public Periodo(DateTime fechaInicio, DateTime fechaFin)
    {
        if (fechaFin <= fechaInicio)
            throw new DomainException("La fecha fin debe ser posterior a la fecha inicio");

        FechaInicio = fechaInicio.Date;
        FechaFin = fechaFin.Date;
    }

    public bool Contiene(DateTime fecha) 
        => fecha.Date >= FechaInicio && fecha.Date <= FechaFin;

    public bool EsFuturo() 
        => FechaInicio > DateTime.Today;

    public bool EstaActivo() 
        => Contiene(DateTime.Today);

    public bool SeSolapa(Periodo otro)
        => FechaInicio <= otro.FechaFin && otro.FechaInicio <= FechaFin;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FechaInicio;
        yield return FechaFin;
    }

    public override string ToString() 
        => $"{FechaInicio:dd/MM/yyyy} - {FechaFin:dd/MM/yyyy}";
}
```

### Ejemplo: Dinero (Money Pattern)

```csharp
public class Dinero : ValueObject
{
    public decimal Cantidad { get; }
    public string Moneda { get; }

    private Dinero() { }

    public Dinero(decimal cantidad, string moneda = "EUR")
    {
        if (cantidad < 0)
            throw new DomainException("La cantidad no puede ser negativa");
        
        Cantidad = Math.Round(cantidad, 2);
        Moneda = moneda.ToUpperInvariant();
    }

    public static Dinero Euros(decimal cantidad) => new(cantidad, "EUR");
    public static Dinero Zero => new(0, "EUR");

    public Dinero Sumar(Dinero otro)
    {
        if (Moneda != otro.Moneda)
            throw new DomainException("No se pueden sumar monedas diferentes");
        
        return new Dinero(Cantidad + otro.Cantidad, Moneda);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Cantidad;
        yield return Moneda;
    }

    public override string ToString() => $"{Cantidad:N2} {Moneda}";
}
```

---

## PARTE 4: EVENTOS DE DOMINIO

```csharp
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// Eventos específicos
public record BecaCreadaEvent(int BecaId, string Codigo, string Nombre) : DomainEvent;
public record BecaPublicadaEvent(int BecaId, string Codigo) : DomainEvent;
public record BecaCerradaEvent(int BecaId) : DomainEvent;
public record ImporteBecaModificadoEvent(int BecaId, decimal ImporteAnterior, decimal ImporteNuevo) : DomainEvent;
```

---

## PARTE 5: EXCEPCIONES DE DOMINIO

```csharp
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object id) 
        : base($"{entityName} con ID {id} no encontrado") { }
}

public class BusinessRuleException : DomainException
{
    public string RuleCode { get; }
    
    public BusinessRuleException(string ruleCode, string message) : base(message)
    {
        RuleCode = ruleCode;
    }
}
```

---

## PARTE 6: INTERFACES DE REPOSITORIO

```csharp
// En Domain - Solo contratos
public interface IBecaRepository
{
    Task<Beca?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Beca?> GetByCodigoAsync(string codigo, CancellationToken ct = default);
    Task<IReadOnlyList<Beca>> GetActivasAsync(CancellationToken ct = default);
    Task<Beca> AddAsync(Beca beca, CancellationToken ct = default);
    Task UpdateAsync(Beca beca, CancellationToken ct = default);
    Task<bool> ExisteCodigoAsync(string codigo, CancellationToken ct = default);
}

// Patrón Specification (opcional)
public interface ISpecification<T>
{
    Expression<Func<T, bool>> ToExpression();
}

public class BecasActivasSpec : ISpecification<Beca>
{
    public Expression<Func<Beca, bool>> ToExpression()
        => b => b.Estado == EstadoBeca.Publicada && !b.IsDeleted;
}
```

---

## CHECKLIST

### Entidades
- [ ] Constructor privado + factory Crear()
- [ ] Propiedades con setter privado
- [ ] Validaciones en factory/métodos
- [ ] Eventos de dominio para cambios importantes
- [ ] Sin dependencias de infraestructura

### Value Objects
- [ ] Inmutables (sin setters)
- [ ] Validación en constructor
- [ ] Igualdad por valor
- [ ] Operaciones que retornan nuevo VO

### General
- [ ] Excepciones de dominio específicas
- [ ] Interfaces de repositorio (solo contratos)
- [ ] Sin referencias a EF, HTTP, JSON

---

*Regla condicional v3.7.0 - Domain-Driven Design*
