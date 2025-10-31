using System.Text.Json;
using Order.Core.Application.Abstractions.Messaging.Outbox;

namespace Order.Infrastructure.Messaging.Outbox;

public class SystemTextJsonEventSerializer : IEventSerializer
{
    private readonly JsonSerializerOptions _options;

    public SystemTextJsonEventSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public string Serialize(IIntegrationEvent @event)
    {
        return JsonSerializer.Serialize(@event, @event.GetType(), _options);
    }

    public IIntegrationEvent Deserialize(string payload, string type)
    {
        var targetType = ResolveType(type)
            ?? throw new InvalidOperationException($"Unable to resolve event type '{type}'. Provide the full type name or assembly-qualified name.");

        var obj = (IIntegrationEvent?)JsonSerializer.Deserialize(payload, targetType, _options);
        return obj ?? throw new InvalidOperationException($"Deserialization returned null for type '{type}'.");
    }

    private static Type? ResolveType(string typeName)
    {
        var t = Type.GetType(typeName, throwOnError: false);
        if (t != null) return t;
        
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            t = asm.GetType(typeName, throwOnError: false, ignoreCase: false);
            if (t != null) return t;
        }

        return null;
    }
}
