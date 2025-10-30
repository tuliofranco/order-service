namespace Order.Api.DTOs;

public sealed record OrderDetailsResponse(
    Guid Id,
    string ClienteNome,
    string Produto,
    decimal Valor,
    string Status,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<OrderStatusHistoryResponse> History
);
