#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="${PROJECT_ROOT:-$HOME/basic_app}"
ENV_FILE="${ENV_FILE:-$PROJECT_ROOT/.env}"
COMPOSE_FILE="${COMPOSE_FILE:-$PROJECT_ROOT/deploy/docker-compose.slot.yml}"
PROD_COMPOSE_FILE="${PROD_COMPOSE_FILE:-$PROJECT_ROOT/docker-compose.prod.yml}"
PROD_COMPOSE_PROJECT="${PROD_COMPOSE_PROJECT:-basic_app}"
STATE_FILE="${STATE_FILE:-$PROJECT_ROOT/.last-good-deploy}"
DEPLOY_SHA="${DEPLOY_SHA:-}"

REGISTRY="${REGISTRY:-ghcr.io/wiratatwork}"
BACKEND_REPO="${BACKEND_REPO:-basic-app-backend}"
FRONTEND_REPO="${FRONTEND_REPO:-basic-app-frontend}"

NGINX_ACTIVE_LINK="${NGINX_ACTIVE_LINK:-/etc/nginx/conf.d/basic_app_active.conf}"
NGINX_BLUE_CONF="${NGINX_BLUE_CONF:-$PROJECT_ROOT/deploy/nginx/basic_app_active.blue.conf}"
NGINX_GREEN_CONF="${NGINX_GREEN_CONF:-$PROJECT_ROOT/deploy/nginx/basic_app_active.green.conf}"
SUDO_BIN="${SUDO_BIN:-sudo}"
PUBLIC_SMOKE_URL="${PUBLIC_SMOKE_URL:-http://127.0.0.1/}"

ACTIVE_SLOT="${ACTIVE_SLOT:-blue}"
PREV_ACTIVE_SLOT=""
ROLLBACK_BACKEND_IMAGE=""
ROLLBACK_FRONTEND_IMAGE=""

slot_port_for() {
  local slot="$1"
  local kind="$2"
  if [ "$slot" = "blue" ]; then
    [ "$kind" = "api" ] && echo "5001" || echo "4201"
    return 0
  fi
  [ "$kind" = "api" ] && echo "5002" || echo "4202"
}

inactive_slot_for() {
  [ "$1" = "blue" ] && echo "green" || echo "blue"
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
  # Copy into /etc/nginx so www-data can read config (home dir symlinks often fail).
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

load_state_file() {
  if [ ! -f "$STATE_FILE" ]; then
    echo "==> No state file at $STATE_FILE. Default active slot: $ACTIVE_SLOT"
    return 0
  fi

  # shellcheck disable=SC1090
  source "$STATE_FILE"

  if [ -n "${ACTIVE_SLOT:-}" ]; then
    PREV_ACTIVE_SLOT="$ACTIVE_SLOT"
  fi

  if [ -n "${BACKEND_IMAGE:-}" ] && [ -n "${FRONTEND_IMAGE:-}" ]; then
    ROLLBACK_BACKEND_IMAGE="$BACKEND_IMAGE"
    ROLLBACK_FRONTEND_IMAGE="$FRONTEND_IMAGE"
  fi

  echo "==> Loaded previous deploy state from $STATE_FILE"
  echo "    Active slot: ${PREV_ACTIVE_SLOT:-unknown}"
}

write_state_file() {
  local active_slot="$1"
  local backend_image="$2"
  local frontend_image="$3"

  cat >"$STATE_FILE" <<EOF
ACTIVE_SLOT=${active_slot}
BACKEND_IMAGE=${backend_image}
FRONTEND_IMAGE=${frontend_image}
DEPLOY_SHA=${DEPLOY_SHA}
DEPLOYED_AT=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
EOF

  echo "==> Updated state file: $STATE_FILE"
}

deploy_inactive_slot() {
  local target_slot="$1"
  local backend_image="$2"
  local frontend_image="$3"
  local api_port frontend_port compose_project

  api_port="$(slot_port_for "$target_slot" api)"
  frontend_port="$(slot_port_for "$target_slot" frontend)"
  compose_project="$(compose_project_for "$target_slot")"

  echo "==> Deploying candidate to slot: $target_slot"
  BACKEND_IMAGE="$backend_image" \
    FRONTEND_IMAGE="$frontend_image" \
    APP_API_HOST_PORT="$api_port" \
    APP_FRONTEND_HOST_PORT="$frontend_port" \
    docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" -p "$compose_project" pull api frontend

  BACKEND_IMAGE="$backend_image" \
    FRONTEND_IMAGE="$frontend_image" \
    APP_API_HOST_PORT="$api_port" \
    APP_FRONTEND_HOST_PORT="$frontend_port" \
    docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" -p "$compose_project" up -d --wait api frontend

  echo "==> Running smoke tests on candidate slot ($target_slot)..."
  "$SCRIPT_DIR/smoke-test.sh" \
    "http://127.0.0.1:${api_port}/health" \
    "http://127.0.0.1:${frontend_port}/"
}

handle_smoke_failure() {
  local previously_active="$1"
  echo "ERROR: Candidate slot smoke tests failed."
  if [ -n "$previously_active" ]; then
    echo "==> Keeping current production slot unchanged: $previously_active"
    switch_nginx_slot "$previously_active"
  fi
  exit 1
}

if [ -z "$DEPLOY_SHA" ]; then
  echo "ERROR: DEPLOY_SHA is required."
  exit 1
fi

if [ ! -f "$ENV_FILE" ]; then
  echo "ERROR: Environment file not found: $ENV_FILE"
  echo "       Create it with DB_CONNECTION and JWT_KEY (see .env.example)."
  exit 1
fi

mkdir -p "$(dirname "$STATE_FILE")"
load_state_file

if [ -n "$PREV_ACTIVE_SLOT" ]; then
  ACTIVE_SLOT="$PREV_ACTIVE_SLOT"
fi
INACTIVE_SLOT="$(inactive_slot_for "$ACTIVE_SLOT")"

NEW_BACKEND_IMAGE="${REGISTRY}/${BACKEND_REPO}:sha-${DEPLOY_SHA}"
NEW_FRONTEND_IMAGE="${REGISTRY}/${FRONTEND_REPO}:sha-${DEPLOY_SHA}"

ensure_shared_postgres

if ! deploy_inactive_slot "$INACTIVE_SLOT" "$NEW_BACKEND_IMAGE" "$NEW_FRONTEND_IMAGE"; then
  handle_smoke_failure "$ACTIVE_SLOT"
fi

switch_nginx_slot "$INACTIVE_SLOT"

echo "==> Verifying public endpoint after cutover..."
if ! "$SCRIPT_DIR/smoke-test.sh" "$PUBLIC_SMOKE_URL"; then
  echo "ERROR: Post-cutover smoke test failed, rolling back traffic to $ACTIVE_SLOT"
  switch_nginx_slot "$ACTIVE_SLOT"
  exit 1
fi

write_state_file "$INACTIVE_SLOT" "$NEW_BACKEND_IMAGE" "$NEW_FRONTEND_IMAGE"

echo "==> Pruning unused images..."
docker image prune -f

echo "==> Blue/green deploy complete. Active slot: $INACTIVE_SLOT"
