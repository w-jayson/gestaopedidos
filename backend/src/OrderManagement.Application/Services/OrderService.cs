using OrderManagement.Application.Contracts;
using OrderManagement.Application.Interfaces;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Application.Services;

// OrderService (Serviço de pedidos): centraliza as regras de negócio de criação, consulta e relatório de pedidos.
public class OrderService
{
    // _commandRepository (repositório de escrita): usado para validar cliente/produtos e salvar pedido + baixa de estoque.
    private readonly IOrderCommandRepository _commandRepository;

    // _readRepository (repositório de leitura): usado para consultas paginadas e relatório Top 5 clientes.
    private readonly IOrderReadRepository _readRepository;

    // Construtor com injeção das dependências de escrita e leitura.
    public OrderService(
        IOrderCommandRepository commandRepository,
        IOrderReadRepository readRepository)
    {
        _commandRepository = commandRepository;
        _readRepository = readRepository;
    }

    // CreateOrderAsync (Criar pedido): valida entrada, consolida itens repetidos, checa estoque e persiste o pedido.
    public async Task<OrderResponse> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Validações básicas de entrada (RN-02 e RN-03).
        ValidateCreateRequest(request);

        // 2. Consolida itens com o mesmo ProductId somando as quantidades (RN-04).
        // consolidatedItems (itens consolidados): dicionário [ProductId -> Quantidade total].
        var consolidatedItems = ConsolidateItems(request.Items!);

        // 3. Verifica se o cliente existe (RN-01).
        // customer (cliente): registro encontrado no banco para o CustomerId informado.
        var customer = await _commandRepository.GetCustomerByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new ResourceNotFoundException($"Cliente com Id {request.CustomerId} não foi encontrado.");
        }

        // 4. Carrega todos os produtos solicitados em uma única consulta e valida existência (RN-05).
        // requestedProductIds (Ids dos produtos solicitados): lista única dos produtos do pedido.
        var requestedProductIds = consolidatedItems.Keys.ToList();
        var products = await _commandRepository.GetProductsByIdsAsync(requestedProductIds, cancellationToken);

        // productsById (produtos por Id): mapa para acesso rápido por identificador.
        var productsById = products.ToDictionary(product => product.Id);

        foreach (var productId in requestedProductIds)
        {
            if (!productsById.ContainsKey(productId))
            {
                throw new ResourceNotFoundException($"Produto com Id {productId} não foi encontrado.");
            }
        }

        // 5. Monta a entidade Order, valida estoque e calcula preços/total no servidor (RN-06, RN-07 e RN-08).
        var order = new Order
        {
            CustomerId = customer.Id,
            OrderDate = DateTime.UtcNow
        };

        // stockDecreases (baixas de estoque): pares (ProductId, Quantidade) a decrementar atomicamente no banco.
        var stockDecreases = new List<(int ProductId, int QuantityToDecrease)>(consolidatedItems.Count);

        foreach (var (productId, quantity) in consolidatedItems)
        {
            var product = productsById[productId];

            if (!product.HasEnoughStock(quantity))
            {
                throw new InsufficientStockException(
                    $"Estoque insuficiente para o produto '{product.Name}'. Disponível: {product.Stock}, solicitado: {quantity}.");
            }

            product.DecreaseStock(quantity);
            order.AddItem(product.Id, quantity, product.Price);
            stockDecreases.Add((product.Id, quantity));
        }

        // 6. Salva pedido, itens e decrementa estoque dentro da mesma transação no banco.
        // savedOrder (pedido salvo): entidade já persistida com o Id gerado pelo SQL Server.
        var savedOrder = await _commandRepository.SaveOrderWithStockUpdateAsync(
            order,
            stockDecreases,
            cancellationToken);

        // 7. Monta o DTO de resposta com os nomes e subtotais calculados.
        var responseItems = savedOrder.Items
            .Select(item =>
            {
                var product = productsById[item.ProductId];
                return new OrderItemResponse(
                    item.ProductId,
                    product.Name,
                    item.Quantity,
                    item.UnitPrice,
                    item.GetSubtotal());
            })
            .ToList();

        return new OrderResponse(
            savedOrder.Id,
            customer.Id,
            customer.Name,
            savedOrder.OrderDate,
            savedOrder.TotalAmount,
            responseItems);
    }

    // GetOrdersAsync (Listar pedidos): valida paginação/filtros de data e consulta a lista paginada.
    public Task<PagedResponse<OrderResponse>> GetOrdersAsync(
        OrderFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        if (filter.Page < 1)
        {
            throw new ValidationException("O parâmetro 'page' deve ser maior ou igual a 1.");
        }

        if (filter.PageSize < 1 || filter.PageSize > 100)
        {
            throw new ValidationException("O parâmetro 'pageSize' deve estar entre 1 e 100.");
        }

        if (filter.CustomerId is <= 0)
        {
            throw new ValidationException("O parâmetro 'customerId' deve ser maior que zero.");
        }

        ValidateDateRange(filter.StartDate, filter.EndDate);

        return _readRepository.GetOrdersAsync(filter, cancellationToken);
    }

    // GetTopCustomersAsync (Obter Top 5 clientes): valida período e consulta o ranking de clientes.
    public Task<IReadOnlyList<TopCustomerResponse>> GetTopCustomersAsync(
        TopCustomersQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidateDateRange(query.StartDate, query.EndDate);

        return _readRepository.GetTopCustomersAsync(query, cancellationToken);
    }

    // ValidateCreateRequest (Validar requisição de criação): checa cliente, presença de itens e quantidades positivas.
    private static void ValidateCreateRequest(CreateOrderRequest? request)
    {
        if (request is null)
        {
            throw new ValidationException("O corpo da requisição do pedido é obrigatório.");
        }

        if (request.CustomerId <= 0)
        {
            throw new ValidationException("O campo 'customerId' deve ser maior que zero.");
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ValidationException("O pedido deve conter pelo menos um item.");
        }

        foreach (var item in request.Items)
        {
            if (item.ProductId <= 0)
            {
                throw new ValidationException("Todos os itens devem possuir 'productId' maior que zero.");
            }

            if (item.Quantity <= 0)
            {
                throw new ValidationException("A quantidade de cada item deve ser maior que zero.");
            }
        }
    }

    // ConsolidateItems (Consolidar itens): agrupa produtos repetidos somando suas quantidades.
    private static Dictionary<int, int> ConsolidateItems(IReadOnlyList<CreateOrderItemRequest> items)
    {
        // consolidated (consolidado): mantém a ordem de primeira aparição de cada produto.
        var consolidated = new Dictionary<int, int>();

        foreach (var item in items)
        {
            if (consolidated.TryGetValue(item.ProductId, out var currentQuantity))
            {
                consolidated[item.ProductId] = currentQuantity + item.Quantity;
            }
            else
            {
                consolidated[item.ProductId] = item.Quantity;
            }
        }

        return consolidated;
    }

    // ValidateDateRange (Validar intervalo de datas): garante que a data inicial não seja maior que a final (RN-09).
    private static void ValidateDateRange(DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            throw new ValidationException("A data inicial ('startDate') não pode ser maior que a data final ('endDate').");
        }
    }
}
