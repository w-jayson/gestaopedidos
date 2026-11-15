using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using OrderManagement.Application.Contracts;
using OrderManagement.IntegrationTests.Fixtures;

namespace OrderManagement.IntegrationTests;

// OrdersApiIntegrationTests (Testes de integração da API de pedidos):
// exercita os endpoints HTTP reais contra um banco SQL Server real gerenciado pelo Testcontainers.
public class OrdersApiIntegrationTests : IClassFixture<SqlServerTestFixture>, IAsyncLifetime
{
    private readonly SqlServerTestFixture _fixture;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public OrdersApiIntegrationTests(SqlServerTestFixture fixture)
    {
        _fixture = fixture;
    }

    // InitializeAsync (Preparar antes de cada teste): restaura os dados do seed e cria um HttpClient limpo.
    public async Task InitializeAsync()
    {
        await _fixture.ResetSeedDataAsync();
        _factory = _fixture.CreateFactory();
        _client = _factory.CreateClient();
    }

    // DisposeAsync (Limpar após cada teste): libera HttpClient e WebApplicationFactory.
    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetCustomersAndProducts_ShouldReturnSeededDataOrderedByName()
    {
        // Act
        var customers = await _client.GetFromJsonAsync<List<CustomerResponse>>("/clientes");
        var products = await _client.GetFromJsonAsync<List<ProductResponse>>("/produtos");

        // Assert
        Assert.NotNull(customers);
        Assert.Equal(8, customers.Count);
        Assert.Equal("Ana Silva", customers[0].Name);

        Assert.NotNull(products);
        Assert.Equal(7, products.Count);
        Assert.Equal("Headset USB", products[0].Name);
    }

    [Fact]
    public async Task CreateOrder_ShouldPersistOrderAndDecreaseStockInSqlServer_WhenRequestIsValid()
    {
        // Arrange: Produto 4 ("Mouse Sem Fio") inicia com Preco = 150.00 e Estoque = 40.
        var request = new CreateOrderRequest(
            CustomerId: 1,
            Items: [new CreateOrderItemRequest(ProductId: 4, Quantity: 3)]);

        // Act
        var response = await _client.PostAsJsonAsync("/pedidos", request);

        // Assert: HTTP 201 Created + cálculo correto no servidor.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdOrder = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(createdOrder);
        Assert.True(createdOrder.Id > 10);
        Assert.Equal("Ana Silva", createdOrder.CustomerName);
        Assert.Equal(450.00m, createdOrder.TotalAmount);
        Assert.Single(createdOrder.Items);
        Assert.Equal(150.00m, createdOrder.Items[0].UnitPrice);

        // Confirma no banco real que o estoque baixou de 40 para 37.
        var products = await _client.GetFromJsonAsync<List<ProductResponse>>("/produtos");
        var mouse = products!.First(product => product.Id == 4);
        Assert.Equal(37, mouse.Stock);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturn409ProblemDetailsAndRollbackStock_WhenConsolidatedDuplicateExceedsStock()
    {
        // Arrange: Produto 6 ("Webcam Full HD") tem Estoque = 2; somando 2 + 1 = 3 deve falhar.
        // Também enviamos Produto 7 ("Mousepad Gamer", Estoque = 50) para provar o rollback completo.
        var request = new CreateOrderRequest(
            CustomerId: 1,
            Items:
            [
                new CreateOrderItemRequest(ProductId: 7, Quantity: 2),
                new CreateOrderItemRequest(ProductId: 6, Quantity: 2),
                new CreateOrderItemRequest(ProductId: 6, Quantity: 1)
            ]);

        // Act
        var response = await _client.PostAsJsonAsync("/pedidos", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);

        // Nenhum dos dois produtos pode ter sofrido alteração de estoque no SQL Server.
        var products = await _client.GetFromJsonAsync<List<ProductResponse>>("/produtos");
        Assert.Equal(2, products!.First(product => product.Id == 6).Stock);
        Assert.Equal(50, products!.First(product => product.Id == 7).Stock);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturn404ProblemDetails_WhenCustomerDoesNotExist()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerId: 9999,
            Items: [new CreateOrderItemRequest(ProductId: 4, Quantity: 1)]);

        // Act
        var response = await _client.PostAsJsonAsync("/pedidos", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturn400ProblemDetails_WhenItemsAreEmpty()
    {
        // Arrange
        var request = new CreateOrderRequest(CustomerId: 1, Items: []);

        // Act
        var response = await _client.PostAsJsonAsync("/pedidos", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task GetOrders_ShouldFilterByCustomerAndPeriodWithPagination()
    {
        // Act 1: Cliente 1 ("Ana Silva") possui 2 pedidos em 2026; com pageSize=1 devem existir 2 páginas.
        var customerPage = await _client.GetFromJsonAsync<PagedResponse<OrderResponse>>(
            "/pedidos?customerId=1&startDate=2026-01-01&endDate=2026-12-31&page=1&pageSize=1");

        // Assert 1
        Assert.NotNull(customerPage);
        Assert.Equal(2, customerPage.TotalCount);
        Assert.Equal(2, customerPage.TotalPages);
        Assert.Single(customerPage.Items);
        Assert.Equal(2, customerPage.Items[0].Id); // Pedido 2 (maio/2026) vem antes do Pedido 1 (fev/2026).

        // Act 2: Filtro de 2025 deve retornar apenas os 2 pedidos de 2025 (Ids 9 e 10).
        var orders2025 = await _client.GetFromJsonAsync<PagedResponse<OrderResponse>>(
            "/pedidos?startDate=2025-01-01&endDate=2025-12-31");

        // Assert 2
        Assert.NotNull(orders2025);
        Assert.Equal(2, orders2025.TotalCount);
        Assert.All(orders2025.Items, order => Assert.Equal(2025, order.OrderDate.Year));
    }

    [Fact]
    public async Task GetTopCustomers_ShouldReturnTop5OrderedDescendingWithoutMergingHomonyms()
    {
        // Act: consulta o ranking do ano de 2026 (Parte 4).
        var topCustomers = await _client.GetFromJsonAsync<List<TopCustomerResponse>>(
            "/relatorios/top-clientes?startDate=2026-01-01&endDate=2026-12-31");

        // Assert: exatamente 5 clientes em ordem decrescente e sem somar a outra "Ana Silva" (Id 6).
        Assert.NotNull(topCustomers);
        Assert.Equal(5, topCustomers.Count);

        Assert.Equal(1, topCustomers[0].CustomerId);
        Assert.Equal("Ana Silva", topCustomers[0].CustomerName);
        Assert.Equal(6600.00m, topCustomers[0].TotalAmount);
        Assert.Equal(2, topCustomers[0].OrdersCount);

        Assert.Equal(3, topCustomers[1].CustomerId);
        Assert.Equal(5060.00m, topCustomers[1].TotalAmount);

        Assert.Equal(2, topCustomers[2].CustomerId);
        Assert.Equal(4500.00m, topCustomers[2].TotalAmount); // Não inclui o pedido de 2025 do Bruno.

        Assert.Equal(4, topCustomers[3].CustomerId);
        Assert.Equal(2440.00m, topCustomers[3].TotalAmount);

        Assert.Equal(5, topCustomers[4].CustomerId);
        Assert.Equal(1200.00m, topCustomers[4].TotalAmount);
    }
}
