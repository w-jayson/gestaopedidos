namespace OrderManagement.Domain.Entities;

// OrderItem (Item do pedido): representa cada linha de produto dentro de um pedido (mapeada para a tabela "ItensPedido").
public class OrderItem
{
    // Id (Identificador): chave primária do item do pedido.
    public int Id { get; set; }

    // OrderId (Id do pedido): chave estrangeira que liga o item ao pedido.
    public int OrderId { get; set; }

    // ProductId (Id do produto): chave estrangeira que aponta para o produto comprado.
    public int ProductId { get; set; }

    // Quantity (Quantidade): quantidade comprada deste produto no pedido.
    public int Quantity { get; set; }

    // UnitPrice (Preço unitário): preço congelado no momento da compra (copiado da tabela Produtos).
    public decimal UnitPrice { get; set; }

    // Order (Pedido): navegação para o pedido pai.
    public Order? Order { get; set; }

    // Product (Produto): navegação para o produto associado.
    public Product? Product { get; set; }

    // GetSubtotal (Obter subtotal): calcula o valor total desta linha (Quantidade * Preço Unitário).
    public decimal GetSubtotal()
    {
        return Quantity * UnitPrice;
    }
}
