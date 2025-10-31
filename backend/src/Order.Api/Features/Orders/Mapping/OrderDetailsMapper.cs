using Order.Api.Features.Orders.DTOs;
using OrderEntity = Order.Core.Domain.Entities;

namespace Order.Api.Features.Orders.Mapping;

public static class OrderDetailsMapper
{
    public static OrderDetailsResponse ToResponse(
        OrderEntity.Order order,
        IReadOnlyList<OrderEntity.OrderStatusHistory> history)
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
