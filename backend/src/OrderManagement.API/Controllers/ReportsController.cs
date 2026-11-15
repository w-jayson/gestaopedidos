using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Services;

namespace OrderManagement.API.Controllers;

// ReportsController (Controller de relatórios): expõe relatórios analíticos de vendas.
[ApiController]
[Route("relatorios")]
public class ReportsController : ControllerBase
{
    // _orderService (serviço de pedidos): valida o período e executa a consulta do ranking.
    private readonly OrderService _orderService;

    // Construtor com injeção do serviço de pedidos.
    public ReportsController(OrderService orderService)
    {
        _orderService = orderService;
    }

    // GetTopCustomers (Obter Top 5 clientes - GET /relatorios/top-clientes):
    // retorna até 5 clientes que mais compraram no período informado.
    [HttpGet("top-clientes")]
    [ProducesResponseType(typeof(IReadOnlyList<TopCustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TopCustomerResponse>>> GetTopCustomers(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        CancellationToken cancellationToken = default)
    {
        // query (consulta): encapsula as datas opcionais de início e fim do período.
        var query = new TopCustomersQuery(startDate, endDate);
        var topCustomers = await _orderService.GetTopCustomersAsync(query, cancellationToken);

        return Ok(topCustomers);
    }
}
