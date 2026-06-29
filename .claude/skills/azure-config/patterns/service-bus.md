# Azure Service Bus - Pub/Sub

> Skill: azure-config | Version: 3.1.0

Patron de mensajeria con Azure Service Bus para comunicacion asincrona.

---

## Configuracion

```csharp
builder.Services.AddAzureClients(clients =>
{
    clients.AddServiceBusClient(builder.Configuration.GetConnectionString("ServiceBus"));
});
```

## Publisher

```csharp
public class MessagePublisher : IMessagePublisher
{
    private readonly ServiceBusClient _client;
    public MessagePublisher(ServiceBusClient client) => _client = client;

    public async Task PublishAsync<T>(string topicOrQueue, T message, CancellationToken ct = default)
    {
        var sender = _client.CreateSender(topicOrQueue);
        var body = BinaryData.FromObjectAsJson(message);
        await sender.SendMessageAsync(new ServiceBusMessage(body), ct);
    }

    public async Task PublishBatchAsync<T>(string topicOrQueue, IEnumerable<T> messages, CancellationToken ct = default)
    {
        var sender = _client.CreateSender(topicOrQueue);
        using var batch = await sender.CreateMessageBatchAsync(ct);
        foreach (var msg in messages)
            batch.TryAddMessage(new ServiceBusMessage(BinaryData.FromObjectAsJson(msg)));
        await sender.SendMessagesAsync(batch, ct);
    }
}
```

## Dead Letter Handling

```csharp
var receiver = client.CreateReceiver(queue,
    new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
var messages = await receiver.ReceiveMessagesAsync(maxMessages: 10);
```

---

*Pattern v3.1.0*
