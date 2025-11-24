using MediatR;
using Order.Core.Application.Services;
using OrderEntity = Order.Core.Domain.Entities.Order;

namespace Order.Core.Application.Orders.Create;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderEntity>
{
    private readonly IOrderService _orderService;

    public CreateOrderCommandHandler(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<OrderEntity> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        OrderEntity order = await _orderService.CreateOrderAsync(
            request.ClienteNome,
            request.Produto,
            request.Valor,
            cancellationToken
        );
        return order;
    }
}