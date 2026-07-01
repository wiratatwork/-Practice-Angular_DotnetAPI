#!/usr/bin/env bash
set -euo pipefail # กำหนดให้หยุดการทำงานถ้ามี error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)" # หาตำแหน่งของไฟล์ rollback-prod.sh
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}" # หาตำแหน่งของไฟล์ docker-compose.prod.yml

if [ -z "${ROLLBACK_BACKEND_IMAGE:-}" ] || [ -z "${ROLLBACK_FRONTEND_IMAGE:-}" ]; then # ถ้า ROLLBACK_BACKEND_IMAGE หรือ ROLLBACK_FRONTEND_IMAGE ว่าง มาจาก deploy-prod.sh
  echo "ERROR: No rollback target available (ROLLBACK_BACKEND_IMAGE / ROLLBACK_FRONTEND_IMAGE not set)." # แสดงข้อความว่าไม่มี rollback target
  exit 1 # ออกจากฟังก์ชัน
fi

echo "==> Rolling back to:" # แสดงข้อความว่าเริ่ม rollback
echo "    Backend:  $ROLLBACK_BACKEND_IMAGE" # แสดงข้อความว่า BACKEND_IMAGE
echo "    Frontend: $ROLLBACK_FRONTEND_IMAGE" # แสดงข้อความว่า FRONTEND_IMAGE

export BACKEND_IMAGE="$ROLLBACK_BACKEND_IMAGE" # ตั้งค่า BACKEND_IMAGE เป็น ROLLBACK_BACKEND_IMAGE
export FRONTEND_IMAGE="$ROLLBACK_FRONTEND_IMAGE" # ตั้งค่า FRONTEND_IMAGE เป็น ROLLBACK_FRONTEND_IMAGE

echo "==> Pulling rollback images..." # แสดงข้อความว่าเริ่มดึง images จาก GHCR
docker compose -f "$COMPOSE_FILE" pull api frontend # ดึง images จาก GHCR

echo "==> Rolling back: API first..." # แสดงข้อความว่าเริ่ม rollback API
docker compose -f "$COMPOSE_FILE" up -d --no-deps --wait api # อัปเดต API

echo "==> Rolling back: frontend..." # แสดงข้อความว่าเริ่ม rollback frontend
docker compose -f "$COMPOSE_FILE" up -d --no-deps --wait frontend # อัปเดต frontend

echo "==> Verifying rollback with smoke tests..." # แสดงข้อความว่าเริ่มทำ smoke test
if ! "$SCRIPT_DIR/smoke-test.sh"; then
  echo "ERROR: Rollback completed but smoke tests still failed." # แสดงข้อความว่า rollback สำเร็จแต่ smoke test ยังไม่สำเร็จ
  exit 1 # ออกจากฟังก์ชัน
fi

echo "==> Rollback successful." # แสดงข้อความว่า rollback สำเร็จ
