namespace Order.Api.Features.Orders.DTOs;

public sealed record OrderStatusHistoryResponse(
    Guid Id,
    Guid OrderId,
    string? FromStatus,
    string ToStatus,
    DateTimeOffset OccurredAt,
    string Source,
    string? CorrelationId
);
