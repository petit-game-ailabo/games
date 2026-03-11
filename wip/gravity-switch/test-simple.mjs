import { chromium } from 'playwright';

const w = 375, h = 812;
const url = `file://${process.cwd()}/index.html?v=${Date.now()}`;

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({
  viewport: { width: w, height: h },
  hasTouch: true,
  isMobile: true
});

const page = await context.newPage();

console.log('📱 簡易テスト開始');
await page.goto(url, { waitUntil: 'networkidle' });
await page.waitForTimeout(2000); // 十分に待つ

// グローバル変数の確認
const globals = await page.evaluate(() => {
  const keys = ['state', 'ST', 'W', 'H', 'titlePlayBtn', 'buttonsReady', 'canvas'];
  const result = {};
  for(const k of keys) {
    result[k] = typeof window[k] !== 'undefined' ? window[k] : 'undefined';
  }
  return result;
});

console.log('\nグローバル変数:', JSON.stringify(globals, null, 2));

// Canvas要素の存在確認
const canvasExists = await page.evaluate(() => {
  const canvas = document.getElementById('game');
  return canvas !== null;
});

console.log('Canvas存在:', canvasExists);

await browser.close();
console.log('\n✅ テスト完了');
