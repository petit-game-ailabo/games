// Sprint 2: タレ/麺/トッピング選択のテスト

const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  
  const htmlPath = 'file://' + path.resolve(__dirname, 'index.html');
  await page.goto(htmlPath);
  
  console.log('1. 初期状態（スープタブ）');
  await page.click('button[data-tab="soup"]');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-tare-1-initial.png' });
  
  console.log('2. タレセレクター確認');
  await page.evaluate(() => {
    document.querySelector('#soupTab').scrollTop = 9999;
  });
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-tare-2-selector.png' });
  
  console.log('3. 配置タブ - タレ購入');
  await page.click('button[data-tab="layout"]');
  await page.waitForTimeout(500);
  await page.evaluate(() => {
    document.querySelector('#layoutTab').scrollTop = 9999;
  });
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-tare-3-purchase-panel.png' });
  
  console.log('4. 味噌タレを購入（500円）');
  await page.click('[data-item="tare-miso"]');
  await page.waitForTimeout(1000);
  await page.screenshot({ path: 'test-tare-4-bought-miso.png' });
  
  console.log('5. 塩タレを購入（300円）');
  await page.click('[data-item="tare-shio"]');
  await page.waitForTimeout(1000);
  await page.screenshot({ path: 'test-tare-5-bought-shio.png' });
  
  console.log('6. チャーシューを購入（500円）');
  await page.click('[data-item="topping-chashu"]');
  await page.waitForTimeout(1000);
  await page.screenshot({ path: 'test-tare-6-bought-chashu.png' });
  
  console.log('7. 太麺ちぢれを購入（800円）');
  await page.click('[data-item="men-thick-wavy"]');
  await page.waitForTimeout(1000);
  await page.screenshot({ path: 'test-tare-7-bought-thick.png' });
  
  console.log('8. スープタブに戻る');
  await page.click('button[data-tab="soup"]');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-tare-8-soup-tab.png' });
  
  console.log('9. 味噌タレを選択');
  await page.evaluate(() => {
    document.querySelector('#soupTab').scrollTop = 9999;
  });
  await page.waitForTimeout(500);
  await page.click('[data-tare="tare-miso"]');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-tare-9-selected-miso.png' });
  
  console.log('10. 太麺ちぢれを選択');
  await page.click('[data-men="men-thick-wavy"]');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-tare-10-selected-thick.png' });
  
  console.log('11. トッピング追加（ネギ）');
  await page.click('[data-topping="topping-negi"]');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-tare-11-added-negi.png' });
  
  console.log('12. トッピング追加（チャーシュー）');
  await page.click('[data-topping="topping-chashu"]');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-tare-12-added-chashu.png' });
  
  console.log('13. ダシ選択（コク強化）');
  await page.evaluate(() => {
    document.querySelector('#soupTab').scrollTop = 0;
  });
  await page.waitForTimeout(500);
  await page.click('[data-dashi="dashi-chicken"]');
  await page.waitForTimeout(500);
  await page.click('[data-dashi="dashi-pork"]');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-tare-13-dashi-selected.png' });
  
  console.log('14. 最終状態確認');
  await page.waitForTimeout(1000);
  await page.screenshot({ path: 'test-tare-14-final.png' });
  
  console.log('完了！');
  await page.waitForTimeout(2000);
  await browser.close();
})();
