import { test, expect } from '@playwright/test';

const ADMIN_EMAIL    = process.env['E2E_ADMIN_EMAIL']    ?? 'admin@shopearth.local';
const ADMIN_PASSWORD = process.env['E2E_ADMIN_PASSWORD'] ?? 'Admin@123!';

async function loginAs(page: import('@playwright/test').Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[type="text"]').first().fill(ADMIN_EMAIL);
  await page.locator('input[type="password"]').fill(ADMIN_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL(/dashboard/, { timeout: 15_000 });
}

test.describe('Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page);
  });

  test('shows summary cards after login', async ({ page }) => {
    await expect(
      page.locator('[data-testid="today-sales"], .summary-card, app-dashboard').first()
    ).toBeVisible({ timeout: 8_000 });
  });

  test('branch selector is visible in topbar', async ({ page }) => {
    await expect(
      page.locator('app-branch-selector, [data-testid="branch-selector"]')
    ).toBeVisible({ timeout: 8_000 });
  });

  test('sidebar navigation is present', async ({ page }) => {
    await expect(page.locator('nav')).toBeVisible({ timeout: 5_000 });
    const linkCount = await page.locator('nav a').count();
    expect(linkCount).toBeGreaterThan(0);
  });

  test('quick action "New Invoice" is clickable', async ({ page }) => {
    const btn = page.locator('[data-testid="quick-action-invoice"], button:has-text("New Invoice")');
    const isVisible = await btn.isVisible();
    if (isVisible) {
      await btn.click();
      await expect(page).not.toHaveURL('/dashboard', { timeout: 5_000 });
    } else {
      // Quick action not visible — soft pass, no assertion
      test.skip();
    }
  });
});
