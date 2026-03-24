// test-recipe-system.js - レシピ保存・読み込み・メニュー設定のテスト
import { chromium } from 'playwright';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

async function test() {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  await page.setViewportSize({ width: 414, height: 896 });
  
  const htmlPath = 'file://' + path.join(__dirname, 'index.html');
  await page.goto(htmlPath);
  await page.waitForTimeout(1000);
  
  console.log('1. 初期状態');
  await page.screenshot({ path: 'test-recipe-1-initial.png' });
  
  // スープタブに移動
  await page.click('text=スープ');
  await page.waitForTimeout(500);
  console.log('2. スープタブ');
  await page.screenshot({ path: 'test-recipe-2-soup-tab.png' });
  
  // ダシを選択
  const dashiButtons = await page.$$('.dashi-btn');
  console.log(`ダシボタン数: ${dashiButtons.length}`);
  
  if (dashiButtons.length > 0) {
    await page.click('.dashi-btn:nth-child(1)'); // 鶏ガラ
    await page.waitForTimeout(300);
  }
  if (dashiButtons.length > 1) {
    await page.click('.dashi-btn:nth-child(2)'); // 野菜
    await page.waitForTimeout(300);
  }
  
  // タレを選択（醤油は初期選択済み）
  console.log('3. ダシ選択後');
  await page.screenshot({ path: 'test-recipe-3-dashi-selected.png' });
  
  // トッピングを選択
  const toppingButtons = await page.$$('.topping-btn:not(.locked)');
  console.log(`トッピングボタン数: ${toppingButtons.length}`);
  
  if (toppingButtons.length > 0) {
    await page.click('.topping-btn:not(.locked):nth-child(1)'); // ネギ
    await page.waitForTimeout(300);
  }
  
  console.log('4. トッピング選択後');
  await page.screenshot({ path: 'test-recipe-4-topping-selected.png' });
  
  // レシピ保存
  page.once('dialog', async dialog => {
    console.log('プロンプト:', dialog.message());
    await dialog.accept('醤油鶏ガラ');
  });
  await page.click('#saveRecipeBtn');
  await page.waitForTimeout(1000);
  
  page.once('dialog', async dialog => {
    console.log('保存完了:', dialog.message());
    await dialog.accept();
  });
  await page.waitForTimeout(500);
  
  console.log('5. レシピ保存後');
  await page.screenshot({ path: 'test-recipe-5-saved.png' });
  
  // 別のレシピを作成
  // ダシをリセット（鶏ガラを解除）
  await page.click('.dashi-btn:nth-child(1)'); // 鶏ガラ解除
  await page.waitForTimeout(300);
  
  // 配置タブで豚骨購入
  await page.click('text=配置');
  await page.waitForTimeout(500);
  
  const porkBtn = await page.$('button:has-text("豚骨")');
  if (porkBtn) {
    await porkBtn.click();
    await page.waitForTimeout(500);
    
    page.once('dialog', async dialog => {
      console.log('購入確認:', dialog.message());
      await dialog.accept();
    });
    await page.waitForTimeout(500);
  }
  
  console.log('6. 豚骨購入');
  await page.screenshot({ path: 'test-recipe-6-bought-pork.png' });
  
  // スープタブに戻る
  await page.click('text=スープ');
  await page.waitForTimeout(500);
  
  // 豚骨ダシを選択（3番目）
  await page.click('.dashi-btn:nth-child(3)');
  await page.waitForTimeout(300);
  
  console.log('7. 豚骨ダシ選択');
  await page.screenshot({ path: 'test-recipe-7-pork-selected.png' });
  
  // 2つ目のレシピを保存
  page.once('dialog', async dialog => {
    await dialog.accept('豚骨ラーメン');
  });
  await page.click('#saveRecipeBtn');
  await page.waitForTimeout(1000);
  
  page.once('dialog', async dialog => {
    await dialog.accept();
  });
  await page.waitForTimeout(500);
  
  console.log('8. 2つ目のレシピ保存');
  await page.screenshot({ path: 'test-recipe-8-second-saved.png' });
  
  // 最初のレシピを読み込み
  page.once('dialog', async dialog => {
    console.log('読込完了:', dialog.message());
    await dialog.accept();
  });
  await page.click('.recipe-load-btn:nth-child(1)');
  await page.waitForTimeout(1000);
  
  console.log('9. レシピ読込後');
  await page.screenshot({ path: 'test-recipe-9-loaded.png' });
  
  // メニューに設定
  page.once('dialog', async dialog => {
    console.log('メニュー設定:', dialog.message());
    await dialog.accept();
  });
  await page.click('.recipe-menu-btn:nth-child(1)');
  await page.waitForTimeout(1000);
  
  console.log('10. メニュー設定後');
  await page.screenshot({ path: 'test-recipe-10-menu-set.png' });
  
  // 価格変更
  await page.fill('#ramenPrice', '1000');
  await page.waitForTimeout(500);
  
  console.log('11. 価格変更');
  await page.screenshot({ path: 'test-recipe-11-price-changed.png' });
  
  console.log('✅ テスト完了');
  
  await page.waitForTimeout(2000);
  await browser.close();
}

test().catch(console.error);
