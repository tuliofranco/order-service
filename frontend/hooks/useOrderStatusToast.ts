"use client";

import { useEffect, useRef } from "react";
import type { OrderItem } from "@/types/order-item";
import { useToast } from "@/hooks/use-toast";

type Status = OrderItem["status"];

export function useOrderStatusToast(
  order: OrderItem | null,
  opts?: { dedupeMs?: number; onlyWhenFinalized?: boolean }
) {
  const { toast } = useToast();
  const prevRef = useRef<Status | null>(null);
  const lastAtRef = useRef<number>(0);
  const dedupeMs = opts?.dedupeMs ?? 1500;

  useEffect(() => {
    if (!order) return;
    const prev = prevRef.current;
    const next = order.status;

    if (prev && prev !== next) {
      if (!opts?.onlyWhenFinalized || next === "Finalizado") {
        const now = Date.now();
        if (now - lastAtRef.current > dedupeMs) {
          toast({
            title: "Status atualizado",
            description: `${prev} → ${next} (pedido ${order.id.slice(0, 8)}...)`,
          });
          lastAtRef.current = now;
        }
      }
    }
    prevRef.current = next;
  }, [order, dedupeMs, opts?.onlyWhenFinalized, toast]);
}
