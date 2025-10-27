using OrderService.Core.Enums;

namespace OrderService.Core.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }

    public string ClienteNome { get; set; } = string.Empty;

    public string Produto { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime DataCriacaoUtc { get; set; }

    public static Order Create(string clienteNome, string produto, decimal valor)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            ClienteNome = clienteNome,
            Produto = produto,
            Valor = valor,
            Status = OrderStatus.Pendente,
            DataCriacaoUtc = DateTime.UtcNow
        };
    }
}
