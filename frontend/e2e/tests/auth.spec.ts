import { test, expect, loginAs } from '../fixtures/auth.fixture';

test.describe('Authentication', () => {
  test('redirects unauthenticated user to login', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/login/);
  });

  test('admin login success', async ({ page }) => {
    await loginAs(page, 'admin');

    await expect(page.locator('.user-label')).toContainText('admin');
    await expect(page.locator('.role-badge')).toContainText('Admin');
  });

  test('shows error on invalid credentials', async ({ page }) => {
    await page.goto('/login');
    await page.locator('#username').fill('admin');
    await page.locator('#password').fill('wrong-password');
    await page.getByRole('button', { name: 'เข้าสู่ระบบ' }).click();

    await expect(page.getByRole('alert')).toBeVisible();
  });

  test('logout redirects to login', async ({ page }) => {
    await loginAs(page, 'admin');
    await page.getByRole('button', { name: 'ออกจากระบบ' }).click();

    await expect(page).toHaveURL(/\/login/);

    await page.goto('/');
    await expect(page).toHaveURL(/\/login/);
  });

  test('session persists after reload', async ({ page }) => {
    await loginAs(page, 'admin');
    await page.reload();

    await expect(page).toHaveURL('/');
    await expect(page.locator('.user-label')).toContainText('admin');
  });
});
