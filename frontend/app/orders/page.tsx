"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Eye } from "lucide-react";

import { ordersService } from "@/lib/services/orders";
import type { OrderItem } from "@/types/order-item";

import { Card, CardContent } from "@/components/ui/card";
import { useToast } from "@/hooks/use-toast";

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
  const [orders, setOrders] = useState<OrderItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { toast } = useToast();

  // Carregar pedidos ao montar
  useEffect(() => {
    let active = true;

    (async () => {
      try {
        setLoading(true);
        const data = await ordersService.list();
        if (active) {
          setOrders(data ?? []);
          setError(null);
        }
      } catch (e: any) {
        if (active) {
          setError(e?.message ?? "Erro ao carregar pedidos");
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
  }, []);

  // Quando um pedido novo é criado
  function handleOrderCreated(created: OrderItem) {
    setOrders((prev) => [created, ...prev]);
    toast({
      title: "Pedido criado!",
      description: `${created.clienteNome} • ${formatCurrency(
        created.valor
      )} • ${created.produto}`,
    });
  }

  return (
    <div className="min-h-screen grid md:grid-cols-[280px_1fr] bg-gray-50">
      {/* Sidebar fixa no desktop */}
      <AppSidebar />

      {/* Conteúdo */}
      <main className="flex items-start justify-center p-4 sm:p-6">
        <div className="w-full max-w-7xl">
          <Card className="border-0 shadow-md rounded-2xl overflow-hidden bg-white">
            <OrdersHeader
              total={orders.length}
              loading={loading}
              title="Pedidos"
              subtitle="Visualize e crie pedidos do sistema"
            />

            <CardContent className="p-6 space-y-6">
              {/* Formulário de criação */}
              <CreateOrderForm onCreated={handleOrderCreated} />

              {/* Mensagem de erro geral */}
              {error && (
                <p className="text-sm text-red-600 font-medium">{error}</p>
              )}

              {/* Tabela */}
              <OrdersTable
                orders={orders}
                loading={loading}
                formatCurrency={formatCurrency}
                formatDateTime={formatDateTime}
                getStatusVariant={getStatusVariant}
                actionRenderer={(order) => (
                  <Link
                    href={`/orders/${order.id}/details`}
                    aria-label={`Ver detalhes do pedido ${order.id}`}
                    className="inline-flex items-center text-sm font-medium border rounded-md px-3 py-1.5 hover:bg-gray-50 transition-colors"
                  >
                    <Eye className="h-4 w-4 mr-2" />
                    Ver detalhes
                  </Link>
                )}
              />
            </CardContent>
          </Card>
        </div>
      </main>
    </div>
  );
}
