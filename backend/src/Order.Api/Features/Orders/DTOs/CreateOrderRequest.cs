namespace Order.Api.Features.Orders.DTOs;

public class CreateOrderRequest
{
    public string ClienteNome { get; set; } = string.Empty;
    public string Produto { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}
