using OrderManagement.Application.Contracts;

namespace OrderManagement.Application.Interfaces;

// ICatalogReadRepository (Interface do repositório de leitura de catálogo):
// fornece listas simples de clientes e produtos para alimentar os selects do frontend.
public interface ICatalogReadRepository
{
    // GetCustomersAsync (Listar clientes): retorna todos os clientes ordenados por nome.
    Task<IReadOnlyList<CustomerResponse>> GetCustomersAsync(CancellationToken cancellationToken = default);

    // GetProductsAsync (Listar produtos): retorna todos os produtos com preço e estoque atual ordenados por nome.
    Task<IReadOnlyList<ProductResponse>> GetProductsAsync(CancellationToken cancellationToken = default);
}
