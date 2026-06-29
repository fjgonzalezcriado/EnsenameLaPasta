---
globs:
  - "**/Application/**/*.cs"
  - "**/UseCases/**/*.cs"
  - "**/Features/**/*.cs"
  - "**/Commands/**/*.cs"
  - "**/Queries/**/*.cs"
  - "**/Handlers/**/*.cs"
  - "**/DTOs/**/*.cs"
  - "**/Validators/**/*.cs"
  - "**/Behaviours/**/*.cs"
  - "**/Mappings/**/*.cs"
  - "**/Services/**/*Service.cs"
  - "**/*Command.cs"
  - "**/*Query.cs"
  - "**/*Handler.cs"
  - "**/*Dto.cs"
  - "**/*DTO.cs"
  - "**/*Validator.cs"
  - "**/*Behaviour.cs"
  - "**/*Behavior.cs"
---

# Reglas para Capa de Aplicación

> Este archivo aplica cuando Claude trabaja con casos de uso, comandos, queries, DTOs y servicios de aplicación.
> La capa Application **orquesta** el dominio pero **no contiene lógica de negocio**.

---

## ⚠️ AVISO IMPORTANTE: MediatR → Mediator

**A partir de 2026**, recomendamos usar **[Mediator](https://github.com/martinothamar/Mediator)** (martinothamar) en lugar de MediatR:

| Aspecto | MediatR v12+ | Mediator |
|---------|--------------|----------|
| **Licencia** | Comercial ($500-2000/año) | **MIT (gratuito)** |
| **Rendimiento** | Reflexión runtime | **Source generators** (más rápido) |
| **API** | IRequest, IRequestHandler | IRequest, IRequestHandler (compatible) |
| **Pipeline Behaviors** | IPipelineBehavior | IPipelineBehavior (idéntico) |

Los ejemplos en este archivo usan la interfaz estándar (`IRequest`, `IRequestHandler`, `IPipelineBehavior`) que es **compatible con ambas librerías**.

> 📚 Ver `Documentos_Base/01_Estructura_Tecnica/GUIA_ARQUITECTURA.md` para guía de migración y detalles completos.

---

## PRINCIPIOS FUNDAMENTALES

| Principio | Descripción |
|-----------|-------------|
| **Orquestación** | Coordina el dominio, no implementa lógica de negocio |
| **CQRS** | Separar Commands (escritura) de Queries (lectura) |
| **Independencia** | Sin dependencias de Infrastructure o Presentation |
| **Validación** | Validar inputs antes de llegar al dominio |

---

## PARTE 1: ESTRUCTURA DE CARPETAS

### Organización por Features (Recomendada)

```
Application/
├── Common/
│   ├── Interfaces/
│   │   ├── IUnitOfWork.cs
│   │   ├── ICurrentUserService.cs
│   │   └── IDateTime.cs
│   ├── Behaviours/
│   │   ├── ValidationBehaviour.cs
│   │   ├── LoggingBehaviour.cs
│   │   └── PerformanceBehaviour.cs
│   ├── Exceptions/
│   │   ├── ValidationException.cs
│   │   ├── NotFoundException.cs
│   │   └── ForbiddenAccessException.cs
│   └── Mappings/
│       └── MappingProfile.cs
│
├── Features/
│   ├── Users/
│   │   ├── Commands/
│   │   │   ├── CreateUser/
│   │   │   │   ├── CreateUserCommand.cs
│   │   │   │   ├── CreateUserCommandHandler.cs
│   │   │   │   └── CreateUserCommandValidator.cs
│   │   │   └── UpdateUser/
│   │   │       └── ...
│   │   ├── Queries/
│   │   │   ├── GetUserById/
│   │   │   │   ├── GetUserByIdQuery.cs
│   │   │   │   ├── GetUserByIdQueryHandler.cs
│   │   │   │   └── UserDto.cs
│   │   │   └── GetAllUsers/
│   │   │       └── ...
│   │   └── EventHandlers/
│   │       └── UserCreatedEventHandler.cs
│   │
│   └── Orders/
│       ├── Commands/
│       ├── Queries/
│       └── EventHandlers/
│
└── DependencyInjection.cs
```

---

## PARTE 2: COMMANDS (CQRS - Escritura)

### Estructura de un Command

```csharp
// CreateUserCommand.cs
namespace MiApp.Application.Features.Users.Commands.CreateUser;

public record CreateUserCommand : IRequest<Guid>
{
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? PhoneNumber { get; init; }
}
```

### Handler del Command

```csharp
// CreateUserCommandHandler.cs
namespace MiApp.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Crear entidad de dominio
        var email = Email.Create(request.Email);
        var user = User.Create(email, request.FirstName, request.LastName);

        // 2. Persistir
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Retornar ID
        return user.Id;
    }
}
```

### Validator del Command (FluentValidation)

```csharp
// CreateUserCommandValidator.cs
namespace MiApp.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email es requerido")
            .EmailAddress().WithMessage("Email no tiene formato válido")
            .MustAsync(BeUniqueEmail).WithMessage("Email ya está registrado");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Nombre es requerido")
            .MaximumLength(100).WithMessage("Nombre no puede exceder 100 caracteres");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Apellido es requerido")
            .MaximumLength(100).WithMessage("Apellido no puede exceder 100 caracteres");
    }

    private async Task<bool> BeUniqueEmail(
        string email,
        CancellationToken cancellationToken)
    {
        return !await _userRepository.ExistsByEmailAsync(email, cancellationToken);
    }
}
```

---

## PARTE 3: QUERIES (CQRS - Lectura)

### Estructura de una Query

```csharp
// GetUserByIdQuery.cs
namespace MiApp.Application.Features.Users.Queries.GetUserById;

public record GetUserByIdQuery(Guid Id) : IRequest<UserDto?>;
```

### Handler de la Query

```csharp
// GetUserByIdQueryHandler.cs
namespace MiApp.Application.Features.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUserByIdQueryHandler(
        IUserRepository userRepository,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<UserDto?> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        
        return user is null ? null : _mapper.Map<UserDto>(user);
    }
}
```

### DTO de respuesta

```csharp
// UserDto.cs
namespace MiApp.Application.Features.Users.Queries.GetUserById;

public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public bool IsActive { get; init; }
}
```

---

## PARTE 4: BEHAVIOURS (Pipeline de Mediator)

> ⚠️ **NOTA IMPORTANTE sobre MediatR vs Mediator**
>
> - **MediatR v12+** es ahora **comercial/de pago** (~$500-2000/año para empresas)
> - Recomendamos usar **[Mediator](https://github.com/martinothamar/Mediator)** (MIT, gratuito)
> - Mediator usa **source generators** → mejor rendimiento, sin reflexión
> - La API es casi idéntica, migración sencilla
> - Ver `Documentos_Base/01_Estructura_Tecnica/GUIA_ARQUITECTURA.md` para detalles

### Behaviour de Validación

```csharp
// ValidationBehaviour.cs
namespace MiApp.Application.Common.Behaviours;

public class ValidationBehaviour<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

### Behaviour de Logging

```csharp
// LoggingBehaviour.cs
namespace MiApp.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehaviour(
        ILogger<LoggingBehaviour<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId;

        _logger.LogInformation(
            "Ejecutando {RequestName} por Usuario {UserId}: {@Request}",
            requestName, userId, request);

        var response = await next();

        _logger.LogInformation(
            "Completado {RequestName} por Usuario {UserId}",
            requestName, userId);

        return response;
    }
}
```

---

## PARTE 5: INTERFACES DE APPLICATION

### IUnitOfWork

```csharp
// IUnitOfWork.cs
namespace MiApp.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### ICurrentUserService

```csharp
// ICurrentUserService.cs
namespace MiApp.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
}
```

### IDateTime

```csharp
// IDateTime.cs
namespace MiApp.Application.Common.Interfaces;

public interface IDateTime
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}
```

---

## PARTE 6: EXCEPCIONES DE APPLICATION

```csharp
// ValidationException.cs
namespace MiApp.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("Se han producido uno o más errores de validación.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}

// NotFoundException.cs
namespace MiApp.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException()
        : base() { }

    public NotFoundException(string message)
        : base(message) { }

    public NotFoundException(string name, object key)
        : base($"Entidad \"{name}\" ({key}) no encontrada.") { }
}

// ForbiddenAccessException.cs
namespace MiApp.Application.Common.Exceptions;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("No tiene permisos para realizar esta acción.") { }
}
```

---

## PARTE 7: DEPENDENCY INJECTION

### Usando Mediator (Recomendado - MIT/Gratuito)

```csharp
// DependencyInjection.cs
namespace MiApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // AutoMapper
        services.AddAutoMapper(assembly);

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Mediator (martinothamar) - MIT license, source generators
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });

        // Pipeline Behaviors (misma sintaxis que MediatR)
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));

        return services;
    }
}
```

### Usando MediatR (Solo si ya tienes licencia comercial)

```csharp
// ⚠️ MediatR v12+ requiere licencia comercial (~$500-2000/año)
// Solo usar si la organización ya tiene licencia activa

public static IServiceCollection AddApplication(this IServiceCollection services)
{
    services.AddMediatR(cfg => {
        cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
    });
    return services;
}
```

---

## REGLAS CRÍTICAS

### ✅ SIEMPRE

- Usar records para Commands y Queries (inmutabilidad)
- Un archivo por Command/Query/Handler
- Validar inputs con FluentValidation antes del Handler
- Usar `CancellationToken` en todos los métodos async
- DTOs específicos por Query (no reutilizar entidades)

### ❌ NUNCA

- Inyectar DbContext directamente (usar repositorios)
- Lógica de negocio en Handlers (pertenece al Domain)
- Referencias a Infrastructure o Presentation
- Usar Entity Framework directamente
- Exponer entidades de dominio como respuesta

---

## PAQUETES NUGET RECOMENDADOS

```xml
<ItemGroup>
  <!-- ⚠️ Mediator (MIT/gratuito) en lugar de MediatR (comercial v12+) -->
  <PackageReference Include="Mediator.Abstractions" Version="2.*" />
  <PackageReference Include="Mediator.SourceGenerator" Version="2.*" OutputItemType="Analyzer" />

  <PackageReference Include="FluentValidation" Version="11.*" />
  <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.*" />
  <PackageReference Include="AutoMapper" Version="13.*" />
  <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.*" />
</ItemGroup>
```

> **¿Por qué Mediator en lugar de MediatR?**
> - MediatR v12+ requiere licencia comercial (~$500-2000/año)
> - Mediator es MIT (gratuito), usa source generators (mejor rendimiento)
> - API compatible, migración sencilla
> - Más info: `GUIA_ARQUITECTURA.md` sección "Mediator vs MediatR"

---

## REFERENCIAS

| Capa | Puede usar |
|------|------------|
| Application | → Domain (interfaces de repositorios) |
| Application | ✗ Infrastructure |
| Application | ✗ Presentation/API |
