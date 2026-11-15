namespace OrderManagement.Application.Contracts;

// PagedResponse (Resposta paginada): encapsula uma página de resultados e os metadados de paginação.
public record PagedResponse<T>(
    IReadOnlyList<T> Items, // Items (Itens): registros da página atual.
    int Page,               // Page (Página): número da página atual.
    int PageSize,           // PageSize (Tamanho da página): limite de registros por página.
    int TotalCount,         // TotalCount (Contagem total): total de registros encontrados considerando os filtros.
    int TotalPages          // TotalPages (Total de páginas): quantidade total de páginas disponíveis.
);
