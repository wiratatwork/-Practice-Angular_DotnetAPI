# Demo App — Machine Management

Full-stack application ประกอบด้วย Angular 22 (Frontend), ASP.NET Core 10 (Backend API), PostgreSQL 16 และ pgAdmin 4 รันผ่าน Docker Compose

---

## Tech Stack

| Layer      | Technology                  |
|------------|-----------------------------|
| Frontend   | Angular 22, Nginx           |
| Backend    | ASP.NET Core 10, EF Core    |
| Database   | PostgreSQL 16               |
| DB Admin   | pgAdmin 4                   |
| Container  | Docker Compose              |

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (รวม Docker Compose)

ตรวจสอบว่า Docker พร้อมใช้งาน:

```bash
docker --version
docker compose version
```

---

## การรัน Project

### 1. Clone หรือเปิด folder ของ project

```bash
cd "c:\Data\Drive D\app\test"
```

### 2. Build และ Start ทุก container

```bash
docker compose up --build
```

> ครั้งแรกจะใช้เวลาสักครู่เพื่อ build image  
> ครั้งถัดไปใช้ `docker compose up` (ไม่ต้อง `--build`)

### 3. รันแบบ Background (detached mode)

```bash
docker compose up --build -d
```

### 4. หยุด container

```bash
docker compose down
```

หยุดพร้อมลบ volume (ล้างข้อมูล database ด้วย):

```bash
docker compose down -v
```

---

รันคำสั่งเพื่อเพิ่มตารางใน pgAdmin (http://localhost:5050)
CREATE TABLE Machine (
    machineno VARCHAR(50) PRIMARY KEY,
    machinename VARCHAR(50),
    plant VARCHAR(10),
    status VARCHAR(10)
);

## URL ทั้งหมด

| Service        | URL                                      | หมายเหตุ                    |
|----------------|------------------------------------------|-----------------------------|
| **Frontend**   | http://localhost:4200                    | Angular App (ผ่าน Nginx)    |
| **Backend API**| http://localhost:5001                    | ASP.NET Core API            |
| **Swagger UI** | http://localhost:5001/swagger            | API Documentation           |
| **Health Check**| http://localhost:5001/health            | API Health Status           |
| **pgAdmin**    | http://localhost:5050                    | PostgreSQL Web Admin        |
| **PostgreSQL** | localhost:5432                           | Direct DB connection        |

---

## Swagger UI

เปิด browser แล้วไปที่:

```
http://localhost:5001/swagger
```

> Swagger จะแสดงก็ต่อเมื่อ `ASPNETCORE_ENVIRONMENT=Development` ซึ่งตั้งไว้ใน `docker-compose.yml` แล้ว

### API Endpoints ที่มี

| Method   | Endpoint                                        | คำอธิบาย                        |
|----------|-------------------------------------------------|----------------------------------|
| `GET`    | `/api/machine`                                  | ดึงข้อมูลเครื่องจักรทั้งหมด      |
| `GET`    | `/api/machine/{machineNo}`                      | ดึงข้อมูลตาม Machine No          |
| `GET`    | `/api/machine/search/{searchTerm}`              | ค้นหาตาม Machine No / Name       |
| `GET`    | `/api/machine/checkDuplicateName/{machineName}` | ตรวจสอบชื่อซ้ำ                   |
| `POST`   | `/api/machine`                                  | สร้างเครื่องจักรใหม่              |
| `PATCH`  | `/api/machine/{machineNo}`                      | อัปเดตข้อมูลเครื่องจักร           |
| `DELETE` | `/api/machine/{machineNo}`                      | ลบเครื่องจักร                    |

---

## pgAdmin

เปิด browser แล้วไปที่:

```
http://localhost:5050
```

### Login credentials

| Field    | Value               |
|----------|---------------------|
| Email    | admin@example.com   |
| Password | admin               |

### เพิ่ม Database Server ใน pgAdmin

1. คลิกขวาที่ **Servers** → **Register** → **Server...**
2. ตั้งชื่อ (เช่น `demo-db`) ในแท็บ **General**
3. ไปแท็บ **Connection** กรอกข้อมูล:

| Field             | Value         |
|-------------------|---------------|
| Host name/address | `postgres`    |
| Port              | `5432`        |
| Maintenance DB    | `demo`        |
| Username          | `sa`          |
| Password          | `Password123!`|

4. คลิก **Save**

---

## Database Connection (Direct)

สำหรับเชื่อมต่อผ่าน client อื่น เช่น DBeaver, TablePlus:

| Field    | Value          |
|----------|----------------|
| Host     | `localhost`    |
| Port     | `5432`         |
| Database | `demo`         |
| Username | `sa`           |
| Password | `Password123!` |

---

## Container Names

| Container        | Service    |
|------------------|------------|
| `demo-frontend`  | Frontend   |
| `demo-api`       | Backend API|
| `demo-db`        | PostgreSQL |
| `demo-pgadmin`   | pgAdmin    |

### ดู logs แต่ละ container

```bash
docker logs demo-api
docker logs demo-frontend
docker logs demo-db
```

### เข้า shell ใน container

```bash
docker exec -it demo-api bash
docker exec -it demo-db psql -U sa -d demo
```

---

## Startup Order

Container จะเริ่มตามลำดับ health check ดังนี้:

```
postgres (healthy) → api (healthy) → frontend
                  ↘ pgadmin
```
