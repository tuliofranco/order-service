import type { OrderStatus } from "@/types/order-status";

export type OrderCreatedResponse = {
  id: string;
  clienteNome: string;
  produto: string;
  valor: number;
  status: OrderStatus;
  data_criacao: string;
}