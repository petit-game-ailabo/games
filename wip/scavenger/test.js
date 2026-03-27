const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  
  try {
    console.log('Loading game...');
    await page.goto('file://' + __dirname + '/index.html');
    await page.waitForTimeout(1000);
    
    console.log('Clicking start button...');
    await page.click('button:has-text("🎮 探索開始")');
    await page.waitForTimeout(500);
    
    console.log('Entering dungeon...');
    await page.click('button[data-action="enter"]');
    await page.waitForTimeout(1000);
    
    console.log('Testing movement (down)...');
    await page.click('button[data-dir="down"]');
    await page.waitForTimeout(300);
    
    console.log('Testing movement (right)...');
    await page.click('button[data-dir="right"]');
    await page.waitForTimeout(300);
    
    console.log('Opening bag...');
    await page.click('button[data-action="bag"]');
    await page.waitForTimeout(500);
    
    console.log('Closing bag...');
    await page.click('button[data-action="closebag"]');
    await page.waitForTimeout(300);
    
    console.log('✅ All smoke tests passed!');
  } catch (error) {
    console.error('❌ Test failed:', error.message);
    process.exit(1);
  } finally {
    await browser.close();
  }
})();
