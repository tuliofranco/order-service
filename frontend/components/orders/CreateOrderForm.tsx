"use client";

import { useState, type ChangeEvent, type FormEvent } from "react";
import type { OrderCreate } from "@/types/order-create-request";
import type { OrderCreatedResponse } from "@/types/order-created-response";
import { useCreateOrder } from "@/hooks/useCreateOrder";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

type Props = {
  onCreated?: (order: OrderCreatedResponse) => void;
};


export default function CreateOrderForm({ onCreated }: Props) {
  const [form, setForm] = useState<OrderCreate>({
    clienteNome: "",
    produto: "",
    valor: 0,
  });

  const { create, submitting, error } = useCreateOrder(onCreated);
  const [validationError, setValidationError] = useState<string | null>(null);

  const handleChange = 
    (field: keyof OrderCreate) =>
    (e: ChangeEvent<HTMLInputElement>) => {
      const v = e.target.value;

      setValidationError(null);

      setForm((prev) => {
        if(field === "valor"){
          if(v === "" ){
            return {...prev, valor: NaN};
          }

          const parsed = parseFloat(v.replace(",", "."));
          return {...prev, valor: parsed };
        }

        return {...prev, [field]: v }
      });
    };

    const handleSubmit = async (e: FormEvent) => {
      e.preventDefault();
      setValidationError(null);

      if(!form.clienteNome || !form.produto || Number.isNaN(form.valor)) {
        setValidationError("Preencha todos os campos corretamente.");
        return;
      }
      try {
        await create(form);
        setForm({clienteNome: "", produto: "", valor: 0});
      } catch {
      }
    };

    const message = validationError ?? error;
    
    return (
      <Card className="mb-6">
        <CardHeader>
          <CardTitle className="text-base sm:text-lg">Novo Pedido</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <div>
              <label className="block text-sm font-medium mb-1">Cliente</label>
              <Input
                placeholder="Nome do cliente"
                value={form.clienteNome}
                onChange={handleChange("clienteNome")}
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-1">Produto</label>
              <Input
                placeholder="Ex.: Boleto"
                value={form.produto}
                onChange={handleChange("produto")}
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-1">Valor</label>
              <Input
                type="number"
                inputMode="decimal"
                step="0.01"
                min="0"
                placeholder="0.00"
                value={Number.isNaN(form.valor) ? "" : String(form.valor)}
                onChange={handleChange("valor")}
              />
            </div>

            <div className="sm:col-span-2 lg:col-span-3 flex flex-col sm:flex-row items-stretch sm:items-center gap-3">
              <Button type="submit" disabled={submitting} className="w-full sm:w-auto">
                {submitting ? "Enviando..." : "Criar Pedido"}
              </Button>

              {message && <p className="text-sm text-red-600">{message}</p>}
            </div>
          </form>
        </CardContent>
      </Card>
    );
}
