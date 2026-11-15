using OrderManagement.Application.Contracts;
using OrderManagement.Application.Services;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Exceptions;
using OrderManagement.UnitTests.Fakes;

namespace OrderManagement.UnitTests;

// OrderServiceTests (Testes unitários do serviço de pedidos):
// garante que todas as regras de negócio e validações funcionem isoladamente em memória.
public class OrderServiceTests
{
    private static (OrderService Service, FakeOrderCommandRepository CommandRepo, FakeOrderReadRepository ReadRepo) CreateSut()
    {
        // SUT (System Under Test - Sistema sob teste): instância real do OrderService com repositórios em memória.
        var commandRepo = new FakeOrderCommandRepository();
        var readRepo = new FakeOrderReadRepository();

        commandRepo.Customers.Add(new Customer
        {
            Id = 1,
            Name = "Ana Silva",
            Email = "ana@email.com"
        });

        commandRepo.Products.AddRange(
        [
            new Product { Id = 10, Name = "Teclado Mecânico", Price = 400.00m, Stock = 10 },
            new Product { Id = 20, Name = "Mouse Sem Fio", Price = 150.00m, Stock = 5 }
        ]);

        var service = new OrderService(commandRepo, readRepo);
        return (service, commandRepo, readRepo);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldCreateOrderAndDecreaseStock_WhenRequestIsValid()
    {
        // Arrange (Preparação)
        var (service, commandRepo, _) = CreateSut();
        var request = new CreateOrderRequest(
            CustomerId: 1,
            Items:
            [
                new CreateOrderItemRequest(ProductId: 10, Quantity: 2),
                new CreateOrderItemRequest(ProductId: 20, Quantity: 1)
            ]);

        // Act (Execução)
        var result = await service.CreateOrderAsync(request);

        // Assert (Verificação): total = (2 * 400) + (1 * 150) = 950 e estoques atualizados.
        Assert.Equal(1, result.Id);
        Assert.Equal("Ana Silva", result.CustomerName);
        Assert.Equal(950.00m, result.TotalAmount);
        Assert.Equal(2, result.Items.Count);
        Assert.True(commandRepo.SaveCalled);
        Assert.Equal(8, commandRepo.Products.First(product => product.Id == 10).Stock);
        Assert.Equal(4, commandRepo.Products.First(product => product.Id == 20).Stock);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldConsolidateDuplicateProducts_WhenSameProductAppearsMultipleTimes()
    {
        // Arrange: o produto 20 aparece duas vezes (2 + 2 = 4 unidades).
        var (service, commandRepo, _) = CreateSut();
        var request = new CreateOrderRequest(
            CustomerId: 1,
            Items:
            [
                new CreateOrderItemRequest(ProductId: 20, Quantity: 2),
                new CreateOrderItemRequest(ProductId: 20, Quantity: 2)
            ]);

        // Act
        var result = await service.CreateOrderAsync(request);

        // Assert: deve virar 1 item consolidado com quantidade 4 e restar 1 unidade em estoque.
        Assert.Single(result.Items);
        Assert.Equal(4, result.Items[0].Quantity);
        Assert.Equal(600.00m, result.TotalAmount);
        Assert.Equal(1, commandRepo.Products.First(product => product.Id == 20).Stock);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldThrowResourceNotFoundException_WhenCustomerDoesNotExist()
    {
        // Arrange
        var (service, commandRepo, _) = CreateSut();
        var request = new CreateOrderRequest(
            CustomerId: 999,
            Items: [new CreateOrderItemRequest(ProductId: 10, Quantity: 1)]);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.CreateOrderAsync(request));
        Assert.False(commandRepo.SaveCalled);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldThrowResourceNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var (service, commandRepo, _) = CreateSut();
        var request = new CreateOrderRequest(
            CustomerId: 1,
            Items: [new CreateOrderItemRequest(ProductId: 999, Quantity: 1)]);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.CreateOrderAsync(request));
        Assert.False(commandRepo.SaveCalled);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldThrowValidationException_WhenItemsListIsNullOrEmpty()
    {
        // Arrange
        var (service, _, _) = CreateSut();
        var requestNullItems = new CreateOrderRequest(CustomerId: 1, Items: null);
        var requestEmptyItems = new CreateOrderRequest(CustomerId: 1, Items: []);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateOrderAsync(requestNullItems));
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateOrderAsync(requestEmptyItems));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task CreateOrderAsync_ShouldThrowValidationException_WhenQuantityIsZeroOrNegative(int invalidQuantity)
    {
        // Arrange
        var (service, _, _) = CreateSut();
        var request = new CreateOrderRequest(
            CustomerId: 1,
            Items: [new CreateOrderItemRequest(ProductId: 10, Quantity: invalidQuantity)]);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateOrderAsync(request));
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldThrowValidationException_WhenCustomerIdIsInvalid()
    {
        // Arrange
        var (service, _, _) = CreateSut();
        var request = new CreateOrderRequest(
            CustomerId: 0,
            Items: [new CreateOrderItemRequest(ProductId: 10, Quantity: 1)]);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateOrderAsync(request));
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldThrowInsufficientStockException_WhenRequestedExceedsStock()
    {
        // Arrange: produto 20 tem apenas 5 unidades em estoque.
        var (service, commandRepo, _) = CreateSut();
        var request = new CreateOrderRequest(
            CustomerId: 1,
            Items: [new CreateOrderItemRequest(ProductId: 20, Quantity: 6)]);

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientStockException>(() => service.CreateOrderAsync(request));
        Assert.False(commandRepo.SaveCalled);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldThrowInsufficientStockException_WhenConsolidatedDuplicateExceedsStock()
    {
        // Arrange: produto 20 tem estoque 5; somando 3 + 3 = 6 deve falhar.
        var (service, commandRepo, _) = CreateSut();
        var request = new CreateOrderRequest(
            CustomerId: 1,
            Items:
            [
                new CreateOrderItemRequest(ProductId: 20, Quantity: 3),
                new CreateOrderItemRequest(ProductId: 20, Quantity: 3)
            ]);

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientStockException>(() => service.CreateOrderAsync(request));
        Assert.False(commandRepo.SaveCalled);
    }

    [Fact]
    public async Task GetOrdersAsync_ShouldThrowValidationException_WhenFiltersOrPaginationAreInvalid()
    {
        // Arrange
        var (service, _, _) = CreateSut();

        // Act & Assert: período invertido, página < 1 e pageSize fora de 1..100.
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetOrdersAsync(new OrderFilterQuery(
                StartDate: new DateOnly(2026, 12, 31),
                EndDate: new DateOnly(2026, 01, 01))));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetOrdersAsync(new OrderFilterQuery(Page: 0)));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetOrdersAsync(new OrderFilterQuery(PageSize: 101)));
    }

    [Fact]
    public async Task GetTopCustomersAsync_ShouldThrowValidationException_WhenStartDateIsAfterEndDate()
    {
        // Arrange
        var (service, _, _) = CreateSut();
        var query = new TopCustomersQuery(
            StartDate: new DateOnly(2026, 06, 01),
            EndDate: new DateOnly(2026, 05, 01));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.GetTopCustomersAsync(query));
    }
}
