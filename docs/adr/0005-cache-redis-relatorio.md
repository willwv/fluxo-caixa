# ADR 0005 — Cache Redis (cache-aside) na frente do relatório de saldo diário

## Status
Aceita

## Contexto
O endpoint de relatório do Consolidado (`GET /consolidado/{data}`) recebe até 50 req/s em pico,
segundo o requisito não-funcional do desafio, com até 5% de perda tolerável. A leitura da réplica
(ver [ADR 0003](0003-database-per-service-e-read-replica.md)) já ajuda a escalar, mas grande parte
dessas requisições provavelmente consulta o mesmo dia (o dia corrente), tornando o resultado um
ótimo candidato a cache.

## Decisão
Implementar **cache-aside** com Redis: o endpoint de relatório primeiro consulta o Redis; em caso
de *miss*, consulta a réplica e grava o resultado no Redis com um **TTL curto (5 segundos, por
padrão configurável)**. Isso tira a maior parte da carga repetida do banco sob picos de tráfego, ao
custo de uma janela pequena e assumida de *staleness* (o relatório pode refletir o saldo com até
~5s de atraso em relação ao banco).

O cache não é invalidado proativamente quando um novo lançamento é consolidado (o que exigiria
acoplar o caminho de escrita do consumer ao formato da chave de cache do caminho de leitura);
prefere-se manter os dois caminhos desacoplados e aceitar a janela de TTL curto como trade-off
deliberado, coerente com a tolerância a perda/atraso já aceita em outras partes do desenho.

## Consequências
- Reduz drasticamente as consultas repetidas à réplica sob carga, sem precisar de mais réplicas de
  leitura para atingir os 50 req/s.
- Introduz mais uma fonte (pequena, limitada a 5s) de consistência eventual no relatório.
- Se `Redis` cair, a leitura simplesmente cai para a réplica a cada request (sem cache) — não há
  ponto único de falha para a disponibilidade do relatório, só perda do ganho de performance.
