import { test, expect } from '../fixtures/auth.fixture';

test.describe('Navigation', () => {
  test('home content shows welcome and username', async ({ authenticatedPage: page }) => {
    await expect(page.getByRole('heading', { name: 'ยินดีต้อนรับ, Welcome' })).toBeVisible();
    await expect(page.locator('strong')).toContainText('admin');
  });

  test('navigates to machine page via sidebar', async ({ authenticatedPage: page }) => {
    await page.getByRole('link', { name: 'Machine' }).click();

    await expect(page).toHaveURL(/\/machine/);
    await expect(page.getByRole('heading', { name: 'Machine Management' })).toBeVisible();
  });

  test('navigates back to home via sidebar', async ({ authenticatedPage: page }) => {
    await page.getByRole('link', { name: 'Machine' }).click();
    await expect(page).toHaveURL(/\/machine/);

    await page.getByRole('link', { name: 'หน้าหลัก' }).click();
    await expect(page).toHaveURL('/');
    await expect(page.getByRole('heading', { name: 'ยินดีต้อนรับ, Welcome' })).toBeVisible();
  });
});
