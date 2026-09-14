# ADR 0001 — Microsserviços (Lançamentos e Consolidado) em vez de monólito

## Status
Aceita

## Contexto
O desafio pede dois serviços de negócio (controle de lançamentos e consolidado diário) e é
explícito num requisito não-funcional: **o serviço de Lançamentos não pode ficar indisponível se
o serviço de Consolidado cair**. O Consolidado também tem um perfil de carga e escalabilidade
diferente do de Lançamentos (50 req/s de leitura em pico, com até 5% de perda tolerável).

## Decisão
Implementar como **dois serviços independentes** (`Lancamentos` e `Consolidado`), cada um com seu
próprio processo, banco de dados e ciclo de deploy, comunicando-se de forma assíncrona (ver
[ADR 0002](0002-integracao-assincrona-rabbitmq.md)). Não foi adotado um monólito modular porque a
premissa central do desafio — isolar a falha de um serviço do outro — é resolvida de forma nativa
por essa separação física, sem precisar simular esse isolamento dentro de um único processo.

## Alternativas consideradas
- **Monólito modular** com módulos desacoplados via eventos in-process: mais simples e rápido de
  implementar, mas não isola verdadeiramente a disponibilidade dos dois serviços (uma falha no
  processo único afeta os dois módulos) e não demonstra tão bem os padrões de integração pedidos
  pelo desafio.
- **SOA/serverless**: descartados por não se encaixarem no escopo e tempo do desafio, e por não
  trazerem benefício adicional relevante sobre microsserviços para este domínio.

## Consequências
- Mais artefatos de infraestrutura para rodar localmente (2 APIs, 3 bancos, broker, cache) — ver
  [ADR 0007](0007-docker-compose-e-kubernetes.md) sobre como isso é mitigado.
- Cada serviço pode escalar e falhar independentemente, o que é exatamente o requisito que motivou
  a escolha.
- Precisa de um contrato de integração explícito e versionado entre os serviços (evento
  `LancamentoRegistrado`, em `FluxoCaixa.Contracts`).
