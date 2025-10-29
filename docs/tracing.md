# Tracing: Como seguir um pedido pelos logs

## Resumo
- **CorrelationId = OrderId**
- Logs em **JSON** com *scopes* (`IncludeScopes = true`)

## Como seguir um pedido
1. Obtenha o `OrderId` no `POST /orders` (retornado pela API).
2. Filtre os logs:
   ```bash
   # usando jq
   cat logs.jsonl | jq 'select(.Scopes[]? .OrderId == "<ORDER_ID>")'

   # ou usando grep (menos preciso)
   grep '"OrderId":"<ORDER_ID>"' logs.jsonl
