import { test as setup, expect } from '@playwright/test';
import path from 'node:path';

const authFile = path.join('playwright', '.auth', 'user.json');

setup('authenticate coordinator', async ({ page }) => {
  const email = process.env.E2E_COORDINATOR_EMAIL ?? 'koordynator@uczelnia.pl';
  const password = process.env.E2E_COORDINATOR_PASSWORD ?? 'TwojeHaslo123';

  await page.goto('/login');
  await page.getByLabel('E-mail').fill(email);
  await page.getByLabel('Hasło').fill(password);
  await page.getByRole('button', { name: 'Zaloguj' }).click();
  await page.waitForURL((url) => !url.pathname.startsWith('/login'));
  await expect(page.getByText('Witaj w systemie Plan Zajęć')).toBeVisible();
  await page.context().storageState({ path: authFile });
});
