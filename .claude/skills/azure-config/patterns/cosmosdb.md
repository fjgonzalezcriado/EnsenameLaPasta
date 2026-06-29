# CosmosDB - Configuracion y Patrones

> Skill: azure-config | Version: 3.1.0

Azure Cosmos DB SDK v3 para aplicaciones .NET 10.

---

## Setup

```csharp
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new CosmosClient(config["CosmosDb:Endpoint"],
        new DefaultAzureCredential(),
        new CosmosClientOptions { SerializerOptions = new() { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase } });
});
```

## CRUD Operations

```csharp
public class CosmosRepository<T> where T : class
{
    private readonly Container _container;

    public CosmosRepository(CosmosClient client, string database, string container)
        => _container = client.GetContainer(database, container);

    public async Task<T> GetAsync(string id, string partitionKey)
        => await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));

    public async Task<T> CreateAsync(T item, string partitionKey)
    {
        var response = await _container.CreateItemAsync(item, new PartitionKey(partitionKey));
        return response.Resource;
    }

    public async Task<T> UpsertAsync(T item, string partitionKey)
    {
        var response = await _container.UpsertItemAsync(item, new PartitionKey(partitionKey));
        return response.Resource;
    }

    public async Task DeleteAsync(string id, string partitionKey)
        => await _container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey));
}
```

## Partition Key Strategy

| Tipo de datos | Partition Key | Razon |
|---------------|---------------|-------|
| Usuarios | /tenantId | Aislamiento por tenant |
| Eventos | /year-month | Distribucion temporal |
| Productos | /categoryId | Queries por categoria |

---

*Pattern v3.1.0*
