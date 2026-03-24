const { test, expect } = require('@playwright/test');

test('客をタップすると感想が表示される', async ({ page }) => {
  await page.goto('http://localhost:8080/index.html');
  
  // タイトル画面を閉じる
  await page.click('button:has-text("はじめから")');
  await page.waitForTimeout(1000);
  
  // 営業タブで営業開始
  await page.click('button:has-text("営業開始")');
  await page.waitForTimeout(1000);
  
  // 客が来るまで少し待つ
  await page.waitForTimeout(3000);
  
  // 客要素が表示されているか確認
  const customer = page.locator('.customer').first();
  await expect(customer).toBeVisible({ timeout: 5000 });
  
  // スクリーンショット1: 客が表示されている状態
  await page.screenshot({ path: 'test-feedback-1-customer.png', fullPage: true });
  
  // 客をクリック（force: trueで強制クリック）
  await customer.click({ force: true });
  await page.waitForTimeout(500);
  
  // 吹き出しが表示されているか確認
  const bubble = page.locator('.customer-bubble');
  await expect(bubble.first()).toBeVisible();
  
  // スクリーンショット2: 感想が表示されている状態
  await page.screenshot({ path: 'test-feedback-2-bubble.png', fullPage: true });
  
  // もう少し待って席に着いた客をタップ
  await page.waitForTimeout(5000);
  
  // 座っている客がいるか確認（SITTING状態の客）
  const customers = await page.locator('.customer').all();
  if (customers.length > 0) {
    // 最初の客を強制クリック
    await customers[0].click({ force: true });
    await page.waitForTimeout(500);
    
    // スクリーンショット3: 食事中の感想
    await page.screenshot({ path: 'test-feedback-3-eating.png', fullPage: true });
  }
});
