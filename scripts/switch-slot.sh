#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="${PROJECT_ROOT:-$HOME/basic_app}"
STATE_FILE="${STATE_FILE:-$PROJECT_ROOT/.last-good-deploy}"
TARGET_SLOT="${1:-}"

NGINX_ACTIVE_LINK="${NGINX_ACTIVE_LINK:-/etc/nginx/conf.d/basic_app_active.conf}"
NGINX_BLUE_CONF="${NGINX_BLUE_CONF:-$PROJECT_ROOT/deploy/nginx/basic_app_active.blue.conf}"
NGINX_GREEN_CONF="${NGINX_GREEN_CONF:-$PROJECT_ROOT/deploy/nginx/basic_app_active.green.conf}"
SUDO_BIN="${SUDO_BIN:-sudo}"

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
$SUDO_BIN cp "$TARGET_CONF" "$NGINX_ACTIVE_LINK"
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
