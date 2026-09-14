# ADR 0003 — Database per service, com primary + read replica no Consolidado

## Status
Aceita

## Contexto
Cada microsserviço precisa de autonomia de dados para não recriar o acoplamento que a mensageria
assíncrona já resolve (ver [ADR 0002](0002-integracao-assincrona-rabbitmq.md)). Além disso, o
Consolidado tem um requisito de leitura pesada e específico: 50 req/s em pico, com até 5% de perda
tolerável.

## Decisão
- **Database per service**: Lançamentos e Consolidado têm bancos PostgreSQL completamente
  separados. O Consolidado nunca lê diretamente do banco de Lançamentos (isso recriaria
  acoplamento de schema entre os dois bounded contexts).
- Dentro do Consolidado, o banco é um par **primary + read replica** (replicação física do
  Postgres). O consumer do RabbitMQ escreve no primary (via incremento atômico, ver
  [ADR 0007](0007-idempotencia-e-incremento-atomico.md)); o endpoint de relatório lê da réplica.
  Isso escala a leitura horizontalmente exatamente onde a carga está (o relatório), sem afetar a
  escrita, e isola o caminho crítico de leitura de uma falha do primary: como o endpoint de
  relatório nunca conecta no primary, uma queda dele não derruba a consulta de saldo — a réplica
  continua respondendo com os dados que já tinha até o último WAL recebido antes da falha.
  Importante não confundir isso com failover completo: a réplica não é promovida a gravável
  automaticamente (não há Patroni/repmgr/pg_auto_failover no desenho), então enquanto o primary
  estiver fora, novos lançamentos não são refletidos no saldo (mas não se perdem: ficam retidos nas
  filas duráveis do RabbitMQ até o primary voltar, mesmo mecanismo do
  [ADR 0004](0004-transactional-outbox.md), aplicado aqui do lado do consumer). Ou seja, leitura e
  escrita sobrevivem à queda do primary por dois mecanismos diferentes — réplica para leitura,
  fila durável para escrita —, não por um failover único do banco.
- Um cache Redis (cache-aside, TTL curto) fica na frente da réplica para absorver ainda mais carga
  de leitura — ver [ADR 0005](0005-cache-redis-relatorio.md).

## Alternativas consideradas
- **Consolidado ler direto do banco de Lançamentos (com uma réplica dele)**: rejeitada em favor da
  leitura do evento — misturaria dois mecanismos de integração (mensageria + banco compartilhado)
  para o mesmo propósito, e acoplaria o schema interno de Lançamentos ao Consolidado.
- **Um único banco sem réplica**: mais simples, mas não demonstra a estratégia de escalabilidade de
  leitura pedida explicitamente pelo desafio ("considere... balanceamento de carga").

## Consequências
- Mais um componente de infraestrutura para operar (réplica). No docker-compose local, a réplica
  usa a imagem oficial `postgres:16-alpine` com um entrypoint customizado
  (`docker/postgres/replica-entrypoint.sh`) que roda `pg_basebackup` contra o primary no primeiro
  start e configura `primary_conninfo` para streaming replication contínua — optou-se por isso em
  vez da imagem `bitnami/postgresql` porque o catálogo gratuito da Bitnami deixou de publicar tags
  de versão fixa (só `latest`), o que não é aceitável para um ambiente reprodutível.
- Pequena janela de lag de replicação entre o primary e a réplica (tipicamente sub-segundo) — mais
  uma fonte de consistência eventual, coerente com o resto do desenho.
