namespace OrderManagement.Application.Contracts;

// TopCustomersQuery (Filtro do relatório Top Clientes): intervalo opcional de datas para o ranking.
public record TopCustomersQuery(
    DateOnly? StartDate = null, // StartDate (Data inicial): início do período (inclusivo).
    DateOnly? EndDate = null    // EndDate (Data final): fim do período (inclusivo).
);

// TopCustomerResponse (Resposta do ranking Top Clientes): resumo de compras de cada cliente no ranking.
public record TopCustomerResponse(
    int CustomerId,       // CustomerId (Id do cliente): identificador único do cliente.
    string CustomerName,  // CustomerName (Nome do cliente): nome completo do cliente.
    int OrdersCount,      // OrdersCount (Quantidade de pedidos): número de pedidos realizados no período.
    decimal TotalAmount   // TotalAmount (Valor total): soma do valor comprado no período.
);
