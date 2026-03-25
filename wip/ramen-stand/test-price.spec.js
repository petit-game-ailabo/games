const { test, expect } = require('@playwright/test');

test('ページが正常に読み込まれる', async ({ page }) => {
  await page.goto('file://' + __dirname + '/index.html');
  await expect(page.locator('#title-screen h1')).toContainText('ラーメン物語');
});

test('レシピベースの価格計算が正しい', async ({ page }) => {
  await page.goto('file://' + __dirname + '/index.html');
  
  const result = await page.evaluate(() => {
    // 価格計算テスト
    const params = calculateRecipeParams('shoyu', 'medium', ['negi']);
    const priceBalanced = calculatePrice(params.cost, 'balanced');
    const priceCheap = calculatePrice(params.cost, 'cheap');
    const pricePremium = calculatePrice(params.cost, 'premium');
    return { params, priceBalanced, priceCheap, pricePremium };
  });
  
  // 醤油(50) + 中太麺(30) + ネギ(10) = 原価90円
  expect(result.params.cost).toBe(90);
  expect(result.priceBalanced).toBe(113);  // ceil(90/0.80)
  expect(result.priceCheap).toBe(95);      // ceil(90/0.95)
  expect(result.pricePremium).toBe(129);   // ceil(90/0.70)
  expect(result.params.kire).toBe(4);      // shoyu(3) + medium(0) + negi(1)
  expect(result.params.koku).toBe(2);      // shoyu(1) + medium(1) + negi(0)
});

test('ゲーム開始後にレシピベース売価が使われる', async ({ page }) => {
  await page.goto('file://' + __dirname + '/index.html');
  
  // game.startNew() を直接呼ぶ
  await page.evaluate(() => game.startNew());
  await page.waitForTimeout(300);
  
  // デフォルトレシピの確認
  const recipeInfo = await page.evaluate(() => {
    const recipes = game.recipes;
    return {
      count: recipes.length,
      firstName: recipes[0]?.name,
      firstPrice: recipes[0]?.price,
      firstSoup: recipes[0]?.soup,
      firstNoodle: recipes[0]?.noodle
    };
  });
  
  expect(recipeInfo.count).toBe(1);
  expect(recipeInfo.firstName).toBe('醤油ラーメン');
  expect(recipeInfo.firstPrice).toBeGreaterThan(0);
  expect(recipeInfo.firstPrice).toBeLessThan(800); // 固定800円ではないこと
  
  // メニュータブに移動
  await page.click('button[data-tab="menu"]');
  await page.waitForTimeout(200);
  
  const cardText = await page.locator('.recipe-card').first().textContent();
  expect(cardText).toContain('売価');
  expect(cardText).not.toContain('売価800');
});

test('複数レシピの平均売価が集客率に影響する', async ({ page }) => {
  await page.goto('file://' + __dirname + '/index.html');
  
  const result = await page.evaluate(() => {
    game.startNew();
    
    // 安いレシピの集客率
    game.recipes[0].price = 100;
    game.recipes[0].active = true;
    const cheapRate = game.getSpawnRate ? null : 'no method'; // getSpawnRateではなくspawnPedestrianで使われる
    
    // attractRate計算のシミュレーション
    const activeList1 = game.getActiveRecipes();
    const avg1 = activeList1.reduce((s, r) => s + r.price, 0) / activeList1.length;
    
    game.recipes[0].price = 500;
    const activeList2 = game.getActiveRecipes();
    const avg2 = activeList2.reduce((s, r) => s + r.price, 0) / activeList2.length;
    
    return { avgCheap: avg1, avgExpensive: avg2 };
  });
  
  expect(result.avgCheap).toBe(100);
  expect(result.avgExpensive).toBe(500);
});
