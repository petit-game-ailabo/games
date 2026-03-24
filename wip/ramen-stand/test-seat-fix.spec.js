const { test, expect } = require('@playwright/test');

test('座席バグ修正: テーブル・座敷にソロ客が座れる', async ({ page }) => {
  await page.goto('http://localhost:8080/index.html');
  await page.waitForTimeout(2000);

  const seatResult = await page.evaluate(() => {
    if (!game) return { error: 'game not found' };
    
    // テーブル席を手動追加してテスト
    const testTable = { type: 'table', x: 5, y: 5 };
    game.furniture.push(testTable);
    
    // getSeatPositions が正しくユニークな座標を返すか
    const positions = game.getSeatPositions(testTable);
    const uniqueCoords = new Set(positions.map(p => `${p.x},${p.y}`));
    
    // findEmptySeat でテーブル席が見つかるか
    const seat = game.findEmptySeat();
    
    // 片付け
    game.furniture.pop();
    
    return {
      positionCount: positions.length,
      uniqueCount: uniqueCoords.size,
      positions: positions,
      seatFound: seat !== null,
      seatCoords: seat ? { x: seat.x, y: seat.y } : null
    };
  });

  console.log('Seat test result:', JSON.stringify(seatResult, null, 2));
  
  // テーブル(2x2, seats=4)から4つのユニーク座標が返る
  expect(seatResult.positionCount).toBe(4);
  expect(seatResult.uniqueCount).toBe(4);
  expect(seatResult.seatFound).toBe(true);
});

test('座席バグ修正: グループ使用中のテーブルにソロ客は座れない', async ({ page }) => {
  await page.goto('http://localhost:8080/index.html');
  await page.waitForTimeout(2000);

  const result = await page.evaluate(() => {
    if (!game) return { error: 'game not found' };
    
    // テーブルを追加
    const testTable = { type: 'table', x: 5, y: 5 };
    game.furniture.push(testTable);
    
    // グループ客がテーブルを使用中をシミュレート
    game.customers.push({
      id: 'test-group',
      seat: { x: 5, y: 5, furniture: testTable, isGroupSeat: true },
      state: 'eating'
    });
    
    // findEmptySeatでこのテーブルの席が返らないことを確認
    const seat = game.findEmptySeat();
    const isTestTableSeat = seat && seat.furniture === testTable;
    
    // 片付け
    game.customers.pop();
    game.furniture.pop();
    
    return {
      seatFound: seat !== null,
      isTestTableSeat: isTestTableSeat
    };
  });

  console.log('Group seat test result:', JSON.stringify(result, null, 2));
  expect(result.isTestTableSeat).toBeFalsy();
});

test('座席バグ修正: ソロ客同士はテーブルで相席可能', async ({ page }) => {
  await page.goto('http://localhost:8080/index.html');
  await page.waitForTimeout(2000);

  const result = await page.evaluate(() => {
    if (!game) return { error: 'game not found' };
    
    // カウンター席を全部埋める
    const counters = game.furniture.filter(f => f.type === 'counter');
    counters.forEach((c, i) => {
      game.customers.push({
        id: `test-solo-${i}`,
        seat: { x: c.x, y: c.y, furniture: c, isGroupSeat: false },
        state: 'eating'
      });
    });
    
    // テーブルを追加
    const testTable = { type: 'table', x: 5, y: 5 };
    game.furniture.push(testTable);
    
    // ソロ客1人がテーブルに座る（isGroupSeat: false）
    game.customers.push({
      id: 'test-solo-table',
      seat: { x: 5, y: 5, furniture: testTable, isGroupSeat: false },
      state: 'eating'
    });
    
    // 別のソロ客もまだテーブルに座れるか
    const seat = game.findEmptySeat();
    const isTestTableSeat = seat && seat.furniture === testTable;
    
    // 片付け
    while (game.customers.length > 0) game.customers.pop();
    game.furniture.pop();
    
    return {
      seatFound: seat !== null,
      isTestTableSeat: isTestTableSeat,
      seatCoords: seat ? { x: seat.x, y: seat.y } : null
    };
  });

  console.log('Solo sharing test result:', JSON.stringify(result, null, 2));
  expect(result.isTestTableSeat).toBe(true);
  // 座標は既に占有中の(5,5)とは異なるはず
  expect(result.seatCoords).not.toEqual({ x: 5, y: 5 });
});
