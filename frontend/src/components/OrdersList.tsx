import type { Order, PagedResponse } from '../types/api';
import { formatCurrency, formatDateTime } from '../utils/formatters';

// OrdersListProps (Propriedades da lista de pedidos):
// recebe os dados da página atual e os callbacks para trocar de página.
interface OrdersListProps {
  ordersPage: PagedResponse<Order> | null;   // ordersPage (página de pedidos): dados retornados por GET /pedidos.
  isLoading: boolean;                        // isLoading (está carregando): mostra aviso enquanto busca dados.
  errorMessage: string | null;               // errorMessage (mensagem de erro): exibe erro caso a API falhe.
  onPageChange: (newPage: number) => void;   // onPageChange (ao mudar página): chama o pai passando a nova página.
}

// OrdersList (Lista de pedidos): renderiza a tabela paginada com resumo de itens e controles Anterior/Próxima.
export function OrdersList({
  ordersPage,
  isLoading,
  errorMessage,
  onPageChange,
}: OrdersListProps) {
  if (errorMessage) {
    return <p className="alert-error">{errorMessage}</p>;
  }

  if (isLoading) {
    return <p className="muted">Carregando pedidos...</p>;
  }

  if (!ordersPage || ordersPage.items.length === 0) {
    return <p className="muted">Nenhum pedido encontrado para os filtros selecionados.</p>;
  }

  // currentPage (página atual) e totalPages (total de páginas) controlam os botões de navegação.
  const currentPage = ordersPage.page;
  const totalPages = ordersPage.totalPages;

  return (
    <div>
      <div className="table-wrapper">
        <table>
          <thead>
            <tr>
              <th>#Pedido</th>
              <th>Cliente</th>
              <th>Data</th>
              <th>Itens</th>
              <th className="text-right">Valor Total</th>
            </tr>
          </thead>
          <tbody>
            {ordersPage.items.map((order) => (
              <tr key={order.id}>
                <td>#{order.id}</td>
                <td>{order.customerName}</td>
                <td>{formatDateTime(order.orderDate)}</td>
                <td>
                  <ul className="items-summary">
                    {order.items.map((item) => (
                      <li key={`${order.id}-${item.productId}`}>
                        {item.quantity}x {item.productName} ({formatCurrency(item.unitPrice)})
                      </li>
                    ))}
                  </ul>
                </td>
                <td className="text-right strong">{formatCurrency(order.totalAmount)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Controles simples de paginação */}
      <div className="pagination">
        <span className="muted">
          Página {currentPage} de {totalPages} ({ordersPage.totalCount} pedido(s))
        </span>

        <div className="pagination-buttons">
          <button
            type="button"
            className="btn-ghost"
            disabled={currentPage <= 1}
            onClick={() => onPageChange(currentPage - 1)}
          >
            Anterior
          </button>

          <button
            type="button"
            className="btn-ghost"
            disabled={currentPage >= totalPages}
            onClick={() => onPageChange(currentPage + 1)}
          >
            Próxima
          </button>
        </div>
      </div>
    </div>
  );
}
