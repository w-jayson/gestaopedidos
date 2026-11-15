using OrderManagement.Application.Contracts;

namespace OrderManagement.Application.Interfaces;

// IOrderReadRepository (Interface do repositório de leitura de pedidos):
// define as consultas otimizadas (Dapper) para listagem paginada e relatório Top 5.
public interface IOrderReadRepository
{
    // GetOrdersAsync (Listar pedidos): retorna uma página de pedidos aplicando filtros opcionais de cliente e período.
    Task<PagedResponse<OrderResponse>> GetOrdersAsync(
        OrderFilterQuery filter,
        CancellationToken cancellationToken = default);

    // GetTopCustomersAsync (Obter Top 5 clientes): retorna até 5 clientes com maior valor total de compras no período.
    Task<IReadOnlyList<TopCustomerResponse>> GetTopCustomersAsync(
        TopCustomersQuery query,
        CancellationToken cancellationToken = default);
}
