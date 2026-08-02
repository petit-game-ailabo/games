// 自動でわけたファイル。もとは photo/index.html 1枚だった
// ===== 状態 =====
let state = 'load';
let cur = 'zashiki';
let day = 1;
const player = { x:430, y:500, face:1, bob:0, moving:false, running:false };
let fade = 0, fadeTo = null, nameT = 0, elapsed = 0, exitLock = false;
let talkNpc = null, lineT = 0;
let scene = null;          // {q, i, t, entered, flags}
// その場面だけのキャラ。ひとりずつ じぶんの じょうたい を持つ
//   pose … 'idle'（ふよふよ）／'walk'（歩いている）／'taiso'（たいそう中）／'gone'（もう居ない）
let cast = [];
let playerPose = 'idle';
let sceneSay = null;       // 場面のセリフ [who, text]
let walkTo = null;         // じどうで あるく先
let taisoT0 = -99, taisoBeats = 0;
let veil = 0;              // 場面の切りかわりの 黒い幕
let talkLock = false;      // 場面が おわった直後に かってに 会話が はじまらないように
let nedokoT = 0, nedokoArmed = false;
// 時計は出さない。画面を移るたびに 時間がすすみ、空気の色と 虫の声で わかるようにする
let steps = 0, mukaeDone = false;
const DAY_STEPS = 24;
const dayT = () => clamp(steps / DAY_STEPS, 0, 1);

function moveMove(x, y, spd, dt) {   // 目的地へ 一歩ぶん すすむ（壁ぞいにすべる）
  const dx = x - player.x, dy = y - player.y, d = Math.hypot(dx, dy);
  if (d < 6) return true;
  const step = Math.min(spd*dt, d);
  const nx = player.x + dx/d*step, ny = player.y + dy/d*step;
  let mx = false, my = false;
  if (walkable(nx, player.y)) { player.x = nx; mx = true; }
  if (walkable(player.x, ny)) { player.y = ny; my = true; }
  const slide = Math.max(4, step*2.6);
  if (!my && Math.abs(dy) > 0.5) {
    for (let k=1; k<=slide; k+=0.5) {
      if (walkable(player.x-k, ny)) { player.x -= k; player.y = ny; break; }
      if (walkable(player.x+k, ny)) { player.x += k; player.y = ny; break; }
    }
  }
  if (!mx && Math.abs(dx) > 0.5) {
    for (let k=1; k<=slide; k+=0.5) {
      if (walkable(nx, player.y-k)) { player.y -= k; player.x = nx; break; }
      if (walkable(nx, player.y+k)) { player.y += k; player.x = nx; break; }
    }
  }
  if (dx > 1) player.face = 1; else if (dx < -1) player.face = -1;
  player.bob += dt*9;
  return false;
}

function enter(id, at) {
  cur = id;
  const st = SC[id].start;
  const p = st[at] || st[Object.keys(st)[0]];
  const f = nearestFree(p[0], p[1]);
  player.x = f.x; player.y = f.y;
  nameT = 3.2; exitLock = true; talkNpc = null; lineT = 0;
}
function linesOf(n) { const t = talksOf(n); return (t && t[day]) || null; }
function resetDay() {
  for (const k in SC) for (const n of (SC[k].npc || [])) {
    n.idx = 0; n.done = !linesOf(n);
  }
  talkNpc = null; nedokoT = 0; nedokoArmed = false; steps = 0; mukaeDone = false;
}
function runScene(q) {
  scene = { q, i:0, t:0, entered:-1, flags:{} };
  sceneSay = null; walkTo = null; cast = []; playerPose = 'idle'; taisoT0 = -99; veil = 0;
}
function startMorning() { resetDay(); runScene(morningScript(day)); state = 'scene'; }
function start() { day = 1; startMorning(); fade = 1; }
