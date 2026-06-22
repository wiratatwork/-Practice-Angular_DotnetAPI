เปลี่ยน image ใน docker-compose.test.yml
ทำไมใน test และ prod ไม่มี postgres
Amazon RDS คืออะไร ทำไมถึงแทนที่ PostgreSQL Container ได้
GHCR (GitHub Container Registry)
แนะนำ Ruleset ที่นิยมตั้ง
ทำไมต้อง SSH ไปติดตั้ง Up Docker ใน EC2 เอง
[ครั้งแรกเท่านั้น เพื่อดึง GHCR Images]
อธิบาย .github\workflows\ci-cd-test.yml โดยละเอียด
จะตรวจสอบอย่างไรว่าเรามี Images ใดบน GHCR บ้าง
[ดูจาก GitHub -> Profile -> Packages]
สามารถตั้งค่าให้ใคร Approve PR ได้
[ต้องใช้ GitHub Organizations จึงจะได้ Full Feature]
ต้องมี 2 VM สำหรับแต่ละ Env ใช่หรือไม่
ในแต่ละ VM จะรันทั้ง 4 Containers ใช่หรือไม่

ทำแล้ว
สร้าง VM ใน EC2 สำหรับ test และ prod และดึง Images ไปรัน