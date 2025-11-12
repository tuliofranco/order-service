using Order.Core.Domain.Entities.Enums;

namespace Order.Core.Domain.Entities;
public class Order
{
    public Guid Id { get; set; }

    public string ClienteNome { get; set; } = string.Empty;

    public string Produto { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime data_criacao { get; set; }
    public DateTime? data_de_efetivacao { get; set; }

    public static Order Create(string clienteNome, string produto, decimal valor)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            ClienteNome = clienteNome,
            Produto = produto,
            Valor = valor,
            Status = OrderStatus.Pendente,
            data_criacao = DateTime.UtcNow
        };
    }
}
