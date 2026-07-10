#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib-nginx.sh
source "$SCRIPT_DIR/lib-nginx.sh"

PROJECT_ROOT="${PROJECT_ROOT:-$HOME/basic_app}"
STATE_FILE="${STATE_FILE:-$PROJECT_ROOT/.last-good-deploy}"
TARGET_SLOT="${1:-}"

NGINX_BLUE_CONF="${NGINX_BLUE_CONF:-$PROJECT_ROOT/deploy/nginx/basic_app_active.blue.conf}"
NGINX_GREEN_CONF="${NGINX_GREEN_CONF:-$PROJECT_ROOT/deploy/nginx/basic_app_active.green.conf}"

if [ "$TARGET_SLOT" != "blue" ] && [ "$TARGET_SLOT" != "green" ]; then
  echo "Usage: $0 <blue|green>"
  exit 1
fi

if [ "$TARGET_SLOT" = "blue" ]; then
  TARGET_CONF="$NGINX_BLUE_CONF"
else
  TARGET_CONF="$NGINX_GREEN_CONF"
fi

echo "==> Switching live traffic to slot: $TARGET_SLOT"
install_nginx_active_config "$TARGET_CONF"
reload_or_start_nginx

touch "$STATE_FILE"
if grep -q "^ACTIVE_SLOT=" "$STATE_FILE"; then
  tmp_file="${STATE_FILE}.tmp"
  awk -v slot="$TARGET_SLOT" '
    BEGIN { updated = 0 }
    /^ACTIVE_SLOT=/ { print "ACTIVE_SLOT=" slot; updated = 1; next }
    { print }
    END { if (updated == 0) print "ACTIVE_SLOT=" slot }
  ' "$STATE_FILE" >"$tmp_file"
  mv "$tmp_file" "$STATE_FILE"
else
  printf "ACTIVE_SLOT=%s\n" "$TARGET_SLOT" >> "$STATE_FILE"
fi

echo "==> Traffic switched to $TARGET_SLOT"
