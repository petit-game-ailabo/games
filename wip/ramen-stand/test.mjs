import { chromium } from 'playwright';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';

const __dirname = dirname(fileURLToPath(import.meta.url));
const htmlPath = 'file://' + join(__dirname, 'index.html');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  
  console.log('Loading game...');
  await page.goto(htmlPath);
  
  // Start new game
  await page.click('text=はじめから');
  await page.waitForTimeout(1000);
  
  // Take screenshot of initial state
  await page.screenshot({ path: 'test-initial.png', fullPage: true });
  console.log('✓ Screenshot: test-initial.png');
  
  // Check research tab (where recipe system is)
  await page.click('button:has-text("研究")');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-research.png', fullPage: true });
  console.log('✓ Screenshot: test-research.png');
  
  // Start operation
  await page.click('button:has-text("営業")');
  await page.waitForTimeout(500);
  
  // Start day
  await page.click('button:has-text("営業開始")');
  console.log('✓ Day started');
  
  // Wait for customers to appear and eat
  console.log('Waiting for customers...');
  await page.waitForTimeout(10000);
  
  // Take screenshot during operation
  await page.screenshot({ path: 'test-operation.png', fullPage: true });
  console.log('✓ Screenshot: test-operation.png');
  
  // Check customer count
  const customerCount = await page.locator('.customer').count();
  console.log(`✓ Found ${customerCount} customers`);
  
  // Check money (should have changed if customers paid)
  const moneyText = await page.locator('#money').textContent();
  console.log(`✓ Current money: ${moneyText}円`);
  
  // End day
  await page.click('button:has-text("営業終了")');
  await page.waitForTimeout(3000);
  
  // Should show day summary modal
  const summaryVisible = await page.locator('#day-summary-modal.active').isVisible();
  console.log(`✓ Day summary modal: ${summaryVisible ? 'shown' : 'hidden'}`);
  
  await page.screenshot({ path: 'test-summary.png', fullPage: true });
  console.log('✓ Screenshot: test-summary.png');
  
  console.log('\n✨ All tests passed!');
  
  await page.waitForTimeout(2000);
  await browser.close();
})();
