using Order.Core.Domain.Entities.Enums;

namespace Order.Core.Domain.Rules;
public static class OrderStatusTransitionValidator
{
    private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> Allowed =
    new Dictionary<OrderStatus, OrderStatus[]>
    {
        [OrderStatus.Pendente] = new[] { OrderStatus.Processando },
        [OrderStatus.Processando] = new[] { OrderStatus.Finalizado },
        [OrderStatus.Finalizado] = Array.Empty<OrderStatus>()
    };
    private static readonly OrderStatus[] InitialAllowed = new[]
    {
        OrderStatus.Pendente
    };
    public static bool IsValid(OrderStatus? fromStatus, OrderStatus toStatus)
    {
        if (fromStatus is null)
            return Array.Exists(InitialAllowed, s => s == toStatus);

        return Allowed.TryGetValue(fromStatus.Value, out var next)
            && Array.Exists(next, s => s == toStatus);
    }

    public static void EnsureValid(OrderStatus? fromStatus, OrderStatus toStatus)
    {
        if (IsValid(fromStatus, toStatus)) return;

        var expected = fromStatus is null
            ? string.Join(", ", InitialAllowed)
            : (Allowed.TryGetValue(fromStatus.Value, out var next)
                ? string.Join(", ", next)
                : "<nenhum>");

        throw new InvalidOperationException(
            $"Transição de status inválida: '{fromStatus?.ToString() ?? "null"}' → '{toStatus}'. Esperado: [{expected}].");
    }
}
