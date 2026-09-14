# ADR 0004 — Transactional Outbox no serviço de Lançamentos

## Status
Aceita

## Contexto
Ao criar um lançamento, o serviço precisa (a) gravar o lançamento no banco e (b) publicar um
evento no RabbitMQ para o Consolidado. Fazer essas duas operações separadamente cria o clássico
problema de **dual write**: se o banco confirma mas a publicação falha (broker fora do ar, timeout,
etc.), o lançamento existe mas o Consolidado nunca fica sabendo dele — quebrando a confiabilidade
que o desafio pede explicitamente.

## Decisão
Usar o padrão **Transactional Outbox**: ao criar um lançamento, o handler grava, na mesma
transação/`DbContext` do EF Core, tanto a entidade `Lancamento` quanto uma linha em
`outbox_messages` com o evento serializado. Um `BackgroundService` (`OutboxPublisherService`) lê
periodicamente as mensagens pendentes, publica no RabbitMQ via MassTransit e só então marca a
mensagem como processada. Se a publicação falhar, a mensagem continua pendente e é tentada de novo
no próximo ciclo.

Isso garante que a existência do lançamento e a futura publicação do evento sejam atômicas do
ponto de vista do banco — nunca existe um lançamento "órfão" sem o evento correspondente
pendente de envio.

## Consequências
- O envio do evento não é imediato (poll a cada ~2s) — mais uma fonte de consistência eventual,
  aceitável dado o requisito de tolerância a perda no Consolidado.
- O consumer do Consolidado deve ser idempotente, pois o outbox publisher garante *at-least-once*,
  não *exactly-once* — ver [ADR 0007](0007-idempotencia-e-incremento-atomico.md).
- Evolução natural (não implementada, por escopo/tempo): usar `FOR UPDATE SKIP LOCKED` ou
  particionar a tabela outbox para múltiplas instâncias do publisher não competirem pela mesma
  linha, caso o volume de escrita cresça muito.
