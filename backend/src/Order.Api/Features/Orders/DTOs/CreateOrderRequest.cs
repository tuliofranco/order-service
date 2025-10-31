namespace Order.Api.Feature.Orders.DTOs;

/// <summary>
/// Dados necessários para criar um novo pedido.
/// </summary>
public class CreateOrderRequest
{
    /// <example>Maria da Silva</example>
    public string ClienteNome { get; set; } = string.Empty;

    /// <example>Mouse Gamer XYZ</example>
    public string Produto { get; set; } = string.Empty;

    /// <example>199.90</example>
    public decimal Valor { get; set; }
}
