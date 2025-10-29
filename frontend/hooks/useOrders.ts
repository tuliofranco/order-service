import useSWR from 'swr';
import { ordersService } from '@/lib/services/orders';
import type { OrderItem } from '@/types/order-item';

export function useOrders() {
  const { data, error, isLoading, mutate } = useSWR<OrderItem[]>(
    '/orders',
    () => ordersService.list(),
    {
      refreshInterval: 3000,
      revalidateOnFocus: true,
    }
  );

  return {
    orders: data ?? [],
    error: error as Error | undefined,
    isLoading: !!isLoading && !data,
    mutate,
  };
}
