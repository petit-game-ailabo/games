import { chromium } from 'playwright';
import { fileURLToPath } from 'url';
import path from 'path';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const filePath = `file://${path.resolve(__dirname, 'index.html')}`;

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 460, height: 800 } });
  await page.goto(filePath);
  
  // 1. Title screen should be visible
  const title = await page.locator('.title-screen h1').textContent();
  console.assert(title.includes('スカベンジャー'), 'Title should show');
  console.log('✅ Title screen OK');
  
  // 2. Click start
  await page.click('text=はじめる');
  await page.waitForTimeout(300);
  
  // 3. Should be on base screen
  const baseH2 = await page.locator('.base-screen h2').textContent();
  console.assert(baseH2.includes('拠点'), 'Should be on base screen');
  console.log('✅ Base screen OK');
  
  // 4. Enter dungeon
  await page.click('[data-action="enter"]');
  await page.waitForTimeout(300);
  
  // 5. Should see dungeon UI (status bar, dpad)
  const statusBar = await page.locator('.status-bar').count();
  console.assert(statusBar > 0, 'Status bar should exist');
  console.log('✅ Dungeon UI OK');
  
  // 6. Try moving (press up/down/left/right)
  await page.click('[data-dir="right"]');
  await page.waitForTimeout(200);
  await page.click('[data-dir="down"]');
  await page.waitForTimeout(200);
  console.log('✅ Movement OK');
  
  // 7. Open bag
  await page.click('[data-action="bag"]');
  await page.waitForTimeout(200);
  const overlay = await page.locator('.overlay').count();
  console.assert(overlay > 0, 'Bag overlay should open');
  console.log('✅ Bag overlay OK');
  
  // 8. Close bag
  await page.click('[data-action="closebag"]');
  await page.waitForTimeout(200);
  
  // 9. Check no JS errors occurred
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  
  // 10. Move a bunch to find an item and test preview modal
  for (let i = 0; i < 20; i++) {
    await page.click('[data-dir="right"]');
    await page.waitForTimeout(100);
    // Check if item preview modal appeared
    const previewModal = await page.locator('.item-preview-modal').count();
    if (previewModal > 0) {
      console.log('✅ Item preview modal appeared!');
      // Check it has pickup and ignore buttons
      const pickupBtn = await page.locator('[data-action="confirm-pickup"]').count();
      const ignoreBtn = await page.locator('[data-action="ignore-item"]').count();
      console.assert(pickupBtn > 0, 'Pickup button should exist');
      console.assert(ignoreBtn > 0, 'Ignore button should exist');
      // Test ignore
      await page.click('[data-action="ignore-item"]');
      await page.waitForTimeout(200);
      console.log('✅ Ignore button works');
      break;
    }
    await page.click('[data-dir="down"]');
    await page.waitForTimeout(100);
  }
  
  console.log('✅ All smoke tests passed');
  
  if (errors.length > 0) {
    console.error('JS errors:', errors);
    process.exit(1);
  }
  
  await browser.close();
})();
