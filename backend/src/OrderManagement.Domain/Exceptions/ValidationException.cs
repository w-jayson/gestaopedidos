namespace OrderManagement.Domain.Exceptions;

// ValidationException (Exceção de validação): lançada quando dados de entrada violam regras básicas (mapeada para HTTP 400).
public class ValidationException : Exception
{
    // Construtor que recebe a mensagem explicando qual validação falhou.
    public ValidationException(string message) // message (mensagem de erro)
        : base(message)
    {
    }
}
