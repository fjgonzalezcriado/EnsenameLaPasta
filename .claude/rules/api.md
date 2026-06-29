---
globs:
  - "src/**/Api/**/*.cs"
  - "**/Controllers/**/*.cs"
  - "**/*Controller.cs"
  - "**/Endpoints/**/*.cs"
  - "**/*Endpoint.cs"
  - "**/MinimalApi/**/*.cs"
---

# Reglas para APIs REST

> Este archivo aplica cuando Claude trabaja con controladores y endpoints API.
> **Detecta automáticamente** la versión de .NET del proyecto.

---

## DETECCIÓN DE VERSIÓN

```xml
<TargetFramework>net10.0</TargetFramework>  <!-- .NET 10 - OpenAPI nativo -->
<TargetFramework>net8.0</TargetFramework>   <!-- .NET 8 - Swashbuckle -->
<TargetFramework>net48</TargetFramework>    <!-- .NET 4.x - Web API 2 -->
```

---

## PARTE 1: CONTROLADORES POR VERSIÓN

### .NET 10 (Recomendado) - Controller con OpenAPI nativo

```csharp
/// <summary>
/// Gestión de becas
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BecasController : ControllerBase
{
    private readonly IBecaService _becaService;
    private readonly ILogger<BecasController> _logger;

    public BecasController(IBecaService becaService, ILogger<BecasController> logger)
    {
        _becaService = becaService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todas las becas con paginación
    /// </summary>
    /// <param name="pagina">Número de página (1-based)</param>
    /// <param name="tamaño">Elementos por página</param>
    /// <returns>Lista paginada de becas</returns>
    [HttpGet]
    [ProducesResponseType<PaginatedResult<BecaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<BecaDto>>> GetAll(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamaño = 10,
        CancellationToken ct = default)
    {
        var resultado = await _becaService.GetPaginatedAsync(pagina, tamaño, ct);
        return Ok(resultado);
    }

    /// <summary>
    /// Obtiene una beca por su ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<BecaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BecaDto>> GetById(int id, CancellationToken ct = default)
    {
        var beca = await _becaService.GetByIdAsync(id, ct);
        
        if (beca is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Beca no encontrada",
                Detail = $"No existe una beca con ID {id}",
                Status = StatusCodes.Status404NotFound
            });
        }
        
        return Ok(beca);
    }

    /// <summary>
    /// Crea una nueva beca
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Gestor")]
    [ProducesResponseType<BecaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BecaDto>> Create(
        [FromBody] CreateBecaRequest request,
        CancellationToken ct = default)
    {
        var beca = await _becaService.CreateAsync(request, ct);
        
        _logger.LogInformation("Beca {BecaId} creada por {User}", 
            beca.Id, User.Identity?.Name);
        
        return CreatedAtAction(nameof(GetById), new { id = beca.Id }, beca);
    }

    /// <summary>
    /// Actualiza una beca existente
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Gestor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateBecaRequest request,
        CancellationToken ct = default)
    {
        var exists = await _becaService.ExistsAsync(id, ct);
        if (!exists)
        {
            return NotFound();
        }

        await _becaService.UpdateAsync(id, request, ct);
        return NoContent();
    }

    /// <summary>
    /// Elimina una beca
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var deleted = await _becaService.DeleteAsync(id, ct);
        
        if (!deleted)
        {
            return NotFound();
        }

        _logger.LogInformation("Beca {BecaId} eliminada por {User}", 
            id, User.Identity?.Name);
        
        return NoContent();
    }
}
```

### .NET 10 - Minimal APIs (Alternativa)

```csharp
// Program.cs - Minimal APIs .NET 10
app.MapGroup("/api/becas")
    .WithTags("Becas")
    .WithOpenApi()
    .MapBecasEndpoints();

// BecasEndpoints.cs
public static class BecasEndpoints
{
    public static RouteGroupBuilder MapBecasEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAll)
            .WithName("GetAllBecas")
            .WithSummary("Obtiene todas las becas")
            .Produces<PaginatedResult<BecaDto>>();

        group.MapGet("/{id:int}", GetById)
            .WithName("GetBecaById")
            .Produces<BecaDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateBeca")
            .RequireAuthorization("GestorPolicy")
            .Produces<BecaDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<IResult> GetAll(
        IBecaService service,
        [AsParameters] PaginationRequest pagination,
        CancellationToken ct)
    {
        var result = await service.GetPaginatedAsync(pagination.Page, pagination.Size, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetById(
        int id,
        IBecaService service,
        CancellationToken ct)
    {
        var beca = await service.GetByIdAsync(id, ct);
        return beca is null 
            ? Results.NotFound() 
            : Results.Ok(beca);
    }

    private static async Task<IResult> Create(
        CreateBecaRequest request,
        IBecaService service,
        CancellationToken ct)
    {
        var beca = await service.CreateAsync(request, ct);
        return Results.CreatedAtRoute("GetBecaById", new { id = beca.Id }, beca);
    }
}
```

### .NET 8/9 (Migrar)

```csharp
// Similar a .NET 10 pero con Swashbuckle
// Diferencias para migración:
// - [SwaggerOperation] → Documentación XML nativa
// - app.UseSwagger() → app.MapOpenApi()
```

### .NET 4.x Web API 2 (Legacy)

```csharp
[RoutePrefix("api/becas")]
public class BecasController : ApiController
{
    private readonly IBecaService _becaService;

    public BecasController(IBecaService becaService)
    {
        _becaService = becaService;
    }

    [HttpGet]
    [Route("")]
    public IHttpActionResult GetAll()
    {
        var becas = _becaService.GetAll();
        return Ok(becas);
    }

    [HttpGet]
    [Route("{id:int}")]
    public IHttpActionResult GetById(int id)
    {
        var beca = _becaService.GetById(id);
        if (beca == null)
            return NotFound();
        return Ok(beca);
    }

    [HttpPost]
    [Route("")]
    [Authorize(Roles = "Admin")]
    public IHttpActionResult Create(CreateBecaRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var beca = _becaService.Create(request);
        return CreatedAtRoute("GetBecaById", new { id = beca.Id }, beca);
    }
}
```

---

## PARTE 2: OPENAPI / SWAGGER

### .NET 10 - OpenAPI Nativo

```csharp
// Program.cs
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "API Comillas - Gestión de Becas",
            Version = "v1",
            Description = "API para la gestión de becas universitarias",
            Contact = new OpenApiContact
            {
                Name = "STIC",
                Email = "soporte.stic@comillas.edu"
            }
        };
        return Task.CompletedTask;
    });
    
    // Seguridad
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };
        return Task.CompletedTask;
    });
});

// Mapear endpoints
app.MapOpenApi();                    // /openapi/v1.json
app.MapScalarApiReference();         // UI alternativa a Swagger UI
```

### .NET 8/9 - Swashbuckle (Migrar a OpenAPI)

```csharp
// Program.cs - A reemplazar
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Comillas", Version = "v1" });
});

app.UseSwagger();
app.UseSwaggerUI();
```

---

## PARTE 3: VALIDACIÓN

### FluentValidation (Todas las versiones modernas)

```csharp
public class CreateBecaRequestValidator : AbstractValidator<CreateBecaRequest>
{
    public CreateBecaRequestValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(200).WithMessage("Máximo 200 caracteres");

        RuleFor(x => x.Importe)
            .GreaterThan(0).WithMessage("El importe debe ser positivo")
            .LessThanOrEqualTo(50000).WithMessage("El importe máximo es 50.000€");

        RuleFor(x => x.FechaInicio)
            .GreaterThanOrEqualTo(DateTime.Today)
            .WithMessage("La fecha de inicio debe ser futura");

        RuleFor(x => x.FechaFin)
            .GreaterThan(x => x.FechaInicio)
            .WithMessage("La fecha fin debe ser posterior a la de inicio");
    }
}

// Registro en Program.cs (.NET 10)
builder.Services.AddValidatorsFromAssemblyContaining<CreateBecaRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
```

---

## PARTE 4: MANEJO DE ERRORES

### Global Exception Handler (.NET 10)

```csharp
// Program.cs
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        
        logger.LogError(exception, "Error no controlado");

        var problemDetails = exception switch
        {
            ValidationException ve => new ValidationProblemDetails(
                ve.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Error de validación"
            },
            
            NotFoundException nf => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Recurso no encontrado",
                Detail = nf.Message
            },
            
            UnauthorizedAccessException => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Acceso denegado"
            },
            
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Error interno del servidor",
                Detail = context.RequestServices
                    .GetRequiredService<IHostEnvironment>().IsDevelopment() 
                        ? exception?.Message 
                        : null
            }
        };

        context.Response.StatusCode = problemDetails.Status ?? 500;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});
```

---

## PARTE 5: VERBOS HTTP

| Verbo | Uso | Respuesta exitosa |
|-------|-----|-------------------|
| **GET** | Obtener recursos | 200 OK |
| **POST** | Crear recurso | 201 Created + Location |
| **PUT** | Reemplazar recurso | 204 No Content |
| **PATCH** | Actualización parcial | 200 OK o 204 |
| **DELETE** | Eliminar recurso | 204 No Content |

---

## CHECKLIST

### Antes de cada endpoint
- [ ] Atributo de verbo HTTP correcto
- [ ] Ruta con restricciones de tipo (`{id:int}`)
- [ ] [Authorize] si requiere autenticación
- [ ] [ProducesResponseType] para documentación
- [ ] Validación de entrada
- [ ] CancellationToken en métodos async
- [ ] Logging de operaciones importantes

### .NET 10 específico
- [ ] OpenAPI nativo (no Swashbuckle)
- [ ] ProblemDetails para errores
- [ ] Minimal APIs donde sea apropiado

---

*Regla condicional v3.7.0 - Multi-versión .NET*
