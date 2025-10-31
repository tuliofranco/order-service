using Order.Api.Feature.Orders.DTOs;

namespace Order.Api.Feature.Orders.Mapping;

public static class OrderStatusHistoryMapper
{
    public static OrderStatusHistoryResponse ToResponse(
        Order.Core.Domain.Entities.OrderStatusHistory h)
    {
        return new OrderStatusHistoryResponse(
            Id: h.Id,
            OrderId: h.OrderId,
            FromStatus: h.FromStatus?.ToString(),
            ToStatus: h.ToStatus.ToString(),
            OccurredAt: h.OccurredAt,
            Source: h.Source,
            CorrelationId: h.CorrelationId
        );
    }
}
