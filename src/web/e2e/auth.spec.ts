import { test, expect } from '@playwright/test';

const ADMIN_EMAIL    = process.env['E2E_ADMIN_EMAIL']    ?? 'admin@shopearth.local';
const ADMIN_PASSWORD = process.env['E2E_ADMIN_PASSWORD'] ?? 'Admin@123!';

test.describe('Authentication', () => {
  test('login page loads and shows login form', async ({ page }) => {
    await page.goto('/login');
    await expect(page.locator('input[type="password"]')).toBeVisible();
  });

  test('login with valid credentials redirects to dashboard', async ({ page }) => {
    await page.goto('/login');

    await page.locator('input[type="text"]').first().fill(ADMIN_EMAIL);
    await page.locator('input[type="password"]').fill(ADMIN_PASSWORD);
    await page.locator('button[type="submit"]').click();

    await expect(page).toHaveURL(/dashboard/, { timeout: 15_000 });
  });

  test('login with wrong password shows error message', async ({ page }) => {
    await page.goto('/login');

    await page.locator('input[type="text"]').first().fill(ADMIN_EMAIL);
    await page.locator('input[type="password"]').fill('wrong-password-xyz!');
    await page.locator('button[type="submit"]').click();

    // Should stay on login page and show an error
    await expect(page).not.toHaveURL(/dashboard/, { timeout: 8_000 });
    await expect(page.locator('.text-red-400').first()).toBeVisible({ timeout: 8_000 });
  });

  test('accessing protected route while logged out redirects to login', async ({ page }) => {
    // Fresh context already has no session — navigate directly to a protected route
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/login/, { timeout: 8_000 });
  });
});
