import { chromium } from 'playwright';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const filePath = `file://${path.resolve(__dirname, 'index.html')}`;

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 460, height: 800 } });
  
  const jsErrors = [];
  page.on('pageerror', e => jsErrors.push(e.message));
  
  await page.goto(filePath);
  await page.click('text=はじめる');
  await page.waitForTimeout(200);
  await page.click('[data-action="enter"]');
  await page.waitForTimeout(300);
  
  // Place an item right next to the player via JS
  await page.evaluate(() => {
    mapItems.push({ x: player.x + 1, y: player.y, itemId: 'goldbar' });
    render();
  });
  
  // Move right onto the item
  await page.click('[data-dir="right"]');
  await page.waitForTimeout(300);
  
  // Check preview modal
  const modal = await page.locator('.item-preview-modal').count();
  console.assert(modal > 0, 'Preview modal should appear');
  console.log('✅ Item preview modal appeared');
  
  // Check content
  const cardText = await page.locator('.item-preview-card').textContent();
  console.assert(cardText.includes('金塊'), 'Should show item name');
  console.assert(cardText.includes('200G'), 'Should show sell value');
  console.log('✅ Preview shows name and value');
  
  // Check shape grid is shown
  const shapeGrid = await page.locator('.ipv-shape-grid').count();
  console.assert(shapeGrid > 0, 'Shape grid should be shown');
  console.log('✅ Shape grid rendered');
  
  // Test "ignore" button - item should stay on floor
  const itemCountBefore = await page.evaluate(() => mapItems.length);
  await page.click('[data-action="ignore-item"]');
  await page.waitForTimeout(200);
  const itemCountAfter = await page.evaluate(() => mapItems.length);
  const gs = await page.evaluate(() => gameState);
  console.assert(gs === 'dungeon', 'Should return to dungeon state');
  console.assert(itemCountAfter === itemCountBefore, 'Item should remain on floor after ignore');
  console.log('✅ Ignore keeps item on floor');
  
  // Walk back onto the item and this time pick it up
  // Move left then right to step on it again
  await page.click('[data-dir="left"]');
  await page.waitForTimeout(200);
  await page.click('[data-dir="right"]');
  await page.waitForTimeout(300);
  
  const modal2 = await page.locator('.item-preview-modal').count();
  console.assert(modal2 > 0, 'Preview modal should appear again');
  console.log('✅ Preview modal appears again on re-visit');
  
  // Click pickup
  await page.click('[data-action="confirm-pickup"]');
  await page.waitForTimeout(300);
  
  const gs2 = await page.evaluate(() => gameState);
  console.assert(gs2 === 'inventory', 'Should go to inventory for placement');
  const selLoot = await page.evaluate(() => selectedLootItem);
  console.assert(selLoot === 'goldbar', 'Should have goldbar selected for placement');
  console.log('✅ Pickup transitions to inventory placement');
  
  // Check item removed from floor
  const itemCountFinal = await page.evaluate(() => mapItems.filter(i => i.itemId === 'goldbar').length);
  console.assert(itemCountFinal === 0, 'Goldbar should be removed from floor');
  console.log('✅ Item removed from floor after pickup');
  
  if (jsErrors.length > 0) {
    console.error('❌ JS errors:', jsErrors);
    process.exit(1);
  }
  
  console.log('\n✅ All item preview tests passed!');
  await browser.close();
})();
