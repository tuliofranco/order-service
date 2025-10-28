"use client";

import { memo } from "react";
import { CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";

type OrdersHeaderProps = {
  title: string;
  subtitle?: string;
  total: number;
  loading: boolean;
};

function OrdersHeader({ title, subtitle, total, loading }: OrdersHeaderProps) {
  return (
    <CardHeader className="pb-4 bg-gradient-to-b from-white to-gray-50">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <CardTitle className="text-2xl font-semibold text-[#0f2740]">
            {title}
          </CardTitle>

          {subtitle ? (
            <p className="text-sm text-gray-500 mt-2">{subtitle}</p>
          ) : null}
        </div>

        <Badge variant="secondary" className="w-fit">
          {loading
            ? "Carregando..."
            : `${total} ${total === 1 ? "pedido" : "pedidos"}`}
        </Badge>
      </div>
    </CardHeader>
  );
}

export default memo(OrdersHeader);
