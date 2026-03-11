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

console.log('📱 機能テスト開始');
await page.goto(url);
await page.waitForTimeout(1000);

// プレイ開始
console.log('\n📌 ゲーム開始');
await page.mouse.click(w/2, h * 0.52 + 25);
await page.waitForTimeout(500);

// 何回かプレイ
for(let i = 0; i < 10; i++) {
  await page.mouse.click(w/2, h/2);
  await page.waitForTimeout(200);
}

await page.waitForTimeout(2000);

// 死んだ後の状態を確認
const afterState = await page.evaluate(() => {
  return {
    state: window.state,
    distance: Math.floor(window.distance || 0),
    sessionCoins: window.sessionCoins || 0,
    deathReason: window.deathReason || '不明'
  };
});

console.log('\n結果:');
console.log('  State:', afterState.state === 2 ? 'DEAD ✅' : (afterState.state === 1 ? 'PLAYING' : 'OTHER'));
console.log('  距離:', afterState.distance + 'm');
console.log('  コイン:', afterState.sessionCoins);
console.log('  死因:', afterState.deathReason);

await browser.close();
console.log('\n✅ テスト完了');
