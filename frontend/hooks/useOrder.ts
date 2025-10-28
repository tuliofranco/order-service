"use client";

import useSWR from "swr";
import type { OrderItem } from "@/types/order-item";
import { ordersService } from "@/lib/services/orders";

const fetcher = (id: string) => ordersService.getById(id);

export function useOrder(id: string, opts?: { refreshMs?: number }) {
  const { data, error, isLoading, mutate } = useSWR<OrderItem>(
    id ? ["order", id] : null,
    () => fetcher(id),
    {
      refreshInterval: opts?.refreshMs ?? 3000,
      revalidateOnFocus: false,
    }
  );

  return {
    order: data ?? null,
    error,
    isLoading,
    mutate,
  };
}
