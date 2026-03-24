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
  
  // 初期状態確認
  await page.screenshot({ path: 'test-selector-1-initial.png' });
  console.log('📸 test-selector-1-initial.png');
  
  // スープタブに移動
  await page.click('button[data-tab="soup"]');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-selector-2-soup-tab.png' });
  console.log('📸 test-selector-2-soup-tab.png - スープタブ');
  
  // タレセクションの確認（醤油は初期解放済み）
  const tareSection = await page.locator('#tareSelector').innerHTML();
  console.log('\n--- タレセクション ---');
  console.log(tareSection);
  
  // 麺セクションの確認
  const menSection = await page.locator('#menSelector').innerHTML();
  console.log('\n--- 麺セクション ---');
  console.log(menSection);
  
  // トッピングセクションの確認
  const toppingSection = await page.locator('#toppingSelector').innerHTML();
  console.log('\n--- トッピングセクション ---');
  console.log(toppingSection);
  
  // 醤油を選択
  await page.click('button[data-tare="tare-shoyu"]');
  await page.waitForTimeout(300);
  await page.screenshot({ path: 'test-selector-3-shoyu-selected.png' });
  console.log('📸 test-selector-3-shoyu-selected.png - 醤油選択');
  
  // 麺を選択（極細ストレート）
  await page.click('button[data-men="men-thin"]');
  await page.waitForTimeout(300);
  await page.screenshot({ path: 'test-selector-4-men-selected.png' });
  console.log('📸 test-selector-4-men-selected.png - 麺選択');
  
  // トッピング選択（ネギ）
  await page.click('button[data-topping="topping-negi"]');
  await page.waitForTimeout(300);
  await page.screenshot({ path: 'test-selector-5-topping-selected.png' });
  console.log('📸 test-selector-5-topping-selected.png - トッピング選択');
  
  // もう1つトッピング追加（海苔）
  await page.click('button[data-topping="topping-nori"]');
  await page.waitForTimeout(300);
  await page.screenshot({ path: 'test-selector-6-multi-toppings.png' });
  console.log('📸 test-selector-6-multi-toppings.png - 複数トッピング');
  
  // ダシも選択してレシピ保存
  await page.click('button[data-dashi="dashi-chicken"]');
  await page.waitForTimeout(300);
  
  // レシピ保存ボタンをクリック
  page.on('dialog', async dialog => {
    console.log('📝 ダイアログ:', dialog.message());
    if (dialog.message().includes('レシピ名を入力')) {
      await dialog.accept('醤油ラーメン');
    } else {
      await dialog.accept();
    }
  });
  
  await page.click('#saveRecipeBtn');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-selector-7-recipe-saved.png' });
  console.log('📸 test-selector-7-recipe-saved.png - レシピ保存完了');
  
  // 保存されたレシピを確認
  const savedRecipes = await page.locator('#savedRecipes').innerHTML();
  console.log('\n--- 保存されたレシピ ---');
  console.log(savedRecipes);
  
  console.log('\n✅ テスト完了！');
  
  await browser.close();
})();
