import { test as base, expect, type Page } from '@playwright/test';

export type AuthRole = 'admin' | 'user';

const CREDENTIALS: Record<AuthRole, { username: string; password: string }> = {
  admin: { username: 'admin', password: 'Admin@1234' },
  user: { username: 'user', password: 'User@1234' },
};

export async function loginAs(page: Page, role: AuthRole = 'admin'): Promise<void> {
  const { username, password } = CREDENTIALS[role];

  await page.goto('/login');
  await page.locator('#username').fill(username);
  await page.locator('#password').fill(password);
  await page.getByRole('button', { name: 'เข้าสู่ระบบ' }).click();
  await expect(page).toHaveURL('/');
}

type AuthFixtures = {
  authenticatedPage: Page;
  role: AuthRole;
};

export const test = base.extend<AuthFixtures>({
  role: ['admin', { option: true }],

  authenticatedPage: async ({ page, role }, use) => {
    await loginAs(page, role);
    await use(page);
  },
});

export { expect } from '@playwright/test';
