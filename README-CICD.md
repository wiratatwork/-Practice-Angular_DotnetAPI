# คู่มือเข้าใจ CI/CD Workflow สำหรับมือใหม่

## 📖 CI/CD คืออะไร?

**CI/CD** ย่อมาจาก **Continuous Integration / Continuous Deployment**

- **CI (Continuous Integration)**: การรวมโค้ดที่เขียนใหม่เข้ากับโค้ดหลักอย่างสม่ำเสมอ โดยมีการทดสอบอัตโนมัติ
- **CD (Continuous Deployment)**: การส่งโค้ดที่ผ่านการทดสอบแล้วไปยังเซิร์ฟเวอร์หรือ registry อัตโนมัติ

ในไฟล์ `ci-cd-test.yml` นี้ เราจะทำทั้ง **Build** (สร้าง Docker Image) และ **Push** (ส่ง Image ไป GHCR) อัตโนมัติ

---

## 🔍 อธิบายไฟล์ทีละส่วน

### 1️⃣ ชื่อ Workflow และ Trigger Event

```yaml
name: CI/CD - Build & Push to GHCR (test branch)

on:
  push:
    branches:
      - test
  pull_request:
    branches:
      - test
```

**คำอธิบาย:**
- `name`: ชื่อของ workflow ที่จะแสดงใน GitHub Actions
- `on`: กำหนดว่า workflow นี้จะทำงานเมื่อไหร่
  - `push` → เมื่อมีการ push โค้ดไปที่ branch `test`
  - `pull_request` → เมื่อมีการเปิด PR (Pull Request) ที่จะ merge เข้า branch `test`

**ตัวอย่างการใช้งาน:**
```bash
# สถานการณ์ที่ workflow จะทำงาน
git push origin test          # ✅ workflow ทำงาน
git push origin main          # ❌ ไม่ทำงาน (branch ไม่ตรง)
```

---

### 2️⃣ Environment Variables (ตัวแปรสำหรับใช้ทั้ง workflow)

```yaml
env:
  REGISTRY: ghcr.io
  IMAGE_OWNER: ${{ github.repository_owner }}
```

**คำอธิบาย:**
- `REGISTRY`: ที่อยู่ของ Container Registry (ใช้ GitHub Container Registry - GHCR)
- `IMAGE_OWNER`: ชื่อเจ้าของ repository (เช่น `wiratatwork`)
  - `${{ github.repository_owner }}` = ดึงชื่อจาก GitHub อัตโนมัติ

**ทำไมต้องใช้ตัวแปร?**
- เขียนครั้งเดียว ใช้ได้หลายที่
- ถ้าต้องการเปลี่ยน registry แก้ที่เดียวพอ

---

### 3️⃣ Job Definition (งานที่จะทำ)

```yaml
jobs:
  build-and-push:
    name: Build & Push Docker Images
    runs-on: ubuntu-latest
```

**คำอธิบาย:**
- `jobs`: รายการงานที่ต้องทำ (workflow อาจมีหลาย jobs)
- `build-and-push`: ชื่อ job นี้ (ตั้งเองได้)
- `runs-on: ubuntu-latest`: ให้รันบน virtual machine ที่เป็น Ubuntu เวอร์ชันล่าสุด

---

### 4️⃣ Permissions (สิทธิ์การเข้าถึง)

```yaml
permissions:
  contents: read
  packages: write
```

**คำอธิบาย:**
- `contents: read`: อนุญาตให้อ่านโค้ดใน repository
- `packages: write`: อนุญาตให้เขียน (push) Docker images ไปที่ GHCR

**ทำไมต้องกำหนดสิทธิ์?**
- เพื่อความปลอดภัย — ให้เฉพาะสิทธิ์ที่จำเป็น
- ป้องกันการเข้าถึงหรือแก้ไขสิ่งที่ไม่ได้รับอนุญาต

---

### 5️⃣ Steps - Checkout (ดึงโค้ด)

```yaml
steps:
  - name: Checkout repository
    uses: actions/checkout@v4
```

**คำอธิบาย:**
- `steps`: ขั้นตอนต่างๆ ที่ต้องทำใน job นี้
- `actions/checkout@v4`: action สำเร็จรูปที่ดึงโค้ดจาก repository มาไว้ใน runner

**ทำไมต้อง checkout?**
- runner เริ่มต้นจะไม่มีโค้ดของเรา
- ต้องดึงโค้ดมาก่อนถึงจะ build Docker image ได้

---

### 6️⃣ Steps - Login to GHCR

```yaml
- name: Log in to GitHub Container Registry
  uses: docker/login-action@v3
  with:
    registry: ${{ env.REGISTRY }}
    username: ${{ github.actor }}
    password: ${{ secrets.GITHUB_TOKEN }}
```

**คำอธิบาย:**
- `docker/login-action@v3`: action สำหรับ login เข้า Docker registry
- `registry`: ghcr.io (จากตัวแปร `REGISTRY`)
- `username`: ชื่อผู้ใช้ที่ trigger workflow นี้
- `password`: token อัตโนมัติที่ GitHub สร้างให้ (ไม่ต้องสร้างเอง)

**ทำไมต้อง login?**
- เพื่อพิสูจน์ตัวตนว่าเรามีสิทธิ์ push images ไปที่ GHCR
- `GITHUB_TOKEN` มีอยู่แล้วทุก workflow ไม่ต้องตั้งค่าเพิ่ม

---

### 7️⃣ Steps - Setup Docker Buildx

```yaml
- name: Set up Docker Buildx
  uses: docker/setup-buildx-action@v3
```

**คำอธิบาย:**
- ติดตั้ง **Docker Buildx** ซึ่งเป็นเครื่องมือ build Docker image แบบขั้นสูง

**ข้อดีของ Buildx:**
- รองรับ **multi-platform builds** (build สำหรับหลาย architecture พร้อมกัน)
- รองรับ **build cache** ทำให้ build เร็วขึ้น
- เป็น standard ใหม่ของ Docker

---

### 8️⃣ Steps - Extract Metadata (Backend)

```yaml
- name: Extract metadata for backend image
  id: meta-backend
  uses: docker/metadata-action@v5
  with:
    images: ${{ env.REGISTRY }}/${{ env.IMAGE_OWNER }}/basic-app-backend
    tags: |
      type=ref,event=branch
      type=sha,prefix=sha-,format=short
      type=raw,value=latest,enable={{is_default_branch}}
```

**คำอธิบาย:**
- `docker/metadata-action@v5`: สร้าง tags และ labels สำหรับ Docker image อัตโนมัติ
- `images`: ชื่อ image ที่จะใช้ → `ghcr.io/wiratatwork/basic-app-backend`
- `id: meta-backend`: ให้ชื่อ step นี้เพื่อเรียกใช้ output ในขั้นตอนถัดไป

**Tags ที่จะได้:**
1. `type=ref,event=branch` → ชื่อ branch เช่น `test`
2. `type=sha,prefix=sha-,format=short` → commit SHA สั้นๆ เช่น `sha-a1b2c3d`
3. `type=raw,value=latest` → tag `latest` (เฉพาะ default branch)

**ตัวอย่าง tags ที่ได้จริง:**
```
ghcr.io/wiratatwork/basic-app-backend:test
ghcr.io/wiratatwork/basic-app-backend:sha-f38a4cc
```

---

### 9️⃣ Steps - Build and Push (Backend)

```yaml
- name: Build and push backend image
  uses: docker/build-push-action@v6
  with:
    context: ./backend
    dockerfile: ./backend/Dockerfile
    push: ${{ github.event_name == 'push' }}
    tags: ${{ steps.meta-backend.outputs.tags }}
    labels: ${{ steps.meta-backend.outputs.labels }}
    cache-from: type=gha
    cache-to: type=gha,mode=max
```

**คำอธิบาย:**
- `context: ./backend`: โฟลเดอร์ที่มี source code สำหรับ build
- `dockerfile`: ตำแหน่งของ Dockerfile
- `push: ${{ github.event_name == 'push' }}`: 
  - ✅ push = true เมื่อเป็น `push` event
  - ❌ push = false เมื่อเป็น `pull_request` event (build อย่างเดียว ไม่ push)
- `tags`: ใช้ tags ที่ได้จากขั้นตอนก่อนหน้า
- `labels`: ข้อมูลเพิ่มเติมของ image (เช่น commit SHA, author)

**Cache:**
- `cache-from: type=gha`: ดึง cache จาก GitHub Actions cache
- `cache-to: type=gha,mode=max`: เก็บ cache ไว้ใน GitHub Actions (mode=max = เก็บทุก layer)

**ประโยชน์ของ cache:**
- Build ครั้งที่ 1: อาจใช้เวลา 5-10 นาที
- Build ครั้งที่ 2: ใช้เวลาแค่ 1-2 นาที (ถ้าโค้ดไม่เปลี่ยนมาก)

---

### 🔟 Steps - Frontend (ทำซ้ำแบบเดียวกับ Backend)

```yaml
- name: Extract metadata for frontend image
  id: meta-frontend
  uses: docker/metadata-action@v5
  with:
    images: ${{ env.REGISTRY }}/${{ env.IMAGE_OWNER }}/basic-app-frontend
    ...

- name: Build and push frontend image
  uses: docker/build-push-action@v6
  with:
    context: ./frontend
    dockerfile: ./frontend/Dockerfile
    ...
```

**คำอธิบาย:**
- ทำแบบเดียวกับ backend แต่เปลี่ยน:
  - image name → `basic-app-frontend`
  - context → `./frontend`
  - dockerfile → `./frontend/Dockerfile`

---

### 1️⃣1️⃣ Steps - Summary (สรุปผล)

```yaml
- name: Print image summary
  if: github.event_name == 'push'
  run: |
    echo "## ✅ Images pushed to GHCR" >> $GITHUB_STEP_SUMMARY
    echo "" >> $GITHUB_STEP_SUMMARY
    echo "### Backend" >> $GITHUB_STEP_SUMMARY
    echo '```' >> $GITHUB_STEP_SUMMARY
    echo "${{ steps.meta-backend.outputs.tags }}" >> $GITHUB_STEP_SUMMARY
    echo '```' >> $GITHUB_STEP_SUMMARY
    ...
```

**คำอธิบาย:**
- `if: github.event_name == 'push'`: ทำงานเฉพาะเมื่อ push (ไม่ใช่ PR)
- `$GITHUB_STEP_SUMMARY`: ไฟล์พิเศษที่ GitHub ใช้แสดงสรุปใน UI
- แสดงรายการ tags ที่ถูก push ไปแล้ว

**ผลลัพธ์ที่เห็นใน GitHub Actions:**
```
✅ Images pushed to GHCR

Backend
ghcr.io/wiratatwork/basic-app-backend:test
ghcr.io/wiratatwork/basic-app-backend:sha-f38a4cc

Frontend
ghcr.io/wiratatwork/basic-app-frontend:test
ghcr.io/wiratatwork/basic-app-frontend:sha-f38a4cc
```

---

## 🎯 สรุป Flow ทั้งหมด

```
┌─────────────────────┐
│  Push to test       │
│  or Open PR         │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  1. Checkout code   │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  2. Login to GHCR   │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  3. Setup Buildx    │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  4. Build Backend   │
│     Image           │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  5. Push Backend    │ (เฉพาะ push event)
│     to GHCR         │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  6. Build Frontend  │
│     Image           │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  7. Push Frontend   │ (เฉพาะ push event)
│     to GHCR         │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  8. Show Summary    │
└─────────────────────┘
```

---

## 🚀 วิธีใช้งาน

### ดูผลการรัน Workflow
1. ไปที่ `https://github.com/<username>/<repo>/actions`
2. คลิกที่ workflow run ล่าสุด
3. ดูรายละเอียดแต่ละ step

### ดึง Image จาก GHCR
```bash
# Login (ครั้งแรก)
echo $GITHUB_TOKEN | docker login ghcr.io -u <username> --password-stdin

# Pull image
docker pull ghcr.io/wiratatwork/basic-app-backend:test
docker pull ghcr.io/wiratatwork/basic-app-frontend:test
```

### ดู Images ใน GHCR
1. ไปที่ `https://github.com/<username>?tab=packages`
2. จะเห็น packages ที่ถูก push ขึ้นมา

---

## 🔧 การปรับแต่ง

### เปลี่ยน branch ที่ต้องการ trigger
```yaml
on:
  push:
    branches:
      - main      # เปลี่ยนจาก test เป็น main
      - develop   # หรือเพิ่มหลาย branch
```

### เพิ่ม tag pattern อื่นๆ
```yaml
tags: |
  type=ref,event=branch
  type=ref,event=tag              # เพิ่ม: ใช้ git tag
  type=semver,pattern={{version}} # เพิ่ม: semantic version
  type=sha,prefix=sha-
```

### Build แค่ backend หรือ frontend อย่างเดียว
ลบส่วนที่ไม่ต้องการออก (ทั้ง metadata + build steps)

---

## ❓ FAQ

**Q: GITHUB_TOKEN หมดอายุไหม?**  
A: ไม่หมดอายุ มันถูกสร้างใหม่ทุกครั้งที่ workflow รัน

**Q: ทำไมต้อง push ถึงจะ push image?**  
A: เพื่อประหยัด quota — PR แค่ทดสอบว่า build ผ่านไหม ไม่จำเป็นต้อง push

**Q: Build ช้ามาก ทำไงดี?**  
A: ใช้ cache (มีอยู่แล้วในไฟล์นี้) และเขียน Dockerfile ให้ efficient

**Q: แก้ไข workflow แล้วต้อง push ใหม่ไหม?**  
A: ใช่ workflow อ่านจาก Git ต้อง commit + push ถึงจะมีผล

**Q: เห็น image ใน GHCR ไหม?**  
A: ต้องตั้งค่าให้ package เป็น public หรือ login ก่อน pull

---

## 📚 เอกสารเพิ่มเติม

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Docker Build Push Action](https://github.com/docker/build-push-action)
- [Working with GHCR](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry)
