"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import type { OrderItem } from "@/types/order-item";

type OrderInfoCardProps = {
  order: OrderItem | null;
  loading: boolean;
  error: string | null;
};

export function OrderInfoCard({ order, loading, error }: OrderInfoCardProps) {
  return (
    <Card className="border-0 shadow-md rounded-2xl overflow-hidden">
      <CardHeader className="pb-4 bg-gradient-to-b from-white to-gray-50">
        <CardTitle className="text-2xl font-semibold text-[#0f2740]">
          Detalhes do Pedido
        </CardTitle>
      </CardHeader>

      <CardContent className="p-6 text-sm">
        {loading && <p>Carregando…</p>}

        {error && (
          <p className="text-red-600">
            {error}
          </p>
        )}

        {!loading && !error && order && (
          <div className="space-y-3">
            <InfoRow label="ID" value={<code className="font-mono break-all">{order.id}</code>} />
            <InfoRow label="Cliente" value={order.clienteNome} />
            <InfoRow label="Produto" value={order.produto} />
            <InfoRow label="Valor" value={formatCurrency(order.valor)} />
            <InfoRow label="Status" value={order.status} />
            <InfoRow
              label="Criado em"
              value={formatDateTime(order.data_criacao)}
            />
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function InfoRow({
  label,
  value,
}: {
  label: string;
  value: React.ReactNode;
}) {
  return (
    <div className="flex flex-col sm:flex-row sm:items-baseline gap-1">
      <span className="font-semibold text-gray-700 min-w-[110px]">{label}:</span>
      <span className="text-gray-900">{value}</span>
    </div>
  );
}
