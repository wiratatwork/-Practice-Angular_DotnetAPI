1: ต้องส่ง Access Token ไปทุก Request ไหม อย่างไร
[ต้องส่งผ่าน HTTP Header ในรูปแบบ Authorization: Bearer <token>]

2: ถ้า Access Token อายุสั้นมีโอกาสถูกขโมยไหม
[มีโอกาสถูกขโมยได้ผ่านการดักจับข้อมูลหรือช่องโหว่ XSS]

3: ถ้ามีโอกาส แล้วมันจะรับประกันความปลอดภัยอย่างไร
[ลดความเสี่ยงโดยใช้ HTTPS, ตั้งอายุสั้น, และเก็บในหน่วยความจำแทน LocalStorage]

4: ทำไมการใช้ HTTPS โอกาสที่ Access Token จะหลุดน้อยลง
[เพราะ HTTPS เข้ารหัสข้อมูลที่รับส่งระหว่าง Browser และ Server ทำให้ผู้ไม่หวังดีไม่สามารถดักอ่านค่า Token ใน Network ได้]

5: มีหลักการ Validate Access Token และ Refresh Token อย่างไร
[Access Token ตรวจสอบลายเซ็น/วันหมดอายุ, Refresh Token ตรวจสอบกับฐานข้อมูลว่ายังไม่ถูกเพิกถอน]

6: Revoke Refresh Token อย่างไร และจะเกิดอะไรตามมา
[ลบหรือทำเครื่องหมายว่าถูกเพิกถอนในฐานข้อมูล ทำให้ Refresh Token นั้นใช้งานไม่ได้อีกต่อไป]

7: การ Revoke Refresh Token แต่ไม่ได้ Revoke Access Token หากหลุดไป ก็ยังสามารถใช้งานได้ในระยะสั้น ๆ ใช่หรือไม่
[ใช่ แต่จะใช้งานได้จนกว่า Access Token นั้นจะหมดอายุ ซึ่งมีอายุสั้นมาก]

8: การใช้ HTTPS จะช่วยให้ดักจับ Access Token ได้อย่างขึ้นใช่หรือไม่ อย่างไร
[ใช่ เพราะ HTTPS เข้ารหัสข้อมูลระหว่างทาง ทำให้ดักจับข้อมูล (Man-in-the-middle) ได้ยากมาก]

9: จะเรียก API Refresh ตอนไหนบ้าง
[เรียกเมื่อ Access Token หมดอายุ หรือเมื่อได้รับ Error 401 Unauthorized จาก Backend]

10: เก็บ Access Token และ Refresh Token ที่ไหน และมีข้อดีอย่างไร
[Access Token เก็บในหน่วยความจำ (JS memory) และ Refresh Token เก็บใน HttpOnly Cookie (ป้องกัน XSS)]

11: Cookies ทำงานอย่างไร ทำไมถึงปลอดภัย และเป็นวิธีที่เป็นที่นิยมไหม
[ส่งอัตโนมัติจาก Browser, HttpOnly ป้องกัน JavaScript เข้าถึงได้, เป็นวิธีมาตรฐานที่ปลอดภัยกว่าการเก็บใน localStorage]

12: มี auth interceptor ไปทำไม
[เพื่อดักจับและเพิ่ม Access Token ลงใน HTTP Request ทุกครั้งโดยอัตโนมัติ]

13: มี auth guard ไปทำไม
[เพื่อป้องกันการเข้าถึง Route ต่างๆ หากผู้ใช้ยังไม่ได้รับอนุญาตหรือ Token หมดอายุ]

14: อธิบาย session initialization เพิ่มเติม
[กระบวนการเรียก API เพื่อตรวจสอบสถานะ Session และออก Access Token ใหม่ทันทีเมื่อโหลดหน้าเว็บใหม่]

15: ทำไมถึง return Access Token และเก็บ Refresh Token ใน Cookies
[เพื่อให้ได้ความสมดุลระหว่างความสะดวกในการรักษา Session และความปลอดภัย (ป้องกัน XSS)]

16: ถ้า Access Token หลุดไป แต่ User กด Logout แล้ว Revoke เท่ากับได้ Access Token ไปก็ไม่มีประโยชน์ใช่หรือไม่ เพราะอะไร
[ถูกต้อง เพราะหาก Access Token นั้นมีอายุการใช้งานสั้น มันจะใช้งานได้เพียงช่วงเวลาสั้นๆ เท่านั้น]

17: ทำไม Refresh หน้าเเว็บ แล้ว Access Token จะหายไป แล้วทำไมถึงแก้ด้วย session initialization
[State ของ App หายไป ต้องใช้ Session Init เพื่อขอ Token ใหม่]

18: ถ้าเก็บ Access Token ใน Memory ปลอดภัยไหม และ Memory บนเครื่อง Client หรือ Server
[ปลอดภัยกว่า localStorage มาก โดยเก็บอยู่ในหน่วยความจำฝั่ง Client (Browser)]

19: ถ้า Refresh Token หมดอายุจะเกิดอะไรขึ้น
[ผู้ใช้งานจะถูกบังคับให้ Login ใหม่เพื่อสร้างคู่ Token ใหม่]

20: มีเหตุการณ์ใดบ้างที่จะทำให้ Validate Refresh Token ไม่ผ่าน
[Token หมดอายุ, ถูก Revoke ไปแล้ว, ไม่พบใน Database, หรือข้อมูลผู้ใช้ถูกระงับ]

21: Rotate Refresh Token คืออะไร
[กระบวนการออก Refresh Token ใหม่ทุกครั้งที่ใช้งาน และเพิกถอนอันเก่าทิ้งทันที]

22: Revoke Refresh Token คืออะไร
[การทำลาย Refresh Token ให้เป็นโมฆะก่อนวันหมดอายุ เพื่อความปลอดภัย]

23: "RefreshCookieName": "refresh_token" ถ้ามีเว็บอื่นที่ใช้ชื่อ cookie เหมือนกันจะเกิดอะไรขึ้น
[ไม่มีผล เพราะ Browser จะแยก Cookie ตาม Domain และ Path ของเว็บไซต์]

24: ทำไมต้อง Config ทั้งที่ appsettings.json และ docker-compose.yml
[appsettings.json สำหรับการตั้งค่าเริ่มต้น และ docker-compose.yml สำหรับการ Inject ค่าผ่าน Environment Variable]

25: const AUTH_HTTP_OPTIONS = { withCredentials: true, }; ทำหน้าที่อะไร
[ตั้งค่าให้ Browser ส่งและรับ Cookie ในการทำ Cross-Origin Request]

26: ทำไมถึงบอกว่า Cross-Origin Request ทั้ง ๆ ที่ก็เป็น localhost เหมือนกันแต่ต่าง port
[เพราะ Browser ถือว่าคนละพอร์ต คือคนละ Origin (Protocol, Domain, Port ต้องเหมือนกันเป๊ะ)]

27: ถ้าไม่มี const AUTH_HTTP_OPTIONS = { withCredentials: true, }; จะเกิดอะไรขึ้น
[Browser จะไม่ส่ง Cookie ไปยัง Backend ทำให้การยืนยันตัวตนด้วย Refresh Token ล้มเหลว]

28: shareReplay(1) ใน auth.service.ts คืออะไร ทำงานอย่างไร
[เก็บค่าล่าสุดที่ emit มาแล้วส่งค่าเดิมให้ Subscriber ใหม่ทันทีโดยไม่ประมวลผลใหม่]

29: หาก Update Code Backend ข้อมูล Cookies จะสูญหาย และทำให้ผู้ใช้งานต้อง Login ใหม่หรือไม่ เพราะอะไร
[ข้อมูล Cookie จะไม่หายเว้นแต่มีการเปลี่ยนชื่อ Domain หรือการตั้งค่า Cookie ที่ผูกกับตัว Server]

30: AppDbContextModelSnapshot.cs มีไฟล์นี้ไปทำไม
[ไฟล์ที่ EF Core ใช้เก็บสถานะปัจจุบันของฐานข้อมูลเพื่อใช้เปรียบเทียบและสร้าง Migration ถัดไป]

31: จะมีไฟล์นี้ไปทำไม ในเมื่อมีไฟล์ประจำการ Add Migrations แต่ละครั้งแล้ว
[เพราะ Snapshot รวมสถานะสุดท้ายทั้งหมดไว้ ช่วยให้ EF Core สร้าง Migration ใหม่ได้อย่างรวดเร็วโดยไม่ต้องย้อนไล่ไฟล์ Migration ทั้งหมด]

32: ทำไมถึงต้องมี 2 Token จริง ๆ มีแค่ Refresh Token ใน HTTP Cookie ก็น่าจะปลอดภัยแล้ว และระบุตัวตน Logged User ได้แล้ว
[Access Token เพื่อประสิทธิภาพ (ไม่ต้อง Query DB ทุก Request) ส่วน Refresh Token เพื่อความปลอดภัยและใช้สร้าง Access Token ใหม่เมื่อหมดอายุ]
