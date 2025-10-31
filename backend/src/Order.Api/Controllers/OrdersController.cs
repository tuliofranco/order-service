using Microsoft.AspNetCore.Mvc;
using Order.Api.Features.Orders.DTOs;
using Order.Api.Features.Orders.Mapping;
using Order.Core.Application.Services;

namespace Order.Api.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var created = await _orderService.CreateOrderAsync(
            request.ClienteNome,
            request.Produto,
            request.Valor,
            ct
        );

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
        var orders = await _orderService.GetAllAsync(ct);
        var response = orders.Select(OrderResponse.FromDomain).ToList();

        _logger.LogInformation("GetAll {Count} orders", response.Count);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await _orderService.GetByIdAsync(id, ct);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found", id);
            return NotFound();
        }

        var history = await _orderService.GetHistoryByOrderIdAsync(id, ct);

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["orderId"] = id
        }))
        {
            _logger.LogInformation("GetById historyCount={Count}", history.Count);
        }

        var dto = OrderDetailsMapper.ToResponse(order, history);
        Response.Headers["X-Correlation-Id"] = id.ToString();
        return Ok(dto);
    }
}
