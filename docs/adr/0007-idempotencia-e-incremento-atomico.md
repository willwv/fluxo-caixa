# ADR 0007 — Consolidação por incremento atômico + idempotência (sem garantir ordem)

## Status
Aceita

## Contexto
O saldo diário é mantido por **soma incremental** (cada evento consumido incrementa
`total_creditos`/`total_debitos` do dia), em vez de recalcular somando todos os lançamentos do dia
a cada leitura — necessário para sustentar 50 req/s de leitura sem reprocessar tudo. Isso levanta
duas questões: **ordem de processamento** e **entregas duplicadas**.

## Decisão
- **Ordem não é garantida nem necessária.** A soma é uma operação comutativa e associativa: o
  resultado final independe da ordem em que os eventos do dia são aplicados. Por isso, não é usado
  nenhum mecanismo de particionamento por chave (partition key/consistent hashing) para ordenar
  o processamento — isso resolveria um problema que este domínio não tem, ao custo de mais
  complexidade operacional (avaliado e conscientemente descartado).
- **Idempotência**: cada evento carrega um `EventId` único. Antes de aplicar o incremento, o
  consumer tenta inserir esse `EventId` numa tabela `processed_events` (`INSERT ... ON CONFLICT DO
  NOTHING`); se a inserção não afetar nenhuma linha, o evento já foi processado antes (entrega
  duplicada do RabbitMQ) e é ignorado.
- **Incremento atômico**: o incremento do saldo do dia é feito com um `UPSERT` SQL
  (`INSERT ... ON CONFLICT (data) DO UPDATE SET total = total + EXCLUDED.total`), executado no
  banco, em vez do padrão "carregar a linha, somar em memória, salvar" — que sofreria *lost update*
  sob múltiplos consumers concorrentes escrevendo no mesmo dia. A marcação de idempotência e o
  incremento acontecem na mesma transação: se o processo falhar entre os dois passos, a transação
  inteira é revertida (nada fica marcado como processado sem o efeito colateral correspondente).

## Consequências
- Simplicidade: nenhuma infraestrutura extra de particionamento/consistent hashing.
- Se o domínio evoluir para precisar de **ordem estrita** (por exemplo, um extrato mostrando saldo
  acumulado após cada transação individual, não só o total do dia), essa decisão precisaria ser
  revisitada.
