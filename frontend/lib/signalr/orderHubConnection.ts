"use client";

import * as signalR from "@microsoft/signalr";

let connection: signalR.HubConnection | null = null;

export function getOrderHubConnection() {
  if (!connection) {
    const hubUrl =
      process.env.NEXT_PUBLIC_ORDER_HUB_URL ?? // caminho completo http://localhost:5127/hub/notification
      "http://localhost:5127/hub/notification";

    console.log("[SignalR] hubUrl =", hubUrl);

    connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        withCredentials: false,
      })
      .withAutomaticReconnect()
      .build();
  }

  return connection;
}
