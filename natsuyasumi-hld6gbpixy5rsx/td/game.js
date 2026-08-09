// なつやすみ 2D見下ろし・試作（td）
// 実写をやめ、**同じ絵柄で揃う 見下ろしドット**へ。目標形は「8方向で動ける ほの暮しの庭」。
//  ・世界タイル … CC0 Top Down Adventure Assets（assets/tileset-world.png・16x16・7列）
//  ・キャラ    … いまの 東方ドット（chars.js・16x16・上段8人／チルノ=2）。向きは 左右反転のみ（当面）
//  ・カメラ    … プレイヤーを 追って スクロール（広い庭）。移動は 8方向。
'use strict';
const cv = document.getElementById('c'), g = cv.getContext('2d');
g.imageSmoothingEnabled = false;
const S = 3, T = 16, TS = T * S;             // 3倍・16pxタイル → 48px
const COLS = 7;                               // タイルセットの 横の枚数
const VW = cv.width, VH = cv.height;

// タイル idx = row*7 + col
const G = 0, G2 = 2, PATH = 22, WATER = 43, TREE = 44, PLANT = 73, FLOWER = 74, BUSH = 1;
const SOLID = new Set([WATER, TREE, BUSH]);

// --- 広い庭を つくる（決まった 乱数で 毎回おなじ）。ほの暮しの庭ふうに スクロールする 広さ
const MW = 44, MH = 34;
let seed = 20260809;
function rnd() { seed = (seed * 1103515245 + 12345) & 0x7fffffff; return seed / 0x7fffffff; }
const map = [];
for (let r = 0; r < MH; r++) {
  const row = [];
  for (let c = 0; c < MW; c++) row.push(rnd() < 0.16 ? G2 : G);   // 草（すこし ムラ）
  map.push(row);
}
function set(c, r, v) { if (c>=0&&r>=0&&c<MW&&r<MH) map[r][c] = v; }
function rect(c0, r0, w, h, v) { for (let r=r0;r<r0+h;r++) for (let c=c0;c<c0+w;c++) set(c,r,v); }
// まわりを 木で 囲う（2重）
for (let c=0;c<MW;c++){ set(c,0,TREE); set(c,1,TREE); set(c,MH-1,TREE); set(c,MH-2,TREE); }
for (let r=0;r<MH;r++){ set(0,r,TREE); set(1,r,TREE); set(MW-1,r,TREE); set(MW-2,r,TREE); }
// 池
rect(6, 6, 5, 4, WATER);
// 道（十字＋まわり道）
rect(3, 16, MW-6, 2, PATH);
rect(20, 3, 2, MH-6, PATH);
// はたけ（土。将来 ひまわり等を うえる 場所）
rect(28, 20, 8, 6, PATH);
// 木・花を すこし ちらす（道・水・畑の 上には 置かない）
for (let i=0;i<120;i++){
  const c = 2 + (rnd()*(MW-4)|0), r = 2 + (rnd()*(MH-4)|0);
  if (map[r][c] !== G && map[r][c] !== G2) continue;
  const k = rnd();
  map[r][c] = k < 0.45 ? TREE : (k < 0.6 ? BUSH : (k < 0.8 ? FLOWER : PLANT));
}

function solidAtCell(c, r) { if (c<0||r<0||c>=MW||r>=MH) return true; return SOLID.has(map[r][c]); }
function solidAt(px, py) { return solidAtCell(Math.floor(px/TS), Math.floor(py/TS)); }

// --- 画像
const tiles = new Image(); tiles.src = 'assets/tileset-world.png';
const chars = new Image(); chars.src = 'data:image/png;base64,' + CHARS_B64;
let ready = 0; tiles.onload = () => ready++; chars.onload = () => ready++;

// --- プレイヤー（足もと＝下中央）と 立ってる 仲間。道の 交点あたりから
const player = { x: 21 * TS, y: 17 * TS, ci: 2, face: 1, bob: 0, moving: false };
const npcs = [
  { ci: 3, x: 24 * TS, y: 12 * TS },   // だいようせい
  { ci: 1, x: 14 * TS, y: 22 * TS },   // まりさ
  { ci: 5, x: 31 * TS, y: 23 * TS },   // リグル（はたけの そば）
];
const cam = { x: 0, y: 0 };

// --- 入力
const keys = {};
addEventListener('keydown', e => { if (e.key.startsWith('Arrow')||e.key===' ') e.preventDefault(); keys[e.key.toLowerCase()] = true; });
addEventListener('keyup',   e => { keys[e.key.toLowerCase()] = false; });

function drawTile(idx, dx, dy) {
  const sx = (idx % COLS) * T, sy = Math.floor(idx / COLS) * T;
  g.drawImage(tiles, sx, sy, T, T, dx, dy, TS, TS);
}
function drawShadow(x, y) {
  g.save(); g.fillStyle = 'rgba(10,20,8,0.28)';
  g.beginPath(); g.ellipse(x, y - 3, TS*0.28, TS*0.12, 0, 0, Math.PI*2); g.fill(); g.restore();
}
function drawChar(ci, x, y, face) {
  const sx = (ci % 8) * 16, sy = Math.floor(ci / 8) * 16, h = TS + 8;
  g.save(); g.translate(Math.round(x), Math.round(y));
  if (face < 0) g.scale(-1, 1);
  g.drawImage(chars, sx, sy, 16, 16, -h/2, -h + 2, h, h);
  g.restore();
}

let last = performance.now();
function loop(now) {
  const dt = Math.min(0.05, (now - last) / 1000); last = now;
  if (ready < 2) { g.fillStyle = '#0d120b'; g.fillRect(0,0,VW,VH); requestAnimationFrame(loop); return; }

  // うごく（8方向）。足もとで あたり判定、軸ごとに 止める
  let ax = 0, ay = 0;
  if (keys['arrowleft']||keys['a']) ax -= 1;
  if (keys['arrowright']||keys['d']) ax += 1;
  if (keys['arrowup']||keys['w']) ay -= 1;
  if (keys['arrowdown']||keys['s']) ay += 1;
  if (ax || ay) { const m = Math.hypot(ax, ay); ax/=m; ay/=m; }
  const spd = 175 * dt;                         // ← 速度アップ（108→175）
  const nx = player.x + ax*spd, ny = player.y + ay*spd;
  if (ax && !solidAt(nx, player.y)) player.x = nx;
  if (ay && !solidAt(player.x, ny)) player.y = ny;
  if (ax > 0.1) player.face = 1; else if (ax < -0.1) player.face = -1;
  player.moving = !!(ax || ay);
  player.bob += dt * (player.moving ? 11 : 2);

  // カメラ：プレイヤーを 中央に。はしは 地図の 外を 見せない
  cam.x = clamp(Math.round(player.x - VW/2), 0, MW*TS - VW);
  cam.y = clamp(Math.round(player.y - VH/2), 0, MH*TS - VH);

  // えがく（見えている ぶんだけ）
  g.fillStyle = '#0d120b'; g.fillRect(0,0,VW,VH);
  const c0 = Math.floor(cam.x/TS), r0 = Math.floor(cam.y/TS);
  for (let r = r0; r <= r0 + VH/TS + 1 && r < MH; r++) {
    for (let c = c0; c <= c0 + VW/TS + 1 && c < MW; c++) {
      if (r < 0 || c < 0) continue;
      const t = map[r][c], dx = c*TS - cam.x, dy = r*TS - cam.y;
      if (t !== G && t !== G2) drawTile(G, dx, dy);   // 透ける絵は 下に 草
      drawTile(t, dx, dy);
    }
  }
  // y で ならべて 前後（カメラぶん ずらして えがく）
  const ents = [...npcs, player].sort((a,b) => a.y - b.y);
  for (const e of ents) {
    const ex = e.x - cam.x, ey = e.y - cam.y;
    drawShadow(ex, ey);
    const off = e === player && player.moving ? Math.abs(Math.sin(player.bob)) * 3 : 0;
    drawChar(e.ci, ex, ey - off, e.face || 1);
  }
  g.fillStyle = 'rgba(230,238,220,0.9)'; g.font = '600 15px system-ui';
  g.fillText('うらの にわ', 14, 26);

  requestAnimationFrame(loop);
}
function clamp(v,a,b){ return v<a?a:(v>b?b:v); }
requestAnimationFrame(loop);
