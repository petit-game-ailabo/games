const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  // コンソールエラーをキャプチャ
  const errors = [];
  page.on('console', msg => {
    console.log(`${msg.type()}: ${msg.text()}`);
    if (msg.type() === 'error') {
      errors.push(msg.text());
      console.log('❌ JS Error:', msg.text());
    }
  });
  
  page.on('pageerror', err => {
    errors.push(err.message);
    console.log('❌ Page Error:', err.message);
  });
  
  // ゲームを開く
  const filePath = 'file://' + path.resolve(__dirname, 'index.html');
  console.log('🎮 Opening:', filePath);
  
  await page.goto(filePath, { waitUntil: 'networkidle' });
  
  // 少し待ってゲームがロードされるのを確認
  await page.waitForTimeout(5000);
  
  // スクリーンショット
  await page.screenshot({ path: 'screenshot.png', fullPage: true });
  console.log('📸 Screenshot saved to screenshot.png');
  
  // 結果
  if (errors.length === 0) {
    console.log('✅ No JavaScript errors detected!');
  } else {
    console.log(`⚠️  ${errors.length} error(s) found`);
  }
  
  await browser.close();
})();
