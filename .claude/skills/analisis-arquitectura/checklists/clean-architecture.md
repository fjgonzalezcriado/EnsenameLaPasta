# Checklist Clean Architecture

> Skill: analisis-arquitectura
> Versión: 2.8.0

---

## Descripción

Checklist para verificar que un proyecto cumple con los principios
de Clean Architecture y las convenciones de desarrollo de Comillas.

---

## 1. Estructura de Proyectos

### Proyectos Requeridos

- [ ] **Domain** - Entidades, Value Objects, Interfaces de repositorio
- [ ] **Application** - Casos de uso, DTOs, Validadores
- [ ] **Infrastructure** - EF Core, Repositorios, Servicios externos
- [ ] **Presentation** (Web/API) - Controllers, Views, Program.cs

### Nomenclatura

- [ ] Proyectos siguen convención `Comillas.[Area].[Proyecto].[Capa]`
- [ ] Ejemplo: `Comillas.RRHH.GestionBecas.Domain`

---

## 2. Capa Domain

### Entidades

- [ ] Constructor privado + método factory `Crear()`
- [ ] Propiedades con `private set`
- [ ] Validaciones de dominio en factory/métodos
- [ ] Sin dependencias de EF Core, HTTP, JSON

```csharp
// ✅ Correcto
public class Beca
{
    public int Id { get; private set; }
    public string Nombre { get; private set; }

    private Beca() { }

    public static Beca Crear(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("Nombre requerido");
        return new Beca { Nombre = nombre };
    }
}
```

### Value Objects

- [ ] Inmutables (sin setters)
- [ ] Validación en constructor
- [ ] `Equals` y `GetHashCode` implementados
- [ ] Operaciones retornan nuevo objeto

### Interfaces

- [ ] Interfaces de repositorio definidas en Domain
- [ ] Interfaces de servicios externos definidas en Domain
- [ ] Sin implementación, solo contratos

### Eventos de Dominio

- [ ] Eventos son records inmutables
- [ ] Nomenclatura: `{Entidad}{Accion}Event`
- [ ] Ejemplo: `BecaCreadaEvent`, `BecaPublicadaEvent`

---

## 3. Capa Application

### Commands y Queries (CQRS)

- [ ] Commands para operaciones de escritura
- [ ] Queries para operaciones de lectura
- [ ] Un handler por command/query
- [ ] Commands/Queries son records

```csharp
// ✅ Correcto
public record CreateBecaCommand(string Nombre, decimal Importe) : IRequest<int>;
public record GetBecaByIdQuery(int Id) : IRequest<BecaDto?>;
```

### Validadores

- [ ] FluentValidation para validar inputs
- [ ] Un validador por Command
- [ ] Mensajes de error en español

### DTOs

- [ ] DTOs específicos por caso de uso
- [ ] No exponer entidades de dominio
- [ ] Records para inmutabilidad

### Dependencias

- [ ] Solo depende de Domain
- [ ] NO referencia Infrastructure
- [ ] NO referencia Presentation

---

## 4. Capa Infrastructure

### DbContext

- [ ] Configuraciones Fluent API en archivos separados
- [ ] `IEntityTypeConfiguration<T>` por entidad
- [ ] Query filters para soft delete
- [ ] Auditoría automática en SaveChanges

### Repositorios

- [ ] Implementan interfaces de Domain
- [ ] Métodos async con CancellationToken
- [ ] AsNoTracking para consultas de solo lectura

### Servicios Externos

- [ ] Implementan interfaces de Application
- [ ] Configuración vía IOptions
- [ ] Logging de operaciones
- [ ] Manejo de errores con reintentos

---

## 5. Capa Presentation

### Controllers

- [ ] Solo orquestación, sin lógica
- [ ] Inyecta IMediator o servicios de Application
- [ ] Atributos de documentación OpenAPI
- [ ] Manejo consistente de errores

### Program.cs

- [ ] Registro de servicios organizado
- [ ] Configuración desde appsettings
- [ ] Pipeline de middleware ordenado

---

## 6. Dependency Injection

### Flujo de Dependencias

```
Presentation → Application → Domain
      ↓
Infrastructure → Domain
```

- [ ] Presentation puede usar Application e Infrastructure
- [ ] Application solo usa Domain
- [ ] Infrastructure implementa interfaces de Domain/Application
- [ ] Domain no depende de nadie

### Registro de Servicios

- [ ] Extension methods por capa: `AddDomain()`, `AddApplication()`, `AddInfrastructure()`
- [ ] Scoped para servicios con estado por request
- [ ] Singleton para servicios stateless

---

## 7. Testing

### Estructura

- [ ] `Domain.Tests` - Tests de entidades y value objects
- [ ] `Application.Tests` - Tests de handlers
- [ ] `Infrastructure.Tests` - Tests de repositorios
- [ ] `Integration.Tests` - Tests end-to-end

### Cobertura

- [ ] Domain: 90%+
- [ ] Application: 80%+
- [ ] Infrastructure: 60%+

---

## 8. Convenciones de Código

### Async/Await

- [ ] Métodos async terminan en `Async`
- [ ] CancellationToken en todos los métodos async
- [ ] ConfigureAwait(false) en librerías

### Null Safety

- [ ] Nullable reference types habilitado
- [ ] `?` para tipos nullables
- [ ] Validación de nulls en boundaries

### Logging

- [ ] Structured logging con Serilog
- [ ] Niveles apropiados (Info, Warning, Error)
- [ ] No loguear datos sensibles

---

## 9. Seguridad

- [ ] Secretos en Azure Key Vault
- [ ] Connection strings sin credenciales en código
- [ ] Validación de inputs con FluentValidation
- [ ] Autorización basada en roles/políticas

---

## 10. Anti-Patterns a Evitar

| Anti-Pattern | Descripción | Correcto |
|--------------|-------------|----------|
| Anemic Domain | Entidades solo con getters/setters | Entidades con comportamiento |
| God Service | Servicio que hace todo | Servicios pequeños y focalizados |
| DbContext en Controller | Acceso directo a BD | Usar repositorios vía IMediator |
| Shared DTOs | Mismo DTO para crear y leer | DTOs específicos por operación |
| Infrastructure en Domain | Referencias a EF en Domain | Domain puro, sin dependencias |

---

## Resultado de la Revisión

| Sección | Cumple | Parcial | No Cumple |
|---------|--------|---------|-----------|
| Estructura de Proyectos | ☐ | ☐ | ☐ |
| Capa Domain | ☐ | ☐ | ☐ |
| Capa Application | ☐ | ☐ | ☐ |
| Capa Infrastructure | ☐ | ☐ | ☐ |
| Capa Presentation | ☐ | ☐ | ☐ |
| Dependency Injection | ☐ | ☐ | ☐ |
| Testing | ☐ | ☐ | ☐ |
| Convenciones | ☐ | ☐ | ☐ |
| Seguridad | ☐ | ☐ | ☐ |

---

**Fecha de revisión:** _______________
**Revisado por:** _______________
**Proyecto:** _______________

---

*Checklist v1.0 - analisis-arquitectura skill*
