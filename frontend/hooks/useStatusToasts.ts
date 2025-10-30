"use client";

import { useEffect, useRef } from "react";
import { useToast } from "@/hooks/use-toast";
import type { OrderItem } from "@/types/orders-list";

type Options = {
  onlyWhenFinalized?: boolean;
  dedupeMs?: number;
};

export function useStatusToasts(
  orders: OrderItem[] | undefined,
  opts: Options = {}
) {
  const { toast } = useToast();
  const prevStatusMapRef = useRef<Map<string, string>>(new Map());
  const initializedRef = useRef(false);
  const lastShownRef = useRef<Map<string, number>>(new Map());

  const { onlyWhenFinalized = false, dedupeMs = 1500 } = opts;

  useEffect(() => {
    if (!orders) return;
    
    if (!initializedRef.current) {
      const m = new Map<string, string>();
      for (const o of orders) m.set(o.id, o.status);
      prevStatusMapRef.current = m;
      initializedRef.current = true;
      return;
    }

    const now = Date.now();
    const prevMap = prevStatusMapRef.current;

    for (const o of orders) {
      const prev = prevMap.get(o.id);
      if (!prev) {
        // novo item: só registra estado
        prevMap.set(o.id, o.status);
        continue;
      }

      if (prev !== o.status) {
        if (!onlyWhenFinalized || o.status === "Finalizado") {
          // duração por status
          const duration =
            o.status === "Finalizado" ? 5000 :
            o.status === "Processando" ? 2500 :
            3000; // Pendente ou outros

          const key = `${o.id}:${o.status}`;
          const last = lastShownRef.current.get(key) ?? 0;
          if (now - last > dedupeMs) {
            toast({
              title: "Status atualizado",
              description: `${o.clienteNome}: ${prev} → ${o.status}`,
              duration,
            });
            lastShownRef.current.set(key, now);
          }
        }
      }
    }
    const next = new Map<string, string>();
    for (const o of orders) next.set(o.id, o.status);
    prevStatusMapRef.current = next;
  }, [orders, toast, onlyWhenFinalized, dedupeMs]);
}
