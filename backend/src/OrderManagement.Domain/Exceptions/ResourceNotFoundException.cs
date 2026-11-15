namespace OrderManagement.Domain.Exceptions;

// ResourceNotFoundException (Exceção de recurso não encontrado): lançada quando um cliente ou produto não existe no banco (mapeada para HTTP 404).
public class ResourceNotFoundException : Exception
{
    // Construtor que recebe a mensagem informando qual recurso não foi encontrado.
    public ResourceNotFoundException(string message) // message (mensagem de erro)
        : base(message)
    {
    }
}
