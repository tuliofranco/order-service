# Order-Service

Sistema simples de gestão de pedidos com **API .NET**, **Frontend React/Next**, **PostgreSQL** e **Azure Service Bus**.
Quando um pedido é criado, os dados são persistidos, um **evento** é publicado na fila e um **Worker** processa o pedido, avançando o status até **Finalizado**.

---

## 🧰 Stack & versões

* **Backend**: .NET SDK **9.0.109**
* **Frontend**: Next.js **^16.0.1**, React **^19**
* **Banco**: PostgreSQL 16 (Docker)
* **Mensageria**: Azure Service Bus — fila **`orders`**
* **Infra**: Docker / Docker Compose
* **Migrations**: automáticas no startup (sem seed)

---

## 🚀 Como subir (1 comando)

```bash
docker compose up --build -d
```

* Frontend (UI): [http://localhost:3000/orders](http://localhost:3000/orders)
* API (Swagger): [http://localhost:5127/swagger/index.html](http://localhost:5127/swagger/index.html)
* Healthcheck: [http://localhost:5127/health](http://localhost:5127/health)
* PgAdmin: [http://localhost:5050/login?next=/](http://localhost:5050/login?next=/)

> Hoje apenas o `docker compose up --build -d` é necessário.

---

## 🔧 Configuração (.env)

Use o arquivo **`.env.example`** como base (copie para `.env` na raiz do projeto).
As variáveis incluem a string de conexão do Postgres e as credenciais do Service Bus.

* **Service Bus**

  * `ASB_CONNECTION`
  * `ASB_ENTITY=orders`
  * Propriedades do evento: `EventType=OrderCreated` e **⚠️ CorrelationId = OrderId** (revisar se está aplicado em todos os pontos).

* **Frontend → API**

  * Opcional: defina **`NEXT_PUBLIC_API_URL`** (ex.: `http://localhost:5127`) para apontar o Frontend para outra URL da API.
  * Se não informar, o frontend usa a configuração padrão do projeto.

---

## 🧭 Endpoints principais (API)

* `POST /orders` → Cria um novo pedido
* `GET  /orders` → Lista todos os pedidos
* `GET  /orders/{id}` → Detalhes de um pedido

### Health

* `GET /health` → checa API, DB e fila

---

## 🖥️ Frontend (Rotas)

* **Lista de pedidos**: `http://localhost:3000/orders`
* **Detalhes do pedido**: `http://localhost:3000/orders/{orderId}/details`

Feedback visual:

* **Toasts** em mudanças de status
* **Polling** a cada ~3s para refletir atualizações

---

## 📦 Outbox & Mensageria (transacional)

* **Tabela**: `outbox_messages`
  Campos: `Id`, `Type`, `Payload`, `OccurredOn`, `Processed` (bool), `ProcessedOn`, `Error` (opcional).
* **Transação única**: o **pedido** e a **mensagem de outbox** são gravados na **mesma transação**.
* **Publicação**: um dispatcher lê `outbox_messages` não processadas e publica na fila **`orders`**.
* **Idempotência**: o consumidor garante consistência usando chaves (ex.: `OrderId`) e controle de mensagens processadas.
* **Delete/clean-up**: o Worker marca como processado e realiza o delete (ou soft-delete) após confirmação de envio.

---

## 🤖 Worker (consumidor)

Fluxo ao consumir `OrderCreated`:

1. Atualiza o status do pedido para **Processando**
2. Aguarda ~5 segundos
3. Atualiza o status para **Finalizado**

Propriedades do evento:

* `EventType=OrderCreated`
* **⚠️ `CorrelationId = OrderId`** (deve estar presente e propagado)

---

## 🗺️ Diagramas

### Sequência (criação do pedido → processamento)

```mermaid
sequenceDiagram
    autonumber
    participant UI as Frontend (Next.js)
    participant API as API (.NET)
    participant DB as PostgreSQL
    participant OB as Outbox (DB)
    participant ASB as Azure Service Bus (orders)
    participant WK as Worker (.NET)

    UI->>API: POST /orders (cliente, produto, valor)
    activate API
    API->>DB: BEGIN TRANSACTION
    API->>DB: INSERT Order (Status=Pendente)
    API->>OB: INSERT OutboxMessage (EventType=OrderCreated, CorrelationId=OrderId)
    API->>DB: COMMIT
    deactivate API
    API-->>UI: 201 Created (OrderId)

    API->>ASB: Publica mensagem (EventType=OrderCreated, CorrelationId=OrderId)
    ASB-->>WK: Deliver OrderCreated

    WK->>DB: Update Order → Status=Processando
    WK->>WK: Delay ~5s
    WK->>DB: Update Order → Status=Finalizado
    WK->>OB: Marca OutboxMessage como processada / delete
```

### Implantação (Docker Compose)

```mermaid
graph LR
  subgraph Docker
    FE["Frontend<br/>:3000"] --- API["API (.NET)<br/>:5127"]
    API --- DB["Postgres<br/>:5432"]
    API --- ASB["Azure Service Bus"]
    WK["Worker (.NET)"] --- DB
    WK --- ASB
    PG["pgAdmin<br/>:5050"] --- DB
  end
```

---

## 📄 Sobre este desafio (PoC)

**Objetivo**
Desenvolver um sistema simples de gestão de pedidos, com criação, listagem e detalhes. A cada pedido criado, a API publica uma mensagem no **Azure Service Bus**; um **Worker** consome, processa e atualiza o status do pedido.

**Tecnologias obrigatórias**

* Backend: C# (.NET 7 ou superior) + Entity Framework + PostgreSQL
* Frontend: React + TailwindCSS
* Mensageria: Azure Service Bus
* Infraestrutura: Docker / Docker Compose

**Requisitos**

* API com endpoints: `POST /orders`, `GET /orders`, `GET /orders/{id}`
* Atributos do pedido: `id`, `cliente`, `produto`, `valor`, `status`, `data_criacao`
* Status: `Pendente → Processando → Finalizado` (ordem obrigatória)
* Persistir no Postgres e publicar no Service Bus ao criar um pedido
* Worker idempotente: ao consumir, marcar **Processando**, aguardar ~5s e marcar **Finalizado**
* Incluir `CorrelationId = OrderId` e `EventType = OrderCreated`
* Health checks para API, banco e fila

**Infra**

* Docker Compose com API, Worker, Frontend, PostgreSQL e PgAdmin
* `.env` para variáveis sensíveis
* Migrações automáticas
* Healthchecks no Compose

**Módulo opcional — IA/Analytics**
Endpoint/tela para perguntas em linguagem natural sobre os pedidos (ex.: “Pedidos hoje?”, “Tempo médio?”, “Pendentes agora?”, “Valor total finalizado no mês”). A LLM interpreta a pergunta, consulta o banco e responde com dados reais.

**Diferenciais técnicos (bônus)**

* Outbox Pattern (mensageria transacional)
* Histórico de status do pedido
* SignalR/WebSockets com fallback
* Testcontainers
* Tracing ponta-a-ponta
* Golden Tests
* Módulo IA/Analytics com LLM

**Critérios de avaliação**

* Qualidade do Código (30%), Mensageria & Confiabilidade (20%), Funcionalidade (15%), Documentação & DX (15%), Frontend & UX (10%), Testes Automatizados (10%)

---

## 🧪 Testes

* **Backend**:

  ```bash
  dotnet test backend/OrderService.sln
  ```
* **Cobertura** (opcional):

  ```bash
  dotnet test backend/OrderService.sln --collect:"XPlat Code Coverage"
  ```

> Testes que dependem do Service Bus podem ser condicionais à presença de variáveis de ambiente.

---

## 🧩 Troubleshooting

* **API não sobe**: verifique `DEFAULT_CONNECTION` no `.env`.
* **Mensageria**: confirme `ASB_CONNECTION` e se a fila **`orders`** existe.
* **Migrations**: aplicadas automaticamente no startup (ver logs da API).
* **Frontend não encontra API**: defina `NEXT_PUBLIC_API_URL` com `http://localhost:5127` e reinicie o frontend.

---

## ✅ Checklist de entrega

* [x] API com `POST/GET/GET {id}`
* [x] Outbox Pattern transacional
* [x] Worker consumindo fila e atualizando status
* [x] Healthchecks (API, DB, fila)
* [x] Frontend com listagem, detalhes, criação, toasts, polling
* [x] Docker Compose (API, Worker, Frontend, Postgres, PgAdmin)
* [x] `.env.example` incluído

> **Ponto de atenção**: confirmar a presença/propagação de **`CorrelationId = OrderId`** em toda a cadeia (**⚠️**).
