// ===== ミニゲーム =====
// 場面の とちゅうで べつの あそびに 入り、**けっかを 場面に 返す**。
//
//   場面に  { k:'mini', name:'mushi', cfg:{…}, out:'mushi_kyou' }  と 書く。
//   おわると けっかが WORLD.num[out] に 入るので、そのあと
//   { k:'if', when:{ num:{ key:'mushi_kyou', min:1 } }, go:'とれた' }  で 分けられる。
//
// **本番の 虫とり・釣りは 第3期で ここに 足す。**
// ひとつの あそびは { start, step, draw } の 3つを 持つ：
//   start(m) … はじめの したく。じぶんの ものは m.d に しまう
//   step(m, dt) … 毎フレーム。おわったら m.result を 入れて m.done = true
//   draw(m)  … 毎フレーム。画面ぜんぶ じぶんで かく
const MINI = {};
let mini = null;

function startMini(name, cfg, out) {
  const def = MINI[name];
  if (!def) return false;              // 知らない 名まえ。場面を 止めずに 素通りする
  mini = { name, def, out, cfg: cfg || {}, t: 0, done: false, result: 0, d: {} };
  if (def.start) def.start(mini);
  state = 'mini';
  return true;
}

function miniStep(dt) {
  if (!mini) { state = 'scene'; return; }
  mini.t += dt;
  if (mini.def.step) mini.def.step(mini, dt);
  if (!mini.done) return;
  // おわった。けっかを 数に しまって 場面に もどす
  if (mini.out) setNum(mini.out, mini.result);
  mini = null;
  state = 'scene';
}

function miniDraw() { if (mini && mini.def.draw) mini.def.draw(mini); }

// --- 仕組みが 動くかを たしかめる ためだけの もの。**本番では つかわない。**
// cfg.s 秒 待って、そのあいだに スペースを おした 回数を かえす
MINI.test = {
  start: m => { m.d.n = 0; },
  step: (m, dt) => {
    if (advance) { m.d.n++; advance = false; }
    if (m.t >= (m.cfg.s || 1.5)) { m.result = m.d.n; m.done = true; }
  },
  draw: m => {
    ctx.fillStyle = '#0b120d'; ctx.fillRect(0, 0, W, H);
    text('（ためしの あそび）', W/2, H/2 - 22, 21, '#9fc39a', 'center');
    text('おした かず ' + m.d.n, W/2, H/2 + 20, 30, '#ffe9ac', 'center');
  },
};
