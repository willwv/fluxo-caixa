#!/usr/bin/env bash
# Valida o requisito não-funcional central do desafio:
# "O serviço de controle de lançamento não deve ficar indisponível se o sistema de consolidado
#  diário cair."
#
# Não dá pra testar isso só com k6 (ele não sabe derrubar containers) - este script orquestra
# docker compose + requisições HTTP: mantém uma carga constante em POST /lancamentos, derruba o
# Consolidado no meio do teste, e confere que a taxa de sucesso do Lançamentos não foi afetada.
#
# Como rodar (a partir da raiz do repo, com "docker-compose up" já de pé):
#   bash loadtest/resiliencia-consolidado-indisponivel.sh
# Requer um shell bash de verdade (Git Bash/WSL/Linux/macOS) - não roda em PowerShell nem cmd.exe.
set -euo pipefail

BASE_URL_LANCAMENTOS="${BASE_URL_LANCAMENTOS:-http://localhost:5001}"
SEED_USERNAME="${SEED_USERNAME:-comerciante}"
SEED_PASSWORD="${SEED_PASSWORD:-TrocarEssaSenha!123}"

DURACAO_TOTAL_S=40
DERRUBAR_APOS_S=10
FICAR_FORA_S=15
INTERVALO_REQUISICAO_S=0.5

echo "==> Login em Lançamentos..."
TOKEN=$(curl -s -X POST "$BASE_URL_LANCAMENTOS/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$SEED_USERNAME\",\"password\":\"$SEED_PASSWORD\"}" | \
  sed -n 's/.*"token":"\([^"]*\)".*/\1/p')

if [ -z "$TOKEN" ]; then
  echo "Falha no login - abortando." >&2
  exit 1
fi

RESULTADOS=$(mktemp)
trap 'rm -f "$RESULTADOS"' EXIT

echo "==> Gerando carga em POST /lancamentos por ${DURACAO_TOTAL_S}s (derrubando o Consolidado em t+${DERRUBAR_APOS_S}s por ${FICAR_FORA_S}s)..."

INICIO=$(date +%s)
FIM=$((INICIO + DURACAO_TOTAL_S))
I=0

while [ "$(date +%s)" -lt "$FIM" ]; do
  I=$((I + 1))
  DATA_LANCAMENTO=$(date -u -d "2000-01-01 + ${I} days" +%Y-%m-%d 2>/dev/null || date -u -v+${I}d -jf "%Y-%m-%d" "2000-01-01" +%Y-%m-%d)

  STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL_LANCAMENTOS/lancamentos" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "{\"data\":\"$DATA_LANCAMENTO\",\"tipo\":1,\"valor\":10.5,\"descricao\":\"Teste de resiliencia $I\"}" \
    --max-time 5 || echo "000")

  echo "$STATUS" >> "$RESULTADOS"
  sleep "$INTERVALO_REQUISICAO_S"
done &
LOOP_PID=$!

sleep "$DERRUBAR_APOS_S"
echo "==> [t+${DERRUBAR_APOS_S}s] Derrubando consolidado-api..."
docker compose stop consolidado-api >/dev/null

sleep "$FICAR_FORA_S"
echo "==> [t+$((DERRUBAR_APOS_S + FICAR_FORA_S))s] Religando consolidado-api..."
docker compose start consolidado-api >/dev/null

wait "$LOOP_PID"

TOTAL=$(wc -l < "$RESULTADOS" | tr -d ' ')
SUCESSO=$(grep -c '^201$' "$RESULTADOS" || true)
FALHA=$((TOTAL - SUCESSO))

echo ""
echo "==> Resultado:"
echo "    Total de requisições:  $TOTAL"
echo "    Sucesso (201):         $SUCESSO"
echo "    Falha:                 $FALHA"
if [ "$TOTAL" -gt 0 ]; then
  TAXA_SUCESSO=$(awk "BEGIN { printf \"%.2f\", ($SUCESSO/$TOTAL)*100 }")
  echo "    Taxa de sucesso:       ${TAXA_SUCESSO}%"
fi
echo ""
echo "    Códigos de status distintos observados:"
sort "$RESULTADOS" | uniq -c
