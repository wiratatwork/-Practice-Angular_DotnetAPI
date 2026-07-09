# Blue/Green Deployment Runbook (Single VM)

เอกสารนี้เป็น runbook สำหรับ production ที่มี VM เดียว โดยใช้ 2 slots (`blue` / `green`) และ Nginx เป็นตัวสลับทราฟฟิก

## โครงสร้าง

- `blue` slot
  - API: `127.0.0.1:5001`
  - Frontend: `127.0.0.1:4201`
- `green` slot
  - API: `127.0.0.1:5002`
  - Frontend: `127.0.0.1:4202`
- Nginx active config:
  - `/etc/nginx/conf.d/basic_app_active.conf`
  - symlink ไปไฟล์ใน `deploy/nginx/`

## ไฟล์สำคัญ

- Slot compose: `deploy/docker-compose.slot.yml`
- Deploy script: `scripts/deploy-prod.sh`
- Rollback script: `scripts/rollback-prod.sh`
- Manual switch script: `scripts/switch-slot.sh`
- Smoke tests: `scripts/smoke-test.sh`
- Deploy state: `~/basic_app/.last-good-deploy`

## Deployment Flow (CI/CD)

1. Build + push image (`sha-<commit>`)
2. ระบุ active slot ปัจจุบันจาก `.last-good-deploy`
3. Deploy image ใหม่ลง slot ที่ inactive
4. Smoke test บน localhost:port ของ slot ที่ inactive
5. สลับ Nginx ไป slot ใหม่ (`nginx -s reload`)
6. Smoke test public endpoint
7. อัปเดต `.last-good-deploy`

## Manual Commands

รันบน VM ที่ deploy:

```bash
cd ~/basic_app
chmod +x scripts/*.sh
```

Deploy commit ใหม่:

```bash
DEPLOY_SHA=<short_sha> ./scripts/deploy-prod.sh
```

สลับทราฟฟิกแบบ manual:

```bash
./scripts/switch-slot.sh blue
./scripts/switch-slot.sh green
```

Rollback ไป last known good:

```bash
./scripts/rollback-prod.sh
```

## Rollback Playbook

1. ตรวจอาการผิดปกติ (error rate, p95 latency, healthcheck fail)
2. สลับกลับ slot เดิมทันที:
   - ใช้ `./scripts/rollback-prod.sh` (แนะนำ)
   - หรือ `./scripts/switch-slot.sh <slot>`
3. รัน smoke tests:
   - `./scripts/smoke-test.sh http://127.0.0.1/`
4. ตรวจ logs ของ slot ที่มีปัญหาเพื่อทำ postmortem

เป้าหมาย: rollback traffic ภายในไม่กี่นาที

## Database Migration (Expand / Contract)

Blue/Green ที่ rollback ได้ต้องใช้ migration แบบ backward-compatible:

1. Expand
   - เพิ่ม column/table/index ใหม่ที่เวอร์ชันเก่ากับใหม่อยู่ร่วมกันได้
   - ไม่ลบ field เก่าในรอบเดียวกับ deployment
2. Deploy app (blue/green cutover)
3. Contract
   - เมื่อยืนยันว่าไม่มี rollback แล้วค่อยลบ schema เดิม

ข้อห้าม:
- หลีกเลี่ยง breaking migration ที่ทำให้ app slot เก่าอ่าน/เขียนไม่ได้ทันที
- หลีกเลี่ยง destructive migration ใน release เดียวกับ cutover

## Monitoring Gates

ก่อน promote slot ใหม่:
- API health endpoint ตอบ 200
- Frontend ตอบ 200
- Error rate ไม่เกิน baseline + threshold
- p95 latency ไม่พุ่งเกิน SLO

หลัง cutover 10-30 นาที:
- ติดตาม 5xx, latency, restart count, memory pressure
- ถ้าเกิน threshold ให้ rollback ทันที

## Known Limitations

- เป็น Blue/Green แบบ single VM จึงยังมี single point of failure
- ถ้าต้องการ high availability จริง ควรย้ายไป 2 VM + external/load balancer switch
