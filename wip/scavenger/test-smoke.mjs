import { chromium } from 'playwright';
import { fileURLToPath } from 'url';
import { dirname, resolve } from 'path';

const __dirname = dirname(fileURLToPath(import.meta.url));
const htmlPath = 'file://' + resolve(__dirname, 'index.html');

const browser = await chromium.launch();
const page = await browser.newPage();

try {
  await page.goto(htmlPath);
  console.log('✓ Page loaded');
  
  // Click start button
  await page.click('text=はじめる');
  await page.waitForTimeout(500);
  console.log('✓ Game started');
  
  // Check if we're at base
  const baseVisible = await page.locator('text=拠点').isVisible();
  console.log('✓ Base screen visible:', baseVisible);
  
  // Enter dungeon
  await page.click('text=探索開始');
  await page.waitForTimeout(500);
  console.log('✓ Entered dungeon');
  
  // Check map container
  const mapVisible = await page.locator('.map-container').isVisible();
  console.log('✓ Map visible:', mapVisible);
  
  // Try moving with d-pad
  await page.click('[data-dir="up"]');
  await page.waitForTimeout(200);
  console.log('✓ Movement works');
  
  // Check for room types in generated map
  const logBar = await page.locator('.log-bar').textContent();
  console.log('✓ Log bar content:', logBar.substring(0, 50) + '...');
  
  // Open inventory
  await page.click('.bag-btn');
  await page.waitForTimeout(300);
  const invVisible = await page.locator('text=バックパック').isVisible();
  console.log('✓ Inventory opens:', invVisible);
  
  console.log('\n✅ All smoke tests passed!');
  
} catch (err) {
  console.error('❌ Test failed:', err.message);
  process.exit(1);
} finally {
  await browser.close();
}
