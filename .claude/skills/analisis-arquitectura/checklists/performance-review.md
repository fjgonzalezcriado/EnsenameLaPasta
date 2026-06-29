# Checklist de Revisión de Rendimiento

> Skill: analisis-arquitectura
> Versión: 2.8.0

---

## Descripción

Checklist para identificar problemas de rendimiento y optimizar
aplicaciones .NET con Entity Framework Core y SQL Server.

---

## 1. Entity Framework Core

### Consultas

- [ ] `AsNoTracking()` en consultas de solo lectura
- [ ] `Select()` para proyecciones (no cargar entidad completa)
- [ ] `AsSplitQuery()` para evitar explosión cartesiana
- [ ] Paginación en listados (`Skip/Take`)
- [ ] `FirstOrDefaultAsync` en lugar de `SingleOrDefaultAsync` si es posible

```csharp
// ❌ Carga entidad completa
var becas = await _context.Becas.ToListAsync();

// ✅ Solo campos necesarios
var becas = await _context.Becas
    .AsNoTracking()
    .Select(b => new BecaListDto
    {
        Id = b.Id,
        Nombre = b.Nombre,
        Estado = b.Estado
    })
    .ToListAsync();
```

### Eager Loading

- [ ] Include solo lo necesario
- [ ] ThenInclude con cuidado (N+1 queries)
- [ ] Considerar Split Queries para múltiples includes

```csharp
// ⚠️ Puede causar explosión cartesiana
var beca = await _context.Becas
    .Include(b => b.Solicitudes)
        .ThenInclude(s => s.Documentos)
    .Include(b => b.Solicitudes)
        .ThenInclude(s => s.Estudiante)
    .FirstOrDefaultAsync(b => b.Id == id);

// ✅ Mejor con Split Query
var beca = await _context.Becas
    .Include(b => b.Solicitudes)
        .ThenInclude(s => s.Documentos)
    .Include(b => b.Solicitudes)
        .ThenInclude(s => s.Estudiante)
    .AsSplitQuery()
    .FirstOrDefaultAsync(b => b.Id == id);
```

### Compiled Queries

- [ ] Usar para consultas frecuentes
- [ ] Evitar para consultas con parámetros dinámicos

```csharp
// ✅ Compiled Query para consultas frecuentes
private static readonly Func<AppDbContext, int, Task<Beca?>> GetByIdQuery =
    EF.CompileAsyncQuery((AppDbContext ctx, int id) =>
        ctx.Becas.FirstOrDefault(b => b.Id == id));

public async Task<Beca?> GetByIdAsync(int id)
{
    return await GetByIdQuery(_context, id);
}
```

---

## 2. Base de Datos

### Índices

- [ ] Índices en columnas de filtro frecuente
- [ ] Índices en foreign keys
- [ ] Índices compuestos para consultas comunes
- [ ] Evitar índices en columnas poco selectivas

```sql
-- ✅ Índices útiles
CREATE INDEX IX_Beca_Estado ON Becas(Estado);
CREATE INDEX IX_Beca_Estado_FechaFin ON Becas(Estado, FechaFin) WHERE IsDeleted = 0;
CREATE INDEX IX_Solicitud_BecaId ON Solicitudes(BecaId);
```

### Query Analysis

- [ ] Revisar query plan de consultas lentas
- [ ] Evitar Table Scans en tablas grandes
- [ ] Verificar que los índices se usan

```sql
-- Ver plan de ejecución
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

SELECT * FROM Becas WHERE Estado = 'Publicada';

-- Ver índices no usados
SELECT * FROM sys.dm_db_index_usage_stats
WHERE database_id = DB_ID() AND user_seeks = 0 AND user_scans = 0;
```

### Stored Procedures

- [ ] Usar SET NOCOUNT ON
- [ ] Parámetros tipados correctamente
- [ ] Sin SELECT * en producción

---

## 3. Async/Await

### Buenas Prácticas

- [ ] Todo I/O es async (DB, HTTP, archivos)
- [ ] No mezclar sync y async (evitar .Result, .Wait())
- [ ] CancellationToken propagado
- [ ] ConfigureAwait(false) en librerías

```csharp
// ❌ Bloquea thread
var result = _service.GetByIdAsync(id).Result;

// ✅ Completamente async
var result = await _service.GetByIdAsync(id, cancellationToken);
```

### Paralelización

- [ ] `Task.WhenAll` para operaciones independientes
- [ ] `Parallel.ForEachAsync` para procesamiento paralelo
- [ ] Limitar grado de paralelismo

```csharp
// ✅ Operaciones paralelas
var task1 = _service1.GetDataAsync();
var task2 = _service2.GetDataAsync();
var task3 = _service3.GetDataAsync();

await Task.WhenAll(task1, task2, task3);

// ✅ Procesamiento paralelo con límite
await Parallel.ForEachAsync(
    items,
    new ParallelOptions { MaxDegreeOfParallelism = 4 },
    async (item, ct) => await ProcessAsync(item, ct));
```

---

## 4. Caché

### Estrategias

- [ ] IMemoryCache para datos locales frecuentes
- [ ] IDistributedCache (Redis) para caché compartida
- [ ] Response caching para respuestas HTTP
- [ ] Output caching (.NET 10)

```csharp
// ✅ Memory Cache con expiration
public async Task<List<BecaDto>> GetBecasActivasAsync()
{
    return await _cache.GetOrCreateAsync(
        "becas_activas",
        async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await _repository.GetActivasAsync();
        });
}

// ✅ Distributed Cache
public async Task<BecaDto?> GetByIdAsync(int id)
{
    var cacheKey = $"beca:{id}";
    var cached = await _distributedCache.GetStringAsync(cacheKey);

    if (cached != null)
        return JsonSerializer.Deserialize<BecaDto>(cached);

    var beca = await _repository.GetByIdAsync(id);
    if (beca != null)
    {
        await _distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(beca),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
    }

    return beca;
}
```

### Invalidación

- [ ] Estrategia de invalidación definida
- [ ] Cache tags para invalidación en grupo
- [ ] No cachear datos sensibles

---

## 5. HTTP y APIs

### Configuración

- [ ] Connection pooling habilitado
- [ ] Timeouts configurados
- [ ] Retry policies con Polly
- [ ] HttpClientFactory en lugar de new HttpClient

```csharp
// ✅ HttpClientFactory con retry
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri("https://api.externa.com");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddTransientHttpErrorPolicy(p =>
    p.WaitAndRetryAsync(3, retryAttempt =>
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

### Response

- [ ] Compresión habilitada (Gzip, Brotli)
- [ ] Paginación en listados
- [ ] Campos parciales (GraphQL-like)

---

## 6. Memory Management

### Evitar Leaks

- [ ] Dispose de IDisposable
- [ ] using para recursos
- [ ] Evitar closures que capturen objetos grandes
- [ ] WeakReference para cachés

```csharp
// ✅ using para dispose automático
await using var stream = await response.Content.ReadAsStreamAsync();

// ✅ Dispose explícito en servicios
public class MyService : IDisposable
{
    private readonly HttpClient _client;

    public void Dispose()
    {
        _client?.Dispose();
    }
}
```

### Large Object Heap

- [ ] Evitar arrays > 85KB
- [ ] ArrayPool para arrays temporales
- [ ] Streaming para archivos grandes

```csharp
// ✅ ArrayPool para evitar LOH allocations
var buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    // usar buffer
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

---

## 7. Logging

### Rendimiento

- [ ] Logging estructurado (no concatenar strings)
- [ ] Nivel apropiado (no Debug en producción)
- [ ] Async sinks para logs
- [ ] Sampling en alta carga

```csharp
// ❌ Concatenación de strings
_logger.LogInformation("Procesando beca " + id + " con importe " + importe);

// ✅ Structured logging
_logger.LogInformation("Procesando beca {BecaId} con importe {Importe}", id, importe);
```

---

## 8. Métricas a Monitorizar

### Application Insights

- [ ] Request duration
- [ ] Dependency duration
- [ ] Exception rate
- [ ] Memory usage

### Umbrales Sugeridos

| Métrica | Aceptable | Revisar | Crítico |
|---------|-----------|---------|---------|
| Response Time P95 | < 200ms | 200-500ms | > 500ms |
| DB Query Time | < 50ms | 50-200ms | > 200ms |
| Error Rate | < 0.1% | 0.1-1% | > 1% |
| Memory Growth | Stable | 10%/hour | > 20%/hour |

---

## 9. Checklist Rápido

### Queries

- [ ] ¿Se usa AsNoTracking?
- [ ] ¿Se proyecta solo lo necesario?
- [ ] ¿Hay paginación?
- [ ] ¿Los índices están creados?

### Async

- [ ] ¿Todo I/O es async?
- [ ] ¿Se propaga CancellationToken?
- [ ] ¿Se usa Task.WhenAll para paralelo?

### Caché

- [ ] ¿Se cachean datos frecuentes?
- [ ] ¿La invalidación está definida?
- [ ] ¿El TTL es apropiado?

### HTTP

- [ ] ¿Se usa HttpClientFactory?
- [ ] ¿Hay retry policies?
- [ ] ¿Compresión habilitada?

---

## Resultado de la Revisión

| Área | Cumple | Parcial | No Cumple |
|------|--------|---------|-----------|
| Entity Framework | ☐ | ☐ | ☐ |
| Base de Datos | ☐ | ☐ | ☐ |
| Async/Await | ☐ | ☐ | ☐ |
| Caché | ☐ | ☐ | ☐ |
| HTTP | ☐ | ☐ | ☐ |
| Memory | ☐ | ☐ | ☐ |
| Logging | ☐ | ☐ | ☐ |

---

**Fecha de revisión:** _______________
**Revisado por:** _______________
**Proyecto:** _______________

---

*Checklist v1.0 - analisis-arquitectura skill*
