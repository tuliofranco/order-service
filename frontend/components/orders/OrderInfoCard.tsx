"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { formatCurrency, formatDateTime } from "@/lib/formatters";
import type { CompleteOrder, ApiOrderHistoryItem } from "@/types/order-item";

type OrderInfoCardProps = {
  order: CompleteOrder | null;
  loading: boolean;
  error: string | null;
};

export function OrderInfoCard({ order, loading, error }: OrderInfoCardProps) {
  return (
    <Card className="border border-gray-200 shadow-md rounded-2xl overflow-hidden bg-white">
      <CardHeader className="pb-4 bg-gradient-to-b from-white to-gray-50 border-b border-gray-200">
        <CardTitle className="text-2xl font-semibold text-[#0f2740]">
          Detalhes do Pedido
        </CardTitle>
      </CardHeader>

      <CardContent className="p-6 text-sm text-gray-900">
        {loading && <p>Carregando…</p>}

        {error && (
          <p className="text-red-600 font-medium">{error}</p>
        )}

        {!loading && !error && order && (
          <div className="space-y-8">
            {/* BLOCO 1: Infos principais */}
            <section className="space-y-3">
              <InfoRow
                label="ID"
                value={
                    <code className="font-mono break-all text-xs bg-gray-100 text-gray-800 px-1 py-0.5 rounded border border-gray-300">
                      {order.id}
                    </code>
                }
              />
              <InfoRow label="Cliente" value={order.clienteNome} />
              <InfoRow label="Produto" value={order.produto} />
              <InfoRow
                label="Valor"
                value={formatCurrency(order.valor)}
              />
              <InfoRow label="Status" value={order.status} />
              <InfoRow
                label="Criado em"
                value={formatDateTime(order.data_criacao)}
              />
            </section>

            {/* BLOCO 2: Histórico de Status */}
            <section>
              <h2 className="text-lg font-semibold text-[#0f2740] mb-4">
                Histórico de Status
              </h2>

              {order.history.length === 0 ? (
                <p className="text-gray-500 text-sm">
                  Nenhuma transição de status registrada.
                </p>
              ) : (
                <Timeline history={order.history} />
              )}
            </section>
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
      <span className="font-semibold text-gray-700 min-w-[110px]">
        {label}:
      </span>
      <span className="text-gray-900">{value}</span>
    </div>
  );
}

function TimelineDot({ status }: { status: ApiOrderHistoryItem["toStatus"] }) {
  const colorByStatus: Record<string, { bg: string; border: string }> = {
    Pendente:     { bg: "bg-yellow-200",    border: "border-yellow-500" },
    Processando:  { bg: "bg-blue-200",      border: "border-blue-500" },
    Finalizado:   { bg: "bg-green-200",     border: "border-green-500" },
  };

  const colors = colorByStatus[status] ?? {
    bg: "bg-gray-200",
    border: "border-gray-500",
  };

  return (
    <div
      className={[
        "w-3 h-3 rounded-full border",
        colors.bg,
        colors.border,
        // alinhamento vertical suave
        "mt-[2px] shrink-0",
      ].join(" ")}
    />
  );
}

function Timeline({ history }: { history: ApiOrderHistoryItem[] }) {
  return (
    <ol className="rounded-md border border-gray-200 bg-white">
      {history.map((h, idx) => (
        <li
          key={h.id}
          className={[
            "flex items-start gap-3 p-4",
            idx < history.length - 1 ? "border-b border-gray-200" : "",
          ].join(" ")}
        >
          {/* Coluna fixa da bolinha */}
          <div className="flex flex-col items-center w-4">
            <TimelineDot status={h.toStatus} />
          </div>

          {/* Coluna do conteúdo */}
          <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between w-full gap-2">
            {/* Esquerda: descrição */}
            <div className="text-sm text-gray-900 font-medium leading-relaxed">
              <div className="flex flex-col">
                {/* Linha principal */}
                <span className="text-gray-900 font-medium">
                  {h.fromStatus
                    ? `${h.fromStatus} → ${h.toStatus}`
                    : `Status inicial: ${h.toStatus}`}
                </span>

                {/* Sub-linha menor */}
                <span className="text-[11px] text-gray-500 font-mono leading-tight">
                  {h.source} • {h.correlationId.slice(0, 8)}
                </span>
              </div>
            </div>

            {/* Direita: timestamp */}
            <time className="text-xs text-gray-500 whitespace-nowrap font-normal">
              {formatDateTime(h.occurredAt)}
            </time>
          </div>
        </li>
      ))}
    </ol>
  );
}
