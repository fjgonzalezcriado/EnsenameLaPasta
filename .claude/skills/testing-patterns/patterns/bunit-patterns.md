# bUnit Patterns - Blazor Component Testing

> Skill: testing-patterns | Version: 3.7.0

Patrones para testing de componentes Blazor usando bUnit.
Cubre: render testing, event handling, forms, auth, JS interop, y componentes con servicios.

---

## Cuando Usar bUnit

| Usar bUnit | No Usar bUnit |
|---|---|
| Verificar renderizado de componentes | Tests de logica de negocio (usar unit tests) |
| Probar event handlers (@onclick, @onchange) | Tests de API endpoints (usar contract tests) |
| Validar formularios EditForm | Tests de rendimiento (usar performance tests) |
| Verificar navegacion y parametros | Tests de base de datos (usar integration tests) |
| Probar AuthorizeView condicional | Tests de CSS/estilos (usar visual regression) |

---

## Paquetes NuGet

```xml
<ItemGroup>
  <PackageReference Include="bunit" Version="1.*" />
  <PackageReference Include="xunit" Version="2.*" />
  <PackageReference Include="FluentAssertions" Version="6.*" />
  <PackageReference Include="Moq" Version="4.*" />
</ItemGroup>
```

---

## Patron 1: Render Testing Basico

```csharp
public class BecaListTests : TestContext
{
    [Fact]
    public void Render_WithBecas_ShowsTable()
    {
        // Arrange
        var becas = new List<BecaDto>
        {
            new() { Id = 1, Nombre = "Beca Excelencia", Importe = 5000m, Estado = "Publicada" },
            new() { Id = 2, Nombre = "Beca Movilidad", Importe = 3000m, Estado = "Borrador" }
        };

        var mockService = new Mock<IBecaService>();
        mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(becas);
        Services.AddSingleton(mockService.Object);

        // Act
        var cut = RenderComponent<BecaList>();

        // Assert
        cut.FindAll("tr").Count.Should().Be(3); // header + 2 rows
        cut.Markup.Should().Contain("Beca Excelencia");
        cut.Markup.Should().Contain("5.000");
        cut.Markup.Should().Contain("Beca Movilidad");
    }

    [Fact]
    public void Render_WithNoBecas_ShowsEmptyMessage()
    {
        // Arrange
        var mockService = new Mock<IBecaService>();
        mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<BecaDto>());
        Services.AddSingleton(mockService.Object);

        // Act
        var cut = RenderComponent<BecaList>();

        // Assert
        cut.Find(".alert-info").TextContent.Should().Contain("No se encontraron");
    }

    [Fact]
    public void Render_WhileLoading_ShowsSpinner()
    {
        // Arrange - Servicio que nunca completa
        var tcs = new TaskCompletionSource<List<BecaDto>>();
        var mockService = new Mock<IBecaService>();
        mockService.Setup(s => s.GetAllAsync()).Returns(tcs.Task);
        Services.AddSingleton(mockService.Object);

        // Act
        var cut = RenderComponent<BecaList>();

        // Assert
        cut.Find(".spinner-border").Should().NotBeNull();
        cut.Markup.Should().NotContain("table");
    }
}
```

---

## Patron 2: Event Handling

```csharp
public class BecaListEventTests : TestContext
{
    private readonly Mock<IBecaService> _mockService;

    public BecaListEventTests()
    {
        _mockService = new Mock<IBecaService>();
        _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<BecaDto>
        {
            new() { Id = 1, Nombre = "Beca Test", Importe = 5000m }
        });
        Services.AddSingleton(_mockService.Object);
    }

    [Fact]
    public void ClickDelete_CallsServiceDelete()
    {
        // Arrange
        _mockService.Setup(s => s.DeleteAsync(1)).Returns(Task.CompletedTask);
        var cut = RenderComponent<BecaList>();

        // Act - Click en boton eliminar
        var deleteButton = cut.Find("[data-testid='delete-1']");
        deleteButton.Click();

        // Confirmar dialogo
        var confirmButton = cut.Find(".btn-danger");
        confirmButton.Click();

        // Assert
        _mockService.Verify(s => s.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public void ClickCrear_NavigatesToCreatePage()
    {
        // Arrange
        var navManager = Services.GetRequiredService<FakeNavigationManager>();
        var cut = RenderComponent<BecaList>();

        // Act
        cut.Find(".btn-comillas-primary").Click();

        // Assert
        navManager.Uri.Should().EndWith("/becas/crear");
    }

    [Fact]
    public void SearchInput_FiltersBecas()
    {
        // Arrange
        _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<BecaDto>
        {
            new() { Id = 1, Nombre = "Beca Excelencia", Importe = 5000m },
            new() { Id = 2, Nombre = "Beca Movilidad", Importe = 3000m }
        });
        var cut = RenderComponent<BecaList>();

        // Act - Escribir en buscador
        var searchInput = cut.Find("input[type='search']");
        searchInput.Input("Excelencia");

        // Assert
        cut.FindAll("tbody tr").Count.Should().Be(1);
        cut.Markup.Should().Contain("Excelencia");
        cut.Markup.Should().NotContain("Movilidad");
    }
}
```

---

## Patron 3: Formularios con Validacion

```csharp
public class BecaFormTests : TestContext
{
    private readonly Mock<IBecaService> _mockService;

    public BecaFormTests()
    {
        _mockService = new Mock<IBecaService>();
        Services.AddSingleton(_mockService.Object);
    }

    [Fact]
    public void Submit_ValidData_CallsCreateAndNavigates()
    {
        // Arrange
        _mockService.Setup(s => s.CreateAsync(It.IsAny<CreateBecaRequest>()))
            .ReturnsAsync(new BecaDto { Id = 1 });

        var navManager = Services.GetRequiredService<FakeNavigationManager>();
        var cut = RenderComponent<BecaCreate>();

        // Act - Rellenar formulario
        cut.Find("#nombre").Change("Beca Test");
        cut.Find("#importe").Change("5000");
        cut.Find("#fechaInicio").Change("2026-04-01");
        cut.Find("#fechaFin").Change("2026-12-31");

        // Submit
        cut.Find("form").Submit();

        // Assert
        _mockService.Verify(s => s.CreateAsync(
            It.Is<CreateBecaRequest>(r =>
                r.Nombre == "Beca Test" && r.Importe == 5000m)),
            Times.Once);
        navManager.Uri.Should().EndWith("/becas");
    }

    [Fact]
    public void Submit_EmptyNombre_ShowsValidationError()
    {
        // Arrange
        var cut = RenderComponent<BecaCreate>();

        // Act - Submit sin datos
        cut.Find("form").Submit();

        // Assert - Mensajes de validacion visibles
        var validationMessages = cut.FindAll(".validation-message");
        validationMessages.Should().NotBeEmpty();
        cut.Markup.Should().Contain("obligatorio");
    }

    [Fact]
    public void Submit_NegativeImporte_ShowsValidationError()
    {
        // Arrange
        var cut = RenderComponent<BecaCreate>();

        // Act
        cut.Find("#nombre").Change("Test");
        cut.Find("#importe").Change("-100");
        cut.Find("form").Submit();

        // Assert
        cut.Markup.Should().Contain("debe ser positivo");
    }

    [Fact]
    public void EditMode_LoadsExistingData()
    {
        // Arrange
        _mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new BecaDto
        {
            Id = 1,
            Nombre = "Beca Existente",
            Importe = 5000m,
            Estado = "Borrador"
        });

        // Act - Render en modo edicion
        var cut = RenderComponent<BecaCreate>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Assert
        cut.Find("#nombre").GetAttribute("value").Should().Be("Beca Existente");
        cut.Find("h1").TextContent.Should().Contain("Editar");
    }
}
```

---

## Patron 4: Autorizacion (AuthorizeView)

```csharp
public class AdminPageTests : TestContext
{
    [Fact]
    public void AuthorizedUser_SeesContent()
    {
        // Arrange - Simular usuario autenticado con rol Admin
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@comillas.edu");
        authContext.SetRoles("Admin");

        // Act
        var cut = RenderComponent<AdminBecas>();

        // Assert
        cut.Markup.Should().Contain("Administración de Becas");
        cut.Markup.Should().NotContain("No tienes permisos");
    }

    [Fact]
    public void UnauthorizedUser_SeesAccessDenied()
    {
        // Arrange - Usuario sin rol Admin
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("user@comillas.edu");
        authContext.SetRoles("Usuario");

        // Act
        var cut = RenderComponent<AdminBecas>();

        // Assert
        cut.Markup.Should().Contain("No tienes permisos");
        cut.Markup.Should().NotContain("Administración de Becas");
    }

    [Fact]
    public void UnauthenticatedUser_SeesLoginPrompt()
    {
        // Arrange - Sin autenticacion
        var authContext = this.AddTestAuthorization();
        authContext.SetNotAuthorized();

        // Act
        var cut = RenderComponent<AdminBecas>();

        // Assert
        cut.Markup.Should().Contain("No tienes permisos");
    }

    [Fact]
    public void AdminRole_SeesAdminBadge_GestorDoesNot()
    {
        // Arrange - Admin ve badge especial
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@comillas.edu");
        authContext.SetRoles("Admin", "Gestor");

        // Act
        var cut = RenderComponent<AdminBecas>();

        // Assert
        cut.Markup.Should().Contain("permisos de administrador");
    }
}
```

---

## Patron 5: Componentes con Parametros y Cascading

```csharp
public class BecaCardTests : TestContext
{
    [Fact]
    public void Render_WithParameters_ShowsCorrectData()
    {
        // Arrange & Act
        var cut = RenderComponent<BecaCard>(parameters => parameters
            .Add(p => p.Beca, new BecaDto
            {
                Id = 1,
                Nombre = "Beca Test",
                Importe = 5000m,
                Estado = "Publicada"
            })
            .Add(p => p.OnDelete, (int id) => { /* callback */ }));

        // Assert
        cut.Find(".card-title").TextContent.Should().Contain("Beca Test");
        cut.Find(".badge").TextContent.Should().Contain("Publicada");
    }

    [Fact]
    public void Render_WithCascadingValue_ReceivesTheme()
    {
        // Arrange & Act
        var cut = RenderComponent<BecaCard>(parameters => parameters
            .Add(p => p.Beca, new BecaDto { Id = 1, Nombre = "Test" })
            .AddCascadingValue("Theme", "dark"));

        // Assert
        cut.Find(".card").ClassList.Should().Contain("dark-mode");
    }

    [Fact]
    public void DeleteCallback_IsInvoked_WithCorrectId()
    {
        // Arrange
        var deletedId = 0;
        var cut = RenderComponent<BecaCard>(parameters => parameters
            .Add(p => p.Beca, new BecaDto { Id = 42, Nombre = "Test" })
            .Add(p => p.OnDelete, (int id) => deletedId = id));

        // Act
        cut.Find(".btn-danger").Click();

        // Assert
        deletedId.Should().Be(42);
    }
}
```

---

## Patron 6: JavaScript Interop Mock

```csharp
public class ChartComponentTests : TestContext
{
    [Fact]
    public void Render_InitializesChart_ViaJsInterop()
    {
        // Arrange - Mock JS interop
        var jsInterop = this.JSInterop;
        jsInterop.SetupVoid("initChart", _ => true);  // Match cualquier argumento

        Services.AddSingleton(new Mock<IBecaService>().Object);

        // Act
        var cut = RenderComponent<BecaDashboard>();

        // Assert - Verificar que se llamo a JS
        jsInterop.VerifyInvoke("initChart");
    }

    [Fact]
    public async Task Export_CallsDownloadFile_ViaJsInterop()
    {
        // Arrange
        var jsInterop = this.JSInterop;
        jsInterop.SetupVoid("downloadFile", _ => true);

        var mockService = new Mock<IBecaService>();
        mockService.Setup(s => s.ExportAsync()).ReturnsAsync(new byte[] { 1, 2, 3 });
        Services.AddSingleton(mockService.Object);

        var cut = RenderComponent<BecaExport>();

        // Act
        cut.Find("#exportBtn").Click();

        // Assert
        jsInterop.VerifyInvoke("downloadFile", 1); // Llamado 1 vez
    }
}
```

---

## Base Class para Tests bUnit

```csharp
// BlazorTestBase.cs - Reutilizar en todos los tests de componentes
public abstract class BlazorTestBase : TestContext
{
    protected Mock<IBecaService> MockBecaService { get; }
    protected FakeNavigationManager NavManager => Services.GetRequiredService<FakeNavigationManager>();

    protected BlazorTestBase()
    {
        MockBecaService = new Mock<IBecaService>();
        Services.AddSingleton(MockBecaService.Object);

        // Servicios comunes
        Services.AddSingleton(new Mock<ILogger<object>>().Object);

        // JS Interop setup global
        JSInterop.SetupVoid("initChart", _ => true);
        JSInterop.SetupVoid("downloadFile", _ => true);
    }

    /// <summary>Simular usuario autenticado con roles</summary>
    protected TestAuthorizationContext SetupAuth(
        string username = "test@comillas.edu",
        params string[] roles)
    {
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized(username);
        auth.SetRoles(roles);
        return auth;
    }

    /// <summary>Crear lista de becas de test</summary>
    protected static List<BecaDto> CreateTestBecas(int count = 3)
    {
        return Enumerable.Range(1, count).Select(i => new BecaDto
        {
            Id = i,
            Nombre = $"Beca Test {i}",
            Importe = 1000m * i,
            Estado = i % 2 == 0 ? "Publicada" : "Borrador"
        }).ToList();
    }
}

// Uso
public class MiComponenteTests : BlazorTestBase
{
    [Fact]
    public void Test_ConBaseClass()
    {
        MockBecaService.Setup(s => s.GetAllAsync()).ReturnsAsync(CreateTestBecas());
        SetupAuth("admin@comillas.edu", "Admin");

        var cut = RenderComponent<BecaList>();
        cut.FindAll("tr").Count.Should().Be(4); // header + 3
    }
}
```

---

## Checklist bUnit

| Check | Descripcion |
|---|---|
| Render states | Loading, empty, data, error |
| Events | Click, input, change, submit |
| Validation | Required fields, invalid data, error messages |
| Auth | Authorized, unauthorized, roles |
| Parameters | Required, optional, cascading |
| Navigation | Links, redirects, route params |
| JS Interop | Setup mocks, verify calls |
| Lifecycle | OnInitialized, OnParametersSet, Dispose |

---

*Pattern bunit-patterns v3.7.0*
