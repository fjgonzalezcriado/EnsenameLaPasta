# Managed Identity en Azure

> Skill: azure-config
> Versión: 2.8.0

---

## Descripción

Configuración de Managed Identity para acceso seguro a servicios Azure
sin credenciales hardcodeadas. Reemplaza connection strings y secrets.

---

## Tipos de Managed Identity

| Tipo | Descripción | Uso |
|------|-------------|-----|
| **System-assigned** | Creada automáticamente con el recurso | App Services, VMs |
| **User-assigned** | Creada independientemente, reutilizable | Múltiples recursos |

---

## Configuración en Program.cs (.NET 10)

```csharp
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════
// AZURE KEY VAULT CON MANAGED IDENTITY
// ═══════════════════════════════════════════════════════════════════════════

var keyVaultUrl = builder.Configuration["KeyVault:Url"];

if (!string.IsNullOrEmpty(keyVaultUrl))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            // Excluir credenciales no necesarias para mejorar rendimiento
            ExcludeEnvironmentCredential = false,  // CI/CD
            ExcludeManagedIdentityCredential = false, // Azure
            ExcludeVisualStudioCredential = false,  // Local
            ExcludeAzureCliCredential = false,      // Local
            ExcludeAzurePowerShellCredential = true,
            ExcludeInteractiveBrowserCredential = true
        }));
}

// ═══════════════════════════════════════════════════════════════════════════
// SQL SERVER CON MANAGED IDENTITY
// ═══════════════════════════════════════════════════════════════════════════

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // La conexión usa Managed Identity si el connection string incluye:
        // Authentication=Active Directory Managed Identity
        sqlOptions.EnableRetryOnFailure(3);
    });
});

// ═══════════════════════════════════════════════════════════════════════════
// AZURE BLOB STORAGE CON MANAGED IDENTITY
// ═══════════════════════════════════════════════════════════════════════════

builder.Services.AddSingleton(sp =>
{
    var blobUrl = builder.Configuration["Azure:BlobStorage:Url"];
    return new BlobServiceClient(
        new Uri(blobUrl!),
        new DefaultAzureCredential());
});

// ═══════════════════════════════════════════════════════════════════════════
// AZURE SERVICE BUS CON MANAGED IDENTITY
// ═══════════════════════════════════════════════════════════════════════════

builder.Services.AddSingleton(sp =>
{
    var serviceBusNamespace = builder.Configuration["Azure:ServiceBus:Namespace"];
    return new ServiceBusClient(
        $"{serviceBusNamespace}.servicebus.windows.net",
        new DefaultAzureCredential());
});
```

---

## Connection Strings con Managed Identity

### SQL Server / Azure SQL

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:miservidor.database.windows.net,1433;Database=MiDb;Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;"
  }
}
```

### Azure Blob Storage

```json
{
  "Azure": {
    "BlobStorage": {
      "Url": "https://micuenta.blob.core.windows.net"
    }
  }
}
```

### Azure Key Vault

```json
{
  "KeyVault": {
    "Url": "https://mi-keyvault.vault.azure.net/"
  }
}
```

---

## Servicio de Blob con Managed Identity

```csharp
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private const string ContainerName = "documentos";

    public AzureBlobStorageService(
        BlobServiceClient blobServiceClient,
        ILogger<AzureBlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

        var blobName = $"{Guid.NewGuid()}/{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: ct);

        _logger.LogInformation("Archivo {FileName} subido a blob {BlobName}", fileName, blobName);

        return blobClient.Uri.ToString();
    }

    public async Task<Stream?> DownloadAsync(string blobUrl, CancellationToken ct = default)
    {
        var blobClient = new BlobClient(new Uri(blobUrl), new DefaultAzureCredential());

        if (!await blobClient.ExistsAsync(ct))
            return null;

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken ct = default)
    {
        var blobClient = new BlobClient(new Uri(blobUrl), new DefaultAzureCredential());
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);

        _logger.LogInformation("Blob eliminado: {BlobUrl}", blobUrl);
    }

    public async Task<string> GetSasUrlAsync(
        string blobUrl,
        TimeSpan expiresIn,
        CancellationToken ct = default)
    {
        var blobClient = new BlobClient(new Uri(blobUrl), new DefaultAzureCredential());

        // Usar User Delegation Key (más seguro que account key)
        var userDelegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.Add(expiresIn),
            ct);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = blobClient.BlobContainerName,
            BlobName = blobClient.Name,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasToken = sasBuilder.ToSasQueryParameters(
            userDelegationKey.Value,
            _blobServiceClient.AccountName);

        return $"{blobClient.Uri}?{sasToken}";
    }
}
```

---

## Configuración de Roles en Azure

### Azure SQL Database

```bash
# En Azure Portal o CLI, asignar rol al Managed Identity
az sql server ad-admin create \
  --resource-group mi-rg \
  --server mi-servidor \
  --display-name "MI Admin" \
  --object-id <managed-identity-object-id>

# En SQL Server, crear usuario para la identidad
CREATE USER [nombre-app-service] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [nombre-app-service];
ALTER ROLE db_datawriter ADD MEMBER [nombre-app-service];
```

### Azure Blob Storage

```bash
# Asignar rol "Storage Blob Data Contributor"
az role assignment create \
  --assignee <managed-identity-object-id> \
  --role "Storage Blob Data Contributor" \
  --scope /subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.Storage/storageAccounts/<account>
```

### Azure Key Vault

```bash
# Asignar rol "Key Vault Secrets User"
az role assignment create \
  --assignee <managed-identity-object-id> \
  --role "Key Vault Secrets User" \
  --scope /subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.KeyVault/vaults/<vault>
```

---

## Desarrollo Local

### Usando Azure CLI

```bash
# Login con tu cuenta de Azure
az login

# La DefaultAzureCredential usará tu sesión de Azure CLI
```

### Usando Visual Studio

1. Tools → Options → Azure Service Authentication
2. Seleccionar cuenta de Azure
3. DefaultAzureCredential detectará automáticamente

### Variables de Entorno (CI/CD)

```yaml
# Azure DevOps / GitHub Actions
env:
  AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
  AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
  AZURE_CLIENT_SECRET: ${{ secrets.AZURE_CLIENT_SECRET }}
```

---

## Checklist

- [ ] Managed Identity habilitada en App Service/VM
- [ ] Roles RBAC asignados para cada servicio
- [ ] Connection strings sin credenciales
- [ ] DefaultAzureCredential configurado
- [ ] Desarrollo local funciona con Azure CLI
- [ ] CI/CD usa Service Principal o federated credentials

---

## Packages NuGet

```xml
<ItemGroup>
  <PackageReference Include="Azure.Identity" Version="1.*" />
  <PackageReference Include="Azure.Storage.Blobs" Version="12.*" />
  <PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.*" />
  <PackageReference Include="Azure.Extensions.AspNetCore.Configuration.Secrets" Version="1.*" />
  <PackageReference Include="Microsoft.Data.SqlClient" Version="5.*" />
</ItemGroup>
```

---

*Pattern v1.0 - azure-config skill*
