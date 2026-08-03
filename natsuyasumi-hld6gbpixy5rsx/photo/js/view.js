// ===== 見るための 別画面 =====
// 虫かご・図鑑・スタンプカード・絵日記 のように、**ただ 見るだけ**の 画面。
// あそび（mini.js）は けっかを 返すが、こちらは 返さない。とじたら もとに もどるだけ。
//
//   場面に  { k:'view', name:'stamp' }  と 書く。
//   スペース／Enter／Esc／タップ で とじる。
//
// ひとつの 画面は { open, draw } を 持つ：
//   open(v) … はじめの したく。じぶんの ものは v.d に しまう
//   draw(v) … 毎フレーム。画面ぜんぶ じぶんで かく
const VIEW = {};
let view = null;

function openView(name, cfg) {
  const def = VIEW[name];
  if (!def) return false;              // 知らない 名まえ。場面を 止めずに 素通りする
  view = { name, def, cfg: cfg || {}, t: 0, d: {} };
  if (def.open) def.open(view);
  state = 'view';
  return true;
}
function closeView() { view = null; state = 'scene'; }
function viewStep(dt) {
  if (!view) { state = 'scene'; return; }
  view.t += dt;
  if (advance && view.t > 0.25) { advance = false; closeView(); }
}
function viewDraw() { if (view && view.def.draw) view.def.draw(view); }

// --- ラジオたいそうの スタンプカード。**31枡（八月ぜんぶ）。**
// おした 日は しるし `stamp:◯` で のこす（D4 が 立てる）
VIEW.stamp = {
  draw: v => {
    ctx.fillStyle = '#17140e'; ctx.fillRect(0, 0, W, H);
    // 紙
    const cw = 736, ch = 396, cx = (W-cw)/2, cy = (H-ch)/2 + 6;
    ctx.save();
    ctx.fillStyle = '#efe6cf'; ctx.fillRect(cx, cy, cw, ch);
    ctx.strokeStyle = 'rgba(90,74,48,0.5)'; ctx.lineWidth = 2;
    ctx.strokeRect(cx+8, cy+8, cw-16, ch-16);
    ctx.restore();
    text('ラジオたいそう', cx + 28, cy + 46, 24, '#4a3d26');
    text('八月', cx + cw - 28, cy + 46, 20, '#7a6a4a', 'right');

    const col = 8, top = cy + 74, bw = (cw - 56) / col, bh = 60;
    let done = 0;
    for (let i = 0; i < 31; i++) {
      const x = cx + 28 + (i % col) * bw, y = top + Math.floor(i / col) * bh;
      ctx.save();
      ctx.strokeStyle = 'rgba(120,102,70,0.45)'; ctx.lineWidth = 1;
      ctx.strokeRect(x + 2, y, bw - 6, bh - 8);
      text(hiduke(i+1), x + 8, y + 15, 12, '#8a7855');
      if (hasFlag('stamp:' + (i+1))) {
        done++;
        // 判こ。まると 二本線だけ。ここも 絵は 持っていないので 手で かく。
        // **日づけの もじに かぶせない。**かぶると 何日か 読めなくなる
        const mx = x + bw/2 - 2, my = y + bh/2 + 8, r = Math.min(bw, bh)*0.26;
        ctx.strokeStyle = 'rgba(186,58,48,0.82)'; ctx.lineWidth = 2.5;
        ctx.beginPath(); ctx.arc(mx, my, r, 0, Math.PI*2); ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(mx - 9, my - 4); ctx.lineTo(mx + 7, my - 4);
        ctx.moveTo(mx - 9, my + 4); ctx.lineTo(mx + 7, my + 4);
        ctx.stroke();
      }
      ctx.restore();
    }
    text(done + ' / 31', cx + cw - 28, cy + ch - 22, 18, '#6b5c3e', 'right');
    text('スペースで とじる', W/2, H - 14, 14, 'rgba(226,238,220,0.55)', 'center', 'normal');
  },
};

// --- 虫かご。**とった 数だけ** 虫を 入れて 見せる（実物大で 成果を かんじる）。
// 数は `mushikago`（虫取りが ふやす）。虫の 絵は 手で かく。
VIEW.mushikago = {
  draw: v => {
    ctx.fillStyle = '#141a12'; ctx.fillRect(0, 0, W, H);
    const n = numOf('mushikago');
    // かご（まるい あみかご）
    const cx = W/2, cy = H/2 + 6, rw = 300, rh = 190;
    ctx.save();
    ctx.fillStyle = '#0d120b';
    ctx.beginPath(); ctx.ellipse(cx, cy, rw, rh, 0, 0, Math.PI*2); ctx.fill();
    // あみめ
    ctx.strokeStyle = 'rgba(150,170,140,0.25)'; ctx.lineWidth = 1;
    for (let gx = -rw; gx <= rw; gx += 26) {
      ctx.beginPath(); ctx.moveTo(cx+gx, cy-rh); ctx.lineTo(cx+gx, cy+rh); ctx.stroke();
    }
    for (let gy = -rh; gy <= rh; gy += 26) {
      ctx.beginPath(); ctx.moveTo(cx-rw, cy+gy); ctx.lineTo(cx+rw, cy+gy); ctx.stroke();
    }
    ctx.strokeStyle = 'rgba(180,150,90,0.7)'; ctx.lineWidth = 4;
    ctx.beginPath(); ctx.ellipse(cx, cy, rw, rh, 0, 0, Math.PI*2); ctx.stroke();
    // 虫（数だけ。ぶつからないよう ぐるっと 並べる）
    for (let i = 0; i < n; i++) {
      const a = i * 2.399963;                      // 黄金角で ばらまく
      const rr = Math.sqrt((i+0.5) / Math.max(n,1));
      const bx = cx + Math.cos(a) * rr * (rw-40);
      const by = cy + Math.sin(a) * rr * (rh-40);
      ctx.fillStyle = '#ffe36b';
      ctx.beginPath(); ctx.ellipse(bx, by, 8, 5, a, 0, Math.PI*2); ctx.fill();
      ctx.fillStyle = '#7a6a20'; ctx.fillRect(bx-1, by-4, 2, 8);
    }
    ctx.restore();
    text('むしかご', W/2, cy - rh - 26, 24, '#cfe6b0', 'center');
    text(n + ' ひき', W/2, cy + rh + 34, 20, '#cfe6b0', 'center');
    text('スペースで とじる', W/2, H - 14, 14, 'rgba(226,238,220,0.55)', 'center', 'normal');
  },
};

// --- 図鑑。とった ことの ある 種類が うまる。しるしは `zukan:◯`（虫取りが 立てる）。
VIEW.zukan = {
  draw: v => {
    ctx.fillStyle = '#17140e'; ctx.fillRect(0, 0, W, H);
    const cw = 640, ch = 380, cx = (W-cw)/2, cy = (H-ch)/2 + 6;
    ctx.fillStyle = '#efe6cf'; ctx.fillRect(cx, cy, cw, ch);
    ctx.strokeStyle = 'rgba(90,74,48,0.5)'; ctx.lineWidth = 2;
    ctx.strokeRect(cx+8, cy+8, cw-16, ch-16);
    text('むしの ずかん', cx + 26, cy + 44, 24, '#4a3d26');
    const kinds = MUSHI_KINDS, col = 3;
    const bw = (cw - 52) / col, bh = 96, top = cy + 70;
    let got = 0;
    for (let i = 0; i < kinds.length; i++) {
      const x = cx + 26 + (i % col) * bw, y = top + Math.floor(i / col) * bh;
      const has = hasFlag('zukan:' + kinds[i]);
      ctx.strokeStyle = 'rgba(120,102,70,0.4)'; ctx.lineWidth = 1;
      ctx.strokeRect(x + 2, y, bw - 8, bh - 12);
      if (has) {
        got++;
        // 虫の 絵（手で）。とったら いろ つき
        const mx = x + bw/2 - 2, my = y + bh/2 - 6;
        ctx.fillStyle = '#c9a23a';
        ctx.beginPath(); ctx.ellipse(mx, my, 15, 9, 0, 0, Math.PI*2); ctx.fill();
        ctx.fillStyle = '#6a5618'; ctx.fillRect(mx-2, my-8, 4, 16);
        text(kinds[i], x + bw/2, y + bh - 20, 15, '#4a3d26', 'center');
      } else {
        text('？', x + bw/2, y + bh/2 + 4, 26, 'rgba(120,102,70,0.5)', 'center');
      }
    }
    text(got + ' / ' + kinds.length, cx + cw - 26, cy + ch - 20, 18, '#6b5c3e', 'right');
    text('スペースで とじる', W/2, H - 14, 14, 'rgba(226,238,220,0.55)', 'center', 'normal');
  },
};
