# Example: Integración con Oracle HCM Cloud

> **Caso de uso**: Obtener empleados activos con paginación y caché
> **API**: Oracle HCM Cloud
> **Autenticación**: OAuth2 Client Credentials
> **Características**: Paginación, Cache distribuido (Redis)

---

## 1. Configuración

```json
{
  "OracleHcmApi": {
    "BaseUrl": "https://oracle-hcm.comillas.edu/api",
    "ClientId": "{{KeyVault}}",
    "ClientSecret": "{{KeyVault}}",
    "TokenUrl": "https://oracle-auth.comillas.edu/oauth/token",
    "PageSize": 100,
    "CacheDurationMinutes": 60
  },
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false"
  }
}
```

---

## 2. Registro en Program.cs

```csharp
// Redis para caché distribuida
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "ComillasApp:";
});

// Oracle HCM Client
builder.Services.AddHttpClient<IOracleHcmClient, OracleHcmClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<OracleHcmApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(45);
})
.AddBearerTokenHandler()
.AddCommonHandlers()
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(5 * attempt)));
```

---

## 3. Service con Caché

```csharp
public class EmployeeService : IEmployeeService
{
    private readonly IOracleHcmClient _oracleHcmClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<EmployeeService> _logger;

    public async Task<List<EmployeeDto>> GetActiveEmployeesAsync(CancellationToken ct = default)
    {
        const string cacheKey = "employees:active";

        // 1. Intentar desde Redis
        var cachedData = await _cache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cachedData))
        {
            _logger.LogInformation("Empleados obtenidos desde Redis");
            return JsonSerializer.Deserialize<List<EmployeeDto>>(cachedData)!;
        }

        _logger.LogInformation("Empleados NO en caché, consultando Oracle HCM");

        // 2. Consultar API con paginación
        var allEmployees = new List<EmployeeDto>();
        var hasMore = true;
        var offset = 0;
        var pageSize = 100;

        while (hasMore)
        {
            var response = await _oracleHcmClient.GetEmployeesAsync(offset, pageSize, ct);
            allEmployees.AddRange(response.Items);

            hasMore = response.HasMore;
            offset += pageSize;

            _logger.LogDebug("Obtenidos {Count} empleados (offset: {Offset})", response.Items.Count, offset);
        }

        // 3. Filtrar solo activos
        var activeEmployees = allEmployees.Where(e => e.Status == "Active").ToList();

        // 4. Cachear en Redis por 1 hora
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(activeEmployees),
            cacheOptions,
            ct);

        _logger.LogInformation("{Count} empleados activos cacheados en Redis", activeEmployees.Count);

        return activeEmployees;
    }
}
```

---

## 4. Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    [HttpGet("active")]
    [ResponseCache(Duration = 600)] // Cache HTTP adicional (10 min)
    public async Task<ActionResult<List<EmployeeDto>>> GetActiveEmployees(CancellationToken ct)
    {
        var employees = await _employeeService.GetActiveEmployeesAsync(ct);
        return Ok(employees);
    }
}
```

---

## 5. Tests

```csharp
[Fact]
public async Task GetActiveEmployees_CachesInRedis()
{
    // Arrange
    var mockClient = new Mock<IOracleHcmClient>();
    mockClient
        .Setup(c => c.GetEmployeesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new OracleHcmListResponse<EmployeeDto>(
            new List<EmployeeDto> { new("001", "John", "Doe", "john@test.com", "IT", "Developer", DateTime.Now, "Active") },
            1,
            false,
            100,
            0,
            null));

    var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    var sut = new EmployeeService(mockClient.Object, cache, Mock.Of<ILogger<EmployeeService>>());

    // Act
    var result1 = await sut.GetActiveEmployeesAsync();
    var result2 = await sut.GetActiveEmployeesAsync(); // Segunda llamada desde caché

    // Assert
    mockClient.Verify(c => c.GetEmployeesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    result2.Should().BeEquivalentTo(result1);
}
```

---

*Example: oracle-hcm-example - STIC.IA v3.7.0*
