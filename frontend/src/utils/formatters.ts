// ============================================================================
// Funções utilitárias de formatação para exibição na tela em português (pt-BR).
// Centralizar aqui evita repetir a lógica de formatação em vários componentes.
// ============================================================================

// formatCurrency (Formatar moeda): transforma número (ex.: 1500.5) em texto "R$ 1.500,50".
export function formatCurrency(value: number): string {
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  }).format(value);
}

// formatDateTime (Formatar data e hora): converte data ISO-8601 da API para "dd/mm/aaaa hh:mm".
export function formatDateTime(isoDate: string): string {
  const parsedDate = new Date(isoDate); // parsedDate (data convertida): objeto Date do JavaScript.

  if (Number.isNaN(parsedDate.getTime())) {
    return isoDate;
  }

  return new Intl.DateTimeFormat('pt-BR', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(parsedDate);
}
