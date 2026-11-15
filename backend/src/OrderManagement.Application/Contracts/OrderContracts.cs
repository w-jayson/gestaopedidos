namespace OrderManagement.Application.Contracts;

// CreateOrderRequest (Requisição de criação de pedido): dados enviados pelo cliente HTTP no POST /pedidos.
public record CreateOrderRequest(
    int CustomerId,                      // CustomerId (Id do cliente): identificador do cliente comprador.
    List<CreateOrderItemRequest>? Items  // Items (Itens): lista de produtos e quantidades desejadas.
);

// CreateOrderItemRequest (Item da requisição de criação): representa cada linha enviada para compor o pedido.
public record CreateOrderItemRequest(
    int ProductId, // ProductId (Id do produto): identificador do produto no catálogo.
    int Quantity   // Quantity (Quantidade): quantidade solicitada do produto.
);

// OrderFilterQuery (Filtro de consulta de pedidos): parâmetros opcionais de busca e paginação do GET /pedidos.
public record OrderFilterQuery(
    int? CustomerId = null,     // CustomerId (Id do cliente): filtra pedidos de um cliente específico.
    DateOnly? StartDate = null, // StartDate (Data inicial): data de início do filtro (inclusiva).
    DateOnly? EndDate = null,   // EndDate (Data final): data de término do filtro (inclusiva).
    int Page = 1,               // Page (Página): número da página atual (começa em 1).
    int PageSize = 10           // PageSize (Tamanho da página): quantidade de pedidos por página (1 a 100).
);

// OrderResponse (Resposta de pedido): representação detalhada de um pedido devolvida pela API.
public record OrderResponse(
    int Id,                               // Id (Identificador): número único do pedido criado/consultado.
    int CustomerId,                       // CustomerId (Id do cliente): identificador do cliente.
    string CustomerName,                  // CustomerName (Nome do cliente): nome exibido na listagem.
    DateTime OrderDate,                   // OrderDate (Data do pedido): data/hora em UTC da criação.
    decimal TotalAmount,                  // TotalAmount (Valor total): soma de todos os subtotais do pedido.
    IReadOnlyList<OrderItemResponse> Items // Items (Itens): lista de itens pertencentes ao pedido.
);

// OrderItemResponse (Resposta de item do pedido): detalhes de um produto dentro do pedido retornado.
public record OrderItemResponse(
    int ProductId,      // ProductId (Id do produto): identificador do produto.
    string ProductName, // ProductName (Nome do produto): nome descritivo do produto.
    int Quantity,       // Quantity (Quantidade): quantidade comprada.
    decimal UnitPrice,  // UnitPrice (Preço unitário): preço aplicado no momento da compra.
    decimal Subtotal    // Subtotal (Subtotal): valor calculado (Quantidade * Preço Unitário).
);
