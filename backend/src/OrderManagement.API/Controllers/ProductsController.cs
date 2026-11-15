using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Interfaces;

namespace OrderManagement.API.Controllers;

// ProductsController (Controller de produtos): fornece os produtos com preço e estoque atual para o frontend.
[ApiController]
[Route("produtos")]
public class ProductsController : ControllerBase
{
    // _catalogReadRepository (repositório de leitura de catálogo): consulta produtos via Dapper.
    private readonly ICatalogReadRepository _catalogReadRepository;

    // Construtor com injeção do repositório de leitura.
    public ProductsController(ICatalogReadRepository catalogReadRepository)
    {
        _catalogReadRepository = catalogReadRepository;
    }

    // GetProducts (Listar produtos - GET /produtos): retorna produtos ordenados alfabeticamente.
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetProducts(
        CancellationToken cancellationToken = default)
    {
        var products = await _catalogReadRepository.GetProductsAsync(cancellationToken);
        return Ok(products);
    }
}
