import { test, expect } from '../fixtures/auth.fixture';
import { deleteE2eMachines, deleteMachine } from '../helpers/machine-api';

test.describe('Machine page', () => {
  // กลุ่ม test สำหรับ machine page
  test.beforeEach(async ({ request, baseURL }) => {
    // รันทุกครั้งก่อนที่จะทำ test
    await deleteE2eMachines(request, baseURL!); // ลบเครื่องจักรที่สร้างขึ้นมาในการทดสอบที่มีชื่อขึ้นต้นด้วย 'E2E-'
  });

  test('shows empty state for admin', async ({ authenticatedPage: page }) => {
    // test สำหรับแสดงสถานะว่างสำหรับ admin
    await page.goto('/machine'); // ไปหน้า machine

    await expect(page.getByText('ยังไม่มีข้อมูลเครื่องจักร')).toBeVisible(); // ตรวจสอบว่ามีข้อมูลเครื่องจักร
  });

  test('admin sees add button', async ({ authenticatedPage: page }) => {
    // test สำหรับแสดงปุ่มเพิ่มเครื่องจักรสำหรับ admin
    await page.goto('/machine'); // ไปหน้า machine

    await expect(page.getByRole('button', { name: 'เพิ่มเครื่องจักร' })).toBeVisible(); // ตรวจสอบว่ามีปุ่มเพิ่มเครื่องจักร
  });

  test('user cannot see add button', async ({ page }) => {
    // test user ทั่วไปไม่เห็นปุ่มเพิ่มเครื่องจักร
    await page.goto('/login'); // ไปหน้า login
    await page.locator('#username').fill('user'); // กรอกชื่อผู้ใช้
    await page.locator('#password').fill('User@1234'); // กรอกรหัสผ่าน
    await page.getByRole('button', { name: 'เข้าสู่ระบบ' }).click(); // คลิกปุ่มเข้าสู่ระบบ
    await expect(page).toHaveURL('/'); // ตรวจสอบว่าเป็นหน้า /

    await page.goto('/machine'); // ไปหน้า machine
    await expect(page.getByRole('button', { name: 'เพิ่มเครื่องจักร' })).toBeHidden(); // ตรวจสอบว่าไม่มีปุ่มเพิ่มเครื่องจักร
  });

  test('admin can create a machine', async ({ authenticatedPage: page, request, baseURL }) => {
    // test สำหรับสร้างเครื่องจักรสำหรับ admin
    const machineNo = `E2E-${Date.now()}`; // สร้างรหัสเครื่องจักร
    const machineName = `E2E Machine ${Date.now()}`; // สร้างชื่อเครื่องจักร

    try {
      await page.goto('/machine'); // ไปหน้า machine
      await page.getByRole('button', { name: 'เพิ่มเครื่องจักร' }).click(); // คลิกปุ่มเพิ่มเครื่องจักร

      await expect(page.getByRole('dialog', { name: 'เพิ่มเครื่องจักร' })).toBeVisible(); // ตรวจสอบว่ามี modal เพิ่มเครื่องจักร

      await page.locator('#machineNo').fill(machineNo); // กรอกรหัสเครื่องจักร
      await page.locator('#machineName').fill(machineName); // กรอกชื่อเครื่องจักร
      await page.locator('#plant').fill('P1'); // กรอกพื้นที่
      await page.locator('#status').selectOption('ONLINE'); // เลือกสถานะ

      await page
        .getByRole('dialog', { name: 'เพิ่มเครื่องจักร' })
        .getByRole('button', { name: 'เพิ่มเครื่องจักร' }) // คลิกปุ่มเพิ่มเครื่องจักร ที่อยู่ใน modal
        .click();

      await expect(page.getByText('เพิ่มเครื่องจักรสำเร็จ')).toBeVisible({ timeout: 10000 }); // ตรวจสอบว่ามีข้อความ 'เพิ่มเครื่องจักรสำเร็จ'
      await expect(page.locator('table.data-table')).toContainText(machineNo); // ตรวจสอบว่ามีรหัสเครื่องจักรในตาราง
      await expect(page.locator('table.data-table')).toContainText(machineName); // ตรวจสอบว่ามีชื่อเครื่องจักรในตาราง
    } finally {
      await deleteMachine(request, baseURL!, machineNo); // ลบเครื่องจักร
    }
  });
});
