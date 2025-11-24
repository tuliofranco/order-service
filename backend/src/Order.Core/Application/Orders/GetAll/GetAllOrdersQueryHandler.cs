using MediatR;
using Order.Core.Application.Services;
using OrderEntity = Order.Core.Domain.Entities.Order;

namespace Order.Core.Application.Orders.GetAll;

public sealed class GetAllOrdersQueryHandler 
    : IRequestHandler<GetAllOrdersQuery, IReadOnlyList<OrderEntity>>
{
    private readonly IOrderService _orderService;

    public GetAllOrdersQueryHandler(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IReadOnlyList<OrderEntity>> Handle(
        GetAllOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetAllAsync(cancellationToken);
        return orders;
    }
}
