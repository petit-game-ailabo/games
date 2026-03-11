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

// JSエラーをキャッチ
page.on('console', msg => {
  if (msg.type() === 'error') console.error('❌ Console Error:', msg.text());
});
page.on('pageerror', err => console.error('❌ Page Error:', err.message));

console.log('📱 テストプレイ開始:', url);
await page.goto(url);
await page.waitForTimeout(500);

// スクリーンショット（タイトル画面）
await page.screenshot({ path: 'test-title.png' });
console.log('📸 タイトル画面キャプチャ');

// プレイボタンをタップ
const playBtnY = h * 0.52 + 25; // ボタン中央
await page.mouse.click(w/2, playBtnY);
await page.waitForTimeout(300);

// ゲーム中にタップして重力反転（複数回）
for(let i = 0; i < 8; i++) {
  await page.mouse.click(w/2, h/2);
  await page.waitForTimeout(400);
}

await page.screenshot({ path: 'test-play.png' });
console.log('📸 プレイ中キャプチャ');

// 死ぬまで待つ（最大10秒）
await page.waitForTimeout(3000);
await page.screenshot({ path: 'test-dead.png' });
console.log('📸 ゲームオーバー画面キャプチャ');

// ショップに移動
const shopBtnY = h * 0.58 + 22;
await page.mouse.click(w/2, shopBtnY);
await page.waitForTimeout(500);

await page.screenshot({ path: 'test-shop.png' });
console.log('📸 ショップ画面キャプチャ');

// タイトルに戻る
const backBtnY = h * 0.2 + 7 * (68 + 12) + 16 + 23;
await page.mouse.click(w/2, backBtnY);
await page.waitForTimeout(300);

// リセットボタンの位置確認（右上）
const resetX = w - 70 + 30; // ボタン中央
const resetY = 10 + 17.5;
await page.mouse.click(resetX, resetY);
await page.waitForTimeout(300);

await page.screenshot({ path: 'test-confirm.png' });
console.log('📸 リセット確認画面キャプチャ');

await browser.close();
console.log('✅ テスト完了');
