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
  
  // 倍速3倍に変更
  await page.click('[data-speed="3"]');
  console.log('✅ 3倍速に変更');
  
  // 15秒待機（複数の客を受け入れる）
  await page.waitForTimeout(15000);
  
  // 早期終了
  await page.click('#closeBusinessBtn');
  console.log('✅ 営業終了');
  await page.waitForTimeout(1000);
  
  // 日次レポートを閉じる
  await page.click('#continueBtn');
  await page.waitForTimeout(500);
  console.log('✅ 2日目へ');
  
  // 2日目の営業開始
  await page.click('#startBusinessBtn');
  console.log('✅ 2日目営業開始');
  
  // 客が来るまで待つ
  await page.waitForTimeout(5000);
  
  // リピーターバッジを探す
  const repeaterBadge = await page.$('.repeater-badge');
  if (repeaterBadge) {
    console.log('✅ リピーター顧客が来店（⭐バッジ確認）');
  } else {
    console.log('⚠️  リピーター顧客がまだ来店していない（ランダムなので正常）');
  }
  
  // 客をクリックしてフィードバック確認
  const customer = await page.$('.customer');
  if (customer) {
    await customer.click();
    await page.waitForTimeout(500);
    
    const toast = await page.$('.feedback-toast');
    if (toast) {
      const toastText = await toast.textContent();
      console.log('✅ フィードバック表示:', toastText.replace(/\s+/g, ' ').trim());
      
      // スクリーンショット撮影
      await page.screenshot({ path: 'test-repeater-system.png' });
      console.log('✅ スクリーンショット保存: test-repeater-system.png');
    }
  }

  await page.waitForTimeout(2000);

  await browser.close();
  console.log('✅ テスト完了');
})();
