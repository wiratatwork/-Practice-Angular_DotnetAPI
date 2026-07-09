import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests', // ตำแหน่งของไฟล์ tests
  fullyParallel: false, // ไม่ทำงานพร้อมกันหลาย test
  forbidOnly: !!process.env.CI, // ยอมให้รันบาง test เท่านั้นหรือไม่
  retries: process.env.CI ? 2 : 0, // ถ้าเป็น CI จะ retry 2 ครั้ง
  workers: process.env.CI ? 1 : undefined, // ถ้าเป็น CI จะทำงานพร้อมกัน 1 ตัว
  reporter: [['html'], ['github']], // รายงานผลการทดสอบใน html และ github
  use: {
    baseURL: process.env.BASE_URL ?? 'http://127.0.0.1:4200',
    trace: 'on-first-retry', // บันทึก trace เมื่อ retry ครั้งแรก
    screenshot: 'only-on-failure', // จะจับภาพหน้าจอเฉพาะเมื่อมีการล้มเหลว
    video: 'retain-on-failure', // จะบันทึกวิดีโอเฉพาะเมื่อมีการล้มเหลว
    ...devices['Desktop Chrome'], // ใช้ device Desktop Chrome
  },
  projects: [
    {
      name: 'chromium', // ใช้ browser chromium
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
