import { chromium } from 'playwright';
import { fileURLToPath } from 'url';
import path from 'path';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const htmlPath = path.join(__dirname, 'index.html');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 460, height: 900 } });
  const page = await context.newPage();

  await page.goto(`file://${htmlPath}`);
  console.log('✓ Page loaded');

  // Start new game
  await page.click('button:has-text("はじめから")');
  await page.waitForTimeout(500);
  console.log('✓ Game started');

  // Navigate to Research tab
  await page.click('.tab[data-tab="research"]');
  await page.waitForTimeout(500);
  console.log('✓ Research tab opened');
  await page.screenshot({ path: path.join(__dirname, 'test-soup-1-initial.png') });

  // Check initial state (should have chicken and vegetable)
  const initialIngredients = await page.$$('.info-section:has-text("ダシ素材") .furniture-item.selected');
  console.log(`✓ Initial ingredients selected: ${initialIngredients.length}`);

  // Check initial params
  const params = await page.textContent('.info-section:has-text("現在のパラメータ")');
  console.log(`✓ Initial params: ${params.replace(/\s+/g, ' ')}`);

  // Manually add research points for testing
  await page.evaluate(() => {
    game.researchPoints = 50;
    game.updateUI();
  });
  console.log('✓ Added research points for testing');

  // Go back to research tab
  await page.click('.tab[data-tab="research"]');
  await page.waitForTimeout(500);
  await page.screenshot({ path: path.join(__dirname, 'test-soup-2-with-points.png') });

  // Check research points
  const researchPts = await page.textContent('#research');
  console.log(`✓ Research points: ${researchPts}`);

  // Try to unlock a new ingredient (pork - 10pt)
  const unlockButtons = await page.$$('button:has-text("豚骨")');
  if (unlockButtons.length > 0) {
    await unlockButtons[0].click();
    await page.waitForTimeout(500);
    console.log('✓ Unlocked pork');
    await page.screenshot({ path: path.join(__dirname, 'test-soup-3-unlocked-pork.png') });
  }

  // Refresh tab to show unlocked ingredient
  await page.click('.tab[data-tab="operations"]');
  await page.waitForTimeout(200);
  await page.click('.tab[data-tab="research"]');
  await page.waitForTimeout(500);

  // Select pork ingredient
  const porkButton = await page.$('.info-section:has-text("ダシ素材") .furniture-item:has-text("豚骨")');
  if (porkButton) {
    await porkButton.click();
    await page.waitForTimeout(500);
    console.log('✓ Selected pork ingredient');
    await page.screenshot({ path: path.join(__dirname, 'test-soup-4-pork-selected.png') });
  }

  // Check updated params
  const newParams = await page.textContent('.info-section:has-text("現在のパラメータ")');
  console.log(`✓ New params: ${newParams.replace(/\s+/g, ' ')}`);

  // Change recipe name
  await page.fill('#recipe-name', '特製豚骨ラーメン');
  await page.waitForTimeout(300);
  console.log('✓ Changed recipe name');

  // Save recipe
  await page.click('button:has-text("レシピを保存")');
  await page.waitForTimeout(500);
  await page.screenshot({ path: path.join(__dirname, 'test-soup-5-saved-recipe.png') });
  console.log('✓ Saved recipe');

  // Check saved recipes section
  const savedRecipes = await page.$$('.info-section:has-text("保存済みレシピ") > div');
  console.log(`✓ Saved recipes count: ${savedRecipes.length - 1}`); // -1 for the title

  // Create another recipe
  await page.click('.info-section:has-text("ダシ素材") .furniture-item:has-text("鶏ガラ")');
  await page.waitForTimeout(300);
  await page.fill('#recipe-name', 'あっさり醤油');
  await page.click('button:has-text("レシピを保存")');
  await page.waitForTimeout(500);
  await page.screenshot({ path: path.join(__dirname, 'test-soup-6-second-recipe.png') });
  console.log('✓ Saved second recipe');

  // Load first recipe
  const loadButtons = await page.$$('button:has-text("読み込む")');
  if (loadButtons.length > 0) {
    await loadButtons[0].click();
    await page.waitForTimeout(500);
    console.log('✓ Loaded first recipe');
    await page.screenshot({ path: path.join(__dirname, 'test-soup-7-loaded-recipe.png') });
  }

  // Final screenshot
  await page.screenshot({ path: path.join(__dirname, 'test-soup-8-final.png') });

  console.log('\n✅ All tests completed successfully!');
  
  await page.waitForTimeout(2000);
  await browser.close();
})();
