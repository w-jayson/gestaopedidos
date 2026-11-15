using Dapper;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Interfaces;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Repositories;

// CatalogReadRepository (Repositório de leitura de catálogo):
// consulta clientes e produtos com Dapper para alimentar os filtros e formulários do frontend.
public class CatalogReadRepository : ICatalogReadRepository
{
    // _connectionFactory (fábrica de conexões): fornece conexões SQL Server para o Dapper.
    private readonly SqlConnectionFactory _connectionFactory;

    // Construtor com injeção da fábrica de conexões.
    public CatalogReadRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // GetCustomersAsync (Listar clientes): retorna todos os clientes ordenados alfabeticamente.
    public async Task<IReadOnlyList<CustomerResponse>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Id AS Id,
                Nome AS Name,
                Email AS Email
            FROM dbo.Clientes
            ORDER BY Nome ASC, Id ASC;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var customers = await connection.QueryAsync<CustomerResponse>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return customers.ToList();
    }

    // GetProductsAsync (Listar produtos): retorna todos os produtos com preço e estoque atual ordenados por nome.
    public async Task<IReadOnlyList<ProductResponse>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Id AS Id,
                Nome AS Name,
                Preco AS Price,
                Estoque AS Stock
            FROM dbo.Produtos
            ORDER BY Nome ASC, Id ASC;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var products = await connection.QueryAsync<ProductResponse>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return products.ToList();
    }
}
