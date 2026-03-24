import { chromium } from 'playwright';

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage();
  
  await page.goto('file://' + process.cwd() + '/index.html');
  
  console.log('1. Initial load');
  await page.waitForTimeout(1000);
  await page.screenshot({ path: 'todaysminigame-games/wip/ramen-stand/test-tap-1-initial.png' });
  
  // Go to purchase tab and buy seats
  console.log('2. Buying seats...');
  await page.click('button[data-tab="purchase"]');
  await page.waitForTimeout(500);
  
  // Scroll to counter seat in purchase panel
  await page.evaluate(() => {
    const counterItem = document.querySelector('[data-item="seat-counter"]');
    if (counterItem) counterItem.scrollIntoView();
  });
  await page.waitForTimeout(300);
  
  // Buy 3 counter seats
  for (let i = 0; i < 3; i++) {
    await page.click('[data-item="seat-counter"]');
    await page.waitForTimeout(200);
  }
  
  console.log('3. Placing seats on grid...');
  await page.click('button[data-tab="layout"]');
  await page.waitForTimeout(500);
  
  // Place seats on grid
  await page.click('.grid-cell:nth-child(8)'); // (1,1)
  await page.waitForTimeout(200);
  await page.click('.grid-cell:nth-child(9)'); // (2,1)
  await page.waitForTimeout(200);
  await page.click('.grid-cell:nth-child(10)'); // (3,1)
  await page.waitForTimeout(500);
  
  console.log('4. Seats placed');
  await page.screenshot({ path: 'todaysminigame-games/wip/ramen-stand/test-tap-2-seats.png' });
  
  // Go to business tab and start
  console.log('5. Starting business...');
  await page.click('button[data-tab="business"]');
  await page.waitForTimeout(500);
  await page.click('#startBusinessBtn');
  await page.waitForTimeout(2000);
  
  console.log('6. Business running');
  await page.screenshot({ path: 'todaysminigame-games/wip/ramen-stand/test-tap-3-business.png' });
  
  // Wait for customers to arrive and start eating
  console.log('7. Waiting for customers to eat...');
  await page.waitForTimeout(5000);
  
  // Check if customers exist
  const customerCount = await page.evaluate(() => {
    return document.querySelectorAll('.customer').length;
  });
  console.log(`   Found ${customerCount} customers`);
  
  await page.screenshot({ path: 'todaysminigame-games/wip/ramen-stand/test-tap-4-customers.png' });
  
  // Click on the first customer
  if (customerCount > 0) {
    console.log('8. Clicking customer for feedback...');
    await page.click('.customer');
    await page.waitForTimeout(1000);
    
    const toastExists = await page.evaluate(() => {
      return !!document.querySelector('.feedback-toast');
    });
    
    if (toastExists) {
      console.log('   ✓ Feedback toast displayed!');
      await page.screenshot({ path: 'todaysminigame-games/wip/ramen-stand/test-tap-5-feedback.png' });
    } else {
      console.log('   ✗ No feedback toast found');
    }
    
    await page.waitForTimeout(3000);
    
    // Try clicking another customer if exists
    const customers = await page.$$('.customer');
    if (customers.length > 1) {
      console.log('9. Clicking second customer...');
      await customers[1].click();
      await page.waitForTimeout(1000);
      await page.screenshot({ path: 'todaysminigame-games/wip/ramen-stand/test-tap-6-second-feedback.png' });
      await page.waitForTimeout(3000);
    }
  } else {
    console.log('   ✗ No customers found to click');
  }
  
  console.log('10. Final state');
  await page.screenshot({ path: 'todaysminigame-games/wip/ramen-stand/test-tap-7-final.png' });
  
  console.log('\nTest complete!');
  await browser.close();
})();
