#!/usr/bin/env bash
set -euo pipefail

SMOKE_MAX_ATTEMPTS="${SMOKE_MAX_ATTEMPTS:-5}"
SMOKE_RETRY_DELAY="${SMOKE_RETRY_DELAY:-5}"
SMOKE_EXPECTED_STATUS="${SMOKE_EXPECTED_STATUS:-200}"

if [ "$#" -gt 0 ]; then
  SMOKE_URLS=("$@")
else
  SMOKE_URLS=(
    "http://127.0.0.1:5001/health"
    "http://127.0.0.1:4200/"
  )
fi

check_url() {
  local url="$1"
  local attempt status

  for ((attempt = 1; attempt <= SMOKE_MAX_ATTEMPTS; attempt++)); do
    status="$(curl -fsS -o /dev/null -w '%{http_code}' "$url" 2>/dev/null || echo "000")"

    if [ "$status" = "$SMOKE_EXPECTED_STATUS" ]; then
      echo "  OK $url (HTTP $status, attempt $attempt/$SMOKE_MAX_ATTEMPTS)"
      return 0
    fi

    echo "  FAIL $url (HTTP $status, attempt $attempt/$SMOKE_MAX_ATTEMPTS)"

    if [ "$attempt" -lt "$SMOKE_MAX_ATTEMPTS" ]; then
      sleep "$SMOKE_RETRY_DELAY"
    fi
  done

  return 1
}

echo "==> Running smoke tests (${SMOKE_MAX_ATTEMPTS} attempts, ${SMOKE_RETRY_DELAY}s delay)..."

failed=0
for url in "${SMOKE_URLS[@]}"; do
  if ! check_url "$url"; then
    failed=1
  fi
done

if [ "$failed" -ne 0 ]; then
  echo "==> Smoke tests failed."
  exit 1
fi

echo "==> Smoke tests passed."
exit 0
