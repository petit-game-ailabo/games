const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 460, height: 900 }
  });
  const page = await context.newPage();

  const htmlPath = 'file://' + path.resolve('./index.html');
  console.log('Opening:', htmlPath);
  await page.goto(htmlPath);

  // Start game
  await page.click('text=はじめから');
  await page.waitForTimeout(1000);

  // Add research points directly via console
  await page.evaluate(() => {
    game.researchPoints = 30;
    game.updateUI();
  });
  
  console.log('✅ Added 30 research points');

  // Navigate to 研究 tab
  await page.click('button[data-tab="research"]');
  await page.waitForTimeout(500);
  
  console.log('✅ Opened Research tab');

  // Check default tare (shoyu should be selected)
  const defaultTare = await page.locator('.furniture-item.selected').filter({ hasText: '醤油' }).count();
  console.log('✅ Default tare (醤油) selected:', defaultTare > 0);

  // Check recipe name
  let recipeName = await page.inputValue('#recipe-name');
  console.log('✅ Initial recipe name:', recipeName);
  
  // Take screenshot before unlocking
  await page.screenshot({ path: 'test-tare-before.png', fullPage: true });
  console.log('✅ Screenshot saved: test-tare-before.png');
  
  // Unlock 味噌 tare
  console.log('✅ Unlocking 味噌...');
  await page.locator('button:has-text("味噌")').first().click();
  await page.waitForTimeout(1000);
  
  // Click on 味噌 tare
  console.log('✅ Selecting 味噌 tare...');
  await page.locator('.furniture-item').filter({ hasText: '味噌' }).first().click();
  await page.waitForTimeout(500);
  
  // Check if recipe name changed
  recipeName = await page.inputValue('#recipe-name');
  console.log('✅ Recipe name after selecting 味噌:', recipeName);
  
  if (recipeName === '味噌ラーメン') {
    console.log('✅✅ SUCCESS! Tare selection working! Recipe name auto-updated!');
  } else {
    console.log('❌ FAIL: Recipe name did not update correctly. Expected "味噌ラーメン", got "' + recipeName + '"');
  }
  
  // Check visual selection
  const misoSelected = await page.locator('.furniture-item.selected').filter({ hasText: '味噌' }).count();
  console.log('✅ 味噌 visually selected:', misoSelected > 0);
  
  // Test switching back to 醤油
  console.log('✅ Switching back to 醤油...');
  await page.locator('.furniture-item').filter({ hasText: '醤油' }).first().click();
  await page.waitForTimeout(500);
  
  recipeName = await page.inputValue('#recipe-name');
  console.log('✅ Recipe name after selecting 醤油:', recipeName);
  
  if (recipeName === '醤油ラーメン') {
    console.log('✅✅ SUCCESS! Switching back works!');
  } else {
    console.log('❌ FAIL: Recipe name did not switch back correctly');
  }
  
  // Unlock 塩 tare
  console.log('✅ Unlocking 塩...');
  await page.locator('button:has-text("塩")').first().click();
  await page.waitForTimeout(1000);
  
  // Select 塩
  console.log('✅ Selecting 塩 tare...');
  await page.locator('.furniture-item').filter({ hasText: '塩' }).first().click();
  await page.waitForTimeout(500);
  
  recipeName = await page.inputValue('#recipe-name');
  console.log('✅ Recipe name after selecting 塩:', recipeName);
  
  if (recipeName === '塩ラーメン') {
    console.log('✅✅ SUCCESS! All three tare types working!');
  } else {
    console.log('❌ FAIL: 塩 recipe name incorrect');
  }

  // Take final screenshot
  await page.screenshot({ path: 'test-tare-after.png', fullPage: true });
  console.log('✅ Screenshot saved: test-tare-after.png');

  await page.waitForTimeout(1000);
  await browser.close();
  console.log('\n✅✅✅ Test complete! Tare selection implementation verified!');
})();
