import { api } from "@/lib/api";
import type { OrderCreate } from "@/types/order-create";
import type { OrdersList } from "@/types/orders-list";
import type { OrderItem } from "@/types/order-item";

export const ordersService = {
  
  async create(payload: OrderCreate) {
    const { data } = await api.post("/orders", payload);
    return data;
  },

 async list(): Promise<OrdersList> {
    const res = await api.get("/orders", {
      headers: { Accept: "application/json" },
    });
    return res.data;
  },
  async getById(id: string): Promise<OrderItem> {
    const { data } = await api.get(`/orders/${id}`, {
      headers: { Accept: "application/json" },
    });
    return data;
  },
};
