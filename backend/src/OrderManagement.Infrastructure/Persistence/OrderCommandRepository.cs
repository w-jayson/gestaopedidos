using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Interfaces;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Infrastructure.Persistence;

// OrderCommandRepository (Repositório de escrita de pedidos):
// implementa as gravações transacionais no SQL Server via Entity Framework Core.
public class OrderCommandRepository : IOrderCommandRepository
{
    // _dbContext (contexto do banco): gerencia consultas de leitura para validação, transação e SaveChanges.
    private readonly AppDbContext _dbContext;

    // Construtor com injeção do AppDbContext.
    public OrderCommandRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // GetCustomerByIdAsync (Buscar cliente por Id): usa AsNoTracking porque é apenas leitura de validação.
    public Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(customer => customer.Id == customerId, cancellationToken);
    }

    // GetProductsByIdsAsync (Buscar produtos por Ids): carrega os produtos sem rastreamento para validar preço/estoque.
    public async Task<IReadOnlyList<Product>> GetProductsByIdsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync(cancellationToken);
    }

    // SaveOrderWithStockUpdateAsync (Salvar pedido com baixa de estoque):
    // abre uma transação explícita, executa baixa atômica de estoque e persiste o pedido com seus itens.
    public async Task<Order> SaveOrderWithStockUpdateAsync(
        Order order,
        IReadOnlyCollection<(int ProductId, int QuantityToDecrease)> stockDecreases,
        CancellationToken cancellationToken = default)
    {
        // transaction (transação): garante "tudo ou nada" — se qualquer etapa falhar, nada é salvo.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Ordenar por ProductId evita deadlocks caso duas requisições concorrentes comprem os mesmos produtos.
        foreach (var (productId, quantityToDecrease) in stockDecreases.OrderBy(item => item.ProductId))
        {
            // Baixa atômica condicional: só decrementa se ainda houver Estoque >= quantityToDecrease no banco.
            // affectedRows (linhas afetadas): se retornar 0, outro pedido concorrente consumiu o estoque antes.
            var affectedRows = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE dbo.Produtos SET Estoque = Estoque - {quantityToDecrease} WHERE Id = {productId} AND Estoque >= {quantityToDecrease}",
                cancellationToken);

            if (affectedRows == 0)
            {
                throw new InsufficientStockException(
                    $"Estoque insuficiente para o produto Id {productId} durante a confirmação do pedido.");
            }
        }

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return order;
    }
}
