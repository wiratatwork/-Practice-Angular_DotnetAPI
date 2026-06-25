#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
STATE_FILE="${STATE_FILE:-$HOME/basic_app/.last-good-deploy}"
DEPLOY_SHA="${DEPLOY_SHA:-}"

REGISTRY="${REGISTRY:-ghcr.io/wiratatwork}"
BACKEND_REPO="${BACKEND_REPO:-basic-app-backend}"
FRONTEND_REPO="${FRONTEND_REPO:-basic-app-frontend}"

ROLLBACK_BACKEND_IMAGE=""
ROLLBACK_FRONTEND_IMAGE=""

load_rollback_target() {
  if [ ! -f "$STATE_FILE" ]; then
    echo "==> No rollback state file at $STATE_FILE (first deploy or no prior success)."
    return 0
  fi

  # shellcheck disable=SC1090
  source "$STATE_FILE"

  if [ -n "${BACKEND_IMAGE:-}" ] && [ -n "${FRONTEND_IMAGE:-}" ]; then
    ROLLBACK_BACKEND_IMAGE="$BACKEND_IMAGE"
    ROLLBACK_FRONTEND_IMAGE="$FRONTEND_IMAGE"
    echo "==> Rollback target loaded from $STATE_FILE"
    echo "    Backend:  $ROLLBACK_BACKEND_IMAGE"
    echo "    Frontend: $ROLLBACK_FRONTEND_IMAGE"
  else
    echo "WARNING: State file exists but is missing BACKEND_IMAGE or FRONTEND_IMAGE."
  fi
}

write_state_file() {
  if [ -z "$DEPLOY_SHA" ]; then
    echo "WARNING: DEPLOY_SHA not set — skipping state file update."
    return 0
  fi

  local backend_image="${REGISTRY}/${BACKEND_REPO}:sha-${DEPLOY_SHA}"
  local frontend_image="${REGISTRY}/${FRONTEND_REPO}:sha-${DEPLOY_SHA}"

  cat >"$STATE_FILE" <<EOF
BACKEND_IMAGE=${backend_image}
FRONTEND_IMAGE=${frontend_image}
DEPLOY_SHA=${DEPLOY_SHA}
DEPLOYED_AT=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
EOF

  echo "==> Updated state file: $STATE_FILE (sha-${DEPLOY_SHA})"
}

deploy_prod_images() {
  unset BACKEND_IMAGE FRONTEND_IMAGE

  echo "==> Pulling latest API and frontend images..."
  docker compose -f "$COMPOSE_FILE" pull api frontend

  echo "==> Rolling update: API first..."
  docker compose -f "$COMPOSE_FILE" up -d --no-deps --wait api

  echo "==> Rolling update: frontend..."
  docker compose -f "$COMPOSE_FILE" up -d --no-deps --wait frontend
}

handle_smoke_failure() {
  echo "ERROR: Smoke tests failed after deploy."

  if [ -z "$ROLLBACK_BACKEND_IMAGE" ] || [ -z "$ROLLBACK_FRONTEND_IMAGE" ]; then
    echo "ERROR: No rollback target available — manual intervention required."
    exit 1
  fi

  echo "==> Attempting automatic rollback..."
  export ROLLBACK_BACKEND_IMAGE ROLLBACK_FRONTEND_IMAGE
  if ! "$SCRIPT_DIR/rollback-prod.sh"; then
    echo "ERROR: Automatic rollback failed."
    exit 1
  fi

  echo "ERROR: Deploy of new version failed; production restored to previous version."
  exit 1
}

load_rollback_target
deploy_prod_images

echo "==> Running smoke tests..."
if ! "$SCRIPT_DIR/smoke-test.sh"; then
  handle_smoke_failure
fi

write_state_file

echo "==> Pruning unused images..."
docker image prune -f

echo "==> Deploy complete."
