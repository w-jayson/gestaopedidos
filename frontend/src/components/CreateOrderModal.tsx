import { useEffect, useState, type FormEvent } from 'react';
import { createOrder } from '../api/client';
import type { Customer, Product } from '../types/api';
import { formatCurrency } from '../utils/formatters';

// DraftItem (Item em rascunho): representa uma linha ainda sendo preenchida no formulário do modal.
interface DraftItem {
  productId: string; // productId (id do produto escolhido no select)
  quantity: number;  // quantity (quantidade digitada pelo usuário)
}

// CreateOrderModalProps (Propriedades do modal de criação de pedido):
// controla se o modal aparece na tela e informa listas de clientes/produtos e callbacks.
interface CreateOrderModalProps {
  isOpen: boolean;           // isOpen (está aberto): controla a visibilidade do modal.
  customers: Customer[];     // customers (clientes): lista vinda de GET /clientes.
  products: Product[];       // products (produtos): lista vinda de GET /produtos.
  onClose: () => void;       // onClose (ao fechar): fecha o modal sem salvar.
  onOrderCreated: () => void; // onOrderCreated (ao criar pedido): avisa o App para atualizar listas.
}

// CreateOrderModal (Modal de criação de pedido): permite escolher cliente, adicionar produtos e enviar POST /pedidos.
export function CreateOrderModal({
  isOpen,
  customers,
  products,
  onClose,
  onOrderCreated,
}: CreateOrderModalProps) {
  const [selectedCustomerId, setSelectedCustomerId] = useState<string>(''); // selectedCustomerId (cliente escolhido)
  const [draftItems, setDraftItems] = useState<DraftItem[]>([
    { productId: '', quantity: 1 },
  ]); // draftItems (itens em edição): começa com 1 linha vazia.

  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);      // isSubmitting (está enviando)
  const [errorMessage, setErrorMessage] = useState<string | null>(null); // errorMessage (erro retornado pela API)

  // useEffect (usar efeito): sempre que o modal abre (isOpen = true), limpa os campos anteriores.
  useEffect(() => {
    if (isOpen) {
      setSelectedCustomerId('');
      setDraftItems([{ productId: '', quantity: 1 }]);
      setErrorMessage(null);
      setIsSubmitting(false);
    }
  }, [isOpen]);

  // Se o modal estiver fechado, não renderiza nada no HTML (return null).
  if (!isOpen) {
    return null;
  }

  // handleAddItemRow (Adicionar linha de item): acrescenta mais um produto ao pedido.
  function handleAddItemRow() {
    setDraftItems((currentItems) => [...currentItems, { productId: '', quantity: 1 }]);
  }

  // handleRemoveItemRow (Remover linha de item): remove a linha pelo índice (mantendo ao menos 1).
  function handleRemoveItemRow(indexToRemove: number) {
    setDraftItems((currentItems) =>
      currentItems.filter((_, index) => index !== indexToRemove),
    );
  }

  // handleItemChange (Alterar campo de um item): atualiza produto ou quantidade de uma linha específica.
  function handleItemChange(indexToUpdate: number, field: 'productId' | 'quantity', value: string) {
    setDraftItems((currentItems) =>
      currentItems.map((item, index) => {
        if (index !== indexToUpdate) {
          return item;
        }

        if (field === 'productId') {
          return { ...item, productId: value };
        }

        const parsedQuantity = Number(value);
        return {
          ...item,
          quantity: Number.isNaN(parsedQuantity) ? 1 : parsedQuantity,
        };
      }),
    );
  }

  // calculateEstimatedTotal (Calcular prévia do total): soma preço * quantidade dos itens já selecionados.
  // Observação: o valor oficial final é sempre recalculado pelo backend em C#.
  function calculateEstimatedTotal(): number {
    return draftItems.reduce((sum, item) => {
      const product = products.find((p) => p.id === Number(item.productId));
      if (!product || item.quantity <= 0) {
        return sum;
      }
      return sum + product.price * item.quantity;
    }, 0);
  }

  // handleSubmit (Lidar com envio do pedido): valida seleção básica e envia POST /pedidos.
  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorMessage(null);

    if (!selectedCustomerId) {
      setErrorMessage('Selecione um cliente antes de salvar o pedido.');
      return;
    }

    const hasUnselectedProduct = draftItems.some((item) => !item.productId);
    if (hasUnselectedProduct) {
      setErrorMessage('Selecione o produto em todas as linhas antes de salvar.');
      return;
    }

    setIsSubmitting(true);

    try {
      await createOrder({
        customerId: Number(selectedCustomerId),
        items: draftItems.map((item) => ({
          productId: Number(item.productId),
          quantity: item.quantity,
        })),
      });

      // Avisa o App que o pedido foi criado (fecha modal + recarrega lista, estoque e ranking).
      onOrderCreated();
    } catch (error) {
      // Exibe a mensagem amigável do ProblemDetails (ex.: estoque insuficiente - HTTP 409).
      setErrorMessage(error instanceof Error ? error.message : 'Erro ao criar pedido.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="modal-title">
      <div className="modal">
        <div className="modal-header">
          <h2 id="modal-title">Novo Pedido</h2>
          <button type="button" className="btn-ghost" onClick={onClose} disabled={isSubmitting}>
            Fechar
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <label className="field">
            <span>Cliente</span>
            <select
              value={selectedCustomerId}
              onChange={(event) => setSelectedCustomerId(event.target.value)}
            >
              <option value="">Selecione um cliente...</option>
              {customers.map((customer) => (
                <option key={customer.id} value={customer.id}>
                  {customer.name} ({customer.email})
                </option>
              ))}
            </select>
          </label>

          <div className="items-editor">
            <div className="items-editor-header">
              <span>Itens do Pedido</span>
              <button type="button" className="btn-secondary" onClick={handleAddItemRow}>
                + Adicionar item
              </button>
            </div>

            {draftItems.map((item, index) => (
              <div className="item-row" key={index}>
                <select
                  aria-label={`Produto do item ${index + 1}`}
                  value={item.productId}
                  onChange={(event) => handleItemChange(index, 'productId', event.target.value)}
                >
                  <option value="">Selecione um produto...</option>
                  {products.map((product) => (
                    <option key={product.id} value={product.id}>
                      {product.name} — {formatCurrency(product.price)} (Estoque: {product.stock})
                    </option>
                  ))}
                </select>

                <input
                  type="number"
                  min={1}
                  aria-label={`Quantidade do item ${index + 1}`}
                  value={item.quantity}
                  onChange={(event) => handleItemChange(index, 'quantity', event.target.value)}
                />

                {draftItems.length > 1 && (
                  <button
                    type="button"
                    className="btn-danger"
                    onClick={() => handleRemoveItemRow(index)}
                  >
                    Remover
                  </button>
                )}
              </div>
            ))}
          </div>

          <div className="modal-total">
            <span>Prévia do Total:</span>
            <strong>{formatCurrency(calculateEstimatedTotal())}</strong>
          </div>

          {errorMessage && <p className="alert-error">{errorMessage}</p>}

          <div className="modal-footer">
            <button type="button" className="btn-ghost" onClick={onClose} disabled={isSubmitting}>
              Cancelar
            </button>
            <button type="submit" className="btn-primary" disabled={isSubmitting}>
              {isSubmitting ? 'Salvando...' : 'Confirmar Pedido'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
