using Microsoft.AspNetCore.Mvc;
using Order.Api.DTOs;
using Order.Core.Services;
using System.Net;
using Order.Core.Logging;
using Microsoft.Extensions.Logging;



namespace Order.Api.Controllers;

/// <summary>
/// CRUD básico de pedidos.
/// </summary>
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
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["component"] = "API",
            ["event"] = "OrderCreated",
            ["orderId"] = correlationId,
            ["correlationId"] = correlationId,
            [Correlation.Key] = correlationId,
        }))
        { }
        _logger.LogInformation(
                "Order created by {Cliente} for {Produto} value {Valor}",
                request.ClienteNome, request.Produto, request.Valor
        );

        Response.Headers["X-Correlation-Id"] = correlationId;
        
        var response = OrderResponse.FromDomain(created);

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response
        );
    }

    /// <summary>
    /// Lista todos os pedidos ordenados pela data de criação (mais recente primeiro).
    /// </summary>
    /// <response code="200">Lista encontrada</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var orders = await _orderService.GetAllAsync(ct);

        var response = orders
            .Select(OrderResponse.FromDomain)
            .ToList();

        return Ok(response);
    }


    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await _orderService.GetByIdAsync(id, ct);
        if (order is null)
            return NotFound();

        var history = await _orderService.GetHistoryByOrderIdAsync(id, ct);

        var dto = OrderDetailsMapper.ToResponse(order, history);
        return Ok(dto);
    }
}
