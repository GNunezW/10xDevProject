import { test, expect } from '@playwright/test';

/**
 * Risk: test-plan.md #5 — unauthenticated caller starts generate (~60s)
 * or reads/exports schedule data.
 * Seed: e2e/seed.spec.ts
 * Real: cookie/middleware redirect, JWT on /api/scheduling/generate.
 * Mocked: none.
 */
test('anonymous visitor cannot open coordinator UI or start schedule generation', async ({
  page,
}) => {
  await page.goto('/');
  await page.waitForURL(/\/login/);
  await expect(page.getByRole('heading', { name: 'Logowanie koordynatora' })).toBeVisible();
  await expect(page.getByText('Witaj w systemie Plan Zajęć')).toHaveCount(0);

  await page.goto('/generuj-plan');
  await page.waitForURL(/\/login/);
  await expect(page.getByRole('heading', { name: 'Logowanie koordynatora' })).toBeVisible();

  const generate = await page.request.post('/api/scheduling/generate');
  expect(generate.status()).toBe(401);
});
