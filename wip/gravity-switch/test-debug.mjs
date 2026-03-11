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

// Console出力をキャッチ
page.on('console', msg => {
  console.log(`  ${msg.text()}`);
});

console.log('📱 デバッグテスト開始');
await page.goto(url);
await page.waitForTimeout(1500);

console.log('\n📌 ボタン上端をタップ:');
await page.mouse.click(w/2, h * 0.52 + 5);
await page.waitForTimeout(500);

await browser.close();
console.log('\n✅ テスト完了');
