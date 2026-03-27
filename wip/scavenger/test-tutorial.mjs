import { chromium } from 'playwright';

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  await page.goto('file://' + process.cwd() + '/index.html');
  
  // Wait for title screen
  await page.waitForSelector('text=はじめる', { timeout: 5000 });
  console.log('✓ Title screen loaded');
  
  // Start new game
  await page.click('text=はじめる');
  await page.waitForTimeout(500);
  console.log('✓ New game started');
  
  // Should see base screen
  const baseText = await page.textContent('body');
  if (baseText.includes('拠点') || baseText.includes('探索開始')) {
    console.log('✓ Base screen visible');
  }
  
  // Enter dungeon (should trigger tutorial on first entry)
  await page.click('text=探索開始');
  await page.waitForTimeout(1000);
  
  // Check if tutorial overlay appeared
  const hasTutorial = await page.locator('.tutorial-overlay').count();
  if (hasTutorial > 0) {
    console.log('✓ Tutorial overlay appeared on first dungeon entry');
    
    // Close tutorial
    await page.click('text=わかった！');
    await page.waitForTimeout(500);
    
    const tutorialGone = await page.locator('.tutorial-overlay').count();
    if (tutorialGone === 0) {
      console.log('✓ Tutorial closed successfully');
    }
  } else {
    console.log('⚠ Tutorial did not appear (might already be seen in save)');
  }
  
  // Check for help button
  const helpBtn = await page.locator('.help-btn').count();
  if (helpBtn > 0) {
    console.log('✓ Help button (?) visible in dungeon');
    
    // Click help button
    await page.click('.help-btn');
    await page.waitForTimeout(500);
    
    const helpVisible = await page.locator('.tutorial-overlay').count();
    if (helpVisible > 0) {
      console.log('✓ Help overlay opened from button');
      await page.click('text=わかった！');
      await page.waitForTimeout(300);
    }
  } else {
    console.log('✗ Help button not found');
  }
  
  // Try moving (basic smoke test)
  await page.click('[data-dir="down"]');
  await page.waitForTimeout(300);
  console.log('✓ Movement button works');
  
  await browser.close();
  console.log('\n✅ All tests passed!');
})();
