
## 3) `docs/adr/0001-correlation-id-equals-order-id.md` (T0.1)
```md
# ADR 0001 — CorrelationId = OrderId

## Contexto
Precisamos rastrear ponta-a-ponta (API → Service Bus → Worker) cada pedido.

## Decisão
- **CorrelationId = OrderId** em todas as mensagens publicadas.
- Logs estruturados via **AddJsonConsole(IncludeScopes = true)**.
- API:
  - `POST /orders`: abre `BeginScope({ OrderId })`.
  - `GET /orders/{id}`: middleware injeta `OrderId` no *scope*.
- Worker: ao consumir a mensagem, abre `BeginScope({ OrderId = payload.OrderId })`.

## Consequências
- Filtragem simples por `OrderId` em qualquer etapa.
- Preparado para evoluir para OpenTelemetry/trace distribuído depois.
