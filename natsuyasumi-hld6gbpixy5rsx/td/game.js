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
// はたけ（土。ひまわりを うえる 場所）
const FIELD = { c: 28, r: 20, w: 8, h: 6 };
rect(FIELD.c, FIELD.r, FIELD.w, FIELD.h, PATH);
function inField(c, r) { return c>=FIELD.c && c<FIELD.c+FIELD.w && r>=FIELD.r && r<FIELD.r+FIELD.h; }
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

// 名まえ（ci → 表示名）。data/cast.json と 同じ ならび
const NAMES = { 0:'れいむ', 1:'まりさ', 2:'チルノ', 3:'だいようせい', 4:'ルーミア', 5:'リグル', 6:'ミスティア', 7:'けーね' };
// 話し手ID → 表示名（会話の [who, ことば] の who）
const WHO = { cirno:'チルノ', dai:'だいようせい', marisa:'まりさ', rumia:'ルーミア', wriggle:'リグル', mystia:'ミスティア', keine:'けーね', reimu:'れいむ' };

// --- プレイヤー（足もと＝下中央）と 立ってる 仲間。道の 交点あたりから。
// 仲間は そばで キーを 押すと 話す（P1と 同じ：近づいただけでは 始めない）。あたたかいトーン
const player = { x: 21 * TS, y: 17 * TS, ci: 2, face: 1, bob: 0, moving: false };
const npcs = [
  { ci: 3, x: 24 * TS, y: 12 * TS, lines: [   // だいようせい
      ['dai','チルノちゃん、この にわ ひろいね'],
      ['cirno','ぜんぶ あたいの ばしょ！'],
      ['dai','じゃあ てつだうよ。なにを する？'],
      ['cirno','うーん、まだ きめてない。それが たのしい'] ] },
  { ci: 1, x: 14 * TS, y: 22 * TS, lines: [   // まりさ
      ['marisa','よお。きょうは いい てんきだ'],
      ['cirno','おさんぽ びより！'],
      ['marisa','はたけ、なんか うえるのか？'],
      ['cirno','ひまわり、うえたいな'] ] },
  { ci: 5, x: 31 * TS, y: 23 * TS, lines: [   // リグル（はたけの そば）
      ['wriggle','このあたり、むしが おおいぞ'],
      ['cirno','つかまえて いい？'],
      ['wriggle','あみが あればな。よるは かぶとも でる'],
      ['cirno','よる……ちょっと どきどき する'] ] },
];
const cam = { x: 0, y: 0 };
const TALK_R = TS * 1.4;                 // これより 近ければ 話しかけられる
let talkNpc = null, talkIdx = 0;         // いま 話している 相手と 何行目か

// --- 昼夜（ほの暮しの庭：昼は ほっこり／夜は すこし 不安）。時間で 空気の色が かわる
// tod = 時刻(0〜24)。1日 = DAYSEC秒 で ひとまわり。朝8時から はじまる
const DAYSEC = 240;
let tod = 8;
// ひかりの色（かける＝multiply。255で そのまま・小さいほど 暗い/色づく）
const LIGHT = [
  { t: 0,  c: [58, 68, 120] },   // まよなか（ふかい あお・くらい）
  { t: 5,  c: [70, 78, 122] },   // よあけ前
  { t: 6.5,c: [205, 150, 138] }, // よあけ（あかね）
  { t: 8,  c: [255, 250, 240] }, // あさ
  { t: 12, c: [255, 255, 255] }, // まひる
  { t: 16, c: [255, 244, 224] }, // ひるさがり
  { t: 18, c: [255, 168, 108] }, // ゆうやけ
  { t: 19.5,c:[150, 110, 145] }, // たそがれ
  { t: 21, c: [70, 80, 132] },   // よる
  { t: 24, c: [58, 68, 120] },   // → まよなかへ つなぐ
];
function ambient(h) {
  for (let i = 0; i < LIGHT.length - 1; i++) {
    const a = LIGHT[i], b = LIGHT[i + 1];
    if (h >= a.t && h <= b.t) {
      const k = (h - a.t) / (b.t - a.t);
      return [0,1,2].map(j => Math.round(a.c[j] + (b.c[j] - a.c[j]) * k));
    }
  }
  return [255, 255, 255];
}
function todName(h) {
  if (h < 5) return 'まよなか'; if (h < 7) return 'よあけ'; if (h < 11) return 'あさ';
  if (h < 15) return 'ひる'; if (h < 17) return 'ひるさがり'; if (h < 19) return 'ゆうがた';
  if (h < 21) return 'よる'; return 'よる';
}
function isNight() { return tod >= 19 || tod < 4.5; }

// --- 蛍（よるだけ）。ひかりの点なので コードで きれいに 描ける＝絵柄が くずれない。
//   夜の 不安と 幻想の 両方。近づいて スペースで つかまえる（P1と 同じ：キーで）
const flies = [];               // {x,y,ph,vx,vy,life}  life:0→1 で ふわっと 出て 消える
const FLY_MAX = 16;
const FLY_R = TS * 0.95;        // これより 近ければ つかまえられる
let caughtHotaru = 0;
function spawnFly() {
  // プレイヤーの まわり（1画面ぶん）で 草の上に わく
  for (let tries = 0; tries < 12; tries++) {
    const c = Math.floor(player.x/TS) + ((rnd()*20|0) - 10);
    const r = Math.floor(player.y/TS) + ((rnd()*14|0) - 7);
    if (c<1||r<1||c>=MW-1||r>=MH-1) continue;
    const t = map[r][c];
    if (t === G || t === G2 || t === WATER || t === FLOWER || t === PLANT) {
      flies.push({ x: c*TS + TS/2, y: r*TS + TS/2, ph: rnd()*6.28, vx: 0, vy: 0, life: 0 });
      return;
    }
  }
}

// --- こよみ（ぼくなつの 魂：夏休みは すぎてゆく）
let day = 1;                       // なつやすみ 何日目
const SUMMER_DAYS = 31;
let garden = [];                  // はたけ（後の 章で うめる）
let dayMsg = '', dayMsgT = 0;     // 「◯日目」の 短い しらせ
function nokori() { return Math.max(0, SUMMER_DAYS - day + 1); }
function newDay() {
  day++;
  growGarden();
  dayMsg = `${day}日目  —  なつやすみ のこり ${nokori()}日`; dayMsgT = 3.4;
  save();
}
// --- ねむる（Zキー）。まっくらに とけて つぎの朝へ
let sleepPhase = 0;              // 0=起きてる。2.0→0 へ。1.0で 朝に とぶ
function startSleep() { if (sleepPhase <= 0 && !talkNpc) sleepPhase = 2.0; }
// --- セーブ／ロード（この夏が つづいてる 感じ）
function save() {
  try { localStorage.setItem('natsuyasumi_td',
    JSON.stringify({ day, tod, caughtHotaru, garden, px: player.x, py: player.y })); } catch (e) {}
}
function load() {
  try {
    const s = JSON.parse(localStorage.getItem('natsuyasumi_td') || 'null');
    if (!s) return;
    day = s.day || 1; tod = s.tod == null ? 8 : s.tod; caughtHotaru = s.caughtHotaru || 0;
    if (Array.isArray(s.garden)) garden = s.garden;
    if (s.px != null) { player.x = s.px; player.y = s.py; }
  } catch (e) {}
}
// はたけの成長：まえの日に みずを あげた 苗が ひと段階 のびる（0種→1芽→2葉→3つぼみ→4さいた）
function growGarden() {
  for (const p of garden) {
    if (p.watered && p.stage < 4) p.stage++;
    p.watered = false;             // あたらしい日：また みずやりが いる
  }
}
function plotAt(c, r) { return garden.find(p => p.c === c && p.r === r); }

// --- 入力。act は 決定（スペース／エンター）。おしっぱなしでは 進まない（1回ぶん）
const keys = {};
let act = false;
addEventListener('keydown', e => {
  if (e.key.startsWith('Arrow')||e.key===' ') e.preventDefault();
  if (!e.repeat && (e.key===' '||e.key==='Enter')) act = true;
  if (!e.repeat && (e.key==='z'||e.key==='Z')) startSleep();
  keys[e.key.toLowerCase()] = true;
});
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

  // ねむり中：時間は 止め、まっくらの まん中で つぎの朝へ とぶ
  if (sleepPhase > 0) {
    const before = sleepPhase; sleepPhase -= dt;
    if (before > 1.0 && sleepPhase <= 1.0) { tod = 7; newDay(); }
    if (sleepPhase < 0) sleepPhase = 0;         // 0を またがず ぴったり 起きる
  } else {
    const prev = tod;
    tod = (tod + dt * 24 / DAYSEC) % 24;         // 時刻を すすめる（1日＝DAYSEC秒）
    if (tod < prev) newDay();                     // よなかを またいだ＝つぎの日
  }
  if (dayMsgT > 0) dayMsgT -= dt;

  // うごく（8方向）。足もとで あたり判定、軸ごとに 止める。**話している あいだは 足を とめる**
  let ax = 0, ay = 0;
  if (!talkNpc && !sleepPhase) {
    if (keys['arrowleft']||keys['a']) ax -= 1;
    if (keys['arrowright']||keys['d']) ax += 1;
    if (keys['arrowup']||keys['w']) ay -= 1;
    if (keys['arrowdown']||keys['s']) ay += 1;
  }
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

  // 蛍：よるだけ ふわふわ わく。昼は しずかに 消える
  if (isNight() && flies.length < FLY_MAX && rnd() < 0.06) spawnFly();
  let nearFly = null, flyD = FLY_R;
  for (let i = flies.length - 1; i >= 0; i--) {
    const f = flies[i];
    f.ph += dt * 1.6;
    // ゆらゆら ただよう（ゆっくり）。ときどき むきを かえる
    if (rnd() < 0.03) { f.vx = (rnd()-0.5)*18; f.vy = (rnd()-0.5)*18; }
    f.x += f.vx * dt; f.y += f.vy * dt;
    f.life += dt * (isNight() ? 0.8 : -1.2);          // 夜は 出て・昼は 引っこむ
    if (f.life <= 0 && !isNight()) { flies.splice(i, 1); continue; }
    f.life = clamp(f.life, 0, 1);
    const d = Math.hypot(f.x - player.x, f.y - player.y);
    if (d < flyD) { flyD = d; nearFly = f; }
  }
  // そばの 仲間（話しかけ用）。いちばん 近いのを ひとり
  let near = null, bestD = TALK_R;
  for (const n of npcs) { const d = Math.hypot(n.x - player.x, n.y - player.y); if (d < bestD) { bestD = d; near = n; } }
  // 足もとの はたけ（うえる／みずやり）
  const pc = Math.floor(player.x/TS), pr = Math.floor(player.y/TS);
  const onField = inField(pc, pr);
  const fieldPlot = onField ? plotAt(pc, pr) : null;
  // キーで 会話／うえる・みずやり／蛍つかまえ（近づいただけでは 始めない・P1）
  if (act) {
    if (talkNpc) { if (++talkIdx >= talkNpc.lines.length) { talkNpc = null; talkIdx = 0; } }
    else if (near) { talkNpc = near; talkIdx = 0; }
    else if (onField) {
      if (!fieldPlot) { garden.push({ c: pc, r: pr, stage: 0, watered: false }); save(); }
      else if (!fieldPlot.watered) { fieldPlot.watered = true; save(); }
    }
    else if (nearFly) { flies.splice(flies.indexOf(nearFly), 1); caughtHotaru++; nearFly = null; save(); }
    act = false;
  }

  // えがく（見えている ぶんだけ）
  g.fillStyle = '#0d120b'; g.fillRect(0,0,VW,VH);
  const c0 = Math.floor(cam.x/TS), r0 = Math.floor(cam.y/TS);
  for (let r = r0; r <= r0 + VH/TS + 1 && r < MH; r++) {
    for (let c = c0; c <= c0 + VW/TS + 1 && c < MW; c++) {
      if (r < 0 || c < 0) continue;
      const t = map[r][c], dx = c*TS - cam.x, dy = r*TS - cam.y;
      if (t !== G && t !== G2) drawTile(G, dx, dy);   // 透ける絵は 下に 草
      drawTile(t, dx, dy);
      if (inField(c, r)) drawFurrow(dx, dy);          // 畑は うねを ひく
    }
  }
  // y で ならべて 前後（キャラ＋ひまわり を 足もとで ソート）
  const plants = garden.map(p => ({ x: p.c*TS + TS/2, y: p.r*TS + TS, plant: p }));
  const ents = [...npcs, player, ...plants].sort((a,b) => a.y - b.y);
  for (const e of ents) {
    const ex = e.x - cam.x, ey = e.y - cam.y;
    if (e.plant) { drawPlant(e.plant.stage, ex, ey, e.plant.watered); continue; }
    drawShadow(ex, ey);
    const off = e === player && player.moving ? Math.abs(Math.sin(player.bob)) * 3 : 0;
    drawChar(e.ci, ex, ey - off, e.face || 1);
  }
  // 昼夜：ひかりの色を かける（キャラ・地面ぜんぶ 染める）。UIより 下、世界より 上
  const [lr, lg, lb] = ambient(tod);
  if (lr < 255 || lg < 255 || lb < 255) {
    g.save(); g.globalCompositeOperation = 'multiply';
    g.fillStyle = `rgb(${lr},${lg},${lb})`; g.fillRect(0, 0, VW, VH);
    g.restore();
  }
  // 夜は すみを すこし くらく（不安げな 気配。まわりが 見えにくい）
  const dark = tod >= 19 || tod < 5 ? 0.34 : (tod >= 18 || tod < 6 ? 0.16 : 0);
  if (dark > 0) {
    const vg = g.createRadialGradient(VW/2, VH/2, VH*0.28, VW/2, VH/2, VH*0.78);
    vg.addColorStop(0, 'rgba(4,6,16,0)'); vg.addColorStop(1, `rgba(4,6,16,${dark})`);
    g.fillStyle = vg; g.fillRect(0, 0, VW, VH);
  }
  // 蛍の あかり（くらさの上で 光る）。lighter で ふわっと 加算
  if (flies.length) {
    g.save(); g.globalCompositeOperation = 'lighter';
    for (const f of flies) {
      const fx = f.x - cam.x, fy = f.y - cam.y;
      if (fx < -20 || fy < -20 || fx > VW+20 || fy > VH+20) continue;
      const pulse = 0.55 + 0.45 * Math.sin(f.ph);
      const a = f.life * pulse;
      const rad = TS * (0.5 + 0.25 * pulse);
      const gr = g.createRadialGradient(fx, fy, 0, fx, fy, rad);
      gr.addColorStop(0, `rgba(220,255,150,${0.9*a})`);
      gr.addColorStop(0.4, `rgba(150,230,90,${0.45*a})`);
      gr.addColorStop(1, 'rgba(120,200,70,0)');
      g.fillStyle = gr; g.beginPath(); g.arc(fx, fy, rad, 0, 6.284); g.fill();
      g.fillStyle = `rgba(245,255,210,${a})`; g.beginPath(); g.arc(fx, fy, 1.6, 0, 6.284); g.fill();
    }
    g.restore();
  }

  // --- HUD（ひかりの上。いつも 読める）
  g.fillStyle = 'rgba(230,238,220,0.9)'; g.font = '600 15px system-ui';
  g.fillText('うらの にわ', 14, 26);
  g.fillStyle = 'rgba(230,238,220,0.5)'; g.font = '12px system-ui';
  g.fillText('Zで ねる', 14, 44);
  // とけい（右上）：時刻と じかんたい
  const hh = Math.floor(tod), mm = Math.floor((tod % 1) * 60);
  const clk = `${hh}:${String(mm).padStart(2,'0')}  ${todName(tod)}`;
  g.font = '600 15px system-ui'; g.textAlign = 'right';
  g.fillStyle = 'rgba(8,12,9,0.45)';
  const cw = g.measureText(clk).width; g.fillRect(VW - cw - 26, 10, cw + 16, 24);
  g.fillStyle = 'rgba(246,250,242,0.95)'; g.fillText(clk, VW - 14, 27); g.textAlign = 'left';
  // こよみ：なつやすみ のこり N日（時計の下）
  g.font = '600 13px system-ui'; g.textAlign = 'right';
  g.fillStyle = 'rgba(255,236,190,0.92)';
  g.fillText(`${day}日目 ・ のこり ${nokori()}日`, VW - 14, 47); g.textAlign = 'left';
  // つかまえた 蛍の かず（夜／持っていれば）
  if (caughtHotaru > 0 || isNight()) {
    g.font = '600 13px system-ui'; g.textAlign = 'right';
    g.fillStyle = 'rgba(220,255,150,0.9)';
    g.fillText(`ほたる ${caughtHotaru}`, VW - 14, 66); g.textAlign = 'left';
  }

  // 会話の まど／足もとの したこと（はなす・うえる・みずやり・つかまえる）
  if (talkNpc) drawSay(talkNpc.lines[talkIdx]);
  else {
    let lbl = null;
    if (near) lbl = '▶ はなす';
    else if (onField) lbl = !fieldPlot ? '▶ うえる' : (!fieldPlot.watered ? '▶ みずやり' : (fieldPlot.stage >= 4 ? 'さいた！' : 'すくすく…'));
    else if (nearFly) lbl = '▶ つかまえる';
    if (lbl) {
      g.fillStyle = 'rgba(8,12,9,0.5)'; g.fillRect(0, VH-40, VW, 40);
      g.fillStyle = 'rgba(246,250,242,0.95)'; g.font = '600 17px system-ui'; g.textAlign = 'center';
      g.fillText(lbl, VW/2, VH-15); g.textAlign = 'left';
    }
  }
  // 「◯日目」の しらせ（すこし 出て 消える）
  if (dayMsgT > 0) {
    const a = Math.min(1, dayMsgT) * Math.min(1, (3.4 - dayMsgT) * 3);
    g.save(); g.globalAlpha = Math.max(0, a);
    g.fillStyle = 'rgba(8,10,20,0.7)'; g.fillRect(0, VH/2 - 34, VW, 68);
    g.fillStyle = '#ffe6a8'; g.font = '600 26px system-ui'; g.textAlign = 'center';
    g.fillText(dayMsg, VW/2, VH/2 + 9); g.textAlign = 'left'; g.restore();
  }
  // ねむり：まっくらに とけて つぎの朝へ
  if (sleepPhase > 0) {
    const a = 1 - Math.abs(sleepPhase - 1.0);   // 1.0で まっくら
    g.fillStyle = `rgba(0,0,0,${a})`; g.fillRect(0, 0, VW, VH);
    if (a > 0.6) {
      g.fillStyle = `rgba(230,238,250,${(a-0.6)/0.4*0.8})`;
      g.font = '600 20px system-ui'; g.textAlign = 'center';
      g.fillText('…zzz', VW/2, VH/2); g.textAlign = 'left';
    }
  }
  act = false;                         // 1フレームで つかいきる
  requestAnimationFrame(loop);
}
function drawSay(line) {
  const bx = 70, by = VH - 118, bw = VW - 140, bh = 92, r = 14;
  g.save();
  g.fillStyle = 'rgba(10,14,26,0.82)';
  g.beginPath(); g.moveTo(bx+r,by); g.arcTo(bx+bw,by,bx+bw,by+bh,r); g.arcTo(bx+bw,by+bh,bx,by+bh,r);
  g.arcTo(bx,by+bh,bx,by,r); g.arcTo(bx,by,bx+bw,by,r); g.fill();
  g.strokeStyle = 'rgba(180,200,230,0.28)'; g.lineWidth = 1; g.stroke();
  g.fillStyle = '#ffe6a8'; g.font = '600 18px system-ui'; g.fillText(WHO[line[0]] || line[0], bx+24, by+30);
  g.fillStyle = '#eef3ff'; g.font = '20px system-ui'; g.fillText(line[1], bx+24, by+62);
  g.fillStyle = 'rgba(230,238,250,0.5)'; g.font = '13px system-ui';
  g.fillText('スペースで つぎへ', bx+bw-150, by+bh-12);
  g.restore();
}
// 畑の うね（土に ほそい 線）
function drawFurrow(dx, dy) {
  g.save(); g.strokeStyle = 'rgba(74,48,26,0.35)'; g.lineWidth = 2;
  for (let i = 1; i <= 2; i++) { const yy = dy + TS*i/3; g.beginPath(); g.moveTo(dx+4, yy); g.lineTo(dx+TS-4, yy); g.stroke(); }
  g.restore();
}
// ひまわり（コードで えがく＝絵柄が くずれない）。(x,y)＝足もと。0種→1芽→2葉→3つぼみ→4さいた
function drawPlant(stage, x, y, watered) {
  g.save();
  g.fillStyle = 'rgba(10,20,8,0.22)'; g.beginPath(); g.ellipse(x, y-2, 8, 3, 0, 0, 6.283); g.fill();
  if (stage === 0) {                                  // 種（つち の もり）
    g.fillStyle = watered ? '#553720' : '#6b4a2a';
    g.beginPath(); g.ellipse(x, y-3, 6, 4, 0, 0, 6.283); g.fill();
    g.restore(); return;
  }
  const H = [0, 12, 24, 32, 38][stage];
  g.strokeStyle = '#3f7a2e'; g.lineWidth = 3; g.lineCap = 'round';
  g.beginPath(); g.moveTo(x, y-2); g.lineTo(x, y-H); g.stroke();
  if (stage === 1) {                                  // 双葉
    g.fillStyle = '#6cbf4a';
    g.beginPath(); g.ellipse(x-3, y-H+2, 4, 2.5, -0.6, 0, 6.283); g.fill();
    g.beginPath(); g.ellipse(x+3, y-H+2, 4, 2.5,  0.6, 0, 6.283); g.fill();
  } else if (stage >= 2) {                             // 葉
    g.fillStyle = '#5aa83e';
    g.beginPath(); g.ellipse(x-6, y-H*0.5,  7, 3.5, -0.5, 0, 6.283); g.fill();
    g.beginPath(); g.ellipse(x+6, y-H*0.62, 7, 3.5,  0.5, 0, 6.283); g.fill();
  }
  if (stage === 3) {                                  // つぼみ
    g.fillStyle = '#4f9a37'; g.beginPath(); g.arc(x, y-H, 6, 0, 6.283); g.fill();
    g.fillStyle = '#f0b429'; g.beginPath(); g.arc(x, y-H, 2.5, 0, 6.283); g.fill();
  }
  if (stage === 4) {                                  // ひまわり さいた
    const cx = x, cy = y-H, R = 11;
    g.fillStyle = '#f7c948';
    for (let i = 0; i < 12; i++) { const a = i/12*6.283; g.beginPath(); g.ellipse(cx+Math.cos(a)*R, cy+Math.sin(a)*R, 5, 3, a, 0, 6.283); g.fill(); }
    g.fillStyle = '#7a4a1e'; g.beginPath(); g.arc(cx, cy, 7, 0, 6.283); g.fill();
    g.fillStyle = '#5c3413';
    for (let i = 0; i < 6; i++) { const a = i/6*6.283; g.beginPath(); g.arc(cx+Math.cos(a)*3, cy+Math.sin(a)*3, 1.2, 0, 6.283); g.fill(); }
  }
  g.restore();
}
function clamp(v,a,b){ return v<a?a:(v>b?b:v); }
load();                               // つづきの 夏から
addEventListener('beforeunload', save);
requestAnimationFrame(loop);
