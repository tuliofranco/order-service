"use client";

import { useState } from "react";
import { ordersService } from "@/lib/services/orders";
import type { OrderCreate } from "@/types/order-create";
import type { OrderItem } from "@/types/order-item";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

type Props = {
  onCreated?: (order: OrderItem) => void; // chamado após sucesso
};

export default function CreateOrderForm({ onCreated }: Props) {
  const [form, setForm] = useState<OrderCreate>({
    clienteNome: "",
    produto: "",
    valor: 0,
  });
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleChange =
    (field: keyof OrderCreate) => (e: React.ChangeEvent<HTMLInputElement>) => {
      const v = e.target.value;
      setForm((prev) => ({
        ...prev,
        [field]: field === "valor" ? Number(v.replace(",", ".")) : v,
      }));
    };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!form.clienteNome || !form.produto || Number.isNaN(form.valor)) {
      setError("Preencha todos os campos corretamente.");
      return;
    }

    try {
      setSubmitting(true);
      const created = await ordersService.create(form);
      // created deve ser o objeto do pedido retornado pela API
      onCreated?.(created as OrderItem);
      setForm({ clienteNome: "", produto: "", valor: 0 });
    } catch (err: any) {
      setError(err?.message ?? "Erro ao criar pedido");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Card className="mb-6">
      <CardHeader>
        <CardTitle>Novo Pedido</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="grid gap-4 sm:grid-cols-3">
          <div className="sm:col-span-1">
            <label className="block text-sm font-medium mb-1">Cliente</label>
            <Input
              placeholder="Nome do cliente"
              value={form.clienteNome}
              onChange={handleChange("clienteNome")}
            />
          </div>

          <div className="sm:col-span-1">
            <label className="block text-sm font-medium mb-1">Produto</label>
            <Input
              placeholder="Ex.: Boleto"
              value={form.produto}
              onChange={handleChange("produto")}
            />
          </div>

          <div className="sm:col-span-1">
            <label className="block text-sm font-medium mb-1">Valor</label>
            <Input
              type="number"
              step="0.01"
              placeholder="0.00"
              value={Number.isNaN(form.valor) ? "" : String(form.valor)}
              onChange={handleChange("valor")}
            />
          </div>

          <div className="sm:col-span-3 flex items-center gap-3">
            <Button type="submit" disabled={submitting}>
              {submitting ? "Enviando..." : "Criar Pedido"}
            </Button>
            {error && <p className="text-sm text-red-600">{error}</p>}
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
