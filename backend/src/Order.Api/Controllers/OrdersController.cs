using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Order.Api.Features.Orders.DTOs;
using Order.Api.Features.Orders.Mapping;
using Order.Api.Notification;
using Order.Core.Application.Orders.Create;
using Order.Core.Application.Orders.GetAll;
using Order.Core.Application.Orders.GetDetails;

namespace Order.Api.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;
    private readonly IOrderNotificationService _notifications;

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger, IOrderNotificationService notifications)
    {
        _mediator = mediator;
        _logger = logger;
        _notifications = notifications;
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var created = await _mediator.Send(new CreateOrderCommand(request.ClienteNome, request.Produto, request.Valor), ct);

        var correlationId = created.Id.ToString();
        Response.Headers["X-Correlation-Id"] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["orderId"] = created.Id,
            ["correlationId"] = correlationId
        }))
        {
            _logger.LogInformation("Order created");
        }

        var response = OrderResponse.FromDomain(created);
        await _notifications.NotifyOrderCreatedAsync(response);

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response
        );
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var orders = await _mediator.Send(new GetAllOrdersQuery(), ct);
        var response = orders.Select(OrderResponse.FromDomain).ToList();

        _logger.LogInformation("GetAll {Count} orders", response.Count);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetOrderDetailsQuery(id), ct);

        if (result is null)
        {
            _logger.LogWarning("Order {OrderId} not found", id);
            return NotFound();
        }

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["orderId"] = id
        }))
        {
            _logger.LogInformation("GetById historyCount={Count}", result.History.Count);
        }

        var dto = OrderDetailsMapper.ToResponse(result.Order, result.History);
        Response.Headers["X-Correlation-Id"] = id.ToString();
        return Ok(dto);
    }
}
