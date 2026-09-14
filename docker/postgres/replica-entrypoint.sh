#!/bin/sh
set -e

if [ -z "$(ls -A "$PGDATA" 2>/dev/null)" ]; then
  echo "PGDATA vazio - clonando do primary (${PRIMARY_HOST}) via pg_basebackup..."
  export PGPASSWORD="$REPLICATION_PASSWORD"

  until pg_basebackup -h "$PRIMARY_HOST" -p 5432 -D "$PGDATA" -U "$REPLICATION_USER" -Fp -Xs -R; do
    echo "Primary ainda não disponível, tentando novamente em 2s..."
    sleep 2
  done

  # pg_basebackup -R não grava a senha em primary_conninfo; garantimos aqui que o walreceiver
  # da réplica consegue reconectar sozinho no primary para streaming contínuo.
  echo "primary_conninfo = 'host=${PRIMARY_HOST} port=5432 user=${REPLICATION_USER} password=${REPLICATION_PASSWORD} sslmode=prefer'" >> "$PGDATA/postgresql.auto.conf"

  chmod 0700 "$PGDATA"
fi

exec docker-entrypoint.sh postgres
