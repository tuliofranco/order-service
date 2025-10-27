using System;

namespace Order.Api.DTOs;

/// <summary>
/// Representa um pedido retornado pela API.
/// </summary>
public class OrderResponse
{
    public Guid Id { get; set; }

    public string ClienteNome { get; set; } = string.Empty;

    public string Produto { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime DataCriacaoUtc { get; set; }

    public static OrderResponse FromDomain(Order.Core.Domain.Entities.Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            ClienteNome = order.ClienteNome,
            Produto = order.Produto,
            Valor = order.Valor,
            Status = order.Status.ToString(),
            DataCriacaoUtc = order.DataCriacaoUtc
        };
    }
}
