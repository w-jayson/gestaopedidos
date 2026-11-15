namespace OrderManagement.Domain.Entities;

// Customer (Cliente): representa quem realiza pedidos no sistema (mapeada para a tabela "Clientes").
public class Customer
{
    // Id (Identificador): chave primária única do cliente.
    public int Id { get; set; }

    // Name (Nome): nome completo do cliente.
    public string Name { get; set; } = string.Empty;

    // Email (E-mail): endereço de e-mail único para identificar o cliente sem ambiguidade.
    public string Email { get; set; } = string.Empty;
}
