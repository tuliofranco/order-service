import { useState } from "react";
import { ordersService } from "@/lib/services/orders";
import type { OrderCreate } from "@/types/order-create-request";
import type { OrderCreatedResponse } from "@/types/order-created-response";

export function useCreateOrder(onCreated?: (order: OrderCreatedResponse) => void) {
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const create = async(payload: OrderCreate) => {
        try
        {
            setError(null);
            setSubmitting(true);
            const created = await ordersService.create(payload);
            onCreated?.(created);
            return created;
        }
        catch (err: any)
        {
            const msg = err?.message ?? "Erro ao criar pedido";
            setError(msg)
            throw err;
        } finally{
            setSubmitting(false);
        }
    };
    return { create, submitting, error };
}