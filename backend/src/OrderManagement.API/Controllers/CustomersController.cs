using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Interfaces;

namespace OrderManagement.API.Controllers;

// CustomersController (Controller de clientes): fornece a lista de clientes para o frontend.
[ApiController]
[Route("clientes")]
public class CustomersController : ControllerBase
{
    // _catalogReadRepository (repositório de leitura de catálogo): consulta clientes via Dapper.
    private readonly ICatalogReadRepository _catalogReadRepository;

    // Construtor com injeção do repositório de leitura.
    public CustomersController(ICatalogReadRepository catalogReadRepository)
    {
        _catalogReadRepository = catalogReadRepository;
    }

    // GetCustomers (Listar clientes - GET /clientes): retorna clientes ordenados alfabeticamente.
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> GetCustomers(
        CancellationToken cancellationToken = default)
    {
        var customers = await _catalogReadRepository.GetCustomersAsync(cancellationToken);
        return Ok(customers);
    }
}
