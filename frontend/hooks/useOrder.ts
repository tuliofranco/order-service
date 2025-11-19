"use client";

import useSWR from "swr";
import { ordersService } from "@/lib/services/orders";
import type { OrderHistory } from "@/types/order-history";

type UseOrderOptions = {
  refreshMs?: number;
};

type UseOrderResult = {
  order: OrderHistory | null;
  isLoading: boolean;
  error: string | null;
  mutate: () => Promise<OrderHistory | undefined>;
};

const fetcher = (id: string) => ordersService.getById(id);

export function useOrder(
  id: string | undefined,
  options?: UseOrderOptions
): UseOrderResult {
  const shouldFetch = Boolean(id);

  const { data, error, isLoading, mutate } = useSWR<OrderHistory, string>(
    shouldFetch ? `/orders/${id}` : null,
    () => fetcher(id!),
    {
      refreshInterval: options?.refreshMs ?? 0,
    }
  );

  return {
    order: data ?? null,
    isLoading,
    error: error ?? null,
    mutate: async () => {
      if (!shouldFetch) return undefined;
      return mutate();
    },
  };
}
