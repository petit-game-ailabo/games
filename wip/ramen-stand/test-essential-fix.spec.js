const { test, expect } = require('@playwright/test');

test('essential設備が配置タブで購入可能', async ({ page }) => {
  await page.goto('http://localhost:8080/index.html');
  
  // タイトル画面の「はじめから」をクリック
  await page.click('text=はじめから');
  await page.waitForTimeout(500);
  
  // 配置タブに移動
  await page.click('button[data-tab="layout"]');
  await page.waitForTimeout(300);
  
  // 厨房サブタブをクリック
  await page.click('text=厨房');
  await page.waitForTimeout(300);
  
  // スープ鍋が表示されていて、disabled クラスがないことを確認
  const soupPotItem = page.locator('.furniture-item', { hasText: 'スープ鍋' });
  await expect(soupPotItem).toBeVisible();
  await expect(soupPotItem).not.toHaveClass(/disabled/);
  
  // ゆで麺機も購入可能
  const noodleBoiler = page.locator('.furniture-item', { hasText: 'ゆで麺機' });
  await expect(noodleBoiler).toBeVisible();
  await expect(noodleBoiler).not.toHaveClass(/disabled/);
  
  // 設置数が表示されている（初期配置で1つずつあるはず）
  const soupPotText = await soupPotItem.textContent();
  expect(soupPotText).toContain('設置数: 1');
  
  // スープ鍋をクリックして選択できることを確認
  await soupPotItem.click();
  await page.waitForTimeout(200);
  await expect(soupPotItem).toHaveClass(/selected/);
});
