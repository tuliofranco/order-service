export type ApiOrderStatus = "Pendente" | "Processando" | "Finalizado";

// Esse é o item do histórico que vem do backend
export interface ApiOrderHistoryItem {
  id: string;
  orderId: string;
  fromStatus: ApiOrderStatus | null;
  toStatus: ApiOrderStatus;
  occurredAt: string;
  source: string;
  correlationId: string;
}

// ESTE é o shape bruto que o backend devolve em GET /orders/:id
export interface ApiOrderResponse {
  id: string;
  clienteNome: string;
  produto: string;
  valor: number;
  status: ApiOrderStatus;
  createdAtUtc: string;
  history: ApiOrderHistoryItem[];
}

// ESTE é o shape que o frontend usa pra renderizar detalhes
// (o que você já chamava de CompleteOrder)
export type CompleteOrder = {
  id: string;
  clienteNome: string;
  produto: string;
  valor: number;
  status: ApiOrderStatus;
  data_criacao: string; // vem de createdAtUtc
  history: ApiOrderHistoryItem[];
};
