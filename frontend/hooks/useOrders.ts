import useSWR from "swr";
import type { OrdersList } from "@/types/orders-list";
import type { UseOrdersResult } from "@/types/use-orders-result";
import { ordersService } from "@/lib/services/orders";

const fetcher = () => ordersService.list();

export function useOrders(): UseOrdersResult {
  const { data, error, isLoading, mutate } = useSWR<OrdersList, Error>(
    "/orders",
    fetcher,
    {
      fallbackData: [],
    }
  );

  const orders: OrdersList = Array.isArray(data) ? data : [];

  return {
    orders: data ?? [],
    isLoading,
    error: error ?? null,
    mutate,
  };
}
