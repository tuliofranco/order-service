// Order.Core.Application/Orders/GetDetails/GetOrderDetailsQueryHandler.cs
using MediatR;
using Order.Core.Application.Services;
using Order.Core.Domain.Entities;

namespace Order.Core.Application.Orders.GetDetails;

public sealed class GetOrderDetailsQueryHandler 
    : IRequestHandler<GetOrderDetailsQuery, GetOrderDetailsResult?>
{
    private readonly IOrderService _orderService;

    public GetOrderDetailsQueryHandler(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<GetOrderDetailsResult?> Handle(
        GetOrderDetailsQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Busca a ordem
        var order = await _orderService.GetByIdAsync(request.Id, cancellationToken);
        if (order is null)
        {
            // Controller vai tratar 404 quando receber null
            return null;
        }

        // 2. Busca o histórico
        var history = await _orderService.GetHistoryByOrderIdAsync(request.Id, cancellationToken);

        // 3. Devolve o “pacote” com Order + History
        return new GetOrderDetailsResult(order, history);
    }
}
