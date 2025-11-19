import { OrderHistoryChange } from "./history-change";
import { OrderStatus } from "./order-status";


export type OrderHistory = {
  id: string;
  clienteNome: string;
  produto: string;
  valor: number;
  status: OrderStatus;
  createdAtUtc: string;
  history: OrderHistoryChange[];
}