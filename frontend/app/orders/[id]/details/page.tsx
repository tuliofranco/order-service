"use client";

import { useParams, useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Sidebar } from "@/components/layout/Sidebar";
import { OrderInfoCard } from "@/components/orders/OrderInfoCard";
import { useOrder } from "@/hooks/useOrder";
import { useOrderStatusToast } from "@/hooks/useOrderStatusToast";

export default function OrderDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();

  const { order, isLoading, error, mutate } = useOrder(id, { refreshMs: 1500 });

  useOrderStatusToast(order, {
    onlyWhenFinalized: false,
    dedupeMs: 1500,
  });

  return (
    <div className="min-h-screen grid md:grid-cols-[280px_1fr] bg-gray-50">
      <Sidebar />
      <main className="flex items-start justify-center p-4 sm:p-6">
        <div className="w-full max-w-3xl">
          <div className="mb-4 flex flex-wrap items-center gap-2">
            <Button variant="outline" onClick={() => router.push("/orders")}>
              ← Voltar
            </Button>
          </div>

          <OrderInfoCard order={order} loading={isLoading} error={error} />
        </div>
      </main>
    </div>
  );
}
