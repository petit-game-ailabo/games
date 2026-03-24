const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  
  // Navigate to game
  await page.goto('file://' + __dirname + '/index.html');
  await page.waitForTimeout(1000);
  
  console.log('1. Initial state');
  await page.screenshot({ path: 'test-sprint1-1-initial.png', fullPage: true });
  
  // Check layout tab
  console.log('2. Layout tab - initial grid');
  await page.click('button[data-tab="layout"]');
  await page.waitForTimeout(1000);
  await page.screenshot({ path: 'test-sprint1-2-layout.png', fullPage: true });
  
  // Purchase a counter seat
  console.log('3. Purchase counter seat');
  await page.click('.purchase-item[data-item="counter"]');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-sprint1-3-bought-seat.png', fullPage: true });
  
  // Purchase another counter seat
  console.log('4. Purchase another counter seat');
  await page.click('.purchase-item[data-item="counter"]');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-sprint1-4-two-seats.png', fullPage: true });
  
  // Go to business tab
  console.log('5. Business tab');
  await page.click('button[data-tab="business"]');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-sprint1-5-business.png', fullPage: true });
  
  // Start business
  console.log('6. Start business');
  await page.click('#startBusinessBtn');
  await page.waitForTimeout(2000);
  await page.screenshot({ path: 'test-sprint1-6-started.png', fullPage: true });
  
  // Wait 5 seconds (customers should appear)
  console.log('7. After 5 seconds (customers appearing)');
  await page.waitForTimeout(5000);
  await page.screenshot({ path: 'test-sprint1-7-customers.png', fullPage: true });
  
  // Wait 10 more seconds
  console.log('8. After 15 seconds total');
  await page.waitForTimeout(10000);
  await page.screenshot({ path: 'test-sprint1-8-mid-business.png', fullPage: true });
  
  // Close business early
  console.log('9. Close business early');
  await page.click('#closeBusinessBtn');
  await page.waitForTimeout(1000);
  await page.screenshot({ path: 'test-sprint1-9-report.png', fullPage: true });
  
  // Continue to next day
  console.log('10. Continue to day 2');
  await page.click('#continueBtn');
  await page.waitForTimeout(500);
  await page.screenshot({ path: 'test-sprint1-10-day2.png', fullPage: true });
  
  console.log('✅ Test complete! Check screenshots.');
  
  await page.waitForTimeout(2000);
  await browser.close();
})();
