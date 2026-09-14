# Fluxo da solução

Visão geral do fluxo entre os dois serviços, cobrindo autenticação, criação de lançamento,
publicação/consumo assíncrono do evento e leitura do relatório consolidado.

```mermaid
flowchart TD
    Client["Comerciante (via Swagger UI)"]

    subgraph SLanc["Serviço de Lançamentos"]
        AuthEP["[POST] /auth/login"]
        UsersTbl[("Users\n(BCrypt hash + pepper)")]
        LancEP["[POST/GET] /lancamentos\n(requer Bearer JWT)"]
        LancDB[("PostgreSQL\nLancamentos + Outbox\n(mesma transação)")]
        OutboxWorker["Outbox Publisher\n(background worker)"]
    end

    subgraph MQ["RabbitMQ"]
        Exchange{{"Evento: LancamentoRegistrado"}}
    end

    subgraph SCons["Serviço de Consolidado"]
        Consumer["Consumer (MassTransit)"]
        Idempo[("Eventos processados\n(dedup / idempotência)")]
        PrimaryDB[("PostgreSQL Primary\nSaldoDiario\n(upsert atômico)")]
        ReplicaDB[("PostgreSQL\nRead Replica")]
        Cache[("Redis Cache")]
        ReportEP["[GET] /consolidado/{data}\n(requer Bearer JWT)"]
    end

    Client -->|"1. login"| AuthEP
    AuthEP --> UsersTbl
    AuthEP -->|"JWT"| Client

    Client -->|"2. novo lançamento"| LancEP
    LancEP --> LancDB
    OutboxWorker -->|"lê pendentes"| LancDB
    OutboxWorker -->|"3. publica evento"| Exchange

    Exchange -->|"4. consome"| Consumer
    Consumer --> Idempo
    Consumer -->|"5. incrementa saldo do dia"| PrimaryDB
    PrimaryDB -.->|"replicação assíncrona"| ReplicaDB

    Client -->|"6. consulta relatório"| ReportEP
    ReportEP -->|"cache hit"| Cache
    Cache -.->|"cache miss"| ReplicaDB
    ReplicaDB -.-> Cache
    ReportEP --> Client
```

**Legenda**: setas sólidas são chamadas síncronas (HTTP); setas tracejadas são fluxo assíncrono
(replicação de banco, cache miss). Os dois serviços só se comunicam pelo RabbitMQ (passos 3→4) —
nenhuma chamada direta entre eles, o que garante o requisito de "Lançamentos não cai se Consolidado
cair" (ver [ADR 0001](../adr/0001-microsservicos-vs-monolito.md) e
[ADR 0002](../adr/0002-integracao-assincrona-rabbitmq.md)).
