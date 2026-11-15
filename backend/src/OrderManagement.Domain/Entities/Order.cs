namespace OrderManagement.Domain.Entities;

// Order (Pedido): representa o cabeçalho de um pedido feito por um cliente (mapeada para a tabela "Pedidos").
public class Order
{
    // Id (Identificador): número único do pedido.
    public int Id { get; set; }

    // CustomerId (Id do cliente): chave estrangeira que aponta para o cliente dono do pedido.
    public int CustomerId { get; set; }

    // OrderDate (Data do pedido): data e hora em UTC em que o pedido foi registrado.
    public DateTime OrderDate { get; set; }

    // TotalAmount (Valor total): soma de todos os itens do pedido calculada no servidor.
    public decimal TotalAmount { get; set; }

    // Customer (Cliente): propriedade de navegação para acessar os dados do cliente.
    public Customer? Customer { get; set; }

    // Items (Itens): lista dos itens que compõem este pedido.
    public List<OrderItem> Items { get; set; } = [];

    // AddItem (Adicionar item): inclui um item na lista e atualiza o valor total do pedido.
    public void AddItem(int productId, int quantity, decimal unitPrice)
    {
        // orderItem (item do pedido): cria o registro com o preço unitário vigente no momento da compra.
        var orderItem = new OrderItem
        {
            ProductId = productId, // productId (id do produto)
            Quantity = quantity,   // quantity (quantidade)
            UnitPrice = unitPrice  // unitPrice (preço unitário)
        };

        Items.Add(orderItem);
        RecalculateTotal();
    }

    // RecalculateTotal (Recalcular total): soma (Quantidade * Preço Unitário) de todos os itens do pedido.
    public void RecalculateTotal()
    {
        TotalAmount = Items.Sum(item => item.GetSubtotal());
    }
}
