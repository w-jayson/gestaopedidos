import type {
  CreateOrderRequest,
  Customer,
  Order,
  OrderFilters,
  PagedResponse,
  ProblemDetails,
  Product,
  TopCustomer,
} from '../types/api';

// API_BASE_URL (URL base da API):
// Usamos "/api" tanto no desenvolvimento local (proxy no vite.config.ts)
// quanto no Docker (proxy reverso no nginx.conf), evitando problemas de CORS.
const API_BASE_URL = '/api';

// requestJson (Requisitar JSON):
// função genérica reutilizável que executa fetch, verifica se houve erro HTTP
// e lê a mensagem amigável do ProblemDetails retornada pela API .NET.
async function requestJson<T>(path: string, options?: RequestInit): Promise<T> {
  // fetch (buscar): função nativa do navegador para fazer chamadas HTTP assíncronas.
  const response = await fetch(`${API_BASE_URL}${path}`, options);

  if (!response.ok) {
    // Tenta ler o corpo JSON no formato ProblemDetails (RFC 7807) enviado pelo backend.
    let errorMessage = `Erro na requisição (HTTP ${response.status}).`;

    try {
      const problem = (await response.json()) as ProblemDetails;
      errorMessage = problem.detail || problem.title || errorMessage;
    } catch {
      // Se a resposta não for JSON válido, mantém a mensagem padrão com o código HTTP.
    }

    throw new Error(errorMessage);
  }

  // Converte o corpo JSON da resposta para o tipo esperado <T>.
  return (await response.json()) as T;
}

// buildQueryString (Montar query string): transforma um objeto de filtros em "?chave=valor&...".
function buildQueryString(params: Record<string, string | number | undefined>): string {
  const searchParams = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== '') {
      searchParams.set(key, String(value));
    }
  }

  const queryText = searchParams.toString();
  return queryText ? `?${queryText}` : '';
}

// fetchCustomers (Buscar clientes): chama GET /clientes para preencher os <select>s da tela.
export function fetchCustomers(): Promise<Customer[]> {
  return requestJson<Customer[]>('/clientes');
}

// fetchProducts (Buscar produtos): chama GET /produtos para listar produtos, preços e estoques.
export function fetchProducts(): Promise<Product[]> {
  return requestJson<Product[]>('/produtos');
}

// fetchOrders (Buscar pedidos): chama GET /pedidos com filtros opcionais e paginação.
export function fetchOrders(filters: OrderFilters): Promise<PagedResponse<Order>> {
  const query = buildQueryString({
    customerId: filters.customerId,
    startDate: filters.startDate,
    endDate: filters.endDate,
    page: filters.page,
    pageSize: filters.pageSize,
  });

  return requestJson<PagedResponse<Order>>(`/pedidos${query}`);
}

// fetchTopCustomers (Buscar Top 5 clientes): chama GET /relatorios/top-clientes no período informado.
export function fetchTopCustomers(startDate?: string, endDate?: string): Promise<TopCustomer[]> {
  const query = buildQueryString({
    startDate,
    endDate,
  });

  return requestJson<TopCustomer[]>(`/relatorios/top-clientes${query}`);
}

// createOrder (Criar pedido): envia POST /pedidos com cliente e itens selecionados.
export function createOrder(payload: CreateOrderRequest): Promise<Order> {
  return requestJson<Order>('/pedidos', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(payload),
  });
}
