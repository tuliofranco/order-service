## Observabilidade

### Tracing & Correlação
Adotamos **CorrelationId = OrderId** e logs estruturados em **JSON** com *scopes* habilitados.  
- API: `POST /orders` usa `BeginScope({ OrderId })`; `GET /orders/{id}` injeta `OrderId` via middleware.  
- Worker: processa mensagens com `BeginScope({ OrderId })`.  
- Service Bus: `CorrelationId` da mensagem = `OrderId`.

➡ Detalhes e como filtrar logs por `OrderId`: veja [docs/tracing.md](docs/tracing.md).  
➡ Decisão registrada como ADR: [docs/adr/0001-correlation-id-equals-order-id.md](docs/adr/0001-correlation-id-equals-order-id.md).
