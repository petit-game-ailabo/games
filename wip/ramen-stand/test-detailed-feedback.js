const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 460, height: 800 } });
  const page = await context.newPage();

  const filePath = 'file://' + path.resolve(__dirname, 'index.html');
  await page.goto(filePath);

  console.log('✅ Game loaded');

  // Start new game
  await page.click('button:has-text("はじめから")');
  await page.waitForTimeout(500);
  console.log('✅ New game started');

  // Start day
  await page.click('button:has-text("営業開始")');
  await page.waitForTimeout(1000);
  console.log('✅ Day started');

  // Wait for a customer to appear
  await page.waitForSelector('.customer', { timeout: 10000 });
  console.log('✅ Customer appeared');

  // Wait for customer to sit (state: SITTING or later)
  await page.waitForTimeout(5000);
  console.log('⏳ Customer should be sitting...');

  // Click on customer (should show preferences)
  const firstCustomer = page.locator('.customer').first();
  await firstCustomer.click({ force: true });
  await page.waitForTimeout(800);
  
  await page.screenshot({ path: path.join(__dirname, 'feedback-1-sitting.png') });
  console.log('✅ Screenshot 1: Customer sitting (preferences)');

  // Wait for customer to finish eating (needs more time: sit + wait + eat)
  console.log('⏳ Waiting for customer to finish eating...');
  await page.waitForTimeout(8000);

  // Click again (should show satisfaction feedback)
  await firstCustomer.click({ force: true });
  await page.waitForTimeout(800);
  
  await page.screenshot({ path: path.join(__dirname, 'feedback-2-after-eating.png') });
  console.log('✅ Screenshot 2: After eating (satisfaction feedback)');

  await page.waitForTimeout(1000);

  await browser.close();
  console.log('✅ Test complete');
})();
