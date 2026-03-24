const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  await page.goto('file://' + __dirname + '/index.html');
  
  // 営業タブに移動
  await page.click('text=営業');
  await page.waitForTimeout(500);
  
  // 営業開始
  await page.click('#startBusinessBtn');
  
  // 複数の客が来るまで待つ（最大20秒）
  for (let i = 0; i < 20; i++) {
    await page.waitForTimeout(1000);
    const customers = await page.$$('.customer');
    console.log(`${i+1}秒: ${customers.length}人`);
    if (customers.length >= 2) {
      console.log(`複数の客が来ました！`);
      break;
    }
  }
  
  await page.waitForTimeout(1000);
  
  // スクリーンショット
  await page.screenshot({ path: 'customer-types-test.png', fullPage: true });
  
  await browser.close();
  console.log('スクリーンショット保存: customer-types-test.png');
})();
