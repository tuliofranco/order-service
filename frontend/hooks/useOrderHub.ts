"use client";

import { useEffect } from "react";
import * as signalR from "@microsoft/signalr";

import type { OrderCreatedResponse } from "@/types/order-created-response";
import type { OrderStatus } from "@/types/order-status";
import { getOrderHubConnection } from "@/lib/signalr/orderHubConnection";

export type OrderStatusChangedPayload = {
  id: string;
  status: OrderStatus;
};

type UseOrderHubOptions = {
  onOrderCreated?: (order: OrderCreatedResponse) => void;
  onOrderStatusChanged?: (orderId: string) => void;
};

export function useOrderHub({
  onOrderCreated,
  onOrderStatusChanged,
}: UseOrderHubOptions = {}) {
  useEffect(() => {
    const connection = getOrderHubConnection();

    async function start() {
      if (connection.state === signalR.HubConnectionState.Disconnected) {
        try {
          await connection.start();
          console.log("[SignalR] Connected to order hub");
        } catch (err) {
          console.error("[SignalR] Error starting connection", err);
        }
      }
    }

    start();

    if (onOrderCreated) {
      connection.on("OrderCreatedNotification", onOrderCreated);
    }

    if (onOrderStatusChanged) {
      connection.on("OrderChangeStatusNotification", onOrderStatusChanged);
    }

    return () => {
      if (onOrderCreated) {
        connection.off("OrderCreatedNotification", onOrderCreated);
      }
      if (onOrderStatusChanged) {
        connection.off("OrderChangeStatusNotification", onOrderStatusChanged);
      }
    };
  }, [onOrderCreated, onOrderStatusChanged]);
}
