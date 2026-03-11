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

console.log('📱 ボタンテスト開始:', url);
await page.goto(url);
await page.waitForTimeout(1000);

// Canvas要素の情報を取得
const canvasInfo = await page.evaluate(() => {
  const canvas = document.getElementById('game');
  const rect = canvas.getBoundingClientRect();
  return {
    canvasWidth: canvas.width,
    canvasHeight: canvas.height,
    rectWidth: rect.width,
    rectHeight: rect.height,
    rectLeft: rect.left,
    rectTop: rect.top,
    W: window.innerWidth,
    H: window.innerHeight
  };
});

console.log('Canvas情報:', JSON.stringify(canvasInfo, null, 2));

// タイトル画面でプレイボタンの座標を計算
const bw = Math.min(220, w - 60);
const playBtnX = w/2 - bw/2;
const playBtnY = h * 0.52;
const playBtnCenterY = playBtnY + 25; // 高さ50の中央

console.log('\n📌 プレイボタンテスト');
console.log('  計算座標: x=' + (w/2) + ', y=' + playBtnCenterY);

await page.mouse.click(w/2, playBtnCenterY);
await page.waitForTimeout(500);

const isPlaying = await page.evaluate(() => {
  return window.state === 1; // ST.PLAY
});

console.log('  結果:', isPlaying ? '✅ 正常に動作' : '❌ 反応なし');

if(!isPlaying) {
  // 10px上をタップ
  console.log('  再試行: 10px上');
  await page.mouse.click(w/2, playBtnCenterY - 10);
  await page.waitForTimeout(500);
  const retry1 = await page.evaluate(() => window.state === 1);
  console.log('  結果:', retry1 ? '✅ 反応あり' : '❌ 反応なし');
  
  if(!retry1) {
    // 10px下をタップ
    console.log('  再試行: 10px下');
    await page.mouse.click(w/2, playBtnCenterY + 10);
    await page.waitForTimeout(500);
    const retry2 = await page.evaluate(() => window.state === 1);
    console.log('  結果:', retry2 ? '✅ 反応あり' : '❌ 反応なし');
  }
}

await browser.close();
console.log('\n✅ テスト完了');
