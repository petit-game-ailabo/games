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
  
  // Place item next to player with no enemies nearby
  await page.evaluate(() => {
    enemies = []; // clear enemies for clean test
    mapItems.push({ x: player.x + 1, y: player.y, itemId: 'goldbar' });
    render();
  });
  
  // Move onto item
  await page.click('[data-dir="right"]');
  await page.waitForTimeout(300);
  
  // Debug: check state and content
  const state1 = await page.evaluate(() => gameState);
  console.log('State after stepping on item:', state1);
  
  const nameVisible = await page.locator('.ipv-name').textContent();
  console.log('Item name shown:', nameVisible);
  
  const valueVisible = await page.locator('.ipv-value').textContent();
  console.log('Value shown:', valueVisible);
  
  const effectVisible = await page.locator('.ipv-effect').textContent();
  console.log('Effect shown:', effectVisible);
  
  // Test ignore
  await page.click('[data-action="ignore-item"]');
  await page.waitForTimeout(200);
  const state2 = await page.evaluate(() => gameState);
  const itemsOnFloor = await page.evaluate(() => mapItems.filter(i => i.itemId === 'goldbar').length);
  console.log('State after ignore:', state2, 'Items on floor:', itemsOnFloor);
  
  // Step on item again
  await page.click('[data-dir="left"]');
  await page.waitForTimeout(200);
  await page.click('[data-dir="right"]');
  await page.waitForTimeout(300);
  
  const state3 = await page.evaluate(() => gameState);
  console.log('State after re-step:', state3);
  
  // Pick up this time
  await page.click('[data-action="confirm-pickup"]');
  await page.waitForTimeout(300);
  
  const state4 = await page.evaluate(() => gameState);
  const selLoot = await page.evaluate(() => selectedLootItem);
  const itemsLeft = await page.evaluate(() => mapItems.filter(i => i.itemId === 'goldbar').length);
  console.log('State after pickup:', state4, 'Selected:', selLoot, 'Items left:', itemsLeft);
  
  if (jsErrors.length > 0) {
    console.error('❌ JS errors:', jsErrors);
    process.exit(1);
  }
  
  // Verify all critical paths
  let pass = true;
  if (state1 !== 'item_preview') { console.error('❌ Should be item_preview'); pass = false; }
  if (state2 !== 'dungeon') { console.error('❌ Should be dungeon after ignore'); pass = false; }
  if (itemsOnFloor !== 1) { console.error('❌ Item should stay on floor'); pass = false; }
  if (state3 !== 'item_preview') { console.error('❌ Should be item_preview again'); pass = false; }
  if (state4 !== 'inventory') { console.error('❌ Should be inventory after pickup'); pass = false; }
  if (selLoot !== 'goldbar') { console.error('❌ Should have goldbar selected'); pass = false; }
  if (itemsLeft !== 0) { console.error('❌ Item should be removed from floor'); pass = false; }
  
  if (pass) console.log('\n✅ ALL TESTS PASSED');
  else process.exit(1);
  
  await browser.close();
})();
