# Deployment Handbook

เอกสารนี้กำหนดมาตรฐานการ deploy ของโปรเจกต์ เพื่อให้ทีมพัฒนา, ผู้ทดสอบ, และผู้อนุมัติใช้งาน flow เดียวกัน

## เป้าหมาย

ต้องการให้การปล่อยระบบเป็น 2 ระดับดังนี้:

1. โค้ดจากนักพัฒนาจะถูกรวมเข้า `test`
2. เมื่อมีการเปลี่ยนแปลงบน `test` จะรัน CI/CD อัตโนมัติ
3. หากผ่านเงื่อนไข จะ deploy ไปยัง Railway environment `test` อัตโนมัติ
4. หัวหน้าหรือผู้รับผิดชอบทดสอบบน `test`
5. เมื่อทดสอบเสร็จและอนุมัติ Pull Request เข้า `main`
6. ระบบจะ deploy ไปยัง Railway environment `production` อัตโนมัติ

## ภาพรวม Flow

```text
feature/* -> Pull Request -> test -> CI ผ่าน -> Auto Deploy to Railway Test
                                                |
                                                v
                                     หัวหน้าทดสอบและอนุมัติ
                                                |
                                                v
                                   Pull Request test -> main
                                                |
                                                v
                                 CI/Release ผ่าน -> Auto Deploy to Production
```

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

ใน Railway ให้มีอย่างน้อย 2 environment:

- `test`
- `production`

ข้อกำหนด:

- ทั้ง 2 environment ต้องแยกค่า environment variables ออกจากกัน
- database ของ `test` และ `production` ต้องไม่ใช้ชุดเดียวกัน
- URL, API keys, และ secrets ของ production ต้องไม่ถูกใช้ใน `test`

## พฤติกรรม CI/CD ที่ต้องการ

### 1. เมื่อมีการ merge หรือ push เข้า `test`

ระบบ CI/CD ต้องทำงานตามลำดับนี้:

1. Checkout source code
2. ติดตั้ง dependencies
3. Build application
4. Run tests
5. Run lint หรือ validation ที่จำเป็น
6. ถ้าทุกอย่างผ่าน ให้ deploy ไป Railway environment `test`

ผลลัพธ์ที่ต้องการ:

- ทีมสามารถเข้าไปทดสอบระบบล่าสุดได้บน environment `test`
- ถ้า CI ไม่ผ่าน ต้องไม่ deploy

### 2. เมื่อ Pull Request จาก `test` ไป `main` ได้รับอนุมัติและ merge

ระบบ CI/CD ต้องทำงานตามลำดับนี้:

1. Checkout source code จาก `main`
2. ติดตั้ง dependencies
3. Build application สำหรับ production
4. Run tests/release checks ที่จำเป็น
5. ถ้าทุกอย่างผ่าน ให้ deploy ไป Railway environment `production`

ผลลัพธ์ที่ต้องการ:

- production จะรับเฉพาะโค้ดที่ผ่านการทดสอบใน `test` แล้ว
- ลดความเสี่ยงจากการ deploy ข้ามขั้นตอน

## ผู้รับผิดชอบในแต่ละขั้น

- นักพัฒนา:
  - พัฒนาใน branch งาน
  - เปิด Pull Request เข้า `test`
  - แก้ไข issue ที่ทำให้ CI ไม่ผ่าน
- หัวหน้าหรือผู้ทดสอบ:
  - ทดสอบบน Railway environment `test`
  - อนุมัติ Pull Request จาก `test` ไป `main`
- ระบบ CI/CD:
  - ตรวจสอบคุณภาพโค้ด
  - deploy อัตโนมัติตาม branch เป้าหมาย

## กฎที่ควรตั้งใน GitHub

แนะนำให้ตั้งค่า branch protection ดังนี้

### สำหรับ `test`

- อนุญาตให้ merge ผ่าน Pull Request
- บังคับให้ CI checks ผ่านก่อน merge
- แนะนำให้ห้าม push ตรง ยกเว้นผู้ดูแลระบบที่จำเป็น

### สำหรับ `main`

- ห้าม push ตรง
- บังคับ Pull Request เท่านั้น
- บังคับ approval อย่างน้อย 1 คนจากหัวหน้าหรือผู้รับผิดชอบ
- บังคับให้ CI checks ผ่านก่อน merge

## สิ่งที่ต้องตั้งค่าใน GitHub

ควรมี GitHub Actions อย่างน้อย 2 workflow:

1. `deploy-test`
  - ทำงานเมื่อมี push ไปที่ `test`
  - build, test, และ deploy ไป Railway `test`
2. `deploy-production`
  - ทำงานเมื่อมี push ไปที่ `main`
  - build, test, และ deploy ไป Railway `production`

ควรเตรียม GitHub Secrets/Variables เช่น:

- `RAILWAY_TOKEN`
- `RAILWAY_PROJECT_ID`
- `RAILWAY_SERVICE_ID`
- ค่าที่ใช้แยก environment หาก workflow ต้องอ้างอิงคนละ target

หมายเหตุ:

- หาก `test` และ `production` ใช้ service หรือ project คนละตัว ให้แยก secrets ให้ชัดเจน
- ไม่ควร hardcode secret ลงใน repository

## สิ่งที่ต้องตั้งค่าใน Railway

ต้องตรวจสอบให้พร้อมดังนี้:

- มี environment `test` และ `production`
- แต่ละ environment มี variables ครบถ้วน
- ตั้งค่า domain หรือ URL แยกกันชัดเจน
- รองรับการ deploy ผ่าน GitHub Actions หรือ Railway CLI

ตัวอย่างชื่อค่าที่มักต้องแยก:

- `ASPNETCORE_ENVIRONMENT`
- `DATABASE_URL`
- `JWT_SECRET`
- `CORS_ORIGIN`
- `API_BASE_URL`

## Release Policy

กติกาการปล่อย production:

- โค้ดที่จะขึ้น production ต้องมาจาก `test` เท่านั้น
- ต้องผ่านการทดสอบบน Railway environment `test` ก่อน
- ต้องได้รับการอนุมัติ Pull Request ก่อน merge เข้า `main`
- เมื่อ merge เข้า `main` แล้ว ระบบต้อง deploy production อัตโนมัติ

## ขั้นตอนการทำงานของทีม

### ฝั่งนักพัฒนา

1. pull code ล่าสุดจาก branch ที่เกี่ยวข้อง
2. สร้าง branch งานใหม่จาก `test` หรือแนวทาง branch ของทีม
3. พัฒนาและทดสอบในเครื่อง
4. push branch งานขึ้น GitHub
5. เปิด Pull Request เข้า `test`
6. รอ CI ผ่าน
7. merge เข้า `test`

### ฝั่งหัวหน้าหรือผู้อนุมัติ

1. เข้าใช้งานระบบบน Railway environment `test`
2. ทดสอบตาม checklist หรือ test scenario
3. ถ้าผ่าน ให้ approve Pull Request จาก `test` ไป `main`
4. merge เข้า `main`
5. ตรวจสอบผลการ deploy production

## Definition of Done ก่อนขึ้น Production

ก่อน merge เข้า `main` ต้องมีครบ:

- CI บน `test` ผ่าน
- deploy ไป `test` สำเร็จ
- ทดสอบ business flow สำคัญแล้ว
- ไม่มี bug blocker หรือ critical issue
- Pull Request ได้รับ approval ตามสิทธิ์ที่กำหนด

## ความเสี่ยงที่ต้องระวัง

- ถ้าอนุญาตให้ push ตรงเข้า `main` อาจทำให้ bypass ขั้นตอนทดสอบ
- ถ้า `test` กับ `production` ใช้ environment variables ชุดเดียวกัน อาจเกิดการปนกันของข้อมูล
- ถ้า CI รันไม่ครบทั้ง build, test, และ validation ความเสี่ยงจะถูกส่งต่อถึง production
- ถ้าไม่มี branch protection กระบวนการอนุมัติอาจถูกข้าม

## สรุปนโยบาย

นโยบาย deploy ของโปรเจกต์นี้คือ:

- รวมงานของนักพัฒนาเข้า `test`
- ให้ CI/CD ตรวจสอบและ deploy ไป Railway `test` อัตโนมัติ
- ให้หัวหน้าทดสอบและอนุมัติการขึ้น production
- เมื่อ merge เข้า `main` ให้ deploy ไป Railway `production` อัตโนมัติ

แนวทางนี้ช่วยให้ `test` เป็น staging area สำหรับตรวจสอบคุณภาพก่อนปล่อยจริง และทำให้ `main` เป็นแหล่งอ้างอิงของ production อย่างชัดเจน