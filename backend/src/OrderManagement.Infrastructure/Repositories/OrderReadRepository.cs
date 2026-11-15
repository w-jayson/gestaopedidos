using Dapper;
using OrderManagement.Application.Contracts;
using OrderManagement.Application.Interfaces;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Repositories;

// OrderReadRepository (Repositório de leitura de pedidos):
// executa consultas SQL otimizadas com Dapper para listagem paginada e relatório Top 5 clientes.
public class OrderReadRepository : IOrderReadRepository
{
    // _connectionFactory (fábrica de conexões): fornece conexões SQL Server para o Dapper.
    private readonly SqlConnectionFactory _connectionFactory;

    // Construtor com injeção da fábrica de conexões.
    public OrderReadRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // GetOrdersAsync (Listar pedidos): consulta paginada com filtros sargáveis e carga dos itens sem N+1.
    public async Task<PagedResponse<OrderResponse>> GetOrdersAsync(
        OrderFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        // parameters (parâmetros SQL): evita SQL injection e permite reaproveitamento de plano no SQL Server.
        var parameters = new DynamicParameters();
        var whereClauses = new List<string>();

        if (filter.CustomerId.HasValue)
        {
            whereClauses.Add("p.ClienteId = @CustomerId");
            parameters.Add("CustomerId", filter.CustomerId.Value);
        }

        AddDateRangeFilters(whereClauses, parameters, filter.StartDate, filter.EndDate);

        var whereSql = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : string.Empty;

        using var connection = _connectionFactory.CreateConnection();

        // 1. Conta quantos pedidos atendem aos filtros para calcular o total de páginas.
        var countSql = $"""
            SELECT COUNT(*)
            FROM dbo.Pedidos AS p
            {whereSql};
            """;

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        if (totalCount == 0)
        {
            return new PagedResponse<OrderResponse>([], filter.Page, filter.PageSize, 0, 0);
        }

        // offset (deslocamento): calcula quantos registros pular conforme a página solicitada.
        var offset = (filter.Page - 1) * filter.PageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", filter.PageSize);

        // 2. Busca apenas os cabeçalhos dos pedidos da página atual.
        var ordersSql = $"""
            SELECT
                p.Id AS Id,
                p.ClienteId AS CustomerId,
                c.Nome AS CustomerName,
                p.DataPedido AS OrderDate,
                p.ValorTotal AS TotalAmount
            FROM dbo.Pedidos AS p
            INNER JOIN dbo.Clientes AS c ON c.Id = p.ClienteId
            {whereSql}
            ORDER BY p.DataPedido DESC, p.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var orderRows = (await connection.QueryAsync<OrderHeaderRow>(
            new CommandDefinition(ordersSql, parameters, cancellationToken: cancellationToken))).ToList();

        if (orderRows.Count == 0)
        {
            var totalPagesEmpty = (int)Math.Ceiling(totalCount / (double)filter.PageSize);
            return new PagedResponse<OrderResponse>([], filter.Page, filter.PageSize, totalCount, totalPagesEmpty);
        }

        // 3. Carrega todos os itens dos pedidos exibidos em uma única consulta (evita problema N+1).
        var orderIds = orderRows.Select(order => order.Id).ToArray();

        const string itemsSql = """
            SELECT
                i.PedidoId AS OrderId,
                i.ProdutoId AS ProductId,
                pr.Nome AS ProductName,
                i.Quantidade AS Quantity,
                i.PrecoUnitario AS UnitPrice,
                (i.Quantidade * i.PrecoUnitario) AS Subtotal
            FROM dbo.ItensPedido AS i
            INNER JOIN dbo.Produtos AS pr ON pr.Id = i.ProdutoId
            WHERE i.PedidoId IN @OrderIds
            ORDER BY i.Id ASC;
            """;

        var itemRows = await connection.QueryAsync<OrderItemRow>(
            new CommandDefinition(itemsSql, new { OrderIds = orderIds }, cancellationToken: cancellationToken));

        // itemsByOrderId (itens agrupados por pedido): facilita associar os itens a cada cabeçalho.
        var itemsByOrderId = itemRows
            .GroupBy(item => item.OrderId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<OrderItemResponse>)group
                    .Select(item => new OrderItemResponse(
                        item.ProductId,
                        item.ProductName,
                        item.Quantity,
                        item.UnitPrice,
                        item.Subtotal))
                    .ToList());

        var items = orderRows
            .Select(order => new OrderResponse(
                order.Id,
                order.CustomerId,
                order.CustomerName,
                DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc),
                order.TotalAmount,
                itemsByOrderId.GetValueOrDefault(order.Id, [])))
            .ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

        return new PagedResponse<OrderResponse>(items, filter.Page, filter.PageSize, totalCount, totalPages);
    }

    // GetTopCustomersAsync (Obter Top 5 clientes): implementa a query corrigida e sargável da Parte 4.
    public async Task<IReadOnlyList<TopCustomerResponse>> GetTopCustomersAsync(
        TopCustomersQuery query,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        var whereClauses = new List<string>();

        AddDateRangeFilters(whereClauses, parameters, query.StartDate, query.EndDate);

        var whereSql = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : string.Empty;

        // Query otimizada (Parte 4):
        // - INNER JOIN garante que apenas clientes com pedidos no período entrem no cálculo;
        // - Filtro de data sargável permite Seek no índice IX_Pedidos_DataPedido_ClienteId;
        // - GROUP BY (c.Id, c.Nome) evita juntar clientes homônimos;
        // - ORDER BY TotalAmount DESC + TOP 5 retorna os 5 maiores compradores.
        var sql = $"""
            SELECT TOP 5
                c.Id AS CustomerId,
                c.Nome AS CustomerName,
                COUNT(DISTINCT p.Id) AS OrdersCount,
                SUM(i.Quantidade * i.PrecoUnitario) AS TotalAmount
            FROM dbo.Clientes AS c
            INNER JOIN dbo.Pedidos AS p ON p.ClienteId = c.Id
            INNER JOIN dbo.ItensPedido AS i ON i.PedidoId = p.Id
            {whereSql}
            GROUP BY c.Id, c.Nome
            ORDER BY TotalAmount DESC, c.Id ASC;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<TopCustomerResponse>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return rows.ToList();
    }

    // AddDateRangeFilters (Adicionar filtros de período): adiciona limites sargáveis sem usar funções na coluna DataPedido.
    private static void AddDateRangeFilters(
        List<string> whereClauses,
        DynamicParameters parameters,
        DateOnly? startDate,
        DateOnly? endDate)
    {
        if (startDate.HasValue)
        {
            whereClauses.Add("p.DataPedido >= @StartDate");
            parameters.Add("StartDate", startDate.Value.ToDateTime(TimeOnly.MinValue));
        }

        if (endDate.HasValue)
        {
            // Usa "< dia seguinte 00:00:00" para incluir todo o último dia mantendo o uso do índice.
            whereClauses.Add("p.DataPedido < @EndDateExclusive");
            parameters.Add("EndDateExclusive", endDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        }
    }

    // OrderHeaderRow (Linha de cabeçalho do pedido): tipo interno auxiliar para materialização do Dapper.
    private sealed record OrderHeaderRow(
        int Id,
        int CustomerId,
        string CustomerName,
        DateTime OrderDate,
        decimal TotalAmount);

    // OrderItemRow (Linha de item do pedido): tipo interno auxiliar para materialização do Dapper.
    private sealed record OrderItemRow(
        int OrderId,
        int ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal Subtotal);
}
