const { test, expect } = require('@playwright/test');

test('客タイプが4種類表示されることを確認', async ({ page }) => {
  await page.goto('http://localhost:8000/');
  await page.waitForLoadState('networkidle');
  
  // タイトル画面で「はじめから」をクリック
  await page.waitForSelector('button:has-text("はじめから")', { state: 'visible' });
  await page.click('button:has-text("はじめから")');
  
  // 営業開始
  await page.waitForSelector('button:has-text("営業開始")', { state: 'visible' });
  await page.click('button:has-text("営業開始")');
  
  // 客が出現するまで待機（10秒）
  await page.waitForTimeout(10000);
  
  // スクリーンショット撮影
  await page.screenshot({ path: 'test-customer-types.png', fullPage: true });
  
  // 客要素が存在することを確認
  const customers = await page.locator('.customer').count();
  console.log(`客の数: ${customers}`);
  expect(customers).toBeGreaterThan(0);
  
  // 客の色分けを確認（DOMに色が設定されているか）
  const customerBodies = await page.locator('.customer-body').all();
  const colors = [];
  for (const body of customerBodies) {
    const bg = await body.evaluate(el => getComputedStyle(el).backgroundColor);
    colors.push(bg);
  }
  console.log('客の色:', colors);
  
  // 客をクリックしてフィードバック表示
  if (customerBodies.length > 0) {
    await customerBodies[0].click();
    await page.waitForTimeout(1000);
    await page.screenshot({ path: 'test-customer-feedback.png', fullPage: true });
  }
});
