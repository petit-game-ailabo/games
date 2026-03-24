const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 460, height: 800 } });
  const page = await context.newPage();

  await page.goto('file://' + __dirname + '/index.html');
  await page.waitForTimeout(1000);

  console.log('✅ ページロード成功');

  // 営業タブに切り替え
  await page.click('[data-tab="business"]');
  await page.waitForTimeout(500);
  console.log('✅ 営業タブに切り替え');

  // 営業開始
  await page.click('#startBusinessBtn');
  console.log('✅ 営業開始');
  await page.waitForTimeout(3000);

  // 客が来るまで待つ
  const customer = await page.waitForSelector('.customer', { timeout: 10000 });
  console.log('✅ 客が来店');

  // 客の状態を確認（eating状態になるまで待つ）
  await page.waitForTimeout(2000);

  // 客をクリック
  await customer.click();
  console.log('✅ 客をタップ');
  await page.waitForTimeout(500);

  // フィードバックトーストが表示されているか確認
  const toast = await page.$('.feedback-toast');
  if (toast) {
    const toastText = await toast.textContent();
    console.log('✅ フィードバック表示:', toastText);
    
    // スクリーンショット撮影
    await page.screenshot({ path: 'test-tap-feedback-verify.png' });
    console.log('✅ スクリーンショット保存: test-tap-feedback-verify.png');
  } else {
    console.log('❌ フィードバックトーストが表示されない');
  }

  await page.waitForTimeout(4000);

  await browser.close();
  console.log('✅ テスト完了');
})();
