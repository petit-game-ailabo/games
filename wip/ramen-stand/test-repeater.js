import { chromium } from 'playwright';

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  
  await page.goto('file://' + process.cwd() + '/index.html');
  await page.waitForTimeout(2000);
  
  console.log('1. タイトル画面のスクショ');
  await page.screenshot({ path: 'test-repeater-1-title.png' });
  
  // ゲームを開始
  console.log('2. ゲーム開始');
  await page.click('button:has-text("はじめから")');
  await page.waitForTimeout(1000);
  
  await page.screenshot({ path: 'test-repeater-2-started.png' });
  
  // 席を追加（リピーター発生確率を上げる）
  console.log('3. 席を追加');
  await page.click('.tab[data-tab="layout"]');
  await page.waitForTimeout(500);
  
  // カウンター席を選択
  const layoutContent = await page.textContent('#tab-content');
  console.log('配置タブの内容:', layoutContent.substring(0, 200));
  
  // furniture-itemクラスの最初の要素（カウンター席）をクリック
  await page.evaluate(() => {
    game.selectFurniture('counter');
  });
  await page.waitForTimeout(500);
  
  // グリッドの適当な位置にクリック（座標ベース）
  await page.evaluate(() => {
    const cells = document.querySelectorAll('.grid-cell');
    if (cells.length > 15) cells[15].click();
  });
  await page.waitForTimeout(500);
  
  // もう一つ席を追加
  await page.evaluate(() => {
    game.selectFurniture('counter');
  });
  await page.waitForTimeout(300);
  await page.evaluate(() => {
    const cells = document.querySelectorAll('.grid-cell');
    if (cells.length > 20) cells[20].click();
  });
  await page.waitForTimeout(500);
  
  await page.screenshot({ path: 'test-repeater-3-seats.png' });
  
  // 営業開始
  console.log('4. 営業開始');
  await page.click('.tab[data-tab="operations"]');
  await page.waitForTimeout(500);
  await page.click('button:has-text("営業開始")');
  await page.waitForTimeout(8000); // 8秒待機
  
  await page.screenshot({ path: 'test-repeater-4-business.png' });
  
  // 複数日営業してリピーター発生を待つ
  for (let day = 1; day <= 10; day++) {
    console.log(`Day ${day}: 閉店→開店`);
    
    // 閉店
    await page.click('button:has-text("営業終了")');
    await page.waitForTimeout(2000);
    
    // 日次レポートが表示されるので閉じる
    const summaryVisible = await page.isVisible('#day-summary-modal.active');
    if (summaryVisible) {
      await page.click('button:has-text("次の日へ")');
      await page.waitForTimeout(1000);
    }
    
    // 開店
    await page.click('button:has-text("営業開始")');
    await page.waitForTimeout(8000);
    
    // リピーターの状態を確認
    const repeaters = await page.evaluate(() => {
      return window.game?.repeaters || [];
    });
    
    console.log(`  リピーター登録数: ${repeaters.length}`);
    
    if (repeaters.length > 0) {
      console.log(`  ✅ リピーター登録確認！`);
      console.log(`  登録者:`, repeaters.map(r => r.name).join(', '));
      
      await page.screenshot({ path: `test-repeater-5-day${day}-registered.png` });
      
      // さらに数日待ってリピーター来店を確認
      for (let extraDay = 1; extraDay <= 5; extraDay++) {
        await page.click('button:has-text("営業終了")');
        await page.waitForTimeout(1500);
        await page.click('button:has-text("次の日へ")');
        await page.waitForTimeout(1000);
        await page.click('button:has-text("営業開始")');
        await page.waitForTimeout(8000);
        
        const nameTagCount = await page.evaluate(() => {
          return document.querySelectorAll('.name-tag').length;
        });
        
        console.log(`  Extra Day ${extraDay}: 名札付き客 ${nameTagCount}人`);
        
        if (nameTagCount > 0) {
          console.log(`  ✅✅ リピーター来店確認！`);
          await page.screenshot({ path: `test-repeater-6-visit-day${day + extraDay}.png` });
          break;
        }
      }
      
      break;
    }
  }
  
  // 最終状態
  await page.screenshot({ path: 'test-repeater-7-final.png' });
  
  // リピーターの状態をログ出力
  const repeaters = await page.evaluate(() => {
    return window.game?.repeaters || [];
  });
  
  console.log('\n現在のリピーター:');
  console.log(JSON.stringify(repeaters, null, 2));
  
  await page.waitForTimeout(2000);
  await browser.close();
  
  console.log('\n✅ テスト完了');
})();
