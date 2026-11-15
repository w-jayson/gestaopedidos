namespace OrderManagement.Application.Contracts;

// CustomerResponse (Resposta de cliente): dados públicos de um cliente retornados no GET /clientes.
public record CustomerResponse(
    int Id,       // Id (Identificador): chave única do cliente.
    string Name,  // Name (Nome): nome completo do cliente.
    string Email  // Email (E-mail): endereço de e-mail cadastrado.
);

// ProductResponse (Resposta de produto): dados públicos de um produto retornados no GET /produtos.
public record ProductResponse(
    int Id,        // Id (Identificador): chave única do produto.
    string Name,   // Name (Nome): nome do produto.
    decimal Price, // Price (Preço): preço unitário vigente no catálogo.
    int Stock      // Stock (Estoque): quantidade disponível atualmente para venda.
);
