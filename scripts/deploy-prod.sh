#!/usr/bin/env bash
set -euo pipefail

COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"

echo "==> Pulling latest API and frontend images..."
docker compose -f "$COMPOSE_FILE" pull api frontend

echo "==> Rolling update: API first..."
docker compose -f "$COMPOSE_FILE" up -d --no-deps --wait api

echo "==> Rolling update: frontend..."
docker compose -f "$COMPOSE_FILE" up -d --no-deps --wait frontend

echo "==> Pruning unused images..."
docker image prune -f

echo "==> Deploy complete."
