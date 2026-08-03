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

// 虫の 種類。虫かご（数）と 図鑑（種類ごとの しるし zukan:◯）で つかう
const MUSHI_KINDS = ['とんぼ', 'ちょう', 'かぶと', 'せみ', 'こがね', 'ばった'];

function startMini(name, cfg, out) {
  const def = MINI[name];
  if (!def) return false;              // 知らない 名まえ。場面を 止めずに 素通りする
  mini = { name, def, out, cfg: cfg || {}, t: 0, done: false, result: 0, d: {} };
  if (def.start) def.start(mini);
  advance = false;   // 場面を すすめた ボタンを ここへ 持ちこまない（初手で 誤爆しない）
  state = 'mini';
  return true;
}

function miniStep(dt) {
  if (!mini) { state = 'scene'; return; }
  mini.t += dt;
  // すでに おわって いたら step は 呼ばない（外から おわらせた ときに
  // つづきの step が けっかを 上書きしないように）
  if (!mini.done && mini.def.step) mini.def.step(mini, dt);
  if (!mini.done) return;
  // おわった。けっかを 数に しまって 場面に もどす
  if (mini.out) setNum(mini.out, mini.result);
  mini = null;
  // ふつうは 場面から 起動するので 場面に もどる。場面が ない ときは 遊びに もどす
  // （場面なしで 起動された ときの 保険。null の scene を 読んで 落ちないように）
  state = scene ? 'scene' : 'play';
}

function miniDraw() { if (mini && mini.def.draw) mini.def.draw(mini); }

// --- 釣り。うきを じっと 見て、**あたり（！）が 来た あいだ**に おす。
//   はやすぎ（あたりの まえに おす）／おそすぎ（にげる）は とれない。
//   とれたら result=1、だめなら 0。あたりの 時こくは 毎回 ちがう。
//   phase … machi（待ち）→ atari（あたり）→ owari（けっかを 見せる）
MINI.tsuri = {
  start: m => {
    m.d.phase = 'machi';
    m.d.bite  = 1.2 + Math.random() * 2.3;   // あたりが 来る 時こく
    m.d.win   = m.cfg.win || 0.85;            // おせる あいだ
    m.d.msg   = 'うきを みてて…';
    m.d.bob   = 0;
  },
  step: (m, dt) => {
    m.d.bob += dt;
    if (m.d.phase === 'machi') {
      if (advance) {                          // あたりの まえに おした
        advance = false;
        m.d.phase = 'owari'; m.d.msg = 'はやすぎた。にげられた';
        m.d.endT = m.t; m.result = 0;
      } else if (m.t >= m.d.bite) {
        m.d.phase = 'atari'; m.d.msg = 'きた！ いま！'; m.d.atariT = m.t;
      }
    } else if (m.d.phase === 'atari') {
      if (advance) {                          // あたりの あいだに おした＝とれた
        advance = false;
        m.d.phase = 'owari'; m.d.msg = 'つれた！';
        m.d.endT = m.t; m.result = 1;
      } else if (m.t >= m.d.atariT + m.d.win) {
        m.d.phase = 'owari'; m.d.msg = 'にげられた…';
        m.d.endT = m.t; m.result = 0;
      }
    } else {                                  // owari：すこし 見せてから おわる
      if (m.t >= m.d.endT + 1.1) m.done = true;
    }
  },
  draw: m => {
    ctx.fillStyle = '#12303f'; ctx.fillRect(0, 0, W, H);
    // ゆれる 水めん
    ctx.strokeStyle = 'rgba(120,170,190,0.30)'; ctx.lineWidth = 2;
    for (let i = 0; i < 5; i++) {
      const y = H*0.4 + i*24 + Math.sin(m.d.bob*1.5 + i)*3;
      ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(W, y); ctx.stroke();
    }
    // うき。あたりの あいだは あかく はねる
    const atari = m.d.phase === 'atari';
    const bx = W/2, by = H*0.52 + Math.sin(m.d.bob*2.2)*4 + (atari ? Math.sin(m.t*42)*7 : 0);
    ctx.fillStyle = '#c23a2f'; ctx.fillRect(bx-2, by-22, 4, 14);
    ctx.fillStyle = atari ? '#ff5a4d' : '#eceff0';
    ctx.beginPath(); ctx.arc(bx, by, 9, 0, Math.PI*2); ctx.fill();
    // ことば
    text(m.d.msg, W/2, H*0.2, 26, atari ? '#ffe36b' : '#cfe6ee', 'center');
    if (atari) text('スペース！', W/2, H*0.8, 22, '#ffe36b', 'center');
  },
};

// 虫とり（MINI.mushi）は **画面の中で やる 方式**（js/field.js）に 置きかわった（P4／D-065）。
//   蝶が 世界を とんでいて、あみを 持って そばで ふって とる。別画面には とばない。

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
