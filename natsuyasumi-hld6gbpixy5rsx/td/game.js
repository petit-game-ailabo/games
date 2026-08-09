// なつやすみ 2D見下ろし・試作（td）
// 実写をやめ、**同じ絵柄で揃う 見下ろしドット**に切りかえた 最初の 一歩。
//  ・世界タイル … CC0 の Top Down Adventure Assets（assets/tileset-world.png・16x16・7列）
//  ・キャラ    … いまの 東方ドット（chars.js の CHARS_B64・16x16・上段に 8人／チルノ=2）
//    ※ 向き（4方向）は あきらめ、左右反転だけ（本人の判断）
'use strict';
const cv = document.getElementById('c'), g = cv.getContext('2d');
g.imageSmoothingEnabled = false;
const S = 3, T = 16, TS = T * S;            // 3倍・16pxタイル → 48px
const COLS = 7;                              // タイルセットの 横の枚数

// タイル。col,row から 通し番号（idx = row*7 + col）
const G = 0, G2 = 2, PATH = 22, WATER = 43, TREE = 44, PLANT = 73, FLOWER = 74, BUSH = 1;
const LEGEND = {
  '.': G, ',': G2, '=': PATH, '~': WATER, '#': TREE, '*': PLANT, 'f': FLOWER, 'T': BUSH,
};
const SOLID = new Set([WATER, TREE, BUSH]);   // 通れない タイル
// 20列 x 11行の 小さな 庭。@ は プレイヤーの はじめの 位置（床は 草）
const MAP = [
  '####################',
  '#..,...=.....,.....##',
  '#.,..*.=..T..,....~~#',
  '#....==@===.....~~~~#',
  '#..T..=...,..*...~~~#',
  '#.,...=......T.....##',
  '#....*=..,.......f..#',
  '#..,..=...T....f.f..#',
  '#.T...=......,......#',
  '#..,..=...*.....,..##',
  '####################',
];
const MH = MAP.length, MW = MAP[0].length;
const OX = Math.floor((cv.width - MW * TS) / 2);
const OY = Math.floor((cv.height - MH * TS) / 2);

function tileOf(ch) { return LEGEND[ch] !== undefined ? LEGEND[ch] : G; }
function cellAt(px, py) {                      // ワールド座標 → マスの 文字
  const c = Math.floor((px - OX) / TS), r = Math.floor((py - OY) / TS);
  if (c < 0 || r < 0 || c >= MW || r >= MH) return '#';   // 外は 壁
  return MAP[r][c];
}
function solidAt(px, py) { return SOLID.has(tileOf(cellAt(px, py))); }

// --- 画像
const tiles = new Image(); tiles.src = 'assets/tileset-world.png';
const chars = new Image(); chars.src = 'data:image/png;base64,' + CHARS_B64;
let ready = 0; tiles.onload = () => ready++; chars.onload = () => ready++;

// --- プレイヤーと 立ってる 仲間（足もと座標＝下中央）
const startC = 6, startR = 3;
const player = { x: OX + startC * TS + TS / 2, y: OY + startR * TS + TS, ci: 2, face: 1, bob: 0, moving: false };
const npcs = [
  { ci: 3, x: OX + 12 * TS + TS/2, y: OY + 2 * TS + TS },   // だいようせい
  { ci: 1, x: OX + 3 * TS + TS/2,  y: OY + 8 * TS + TS },   // まりさ
];

// --- 入力
const keys = {};
addEventListener('keydown', e => { if (['ArrowUp','ArrowDown','ArrowLeft','ArrowRight',' '].includes(e.key)) e.preventDefault(); keys[e.key.toLowerCase()] = true; keys[e.key] = true; });
addEventListener('keyup', e => { keys[e.key.toLowerCase()] = false; keys[e.key] = false; });

function drawTile(idx, dx, dy) {
  const sx = (idx % COLS) * T, sy = Math.floor(idx / COLS) * T;
  g.drawImage(tiles, sx, sy, T, T, dx, dy, TS, TS);
}
function drawShadow(x, y) {
  g.save(); g.fillStyle = 'rgba(10,20,8,0.28)';
  g.beginPath(); g.ellipse(x, y - 3, TS*0.28, TS*0.12, 0, 0, Math.PI*2); g.fill(); g.restore();
}
function drawChar(ci, x, y, face) {
  const sx = (ci % 8) * 16, sy = Math.floor(ci / 8) * 16;
  g.save(); g.translate(Math.round(x), Math.round(y));
  if (face < 0) g.scale(-1, 1);
  // 足もと(x,y)を そろえて、少し 大きめ(想定56px)に。ちょい ういてる感じの bob
  const h = TS + 8;
  g.drawImage(chars, sx, sy, 16, 16, -h/2, -h + 2, h, h);
  g.restore();
}

let last = performance.now();
function loop(now) {
  const dt = Math.min(0.05, (now - last) / 1000); last = now;
  if (ready < 2) { g.fillStyle = '#0d120b'; g.fillRect(0,0,cv.width,cv.height); requestAnimationFrame(loop); return; }

  // うごく（斜めも）。足もとで あたり判定、軸ごとに 止める（すべる）
  let ax = 0, ay = 0;
  if (keys['arrowleft'] || keys['a']) ax -= 1;
  if (keys['arrowright']|| keys['d']) ax += 1;
  if (keys['arrowup']   || keys['w']) ay -= 1;
  if (keys['arrowdown'] || keys['s']) ay += 1;
  if (ax || ay) { const m = Math.hypot(ax, ay); ax/=m; ay/=m; }
  const spd = 108 * dt;
  const nx = player.x + ax*spd, ny = player.y + ay*spd;
  if (ax && !solidAt(nx, player.y)) player.x = nx;
  if (ay && !solidAt(player.x, ny)) player.y = ny;
  if (ax > 0.1) player.face = 1; else if (ax < -0.1) player.face = -1;
  player.moving = !!(ax || ay);
  player.bob += dt * (player.moving ? 10 : 2);

  // えがく
  g.fillStyle = '#0d120b'; g.fillRect(0,0,cv.width,cv.height);
  for (let r = 0; r < MH; r++) for (let c = 0; c < MW; c++) {
    const ch = MAP[r][c];
    // 木・草花は 下地に 草を しいてから 上に のせる（透け対策）
    if (ch !== '.' && ch !== ',') drawTile(G, OX + c*TS, OY + r*TS);
    drawTile(tileOf(ch), OX + c*TS, OY + r*TS);
  }
  // y で ならべて 前後
  const ents = [...npcs, player].sort((a, b) => a.y - b.y);
  for (const e of ents) {
    drawShadow(e.x, e.y);
    const off = e === player && player.moving ? Math.abs(Math.sin(player.bob)) * 3 : 0;
    drawChar(e.ci, e.x, e.y - off, e.face || 1);
  }
  // そっと 場所名
  g.fillStyle = 'rgba(230,238,220,0.85)';
  g.font = '600 15px system-ui'; g.fillText('うらの にわ', OX + 6, OY + 20);

  requestAnimationFrame(loop);
}
requestAnimationFrame(loop);
