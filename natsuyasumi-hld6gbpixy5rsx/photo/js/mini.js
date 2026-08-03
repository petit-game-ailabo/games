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

// --- 虫とり。あみを 左右に うごかし、虫に かさねて スペースで ふる。
//   **その日その場所の 虫は かぎりが ある**（cfg.pool の のこり。DESIGN §6）。
//   とれたら result=1、にげられたら 0。もう いない ときも 0。
//   phase … oi（おいかけ）→ tore（とれた）／nige（にげた）／none（もう いない）
//   cfg … { pool:'mushi:aze', max:18, time:7 }
MINI.mushi = {
  start: m => {
    const d = m.d;
    d.pool = m.cfg.pool || 'mushi'; d.max = m.cfg.max || 18;
    d.left = leftToday(d.pool, d.max);
    d.nx = W / 2; d.swing = -1;
    if (d.left <= 0) { d.phase = 'none'; d.msg = 'きょうは もう いない'; d.endT = 0; m.result = 0; return; }
    d.phase = 'oi'; d.msg = 'あみで つかまえて';
    d.bx = 120 + Math.random() * (W - 240);
    d.by = H * 0.42;
    d.bvx = (Math.random() < 0.5 ? -1 : 1) * (110 + Math.random() * 70);
    d.turn = 0.8 + Math.random() * 1.4;
  },
  step: (m, dt) => {
    const d = m.d;
    if (d.phase === 'oi') {
      // あみ：矢印 か、タッチなら ゆびの ところ
      if (keys.ArrowLeft || keys.KeyA)  d.nx -= 320 * dt;
      if (keys.ArrowRight || keys.KeyD) d.nx += 320 * dt;
      if (stick.on) d.nx = stick.x;
      d.nx = clamp(d.nx, 40, W - 40);
      // 虫：ふらふら 飛ぶ。ときどき むきを かえる
      d.turn -= dt;
      if (d.turn <= 0) { d.bvx = -d.bvx * (0.7 + Math.random() * 0.6); d.turn = 0.8 + Math.random() * 1.4; }
      d.bx += d.bvx * dt;
      if (d.bx < 60)     { d.bx = 60;     d.bvx = Math.abs(d.bvx); }
      if (d.bx > W - 60) { d.bx = W - 60; d.bvx = -Math.abs(d.bvx); }
      d.by = H * 0.42 + Math.sin(m.t * 3.3) * 26;
      // ふる。あみの よこ位置が 虫に かさなっていれば とれる
      if (advance && d.swing < 0) { advance = false; d.swing = m.t; }
      if (d.swing >= 0) {
        if (Math.abs(d.bx - d.nx) < 44) {
          m.result = 1; useToday(d.pool, d.max);
          // 何を とったか。out の 数 1つでは 種類を 返せないので、ここで じかに
          // 虫かご（数）と 図鑑（種類ごとの しるし）に かきこむ
          d.tore = MUSHI_KINDS[Math.floor(Math.random() * MUSHI_KINDS.length)];
          addNum('mushikago');
          setFlag('zukan:' + d.tore);
          d.phase = 'tore'; d.msg = 'つかまえた！（' + d.tore + '）'; d.endT = m.t;
        } else if (m.t > d.swing + 0.28) d.swing = -1;
      }
      if (m.t > (m.cfg.time || 7)) { d.phase = 'nige'; d.msg = 'にげられた…'; d.endT = m.t; m.result = 0; }
    } else {
      if (m.t >= d.endT + 1.1) m.done = true;
    }
  },
  draw: m => {
    const d = m.d;
    ctx.fillStyle = '#0e1c10'; ctx.fillRect(0, 0, W, H);
    ctx.strokeStyle = 'rgba(120,170,110,0.22)'; ctx.lineWidth = 2;
    for (let i = 0; i < 6; i++) { const y = H*0.72 + i*11;
      ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(W, y); ctx.stroke(); }
    if (d.phase !== 'none') {
      // 虫（はねが ぱたぱた）
      const flap = 6 + Math.abs(Math.sin(m.t * 22)) * 5;
      ctx.fillStyle = '#ffe36b';
      ctx.beginPath(); ctx.ellipse(d.bx, d.by, flap, 8, 0, 0, Math.PI*2); ctx.fill();
      ctx.fillStyle = '#7a6a20'; ctx.fillRect(d.bx - 1, d.by - 7, 2, 14);
      // あみ（ふると 上へ のびる）
      const sw = d.swing >= 0, ny = H*0.64 - (sw ? 44 : 0);
      ctx.strokeStyle = '#d8d2c4'; ctx.lineWidth = 3;
      ctx.beginPath(); ctx.moveTo(d.nx, H - 8); ctx.lineTo(d.nx, ny); ctx.stroke();
      ctx.strokeStyle = sw ? '#fff' : '#b7c7d0';
      ctx.beginPath(); ctx.arc(d.nx, ny - 18, 18, 0, Math.PI*2); ctx.stroke();
    }
    text(d.msg, W/2, H*0.15, 26, d.phase === 'tore' ? '#ffe36b' : '#cfe6ee', 'center');
    if (d.phase === 'oi') text('← →  で あみ　スペースで ふる', W/2, H*0.9, 18, '#9fc39a', 'center');
  },
};

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
