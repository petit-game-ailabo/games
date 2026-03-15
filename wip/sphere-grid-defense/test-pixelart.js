const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  const htmlPath = 'file://' + path.resolve(__dirname, 'index.html');
  await page.goto(htmlPath);
  await page.setViewportSize({ width: 800, height: 600 });
  
  console.log('⏳ Waiting for page load...');
  await page.waitForTimeout(1000);
  
  // タイトル画面キャプチャ
  await page.screenshot({ path: 'test-screenshot-title.png' });
  console.log('📸 Title screen captured');
  
  // ゲーム開始
  await page.click('canvas');
  await page.waitForTimeout(500);
  
  // キャスティング完了待ち
  console.log('⏳ Waiting for casting to complete...');
  await page.waitForTimeout(3000);
  
  // ゲームプレイ画面キャプチャ
  await page.screenshot({ path: 'test-screenshot-gameplay.png' });
  console.log('📸 Gameplay screen captured');
  
  // JSエラーチェック
  const errors = [];
  page.on('console', msg => {
    if (msg.type() === 'error') {
      errors.push(msg.text());
    }
  });
  
  await page.waitForTimeout(2000);
  
  if (errors.length > 0) {
    console.error('❌ JavaScript errors detected:');
    errors.forEach(err => console.error('  ', err));
  } else {
    console.log('✅ No JavaScript errors detected');
  }
  
  await browser.close();
  console.log('✅ Test completed');
})();
