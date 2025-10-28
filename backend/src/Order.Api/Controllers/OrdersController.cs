using Microsoft.AspNetCore.Mvc;
using Order.Api.DTOs;
using Order.Core.Services;
using System.Net;

namespace Order.Api.Controllers;

/// <summary>
/// CRUD básico de pedidos.
/// </summary>
[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Cria um novo pedido.
    /// </summary>
    /// <remarks>
    /// - Status inicial sempre será "Pendente".  
    /// - Após criar, o pedido será enviado para processamento via Service Bus (futuro).
    /// </remarks>
    /// <param name="request">Dados de criação do pedido</param>
    /// <response code="201">Pedido criado com sucesso</response>
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

        var response = OrderResponse.FromDomain(created);

        // retorna 201 + Location header (/orders/{id})
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

    /// <summary>
    /// Busca um pedido pelo seu ID.
    /// </summary>
    /// <param name="id">Id do pedido</param>
    /// <response code="200">Pedido encontrado</response>
    /// <response code="404">Pedido não existe</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await _orderService.GetByIdAsync(id, ct);

        if (order is null)
            return NotFound();

        return Ok(OrderResponse.FromDomain(order));
    }
}
