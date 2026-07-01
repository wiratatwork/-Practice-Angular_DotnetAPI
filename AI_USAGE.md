ใช้ Amazon Kiro เป็น Editor ใช้ Model Haiku 4.5 กับ Sonnet 4.6
ผู้สัมภาษร์มีหน้าที่วางลำดับการทำงานของโปรแกรม และ Prompt ทีละส่วน เช่น
ให้ AI ช่วย สร้าง UI Machine Component / Machinecontroller API และทดสอบ Validation

ตัวอย่าง Prompt
ให้ Gemini สร้างคำสั่งสร้างตาราง
machineno varchar50
machinename varchar50
plant varchar10
status varchar10


แก้ไขให้ใช้ PostgreSQL แทน ส่วน credential ใน appsettings.json ให้ดูจาก docker-compose.yml


ที่ MachineController.cs
1. เพิ่ม Validation ชื่อ MachineNo ห้ามซ้ำในการเพิ่ม-แก้ไข โดยเทียบจาก trim and lowercase
2. ต้องการตั้งชื่อ ออกแบบ API ด้วยแนวคิด RESTFul API

- ต้องการใช้ค่า dropdown Status เป็น "placeholder --เลือก Status ---" "ONLINE","OFFLINE" ที่ machine.component.html
OK เช็ค machine.service.ts เรื่อง url
OK -> ปิดการแก้ไข ทดสอบชื่อซ้ำกรณี update machine ว่าใช้ชื่อเดิมได้

ที่ MachineController.cs จะมี api SearchMachines 
ต้องการเพิ่มช่องค้นหาที่ machine.component.html .ให้มีช่อง ค้นหา โดย debource 300ms


ส่วนที่แก้ไข
เนื่องจาก AI Gen Model Machine ออกมาไม่ตรงกับ database จึงได้แก้ไขและเพิ่ม data annotation เพื่อทำ backend validation
