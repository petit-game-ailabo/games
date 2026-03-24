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
  
  // ステータスバーをスクショ
  console.log('1. ステータスバー確認（フェーズ1: 屋台, 席数0/4）');
  await page.screenshot({ path: 'test-grid-1-status.png', fullPage: true });
  
  // 配置タブを開く
  await page.click('.tab[data-tab="layout"]');
  await page.waitForTimeout(300);
  
  console.log('2. 配置タブ（席数制限の表示）');
  await page.screenshot({ path: 'test-grid-2-layout.png', fullPage: true });
  
  // カウンター席を選択
  await page.click('.furniture-item:has-text("カウンター席")');
  await page.waitForTimeout(200);
  
  // グリッドの中央あたりに1つ目の席を配置 (x=2, y=2)
  const grid = await page.$('#grid-overlay');
  const gridBox = await grid.boundingBox();
  const TILE_SIZE = 40;
  await page.click('#grid-overlay', {
    position: { x: 2 * TILE_SIZE + TILE_SIZE/2, y: 2 * TILE_SIZE + TILE_SIZE/2 }
  });
  await page.waitForTimeout(300);
  
  console.log('3. カウンター席を1つ配置（1/4席）');
  await page.screenshot({ path: 'test-grid-3-first-seat.png', fullPage: true });
  
  // 2つ目、3つ目、4つ目の席を配置
  await page.click('#grid-overlay', {
    position: { x: 3 * TILE_SIZE + TILE_SIZE/2, y: 2 * TILE_SIZE + TILE_SIZE/2 }
  });
  await page.waitForTimeout(200);
  
  await page.click('#grid-overlay', {
    position: { x: 4 * TILE_SIZE + TILE_SIZE/2, y: 2 * TILE_SIZE + TILE_SIZE/2 }
  });
  await page.waitForTimeout(200);
  
  await page.click('#grid-overlay', {
    position: { x: 5 * TILE_SIZE + TILE_SIZE/2, y: 2 * TILE_SIZE + TILE_SIZE/2 }
  });
  await page.waitForTimeout(300);
  
  console.log('4. カウンター席を4つ配置（4/4席: 上限到達）');
  await page.screenshot({ path: 'test-grid-4-full-seats.png', fullPage: true });
  
  // 5つ目を配置しようとする（エラー通知が出るはず）
  await page.click('#grid-overlay', {
    position: { x: 6 * TILE_SIZE + TILE_SIZE/2, y: 2 * TILE_SIZE + TILE_SIZE/2 }
  });
  await page.waitForTimeout(500);
  
  console.log('5. 5つ目の席を配置しようとする（エラー通知）');
  await page.screenshot({ path: 'test-grid-5-limit-error.png', fullPage: true });
  
  // 営業タブに戻って営業開始
  await page.click('.tab[data-tab="operations"]');
  await page.waitForTimeout(200);
  await page.click('button:has-text("営業開始")');
  await page.waitForTimeout(3000); // 3秒営業
  
  console.log('6. 営業中（客が来ているか確認）');
  await page.screenshot({ path: 'test-grid-6-business.png', fullPage: true });
  
  console.log('✓ テスト完了');
  
  await browser.close();
})();
