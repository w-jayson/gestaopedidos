import { useState, type FormEvent } from 'react';
import type { Customer } from '../types/api';

// OrdersFilterBarProps (Propriedades da barra de filtros de pedidos):
// recebe a lista de clientes para o <select> e as funções de callback para avisar o componente pai.
interface OrdersFilterBarProps {
  customers: Customer[]; // customers (clientes): opções exibidas no dropdown de cliente.
  onApplyFilters: (customerId: string, startDate: string, endDate: string) => void; // onApplyFilters (ao aplicar filtros)
  onClearFilters: () => void; // onClearFilters (ao limpar filtros)
}

// OrdersFilterBar (Barra de filtros de pedidos): permite filtrar os pedidos por cliente e período.
export function OrdersFilterBar({
  customers,
  onApplyFilters,
  onClearFilters,
}: OrdersFilterBarProps) {
  // Estados locais de rascunho: guardam o que o usuário está selecionando antes de clicar em "Filtrar".
  const [customerId, setCustomerId] = useState<string>(''); // customerId (id do cliente selecionado)
  const [startDate, setStartDate] = useState<string>('');   // startDate (data inicial)
  const [endDate, setEndDate] = useState<string>('');       // endDate (data final)

  // handleSubmit (Lidar com envio): envia os filtros digitados para o App buscar na API.
  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    onApplyFilters(customerId, startDate, endDate);
  }

  // handleClear (Lidar com limpeza): zera os campos locais e avisa o App para recarregar sem filtros.
  function handleClear() {
    setCustomerId('');
    setStartDate('');
    setEndDate('');
    onClearFilters();
  }

  return (
    <form className="filter-bar" onSubmit={handleSubmit}>
      <label className="field">
        <span>Cliente</span>
        <select
          value={customerId}
          onChange={(event) => setCustomerId(event.target.value)}
        >
          <option value="">Todos os clientes</option>
          {customers.map((customer) => (
            <option key={customer.id} value={customer.id}>
              {customer.name} ({customer.email})
            </option>
          ))}
        </select>
      </label>

      <label className="field">
        <span>Data Inicial</span>
        <input
          type="date"
          value={startDate}
          onChange={(event) => setStartDate(event.target.value)}
        />
      </label>

      <label className="field">
        <span>Data Final</span>
        <input
          type="date"
          value={endDate}
          onChange={(event) => setEndDate(event.target.value)}
        />
      </label>

      <div className="filter-actions">
        <button type="submit" className="btn-secondary">
          Filtrar
        </button>
        <button type="button" className="btn-ghost" onClick={handleClear}>
          Limpar
        </button>
      </div>
    </form>
  );
}
