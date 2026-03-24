import { chromium } from 'playwright';
import { fileURLToPath } from 'url';
import path from 'path';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  const htmlPath = path.join(__dirname, 'index.html');
  await page.goto(`file://${htmlPath}`);
  
  console.log('✅ ページ読み込み完了');
  
  // スープタブに移動
  await page.click('button[data-tab="soup"]');
  await page.waitForTimeout(500);
  
  console.log('\n--- 初期価格確認 ---');
  const initialPrice = await page.inputValue('#ramenPrice');
  console.log('初期価格:', initialPrice, '円');
  
  await page.screenshot({ path: 'test-price-1-initial.png' });
  console.log('📸 test-price-1-initial.png');
  
  // 価格変更
  await page.fill('#ramenPrice', '1200');
  await page.waitForTimeout(300);
  
  const newPrice = await page.inputValue('#ramenPrice');
  console.log('変更後の価格:', newPrice, '円');
  
  await page.screenshot({ path: 'test-price-2-changed.png' });
  console.log('📸 test-price-2-changed.png - 価格変更');
  
  // レシピ作成（ダシ、タレ、麺、トッピング）
  await page.click('button[data-dashi="dashi-chicken"]');
  await page.click('button[data-tare="tare-shoyu"]');
  await page.click('button[data-men="men-thin"]');
  await page.click('button[data-topping="topping-negi"]');
  await page.waitForTimeout(300);
  
  // レシピ保存
  page.on('dialog', async dialog => {
    if (dialog.message().includes('レシピ名を入力')) {
      await dialog.accept('高級醤油ラーメン');
    } else {
      await dialog.accept();
    }
  });
  
  await page.click('#saveRecipeBtn');
  await page.waitForTimeout(500);
  
  await page.screenshot({ path: 'test-price-3-recipe-saved.png' });
  console.log('📸 test-price-3-recipe-saved.png - レシピ保存');
  
  // メニューに設定
  const recipeMenuBtn = await page.locator('.recipe-menu-btn').first();
  await recipeMenuBtn.click();
  await page.waitForTimeout(500);
  
  await page.screenshot({ path: 'test-price-4-menu-set.png' });
  console.log('📸 test-price-4-menu-set.png - メニュー設定');
  
  // 営業タブに移動して価格が反映されているか確認
  await page.click('button[data-tab="business"]');
  await page.waitForTimeout(500);
  
  await page.screenshot({ path: 'test-price-5-business-tab.png' });
  console.log('📸 test-price-5-business-tab.png - 営業タブ');
  
  console.log('\n✅ 価格設定テスト完了！');
  
  await browser.close();
})();
