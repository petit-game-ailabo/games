// 自動でわけたファイル。もとは photo/index.html 1枚だった
// ===== 描画 =====
const tmp = document.createElement('canvas'); tmp.width = 16; tmp.height = 16;
const tctx = tmp.getContext('2d');
function drawChar(ci, x, y, h, flip, haze) {
  const cx = (ci%8)*16, cy = Math.floor(ci/8)*16, w = h;
  let src = imgChars, sx = cx, sy = cy;
  if (haze > 0.01) {
    tctx.clearRect(0,0,16,16);
    tctx.globalCompositeOperation = 'source-over';
    tctx.drawImage(imgChars, cx, cy, 16, 16, 0, 0, 16, 16);
    tctx.globalCompositeOperation = 'source-atop';
    tctx.globalAlpha = haze;
    tctx.fillStyle = SC[cur].haze || '#e6eee0';
    tctx.fillRect(0,0,16,16);
    tctx.globalAlpha = 1; tctx.globalCompositeOperation = 'source-over';
    src = tmp; sx = 0; sy = 0;
  }
  ctx.save();
  ctx.translate(Math.round(x), Math.round(y));
  if (flip) ctx.scale(-1, 1);
  ctx.drawImage(src, sx, sy, 16, 16, -w/2, -h, w, h);
  ctx.restore();
}
function shadow(x, y, h, soft) {
  ctx.save();
  ctx.fillStyle = '#0c1608';
  ctx.globalAlpha = 0.16 * (soft || 1);
  ctx.beginPath(); ctx.ellipse(x, y, h*0.30, h*0.105, 0, 0, Math.PI*2); ctx.fill();
  ctx.globalAlpha = 0.30 * (soft || 1);
  ctx.beginPath(); ctx.ellipse(x, y, h*0.19, h*0.062, 0, 0, Math.PI*2); ctx.fill();
  ctx.restore();
}
function text(t, x, y, size, col, align, weight) {
  ctx.fillStyle = col;
  ctx.font = (weight||'bold') + ' ' + size + 'px "Hiragino Kaku Gothic ProN","Noto Sans JP",sans-serif';
  ctx.textAlign = align || 'left'; ctx.textBaseline = 'alphabetic';
  ctx.fillText(t, x, y);
}
function wrap(t, size, maxW) {
  ctx.font = 'bold ' + size + 'px "Hiragino Kaku Gothic ProN","Noto Sans JP",sans-serif';
  const out = []; let line = '';
  for (const ch of t) {
    if (ctx.measureText(line + ch).width > maxW && line) { out.push(line); line = ch; }
    else line += ch;
  }
  if (line) out.push(line);
  return out;
}
// 写真のうえに ふとんを かく。輪郭線は出さず、面だけ。
// すこし ぼかして 写真の解像感に あわせないと 貼りものに見える
function drawFuton(n) {
  const q = n.quad;                                   // [おく左, おく右, 手前右, 手前左]
  const P = (a,b,t) => [a[0]+(b[0]-a[0])*t, a[1]+(b[1]-a[1])*t];
  const L = t => P(q[0], q[3], t), R = t => P(q[1], q[2], t);
  const band = (t0, t1, inset) => {
    const a = L(t0), b = R(t0), c = R(t1), d = L(t1);
    if (!inset) return [a,b,c,d];
    return [P(a,b,inset), P(b,a,inset), P(c,d,inset), P(d,c,inset)];
  };
  const path = pts => { ctx.beginPath(); ctx.moveTo(pts[0][0], pts[0][1]);
                        for (let i=1;i<pts.length;i++) ctx.lineTo(pts[i][0], pts[i][1]);
                        ctx.closePath(); ctx.fill(); };
  ctx.save();
  ctx.filter = 'blur(2.2px)';
  ctx.globalAlpha = 0.42; ctx.fillStyle = '#241d10';           // たたみに おちる かげ
  path([[q[0][0]-8,q[0][1]+10],[q[1][0]+12,q[1][1]+10],[q[2][0]+14,q[2][1]+8],[q[3][0]-12,q[3][1]+8]]);
  ctx.filter = 'blur(1.1px)';
  ctx.globalAlpha = 1;    ctx.fillStyle = '#eee7d6'; path(band(0, 1));          // 敷きぶとん
  ctx.globalAlpha = 0.45; ctx.fillStyle = '#a99c80'; path(band(0.93, 1));       // 手前の陰
  // 夏の掛けぶとん。たたみの色と はっきり分けないと ただの白い板に見える
  ctx.globalAlpha = 1;    ctx.fillStyle = '#8ea6b8'; path(band(0.38, 0.96));
  ctx.globalAlpha = 0.55; ctx.fillStyle = '#6f8798'; path(band(0.62, 0.66));    // しわ
  ctx.globalAlpha = 1;    ctx.fillStyle = '#fbf8ef'; path(band(0.31, 0.40));    // めくり返した裏地
  ctx.globalAlpha = 0.40; ctx.fillStyle = '#7d8f9c'; path(band(0.40, 0.435));   // その影
  ctx.globalAlpha = 1;    ctx.fillStyle = '#fdfbf5'; path(band(0.06, 0.22, 0.19)); // まくら
  ctx.globalAlpha = 0.35; ctx.fillStyle = '#a2957b'; path(band(0.22, 0.255, 0.19));
  ctx.restore();
}
function hazeOf(y) {
  const sc = SC[cur];
  const far = 1 - clamp((y - sc.yTop) / (sc.yBot - sc.yTop), 0, 1);
  return far*far*0.30;
}
function sayBox(who, txt) {
  const lines = wrap(txt, 19, 660);
  const bh = 46 + lines.length*27, by = H - bh - 18;
  ctx.save(); ctx.fillStyle = 'rgba(10,16,12,0.72)'; ctx.fillRect(28, by, W-56, bh); ctx.restore();
  drawChar(CI[who], 74, by + bh - 12, 56, false, 0);
  text(NAME[who] || '', 112, by + 26, 14, '#a9c79c');
  lines.forEach((s, i) => text(s, 112, by + 52 + i*27, 19, '#ffffff'));
}
const sayDur = t => 1.9 + t.length * 0.085;
