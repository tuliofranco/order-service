import { OrderStatus } from "./order-status";

export interface OrderHistoryChange {
  id: string;
  orderId: string;
  fromStatus: OrderStatus | null;
  toStatus: OrderStatus;
  occurredAt: string;
  source: string;
  correlationId: string;
}