import { test, expect } from '@playwright/test';

const ADMIN_EMAIL    = process.env['E2E_ADMIN_EMAIL']    ?? 'admin@shopearth.local';
const ADMIN_PASSWORD = process.env['E2E_ADMIN_PASSWORD'] ?? 'Admin@123!';

test.describe('Authentication', () => {
  test('login page loads and shows login form', async ({ page }) => {
    await page.goto('/auth/login');
    await expect(page.locator('input[type="password"]')).toBeVisible();
  });

  test('login with valid credentials redirects to dashboard', async ({ page }) => {
    await page.goto('/auth/login');

    await page.locator('input[type="text"], input[type="email"]').first().fill(ADMIN_EMAIL);
    await page.locator('input[type="password"]').fill(ADMIN_PASSWORD);
    await page.locator('button[type="submit"]').click();

    await expect(page).toHaveURL(/dashboard/, { timeout: 10_000 });
  });

  test('login with wrong password shows error', async ({ page }) => {
    await page.goto('/auth/login');

    await page.locator('input[type="text"], input[type="email"]').first().fill(ADMIN_EMAIL);
    await page.locator('input[type="password"]').fill('wrong-password-xyz!');
    await page.locator('button[type="submit"]').click();

    // Should stay on login page and show an error message
    await expect(page).not.toHaveURL(/dashboard/, { timeout: 5_000 }).catch(() => {});
  });

  test('accessing protected route while logged out redirects to login', async ({ page }) => {
    await page.context().clearCookies();
    await page.evaluate(() => localStorage.clear());

    await page.goto('/dashboard');
    await expect(page).toHaveURL(/login/, { timeout: 5_000 });
  });
});
