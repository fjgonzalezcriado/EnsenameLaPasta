---
globs:
  - "**/*.razor"
  - "**/*.razor.cs"
  - "**/*.razor.css"
  - "**/Components/**/*.cs"
  - "**/Pages/**/*.cshtml"
  - "**/Areas/**/*.razor"
  - "**/_Imports.razor"
  - "**/App.razor"
  - "**/Routes.razor"
  - "**/BlazorWebAssembly*.csproj"
---

# Reglas para Blazor

> Este archivo aplica cuando Claude trabaja con proyectos Blazor.
> **Detecta automáticamente** el modo de renderizado del proyecto.

---

## DETECCIÓN DE MODO BLAZOR

```xml
<!-- En .csproj -->
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">    <!-- Blazor WASM -->
<Project Sdk="Microsoft.NET.Sdk.Web">                  <!-- Blazor Server / SSR -->

<!-- Paquetes que indican el modo -->
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" />  <!-- WASM -->
<PackageReference Include="Microsoft.AspNetCore.Components.Web" />           <!-- Server/SSR -->
```

---

## MODOS DE RENDERIZADO (.NET 10)

| Modo | Descripción | Uso recomendado |
|------|-------------|-----------------|
| **Static SSR** | HTML estático sin interactividad | Contenido informativo, SEO |
| **Interactive Server** | WebSocket, estado en servidor | Apps internas, tiempo real |
| **Interactive WASM** | Ejecuta en navegador | Apps offline, cálculos cliente |
| **Interactive Auto** | Server primero, WASM después | Mejor de ambos mundos |

### Configuración en Program.cs (.NET 10)

```csharp
// Program.cs - Blazor Web App (.NET 10)
var builder = WebApplication.CreateBuilder(args);

// Añadir servicios Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()     // Server-side
    .AddInteractiveWebAssemblyComponents(); // WASM

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

// Mapear componentes con modos de renderizado
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Client.Program).Assembly);

app.Run();
```

---

## PARTE 1: ESTRUCTURA DE PROYECTO

### Blazor Web App (.NET 10 - Recomendado)

```
Comillas.MiApp/
├── Comillas.MiApp/                 # Proyecto Server
│   ├── Components/
│   │   ├── App.razor
│   │   ├── Routes.razor
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor
│   │   │   ├── NavMenu.razor
│   │   │   └── MainLayout.razor.css
│   │   └── Pages/
│   │       ├── Home.razor
│   │       ├── Becas/
│   │       │   ├── Index.razor
│   │       │   ├── Create.razor
│   │       │   └── Edit.razor
│   │       └── Error.razor
│   ├── Services/
│   │   └── BecaService.cs
│   └── Program.cs
│
├── Comillas.MiApp.Client/          # Proyecto WASM (componentes interactivos)
│   ├── Pages/
│   │   └── Counter.razor
│   ├── Services/
│   │   └── ClientBecaService.cs
│   └── Program.cs
│
└── Comillas.MiApp.Shared/          # DTOs compartidos
    ├── DTOs/
    │   └── BecaDto.cs
    └── Validators/
        └── CreateBecaValidator.cs
```

### Blazor Server Tradicional

```
Comillas.MiApp.Server/
├── Components/
│   ├── App.razor
│   └── Pages/
│       └── Index.razor
├── Data/
│   └── BecaService.cs
└── Program.cs
```

---

## PARTE 2: COMPONENTES BLAZOR

### Componente con Render Mode (.NET 10)

```razor
@* Becas/Index.razor *@
@page "/becas"
@rendermode InteractiveServer   @* Modo interactivo server-side *@
@inject IBecaService BecaService
@inject NavigationManager Navigation

<PageTitle>Gestión de Becas</PageTitle>

<h1>Listado de Becas</h1>

<div class="toolbar mb-3">
    <button class="btn btn-comillas-primary" @onclick="CrearNueva">
        <i class="bi bi-plus"></i> Nueva Beca
    </button>
    <input type="search"
           class="form-control d-inline-block w-auto ms-2"
           placeholder="Buscar..."
           @bind="filtro"
           @bind:event="oninput"
           @bind:after="FiltrarBecas" />
</div>

@if (cargando)
{
    <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Cargando...</span>
    </div>
}
else if (becas is null || !becas.Any())
{
    <div class="alert alert-info">
        No se encontraron becas.
    </div>
}
else
{
    <QuickGrid Items="@becasFiltradas.AsQueryable()" Pagination="@pagination" Class="table">
        <PropertyColumn Property="@(b => b.Codigo)" Title="Código" Sortable="true" />
        <PropertyColumn Property="@(b => b.Nombre)" Title="Nombre" Sortable="true" />
        <PropertyColumn Property="@(b => b.Importe)" Title="Importe" Format="C" Sortable="true" />
        <PropertyColumn Property="@(b => b.Estado)" Title="Estado" />
        <TemplateColumn Title="Acciones">
            <div class="btn-group btn-group-sm">
                <button class="btn btn-outline-primary" @onclick="() => Editar(context.Id)">
                    <i class="bi bi-pencil"></i>
                </button>
                <button class="btn btn-outline-danger" @onclick="() => Eliminar(context)">
                    <i class="bi bi-trash"></i>
                </button>
            </div>
        </TemplateColumn>
    </QuickGrid>

    <Paginator State="@pagination" />
}

@code {
    private List<BecaDto>? becas;
    private IEnumerable<BecaDto> becasFiltradas = [];
    private string filtro = string.Empty;
    private bool cargando = true;
    private PaginationState pagination = new() { ItemsPerPage = 10 };

    protected override async Task OnInitializedAsync()
    {
        await CargarBecas();
    }

    private async Task CargarBecas()
    {
        cargando = true;
        try
        {
            becas = await BecaService.GetAllAsync();
            becasFiltradas = becas;
        }
        finally
        {
            cargando = false;
        }
    }

    private void FiltrarBecas()
    {
        if (string.IsNullOrWhiteSpace(filtro))
        {
            becasFiltradas = becas ?? [];
        }
        else
        {
            becasFiltradas = becas?.Where(b =>
                b.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                b.Codigo.Contains(filtro, StringComparison.OrdinalIgnoreCase)) ?? [];
        }
    }

    private void CrearNueva() => Navigation.NavigateTo("/becas/crear");

    private void Editar(int id) => Navigation.NavigateTo($"/becas/editar/{id}");

    private async Task Eliminar(BecaDto beca)
    {
        // Usar componente de confirmación
        var confirmado = await ConfirmarEliminacion(beca.Nombre);
        if (confirmado)
        {
            await BecaService.DeleteAsync(beca.Id);
            await CargarBecas();
        }
    }
}
```

### Componente de Formulario con Validación

```razor
@* Becas/Create.razor *@
@page "/becas/crear"
@page "/becas/editar/{Id:int}"
@rendermode InteractiveServer
@inject IBecaService BecaService
@inject NavigationManager Navigation

<PageTitle>@(Id.HasValue ? "Editar" : "Crear") Beca</PageTitle>

<h1>@(Id.HasValue ? "Editar" : "Nueva") Beca</h1>

<EditForm Model="@modelo" OnValidSubmit="Guardar" FormName="becaForm">
    <DataAnnotationsValidator />
    <FluentValidationValidator />

    <div class="row">
        <div class="col-md-6 mb-3">
            <label for="codigo" class="form-label">Código *</label>
            <InputText id="codigo"
                       @bind-Value="modelo.Codigo"
                       class="form-control"
                       disabled="@Id.HasValue" />
            <ValidationMessage For="@(() => modelo.Codigo)" class="text-danger" />
        </div>

        <div class="col-md-6 mb-3">
            <label for="nombre" class="form-label">Nombre *</label>
            <InputText id="nombre" @bind-Value="modelo.Nombre" class="form-control" />
            <ValidationMessage For="@(() => modelo.Nombre)" class="text-danger" />
        </div>
    </div>

    <div class="row">
        <div class="col-md-6 mb-3">
            <label for="importe" class="form-label">Importe *</label>
            <InputNumber id="importe"
                         @bind-Value="modelo.Importe"
                         class="form-control"
                         step="0.01" />
            <ValidationMessage For="@(() => modelo.Importe)" class="text-danger" />
        </div>

        <div class="col-md-6 mb-3">
            <label for="estado" class="form-label">Estado</label>
            <InputSelect id="estado" @bind-Value="modelo.Estado" class="form-select">
                @foreach (var estado in Enum.GetValues<EstadoBeca>())
                {
                    <option value="@estado">@estado</option>
                }
            </InputSelect>
        </div>
    </div>

    <div class="row">
        <div class="col-md-6 mb-3">
            <label for="fechaInicio" class="form-label">Fecha Inicio *</label>
            <InputDate id="fechaInicio" @bind-Value="modelo.FechaInicio" class="form-control" />
            <ValidationMessage For="@(() => modelo.FechaInicio)" class="text-danger" />
        </div>

        <div class="col-md-6 mb-3">
            <label for="fechaFin" class="form-label">Fecha Fin *</label>
            <InputDate id="fechaFin" @bind-Value="modelo.FechaFin" class="form-control" />
            <ValidationMessage For="@(() => modelo.FechaFin)" class="text-danger" />
        </div>
    </div>

    <div class="mb-3">
        <label for="descripcion" class="form-label">Descripción</label>
        <InputTextArea id="descripcion" @bind-Value="modelo.Descripcion" class="form-control" rows="3" />
    </div>

    <div class="d-flex gap-2">
        <button type="submit" class="btn btn-comillas-primary" disabled="@guardando">
            @if (guardando)
            {
                <span class="spinner-border spinner-border-sm me-1"></span>
            }
            @(Id.HasValue ? "Guardar Cambios" : "Crear Beca")
        </button>
        <button type="button" class="btn btn-secondary" @onclick="Cancelar">
            Cancelar
        </button>
    </div>
</EditForm>

@code {
    [Parameter] public int? Id { get; set; }

    private BecaFormModel modelo = new();
    private bool guardando;

    protected override async Task OnParametersSetAsync()
    {
        if (Id.HasValue)
        {
            var beca = await BecaService.GetByIdAsync(Id.Value);
            if (beca is not null)
            {
                modelo = new BecaFormModel
                {
                    Codigo = beca.Codigo,
                    Nombre = beca.Nombre,
                    Importe = beca.Importe,
                    Estado = beca.Estado,
                    FechaInicio = beca.FechaInicio,
                    FechaFin = beca.FechaFin,
                    Descripcion = beca.Descripcion
                };
            }
        }
    }

    private async Task Guardar()
    {
        guardando = true;
        try
        {
            if (Id.HasValue)
            {
                await BecaService.UpdateAsync(Id.Value, modelo);
            }
            else
            {
                await BecaService.CreateAsync(modelo);
            }
            Navigation.NavigateTo("/becas");
        }
        finally
        {
            guardando = false;
        }
    }

    private void Cancelar() => Navigation.NavigateTo("/becas");

    // Modelo del formulario
    public class BecaFormModel
    {
        [Required(ErrorMessage = "El código es obligatorio")]
        [StringLength(20, MinimumLength = 3)]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200, MinimumLength = 5)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 100000, ErrorMessage = "El importe debe estar entre 0.01 y 100.000")]
        public decimal Importe { get; set; }

        public EstadoBeca Estado { get; set; } = EstadoBeca.Borrador;

        [Required]
        public DateTime FechaInicio { get; set; } = DateTime.Today;

        [Required]
        public DateTime FechaFin { get; set; } = DateTime.Today.AddMonths(6);

        [StringLength(2000)]
        public string? Descripcion { get; set; }
    }
}
```

---

## PARTE 3: COMPONENTES REUTILIZABLES

### Componente de Confirmación

```razor
@* Shared/ConfirmDialog.razor *@
<div class="modal @(visible ? "show d-block" : "")" tabindex="-1" role="dialog">
    <div class="modal-dialog" role="document">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">@Titulo</h5>
                <button type="button" class="btn-close" @onclick="Cancelar"></button>
            </div>
            <div class="modal-body">
                <p>@Mensaje</p>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" @onclick="Cancelar">
                    Cancelar
                </button>
                <button type="button" class="btn btn-@ColorBoton" @onclick="Confirmar">
                    @TextoConfirmar
                </button>
            </div>
        </div>
    </div>
</div>
@if (visible)
{
    <div class="modal-backdrop fade show"></div>
}

@code {
    [Parameter] public string Titulo { get; set; } = "Confirmar";
    [Parameter] public string Mensaje { get; set; } = "¿Está seguro?";
    [Parameter] public string TextoConfirmar { get; set; } = "Confirmar";
    [Parameter] public string ColorBoton { get; set; } = "danger";
    [Parameter] public EventCallback<bool> OnClose { get; set; }

    private bool visible;

    public void Mostrar() => visible = true;

    private async Task Confirmar()
    {
        visible = false;
        await OnClose.InvokeAsync(true);
    }

    private async Task Cancelar()
    {
        visible = false;
        await OnClose.InvokeAsync(false);
    }
}
```

### Componente de Notificación Toast

```razor
@* Shared/ToastContainer.razor *@
@implements IDisposable
@inject ToastService ToastService

<div class="toast-container position-fixed bottom-0 end-0 p-3">
    @foreach (var toast in toasts)
    {
        <div class="toast show" role="alert">
            <div class="toast-header bg-@toast.Type text-white">
                <strong class="me-auto">@toast.Titulo</strong>
                <button type="button" class="btn-close btn-close-white"
                        @onclick="() => Cerrar(toast)"></button>
            </div>
            <div class="toast-body">
                @toast.Mensaje
            </div>
        </div>
    }
</div>

@code {
    private List<ToastMessage> toasts = [];

    protected override void OnInitialized()
    {
        ToastService.OnShow += MostrarToast;
    }

    private async Task MostrarToast(ToastMessage toast)
    {
        toasts.Add(toast);
        StateHasChanged();

        await Task.Delay(5000);
        Cerrar(toast);
    }

    private void Cerrar(ToastMessage toast)
    {
        toasts.Remove(toast);
        StateHasChanged();
    }

    public void Dispose()
    {
        ToastService.OnShow -= MostrarToast;
    }
}
```

### Servicio de Toast

```csharp
// Services/ToastService.cs
public class ToastService
{
    public event Func<ToastMessage, Task>? OnShow;

    public async Task Success(string mensaje, string titulo = "Éxito")
        => await Show(new ToastMessage(titulo, mensaje, "success"));

    public async Task Error(string mensaje, string titulo = "Error")
        => await Show(new ToastMessage(titulo, mensaje, "danger"));

    public async Task Warning(string mensaje, string titulo = "Aviso")
        => await Show(new ToastMessage(titulo, mensaje, "warning"));

    public async Task Info(string mensaje, string titulo = "Información")
        => await Show(new ToastMessage(titulo, mensaje, "info"));

    private async Task Show(ToastMessage toast)
    {
        if (OnShow is not null)
            await OnShow.Invoke(toast);
    }
}

public record ToastMessage(string Titulo, string Mensaje, string Type);
```

---

## PARTE 4: STREAMING RENDERING (.NET 10)

### Componente con Streaming

```razor
@* Becas/Dashboard.razor *@
@page "/becas/dashboard"
@attribute [StreamRendering]   @* Habilita streaming *@
@inject IBecaService BecaService

<PageTitle>Dashboard de Becas</PageTitle>

<h1>Dashboard</h1>

<div class="row">
    @if (estadisticas is null)
    {
        <div class="col-12">
            <p><em>Cargando estadísticas...</em></p>
        </div>
    }
    else
    {
        <div class="col-md-3">
            <div class="card bg-primary text-white">
                <div class="card-body">
                    <h5 class="card-title">Total Becas</h5>
                    <p class="card-text display-4">@estadisticas.TotalBecas</p>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card bg-success text-white">
                <div class="card-body">
                    <h5 class="card-title">Activas</h5>
                    <p class="card-text display-4">@estadisticas.BecasActivas</p>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card bg-info text-white">
                <div class="card-body">
                    <h5 class="card-title">Solicitudes</h5>
                    <p class="card-text display-4">@estadisticas.TotalSolicitudes</p>
                </div>
            </div>
        </div>
        <div class="col-md-3">
            <div class="card bg-warning">
                <div class="card-body">
                    <h5 class="card-title">Importe Total</h5>
                    <p class="card-text display-4">@estadisticas.ImporteTotal.ToString("C")</p>
                </div>
            </div>
        </div>
    }
</div>

@code {
    private DashboardEstadisticas? estadisticas;

    protected override async Task OnInitializedAsync()
    {
        // El streaming mostrará "Cargando..." mientras se obtienen los datos
        estadisticas = await BecaService.GetEstadisticasAsync();
    }
}
```

---

## PARTE 5: AUTENTICACIÓN Y AUTORIZACIÓN

### Componente Protegido

```razor
@page "/admin/becas"
@attribute [Authorize(Roles = "Admin,Gestor")]
@inject AuthenticationStateProvider AuthProvider

<AuthorizeView Roles="Admin,Gestor">
    <Authorized>
        <h1>Administración de Becas</h1>
        <p>Bienvenido, @context.User.Identity?.Name</p>

        <AuthorizeView Roles="Admin">
            <Authorized>
                <div class="alert alert-info">
                    Tienes permisos de administrador.
                </div>
            </Authorized>
        </AuthorizeView>

        @* Contenido para Admin y Gestor *@
    </Authorized>
    <NotAuthorized>
        <div class="alert alert-danger">
            No tienes permisos para acceder a esta página.
        </div>
    </NotAuthorized>
</AuthorizeView>
```

### Configuración de Auth (.NET 10)

```csharp
// Program.cs
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider,
    PersistingRevalidatingAuthenticationStateProvider>();

// Microsoft Entra ID / Azure AD
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
```

---

## PARTE 6: SERVICIOS Y HTTP

### Servicio para Blazor Server

```csharp
// Services/BecaService.cs (Server)
public class BecaService : IBecaService
{
    private readonly ApplicationDbContext _context;

    public BecaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BecaDto>> GetAllAsync()
    {
        return await _context.Becas
            .AsNoTracking()
            .Select(b => new BecaDto
            {
                Id = b.Id,
                Codigo = b.Codigo,
                Nombre = b.Nombre,
                Importe = b.Importe,
                Estado = b.Estado.ToString()
            })
            .ToListAsync();
    }
}
```

### Servicio para Blazor WASM (llamadas HTTP)

```csharp
// Services/ClientBecaService.cs (WASM)
public class ClientBecaService : IBecaService
{
    private readonly HttpClient _http;

    public ClientBecaService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<BecaDto>> GetAllAsync()
    {
        return await _http.GetFromJsonAsync<List<BecaDto>>("api/becas") ?? [];
    }

    public async Task<BecaDto?> GetByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<BecaDto?>($"api/becas/{id}");
    }

    public async Task CreateAsync(CreateBecaRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/becas", request);
        response.EnsureSuccessStatusCode();
    }
}
```

---

## PARTE 7: CSS ISOLATION

### Estilos de Componente

```css
/* Becas/Index.razor.css */
.toolbar {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 1rem 0;
}

.toolbar input[type="search"] {
    max-width: 250px;
}

/* Usar ::deep para afectar hijos */
::deep .table th {
    background-color: var(--comillas-azul);
    color: white;
}

::deep .table tr:hover {
    background-color: var(--comillas-gris-claro);
}
```

---

## PARTE 8: PRUEBAS DE COMPONENTES

### Test con bUnit

```csharp
// Tests/Components/BecaListTests.cs
public class BecaListTests : TestContext
{
    [Fact]
    public void MuestraSpinner_CuandoEstaCargando()
    {
        // Arrange
        var mockService = new Mock<IBecaService>();
        mockService.Setup(s => s.GetAllAsync())
            .Returns(new TaskCompletionSource<List<BecaDto>>().Task);

        Services.AddSingleton(mockService.Object);

        // Act
        var cut = RenderComponent<BecaList>();

        // Assert
        cut.Find(".spinner-border").Should().NotBeNull();
    }

    [Fact]
    public void MuestraBecas_CuandoHayDatos()
    {
        // Arrange
        var becas = new List<BecaDto>
        {
            new() { Id = 1, Nombre = "Beca Test", Importe = 5000 }
        };

        var mockService = new Mock<IBecaService>();
        mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(becas);

        Services.AddSingleton(mockService.Object);

        // Act
        var cut = RenderComponent<BecaList>();

        // Assert
        cut.Markup.Should().Contain("Beca Test");
        cut.Markup.Should().Contain("5.000");
    }
}
```

---

## CHECKLIST

### Componentes
- [ ] Modo de renderizado apropiado (`@rendermode`)
- [ ] Streaming para datos lentos (`@attribute [StreamRendering]`)
- [ ] Validación con DataAnnotations o FluentValidation
- [ ] Manejo de estados (cargando, vacío, error)
- [ ] CSS isolation para estilos

### Seguridad
- [ ] `[Authorize]` en páginas protegidas
- [ ] `<AuthorizeView>` para UI condicional
- [ ] Validación en servidor (no confiar en cliente)
- [ ] CSRF con `[ValidateAntiForgeryToken]`

### Rendimiento
- [ ] QuickGrid para tablas grandes
- [ ] Virtualización para listas largas
- [ ] Lazy loading de componentes
- [ ] Evitar re-renders innecesarios

### Accesibilidad
- [ ] `aria-label` en elementos interactivos
- [ ] `role="alert"` en notificaciones
- [ ] Focus visible en formularios
- [ ] Contraste de colores WCAG 2.1 AA

---

## PAQUETES NUGET RECOMENDADOS

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Components.QuickGrid" Version="10.*" />
  <PackageReference Include="Blazored.FluentValidation" Version="2.*" />
  <PackageReference Include="Blazored.LocalStorage" Version="4.*" />
  <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Authentication" Version="10.*" />
</ItemGroup>
```

---

*Regla condicional v3.7.0 - Blazor (.NET 10)*
