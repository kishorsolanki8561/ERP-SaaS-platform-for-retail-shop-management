import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 1 : 0,
  workers: 1,
  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
  ],
  use: {
    baseURL: process.env['APP_BASE_URL'] ?? 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  // In CI, the Angular dev server is expected to be started by the workflow.
  // Locally, uncomment webServer to have Playwright auto-start it.
  // webServer: {
  //   command: 'npm run start',
  //   url: 'http://localhost:4200',
  //   reuseExistingServer: !process.env['CI'],
  // },
});
