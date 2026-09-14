# ADR 0002 — Integração assíncrona via RabbitMQ/MassTransit (evento `LancamentoRegistrado`)

## Status
Aceita

## Contexto
Com dois serviços separados (ver [ADR 0001](0001-microsservicos-vs-monolito.md)), é preciso
definir como Lançamentos informa o Consolidado sobre novos lançamentos. Uma chamada HTTP síncrona
do Lançamentos para o Consolidado acoplaria a disponibilidade dos dois serviços exatamente ao
contrário do que o requisito não-funcional pede.

## Decisão
Lançamentos publica um evento de domínio (`LancamentoRegistrado`) em um exchange do **RabbitMQ**
via **MassTransit**; o Consolidado consome esse evento e atualiza sua própria projeção de saldo.
Nenhum dos serviços chama o outro diretamente por HTTP. Se o Consolidado cair, as mensagens ficam
retidas na fila (RabbitMQ com filas duráveis) e são processadas quando ele voltar — Lançamentos
nunca percebe a indisponibilidade do Consolidado.

RabbitMQ foi escolhido em vez de Kafka por ser mais simples de operar e rodar localmente
(um único container, sem necessidade de Zookeeper/KRaft ou de modelar partições), o que é
suficiente para o volume descrito no desafio (50 req/s de leitura, escrita presumivelmente menor).
Kafka seria mais indicado se o domínio precisasse de replay de eventos/Event Sourcing ou throughput
ordens de magnitude maior — ver seção de evoluções futuras no README.

## Consequências
- A versão do pacote MassTransit fica fixada em `8.*`: a partir da v9, o projeto passou a exigir
  uma chave de licença (gratuita, mas com cadastro em serviço de terceiros) para uso do transporte
  RabbitMQ. Fixar em `8.*` mantém a solução 100% open-source e reproduzível pelo avaliador sem
  nenhum cadastro externo.
- Precisa do padrão **Transactional Outbox** no lado de Lançamentos para evitar o problema de
  "dual write" entre o banco e o broker — ver [ADR 0004](0004-transactional-outbox.md).
- Precisa de **idempotência** no consumer do Consolidado, já que RabbitMQ garante *at-least-once
  delivery* (mensagens podem ser entregues mais de uma vez) — ver
  [ADR 0007](0007-idempotencia-e-incremento-atomico.md).
- A consolidação passa a ser **eventualmente consistente**: existe uma pequena janela entre o
  lançamento ser criado e o saldo consolidado refletir isso. Esse trade-off é aceitável e
  compatível com o requisito de até 5% de perda tolerável no Consolidado.
