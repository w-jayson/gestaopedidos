// ============================================================================
// Tipos TypeScript espelhando exatamente os contratos JSON da API .NET.
// Em TypeScript, "interface" funciona como os DTOs (records) do C#:
// define quais campos existem e seus tipos para evitar erros de digitação.
// ============================================================================

// Customer (Cliente): representa um cliente retornado por GET /clientes.
export interface Customer {
  id: number;    // id (identificador): chave única do cliente no banco.
  name: string;  // name (nome): nome completo do cliente.
  email: string; // email (e-mail): e-mail cadastrado do cliente.
}

// Product (Produto): representa um produto retornado por GET /produtos.
export interface Product {
  id: number;    // id (identificador): chave única do produto no banco.
  name: string;  // name (nome): nome descritivo do produto.
  price: number; // price (preço): preço unitário atual no catálogo.
  stock: number; // stock (estoque): quantidade disponível em estoque.
}

// OrderItem (Item do pedido): representa um item já salvo dentro de um pedido.
export interface OrderItem {
  productId: number;   // productId (id do produto): identificador do produto comprado.
  productName: string; // productName (nome do produto): nome exibido na tabela.
  quantity: number;    // quantity (quantidade): quantidade comprada neste item.
  unitPrice: number;   // unitPrice (preço unitário): preço aplicado no momento da compra.
  subtotal: number;    // subtotal (subtotal): quantity * unitPrice.
}

// Order (Pedido): representa um pedido completo retornado por GET /pedidos ou POST /pedidos.
export interface Order {
  id: number;           // id (identificador): número único do pedido.
  customerId: number;   // customerId (id do cliente): identificador do cliente comprador.
  customerName: string; // customerName (nome do cliente): nome do cliente comprador.
  orderDate: string;    // orderDate (data do pedido): data/hora em texto ISO-8601 vinda da API.
  totalAmount: number;  // totalAmount (valor total): valor total do pedido calculado no servidor.
  items: OrderItem[];   // items (itens): lista de itens que compõem este pedido.
}

// PagedResponse (Resposta paginada): estrutura genérica <T> retornada por GET /pedidos.
// O "<T>" permite reutilizar essa mesma interface para qualquer tipo de lista paginada.
export interface PagedResponse<T> {
  items: T[];         // items (itens): registros da página atual.
  page: number;       // page (página): número da página atual (começa em 1).
  pageSize: number;   // pageSize (tamanho da página): limite de registros por página.
  totalCount: number; // totalCount (total de registros): quantidade total considerando os filtros.
  totalPages: number; // totalPages (total de páginas): quantidade total de páginas disponíveis.
}

// TopCustomer (Cliente no ranking Top 5): retornado por GET /relatorios/top-clientes.
export interface TopCustomer {
  customerId: number;   // customerId (id do cliente): identificador único do cliente.
  customerName: string; // customerName (nome do cliente): nome do cliente.
  ordersCount: number;  // ordersCount (quantidade de pedidos): quantos pedidos fez no período.
  totalAmount: number;  // totalAmount (valor total): soma total comprada no período.
}

// CreateOrderItemRequest (Item para criar pedido): cada linha enviada no POST /pedidos.
export interface CreateOrderItemRequest {
  productId: number; // productId (id do produto): produto escolhido pelo usuário.
  quantity: number;  // quantity (quantidade): quantidade desejada.
}

// CreateOrderRequest (Requisição de criação de pedido): corpo JSON enviado para POST /pedidos.
export interface CreateOrderRequest {
  customerId: number;              // customerId (id do cliente): cliente selecionado.
  items: CreateOrderItemRequest[]; // items (itens): lista com pelo menos 1 item.
}

// OrderFilters (Filtros de pedidos): parâmetros opcionais usados na consulta GET /pedidos.
export interface OrderFilters {
  customerId?: number; // customerId (id do cliente): "?" indica campo opcional.
  startDate?: string;  // startDate (data inicial): formato YYYY-MM-DD.
  endDate?: string;    // endDate (data final): formato YYYY-MM-DD.
  page: number;        // page (página): página desejada.
  pageSize: number;    // pageSize (tamanho da página): itens por página.
}

// ProblemDetails (Detalhes do erro HTTP): formato padrão RFC 7807 devolvido pela API em erros 400/404/409/500.
export interface ProblemDetails {
  title?: string;  // title (título): resumo curto do erro (ex.: "Estoque insuficiente").
  status?: number; // status (código HTTP): 400, 404, 409 ou 500.
  detail?: string; // detail (detalhe): mensagem explicativa do que aconteceu.
}
