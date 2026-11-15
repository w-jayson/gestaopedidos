using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Services;

namespace OrderManagement.API.Controllers;

// OrdersController (Controller de pedidos): expõe os endpoints de criação e listagem paginada de pedidos.
[ApiController]
[Route("pedidos")]
public class OrdersController : ControllerBase
{
    // _orderService (serviço de pedidos): contém as regras de negócio de criação e consulta.
    private readonly OrderService _orderService;

    // Construtor com injeção do serviço de pedidos.
    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    // CreateOrder (Criar pedido - POST /pedidos): valida cliente/estoque e grava um novo pedido com seus itens.
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        // createdOrder (pedido criado): resposta com dados persistidos e total calculado no servidor.
        var createdOrder = await _orderService.CreateOrderAsync(request, cancellationToken);

        return Created($"/pedidos/{createdOrder.Id}", createdOrder);
    }

    // GetOrders (Listar pedidos - GET /pedidos): retorna pedidos paginados com filtros opcionais de cliente e período.
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<OrderResponse>>> GetOrders(
        [FromQuery] int? customerId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        // filter (filtro): agrupa os parâmetros recebidos via query string.
        var filter = new OrderFilterQuery(customerId, startDate, endDate, page, pageSize);
        var pagedOrders = await _orderService.GetOrdersAsync(filter, cancellationToken);

        return Ok(pagedOrders);
    }
}
