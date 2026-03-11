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
let consoleLogs = [];
page.on('console', msg => {
  consoleLogs.push({ type: msg.type(), text: msg.text() });
});

console.log('📱 実際のゲームテスト開始');
await page.goto(url);
await page.waitForTimeout(1500);

console.log('\nConsoleログ:');
consoleLogs.forEach(log => console.log(`  [${log.type}]`, log.text));

// タイトル画面のスクリーンショットを撮る
await page.screenshot({ path: 'test-state1.png' });
console.log('\n📸 タイトル画面キャプチャ');

// プレイボタンをタップ（複数回、位置を変えて）
console.log('\n📌 プレイボタンタップテスト');

const playY = h * 0.52;
const tests = [
  { name: 'ボタン上端', y: playY + 5 },
  { name: 'ボタン中央', y: playY + 25 },
  { name: 'ボタン下端', y: playY + 45 },
];

for(const test of tests) {
  consoleLogs = [];
  console.log(`\n  ${test.name}: y=${test.y}`);
  await page.mouse.click(w/2, test.y);
  await page.waitForTimeout(300);
  
  // スクリーンショットで確認
  const screenshotPath = `test-click-${test.name}.png`;
  await page.screenshot({ path: screenshotPath });
  console.log(`    📸 ${screenshotPath}`);
  
  if(consoleLogs.length > 0) {
    console.log('    Console:', consoleLogs.map(l => l.text).join(', '));
  }
}

await browser.close();
console.log('\n✅ テスト完了');
