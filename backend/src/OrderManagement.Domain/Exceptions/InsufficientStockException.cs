namespace OrderManagement.Domain.Exceptions;

// InsufficientStockException (Exceção de estoque insuficiente): lançada quando a quantidade pedida supera o saldo em estoque (mapeada para HTTP 409).
public class InsufficientStockException : Exception
{
    // Construtor que recebe a mensagem detalhando o produto sem saldo suficiente.
    public InsufficientStockException(string message) // message (mensagem de erro)
        : base(message)
    {
    }
}
