using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Interfaces;

// IOrderCommandRepository (Interface do repositório de escrita de pedidos):
// define as operações usadas na criação transacional de pedidos e baixa de estoque.
public interface IOrderCommandRepository
{
    // GetCustomerByIdAsync (Buscar cliente por Id): verifica e retorna o cliente pelo seu identificador.
    Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default);

    // GetProductsByIdsAsync (Buscar produtos por Ids): carrega os produtos necessários para validar preço e estoque.
    Task<IReadOnlyList<Product>> GetProductsByIdsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default);

    // SaveOrderWithStockUpdateAsync (Salvar pedido com atualização de estoque):
    // persiste o pedido e decrementa o estoque dos produtos dentro da mesma transação no banco.
    Task<Order> SaveOrderWithStockUpdateAsync(
        Order order,
        IReadOnlyCollection<(int ProductId, int QuantityToDecrease)> stockDecreases,
        CancellationToken cancellationToken = default);
}
