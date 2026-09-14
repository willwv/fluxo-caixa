# Diagrama de Container (C4 — Nível 2)

Visão dos containers (unidades implantáveis) que compõem o sistema e como se comunicam entre si.
É a mesma informação do [solution-flow.md](./solution-flow.md), porém no nível de abstração correto
do C4: sem os componentes internos de cada serviço (esses ficam no
[diagrama de componentes](./c4-component.md), Nível 3).

```mermaid
flowchart TD
    Client(["[Pessoa]\nComerciante"])

    subgraph Sistema["Sistema de Fluxo de Caixa"]
        LancApi["[Container: ASP.NET Core / .NET 8]\nAPI de Lançamentos\nCRUD de lançamentos, autenticação JWT e outbox"]
        ConsApi["[Container: ASP.NET Core / .NET 8]\nAPI de Consolidado\nConsulta do saldo diário consolidado"]
        LancDB[("[Container: PostgreSQL]\nBanco de Lançamentos\nLancamentos, Outbox, Users")]
        Mq{{"[Container: RabbitMQ]\nMessage Broker\nEvento LancamentoRegistrado"}}
        ConsPrimary[("[Container: PostgreSQL Primary]\nBanco de Consolidado (escrita)\nSaldoDiario, ProcessedEvent")]
        ConsReplica[("[Container: PostgreSQL Replica]\nBanco de Consolidado (leitura)")]
        Redis[("[Container: Redis]\nCache\nCache-aside do relatório consolidado")]
    end

    Client -->|"HTTPS/JSON\nlogin, CRUD de lançamentos"| LancApi
    Client -->|"HTTPS/JSON\nconsulta saldo diário"| ConsApi

    LancApi -->|"EF Core / Npgsql"| LancDB
    LancApi -->|"AMQP (MassTransit)\npublica LancamentoRegistrado\n(via outbox)"| Mq

    Mq -->|"AMQP (MassTransit)\nconsome LancamentoRegistrado"| ConsApi
    ConsApi -->|"EF Core / Npgsql\nupsert atômico + idempotência"| ConsPrimary
    ConsApi -->|"EF Core / Npgsql\nleitura do relatório"| ConsReplica
    ConsApi -->|"StackExchange.Redis\ncache-aside"| Redis

    ConsPrimary -.->|"replicação física assíncrona"| ConsReplica
```

**Legenda**: retângulos são containers de aplicação, cilindros são bancos de dados/cache, o hexágono
é o broker de mensagens. Note que **não existe nenhuma seta direta entre a API de Lançamentos e a API
de Consolidado** — toda comunicação passa pelo RabbitMQ, sustentando a decisão dos
[ADR 0001](../adr/0001-microsservicos-vs-monolito.md) e
[ADR 0002](../adr/0002-integracao-assincrona-rabbitmq.md).
