#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"

if [ -z "${ROLLBACK_BACKEND_IMAGE:-}" ] || [ -z "${ROLLBACK_FRONTEND_IMAGE:-}" ]; then
  echo "ERROR: No rollback target available (ROLLBACK_BACKEND_IMAGE / ROLLBACK_FRONTEND_IMAGE not set)."
  exit 1
fi

echo "==> Rolling back to:"
echo "    Backend:  $ROLLBACK_BACKEND_IMAGE"
echo "    Frontend: $ROLLBACK_FRONTEND_IMAGE"

export BACKEND_IMAGE="$ROLLBACK_BACKEND_IMAGE"
export FRONTEND_IMAGE="$ROLLBACK_FRONTEND_IMAGE"

echo "==> Pulling rollback images..."
docker compose -f "$COMPOSE_FILE" pull api frontend

echo "==> Rolling back: API first..."
docker compose -f "$COMPOSE_FILE" up -d --no-deps --wait api

echo "==> Rolling back: frontend..."
docker compose -f "$COMPOSE_FILE" up -d --no-deps --wait frontend

echo "==> Verifying rollback with smoke tests..."
if ! "$SCRIPT_DIR/smoke-test.sh"; then
  echo "ERROR: Rollback completed but smoke tests still failed."
  exit 1
fi

echo "==> Rollback successful."
