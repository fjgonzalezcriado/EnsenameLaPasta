---
globs:
  - "**/Program.cs"
  - "**/Startup.cs"
  - "**/appsettings*.json"
  - "**/Views/**/*.cshtml"
  - "**/Pages/**/*.cshtml"
  - "**/Areas/**/*.cshtml"
  - "**/*.razor"
  - "**/Components/**/*.razor"
  - "**/wwwroot/**/*"
  - "**/*.css"
  - "**/*.scss"
  - "**/*.js"
  - "**/*.ts"
---

# Reglas para Aplicaciones Web ASP.NET

> Este archivo aplica cuando Claude trabaja con aplicaciones web.
> **Detecta automáticamente** la versión de .NET del proyecto.

---

## DETECCIÓN DE VERSIÓN

Antes de aplicar reglas, Claude debe identificar la versión:

```xml
<!-- En .csproj -->
<TargetFramework>net10.0</TargetFramework>  <!-- .NET 10 -->
<TargetFramework>net9.0</TargetFramework>   <!-- .NET 9 -->
<TargetFramework>net8.0</TargetFramework>   <!-- .NET 8 -->
<TargetFramework>net48</TargetFramework>    <!-- .NET Framework 4.8 -->
```

---

## PARTE 1: CONFIGURACIÓN POR VERSIÓN

### .NET 10 (LTS - Recomendado) ✅

```csharp
// Program.cs - .NET 10 con mejoras
var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════
// SERVICIOS - .NET 10
// ═══════════════════════════════════════════════════════════════

// Configuración tipada con validación integrada (nuevo en .NET 10)
builder.Services.AddOptionsWithValidateOnStart<AppSettings>()
    .Bind(builder.Configuration.GetSection("AppSettings"))
    .ValidateDataAnnotations();

// OpenAPI nativo (reemplaza Swashbuckle) - .NET 10
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info.Title = "API Comillas";
        document.Info.Version = "v1";
        return Task.CompletedTask;
    });
});

// Base de datos con mejoras EF Core 10
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// Autenticación mejorada
builder.Services.AddAuthentication()
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

// Health checks con tags
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database", tags: ["ready"])
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

var app = builder.Build();

// ═══════════════════════════════════════════════════════════════
// PIPELINE HTTP - .NET 10
// ═══════════════════════════════════════════════════════════════

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// OpenAPI endpoints nativos (.NET 10)
app.MapOpenApi();

// Health checks con filtros
app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });
app.MapHealthChecks("/health/live", new() { Predicate = check => check.Tags.Contains("live") });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

### .NET 8/9 (Migrar a .NET 10)

```csharp
// Program.cs - .NET 8/9
var builder = WebApplication.CreateBuilder(args);

// Configuración
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));

// Swagger (reemplazar por OpenAPI nativo en .NET 10)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

### .NET Framework 4.x (Legacy)

```csharp
// Global.asax.cs
public class MvcApplication : System.Web.HttpApplication
{
    protected void Application_Start()
    {
        AreaRegistration.RegisterAllAreas();
        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
        RouteConfig.RegisterRoutes(RouteTable.Routes);
        BundleConfig.RegisterBundles(BundleTable.Bundles);
    }
}

// Web.config para connection strings
<connectionStrings>
    <add name="DefaultConnection" 
         connectionString="Server=...;Database=...;" 
         providerName="System.Data.SqlClient" />
</connectionStrings>
```

---

## PARTE 2: COMPARATIVA DE CARACTERÍSTICAS

| Característica | .NET 4.x | .NET 8 | .NET 9 | .NET 10 |
|----------------|----------|--------|--------|---------|
| **Configuración** | Web.config | appsettings.json | appsettings.json | appsettings.json + validación |
| **DI** | Manual/Ninject | Built-in | Built-in | Built-in mejorado |
| **OpenAPI** | N/A | Swashbuckle | Swashbuckle | **Nativo** |
| **EF** | EF 6 | EF Core 8 | EF Core 9 | EF Core 10 |
| **Hosting** | IIS | Kestrel | Kestrel | Kestrel optimizado |
| **C#** | 7.3 | 12 | 13 | 13+ |

---

## PARTE 3: VISTAS RAZOR

### Blazor (.NET 10)

```razor
@* Componente Blazor .NET 10 con streaming rendering *@
@page "/becas"
@attribute [StreamRendering]
@inject IBecaService BecaService

<PageTitle>Listado de Becas</PageTitle>

@if (becas is null)
{
    <p>Cargando...</p>
}
else
{
    <QuickGrid Items="@becas.AsQueryable()" Pagination="@pagination">
        <PropertyColumn Property="@(b => b.Nombre)" Sortable="true" />
        <PropertyColumn Property="@(b => b.Importe)" Format="C" />
        <TemplateColumn>
            <a href="@($"/becas/{context.Id}")">Ver</a>
        </TemplateColumn>
    </QuickGrid>
    <Paginator State="@pagination" />
}

@code {
    private List<Beca>? becas;
    private PaginationState pagination = new() { ItemsPerPage = 10 };

    protected override async Task OnInitializedAsync()
    {
        becas = await BecaService.GetAllAsync();
    }
}
```

### Razor Views MVC (Todas las versiones)

```html
@model BecaViewModel

@{
    ViewData["Title"] = "Detalle de Beca";
}

<div class="container">
    <h1>@Model.Nombre</h1>
    
    <div class="card">
        <div class="card-body">
            <dl class="row">
                <dt class="col-sm-3">Importe</dt>
                <dd class="col-sm-9">@Model.Importe.ToString("C")</dd>
            </dl>
        </div>
    </div>
    
    @if (Model.PuedeEditar)
    {
        <a asp-action="Edit" asp-route-id="@Model.Id" class="btn btn-comillas-primary">
            Editar
        </a>
    }
</div>
```

---

## PARTE 4: ESTILOS CSS - COLORES COMILLAS

```css
:root {
    /* Primarios Comillas */
    --comillas-azul: #003366;
    --comillas-azul-claro: #0066cc;
    --comillas-azul-hover: #004d99;
    
    /* Secundarios */
    --comillas-gris: #666666;
    --comillas-gris-claro: #f5f5f5;
    --comillas-blanco: #ffffff;
    
    /* Estados */
    --color-exito: #28a745;
    --color-error: #dc3545;
    --color-aviso: #ffc107;
    --color-info: #17a2b8;
    
    /* Tipografía */
    --font-family-base: 'Segoe UI', system-ui, sans-serif;
    --font-size-base: 1rem;
}

/* Botones Comillas */
.btn-comillas-primary {
    background-color: var(--comillas-azul);
    color: var(--comillas-blanco);
    padding: 0.5rem 1.5rem;
    border: none;
    border-radius: 4px;
    font-weight: 500;
    transition: all 0.2s ease;
}

.btn-comillas-primary:hover {
    background-color: var(--comillas-azul-hover);
    transform: translateY(-1px);
}

.btn-comillas-primary:focus {
    outline: 3px solid var(--comillas-azul-claro);
    outline-offset: 2px;
}

/* Responsive */
@media (max-width: 768px) {
    .container { padding: 1rem; }
}

@media (min-width: 1024px) {
    .container { max-width: 1200px; margin: 0 auto; }
}
```

---

## PARTE 5: JAVASCRIPT MODERNO

```javascript
// Namespace Comillas - Compatible ES2022+
const Comillas = globalThis.Comillas ?? {};

Comillas.Becas = (() => {
    'use strict';
    
    const init = () => {
        bindEvents();
    };
    
    const bindEvents = () => {
        document.querySelectorAll('[data-action="delete"]')
            .forEach(btn => btn.addEventListener('click', handleDelete));
    };
    
    const handleDelete = async (event) => {
        event.preventDefault();
        
        if (!confirm('¿Está seguro de eliminar?')) return;
        
        const id = event.target.dataset.id;
        
        try {
            const response = await fetch(`/api/becas/${id}`, {
                method: 'DELETE',
                headers: {
                    'RequestVerificationToken': getAntiForgeryToken()
                }
            });
            
            if (response.ok) {
                event.target.closest('tr')?.remove();
                showNotification('Eliminado correctamente', 'success');
            } else {
                throw new Error(`HTTP ${response.status}`);
            }
        } catch (error) {
            console.error('Error:', error);
            showNotification('Error al eliminar', 'error');
        }
    };
    
    const getAntiForgeryToken = () => 
        document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    
    const showNotification = (message, type) => {
        // Implementar notificación toast
    };
    
    return { init };
})();

document.addEventListener('DOMContentLoaded', Comillas.Becas.init);
```

---

## PARTE 6: ACCESIBILIDAD (WCAG 2.1 AA)

### Contraste de Colores

| Elemento | Color | Fondo | Ratio | Estado |
|----------|-------|-------|-------|--------|
| Texto normal | #003366 | #ffffff | 12.6:1 | ✅ |
| Texto sobre azul | #ffffff | #003366 | 12.6:1 | ✅ |
| Links | #0066cc | #ffffff | 5.4:1 | ✅ |

### Formularios Accesibles

```html
<form id="becaForm" novalidate>
    <div class="form-group">
        <label for="nombre" class="form-label">
            Nombre de la beca <span aria-label="obligatorio">*</span>
        </label>
        <input type="text" 
               id="nombre" 
               name="nombre"
               class="form-control"
               required 
               minlength="3" 
               maxlength="200"
               aria-describedby="nombre-help nombre-error">
        <small id="nombre-help" class="form-text">
            Entre 3 y 200 caracteres
        </small>
        <div id="nombre-error" class="invalid-feedback" role="alert">
            El nombre es obligatorio
        </div>
    </div>
    
    <button type="submit" class="btn-comillas-primary">
        Guardar
    </button>
</form>
```

---

## PARTE 7: SEGURIDAD

### Secretos - NUNCA en código

```csharp
// ❌ NUNCA
"ConnectionStrings": {
    "Default": "Server=prod;Password=123456"
}

// ✅ Desarrollo - User Secrets
dotnet user-secrets set "ConnectionStrings:Default" "Server=..."

// ✅ Producción - Azure Key Vault (.NET 10)
builder.Configuration.AddAzureKeyVault(
    new Uri(builder.Configuration["KeyVault:Url"]!),
    new DefaultAzureCredential());
```

### Validación Anti-Forgery

```csharp
// .NET 10 - Automático en formularios
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
```

---

## CHECKLIST POR VERSIÓN

### .NET 10 ✅
- [ ] OpenAPI nativo (no Swashbuckle)
- [ ] AddOptionsWithValidateOnStart
- [ ] EF Core 10 features
- [ ] Health checks con tags
- [ ] Blazor streaming rendering

### .NET 8/9 (Preparar migración)
- [ ] Identificar Swashbuckle → OpenAPI
- [ ] Revisar breaking changes
- [ ] Actualizar NuGets

### .NET 4.x (Mantener)
- [ ] No introducir dependencias modernas
- [ ] Respetar Web.config
- [ ] Entity Framework 6

---

*Regla condicional v3.7.0 - Multi-versión .NET*
