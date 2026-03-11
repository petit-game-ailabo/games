import { chromium } from 'playwright';

const w = 375, h = 812;
const url = `file://${process.cwd()}/index.html?v=${Date.now()}`;

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({
  viewport: { width: w, height: h },
  hasTouch: true,
  isMobile: true
});

// イベントリスナーを追加
await context.addInitScript(() => {
  window.clickLog = [];
});

const page = await context.newPage();

console.log('📱 イベントテスト開始');
await page.goto(url);
await page.waitForTimeout(1000);

// Stateを確認
const initialState = await page.evaluate(() => {
  return {
    state: window.state,
    ST_TITLE: window.ST?.TITLE,
    ST_PLAY: window.ST?.PLAY,
    buttonsReady: window.buttonsReady,
    titlePlayBtn: window.titlePlayBtn,
    W: window.W,
    H: window.H
  };
});

console.log('\n初期状態:', JSON.stringify(initialState, null, 2));

// プレイボタンをクリック
const playBtnY = h * 0.52 + 25;
console.log('\nクリック実行: x=' + (w/2) + ', y=' + playBtnY);

// pointerdownイベントを直接発火
await page.evaluate((x, y) => {
  const canvas = document.getElementById('game');
  const rect = canvas.getBoundingClientRect();
  
  const evt = new PointerEvent('pointerdown', {
    clientX: x,
    clientY: y,
    bubbles: true,
    cancelable: true
  });
  
  console.log('Dispatching pointerdown at:', x, y);
  console.log('Canvas rect:', rect);
  console.log('State before:', window.state);
  
  canvas.dispatchEvent(evt);
  
  console.log('State after:', window.state);
  
  return {
    dispatchedAt: { x, y },
    rect: { left: rect.left, top: rect.top, width: rect.width, height: rect.height },
    stateBefore: window.state,
    stateAfter: window.state
  };
}, w/2, playBtnY);

await page.waitForTimeout(500);

// State変更を確認
const afterState = await page.evaluate(() => ({
  state: window.state,
  audioCtx: window.audioCtx !== null,
  player: window.player
}));

console.log('\nイベント後のstate:', JSON.stringify(afterState, null, 2));

await browser.close();
console.log('\n✅ テスト完了');
