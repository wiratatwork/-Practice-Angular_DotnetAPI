#!/usr/bin/env bash
set -euo pipefail # หยุดการทำงานถ้ามี error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)" # หาตำแหน่งของไฟล์ deploy-prod.sh
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}" # หาตำแหน่งของไฟล์ docker-compose.prod.yml
STATE_FILE="${STATE_FILE:-$HOME/basic_app/.last-good-deploy}" # หาตำแหน่งของไฟล์ .last-good-deploy
DEPLOY_SHA="${DEPLOY_SHA:-}" # ตั้งค่า DEPLOY_SHA เป็นค่าว่าง

REGISTRY="${REGISTRY:-ghcr.io/wiratatwork}" # ตั้งค่า REGISTRY เป็น ghcr.io/wiratatwork
BACKEND_REPO="${BACKEND_REPO:-basic-app-backend}" # ตั้งค่า BACKEND_REPO เป็น basic-app-backend
FRONTEND_REPO="${FRONTEND_REPO:-basic-app-frontend}" # ตั้งค่า FRONTEND_REPO เป็น basic-app-frontend

ROLLBACK_BACKEND_IMAGE="" # ตั้งค่า ROLLBACK_BACKEND_IMAGE เป็นค่าว่าง
ROLLBACK_FRONTEND_IMAGE="" # ตั้งค่า ROLLBACK_FRONTEND_IMAGE เป็นค่าว่าง

load_rollback_target() { # โหลดข้อมูลจากไฟล์ .last-good-deploy
  if [ ! -f "$STATE_FILE" ]; then # ถ้าไฟล์ .last-good-deploy ไม่มี
    echo "==> No rollback state file at $STATE_FILE (first deploy or no prior success)." # แสดงข้อความว่าไฟล์ .last-good-deploy ไม่มี
    return 0 # ออกจากฟังก์ชัน
  fi

  # shellcheck disable=SC1090
  source "$STATE_FILE" # อ่านข้อมูลจากไฟล์ .last-good-deploy
  # ตัวอย่างข้อมูลในไฟล์ .last-good-deploy
  # BACKEND_IMAGE=ghcr.io/wiratatwork/basic-app-backend:sha-abc1234
  # FRONTEND_IMAGE=ghcr.io/wiratatwork/basic-app-frontend:sha-abc1234
  # DEPLOY_SHA=abc1234
  # DEPLOYED_AT=2026-06-25T12:00:00Z

  if [ -n "${BACKEND_IMAGE:-}" ] && [ -n "${FRONTEND_IMAGE:-}" ]; then # ถ้า BACKEND_IMAGE และ FRONTEND_IMAGE ไม่เป็นค่าว่าง
    ROLLBACK_BACKEND_IMAGE="$BACKEND_IMAGE" # ตั้งค่า ROLLBACK_BACKEND_IMAGE เป็น BACKEND_IMAGE
    ROLLBACK_FRONTEND_IMAGE="$FRONTEND_IMAGE" # ตั้งค่า ROLLBACK_FRONTEND_IMAGE เป็น FRONTEND_IMAGE
    echo "==> Rollback target loaded from $STATE_FILE" # แสดงข้อความว่าโหลดข้อมูลจากไฟล์ .last-good-deploy
    echo "    Backend:  $ROLLBACK_BACKEND_IMAGE" # แสดงข้อความว่า BACKEND_IMAGE
    echo "    Frontend: $ROLLBACK_FRONTEND_IMAGE" # แสดงข้อความว่า FRONTEND_IMAGE
  else
    echo "WARNING: State file exists but is missing BACKEND_IMAGE or FRONTEND_IMAGE." # แสดงข้อความว่าไฟล์ .last-good-deploy มีข้อมูลข้าม BACKEND_IMAGE หรือ FRONTEND_IMAGE
    return 1 # ออกจากฟังก์ชัน
  fi
}

write_state_file() { # บันทึกข้อมูลลงไฟล์ .last-good-deploy
  if [ -z "$DEPLOY_SHA" ]; then # ถ้า DEPLOY_SHA ว่าง
    echo "WARNING: DEPLOY_SHA not set — skipping state file update." # แสดงข้อความว่า DEPLOY_SHA ไม่ตั้งค่า — ข้ามการบันทึกข้อมูลลงไฟล์ .last-good-deploy
    return 0 # ออกจากฟังก์ชัน
  fi

  local backend_image="${REGISTRY}/${BACKEND_REPO}:sha-${DEPLOY_SHA}" # ตั้งค่า backend_image เป็น REGISTRY/BACKEND_REPO:sha-DEPLOY_SHA
  local frontend_image="${REGISTRY}/${FRONTEND_REPO}:sha-${DEPLOY_SHA}" # ตั้งค่า frontend_image เป็น REGISTRY/FRONTEND_REPO:sha-DEPLOY_SHA

  # บันทึกข้อมูลลงไฟล์ .last-good-deploy
  cat >"$STATE_FILE" <<EOF
BACKEND_IMAGE=${backend_image}
FRONTEND_IMAGE=${frontend_image}
DEPLOY_SHA=${DEPLOY_SHA}
DEPLOYED_AT=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
EOF

  echo "==> Updated state file: $STATE_FILE (sha-${DEPLOY_SHA})" # แสดงข้อความว่าเริ่มบันทึกข้อมูลลงไฟล์ .last-good-deploy
}

deploy_prod_images() { # ดำเนินการ deploy ภายใน docker-compose.prod.yml
  unset BACKEND_IMAGE FRONTEND_IMAGE # ลบค่า BACKEND_IMAGE และ FRONTEND_IMAGE

  echo "==> Pulling latest API and frontend images..." # แสดงข้อความว่าเริ่มดึง images จาก GHCR
  docker compose -f "$COMPOSE_FILE" pull api frontend # ดึง images จาก GHCR

  echo "==> Rolling update: API first..." # แสดงข้อความว่าเริ่มอัปเดต API
  docker compose -f "$COMPOSE_FILE" up -d --no-deps --wait api # อัปเดต API

  echo "==> Rolling update: frontend..." # แสดงข้อความว่าเริ่มอัปเดต frontend
  docker compose -f "$COMPOSE_FILE" up -d --no-deps --wait frontend # อัปเดต frontend
}

handle_smoke_failure() { # จัดการการทำ smoke test
  echo "ERROR: Smoke tests failed after deploy." # แสดงข้อความว่า smoke test ไม่สำเร็จ

  if [ -z "$ROLLBACK_BACKEND_IMAGE" ] || [ -z "$ROLLBACK_FRONTEND_IMAGE" ]; then # ถ้า ROLLBACK_BACKEND_IMAGE หรือ ROLLBACK_FRONTEND_IMAGE ว่าง
    echo "ERROR: No rollback target available — manual intervention required." # แสดงข้อความว่าไม่มี rollback target
    exit 1 # ออกจากฟังก์ชัน
  fi

  echo "==> Attempting automatic rollback..."
  export ROLLBACK_BACKEND_IMAGE ROLLBACK_FRONTEND_IMAGE # ตั้งค่า ROLLBACK_BACKEND_IMAGE และ ROLLBACK_FRONTEND_IMAGE
  if ! "$SCRIPT_DIR/rollback-prod.sh"; then # ถ้า rollback ไม่สำเร็จ
    echo "ERROR: Automatic rollback failed." # แสดงข้อความว่า rollback ไม่สำเร็จ
    exit 1 # ออกจากฟังก์ชัน
  fi

  echo "ERROR: Deploy of new version failed; production restored to previous version." # แสดงข้อความว่า deploy ใหม่ไม่สำเร็จ; production กลับมาใช้งานเดิม
  exit 1 # ออกจากฟังก์ชัน
}

load_rollback_target # โหลดข้อมูลจากไฟล์ .last-good-deploy
deploy_prod_images # ดำเนินการ deploy ภายใน docker-compose.prod.yml

echo "==> Running smoke tests..." # แสดงข้อความว่าเริ่มทำ smoke test
if ! "$SCRIPT_DIR/smoke-test.sh"; then # ถ้า smoke test ไม่สำเร็จ
  handle_smoke_failure # จัดการการทำ smoke test
fi

write_state_file # บันทึกข้อมูลลงไฟล์ .last-good-deploy

echo "==> Pruning unused images..." # แสดงข้อความว่าเริ่มลบ images ที่ไม่ใช้
docker image prune -f # ลบ images ที่ไม่ใช้

echo "==> Deploy complete." # แสดงข้อความว่า deploy สำเร็จ
