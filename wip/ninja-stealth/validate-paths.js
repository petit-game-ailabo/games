const fs = require('fs');

const html = fs.readFileSync('index.html', 'utf8');

// stages配列を抽出
const stagesMatch = html.match(/const stages = \[([\s\S]+?)\n\];/);
if (!stagesMatch) {
  console.error('stages配列が見つかりません');
  process.exit(1);
}

// DIR定数を定義
const DIR = { DOWN: 0, LEFT: 1, RIGHT: 2, UP: 3 };

// stagesを評価
let stages;
eval('stages = [' + stagesMatch[1] + '\n];');

let errorCount = 0;

stages.forEach((stage, i) => {
  console.log(`\n=== Stage ${i+1} (size: ${stage.size}) ===`);
  
  stage.guards.forEach((g, j) => {
    if (g.type === 'patrol' && g.path) {
      console.log(`  Guard ${j+1} (patrol): ${g.path.length} points`);
      
      g.path.forEach((p, k) => {
        if (p.y >= stage.map.length || p.x >= stage.map[0].length) {
          console.error(`    ❌ Point ${k+1} (${p.x},${p.y}) is OUT OF BOUNDS!`);
          errorCount++;
        } else if (stage.map[p.y][p.x] === 1) {
          console.error(`    ❌ Point ${k+1} (${p.x},${p.y}) is WALL!`);
          errorCount++;
        } else {
          // 隣接チェック（次のポイントとの距離）
          const nextIdx = (k + 1) % g.path.length;
          const next = g.path[nextIdx];
          const dist = Math.abs(next.x - p.x) + Math.abs(next.y - p.y);
          if (dist !== 1) {
            console.error(`    ⚠️  Point ${k+1} (${p.x},${p.y}) → Point ${nextIdx+1} (${next.x},${next.y}): distance=${dist} (not adjacent!)`);
            errorCount++;
          }
        }
      });
      
      if (errorCount === 0) {
        console.log('    ✓ All points valid!');
      }
    } else if (g.type === 'camera') {
      console.log(`  Guard ${j+1} (camera): (${g.x},${g.y})`);
      
      if (g.y >= stage.map.length || g.x >= stage.map[0].length) {
        console.error(`    ❌ Camera position is OUT OF BOUNDS!`);
        errorCount++;
      } else if (stage.map[g.y][g.x] === 1) {
        console.error(`    ❌ Camera is in WALL!`);
        errorCount++;
      } else {
        console.log('    ✓ Position valid!');
      }
    }
  });
});

console.log(`\n=== Total Errors: ${errorCount} ===`);
process.exit(errorCount > 0 ? 1 : 0);
