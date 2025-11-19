import { OrderStatus } from '@/types/order-status';

export interface HistoryItem {
  id: string;
  orderId: string;
  fromStatus: OrderStatus | null;
  toStatus: OrderStatus;
  occurredAt: string;
  source: string;
  correlationId: string;
}