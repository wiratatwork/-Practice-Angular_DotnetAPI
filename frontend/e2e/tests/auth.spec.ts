import { test, expect, loginAs } from '../fixtures/auth.fixture';

test.describe('Authentication', () => {
  // กลุ่ม test สำหรับ authentication
  test('redirects unauthenticated user to login', async ({ page }) => {
    // test สำหรับ redirect ผู้ใช้ที่ไม่ได้รับการยืนยันไปหน้า login
    await page.goto('/'); // ไปหน้า /
    await expect(page).toHaveURL(/\/login/); // ตรวจสอบว่าเป็นหน้า login
  });

  test('admin login success', async ({ page }) => {
    // test สำหรับ login ผู้ใช้ที่เป็น admin
    await loginAs(page, 'admin'); // login

    await expect(page.locator('.user-label')).toContainText('admin'); // ตรวจสอบว่าเป็นผู้ใช้ admin
    await expect(page.locator('.role-badge')).toContainText('Admin'); // ตรวจสอบว่าเป็นผู้ใช้ admin
  });

  test('shows error on invalid credentials', async ({ page }) => {
    // test สำหรับแสดง error ที่ไม่ถูกต้อง
    await page.goto('/login'); // ไปหน้า login
    await page.locator('#username').fill('admin'); // กรอกชื่อผู้ใช้
    await page.locator('#password').fill('wrong-password'); // กรอกรหัสผ่านที่ไม่ถูกต้อง
    await page.getByRole('button', { name: 'เข้าสู่ระบบ' }).click(); // คลิกปุ่มเข้าสู่ระบบ

    await expect(page.getByRole('alert')).toBeVisible(); // ตรวจสอบว่ามี error
  });

  test('logout redirects to login', async ({ page }) => {
    // test สำหรับ logout ไปหน้า login
    await loginAs(page, 'admin'); // login
    await page.getByRole('button', { name: 'ออกจากระบบ' }).click(); // คลิกปุ่มออกจากระบบ

    await expect(page).toHaveURL(/\/login/); // ตรวจสอบว่าเป็นหน้า login

    await page.goto('/'); // ไปหน้า /
    await expect(page).toHaveURL(/\/login/); // ตรวจสอบว่าเป็นหน้า login
  });

  test('session persists after reload', async ({ page }) => {
    await loginAs(page, 'admin'); // login
    await page.reload(); // รีโหลดหน้า

    await expect(page).toHaveURL('/'); // ตรวจสอบว่าเป็นหน้า /
    await expect(page.locator('.user-label')).toContainText('admin'); // ตรวจสอบว่าเป็นผู้ใช้ admin
  });
});
