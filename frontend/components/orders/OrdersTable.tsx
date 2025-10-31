"use client";

import { memo } from "react";
import { Badge } from "@/components/ui/badge";
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table";
import type { OrderItem } from "@/types/orders-list";

type OrdersTableProps = {
  orders: OrderItem[];
  loading: boolean;
  formatCurrency: (v: number) => string;
  formatDateTime: (iso: string) => string;
  getStatusVariant: (status: string) => "default" | "secondary" | "destructive" | "outline";
  actionRenderer: (order: OrderItem) => React.ReactNode;
};

function OrdersTable({
  orders,
  loading,
  formatCurrency,
  formatDateTime,
  getStatusVariant,
  actionRenderer,
}: OrdersTableProps) {
  const hasData = orders.length > 0;

  return (
    <div className="rounded-lg border bg-white">
      <div className="w-full overflow-x-auto">
        <Table className="min-w-[720px] sm:min-w-0">
          <TableHeader>
            <TableRow className="bg-gray-50">
              <TableHead className="font-semibold">ID</TableHead>
              <TableHead className="font-semibold">Cliente</TableHead>
              <TableHead className="font-semibold hidden sm:table-cell">Produto</TableHead>
              <TableHead className="font-semibold">Valor</TableHead>
              <TableHead className="font-semibold">Status</TableHead>
              <TableHead className="font-semibold hidden md:table-cell">Data de Criação</TableHead>
              <TableHead className="font-semibold text-right">Ações</TableHead>
            </TableRow>
          </TableHeader>

          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={7} className="text-center py-8 text-gray-500 text-sm">
                  Carregando pedidos...
                </TableCell>
              </TableRow>
            ) : !hasData ? (
              <TableRow>
                <TableCell colSpan={7} className="text-center py-8 text-gray-500 text-sm">
                  Nenhum pedido disponível.
                </TableCell>
              </TableRow>
            ) : (
              orders.map((order) => (
                <TableRow key={order.id} className="hover:bg-gray-50 transition-colors">
                  <TableCell className="font-mono text-xs text-gray-600">
                    {order.id.slice(0, 8)}…
                  </TableCell>

                  <TableCell className="font-medium truncate max-w-[160px]">
                    {order.clienteNome}
                  </TableCell>

                  <TableCell className="hidden sm:table-cell truncate max-w-[180px]">
                    {order.produto}
                  </TableCell>

                  <TableCell className="font-semibold text-[#0f2740] whitespace-nowrap">
                    {formatCurrency(order.valor)}
                  </TableCell>

                  <TableCell>
                    <Badge variant={getStatusVariant(order.status)}>{order.status}</Badge>
                  </TableCell>

                  <TableCell className="hidden md:table-cell text-sm text-gray-600 whitespace-nowrap">
                    {formatDateTime(order.data_criacao)}
                  </TableCell>

                  <TableCell className="text-right whitespace-nowrap">
                    {actionRenderer(order)}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}

export default memo(OrdersTable);
