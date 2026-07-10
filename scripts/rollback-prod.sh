#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="${PROJECT_ROOT:-$HOME/basic_app}"
ENV_FILE="${ENV_FILE:-$PROJECT_ROOT/.env}"
COMPOSE_FILE="${COMPOSE_FILE:-$PROJECT_ROOT/deploy/docker-compose.slot.yml}"
PROD_COMPOSE_FILE="${PROD_COMPOSE_FILE:-$PROJECT_ROOT/docker-compose.prod.yml}"
PROD_COMPOSE_PROJECT="${PROD_COMPOSE_PROJECT:-basic_app}"
STATE_FILE="${STATE_FILE:-$PROJECT_ROOT/.last-good-deploy}"

NGINX_ACTIVE_LINK="${NGINX_ACTIVE_LINK:-/etc/nginx/conf.d/basic_app_active.conf}"
NGINX_BLUE_CONF="${NGINX_BLUE_CONF:-$PROJECT_ROOT/deploy/nginx/basic_app_active.blue.conf}"
NGINX_GREEN_CONF="${NGINX_GREEN_CONF:-$PROJECT_ROOT/deploy/nginx/basic_app_active.green.conf}"
SUDO_BIN="${SUDO_BIN:-sudo}"
PUBLIC_SMOKE_URL="${PUBLIC_SMOKE_URL:-http://127.0.0.1/}"

ACTIVE_SLOT=""
BACKEND_IMAGE=""
FRONTEND_IMAGE=""

slot_port_for() {
  local slot="$1"
  local kind="$2"
  if [ "$slot" = "blue" ]; then
    [ "$kind" = "api" ] && echo "5001" || echo "4201"
    return 0
  fi
  [ "$kind" = "api" ] && echo "5002" || echo "4202"
}

compose_project_for() {
  echo "basicapp-$1"
}

nginx_conf_for() {
  [ "$1" = "blue" ] && echo "$NGINX_BLUE_CONF" || echo "$NGINX_GREEN_CONF"
}

reload_or_start_nginx() {
  $SUDO_BIN nginx -t
  if $SUDO_BIN systemctl is-active --quiet nginx 2>/dev/null; then
    $SUDO_BIN systemctl reload nginx
  elif $SUDO_BIN pgrep -x nginx >/dev/null 2>&1; then
    $SUDO_BIN nginx -s reload
  else
    echo "==> Starting nginx..."
    if ! $SUDO_BIN systemctl start nginx 2>/dev/null; then
      $SUDO_BIN nginx
    fi
  fi
}

switch_nginx_slot() {
  local slot="$1"
  local slot_conf
  slot_conf="$(nginx_conf_for "$slot")"
  echo "==> Switching Nginx active slot to: $slot"
  $SUDO_BIN cp "$slot_conf" "$NGINX_ACTIVE_LINK"
  reload_or_start_nginx
}

ensure_shared_postgres() {
  if [ ! -f "$PROD_COMPOSE_FILE" ]; then
    echo "ERROR: Production compose file not found: $PROD_COMPOSE_FILE"
    exit 1
  fi

  echo "==> Ensuring shared PostgreSQL is running..."
  docker compose --env-file "$ENV_FILE" -f "$PROD_COMPOSE_FILE" -p "$PROD_COMPOSE_PROJECT" up -d postgres
}

if [ ! -f "$ENV_FILE" ]; then
  echo "ERROR: Environment file not found: $ENV_FILE"
  echo "       Create it with DB_CONNECTION and JWT_KEY (see .env.example)."
  exit 1
fi

if [ ! -f "$STATE_FILE" ]; then
  echo "ERROR: State file not found: $STATE_FILE"
  exit 1
fi

# shellcheck disable=SC1090
source "$STATE_FILE"

if [ -z "${ACTIVE_SLOT:-}" ] || [ -z "${BACKEND_IMAGE:-}" ] || [ -z "${FRONTEND_IMAGE:-}" ]; then
  echo "ERROR: State file missing ACTIVE_SLOT/BACKEND_IMAGE/FRONTEND_IMAGE"
  exit 1
fi

ROLLBACK_SLOT="$ACTIVE_SLOT"
ROLLBACK_API_PORT="$(slot_port_for "$ROLLBACK_SLOT" api)"
ROLLBACK_FRONTEND_PORT="$(slot_port_for "$ROLLBACK_SLOT" frontend)"
ROLLBACK_COMPOSE_PROJECT="$(compose_project_for "$ROLLBACK_SLOT")"

ensure_shared_postgres

echo "==> Re-deploying rollback images on slot: $ROLLBACK_SLOT"
BACKEND_IMAGE="$BACKEND_IMAGE" \
  FRONTEND_IMAGE="$FRONTEND_IMAGE" \
  APP_API_HOST_PORT="$ROLLBACK_API_PORT" \
  APP_FRONTEND_HOST_PORT="$ROLLBACK_FRONTEND_PORT" \
  docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" -p "$ROLLBACK_COMPOSE_PROJECT" pull api frontend

BACKEND_IMAGE="$BACKEND_IMAGE" \
  FRONTEND_IMAGE="$FRONTEND_IMAGE" \
  APP_API_HOST_PORT="$ROLLBACK_API_PORT" \
  APP_FRONTEND_HOST_PORT="$ROLLBACK_FRONTEND_PORT" \
  docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" -p "$ROLLBACK_COMPOSE_PROJECT" up -d --wait api frontend

switch_nginx_slot "$ROLLBACK_SLOT"

echo "==> Verifying rollback slot and public endpoint..."
"$SCRIPT_DIR/smoke-test.sh" \
  "http://127.0.0.1:${ROLLBACK_API_PORT}/health" \
  "http://127.0.0.1:${ROLLBACK_FRONTEND_PORT}/" \
  "$PUBLIC_SMOKE_URL"

echo "==> Rollback successful on slot: $ROLLBACK_SLOT"
