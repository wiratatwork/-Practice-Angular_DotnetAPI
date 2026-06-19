# NOTE: Docker & Proxy Config Q&A

## Proxy Config

**1. `frontend/proxy.conf.json` ทำหน้าที่อะไร ไม่มีจะเกิดอะไรขึ้น?**

ใช้ตอนรัน `ng serve` (dev บนเครื่อง) — บอก Angular Dev Server ว่า request ที่ขึ้นต้นด้วย `/api` ให้ forward ไปที่ `http://localhost:5001` แทน
ถ้าไม่มี → Angular จะส่ง request `/api/...` ไปที่ `localhost:4200` ตัวเอง → ได้รับ 404 ทุก request

**2. `frontend/proxy.conf.docker.json` ทำหน้าที่อะไร ไม่มีจะเกิดอะไรขึ้น?**

เหมือนข้อ 1 แต่ใช้ตอนรันใน Docker dev mode — target เปลี่ยนจาก `localhost:5001` เป็น `http://api:5001` (ชื่อ service ใน docker network)
ถ้าไม่มี → Angular Dev Server ใน container จะหา `localhost:5001` ซึ่งไม่มีใน container → request `/api` fail ทั้งหมด

---

## Docker Compose

**3. `docker compose -f docker-compose.yml -f docker-compose.dev.yml up` ทำงานยังไง?**

Docker Compose **merge** ไฟล์ทั้งสองเข้าด้วยกัน โดยไฟล์หลังทับไฟล์แรก (override):
- `docker-compose.yml` = base config (production-like): image, port, network, postgres, pgadmin
- `docker-compose.dev.yml` = override เฉพาะ dev: เปลี่ยน Dockerfile เป็น `Dockerfile.dev`, เพิ่ม volumes สำหรับ hot-reload, เปลี่ยน port frontend เป็น `4201:4200`

ผลลัพธ์คือ service ที่ซ้ำกันจะถูก merge โดย dev ทับ base, service ที่ไม่ซ้ำ (postgres, pgadmin) คงอยู่ครบ

**4. ทำไม `docker-compose.dev.yml` ไม่ต้องมี container ของ pgAdmin และ postgres?**

เพราะมันเป็น **override file** ไม่ใช่ standalone — เมื่อใช้ร่วมกับ `docker-compose.yml` postgres และ pgadmin ถูก define ไว้แล้วใน base file และถูก inherit มาโดยอัตโนมัติ
ไม่จำเป็นต้องเขียนซ้ำ ถ้าไม่ต้องการเปลี่ยนค่าอะไร

**5. `volumes: - ./backend:/src - api_bin:/src/bin - api_obj:/src/obj` หมายถึงอะไร?**

| Volume | ความหมาย |
|---|---|
| `./backend:/src` | mount โฟลเดอร์ `backend` บนเครื่องเข้า container ที่ `/src` → แก้โค้ดแล้ว hot-reload ได้ทันที |
| `api_bin:/src/bin` | ใช้ named volume แยกเก็บ build output (`bin`) ไว้ใน Docker → ไม่ให้ mount ทับจากเครื่อง host ซึ่งอาจว่างเปล่าหรือ platform ต่างกัน |
| `api_obj:/src/obj` | เหมือนกัน สำหรับ intermediate build files (`obj`) → ป้องกัน dotnet build error จาก platform mismatch |

---

## Port Mapping

**6. ทำไม container frontend ถึงมี port map 2 รายการ คือ `4201:4200` ใช้งานได้ แต่ `4200:80` เข้าไม่ได้?**

เพราะทั้งสองใช้ Dockerfile **คนละตัว**:

| Config | Dockerfile | Process ใน container | Port ใน container | Port บนเครื่อง |
|---|---|---|---|---|
| `docker-compose.yml` | `Dockerfile` (production) | nginx serve static files | **80** | 4200 |
| `docker-compose.dev.yml` | `Dockerfile.dev` (dev) | `ng serve` | **4200** | 4201 |

`4200:80` เข้าไม่ได้ → แปลว่า Dockerfile (production) build ยังไม่สมบูรณ์ หรือ nginx config มีปัญหา ให้ตรวจ `frontend/Dockerfile` และ `frontend/nginx.conf`
