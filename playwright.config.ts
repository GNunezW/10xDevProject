import { defineConfig, devices } from '@playwright/test';

/**
 * Blazor Server at http://localhost:5223. Start the app yourself
 * (`dotnet run --launch-profile http --project plan-zajec-uczelnia/plan-zajec-uczelnia.csproj`)
 * — webServer is not used because the API needs user-secrets and Postgres.
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: 'list',
  use: {
    baseURL: process.env.BASE_URL ?? 'http://localhost:5223',
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'setup',
      testMatch: /auth\.setup\.ts/,
    },
    {
      name: 'anonymous',
      use: {
        ...devices['Desktop Chrome'],
        storageState: { cookies: [], origins: [] },
      },
      testMatch: /seed\.spec\.ts|anonymous-.*\.spec\.ts/,
    },
    {
      name: 'authenticated',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'playwright/.auth/user.json',
      },
      dependencies: ['setup'],
      testIgnore: /seed\.spec\.ts|anonymous-.*\.spec\.ts|auth\.setup\.ts/,
    },
  ],
});
