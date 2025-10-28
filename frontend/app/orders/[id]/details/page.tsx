"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { ordersService } from "@/lib/services/orders";
import type { OrderItem } from "@/types/order-item";

import { Sidebar } from "@/components/layout/Sidebar";
import { OrderInfoCard } from "@/components/orders/OrderInfoCard";
import { Button } from "@/components/ui/button";

export default function OrderDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();

  const [order, setOrder] = useState<OrderItem | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // Fetch dos dados
  useEffect(() => {
    let active = true;

    (async () => {
      try {
        setLoading(true);
        const data = await ordersService.getById(id);
        if (active) {
          setOrder(data);
          setError(null);
        }
      } catch (e: any) {
        if (active) {
          setError(e?.message ?? "Erro ao carregar o pedido");
          setOrder(null);
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    })();

    return () => {
      active = false;
    };
  }, [id]);

  return (
    <div className="min-h-screen grid md:grid-cols-[280px_1fr] bg-gray-50">
      <Sidebar />

      <main className="flex items-start justify-center p-4 sm:p-6">
        <div className="w-full max-w-3xl">
          {/* Actions/top nav */}
          <div className="mb-4 flex flex-wrap items-center gap-2">
            <Button
              variant="outline"
              onClick={() => router.push("/orders")}
            >
              ← Voltar
            </Button>
          </div>
          <OrderInfoCard order={order} loading={loading} error={error} />
        </div>
      </main>
    </div>
  );
}
