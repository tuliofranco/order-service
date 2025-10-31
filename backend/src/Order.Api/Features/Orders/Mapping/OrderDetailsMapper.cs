using System.Linq;
using Order.Api.Feature.Orders.DTOs;
namespace Order.Api.Feature.Orders.Mapping;

public static class OrderDetailsMapper
{
    public static OrderDetailsResponse ToResponse(
        Order.Core.Domain.Entities.Order order,
        IReadOnlyList<Order.Core.Domain.Entities.OrderStatusHistory> history)
    {
        return new OrderDetailsResponse(
            Id: order.Id,
            ClienteNome: order.ClienteNome,
            Produto: order.Produto,
            Valor: order.Valor,
            Status: order.Status.ToString(),
            CreatedAtUtc: order.data_criacao,
            History: history
                .OrderBy(h => h.OccurredAt)
                .Select(OrderStatusHistoryMapper.ToResponse)
                .ToList()
        );
    }
}
