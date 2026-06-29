---
name: azure-config
description: >
  Configures Azure services for .NET applications: Key Vault secrets
  management, Blob Storage file handling, Redis distributed cache,
  Application Insights telemetry, Managed Identity, Service Bus,
  CosmosDB, and Azure Functions.
  USE FOR: Azure Key Vault setup, Blob Storage configuration,
  Application Insights, Azure AD app registration, configurar Azure,
  Key Vault, Blob Storage, App Insights.
  DO NOT USE FOR: security auditing (use security-audit),
  Azure AD deep audit (use identity-auditor agent),
  RBAC policy design (use rbac-designer agent).
---

# Configuración Azure - Comillas

Este skill ayuda a configurar servicios Azure siguiendo estándares STIC.

---

## Servicios Cubiertos

| Servicio | Uso | Obligatoriedad |
|----------|-----|----------------|
| **Azure Key Vault** | Secretos y certificados | OBLIGATORIO en producción |
| **Azure Blob Storage** | Almacenamiento de archivos | OBLIGATORIO (no usar disco local) |
| **Redis Cache** | Caché distribuida y sesiones | Recomendado |
| **Application Insights** | Telemetría y logging | Recomendado |

---

## 1. Azure Key Vault

### Configuración Program.cs

```csharp
// .NET 10
var builder = WebApplication.CreateBuilder(args);

// Cargar Key Vault en producción
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUrl = builder.Configuration["KeyVault:Url"];
    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUrl),
            new DefaultAzureCredential());
    }
}
```

### appsettings.Production.json

```json
{
  "KeyVault": {
    "Url": "https://kv-comillas-[proyecto].vault.azure.net/"
  }
}
```

### Secretos a Almacenar

| Secreto | Nombre en Key Vault | Ejemplo |
|---------|---------------------|---------|
| Connection string BD | `ConnectionStrings--DefaultConnection` | Server=... |
| API keys externas | `ExternalApi--ApiKey` | sk-... |
| Certificados | `Certificates--[Nombre]` | Base64 |

---

## 2. Azure Blob Storage

### Servicio de Almacenamiento

```csharp
public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string container, CancellationToken ct = default);
    Task<Stream?> DownloadAsync(string blobName, string container, CancellationToken ct = default);
    Task DeleteAsync(string blobName, string container, CancellationToken ct = default);
    Task<bool> ExistsAsync(string blobName, string container, CancellationToken ct = default);
}

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        BlobServiceClient blobServiceClient,
        ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string container,
        CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

        var blobName = $"{Guid.NewGuid()}/{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: ct);

        _logger.LogInformation("Archivo {FileName} subido a {Container}/{BlobName}",
            fileName, container, blobName);

        return blobName;
    }

    public async Task<Stream?> DownloadAsync(
        string blobName,
        string container,
        CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(ct))
            return null;

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct);
        return response.Value.Content;
    }

    public async Task DeleteAsync(
        string blobName,
        string container,
        CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);

        _logger.LogInformation("Archivo {BlobName} eliminado de {Container}",
            blobName, container);
    }

    public async Task<bool> ExistsAsync(
        string blobName,
        string container,
        CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);
        var blobClient = containerClient.GetBlobClient(blobName);

        return await blobClient.ExistsAsync(ct);
    }
}
```

### Registro en DI

```csharp
// Program.cs
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(
        builder.Configuration.GetConnectionString("BlobStorage"));
});

builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();
```

---

## 3. Redis Cache

### Configuración

```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "Comillas_";
});

// Uso como caché distribuida
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
```

### Uso del Cache

```csharp
public class BecaService
{
    private readonly IDistributedCache _cache;
    private readonly IBecaRepository _repository;

    public async Task<BecaDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var cacheKey = $"beca:{id}";

        // Intentar obtener de caché
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<BecaDto>(cached);
        }

        // Obtener de BD
        var beca = await _repository.GetByIdAsync(id, ct);
        if (beca == null) return null;

        var dto = MapToDto(beca);

        // Guardar en caché (5 minutos)
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(dto),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            },
            ct);

        return dto;
    }
}
```

---

## 4. Application Insights

### Configuración

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

// Telemetría personalizada
builder.Services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
```

### Telemetría Personalizada

```csharp
public class CustomTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = "Comillas.MiApp";
        telemetry.Context.GlobalProperties["Environment"] =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
    }
}
```

### Tracking de Eventos

```csharp
public class BecaService
{
    private readonly TelemetryClient _telemetry;

    public async Task<BecaDto> CreateAsync(CreateBecaRequest request, CancellationToken ct)
    {
        // ... crear beca ...

        _telemetry.TrackEvent("BecaCreada", new Dictionary<string, string>
        {
            ["BecaId"] = beca.Id.ToString(),
            ["Nombre"] = beca.Nombre,
            ["Importe"] = beca.Importe.ToString("F2")
        });

        return dto;
    }
}
```

---

## Entorno de Desarrollo (Docker)

### docker-compose.yml

```yaml
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    ports:
      - "1433:1433"
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=YourStrong@Passw0rd
      - MSSQL_PID=Developer
      - MSSQL_COLLATION=SQL_Latin1_General_CP1250_CI_AS

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

  azurite:
    image: mcr.microsoft.com/azure-storage/azurite
    ports:
      - "10000:10000"  # Blob
      - "10001:10001"  # Queue
      - "10002:10002"  # Table
    command: "azurite --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0"

  mailhog:
    image: mailhog/mailhog
    ports:
      - "1025:1025"  # SMTP
      - "8025:8025"  # Web UI
```

### appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MiApp;User=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True",
    "Redis": "localhost:6379",
    "BlobStorage": "UseDevelopmentStorage=true"
  }
}
```

---

## Checklist de Configuración

### Key Vault
- [ ] URL configurada en appsettings.Production.json
- [ ] Managed Identity habilitada
- [ ] Secretos migrados a Key Vault
- [ ] Sin secretos en código

### Blob Storage
- [ ] IBlobStorageService implementado
- [ ] Contenedores creados
- [ ] Sin uso de disco local
- [ ] Cleanup de archivos temporales

### Redis
- [ ] Connection string configurada
- [ ] Sesiones usando Redis
- [ ] Cache de consultas frecuentes
- [ ] TTL configurado

### Application Insights
- [ ] Connection string configurada
- [ ] Eventos personalizados
- [ ] Métricas de negocio
- [ ] Alertas configuradas

---

*Skill azure-config v3.7.0*
