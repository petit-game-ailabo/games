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

  await page.waitForTimeout(2000);

  // Take screenshot before clicking
  await page.screenshot({ path: path.join(__dirname, 'test-customer-before.png') });
  console.log('✅ Screenshot taken (before click)');

  // Click on the first customer
  const customer = await page.locator('.customer').first();
  await customer.click({ force: true });
  await page.waitForTimeout(500);

  // Take screenshot after clicking (notification should show)
  await page.screenshot({ path: path.join(__dirname, 'test-customer-after.png') });
  console.log('✅ Screenshot taken (after click)');

  // Wait for customer to sit and eat
  console.log('⏳ Waiting for customer to eat...');
  await page.waitForTimeout(8000);

  // Click on customer again (should show feedback with satisfaction)
  await customer.click({ force: true });
  await page.waitForTimeout(500);

  await page.screenshot({ path: path.join(__dirname, 'test-customer-feedback.png') });
  console.log('✅ Screenshot taken (feedback after eating)');

  // Check if notification is visible
  const notification = await page.locator('#notification');
  const isVisible = await notification.isVisible();
  console.log(`✅ Notification visible: ${isVisible}`);

  await page.waitForTimeout(2000);

  await browser.close();
  console.log('✅ Test complete');
})();
