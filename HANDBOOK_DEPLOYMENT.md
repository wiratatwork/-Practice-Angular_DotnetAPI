# Deployment Handbook

เอกสารนี้กำหนดมาตรฐานการ deploy ของโปรเจกต์ เพื่อให้ทีมพัฒนา, ผู้ทดสอบ, และผู้อนุมัติใช้งาน flow เดียวกัน

## เป้าหมาย

ต้องการให้การปล่อยระบบเป็น 2 ระดับดังนี้:

1. โค้ดจากนักพัฒนาจะถูกรวมเข้า `test`
2. เมื่อมีการ push เข้า `test` จะรัน CI/CD อัตโนมัติ — build Docker images และ push ไป GHCR (tag `:test`)
3. ทีมดึง images จาก GHCR ไปรันบน test server ด้วย `docker-compose.test.yml` เพื่อทดสอบ
4. หัวหน้าหรือผู้รับผิดชอบทดสอบบน test environment
5. เมื่อทดสอบเสร็จและอนุมัติ Pull Request จาก `test` เข้า `main`
6. ระบบจะ build images (tag `:prod`), สแกนช่องโหว่, และ deploy ไป production VM อัตโนมัติ

## ภาพรวม Flow

```text
feature/* -> Pull Request -> test -> push -> CI + Build + Push GHCR (:test) + Trivy
                                                |
                                                v
                              ทีม pull images ไป test server (docker-compose.test.yml)
                                                |
                                                v
                                     หัวหน้าทดสอบและอนุมัติ
                                                |
                                                v
                                   Pull Request test -> main
                                                |
                                                v
                    CI + Build + Push GHCR (:prod) + Trivy -> Deploy VM + Smoke test
```

## Infrastructure

| ส่วน | เทคโนโลยี |
|------|-----------|
| Container Registry | GitHub Container Registry (GHCR) — `ghcr.io/<owner>/basic-app-backend`, `basic-app-frontend` |
| Test environment | VM/เครื่องทดสอบ — รัน `docker-compose.test.yml` ดึง image tag `:test` |
| Production environment | VM — รัน `docker-compose.prod.yml` ดึง image tag `:prod` (deploy ผ่าน GitHub Actions) |
| CI/CD | GitHub Actions |
| Static analysis | CodeQL (`.github/workflows/codeql.yml`) |
| Image vulnerability scan | Trivy (`.github/workflows/reusable-trivy-scan.yml`) |

## Branch Strategy

ใช้ branch หลัก 2 ตัว:

- `test`: ใช้รวมงานจากนักพัฒนาเพื่อทดสอบร่วมกัน
- `main`: ใช้สำหรับ production เท่านั้น

แนวทางทำงานที่แนะนำ:

- นักพัฒนาสร้าง branch งาน เช่น `feature/login`, `feature/machine-search`
- เมื่อพัฒนาเสร็จ ให้เปิด Pull Request เข้า `test`
- ไม่ควร push งานที่ยังไม่พร้อมใช้งานเข้า `main` โดยตรง
- การขึ้น production ให้ทำผ่าน Pull Request จาก `test` ไป `main` เท่านั้น

## Environment ที่ต้องมี

### Test server

- รัน Docker Compose จาก `docker-compose.test.yml`
- ใช้ images จาก GHCR tag `:test`
- แยก database, secrets, และ URL จาก production

### Production VM

- รัน Docker Compose จาก `docker-compose.prod.yml`
- ใช้ images จาก GHCR tag `:prod` และ `sha-<commit>` สำหรับ rollback
- ค่า environment variables ตั้งบน VM (`.env`) — ไม่ commit ลง repository

ข้อกำหนด:

- database ของ test และ production ต้องไม่ใช้ชุดเดียวกัน
- URL, API keys, และ secrets ของ production ต้องไม่ถูกใช้ใน test

## GitHub Actions Workflows

| Workflow | Trigger | สิ่งที่ทำ |
|----------|---------|-----------|
| [`ci-cd-test.yml`](.github/workflows/ci-cd-test.yml) | push → `test` | CI → build/push images `:test` → Trivy scan |
| [`ci-cd-main.yml`](.github/workflows/ci-cd-main.yml) | PR/push → `main`, `workflow_dispatch` | CI → build/push images `:prod` → Trivy → deploy production VM |
| [`codeql.yml`](.github/workflows/codeql.yml) | push/PR → `main`, `test` | CodeQL static analysis |
| [`reusable-ci.yml`](.github/workflows/reusable-ci.yml) | workflow_call | Lint + unit/integration tests |
| [`reusable-build-push.yml`](.github/workflows/reusable-build-push.yml) | workflow_call | Build และ push Docker images ไป GHCR |
| [`reusable-trivy-scan.yml`](.github/workflows/reusable-trivy-scan.yml) | workflow_call | สแกนช่องโหว่ CRITICAL/HIGH ใน images |

### Image tags ใน GHCR

- **test branch:** `basic-app-backend:test`, `basic-app-frontend:test` และ `sha-<short-commit>`
- **main branch:** `basic-app-backend:prod`, `basic-app-frontend:prod` และ `sha-<short-commit>`

## พฤติกรรม CI/CD ที่เกิดขึ้นจริง

### 1. เมื่อ push เข้า `test`

ลำดับงานใน [`ci-cd-test.yml`](.github/workflows/ci-cd-test.yml):

1. **CI** — lint frontend/backend, unit tests, integration tests (PostgreSQL service)
2. **Build & Push** — build Docker images และ push ไป GHCR tag `:test`
3. **Trivy Scan** — สแกน backend/frontend images; fail ถ้าพบ CRITICAL/HIGH ที่ fix ได้

**หมายเหตุ:** workflow นี้ **ไม่ deploy ไป test server อัตโนมัติ** — ทีมต้อง `docker compose pull` บน test server เองหลัง CI ผ่าน

### 2. เมื่อเปิด PR หรือ push เข้า `main`

ลำดับงานใน [`ci-cd-main.yml`](.github/workflows/ci-cd-main.yml):

1. **CI** — lint + tests (รันทั้ง PR และ push)
2. **Build & Push** — build และ push images tag `:prod` (PR: build อย่างเดียวตาม `push_images`; push จริงเมื่อ merge)
3. **Trivy Scan** — สแกน images ก่อน deploy
4. **Deploy to Production** — รันเฉพาะเมื่อ `push` หรือ `workflow_dispatch`:
   - copy scripts ไป VM ผ่าน SCP
   - SSH รัน [`scripts/deploy-prod.sh`](scripts/deploy-prod.sh)
   - rolling update API แล้ว frontend
   - smoke test ที่ `http://127.0.0.1:5001/health` และ `http://127.0.0.1:4200/`
   - ถ้า smoke fail → rollback อัตโนมัติด้วย [`scripts/rollback-prod.sh`](scripts/rollback-prod.sh)
   - บันทึก state ลง `~/.last-good-deploy` สำหรับ rollback ครั้งถัดไป

Deploy job ใช้ GitHub Environment **`production`** และ `concurrency: deploy-production` (ไม่ cancel deploy ที่กำลังรัน)

## Deploy Scripts

| Script | หน้าที่ |
|--------|---------|
| [`scripts/deploy-prod.sh`](scripts/deploy-prod.sh) | pull images, rolling update, smoke test, auto rollback, บันทึก last-good state |
| [`scripts/rollback-prod.sh`](scripts/rollback-prod.sh) | rollback ไป image ก่อนหน้า + verify smoke test |
| [`scripts/smoke-test.sh`](scripts/smoke-test.sh) | ตรวจ HTTP health endpoints (retry 5 ครั้ง) |

## ผู้รับผิดชอบในแต่ละขั้น

- **นักพัฒนา:**
  - พัฒนาใน branch งาน
  - เปิด Pull Request เข้า `test`
  - แก้ไข issue ที่ทำให้ CI ไม่ผ่าน
- **หัวหน้าหรือผู้ทดสอบ:**
  - ทดสอบบน test server (images จาก GHCR tag `:test`)
  - อนุมัติ Pull Request จาก `test` ไป `main`
- **ระบบ CI/CD:**
  - ตรวจสอบคุณภาพโค้ดและช่องโหว่
  - deploy production อัตโนมัติเมื่อ merge เข้า `main`

## กฎที่ตั้งใน GitHub (Ruleset)

### สำหรับ `main` (Ruleset: Protect Main Branch)

- ห้าม push ตรง (Restrict updates) — ต้องผ่าน PR
- บังคับ Pull Request + approval อย่างน้อย 1 คน
- Dismiss stale approvals เมื่อมี commit ใหม่
- ห้าม approve commit ที่ตัวเอง push
- ต้อง resolve conversation ครบก่อน merge
- บังคับ status checks ผ่านก่อน merge (CI jobs, Trivy, CodeQL)
- บังคับ branch up-to-date กับ `main` ก่อน merge
- ห้าม force push และห้ามลบ branch

### สำหรับ `test` (แนะนำ)

- อนุญาต merge ผ่าน Pull Request
- บังคับให้ CI checks ผ่านก่อน merge
- แนะนำให้ห้าม push ตรง ยกเว้นผู้ดูแลระบบ

## สิ่งที่ต้องตั้งค่าใน GitHub

### Repository Secrets

| Secret | ใช้เมื่อ |
|--------|----------|
| `PROD_HOST` | SSH ไป production VM |
| `PROD_USER` | SSH username |
| `PROD_SSH_KEY` | SSH private key |
| `GHCR_TOKEN` | VM login ดึง images จาก GHCR |
| `GHCR_USERNAME` | username สำหรับ GHCR login บน VM |

### Repository Variables

| Variable | ใช้เมื่อ |
|----------|--------|
| `PROD_URL` | แสดง URL ใน GitHub Environment `production` |

### GitHub Environment

- **`production`** — ครอบ deploy job; สามารถเพิ่ม required reviewers ได้ใน Settings → Environments

### Dependabot

[`dependabot.yml`](.github/dependabot.yml) อัปเดต npm, NuGet, และ GitHub Actions รายสัปดาห์ — target branch `test`

หมายเหตุ:

- ไม่ควร hardcode secret ลงใน repository
- ไฟล์ `.env` และ `GitHub Token.txt` อยู่ใน `.gitignore` แล้ว

## Release Policy

กติกาการปล่อย production:

- โค้ดที่จะขึ้น production ต้องมาจาก `test` เท่านั้น
- ต้องผ่านการทดสอบบน test environment ก่อน
- ต้องได้รับการอนุมัติ Pull Request ก่อน merge เข้า `main`
- เมื่อ merge เข้า `main` แล้ว ระบบ deploy production อัตโนมัติ (หลัง CI + Trivy ผ่าน)
- Production deploy ใช้ image tag `sha-<commit>` สำหรับ rollback; state เก็บใน `.last-good-deploy` บน VM

## ขั้นตอนการทำงานของทีม

### ฝั่งนักพัฒนา

1. pull code ล่าสุดจาก `test`
2. สร้าง branch งานใหม่
3. พัฒนาและทดสอบในเครื่อง (`docker compose up`)
4. push branch งานขึ้น GitHub
5. เปิด Pull Request เข้า `test`
6. รอ CI ผ่าน → merge เข้า `test`
7. แจ้งทีมให้ pull images ใหม่บน test server (ถ้าจำเป็น)

### ฝั่งหัวหน้าหรือผู้อนุมัติ

1. ทดสอบบน test server (images tag `:test`)
2. ทดสอบตาม checklist หรือ test scenario
3. ถ้าผ่าน ให้ approve Pull Request จาก `test` ไป `main`
4. merge เข้า `main`
5. ตรวจสอบ GitHub Actions deploy job และ production URL

### Deploy manual (กรณีฉุกเฉิน)

- ใช้ **workflow_dispatch** บน workflow `CI/CD - Build & Push to GHCR (main branch)` เพื่อ trigger deploy ใหม่
- Rollback บน VM: รัน `scripts/rollback-prod.sh` ด้วย `ROLLBACK_BACKEND_IMAGE` / `ROLLBACK_FRONTEND_IMAGE` จาก `.last-good-deploy`

## Definition of Done ก่อนขึ้น Production

ก่อน merge เข้า `main` ต้องมีครบ:

- CI บน `test` ผ่าน
- images `:test` ถูก push ไป GHCR และทดสอบบน test server แล้ว
- Trivy scan ไม่พบ CRITICAL/HIGH ที่ fix ได้
- CodeQL ไม่มี finding ที่ block (ถ้าเปิดใช้)
- ทดสอบ business flow สำคัญแล้ว
- ไม่มี bug blocker หรือ critical issue
- Pull Request ได้รับ approval ตามสิทธิ์ที่กำหนด

## ความเสี่ยงที่ต้องระวัง

- ถ้าอนุญาตให้ push ตรงเข้า `main` อาจทำให้ bypass ขั้นตอนทดสอบ
- ถ้า test กับ production ใช้ environment variables ชุดเดียวกัน อาจเกิดการปนกันของข้อมูล
- ถ้า CI รันไม่ครบทั้ง build, test, Trivy, และ CodeQL ความเสี่ยงจะถูกส่งต่อถึง production
- ถ้าไม่มี branch protection กระบวนการอนุมัติอาจถูกข้าม
- CodeQL รันเฉพาะเมื่อไฟล์ใน path ที่กำหนดเปลี่ยน — PR ที่แก้แค่ config อาจไม่ trigger CodeQL แต่ ruleset อาจยัง require check อยู่

## สรุปนโยบาย

นโยบาย deploy ของโปรเจกต์นี้คือ:

- รวมงานของนักพัฒนาเข้า `test`
- ให้ CI/CD build และ push images ไป GHCR tag `:test` อัตโนมัติ
- ให้ทีมทดสอบบน test server ก่อนขึ้น production
- ให้หัวหน้าทดสอบและอนุมัติการขึ้น production
- เมื่อ merge เข้า `main` ให้ CI/CD deploy ไป production VM อัตโนมัติ พร้อม smoke test และ rollback

แนวทางนี้ช่วยให้ `test` เป็น staging area สำหรับตรวจสอบคุณภาพก่อนปล่อยจริง และทำให้ `main` เป็นแหล่งอ้างอิงของ production อย่างชัดเจน
