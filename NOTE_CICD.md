เปลี่ยน image ใน docker-compose.test.yml
[ใช้ Image ที่ Build แล้วจาก GHCR แทน]
ทำไมใน test และ prod ไม่มี postgres
[เพราะใช้วิธี Override มาจาก docker-compose.yml (ไฟล์ Base)]
Amazon RDS คืออะไร ทำไมถึงแทนที่ PostgreSQL Container ได้
[Amazon Relational Database System เป็น Database ที่ AWS ดูแลแทนให้ทั้งหมด เช่น Backup, Monitor, Replica]
GHCR (GitHub Container Registry) คืออะไร
[ที่จัดเก็บ Container Images ของ Github]
แนะนำ Ruleset ที่นิยมตั้ง
[Restrict updates = บังคับเข้า PR เท่านั้น
Restrict deletions = ห้ามลบ main
Required a pull request before merging = บังคับ Merge ผ่าน Pull Request
Block force pushes = ป้องกัน Push แบบ force
Require status checks to pass = ต้องให้ Github Actions ผ่าน
Require branches to be up to date before merging = บังคับให้ Dev ต้อง Rebase ไปแก้ใน Branch feature ก่อนเสมอ]
ทำไมต้อง SSH ไปติดตั้ง Up Docker ใน VM เอง
[ครั้งแรกเท่านั้น เพื่อดึง GHCR Images]
จะตรวจสอบอย่างไรว่าเรามี Images ใดบน GHCR บ้าง
[ดูจาก GitHub -> Profile -> Packages]
สามารถตั้งค่าให้ใคร Approve PR ได้
[สามารถทำได้บน Github Organization ส่วน Personal Repo สามารถกำหนดได้เพียงจำนวน Approval เท่านั้น]
ต้องมี 2 VM สำหรับแต่ละ Env ใช่หรือไม่
[ใช่]
ในแต่ละ VM จะรันทั้ง 4 Containers ใช่หรือไม่
[ใช่ หรือสามารถแยก PostgresQL มาใช้บริการ RDS ของ Cloud Provider ได้]
อธิบายหลักการทำงานของ SSH Key
[Gen Public / Private Key -> ส่ง Public Key เข้า VM -> เมื่อ SSH VM ส่ง Challenge ให้ Client ประทับตราได้ Signature และส่ง Signature ให้ VM Verify]
ทำไมเราสามารถเข้า SSH เครื่องได้ โดยมีไฟล์ SSH key ในเครื่อง Host
[Client ประทับตรา Challenge ด้วย Private Key และส่งให้ Server ที่มี Public Key Verify]
ใน VM Prod คำสั่ง  docker compose -f docker-compose.yml -f docker-compose.prod.yml pull ทำอะไร
[ดึง Images จาก docker-compose และ Override ด้วย docker-compose.prod]
DNS record A คืออะไร
[A = Address บอกว่า Domain นี้ชี้ไปยัง IP Address ใด]
DNS คืออะไร ทั่วโลกใช้ Server เดียวกันไหม อย่างไร มีโอกาสที่ domain เดียวกัน จะชี้ Ip address คนละตัวเมื่ออยู่ต่างประเทศกันไหม
[Domain Name System ระบบค้นหา IP Address มีหลาย Server และมีโอกาสที่ต่างประเทศกัน จะได้ IP ต่างกัน เพื่อคนหา Server ที่ใกล้ที่สุด]
Host @ คือไม่ต้องกรอกอะไรใน URL ใช่หรือไม่ แล้วมีค่าอื่น ๆ ที่นิยมใช้อีกหรือไม่
[ใช่ อื่น ๆ ที่นิยมเช่น www]
TTL คืออะไร ตั้งไว้ 1 สัปดาห์มีผลอย่างไร
[ระยะเวลาที่ให้ DNS Server Cache IP ไว้ เพื่อไม่ต้องค้นหาใหม่ หากตั้งไว้นาน เมื่อเปลี่ยน IP User อาจได้เลข IP เก่า]
ipconfig /flushdns ทำหน้าที่อะไร
[ล้าง Cache IP Address บนเครื่อง ให้ไปถาม DNS Server ใหม่]
certbot คืออะไร เอาไว้ทำอะไร
[ขอ ติดตั้ง ต่ออายุ SSL/TSL Certificate]
ทำไมต้องติดตั้ง SSL/TSL Certificate
[เอกสาร Ditital ที่ออกโดยองค์กรกลาง เช่น Let's Encrypt เมื่อ User เข้าผ่าน URL -> Browser ขอ SSL/TSL Certiciate ในนั้นจะบอกว่า Cert เป็นของ Domain นั้นจริง ๆ หรือไม่ และ Cert ก็ทำหน้าที่ Encrypt ข้อมูลระหว่าง Client Server ด้วย
SSL/TLS Certificate คือเอกสารดิจิทัลที่ออกโดยองค์กรกลาง (Certificate Authority เช่น Let's Encrypt) เพื่อยืนยันว่าเว็บไซต์เป็นเจ้าของ Domain นั้นจริง เมื่อผู้ใช้เข้าเว็บไซต์ Browser จะตรวจสอบความถูกต้องของ Certificate หากผ่านการตรวจสอบ ทั้ง Client และ Server จะสร้างกุญแจเข้ารหัสร่วมกัน (Session Key) แล้วใช้กุญแจนี้เข้ารหัสข้อมูลทั้งหมดที่รับส่งระหว่างกัน ทำให้ข้อมูลไม่สามารถถูกดักอ่านหรือแก้ไขได้ระหว่างทาง]
http กับ https ส่งผลต่อความปลอดภัยอย่างไร
[https เข้ารหัสข้อมูล และยืนยันว่า Domain มีความน่าเชื่อถือ เพราะมีเอกสารยืนยันจาก CA]
ทำไมต้องซื้อ Certificate ทั้งๆที่ก็มี Certbot
[Cert ที่เข้ารหัสได้รัดกุมกว่า ต่ออายุได้นานกว่า ปกติ 90 วัน หรือมีค่าประกันกรณีถูกดักจับข้อมูล]
docker image prune -f คืออะไร
[ลบ Images ที่ไม่ได้ใช้งาน ประหยัดพื้นที่ Disk]
วิธีนี้ (GitHub action) ส่งให้กระทบต่อผู้ใช้งานอย่างไร
[Downtime สั้น ๆ Long Request อาจถูกตัด หรือ Code 502]
https ใช้ port 443 ใช่ไหม
[ใช่]
VM กับ VPS คือสิ่งเดียวกันไหม
[เกือบเป็นสิ่งเดียวกัน VPS คือ VM ที่มาจากผู้ให้บริการ แต่ VM อาจมาจากการตั้ง Local ได้ด้วย]
ด้วยการเข้าแบบ SSH Key จะเข้าด้วย Computer เครื่องอื่นได้อย่างไร โดยมี 2 กรณี คือ Copy Private Key ไป และไม่ให้ Private Key
[ควรสร้าง SSH Key คู่ใหม่ และเพิ่ม Public Key ใน VM]
สามารถมี Public Key ใน VM มากกว่า 1 Key ไหม ถ้าได้ หมายความว่าถ้ามีไฟล์ Private Key คู่ใดก็ได้ในเครื่องเรา ก็จะเข้าได้ใช่ไหม
[ใช่]