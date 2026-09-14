# Diagrama de Componentes (C4 — Nível 3)

Detalha os componentes internos de cada container de aplicação do
[diagrama de container](./c4-container.md) (Nível 2). Um diagrama por serviço, já que cada um é um
container/processo independente e não faz sentido combiná-los num único diagrama de componentes.

## API de Lançamentos

```mermaid
flowchart TD
    Client(["Comerciante"])

    subgraph LancApi["Container: API de Lançamentos"]
        AuthCtrl["[Controller]\nAuthController\nPOST /auth/login"]
        LancCtrl["[Controller]\nLancamentosController\nPOST/GET /lancamentos"]

        ValidBehavior["[MediatR Pipeline Behavior]\nValidationBehavior\nFluentValidation antes do handler"]

        LoginHandler["[MediatR Handler]\nLoginHandler\nautentica e emite JWT"]
        CriarHandler["[MediatR Handler]\nCriarLancamentoHandler\ncria lançamento + grava outbox\n(mesma transação)"]
        ObterHandler["[MediatR Handler]\nObterLancamentosHandler\nconsulta lançamentos"]

        UserRepo["[Repository]\nUserRepository"]
        PwdHasher["[Component]\nBCryptPasswordHasher\nBCrypt + pepper"]
        JwtGen["[Component]\nJwtTokenGenerator"]

        LancRepo["[Repository]\nLancamentoRepository"]
        OutboxWriter["[Component]\nOutboxWriter\ngrava evento na outbox"]
        UoW["[Component]\nUnitOfWork\ncommit atômico"]

        DbCtx[("[EF Core DbContext]\nLancamentosDbContext")]

        OutboxPublisher["[Background Worker]\nOutboxPublisherService\npolling a cada 2s, lote de 50"]
    end

    LancDB[("PostgreSQL\nLancamentos + Outbox + Users")]
    Mq{{"RabbitMQ"}}

    Client -->|"HTTP"| AuthCtrl
    Client -->|"HTTP + Bearer JWT"| LancCtrl

    AuthCtrl -->|"LoginCommand"| ValidBehavior
    LancCtrl -->|"CriarLancamentoCommand /\nObterLancamentosQuery"| ValidBehavior
    ValidBehavior --> LoginHandler
    ValidBehavior --> CriarHandler
    ValidBehavior --> ObterHandler

    LoginHandler --> UserRepo
    LoginHandler --> PwdHasher
    LoginHandler --> JwtGen

    CriarHandler --> LancRepo
    CriarHandler --> OutboxWriter
    CriarHandler --> UoW
    ObterHandler --> LancRepo

    UserRepo --> DbCtx
    LancRepo --> DbCtx
    OutboxWriter --> DbCtx
    UoW --> DbCtx
    DbCtx --> LancDB

    OutboxPublisher -->|"lê pendentes"| DbCtx
    OutboxPublisher -->|"publica evento"| Mq
```

## API de Consolidado

```mermaid
flowchart TD
    Client(["Comerciante"])
    Mq{{"RabbitMQ"}}

    subgraph ConsApi["Container: API de Consolidado"]
        Consumer["[MassTransit Consumer]\nLancamentoRegistradoConsumer\ntraduz evento em AplicarLancamentoCommand"]
        ConsCtrl["[Controller]\nConsolidadoController\nGET /consolidado/{data}"]

        AplicarHandler["[MediatR Handler]\nAplicarLancamentoHandler"]
        ObterHandler["[MediatR Handler]\nObterSaldoDiarioHandler"]

        WriteRepo["[Repository]\nSaldoDiarioWriteRepository\nupsert atômico + idempotência\n(ProcessedEvent)"]
        ReadRepo["[Repository]\nSaldoDiarioReadRepository\ncache-aside com Redis"]

        WriteDbCtx[("[EF Core DbContext]\nConsolidadoWriteDbContext")]
        ReadDbCtx[("[EF Core DbContext]\nConsolidadoReadDbContext")]
    end

    PrimaryDB[("PostgreSQL Primary\nSaldoDiario, ProcessedEvent")]
    ReplicaDB[("PostgreSQL Replica")]
    Redis[("Redis")]

    Mq -->|"consome LancamentoRegistrado"| Consumer
    Consumer --> AplicarHandler
    AplicarHandler --> WriteRepo
    WriteRepo --> WriteDbCtx
    WriteDbCtx --> PrimaryDB

    Client -->|"HTTP + Bearer JWT"| ConsCtrl
    ConsCtrl --> ObterHandler
    ObterHandler --> ReadRepo
    ReadRepo -->|"cache hit"| Redis
    ReadRepo -->|"cache miss"| ReadDbCtx
    ReadDbCtx --> ReplicaDB
    ReadRepo -.->|"popula cache"| Redis
```

**Legenda**: controllers apenas traduzem HTTP/mensageria em comandos/queries do MediatR; toda regra de
negócio fica nos handlers e nos repositórios de infraestrutura. `SaldoDiarioWriteRepository` nunca é
chamado pelo lado de leitura, e `SaldoDiarioReadRepository` nunca toca o primary — a separação reflete
o [ADR 0003](../adr/0003-database-per-service-e-read-replica.md).
