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
  const held = hasItem('ami') && bugsActive();
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
