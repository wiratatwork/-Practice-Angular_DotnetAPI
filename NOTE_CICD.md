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
อธิบายหลักการทำงานของ SSH Key
ทำไมเราสามารถเข้า SSH เครื่องได้ โดยมีไฟล์ SSH key ในเครื่อง Host
ใน VM Prod คำสั่ง  docker compose -f docker-compose.yml -f docker-compose.prod.yml pull ทำอะไร
DNS record A คืออะไร
@ คือไม่ต้องกรอกอะไรใน URL ใช่หรือไม่ แล้วมีค่าอื่น ๆ ที่นิยมใช้อีกหรือไม่
TTL คืออะไร ตั้งไว้ 1 สัปดาห์มีผลอย่างไร
certbot คืออะไร เอาไว้ทำอะไร
ipconfig /flushdns ทำหน้าที่อะไร
networks: demo-network: external: true ทำหน้าที่อะไร


ทำแล้ว
[OK] สร้าง VM ใน EC2 สำหรับ test และ prod และดึง Images ไปรัน
ปิด การเข้า port โดยตรงบน Prod
ทำให้ Auto Update App ที่ Droplet
ทำ README วิธีการรันในสภาพแวดล้อมต่าง ๆ