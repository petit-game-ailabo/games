// Sprint 2: ダシ素材購入→スープ研究のテスト

const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  
  const htmlPath = 'file://' + path.resolve(__dirname, 'index.html');
  await page.goto(htmlPath);
  
  console.log('1. 初期状態');
  await page.screenshot({ path: 'test-sprint2-1-initial.png' });
  await page.waitForTimeout(1000);
  
  console.log('2. 配置タブ - ダシ素材セクション確認');
  await page.click('button[data-tab="layout"]');
  await page.waitForTimeout(500);
  
  // スクロールしてダシ素材を表示
  await page.evaluate(() => {
    document.querySelector('#layoutTab').scrollTop = 9999;
  });
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-sprint2-2-dashi-section.png' });
  
  console.log('3. 豚骨を購入（500円）');
  await page.click('[data-item="dashi-pork"]');
  await page.waitForTimeout(1000);
  await page.screenshot({ path: 'test-sprint2-3-bought-pork.png' });
  
  console.log('4. スープタブに移動');
  await page.click('button[data-tab="soup"]');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-sprint2-4-soup-tab.png' });
  
  console.log('5. 鶏ガラを選択');
  await page.click('[data-dashi="dashi-chicken"]');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-sprint2-5-chicken-selected.png' });
  
  console.log('6. 野菜を追加');
  await page.click('[data-dashi="dashi-vegetable"]');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-sprint2-6-vegetable-added.png' });
  
  console.log('7. 豚骨を追加');
  await page.click('[data-dashi="dashi-pork"]');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-sprint2-7-pork-added.png' });
  
  console.log('8. スープステータス最終確認');
  await page.waitForTimeout(1000);
  await page.screenshot({ path: 'test-sprint2-8-final-stats.png' });
  
  console.log('完了！');
  await page.waitForTimeout(2000);
  await browser.close();
})();
