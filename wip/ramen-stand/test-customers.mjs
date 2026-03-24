import { chromium } from 'playwright';
import { fileURLToPath } from 'url';
import { dirname, resolve } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  const htmlPath = resolve(__dirname, 'index.html');
  await page.goto(`file://${htmlPath}`);
  
  // タイトル画面で「はじめから」をクリック
  await page.click('button:has-text("はじめから")');
  await page.waitForTimeout(500);
  
  // 配置タブで席を4つ配置
  await page.click('.tab[data-tab="layout"]');
  await page.waitForTimeout(200);
  
  await page.click('.furniture-item:has-text("カウンター席")');
  await page.waitForTimeout(100);
  
  const TILE_SIZE = 40;
  for (let i = 0; i < 4; i++) {
    await page.click('#grid-overlay', {
      position: { x: (2 + i) * TILE_SIZE + TILE_SIZE/2, y: 2 * TILE_SIZE + TILE_SIZE/2 }
    });
    await page.waitForTimeout(100);
  }
  
  // 営業タブに戻って営業開始
  await page.click('.tab[data-tab="operations"]');
  await page.waitForTimeout(200);
  
  console.log('営業開始...');
  await page.click('button:has-text("営業開始")');
  
  // 5秒ごとにスクショを撮る（合計20秒）
  for (let i = 1; i <= 4; i++) {
    await page.waitForTimeout(5000);
    await page.screenshot({ path: `test-customers-${i * 5}s.png`, fullPage: true });
    console.log(`${i * 5}秒経過: test-customers-${i * 5}s.png`);
    
    // 客の要素数をカウント
    const customerCount = await page.$$eval('.customer', els => els.length);
    console.log(`  → 客の数: ${customerCount}`);
  }
  
  console.log('✓ テスト完了');
  
  await browser.close();
})();
