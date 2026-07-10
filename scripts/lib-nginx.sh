#!/usr/bin/env bash

NGINX_ACTIVE_LINK="${NGINX_ACTIVE_LINK:-/etc/nginx/conf.d/basic_app_active.conf}"
NGINX_SITES_AVAILABLE="${NGINX_SITES_AVAILABLE:-/etc/nginx/sites-available/basic_app_active.conf}"
NGINX_SITES_ENABLED="${NGINX_SITES_ENABLED:-/etc/nginx/sites-enabled/basic_app_active.conf}"
SUDO_BIN="${SUDO_BIN:-sudo}"

install_nginx_active_config() {
  local slot_conf="$1"

  # Use a single include path only. Writing to both conf.d and sites-enabled
  # makes nginx load duplicate upstream blocks and fail nginx -t.
  $SUDO_BIN mkdir -p "$(dirname "$NGINX_ACTIVE_LINK")"
  $SUDO_BIN cp "$slot_conf" "$NGINX_ACTIVE_LINK"

  # Clean up legacy installs that used sites-enabled.
  $SUDO_BIN rm -f "$NGINX_SITES_ENABLED" "$NGINX_SITES_AVAILABLE"
  $SUDO_BIN rm -f /etc/nginx/sites-enabled/default
}

reload_or_start_nginx() {
  if ! command -v nginx >/dev/null 2>&1; then
    echo "ERROR: nginx is not installed. Install it on the VM (e.g. sudo apt install nginx)."
    return 1
  fi

  $SUDO_BIN nginx -t

  if $SUDO_BIN pgrep -x nginx >/dev/null 2>&1; then
    echo "==> Reloading nginx..."
    $SUDO_BIN nginx -s reload
  else
    echo "==> Starting nginx..."
    if ! $SUDO_BIN systemctl start nginx 2>/dev/null; then
      $SUDO_BIN nginx
    fi
  fi

  local attempt
  for attempt in 1 2 3 4 5 6 7 8 9 10; do
    if curl -fsS -o /dev/null --connect-timeout 2 http://127.0.0.1/ 2>/dev/null; then
      echo "==> Nginx is responding on port 80."
      return 0
    fi
    if $SUDO_BIN ss -tln 2>/dev/null | grep -q ':80 '; then
      echo "==> Port 80 is listening."
      return 0
    fi
    sleep 1
  done

  echo "ERROR: Nginx is not listening on port 80 after reload/start."
  echo "       Check: sudo nginx -t && sudo ss -tlnp | grep ':80'"
  return 1
}
