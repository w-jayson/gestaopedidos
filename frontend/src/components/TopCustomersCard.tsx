import { useEffect, useState, type FormEvent } from 'react';
import { fetchTopCustomers } from '../api/client';
import type { TopCustomer } from '../types/api';
import { formatCurrency } from '../utils/formatters';

// TopCustomersCardProps (Propriedades do card Top 5 Clientes):
// define os dados que o componente pai (App) envia para este componente.
interface TopCustomersCardProps {
  refreshTrigger: number; // refreshTrigger (gatilho de atualização): muda quando um pedido novo é criado.
}

// TopCustomersCard (Card Top 5 Clientes): exibe o ranking dos clientes que mais compraram no período.
export function TopCustomersCard({ refreshTrigger }: TopCustomersCardProps) {
  // useState (usar estado): guarda valores na memória do componente; quando mudam, a tela redesenha.
  const [startDate, setStartDate] = useState<string>('2026-01-01'); // startDate (data inicial do filtro)
  const [endDate, setEndDate] = useState<string>('2026-12-31');     // endDate (data final do filtro)
  const [appliedRange, setAppliedRange] = useState<{ startDate: string; endDate: string }>({
    startDate: '2026-01-01',
    endDate: '2026-12-31',
  }); // appliedRange (período aplicado): só muda quando o usuário clica em "Atualizar".

  const [topCustomers, setTopCustomers] = useState<TopCustomer[]>([]); // topCustomers (lista do ranking)
  const [isLoading, setIsLoading] = useState<boolean>(false);          // isLoading (está carregando)
  const [errorMessage, setErrorMessage] = useState<string | null>(null); // errorMessage (mensagem de erro)

  // useEffect (usar efeito): roda automaticamente ao abrir a tela e sempre que
  // "refreshTrigger" ou "appliedRange" mudarem, buscando o ranking atualizado na API.
  useEffect(() => {
    let isCancelled = false; // isCancelled (foi cancelado): evita atualizar estado se o componente fechar antes da resposta.

    async function loadRanking() {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        const data = await fetchTopCustomers(appliedRange.startDate, appliedRange.endDate);
        if (!isCancelled) {
          setTopCustomers(data);
        }
      } catch (error) {
        if (!isCancelled) {
          setErrorMessage(error instanceof Error ? error.message : 'Erro ao carregar ranking.');
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }

    loadRanking();

    // Função de limpeza executada se o componente desmontar antes do fetch terminar.
    return () => {
      isCancelled = true;
    };
  }, [refreshTrigger, appliedRange]);

  // handleSubmit (Lidar com envio do formulário): aplica o período escolhido sem recarregar a página.
  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); // preventDefault (evitar padrão): impede o navegador de recarregar a aba inteira.
    setAppliedRange({ startDate, endDate });
  }

  // handleClearPeriod (Limpar período): mostra o ranking considerando todo o histórico sem filtro de datas.
  function handleClearPeriod() {
    setStartDate('');
    setEndDate('');
    setAppliedRange({ startDate: '', endDate: '' });
  }

  return (
    <section className="card">
      <div className="card-header">
        <div>
          <h2>Top 5 Clientes</h2>
          <p className="subtitle">Ranking por maior valor total de pedidos no período</p>
        </div>

        <form className="inline-form" onSubmit={handleSubmit}>
          <label className="field-inline">
            <span>De</span>
            <input
              type="date"
              value={startDate}
              onChange={(event) => setStartDate(event.target.value)}
            />
          </label>

          <label className="field-inline">
            <span>Até</span>
            <input
              type="date"
              value={endDate}
              onChange={(event) => setEndDate(event.target.value)}
            />
          </label>

          <button type="submit" className="btn-secondary">
            Atualizar
          </button>
          <button type="button" className="btn-ghost" onClick={handleClearPeriod}>
            Todo período
          </button>
        </form>
      </div>

      {errorMessage && <p className="alert-error">{errorMessage}</p>}

      {isLoading ? (
        <p className="muted">Carregando ranking...</p>
      ) : topCustomers.length === 0 ? (
        <p className="muted">Nenhuma compra registrada no período informado.</p>
      ) : (
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Posição</th>
                <th>Cliente</th>
                <th>Pedidos</th>
                <th className="text-right">Total Comprado</th>
              </tr>
            </thead>
            <tbody>
              {topCustomers.map((customer, index) => (
                <tr key={customer.customerId}>
                  <td>#{index + 1}</td>
                  <td>{customer.customerName}</td>
                  <td>{customer.ordersCount}</td>
                  <td className="text-right strong">{formatCurrency(customer.totalAmount)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
