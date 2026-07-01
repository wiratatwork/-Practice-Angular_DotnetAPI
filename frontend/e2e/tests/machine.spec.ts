import { test, expect } from '../fixtures/auth.fixture';
import { deleteE2eMachines, deleteMachine } from '../helpers/machine-api';

test.describe('Machine page', () => {
  test.beforeEach(async ({ request, baseURL }) => {
    await deleteE2eMachines(request, baseURL!);
  });

  test('shows empty state for admin', async ({ authenticatedPage: page }) => {
    await page.goto('/machine');

    await expect(page.getByText('ยังไม่มีข้อมูลเครื่องจักร')).toBeVisible();
  });

  test('admin sees add button', async ({ authenticatedPage: page }) => {
    await page.goto('/machine');

    await expect(page.getByRole('button', { name: 'เพิ่มเครื่องจักร' })).toBeVisible();
  });

  test('user cannot see add button', async ({ page }) => {
    await page.goto('/login');
    await page.locator('#username').fill('user');
    await page.locator('#password').fill('User@1234');
    await page.getByRole('button', { name: 'เข้าสู่ระบบ' }).click();
    await expect(page).toHaveURL('/');

    await page.goto('/machine');
    await expect(page.getByRole('button', { name: 'เพิ่มเครื่องจักร' })).toBeHidden();
  });

  test('admin can create a machine', async ({ authenticatedPage: page, request, baseURL }) => {
    const machineNo = `E2E-${Date.now()}`;
    const machineName = `E2E Machine ${Date.now()}`;

    try {
      await page.goto('/machine');
      await page.getByRole('button', { name: 'เพิ่มเครื่องจักร' }).click();

      await expect(page.getByRole('dialog', { name: 'เพิ่มเครื่องจักร' })).toBeVisible();

      await page.locator('#machineNo').fill(machineNo);
      await page.locator('#machineName').fill(machineName);
      await page.locator('#plant').fill('P1');
      await page.locator('#status').selectOption('ONLINE');

      await page
        .getByRole('dialog', { name: 'เพิ่มเครื่องจักร' })
        .getByRole('button', { name: 'เพิ่มเครื่องจักร' })
        .click();

      await expect(page.getByText('เพิ่มเครื่องจักรสำเร็จ')).toBeVisible({ timeout: 10000 });
      await expect(page.locator('table.data-table')).toContainText(machineNo);
      await expect(page.locator('table.data-table')).toContainText(machineName);
    } finally {
      await deleteMachine(request, baseURL!, machineNo);
    }
  });
});
