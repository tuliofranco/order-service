"use client";

import useSWR from "swr";
import {
  CompleteOrder,
  ApiOrderResponse,
} from "@/types/order-item";
import { ordersService } from "@/lib/services/orders";

type UseOrderOptions = {
  refreshMs?: number;
};

// função de adaptação: ApiOrderResponse -> CompleteOrder
function mapApiToCompleteOrder(data: ApiOrderResponse): CompleteOrder {
  // garante o histórico ordenado por occurredAt (mais antigo primeiro)
  const sortedHistory = [...data.history].sort(
    (a, b) =>
      new Date(a.occurredAt).getTime() -
      new Date(b.occurredAt).getTime()
  );

  return {
    id: data.id,
    clienteNome: data.clienteNome,
    produto: data.produto,
    valor: data.valor,
    status: data.status,
    data_criacao: data.createdAtUtc,
    history: sortedHistory,
  };
}

// usamos SWR mas agora sem fetch manual; usamos o service
export function useOrder(id: string, opts?: UseOrderOptions) {
  const { data, error, isLoading, mutate } = useSWR(
    // key única por pedido
    id ? ["order-by-id", id] : null,
    // fetcher
    async ([, orderId]): Promise<CompleteOrder> => {
      const apiResponse = await ordersService.getById(orderId);
      return mapApiToCompleteOrder(apiResponse);
    },
    {
      refreshInterval: opts?.refreshMs ?? 0,
    }
  );

  return {
    order: data ?? null,
    isLoading,
    error: error ? (error as any).message ?? "Erro desconhecido" : null,
    mutate,
  };
}
