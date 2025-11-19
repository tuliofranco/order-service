import type { KeyedMutator } from "swr";
import type { OrdersList } from "./orders-list";

export type UseOrdersResult = {
  orders: OrdersList;
  isLoading: boolean;
  error: Error | null;
  mutate: KeyedMutator<OrdersList>;
};
