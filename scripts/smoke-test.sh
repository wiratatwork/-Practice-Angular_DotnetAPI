#!/usr/bin/env bash
set -euo pipefail # กำหนดให้หยุดการทำงานถ้ามี error

SMOKE_MAX_ATTEMPTS="${SMOKE_MAX_ATTEMPTS:-5}" # ตั้งค่า SMOKE_MAX_ATTEMPTS เป็น 5
SMOKE_RETRY_DELAY="${SMOKE_RETRY_DELAY:-5}" # ตั้งค่า SMOKE_RETRY_DELAY เป็น 5
SMOKE_EXPECTED_STATUS="${SMOKE_EXPECTED_STATUS:-200}" # ตั้งค่า SMOKE_EXPECTED_STATUS เป็น 200

if [ "$#" -gt 0 ]; then # ถ้ามีพารามิเตอร์
  SMOKE_URLS=("$@") # ตั้งค่า SMOKE_URLS เป็นพารามิเตอร์
else # ถ้าไม่มีพารามิเตอร์
  SMOKE_URLS=( # ตั้งค่า SMOKE_URLS เป็น http://127.0.0.1:5001/health และ http://127.0.0.1:4200/
    "http://127.0.0.1:5001/health"
    "http://127.0.0.1:4200/"
  )
fi

check_url() { # ตรวจสอบ URL
  local url="$1" # ตั้งค่า url เป็นพารามิเตอร์
  local attempt status # ตั้งค่า attempt และ status

  for ((attempt = 1; attempt <= SMOKE_MAX_ATTEMPTS; attempt++)); do # วนลูปผ่าน SMOKE_MAX_ATTEMPTS
    status="$(curl -fsS -o /dev/null -w '%{http_code}' "$url" 2>/dev/null || echo "000")" # ตั้งค่า status เป็น HTTP code

    if [ "$status" = "$SMOKE_EXPECTED_STATUS" ]; then # ถ้า status เป็น SMOKE_EXPECTED_STATUS
      echo "  OK $url (HTTP $status, attempt $attempt/$SMOKE_MAX_ATTEMPTS)" # แสดงข้อความว่า URL สำเร็จ
      return 0 # ออกจากฟังก์ชัน
    fi

    echo "  FAIL $url (HTTP $status, attempt $attempt/$SMOKE_MAX_ATTEMPTS)" # แสดงข้อความว่า URL ไม่สำเร็จ

    if [ "$attempt" -lt "$SMOKE_MAX_ATTEMPTS" ]; then # ถ้า attempt น้อยกว่า SMOKE_MAX_ATTEMPTS
      sleep "$SMOKE_RETRY_DELAY" # รอ SMOKE_RETRY_DELAY
    fi
  done

  return 1 # ออกจากฟังก์ชัน
}

echo "==> Running smoke tests (${SMOKE_MAX_ATTEMPTS} attempts, ${SMOKE_RETRY_DELAY}s delay)..." # แสดงข้อความว่าเริ่มทำ smoke test

failed=0 # ตั้งค่า failed เป็น 0
for url in "${SMOKE_URLS[@]}"; do # วนลูปผ่าน SMOKE_URLS
  if ! check_url "$url"; then # ถ้า check_url ไม่สำเร็จ
    failed=1 # ตั้งค่า failed เป็น 1
  fi
done

if [ "$failed" -ne 0 ]; then # ถ้า failed ไม่เป็น 0
  echo "==> Smoke tests failed." # แสดงข้อความว่า smoke test ไม่สำเร็จ
  exit 1 # ออกจากฟังก์ชัน
fi

echo "==> Smoke tests passed." # แสดงข้อความว่า smoke test สำเร็จ
exit 0 # ออกจากฟังก์ชัน
