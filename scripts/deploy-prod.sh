#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib-nginx.sh
source "$SCRIPT_DIR/lib-nginx.sh"
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

# ฟังก์ชัน inactive_slot_for จะรับค่า slot ที่ active ("blue" หรือ "green") แล้วคืนค่า slot ฝั่งตรงข้ามที่ไม่ได้ active ในขณะนี้
inactive_slot_for() {
  [ "$1" = "blue" ] && echo "green" || echo "blue"
}

compose_project_for() {
  echo "basicapp-$1"
}

# ฟังก์ชัน nginx_conf_for รับค่า slot ("blue" หรือ "green") แล้วคืนค่า nginx config file ที่เหมาะสมกับ slot นั้น
nginx_conf_for() {
  [ "$1" = "blue" ] && echo "$NGINX_BLUE_CONF" || echo "$NGINX_GREEN_CONF"
}

# ฟังก์ชัน switch_nginx_slot ใช้สำหรับเปลี่ยน active slot ของ Nginx
# - รับพารามิเตอร์ slot ("blue" หรือ "green") เพื่อเลือก config ของฝั่งที่จะ active
# - นำ config ที่ได้ไปติดตั้งเป็น nginx active config
# - reload หรือ start nginx เพื่อให้การเปลี่ยนแปลงมีผล
switch_nginx_slot() {
  local slot="$1"
  local slot_conf
  slot_conf="$(nginx_conf_for "$slot")"
  echo "==> Switching Nginx active slot to: $slot"
  install_nginx_active_config "$slot_conf"
  reload_or_start_nginx
}

# ฟังก์ชัน ensure_shared_postgres
# ตรวจสอบให้แน่ใจว่า PostgreSQL (ฐานข้อมูล) ที่ใช้ร่วมกันใน production environment ทำงานอยู่
# - ตรวจสอบว่าไฟล์ docker compose สำหรับ production มีอยู่หรือไม่ หากไม่พบให้แจ้ง error และหยุดทำงาน
# - หากพบไฟล์ดังกล่าว จะสั่งให้ docker compose รัน service postgres แบบ detached โดยใช้ environment และ project ที่กำหนด
ensure_shared_postgres() {
  if [ ! -f "$PROD_COMPOSE_FILE" ]; then
    echo "ERROR: Production compose file not found: $PROD_COMPOSE_FILE"
    exit 1
  fi

  echo "==> Ensuring shared PostgreSQL is running..."
  docker compose --env-file "$ENV_FILE" -f "$PROD_COMPOSE_FILE" -p "$PROD_COMPOSE_PROJECT" up -d postgres
}

load_state_file() {
  # ตรวจสอบว่าไฟล์ state file มีอยู่หรือไม่ หากไม่มีให้แสดงข้อความแจ้งและกำหนด slot ที่ใช้งานอยู่ (ACTIVE_SLOT) เป็นค่าเริ่มต้น
  if [ ! -f "$STATE_FILE" ]; then
    echo "==> No state file at $STATE_FILE. Default active slot: $ACTIVE_SLOT"
    return 0
  fi

  # shellcheck disable=SC1090
  # โหลดค่าตัวแปรสถานะล่าสุดจากไฟล์ state
  source "$STATE_FILE"

  # ถ้ามีค่า ACTIVE_SLOT อยู่ใน state file ให้เก็บไว้ใน PREV_ACTIVE_SLOT เพื่อใช้ระบุ slot เดิมที่ active ก่อน deploy ล่าสุด
  if [ -n "${ACTIVE_SLOT:-}" ]; then
    PREV_ACTIVE_SLOT="$ACTIVE_SLOT"
  fi

  # ถ้ามีค่า BACKEND_IMAGE และ FRONTEND_IMAGE (หมายถึงมีค่า image สำหรับ backend และ frontend ที่ถูก deploy ล่าสุด)
  # ให้กำหนด ROLLBACK_BACKEND_IMAGE และ ROLLBACK_FRONTEND_IMAGE เป็นค่าดังกล่าว
  if [ -n "${BACKEND_IMAGE:-}" ] && [ -n "${FRONTEND_IMAGE:-}" ]; then
    ROLLBACK_BACKEND_IMAGE="$BACKEND_IMAGE"
    ROLLBACK_FRONTEND_IMAGE="$FRONTEND_IMAGE"
  fi

  # แสดง status ล่าสุดที่โหลดมาจาก state file ก่อนหน้า เพื่อ debug หรือเช็คค่าสถานะล่าสุดที่ใช้งาน
  echo "==> Loaded previous deploy state from $STATE_FILE"
  echo "    Active slot: ${PREV_ACTIVE_SLOT:-unknown}"
  echo "    Backend image: ${ROLLBACK_BACKEND_IMAGE:-unknown}"
  echo "    Frontend image: ${ROLLBACK_FRONTEND_IMAGE:-unknown}"
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

  # กำหนดหมายเลข port สำหรับ backend (api) และ frontend ตาม slot ที่จะ deploy
  api_port="$(slot_port_for "$target_slot" api)"
  frontend_port="$(slot_port_for "$target_slot" frontend)"
  # กำหนดชื่อ docker compose project สำหรับ slot นี้เพื่อแยก environment ระหว่าง blue/green
  compose_project="$(compose_project_for "$target_slot")"

  # ดึง (pull) image ของ api และ frontend สำหรับ slot ที่ต้อง deploy โดยใช้ค่า image ที่ระบุ (backend_image/frontend_image)
  echo "==> Deploying candidate to slot: $target_slot"
  BACKEND_IMAGE="$backend_image" \
    FRONTEND_IMAGE="$frontend_image" \
    APP_API_HOST_PORT="$api_port" \
    APP_FRONTEND_HOST_PORT="$frontend_port" \
    docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" -p "$compose_project" pull api frontend

  # สั่ง docker compose ขึ้น service api และ frontend (slot ผู้ท้าชิง) แบบ detached mode พร้อมรอ container ขึ้นครบ
  BACKEND_IMAGE="$backend_image" \
    FRONTEND_IMAGE="$frontend_image" \
    APP_API_HOST_PORT="$api_port" \
    APP_FRONTEND_HOST_PORT="$frontend_port" \
    docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" -p "$compose_project" up -d --wait api frontend

  # รัน smoke test เพื่อตรวจสอบ health ของ slot ที่กำลังจะ deploy (api และ frontend)
  echo "==> Running smoke tests on candidate slot ($target_slot)..."
  "$SCRIPT_DIR/smoke-test.sh" \
    "http://127.0.0.1:${api_port}/health" \
    "http://127.0.0.1:${frontend_port}/"
}

# ฟังก์ชันนี้จะถูกเรียกเมื่อ smoke test ของ slot ใหม่ (candidate slot) ล้มเหลว
# จะคืนค่า slot ที่ production ยังทำงานปกติ (previously_active) ถ้ามี และออกจากสคริปท์ด้วย error
handle_smoke_failure() {
  local previously_active="$1"
  echo "ERROR: Candidate slot smoke tests failed."
  if [ -n "$previously_active" ]; then
    echo "==> Keeping current production slot unchanged: $previously_active"
    switch_nginx_slot "$previously_active"
  fi
  exit 1
}
# ตรวจสอบว่า DEPLOY_SHA ถูกกำหนดหรือไม่ หากไม่มีก็ให้แสดง error และหยุดการทำงานของ script
if [ -z "$DEPLOY_SHA" ]; then
  echo "ERROR: DEPLOY_SHA is required."
  exit 1
fi

# ตรวจสอบว่าไฟล์ environment (.env) สำหรับกำหนดตัวแปรสำคัญถูกสร้างขึ้นหรือยัง
if [ ! -f "$ENV_FILE" ]; then
  echo "ERROR: Environment file not found: $ENV_FILE"
  echo "       Create it with DB_CONNECTION and JWT_KEY (see .env.example)."
  exit 1
fi

# สร้างไดเรกทอรีสำหรับ STATE_FILE หากยังไม่มี
mkdir -p "$(dirname "$STATE_FILE")"
# โหลดข้อมูลสถานะการ deploy ล่าสุดจากไฟล์
load_state_file

# หากมีการระบุ slot ที่ active ก่อนหน้า (PREV_ACTIVE_SLOT) ให้นำค่ามาใช้กำหนด ACTIVE_SLOT
if [ -n "$PREV_ACTIVE_SLOT" ]; then
  ACTIVE_SLOT="$PREV_ACTIVE_SLOT"
fi
# กำหนด INACTIVE_SLOT ให้เป็น slot ที่ไม่ได้ active อยู่ในปัจจุบัน
INACTIVE_SLOT="$(inactive_slot_for "$ACTIVE_SLOT")"

# กำหนดชื่อ image สำหรับ backend และ frontend ตาม SHA ที่ต้องการ deploy
NEW_BACKEND_IMAGE="${REGISTRY}/${BACKEND_REPO}:sha-${DEPLOY_SHA}"
NEW_FRONTEND_IMAGE="${REGISTRY}/${FRONTEND_REPO}:sha-${DEPLOY_SHA}"

# ตรวจสอบและเริ่มต้น PostgreSQL ที่ใช้ร่วมกันใน production environment
ensure_shared_postgres

# ทำการ deploy service (api และ frontend) ไปยัง slot ที่ไม่ได้ active อยู่ (INACTIVE_SLOT)
# ถ้าการ deploy หรือการ smoke test ล้มเหลว จะทำการ roll back กลับไปยัง slot เดิม (ACTIVE_SLOT)
if ! deploy_inactive_slot "$INACTIVE_SLOT" "$NEW_BACKEND_IMAGE" "$NEW_FRONTEND_IMAGE"; then
  handle_smoke_failure "$ACTIVE_SLOT"
fi

# ทำการเปลี่ยน active slot ของ Nginx ไปยัง slot ที่ไม่ได้ active อยู่ (INACTIVE_SLOT)
switch_nginx_slot "$INACTIVE_SLOT"

# ตรวจสอบ public endpoint หลังจากสลับ traffic (cutover) ไปยัง slot ใหม่
# ถ้าการ smoke test ล้มเหลว จะทำการ roll back กลับไปยัง slot เดิม (ACTIVE_SLOT)
echo "==> Verifying public endpoint after cutover..."
if ! "$SCRIPT_DIR/smoke-test.sh" "$PUBLIC_SMOKE_URL"; then
  echo "ERROR: Post-cutover smoke test failed, rolling back traffic to $ACTIVE_SLOT"
  switch_nginx_slot "$ACTIVE_SLOT"
  exit 1
fi

# บันทึกสถานะการ deploy ล่าสุด (slot และชื่อ image ที่ถูก deploy) ลงใน state file
write_state_file "$INACTIVE_SLOT" "$NEW_BACKEND_IMAGE" "$NEW_FRONTEND_IMAGE"

# ลบ image ที่ไม่ถูกใช้งานเพื่อลดพื้นที่บนเครื่อง (prune unused Docker images)
echo "==> Pruning unused images..."
docker image prune -f

echo "==> Blue/green deploy complete. Active slot: $INACTIVE_SLOT"
