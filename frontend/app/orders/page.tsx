"use client";

import Link from "next/link";
import { Eye } from "lucide-react";

import { useOrders } from "@/hooks/useOrders";
import { useToast } from "@/hooks/use-toast";
import { useStatusToasts } from "@/hooks/useStatusToasts";

import AppSidebar from "@/components/layout/AppSidebar";
import OrdersHeader from "@/components/orders/OrdersHeader";
import OrdersTable from "@/components/orders/OrdersTable";
import CreateOrderForm from "@/components/orders/CreateOrderForm";

import {
  formatDateTime,
  formatCurrency,
  getStatusVariant,
} from "@/lib/formatters";

export default function OrdersPage() {
  const { orders, isLoading: loading, error, mutate } = useOrders();
  const { toast } = useToast();

  useStatusToasts(orders, { onlyWhenFinalized: false, dedupeMs: 1500 });

  async function handleOrderCreated() {
    await mutate();
    toast({
      title: "Pedido criado!",
      description: "Estamos processando o status em tempo real.",
    });
  }

  return (
    <div
      className="
        min-h-screen
        grid
        bg-gray-50
        lg:grid-cols-[280px_1fr]  /* só em telas largas eu reservo espaço pra sidebar */
      "
    >
      <AppSidebar />

      <main className="flex items-start justify-center p-3 sm:p-6">
        <div className="w-full max-w-7xl">
          <div className="border-0 shadow-md rounded-2xl overflow-hidden bg-white">
            <OrdersHeader
              total={orders.length}
              loading={loading}
              title="Pedidos"
              subtitle="Visualize e crie pedidos do sistema"
            />

            <div className="p-4 sm:p-6 space-y-6">
              <CreateOrderForm onCreated={handleOrderCreated} />

              {error && (
                <p className="text-sm text-red-600 font-medium">
                  {error.message ?? "Erro ao carregar pedidos"}
                </p>
              )}

              <OrdersTable
                orders={orders ?? []}
                loading={loading}
                formatCurrency={formatCurrency}
                formatDateTime={formatDateTime}
                getStatusVariant={getStatusVariant}
                actionRenderer={(order) => (
                  <Link
                    href={`/orders/${order.id}/details`}
                    aria-label={`Ver detalhes do pedido ${order.id}`}
                    className="
                      inline-flex items-center justify-center
                      text-sm font-medium
                      border rounded-md px-2.5 py-1.5
                      hover:bg-gray-50 transition-colors
                    "
                  >
                    <Eye className="h-4 w-4 sm:mr-2" />
                    <span className="hidden sm:inline">Ver detalhes</span>
                  </Link>
                )}
              />
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
