using System;
using OrderEntity = Order.Core.Domain.Entities.Order;
namespace Order.Api.Features.Orders.DTOs;

public class OrderResponse
{
    public Guid Id { get; set; }

    public string ClienteNome { get; set; } = string.Empty;

    public string Produto { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime data_criacao { get; set; }

    public static OrderResponse FromDomain(OrderEntity order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            ClienteNome = order.ClienteNome,
            Produto = order.Produto,
            Valor = order.Valor,
            Status = order.Status.ToString(),
            data_criacao = order.data_criacao
        };
    }
}
