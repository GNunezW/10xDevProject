import { test, expect } from '@playwright/test';

/**
 * Seed exemplar for generated E2E tests.
 * Patterns: getByRole / getByLabel, wait for URL/state, isolated (no shared data).
 * Risk-shaped name so later specs copy that, not "test 1".
 */
test('anonymous visitor is redirected to login from coordinator home', async ({ page }) => {
  await page.goto('/');

  await page.waitForURL(/\/login/);
  await expect(page.getByRole('heading', { name: 'Logowanie koordynatora' })).toBeVisible();
  await expect(page.getByText('Witaj w systemie Plan Zajęć')).toHaveCount(0);
});
