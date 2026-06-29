# API Contract Testing

> Skill: testing-patterns | Version: 3.7.0

Contract testing verifica que los endpoints mantienen su contrato: status codes, estructura de respuesta, headers, y compatibilidad backward. Incluye WebApplicationFactory, Pact.NET para consumer-driven contracts, y OpenAPI validation.

---

## Cuando Usar Contract Testing

| Usar | No Usar |
|------|---------|
| APIs consumidas por otros equipos/sistemas | APIs internas con un solo consumidor |
| Endpoints publicos con contratos documentados | Logica de negocio (usar unit tests) |
| Verificar backward compatibility en PRs | Tests de rendimiento (usar performance-testing) |
| Integrar con microservicios externos | Validacion de datos (usar FluentValidation tests) |

---

## Paquetes NuGet

```xml
<ItemGroup>
  <!-- Contract testing basico -->
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.*" />
  <PackageReference Include="xunit" Version="2.*" />
  <PackageReference Include="FluentAssertions" Version="6.*" />

  <!-- Consumer-driven contracts (opcional) -->
  <PackageReference Include="PactNet" Version="5.*" />

  <!-- OpenAPI validation (opcional) -->
  <PackageReference Include="Verify.Http" Version="6.*" />
</ItemGroup>
```

---

## Patron 1: Contract Tests Basicos con WebApplicationFactory

```csharp
public class BecasContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BecasContractTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Reemplazar DB con in-memory para aislamiento
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(o =>
                    o.UseInMemoryDatabase("ContractTests_" + Guid.NewGuid()));
            });
        }).CreateClient();
    }

    // ═══════════════════════════════════════════════════════════
    // STATUS CODE CONTRACTS
    // ═══════════════════════════════════════════════════════════

    [Theory]
    [InlineData("/api/becas", HttpStatusCode.OK)]
    [InlineData("/api/becas/999999", HttpStatusCode.NotFound)]
    [InlineData("/api/health", HttpStatusCode.OK)]
    [InlineData("/api/health/ready", HttpStatusCode.OK)]
    [InlineData("/api/noexiste", HttpStatusCode.NotFound)]
    public async Task Endpoint_ReturnsExpectedStatusCode(string url, HttpStatusCode expected)
    {
        // Act
        var response = await _client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(expected);
    }

    // ═══════════════════════════════════════════════════════════
    // RESPONSE SHAPE CONTRACTS
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetBecas_ReturnsPaginatedResponse_WithRequiredFields()
    {
        // Act
        var response = await _client.GetAsync("/api/becas");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert - Verificar estructura del contrato
        json.TryGetProperty("items", out var items).Should().BeTrue("'items' es obligatorio");
        json.TryGetProperty("totalItems", out var total).Should().BeTrue("'totalItems' es obligatorio");
        json.TryGetProperty("page", out var page).Should().BeTrue("'page' es obligatorio");
        json.TryGetProperty("pageSize", out var size).Should().BeTrue("'pageSize' es obligatorio");

        // Tipos correctos
        items.ValueKind.Should().Be(JsonValueKind.Array);
        total.ValueKind.Should().Be(JsonValueKind.Number);
        page.ValueKind.Should().Be(JsonValueKind.Number);
    }

    [Fact]
    public async Task GetBecaById_ReturnsExpectedShape()
    {
        // Arrange - Crear dato via API
        var createRequest = new { Nombre = "Contract Test", Importe = 1000m };
        var createResponse = await _client.PostAsJsonAsync("/api/becas", createRequest);
        var location = createResponse.Headers.Location;

        // Act
        var response = await _client.GetAsync(location);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert - Verificar todos los campos del contrato
        json.TryGetProperty("id", out _).Should().BeTrue();
        json.TryGetProperty("nombre", out var nombre).Should().BeTrue();
        json.TryGetProperty("importe", out var importe).Should().BeTrue();
        json.TryGetProperty("estado", out _).Should().BeTrue();
        json.TryGetProperty("createdAt", out _).Should().BeTrue();

        nombre.GetString().Should().Be("Contract Test");
        importe.GetDecimal().Should().Be(1000m);
    }

    // ═══════════════════════════════════════════════════════════
    // HEADER CONTRACTS
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task AllEndpoints_ReturnJsonContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/becas");

        // Assert
        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/json");
    }

    [Fact]
    public async Task Create_ReturnsLocationHeader()
    {
        // Arrange
        var request = new { Nombre = "Location Test", Importe = 500m };

        // Act
        var response = await _client.PostAsJsonAsync("/api/becas", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull("POST exitoso debe incluir Location header");
        response.Headers.Location!.ToString().Should().Contain("/api/becas/");
    }

    // ═══════════════════════════════════════════════════════════
    // ERROR RESPONSE CONTRACTS (ProblemDetails RFC 7807)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task NotFound_ReturnsProblemDetails()
    {
        // Act
        var response = await _client.GetAsync("/api/becas/999999");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert - ProblemDetails shape
        json.TryGetProperty("type", out _).Should().BeTrue();
        json.TryGetProperty("title", out _).Should().BeTrue();
        json.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetInt32().Should().Be(404);
    }

    [Fact]
    public async Task ValidationError_ReturnsValidationProblemDetails()
    {
        // Arrange - Datos invalidos
        var request = new { Nombre = "", Importe = -100m };

        // Act
        var response = await _client.PostAsJsonAsync("/api/becas", request);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert - ValidationProblemDetails shape
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        json.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.ValueKind.Should().Be(JsonValueKind.Object);
    }

    // ═══════════════════════════════════════════════════════════
    // BACKWARD COMPATIBILITY CONTRACTS
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetBecas_V1Fields_StillPresent()
    {
        // Estos campos existian en v1 y DEBEN seguir existiendo
        var response = await _client.GetAsync("/api/becas");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = json.GetProperty("items");

        if (items.GetArrayLength() > 0)
        {
            var firstItem = items[0];
            // Campos v1 - NUNCA eliminar
            firstItem.TryGetProperty("id", out _).Should().BeTrue("'id' existe desde v1");
            firstItem.TryGetProperty("nombre", out _).Should().BeTrue("'nombre' existe desde v1");
            firstItem.TryGetProperty("importe", out _).Should().BeTrue("'importe' existe desde v1");
            firstItem.TryGetProperty("estado", out _).Should().BeTrue("'estado' existe desde v1");
        }
    }
}
```

---

## Patron 2: Consumer-Driven Contracts con Pact.NET

Para APIs consumidas por otros equipos/microservicios.

### Provider Side (tu API)

```csharp
public class BecasProviderPactTests : IDisposable
{
    private readonly PactVerifier _verifier;

    public BecasProviderPactTests()
    {
        _verifier = new PactVerifier("BecasAPI");
    }

    [Fact]
    public void EnsureProviderHonoursConsumerPacts()
    {
        // Arrange - Levantar API de test
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var baseUri = client.BaseAddress!;

        // Act & Assert - Verificar contra pacts de consumidores
        _verifier
            .WithHttpEndpoint(baseUri)
            .WithPactBrokerSource(new Uri("https://pact-broker.comillas.edu"), options =>
            {
                options.ConsumerVersionSelectors(
                    new ConsumerVersionSelector { MainBranch = true });
            })
            .WithProviderStateUrl(new Uri(baseUri, "/api/pact-states"))
            .Verify();
    }

    public void Dispose() => _verifier.Dispose();
}
```

### Consumer Side (quien consume tu API)

```csharp
public class BecasConsumerPactTests
{
    private readonly IPactBuilderV4 _pactBuilder;

    public BecasConsumerPactTests()
    {
        var pact = Pact.V4("FrontendApp", "BecasAPI", new PactConfig
        {
            PactDir = Path.Combine("..", "..", "..", "pacts")
        });
        _pactBuilder = pact.WithHttpInteractions();
    }

    [Fact]
    public async Task GetBecas_ReturnsExpectedFormat()
    {
        // Arrange - Definir expectativa del consumidor
        _pactBuilder
            .UponReceiving("a request for all becas")
            .WithRequest(HttpMethod.Get, "/api/becas")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new
            {
                items = Match.MinType(new
                {
                    id = Match.Integer(1),
                    nombre = Match.Type("Beca Test"),
                    importe = Match.Decimal(5000.00m),
                    estado = Match.Type("Publicada")
                }, 1),
                totalItems = Match.Integer(1),
                page = Match.Integer(1),
                pageSize = Match.Integer(10)
            });

        // Act & Assert
        await _pactBuilder.VerifyAsync(async ctx =>
        {
            var client = new HttpClient { BaseAddress = ctx.MockServerUri };
            var response = await client.GetFromJsonAsync<JsonElement>("/api/becas");

            response.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
        });
    }
}
```

---

## Patron 3: OpenAPI Contract Validation

Verificar que la API cumple con su especificacion OpenAPI.

```csharp
public class OpenApiContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OpenApiContractTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task OpenApiSpec_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/openapi/v1.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"openapi\"");
        content.Should().Contain("\"paths\"");
    }

    [Fact]
    public async Task OpenApiSpec_ContainsAllExpectedPaths()
    {
        // Act
        var spec = await _client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var paths = spec.GetProperty("paths");

        // Assert - Todos los endpoints documentados
        var expectedPaths = new[]
        {
            "/api/becas",
            "/api/becas/{id}",
            "/api/health",
            "/api/health/ready"
        };

        foreach (var path in expectedPaths)
        {
            paths.TryGetProperty(path, out _).Should().BeTrue(
                $"Path '{path}' debe estar en la especificacion OpenAPI");
        }
    }

    [Fact]
    public async Task OpenApiSpec_BecaSchema_HasRequiredFields()
    {
        // Act
        var spec = await _client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var schemas = spec.GetProperty("components").GetProperty("schemas");

        // Assert - BecaDto schema
        schemas.TryGetProperty("BecaDto", out var becaSchema).Should().BeTrue();
        var properties = becaSchema.GetProperty("properties");

        properties.TryGetProperty("id", out _).Should().BeTrue();
        properties.TryGetProperty("nombre", out _).Should().BeTrue();
        properties.TryGetProperty("importe", out _).Should().BeTrue();
        properties.TryGetProperty("estado", out _).Should().BeTrue();
    }
}
```

---

## Patron 4: Versionado de Contratos

```csharp
public class ApiVersionContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiVersionContractTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task V1Endpoints_StillAvailable()
    {
        // Endpoints v1 que NUNCA deben desaparecer
        var v1Endpoints = new[]
        {
            ("GET", "/api/becas"),
            ("GET", "/api/becas/1"),
            ("POST", "/api/becas"),
            ("PUT", "/api/becas/1"),
            ("DELETE", "/api/becas/1")
        };

        foreach (var (method, url) in v1Endpoints)
        {
            var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (method is "POST" or "PUT")
            {
                request.Content = JsonContent.Create(new { Nombre = "Test", Importe = 1000m });
            }

            var response = await _client.SendAsync(request);

            // No debe ser 404 ni 405
            response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
                $"{method} {url} debe seguir existiendo (contrato v1)");
            response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed,
                $"{method} {url} debe seguir aceptando {method} (contrato v1)");
        }
    }

    [Fact]
    public async Task ResponseFields_NeverRemoved_OnlyAdded()
    {
        // Campos que existian en v1 - regla: solo se anaden campos, nunca se eliminan
        var response = await _client.GetAsync("/api/becas");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        // v1 contract fields
        var requiredFields = new[] { "items", "totalItems", "page", "pageSize" };
        foreach (var field in requiredFields)
        {
            json.TryGetProperty(field, out _).Should().BeTrue(
                $"Campo '{field}' es parte del contrato v1 y no debe eliminarse");
        }
    }
}
```

---

## Checklist de Contract Testing

| Check | Descripcion |
|-------|-------------|
| Status codes | Todos los endpoints devuelven los codigos esperados |
| Response shape | Campos obligatorios presentes con tipos correctos |
| Error format | Errores siguen ProblemDetails RFC 7807 |
| Headers | Content-Type, Location, CORS headers correctos |
| Backward compat | Campos v1 siguen presentes |
| OpenAPI sync | Spec refleja endpoints reales |
| Pact (si aplica) | Contratos de consumidores verificados |

---

*Pattern api-contract-testing v3.7.0*
