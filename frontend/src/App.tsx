import { useEffect, useState } from 'react';
import { fetchCustomers, fetchOrders, fetchProducts } from './api/client';
import { CreateOrderModal } from './components/CreateOrderModal';
import { OrdersFilterBar } from './components/OrdersFilterBar';
import { OrdersList } from './components/OrdersList';
import { TopCustomersCard } from './components/TopCustomersCard';
import type { Customer, Order, OrderFilters, PagedResponse, Product } from './types/api';

// DEFAULT_PAGE_SIZE (Tamanho padrão da página): 5 pedidos por página para testar paginação facilmente.
const DEFAULT_PAGE_SIZE = 5;

// App (Aplicação principal): componente raiz que guarda os estados globais simples da tela
// e conecta o Card Top 5, os Filtros, a Tabela de Pedidos e o Modal de Criação.
export function App() {
  // Estados dos catálogos usados nos <select>s (clientes e produtos).
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [products, setProducts] = useState<Product[]>([]);

  // Estados da listagem paginada de pedidos.
  const [ordersPage, setOrdersPage] = useState<PagedResponse<Order> | null>(null);
  const [isLoadingOrders, setIsLoadingOrders] = useState<boolean>(false);
  const [ordersError, setOrdersError] = useState<string | null>(null);

  // filters (filtros atuais): controla filtros aplicados e número da página atual.
  const [filters, setFilters] = useState<OrderFilters>({
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });

  // isCreateModalOpen (modal aberto): controla se o modal de "Novo Pedido" está visível.
  const [isCreateModalOpen, setIsCreateModalOpen] = useState<boolean>(false);

  // successMessage (mensagem de sucesso): exibe feedback positivo após criar um pedido.
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // refreshTrigger (gatilho de atualização): incrementar este número força os useEffects
  // a recarregar pedidos, produtos (estoque atualizado) e Top 5 clientes após criar um pedido.
  const [refreshTrigger, setRefreshTrigger] = useState<number>(0);

  // Efeito 1: Carrega clientes e produtos na abertura da página e sempre que um novo pedido é criado.
  useEffect(() => {
    let isCancelled = false;

    async function loadCatalog() {
      try {
        // Promise.all executa as duas chamadas HTTP ao mesmo tempo para ganhar velocidade.
        const [customersData, productsData] = await Promise.all([
          fetchCustomers(),
          fetchProducts(),
        ]);

        if (!isCancelled) {
          setCustomers(customersData);
          setProducts(productsData);
        }
      } catch (error) {
        if (!isCancelled) {
          setOrdersError(
            error instanceof Error ? error.message : 'Erro ao carregar clientes e produtos.',
          );
        }
      }
    }

    loadCatalog();

    return () => {
      isCancelled = true;
    };
  }, [refreshTrigger]);

  // Efeito 2: Carrega os pedidos sempre que "filters" (página/filtros) ou "refreshTrigger" mudarem.
  useEffect(() => {
    let isCancelled = false;

    async function loadOrders() {
      setIsLoadingOrders(true);
      setOrdersError(null);

      try {
        const data = await fetchOrders(filters);
        if (!isCancelled) {
          setOrdersPage(data);
        }
      } catch (error) {
        if (!isCancelled) {
          setOrdersError(error instanceof Error ? error.message : 'Erro ao carregar pedidos.');
        }
      } finally {
        if (!isCancelled) {
          setIsLoadingOrders(false);
        }
      }
    }

    loadOrders();

    return () => {
      isCancelled = true;
    };
  }, [filters, refreshTrigger]);

  // handleApplyFilters (Aplicar filtros): volta para a página 1 com os novos filtros selecionados.
  function handleApplyFilters(customerId: string, startDate: string, endDate: string) {
    setSuccessMessage(null);
    setFilters({
      customerId: customerId ? Number(customerId) : undefined,
      startDate: startDate || undefined,
      endDate: endDate || undefined,
      page: 1,
      pageSize: DEFAULT_PAGE_SIZE,
    });
  }

  // handleClearFilters (Limpar filtros): remove todos os filtros e volta para a página 1.
  function handleClearFilters() {
    setSuccessMessage(null);
    setFilters({
      page: 1,
      pageSize: DEFAULT_PAGE_SIZE,
    });
  }

  // handlePageChange (Trocar página): mantém os filtros atuais e altera apenas o número da página.
  function handlePageChange(newPage: number) {
    setFilters((currentFilters) => ({
      ...currentFilters,
      page: newPage,
    }));
  }

  // handleOrderCreated (Pedido criado com sucesso):
  // fecha o modal, volta para a página 1 (para ver o pedido novo no topo) e dispara atualização geral.
  function handleOrderCreated() {
    setIsCreateModalOpen(false);
    setSuccessMessage('Pedido cadastrado com sucesso! Estoque e ranking atualizados.');
    setFilters((currentFilters) => ({
      ...currentFilters,
      page: 1,
    }));
    setRefreshTrigger((count) => count + 1);
  }

  return (
    <div className="container">
      <header className="page-header">
        <div>
          <h1>Mini Gestão de Pedidos</h1>
          <p className="subtitle">Cadastro, consulta paginada e ranking de clientes</p>
        </div>

        <button
          type="button"
          className="btn-primary"
          onClick={() => {
            setSuccessMessage(null);
            setIsCreateModalOpen(true);
          }}
        >
          + Novo Pedido
        </button>
      </header>

      {successMessage && (
        <div className="alert-success" role="status">
          <span>{successMessage}</span>
          <button
            type="button"
            className="btn-ghost"
            onClick={() => setSuccessMessage(null)}
          >
            Fechar
          </button>
        </div>
      )}

      <main className="main-content">
        {/* Card do relatório Top 5 clientes */}
        <TopCustomersCard refreshTrigger={refreshTrigger} />

        {/* Seção de listagem e filtros de pedidos */}
        <section className="card">
          <div className="card-header">
            <div>
              <h2>Pedidos Cadastrados</h2>
              <p className="subtitle">Ordenados do mais recente para o mais antigo</p>
            </div>
          </div>

          <OrdersFilterBar
            customers={customers}
            onApplyFilters={handleApplyFilters}
            onClearFilters={handleClearFilters}
          />

          <OrdersList
            ordersPage={ordersPage}
            isLoading={isLoadingOrders}
            errorMessage={ordersError}
            onPageChange={handlePageChange}
          />
        </section>
      </main>

      {/* Modal de criação de novo pedido */}
      <CreateOrderModal
        isOpen={isCreateModalOpen}
        customers={customers}
        products={products}
        onClose={() => setIsCreateModalOpen(false)}
        onOrderCreated={handleOrderCreated}
      />
    </div>
  );
}
