const { test, expect } = require('@playwright/test');

test('ダシ素材の購入とスープ研究が機能する', async ({ page }) => {
  await page.goto('http://localhost:8080/index.html');
  
  // 配置タブに移動
  await page.click('button[data-tab="layout"]');
  await page.waitForTimeout(500);
  
  // 豚骨ダシを購入（初期所持金10000円、豚骨500円）
  const porkDashiBtn = page.locator('[data-item="dashi-pork"]');
  await expect(porkDashiBtn).toBeVisible();
  
  // 購入前は unlocked ではない（まだ購入していない）
  await expect(porkDashiBtn).not.toHaveClass(/unlocked/);
  
  // 購入ボタンをクリック
  await porkDashiBtn.click();
  
  // アラート処理
  page.on('dialog', async dialog => {
    const message = dialog.message();
    console.log('Alert:', message);
    await dialog.accept();
  });
  
  await page.waitForTimeout(500);
  
  // 購入後は unlocked になる
  await expect(porkDashiBtn).toHaveClass(/unlocked/);
  
  // 所持金が9500円に減っている
  const moneyDisplay = page.locator('#moneyDisplay');
  await expect(moneyDisplay).toHaveText('9500');
  
  // スープタブに移動
  await page.click('button[data-tab="soup"]');
  await page.waitForTimeout(500);
  
  // ダシセレクターに豚骨が表示される
  const porkDashiSelector = page.locator('[data-dashi="dashi-pork"]');
  await expect(porkDashiSelector).toBeVisible();
  await expect(porkDashiSelector).not.toHaveClass(/locked/);
  
  // 豚骨をクリックして選択
  await porkDashiSelector.click();
  await page.waitForTimeout(300);
  
  // 選択状態になる
  await expect(porkDashiSelector).toHaveClass(/selected/);
  
  // スープステータスが更新される（豚骨: コク+30, キレ+5, 香り+5, 濃さ+30）
  const kokBar = page.locator('#kokBar');
  const kokText = await kokBar.textContent();
  expect(parseInt(kokText)).toBeGreaterThan(0);
  
  // スクリーンショット
  await page.screenshot({ path: 'test-soup-system.png', fullPage: true });
});
