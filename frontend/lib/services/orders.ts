import { api } from "@/lib/api";
import type { OrderCreate } from "@/types/order-create-request";
import type { OrdersList } from "@/types/orders-list";
import type { OrderCreatedResponse } from "@/types/order-created-response";
import type { OrderHistory } from "@/types/order-history";

export const ordersService = {
  async create(payload: OrderCreate) :Promise<OrderCreatedResponse>{
    const { data } = await api.post("/orders", payload);
    return data;
  },

  async list(): Promise<OrdersList> {
    const { data } = await api.get<OrdersList>("/orders", {
      headers: { Accept: "application/json" },
    });
    return data;
  },

  async getById(id: string): Promise<OrderHistory> {
    const { data } = await api.get(`/orders/${id}`, {
      headers: { Accept: "application/json" },
    });
    return data;
  },
};
