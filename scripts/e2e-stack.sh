#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILES=(-f docker-compose.yml -f docker-compose.e2e.yml)
PROJECT_NAME="${COMPOSE_PROJECT_NAME:-basic-app-e2e}"
ACTION="${1:-up}"

cd "$ROOT_DIR"

case "$ACTION" in
  up)
    docker compose -p "$PROJECT_NAME" "${COMPOSE_FILES[@]}" up -d --build --wait postgres api frontend
    "$ROOT_DIR/scripts/smoke-test.sh"
    ;;
  down)
    docker compose -p "$PROJECT_NAME" "${COMPOSE_FILES[@]}" down -v --remove-orphans
    ;;
  *)
    echo "Usage: $0 {up|down}"
    exit 1
    ;;
esac
