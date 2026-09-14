# ADR 0008 — docker-compose como forma de rodar localmente; Kubernetes como deploy-alvo de produção

## Status
Aceita

## Contexto
O desafio pede um README com "instruções claras de como rodar localmente" e, ao mesmo tempo,
espera que o candidato demonstre conhecimento de padrões de mercado para alta disponibilidade e
escalabilidade (dimensionamento horizontal, balanceamento de carga, failover). Docker Compose e
Kubernetes são ferramentas diferentes, com formatos de configuração incompatíveis entre si:
Compose orquestra containers em um único host; Kubernetes orquestra um cluster, com seus próprios
manifests (`Deployment`, `Service`, `HPA`, ...) aplicados via `kubectl`, não via `docker compose`.

## Decisão
- **docker-compose é a única forma de execução incluída no repositório** (`docker-compose.yml`).
  É o que o avaliador efetivamente roda para testar a aplicação, com um único comando
  (`docker-compose up`) e sem dependências além do Docker.
- **Kubernetes não é implementado** (não há pasta `/k8s` com manifests) — fica documentado aqui
  como o que seria usado em um ambiente de produção real:
  - **Deployment** com múltiplas réplicas de `consolidado-api` (e `lancamentos-api`), com
    `readinessProbe`/`livenessProbe` apontando para `/health` — dá failover e self-healing
    automáticos (pod não saudável é reiniciado/substituído).
  - **Service** do tipo `ClusterIP` (atrás de um `Ingress`) fazendo o balanceamento de carga entre
    os pods — substitui a necessidade de um reverse proxy manual.
  - **HorizontalPodAutoscaler (HPA)** em `consolidado-api`, escalando réplicas com base em CPU ou
    numa métrica customizada de requisições/segundo, diretamente amarrado à meta de 50 req/s com
    até 5% de perda do requisito não-funcional.
  - **PodDisruptionBudget** garantindo que atualizações/rolling updates não derrubem todas as
    réplicas de uma vez, reforçando a disponibilidade contínua do serviço de Lançamentos.

## Alternativas consideradas
- **Rodar Kubernetes localmente (kind/minikube) como forma oficial de execução**: rejeitada por
  aumentar a fricção para o avaliador (precisaria de mais ferramentas instaladas além do Docker)
  sem agregar valor de correção da aplicação em si.
- **Docker Swarm** (reaproveitando o mesmo `docker-compose.yml` via `docker stack deploy`): daria
  orquestração real (réplicas, rolling update) sem sair do ecossistema Docker, mas tem muito menos
  valor de portfólio do que citar e justificar Kubernetes, que é o padrão de mercado mencionado no
  próprio enunciado do desafio.

## Consequências
- Zero fricção para o avaliador rodar o projeto localmente.
- A demonstração de conhecimento de Kubernetes fica só na documentação (este ADR), não em código
  executável — trade-off explícito diante do tempo limitado do desafio.
