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
