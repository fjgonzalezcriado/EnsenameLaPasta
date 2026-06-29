# Pattern: Response Caching

> **Pattern**: Response Caching
> **Problema**: Llamadas repetidas a datos que no cambian frecuentemente
> **Solución**: IMemoryCache / IDistributedCache con políticas de invalidación

---

## 🎯 Problema

Llamadas API repetidas para datos estáticos:
- **Catálogos** (países, universidades, asignaturas)
- **Configuración** (tarifas, convocatorias)
- **Datos de referencia** (códigos postales, monedas)

```csharp
// ❌ Sin caché - llama API cada vez
public async Task<List<UniversidadDto>> GetUniversidadesAsync()
{
    // Llama Banner API en cada request (latencia 500ms)
    return await _bannerClient.GetUniversidadesAsync();
}
```

---

## ✅ Solución: Response Caching

### Estrategias de Caché

| Estrategia | Uso | TTL típico |
|------------|-----|------------|
| **In-Memory** | Datos de configuración | 5-60 min |
| **Distributed (Redis)** | Multi-instancia, alta escala | 1-24 horas |
| **HTTP Cache-Control** | Browser/CDN caching | 5 min - 1 día |

---

## 1. In-Memory Cache (IMemoryCache)

### Configuración

```csharp
// Program.cs
builder.Services.AddMemoryCache();
```

### Implementación

```csharp
public class UniversidadService : IUniversidadService
{
    private readonly IBannerClient _bannerClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UniversidadService> _logger;

    public UniversidadService(
        IBannerClient bannerClient,
        IMemoryCache cache,
        ILogger<UniversidadService> logger)
    {
        _bannerClient = bannerClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<UniversidadDto>> GetUniversidadesAsync(CancellationToken ct = default)
    {
        const string cacheKey = "universidades";

        // 1. Intentar obtener desde caché
        if (_cache.TryGetValue(cacheKey, out List<UniversidadDto>? cachedData))
        {
            _logger.LogDebug("Universidades obtenidas desde caché");
            return cachedData!;
        }

        _logger.LogInformation("Universidades NO en caché, consultando Banner API");

        // 2. Si no está en caché, consultar API
        var universidades = await _bannerClient.GetUniversidadesAsync(ct);

        // 3. Guardar en caché con expiración
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            SlidingExpiration = TimeSpan.FromMinutes(30)
        };

        _cache.Set(cacheKey, universidades, cacheOptions);

        _logger.LogInformation("{Count} universidades cacheadas por 1 hora", universidades.Count);

        return universidades;
    }

    // Invalidar caché cuando se actualiza una universidad
    public async Task ActualizarUniversidadAsync(int id, UpdateUniversidadRequest request, CancellationToken ct)
    {
        await _bannerClient.UpdateUniversidadAsync(id, request, ct);

        // Invalidar caché
        _cache.Remove("universidades");
        _logger.LogInformation("Caché de universidades invalidado tras actualización");
    }
}
```

---

## 2. Distributed Cache (Redis)

### Configuración

```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "ComillasApp:";
});
```

### Implementación

```csharp
public class UniversidadService : IUniversidadService
{
    private readonly IBannerClient _bannerClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<UniversidadService> _logger;

    public async Task<List<UniversidadDto>> GetUniversidadesAsync(CancellationToken ct = default)
    {
        const string cacheKey = "universidades";

        // 1. Intentar obtener desde Redis
        var cachedData = await _cache.GetStringAsync(cacheKey, ct);

        if (!string.IsNullOrEmpty(cachedData))
        {
            _logger.LogDebug("Universidades obtenidas desde Redis");
            return JsonSerializer.Deserialize<List<UniversidadDto>>(cachedData)!;
        }

        _logger.LogInformation("Universidades NO en Redis, consultando Banner API");

        // 2. Consultar API
        var universidades = await _bannerClient.GetUniversidadesAsync(ct);

        // 3. Guardar en Redis
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
            SlidingExpiration = TimeSpan.FromHours(2)
        };

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(universidades),
            cacheOptions,
            ct);

        _logger.LogInformation("{Count} universidades cacheadas en Redis por 24 horas", universidades.Count);

        return universidades;
    }
}
```

### appsettings.json

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false"
  }
}
```

---

## 3. Cache Aside Pattern

### Helper Genérico

```csharp
public static class CacheHelper
{
    public static async Task<T> GetOrCreateAsync<T>(
        this IMemoryCache cache,
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null)
    {
        if (cache.TryGetValue(key, out T? cachedValue))
            return cachedValue!;

        var value = await factory();

        var options = new MemoryCacheEntryOptions();

        if (absoluteExpiration.HasValue)
            options.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;

        if (slidingExpiration.HasValue)
            options.SlidingExpiration = slidingExpiration.Value;

        cache.Set(key, value, options);

        return value;
    }
}

// Uso
public async Task<List<UniversidadDto>> GetUniversidadesAsync(CancellationToken ct = default)
{
    return await _cache.GetOrCreateAsync(
        "universidades",
        () => _bannerClient.GetUniversidadesAsync(ct),
        absoluteExpiration: TimeSpan.FromHours(1));
}
```

---

## 4. HTTP Response Caching

### Configuración en Program.cs

```csharp
// Program.cs
builder.Services.AddResponseCaching();

var app = builder.Build();

app.UseResponseCaching();
app.UseHttpCacheHeaders(); // Opcional: Marvin.Cache.Headers
```

### Controller con Cache

```csharp
[ApiController]
[Route("api/[controller]")]
public class UniversidadesController : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 600)] // Cache 10 minutos
    public async Task<ActionResult<List<UniversidadDto>>> GetAll(CancellationToken ct)
    {
        var universidades = await _service.GetUniversidadesAsync(ct);
        return Ok(universidades);
    }

    [HttpGet("{id}")]
    [ResponseCache(VaryByHeader = "User-Agent", Duration = 300)]
    public async Task<ActionResult<UniversidadDto>> GetById(int id, CancellationToken ct)
    {
        var universidad = await _service.GetByIdAsync(id, ct);
        return universidad is null ? NotFound() : Ok(universidad);
    }
}
```

---

## 5. Cache-Control Headers

### Configuración Personalizada

```csharp
[HttpGet]
public async Task<ActionResult<List<UniversidadDto>>> GetAll()
{
    var universidades = await _service.GetUniversidadesAsync();

    // Cache-Control personalizado
    Response.Headers.CacheControl = "public, max-age=600, s-maxage=1800";
    Response.Headers.Expires = DateTime.UtcNow.AddMinutes(10).ToString("R");
    Response.Headers.ETag = GenerateETag(universidades);

    return Ok(universidades);
}

private string GenerateETag(object data)
{
    var json = JsonSerializer.Serialize(data);
    var hash = MD5.HashData(Encoding.UTF8.GetBytes(json));
    return Convert.ToBase64String(hash);
}
```

---

## 6. Invalidación de Caché

### Estrategias

#### Por Tiempo (TTL)

```csharp
// Automático con AbsoluteExpiration
var options = new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
};
```

#### Por Evento (Manual)

```csharp
public async Task ActualizarUniversidadAsync(int id, UpdateRequest request, CancellationToken ct)
{
    await _bannerClient.UpdateAsync(id, request, ct);

    // Invalidar caché específico
    _cache.Remove($"universidad:{id}");

    // Invalidar caché de lista
    _cache.Remove("universidades");
}
```

#### Por Patrón (Redis)

```csharp
public async Task InvalidarCachePorPatronAsync(string pattern)
{
    // Ejemplo: invalidar "universidad:*"
    var redis = ConnectionMultiplexer.Connect(connectionString);
    var server = redis.GetServer(redis.GetEndPoints().First());

    foreach (var key in server.Keys(pattern: $"ComillasApp:{pattern}"))
    {
        await _cache.RemoveAsync(key.ToString());
    }
}
```

---

## 📊 Comparativa

| Tipo | Latencia | Persistencia | Multi-instancia | Uso |
|------|----------|--------------|-----------------|-----|
| **IMemoryCache** | <1ms | En proceso | ❌ | Datos temporales, single instance |
| **Redis** | ~5ms | Sí | ✅ | Producción, multi-instance |
| **HTTP Cache** | ~0ms (browser) | No | ✅ | Datos públicos, CDN |

---

## ⚠️ Consideraciones

### ✅ Cachear

- Datos de catálogo (países, monedas)
- Configuración estática
- Resultados de cálculos pesados
- Listados con paginación

### ❌ NO Cachear

- Datos sensibles (usuarios, contraseñas)
- Datos en tiempo real (stock, precios)
- Datos personalizados por usuario (sin VaryBy)
- Respuestas con datos dinámicos

---

## 🧪 Testing

```csharp
[Fact]
public async Task GetUniversidades_CachesResponse()
{
    // Arrange
    var mockClient = new Mock<IBannerClient>();
    mockClient
        .Setup(c => c.GetUniversidadesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<UniversidadDto> { new() { Id = 1, Nombre = "Test" } });

    var cache = new MemoryCache(new MemoryCacheOptions());
    var sut = new UniversidadService(mockClient.Object, cache, Mock.Of<ILogger<UniversidadService>>());

    // Act - Primera llamada
    var result1 = await sut.GetUniversidadesAsync();

    // Act - Segunda llamada
    var result2 = await sut.GetUniversidadesAsync();

    // Assert
    mockClient.Verify(c => c.GetUniversidadesAsync(It.IsAny<CancellationToken>()), Times.Once); // Solo 1 llamada API
    result1.Should().BeEquivalentTo(result2);
}
```

---

## 📚 Referencias

- [Response Caching](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/response)
- [Distributed Caching](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed)

---

*Pattern: response-caching - STIC.IA v3.7.0*
