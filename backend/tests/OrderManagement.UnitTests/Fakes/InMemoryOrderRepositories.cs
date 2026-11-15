using OrderManagement.Application.Contracts;
using OrderManagement.Application.Interfaces;
using OrderManagement.Domain.Entities;

namespace OrderManagement.UnitTests.Fakes;

// FakeOrderCommandRepository (Repositório falso de escrita em memória):
// simula o banco de dados em memória para testar regras do OrderService sem precisar de SQL Server.
public class FakeOrderCommandRepository : IOrderCommandRepository
{
    // Customers (Clientes): lista em memória representando a tabela Clientes.
    public List<Customer> Customers { get; } = [];

    // Products (Produtos): lista em memória representando a tabela Produtos.
    public List<Product> Products { get; } = [];

    // SavedOrders (Pedidos salvos): armazena pedidos gravados com sucesso durante o teste.
    public List<Order> SavedOrders { get; } = [];

    // SaveCalled (Salvamento chamado): indica se o serviço chegou a persistir no repositório.
    public bool SaveCalled { get; private set; }

    // _nextOrderId (próximo Id de pedido): simula o IDENTITY autoincremental do SQL Server.
    private int _nextOrderId = 1;

    public Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var customer = Customers.FirstOrDefault(item => item.Id == customerId);
        return Task.FromResult(customer);
    }

    public Task<IReadOnlyList<Product>> GetProductsByIdsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default)
    {
        // Cria cópias simples para simular AsNoTracking do EF Core.
        IReadOnlyList<Product> result = Products
            .Where(product => productIds.Contains(product.Id))
            .Select(product => new Product
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Stock = product.Stock
            })
            .ToList();

        return Task.FromResult(result);
    }

    public Task<Order> SaveOrderWithStockUpdateAsync(
        Order order,
        IReadOnlyCollection<(int ProductId, int QuantityToDecrease)> stockDecreases,
        CancellationToken cancellationToken = default)
    {
        SaveCalled = true;

        foreach (var (productId, quantityToDecrease) in stockDecreases)
        {
            var product = Products.First(item => item.Id == productId);
            product.DecreaseStock(quantityToDecrease);
        }

        order.Id = _nextOrderId++;
        SavedOrders.Add(order);

        return Task.FromResult(order);
    }
}

// FakeOrderReadRepository (Repositório falso de leitura em memória):
// registra os filtros recebidos para validar se OrderService valida parâmetros antes de consultar.
public class FakeOrderReadRepository : IOrderReadRepository
{
    public OrderFilterQuery? LastOrderFilter { get; private set; }
    public TopCustomersQuery? LastTopCustomersQuery { get; private set; }

    public Task<PagedResponse<OrderResponse>> GetOrdersAsync(
        OrderFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        LastOrderFilter = filter;
        return Task.FromResult(new PagedResponse<OrderResponse>([], filter.Page, filter.PageSize, 0, 0));
    }

    public Task<IReadOnlyList<TopCustomerResponse>> GetTopCustomersAsync(
        TopCustomersQuery query,
        CancellationToken cancellationToken = default)
    {
        LastTopCustomersQuery = query;
        return Task.FromResult<IReadOnlyList<TopCustomerResponse>>([]);
    }
}
