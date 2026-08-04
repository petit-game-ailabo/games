// 画面の中の あそび（P4）。別画面に とばず、**歩いている 世界の うえで** やる。
//   ・虫とり … 蝶が 画面を とんでいて、あみを 持って そばで キーを 押すと ふって とる
//   ・釣り  … 水べで 竿を ふると うきが 水に 入り、その場で 釣る（P4b・下の FISH）
// もとの「画面ぜんぶを のっとる ミニ」（mini.js）は これに 置きかわる。

// ===== 虫（蝶） =====
// 画面に `bugs:{ pool, max, n }` が あれば、昼のあいだ 蝶が n匹 とぶ。
// **その日その場所の 虫は 有限**（DESIGN §6）：pool の のこりが 0 に なったら もう とばない。
const FLY_KINDS = ['ちょう', 'とんぼ', 'こがね'];   // 野に とぶ 捕れる 虫
const FLY_COL = { 'ちょう':'#ffd54a', 'とんぼ':'#8fd0e0', 'こがね':'#8fd06a' };

let bugs = [], bugScreen = null;
let netT = -9;              // あみを ふった 時こく（見た目の ため）。elapsed 基準

function spawnBug() {
  return {
    kind: FLY_KINDS[Math.floor(Math.random() * FLY_KINDS.length)],
    gx: 140 + Math.random() * (W - 280),      // 地めんの うえの x
    gy: 300 + Math.random() * 120,            // 地めんの うえの y（とどく 帯）
    vx: (Math.random() < 0.5 ? -1 : 1) * (55 + Math.random() * 45),
    turn: 0.7 + Math.random() * 1.4,
    ph: Math.random() * 6.28,                 // 上下ゆれの 位相
    flee: 0,
  };
}
// その蝶が いま 画面の どこに 見えるか（地めんから ふわっと 浮いている）
function bugScreenPos(b) {
  const h = heightAt(b.gy);
  const fly = h * 0.42 + Math.sin(elapsed * 3.2 + b.ph) * 10 + 18;
  return { x: b.gx, y: b.gy - fly, h };
}
function bugsActive() {
  const sc = SC[cur];
  return !!(sc && sc.bugs && !WORLD.yoruDone && leftToday(sc.bugs.pool, sc.bugs.max) > 0);
}
function updateBugs(dt) {
  const sc = SC[cur];
  if (!sc || !sc.bugs || WORLD.yoruDone) { bugs = []; bugScreen = null; return; }
  if (bugScreen !== cur) { bugScreen = cur; bugs = []; }
  const left = leftToday(sc.bugs.pool, sc.bugs.max);
  const want = Math.min(sc.bugs.n || 3, left);
  while (bugs.length < want) bugs.push(spawnBug());
  if (bugs.length > want) bugs.length = want;   // もう いない ぶんは 消す
  for (const b of bugs) {
    // にげている あいだは 速く 遠ざかる
    b.turn -= dt;
    if (b.turn <= 0) { b.vx = -b.vx * (0.7 + Math.random() * 0.7); b.turn = 0.7 + Math.random() * 1.4; }
    const sp = b.flee > 0 ? 2.4 : 1;
    b.gx += b.vx * dt * sp;
    if (b.gx < 90)      { b.gx = 90;      b.vx = Math.abs(b.vx); }
    if (b.gx > W - 90)  { b.gx = W - 90;  b.vx = -Math.abs(b.vx); }
    b.gy += Math.sin(elapsed * 0.7 + b.ph) * 8 * dt;
    b.gy = clamp(b.gy, 288, 432);
    if (b.flee > 0) b.flee -= dt;
  }
}
function drawBugs() {
  for (const b of bugs) {
    const p = bugScreenPos(b);
    const flap = 5 + Math.abs(Math.sin(elapsed * 20 + b.ph)) * 5;
    const s = clamp(p.h / 120, 0.5, 1.2);          // 奥は 小さく
    ctx.save();
    ctx.translate(p.x, p.y);
    ctx.fillStyle = FLY_COL[b.kind] || '#ffe36b';
    ctx.beginPath(); ctx.ellipse(-flap*0.5*s, 0, flap*s, 5*s, 0, 0, Math.PI*2); ctx.fill();
    ctx.beginPath(); ctx.ellipse( flap*0.5*s, 0, flap*s, 5*s, 0, 0, Math.PI*2); ctx.fill();
    ctx.fillStyle = 'rgba(60,50,20,0.9)';
    ctx.fillRect(-1*s, -6*s, 2*s, 12*s);
    ctx.restore();
  }
}
// あみの 先の 画面いち（プレイヤーの すこし 前・上）
function netPoint() {
  const h = heightAt(player.y);
  return { x: player.x + (player.face || 1) * 20, y: player.y - h * 0.62, r: 74 };
}
// あみを ふる。とれたら true。虫が いなくても ふる しぐさは する
function swingNet() {
  netT = elapsed;
  const sc = SC[cur];
  const np = netPoint();
  let best = null, bestD = np.r;
  for (const b of bugs) {
    const p = bugScreenPos(b);
    const d = Math.hypot(p.x - np.x, p.y - np.y);
    if (d < bestD) { bestD = d; best = b; }
  }
  if (best) {
    useToday(sc.bugs.pool, sc.bugs.max);
    addNum('mushikago');
    setFlag('zukan:' + best.kind);
    bugs.splice(bugs.indexOf(best), 1);
    toast('つかまえた！　' + best.kind, 1.6);
    playSfx && SFX.tore && playSfx('tore');
    return true;
  }
  // 近くの 虫が 逃げる（当たらなかった とき）
  for (const b of bugs) {
    const p = bugScreenPos(b);
    if (Math.hypot(p.x - np.x, p.y - np.y) < np.r * 1.8) { b.flee = 0.9; b.vx = (p.x < np.x ? -1 : 1) * Math.abs(b.vx); }
  }
  return false;
}

// あみの 絵。ふった しゅんかん だけ、プレイヤーの 前で 弧を えがく（キャラの ポーズは 変えない）
function drawNet() {
  const held = holding('ami') && bugsActive();
  const sw = elapsed - netT;
  if (!held && !(sw >= 0 && sw < 0.4)) return;
  const np = netPoint();
  const swinging = sw >= 0 && sw < 0.4;
  const ang = swinging ? (-0.9 + sw / 0.4 * 1.8) : -0.2;   // ふると 弧を なぞる
  ctx.save();
  ctx.strokeStyle = swinging ? '#ffffff' : 'rgba(215,210,196,0.85)';
  ctx.lineWidth = 3;
  const hx = player.x, hy = player.y - heightAt(player.y) * 0.34;   // 手もと
  const nx = hx + Math.cos(ang) * 40 * (player.face || 1), ny = hy - 40 + Math.sin(ang) * 30;
  ctx.beginPath(); ctx.moveTo(hx, hy); ctx.lineTo(nx, ny); ctx.stroke();
  ctx.beginPath(); ctx.arc(nx, ny, 16, 0, Math.PI * 2); ctx.stroke();
  ctx.restore();
}

// ===== 蛍（D8）=====
// **晩ごはんの あと（夜）に あぜみちへ 行くと**、ほたるが ふわふわ とぶ。
// 捕まえる ものでは なく、ただ きれいな よるの 情景（P7 の あかるさ／わくわく の 側）。
let hotaru = [];
function hotaruOn() { return cur === 'aze' && WORLD.yoruDone; }
function spawnHotaru() {
  return {
    x: 90 + Math.random() * (W - 180),
    y: 260 + Math.random() * 180,
    a: Math.random() * 6.28, sp: 10 + Math.random() * 16,
    blink: Math.random() * 6.28, ph: Math.random() * 6.28,
  };
}
function updateHotaru(dt) {
  if (!hotaruOn()) { hotaru = []; return; }
  while (hotaru.length < 11) hotaru.push(spawnHotaru());
  for (const f of hotaru) {
    f.a += (Math.random() - 0.5) * dt * 2.4;
    f.x += Math.cos(f.a) * f.sp * dt;
    f.y += Math.sin(f.a) * f.sp * 0.5 * dt;
    f.blink += dt;
    if (f.x < 70) { f.x = 70; f.a = Math.PI - f.a; }
    if (f.x > W - 70) { f.x = W - 70; f.a = Math.PI - f.a; }
    if (f.y < 250) f.y = 250;
    if (f.y > 460) f.y = 460;
  }
}
function drawHotaru() {
  if (!hotaruOn()) return;
  for (const f of hotaru) {
    const g = 0.3 + 0.7 * Math.abs(Math.sin(f.blink * 2.1 + f.ph));
    ctx.save();
    ctx.globalCompositeOperation = 'lighter';
    ctx.fillStyle = 'rgba(150,230,120,' + (0.45 * g).toFixed(2) + ')';   // にじむ 光
    ctx.beginPath(); ctx.arc(f.x, f.y, 5 + 3 * g, 0, Math.PI * 2); ctx.fill();
    ctx.fillStyle = 'rgba(238,255,205,' + (0.9 * g).toFixed(2) + ')';     // しん
    ctx.beginPath(); ctx.arc(f.x, f.y, 1.8, 0, Math.PI * 2); ctx.fill();
    ctx.restore();
  }
}

// ===== 釣り（P4b）=====
// 水べで 竿を ふると、**その場で** うきが 水に 入る（別画面に とばない）。
//   うきを 見て、あたり（！）の あいだに キーを 押すと つれる。はやすぎ／おそすぎは にげる。
//   phase … machi（待ち）→ atari（あたり）→ owari（けっかを 見せる）
let fishing = null;
function startFishing() {
  const sp = (SC[cur].spot || []).find(s => s.fish) || { x: player.x, y: player.y };
  fishing = {
    phase: 'machi', t: 0,
    bite: 1.2 + Math.random() * 2.3,   // あたりが 来る 時こく
    win: 0.9,                          // おせる あいだ
    msg: 'うきを みてて…',
    // うきは 水べの spot の すこし 奥。竿の 先から のびる
    fx: sp.x + 8, fy: sp.y - 6,
    result: 0,
  };
}
function fishingTick(dt) {
  if (!fishing) return;
  const f = fishing; f.t += dt;
  if (f.phase === 'machi') {
    if (advance) { advance = false; f.phase = 'owari'; f.msg = 'はやすぎた。にげられた'; f.endT = f.t; f.result = 0; }
    else if (f.t >= f.bite) { f.phase = 'atari'; f.msg = 'きた！ いま！'; f.atariT = f.t; }
  } else if (f.phase === 'atari') {
    if (advance) { advance = false; f.phase = 'owari'; f.msg = 'つれた！'; f.endT = f.t; f.result = 1; }
    else if (f.t >= f.atariT + f.win) { f.phase = 'owari'; f.msg = 'にげられた…'; f.endT = f.t; f.result = 0; }
  } else {
    if (f.t >= f.endT + 1.2) {
      if (f.result) { addNum('tsuri'); setFlag('tsutta'); toast('さかなを つった！', 1.8); }
      else toast(f.msg, 1.4);
      fishing = null;
    }
  }
}
function drawFishing() {
  if (!fishing) return;
  const f = fishing, atari = f.phase === 'atari';
  // 竿：プレイヤーの 手もとから うきへ。糸も
  const hx = player.x + (player.face || 1) * 14, hy = player.y - heightAt(player.y) * 0.5;
  const by = f.fy + Math.sin(elapsed * 2.2) * 3 + (atari ? Math.sin(elapsed * 40) * 6 : 0);
  ctx.save();
  ctx.strokeStyle = 'rgba(90,70,50,0.9)'; ctx.lineWidth = 2;   // 竿
  ctx.beginPath(); ctx.moveTo(hx, hy); ctx.lineTo(hx + (player.face || 1) * 26, hy - 26); ctx.stroke();
  ctx.strokeStyle = 'rgba(230,235,235,0.5)'; ctx.lineWidth = 1; // 糸
  ctx.beginPath(); ctx.moveTo(hx + (player.face || 1) * 26, hy - 26); ctx.lineTo(f.fx, by); ctx.stroke();
  // うき
  ctx.fillStyle = '#c23a2f'; ctx.fillRect(f.fx - 2, by - 13, 4, 9);
  ctx.fillStyle = atari ? '#ff5a4d' : '#eceff0';
  ctx.beginPath(); ctx.arc(f.fx, by, 7, 0, Math.PI * 2); ctx.fill();
  // 波紋
  ctx.strokeStyle = 'rgba(180,210,220,0.35)'; ctx.lineWidth = 1.5;
  const rr = 8 + (elapsed * 18 % 22);
  ctx.beginPath(); ctx.ellipse(f.fx, by + 3, rr, rr * 0.4, 0, 0, Math.PI * 2); ctx.stroke();
  // ことば
  ctx.fillStyle = 'rgba(8,12,9,0.5)'; ctx.fillRect(0, 26, W, 40);
  text(f.msg, W / 2, 54, 22, atari ? '#ffe36b' : '#dcecef', 'center');
  if (atari) text('いま スペース！', W / 2, H - 60, 20, '#ffe36b', 'center');
  ctx.restore();
}

// ===== spot の 雑な 絵（P5）=====
// 写真の うえに **物が ある**と 分かるように、手で ざっくり かく。
//   ・光る 目じるしは 置かない（実写に 浮く。D-048／D-066）
//   ・地めんの 物（suika/saisen/…）は キャラより **後ろ**（layer:'ground'）、
//     ぶらさがる 物（風鈴/日めくり）は キャラより **前**（layer:'hang'）
// spot に `draw:'suika'` の ように 形の 名まえを つけると 出る。
const SPOT_LAYER = { suika:'ground', saisen:'ground', monohoshi:'ground', drawer:'ground',
                     fuurin:'hang', himekuri:'hang', ishi:'ground' };
function drawSpots(layer) {
  for (const sp of (SC[cur].spot || [])) {
    if (!sp.draw || SPOT_LAYER[sp.draw] !== layer) continue;
    // 地めんの物は 遠近で 大きさを 変える。ぶらさがる物（軒の 高い ところ）は
    // 画面の y が 小さく 出て しまい 極端に 小さく なるので、ほどよい 固定 大きさ
    const s = layer === 'hang' ? 1.15 : clamp(heightAt(sp.y) / 120, 0.6, 1.35);
    const f = SPOT_ART[sp.draw];
    if (f) f(sp.x, sp.y, s);
  }
}
function ellShadow(x, y, w) {
  ctx.fillStyle = 'rgba(0,0,0,0.22)';
  ctx.beginPath(); ctx.ellipse(x, y, w, w * 0.32, 0, 0, Math.PI * 2); ctx.fill();
}
const SPOT_ART = {
  // すいか。みどりの たまに こい しま
  suika: (x, y, s) => {
    const r = 26 * s;
    ellShadow(x, y + 2, r * 0.95);
    ctx.save();
    ctx.fillStyle = '#3f7d33';
    ctx.beginPath(); ctx.ellipse(x, y - r * 0.7, r, r * 0.82, 0, 0, Math.PI * 2); ctx.fill();
    ctx.strokeStyle = 'rgba(20,54,18,0.75)'; ctx.lineWidth = 2.4 * s;
    for (let i = -2; i <= 2; i++) {
      ctx.beginPath();
      ctx.ellipse(x + i * r * 0.34, y - r * 0.7, r * 0.16, r * 0.82, 0, 0, Math.PI * 2);
      ctx.stroke();
    }
    ctx.restore();
  },
  // 賽銭箱。木の はこ、上に すのこと お金の あな
  saisen: (x, y, s) => {
    const w = 46 * s, hh = 30 * s;
    ellShadow(x, y + 2, w * 0.6);
    ctx.save();
    ctx.fillStyle = '#6b4a2b';
    ctx.beginPath();
    ctx.moveTo(x - w / 2, y); ctx.lineTo(x + w / 2, y);
    ctx.lineTo(x + w / 2 - 4 * s, y - hh); ctx.lineTo(x - w / 2 + 4 * s, y - hh);
    ctx.closePath(); ctx.fill();
    ctx.fillStyle = '#5a3d22';                          // 上の すのこ面
    ctx.beginPath();
    ctx.moveTo(x - w / 2 + 4 * s, y - hh); ctx.lineTo(x + w / 2 - 4 * s, y - hh);
    ctx.lineTo(x + w / 2 - 10 * s, y - hh - 9 * s); ctx.lineTo(x - w / 2 + 10 * s, y - hh - 9 * s);
    ctx.closePath(); ctx.fill();
    ctx.strokeStyle = 'rgba(28,18,8,0.7)'; ctx.lineWidth = 1.4 * s;
    for (let i = 1; i < 5; i++) { const gx = x - w / 2 + 8 * s + i * (w - 16 * s) / 5;
      ctx.beginPath(); ctx.moveTo(gx, y - hh); ctx.lineTo(gx, y - 3 * s); ctx.stroke(); }
    ctx.fillStyle = '#20140a';                          // お金の あな
    ctx.fillRect(x - w * 0.24, y - hh - 5 * s, w * 0.48, 3 * s);
    ctx.restore();
  },
  // 物干しロープ。細い さおに せんたくばさみ だけ のこる
  monohoshi: (x, y, s) => {
    const w = 90 * s;
    ctx.save();
    ctx.strokeStyle = 'rgba(90,74,54,0.85)'; ctx.lineWidth = 3 * s;   // 支柱
    ctx.beginPath(); ctx.moveTo(x - w / 2, y); ctx.lineTo(x - w / 2, y - 54 * s); ctx.stroke();
    ctx.beginPath(); ctx.moveTo(x + w / 2, y); ctx.lineTo(x + w / 2, y - 54 * s); ctx.stroke();
    ctx.strokeStyle = 'rgba(230,230,225,0.6)'; ctx.lineWidth = 1.5 * s;  // ロープ（たわむ）
    ctx.beginPath(); ctx.moveTo(x - w / 2, y - 50 * s);
    ctx.quadraticCurveTo(x, y - 40 * s, x + w / 2, y - 50 * s); ctx.stroke();
    ctx.fillStyle = '#c9543f';                                         // せんたくばさみ
    for (const t of [-0.3, 0.15, 0.55]) {
      const px = x + t * w, py = y - 46 * s + Math.abs(t) * 6 * s;
      ctx.fillRect(px - 2 * s, py, 4 * s, 7 * s);
    }
    ctx.restore();
  },
  // 引き出し（スタンプカードの ある）。木の 小箱に カードが すこし のぞく
  drawer: (x, y, s) => {
    const w = 40 * s, hh = 26 * s;
    ellShadow(x, y + 2, w * 0.55);
    ctx.save();
    ctx.fillStyle = '#7a5730'; ctx.fillRect(x - w / 2, y - hh, w, hh);
    ctx.strokeStyle = 'rgba(30,20,10,0.6)'; ctx.lineWidth = 1.4 * s;
    ctx.strokeRect(x - w / 2, y - hh, w, hh);
    ctx.fillStyle = '#3a2814';                                          // とって
    ctx.fillRect(x - 5 * s, y - hh * 0.62, 10 * s, 3 * s);
    ctx.fillStyle = '#eee7d0';                                          // のぞく カード
    ctx.fillRect(x - w * 0.3, y - hh - 6 * s, w * 0.6, 7 * s);
    ctx.fillStyle = '#c9543f'; ctx.fillRect(x - w * 0.3, y - hh - 6 * s, w * 0.6, 2 * s);
    ctx.restore();
  },
  // 軒下の 風鈴。ガラスの おわんと 舌、みじかい たんざく（風で ゆれる）。
  // 明るい 窓を 背に しても 見えるよう、濃い ふち・暗い 留め具・赤い たんざくで はっきり
  fuurin: (x, y, s) => {
    const sway = Math.sin(elapsed * 1.6) * 4 * s;
    ctx.save();
    ctx.fillStyle = 'rgba(40,32,24,0.9)';                                // 軒の 留め具
    ctx.fillRect(x - 7 * s, y - 30 * s, 14 * s, 4 * s);
    ctx.strokeStyle = 'rgba(70,70,70,0.85)'; ctx.lineWidth = 1.4;        // つり糸
    ctx.beginPath(); ctx.moveTo(x, y - 26 * s); ctx.lineTo(x + sway, y - 11 * s); ctx.stroke();
    ctx.fillStyle = 'rgba(150,195,215,0.92)';                            // ガラス（青みを 濃く）
    ctx.beginPath(); ctx.ellipse(x + sway, y, 11 * s, 10 * s, 0, Math.PI, 0); ctx.fill();
    ctx.strokeStyle = 'rgba(40,70,90,0.95)'; ctx.lineWidth = 2;          // 濃い ふち
    ctx.beginPath(); ctx.ellipse(x + sway, y, 11 * s, 10 * s, 0, Math.PI, 0); ctx.stroke();
    ctx.beginPath(); ctx.moveTo(x + sway - 11 * s, y); ctx.lineTo(x + sway + 11 * s, y); ctx.stroke();
    ctx.fillStyle = 'rgba(60,90,110,0.95)';                              // 舌
    ctx.beginPath(); ctx.arc(x + sway * 1.4, y + 9 * s, 2.6 * s, 0, Math.PI * 2); ctx.fill();
    ctx.fillStyle = '#d84a3a';                                           // たんざく（赤で 目立つ）
    ctx.fillRect(x + sway * 1.6 - 2.5 * s, y + 11 * s, 5 * s, 14 * s);
    ctx.restore();
  },
  // 手前の 草むら（E2・前景）。何本かの 葉を たばねて かく。near ほど 大きい。
  // キャラより 前に 来る ことが ある（回りこみ）ので、根もとは 濃く 葉先は うすく
  kusamura: (x, y, s) => {
    ctx.save();
    const blades = 9, w = 46 * s, hgt = 40 * s;
    for (let i = 0; i < blades; i++) {
      const t = i / (blades - 1) - 0.5;               // -0.5..0.5
      const bx = x + t * w, lean = t * 14 * s + (Math.sin(elapsed * 0.9 + i) * 2 * s);
      const bh = hgt * (0.7 + 0.5 * (1 - Math.abs(t) * 1.4));
      const g = 90 + i % 3 * 20;
      ctx.strokeStyle = 'rgb(' + (52 + i % 2 * 14) + ',' + g + ',' + (44 + i % 2 * 10) + ')';
      ctx.lineWidth = 3.2 * s; ctx.lineCap = 'round';
      ctx.beginPath(); ctx.moveTo(bx, y);
      ctx.quadraticCurveTo(bx + lean * 0.5, y - bh * 0.6, bx + lean, y - bh);
      ctx.stroke();
    }
    ctx.restore();
  },
  // とび石。水べに ぽつぽつ ある ひらたい 石（D10・意味のない 小川を わたる 遊び）。
  // ゲーム的な ごほうびは つけない。ただ 石を つたって いける だけ
  ishi: (x, y, s) => {
    ctx.save();
    ctx.fillStyle = 'rgba(0,0,0,0.16)';
    ctx.beginPath(); ctx.ellipse(x, y + 3 * s, 15 * s, 6 * s, 0, 0, Math.PI * 2); ctx.fill();
    ctx.fillStyle = '#8b8577';                       // 石
    ctx.beginPath(); ctx.ellipse(x, y, 14 * s, 8 * s, 0, 0, Math.PI * 2); ctx.fill();
    ctx.fillStyle = 'rgba(220,216,204,0.5)';         // 上の ひかり
    ctx.beginPath(); ctx.ellipse(x - 3 * s, y - 2 * s, 7 * s, 3 * s, 0, 0, Math.PI * 2); ctx.fill();
    ctx.restore();
  },
  // 日めくり。壁の 小さな こよみ（赤い 見出しと 数字）
  himekuri: (x, y, s) => {
    const w = 22 * s, hh = 30 * s;
    ctx.save();
    ctx.fillStyle = '#f3efe6'; ctx.fillRect(x - w / 2, y - hh, w, hh);
    ctx.fillStyle = '#c23a2f'; ctx.fillRect(x - w / 2, y - hh, w, 8 * s);
    ctx.strokeStyle = 'rgba(60,50,40,0.5)'; ctx.lineWidth = 1; ctx.strokeRect(x - w / 2, y - hh, w, hh);
    ctx.fillStyle = '#3a3a3a';
    text('八', x, y - hh * 0.35, 12 * s, '#3a3a3a', 'center');
    ctx.restore();
  },
};

// ===== ごはんの絵（D13）=====
// 朝ごはん・晩ごはんの 場面で、**日ごとに ちがう** おかずを ちゃぶ台に かく（§1・演出）。
// `{k:'gohan', at:'breakfast'|'dinner'}` で 出て、場面が おわると 消える。
// 中身は [ずれx, ずれy, いろ]。器は 白、中身は その いろ。絵は 手で かく（写真の上）
let gohanShow = null;
const GOHAN = {
  breakfast: [
    { name:'ごはんと みそしる', items:[[-26,1,'#f4efe2'],[2,3,'#7a4a22'],[28,-3,'#eccb66']] },
    { name:'おにぎり',         items:[[-20,1,'#f4efe2'],[6,1,'#f4efe2'],[-6,-7,'#2f3a24']] },
    { name:'たまごかけごはん',  items:[[-8,1,'#f4efe2'],[20,3,'#e8b84a']] },
    { name:'おかゆと うめぼし',  items:[[-2,1,'#f6f2e8'],[20,5,'#b23a2f']] },
  ],
  dinner: [
    { name:'カレーライス',     items:[[-8,1,'#f4efe2'],[16,2,'#8a5a1e']] },
    { name:'やきざかなと ごはん', items:[[-22,2,'#f4efe2'],[16,-2,'#9aa0a2']] },
    { name:'やさいいため',     items:[[-4,1,'#5aa845'],[20,4,'#e0803a']] },
    { name:'なすの にもの',     items:[[-6,1,'#4b2e5e'],[18,4,'#f4efe2']] },
    { name:'そうめん',         items:[[-2,1,'#eef2f4'],[18,6,'#3f7d33']] },
  ],
};
function gohanFor(at, day) {
  const menu = GOHAN[at] || GOHAN.breakfast;
  return menu[(day - 1) % menu.length];
}
function drawGohan() {
  if (!gohanShow || cur !== 'doma') return;
  const cx = 548, cy = 502;
  ctx.save();
  ctx.fillStyle = 'rgba(0,0,0,0.18)';                          // かげ
  ctx.beginPath(); ctx.ellipse(cx, cy + 9, 54, 15, 0, 0, Math.PI * 2); ctx.fill();
  ctx.fillStyle = '#7a4a24';                                   // ちゃぶ台
  ctx.beginPath(); ctx.ellipse(cx, cy, 50, 20, 0, 0, Math.PI * 2); ctx.fill();
  ctx.strokeStyle = 'rgba(40,24,10,0.5)'; ctx.lineWidth = 2; ctx.stroke();
  for (const [dx, dy, col] of gohanShow.items) {
    ctx.fillStyle = '#ece6d6';                                 // 器
    ctx.beginPath(); ctx.ellipse(cx + dx, cy + dy, 11, 6, 0, 0, Math.PI * 2); ctx.fill();
    ctx.fillStyle = col;                                       // 中身
    ctx.beginPath(); ctx.ellipse(cx + dx, cy + dy - 1, 7, 3.4, 0, 0, Math.PI * 2); ctx.fill();
  }
  ctx.restore();
}

// ===== HUD（P8）=====
// ふだんは 隠れていて、**移動キーを 押すと 少しの間だけ** 出る（常時だと ゲーム感が 強い）。
//   ・持ちもの（あみ／さお）を 数字キーで 持ちかえ（いま持っているのを 枠で かこむ）
//   ・虫かごの 数、C で 中身を 見る
let hudT = 0;
function hudTick(dt) { if (hudT > 0) hudT -= dt; }
function hudPeek() { hudT = 2.6; }
function drawHud() {
  if (hudT <= 0) return;
  const a = clamp(hudT > 0.5 ? 1 : hudT / 0.5, 0, 1);
  const tools = toolsHeld();
  const kago = numOf('mushikago');
  if (!tools.length && !kago) return;
  ctx.save(); ctx.globalAlpha = a;
  const pad = 10, x0 = W - 236, y0 = 44;
  ctx.fillStyle = 'rgba(8,12,9,0.5)'; ctx.fillRect(x0, y0, 226, 62);
  // 道具
  let x = x0 + pad;
  ctx.font = '15px system-ui, sans-serif';
  for (let i = 0; i < tools.length; i++) {
    const k = tools[i], nm = (ITEMS[k] || {}).name || k, on = holding(k);
    const label = (i + 1) + ' ' + nm;
    const wtxt = ctx.measureText(label).width + 12;
    if (on) { ctx.fillStyle = 'rgba(255,230,150,0.22)'; ctx.fillRect(x - 2, y0 + 8, wtxt, 22);
              ctx.strokeStyle = '#ffe08a'; ctx.lineWidth = 1.5; ctx.strokeRect(x - 2, y0 + 8, wtxt, 22); }
    text(label, x + 4, y0 + 24, 15, on ? '#ffe8a8' : '#cfe0c8');
    x += wtxt + 8;
  }
  if (!tools.length) text('（道具は まだ ない）', x0 + pad, y0 + 24, 14, '#9fb69a');
  // 虫かご
  text('むしかご ×' + kago + '　[C かご / V ずかん]', x0 + pad, y0 + 48, 14, '#cfe0c8');
  ctx.restore();
}

// ===== 画面下の 短い しらせ（トースト）=====
let toastMsg = '', toastT = 0;
function toast(s, dur) { toastMsg = s; toastT = dur || 1.4; }
function toastTick(dt) { if (toastT > 0) toastT -= dt; }
function drawToast() {
  if (toastT <= 0 || !toastMsg) return;
  const a = clamp(toastT > 0.3 ? 1 : toastT / 0.3, 0, 1);
  ctx.save(); ctx.globalAlpha = a;
  ctx.fillStyle = 'rgba(8,12,9,0.5)'; ctx.fillRect(0, H - 52, W, 52);
  text(toastMsg, W/2, H - 22, 20, '#fff4cf', 'center');
  ctx.restore();
}
