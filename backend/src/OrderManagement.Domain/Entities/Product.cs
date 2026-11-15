using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Domain.Entities;

// Product (Produto): representa um item disponível no catálogo para venda (mapeada para a tabela "Produtos").
public class Product
{
    // Id (Identificador): chave primária única do produto.
    public int Id { get; set; }

    // Name (Nome): nome descritivo do produto.
    public string Name { get; set; } = string.Empty;

    // Price (Preço): preço unitário atual praticado no catálogo.
    public decimal Price { get; set; }

    // Stock (Estoque): quantidade disponível em estoque no momento.
    public int Stock { get; set; }

    // HasEnoughStock (Tem estoque suficiente): verifica se há saldo para atender à quantidade pedida.
    public bool HasEnoughStock(int requestedQuantity) // requestedQuantity (quantidade solicitada)
    {
        return requestedQuantity > 0 && Stock >= requestedQuantity;
    }

    // DecreaseStock (Baixar estoque): subtrai a quantidade vendida do saldo disponível.
    public void DecreaseStock(int quantity) // quantity (quantidade)
    {
        if (quantity <= 0)
        {
            throw new ValidationException("A quantidade solicitada deve ser maior que zero.");
        }

        if (Stock < quantity)
        {
            throw new InsufficientStockException(
                $"Estoque insuficiente para o produto '{Name}'. Disponível: {Stock}, solicitado: {quantity}.");
        }

        Stock -= quantity;
    }
}
