// 奥行き演出のパス（ドット絵×3D風の見せかた ― 例のスクエニ商標の名前は 使わない）。
// 本物は 3D空間＋板スプライトだが、ここは **1枚絵のまま** それらしく 見せる：
//   1) 遠景ぼかし（擬似 被写界深度）… 画面の 上ほど＝奥ほど ぼかす。背景を 焼いて キャッシュ
//   2) ブルーム … 明るい ところだけ 抜き出して にじませ、加算で のせる（背景に 焼く）
//   3) カラーグレーディング … 少し 彩度と コントラストを 上げ、影を 青、光を 暖色へ
//   4) 塵 … 光の中を ただよう つぶ（毎フレーム・キャラの 前）
//   5) 光の すじ … 窓/木もれ日 の 斜めの 帯（場所ごとに 向きを かえる）
//   6) ヴィネット … 四すみを おとす
// 1〜3 は **絵ごとに 一度だけ** 焼くので、毎フレームは 出来あがった 絵を 1枚 貼るだけ。
'use strict';

let fxOn = true;                    // F キーで 切りかえ（ビフォー/アフターを 見るため）
const fxCache = new Map();          // src → 焼いた canvas
let dust = null;                    // 塵のつぶ

function fxToggle() { fxOn = !fxOn; return fxOn; }

// --- 場所ごとの 味つけ。ray.xs は **光が 入ってくる 窓/木あいだの 位置**（絵に合わせて 置く）。
// 位置を 絵と そろえないと「貼りもの」に 見えるので、等間隔では ならべない。
const FX_TUNE = {
  // ざしき：左の 大きな 窓と、中央右の あけた 障子から 斜めに 差しこむ
  zashiki: { blur: 3.0, farY: 300, bloom: 0.22, warm: 0.06, ray: { a: -0.52, w: 130, s: 0.15, xs: [70, 250, 600, 760] } },
  // どま：左の 格子窓から。板の間に 帯が おちる
  doma:    { blur: 3.2, farY: 300, bloom: 0.26, warm: 0.08, ray: { a: -0.50, w: 120, s: 0.17, xs: [60, 210, 420] } },
  // ろうか：ガラス戸ぞいに 何本も。柱の あいだから こまかく
  rouka:   { blur: 3.0, farY: 250, bloom: 0.22, warm: 0.06, ray: { a: -0.62, w: 90,  s: 0.15, xs: [120, 260, 400, 540, 680] } },
  // いえのまえ：そとは 上からの 日ざし。木の あいだから ひろく
  iemae:   { blur: 3.6, farY: 330, bloom: 0.20, warm: 0.04, ray: { a: -0.44, w: 190, s: 0.10, xs: [180, 520, 860] } },
  // あぜみち：並木の あいだから。道の うえに 帯が かかる
  aze:     { blur: 3.6, farY: 330, bloom: 0.18, warm: 0.03, ray: { a: -0.42, w: 175, s: 0.11, xs: [240, 560, 880] } },
  // もり：木もれ日。細いのを たくさん
  mori:    { blur: 3.4, farY: 300, bloom: 0.24, warm: 0.05, ray: { a: -0.58, w: 80,  s: 0.16, xs: [150, 300, 430, 560, 700, 840] } },
};
function fxTune(key) { return FX_TUNE[key] || FX_TUNE.aze; }

function mkCanvas(w, h) { const c = document.createElement('canvas'); c.width = w; c.height = h; return c; }

// 背景を 焼く：遠景ぼかし＋ブルーム＋グレーディング。画面ごとに 一度だけ
function fxBake(img, key) {
  const t = fxTune(key);
  const out = mkCanvas(W, H), o = out.getContext('2d');

  // 1) 素の絵
  o.drawImage(img, 0, 0, W, H);

  // 2) 遠景ぼかし：ぼかした 絵を 上（奥）ほど 濃く 重ねる。境目が 出ないよう グラデで
  const bl = mkCanvas(W, H), b = bl.getContext('2d');
  b.filter = 'blur(' + t.blur + 'px)';
  b.drawImage(img, 0, 0, W, H);
  b.filter = 'none';
  const mask = mkCanvas(W, H), m = mask.getContext('2d');
  m.drawImage(bl, 0, 0);
  const g1 = m.createLinearGradient(0, 0, 0, H);        // 上=残す(奥) 下=消す(手前)
  g1.addColorStop(0, 'rgba(0,0,0,0.85)');
  g1.addColorStop(Math.min(0.95, t.farY / H * 0.7), 'rgba(0,0,0,0.20)');   // 中景は くっきり 残す
  g1.addColorStop(Math.min(0.98, t.farY / H), 'rgba(0,0,0,0.05)');
  g1.addColorStop(1, 'rgba(0,0,0,0)');
  m.globalCompositeOperation = 'destination-in';
  m.fillStyle = g1; m.fillRect(0, 0, W, H);
  o.drawImage(mask, 0, 0);

  // 3) ブルーム：**しきい値より 明るい ところだけ** 抜いて にじませ、加算。
  //    絵ぜんたいを にじませると 画面が 白く 浮くので、光源・窓・空だけを 拾う
  const br = mkCanvas(W, H), r = br.getContext('2d');
  r.drawImage(img, 0, 0, W, H);
  try {
    const id = r.getImageData(0, 0, W, H), d = id.data;
    for (let i = 0; i < d.length; i += 4) {
      // 輝度が TH 未満は 捨てる。こえた ぶんだけ 残す（やわらかい しきい値）
      const l = (d[i]*0.299 + d[i+1]*0.587 + d[i+2]*0.114) / 255;
      const k = l <= 0.72 ? 0 : (l - 0.72) / 0.28;
      d[i] *= k; d[i+1] *= k; d[i+2] *= k;
    }
    r.putImageData(id, 0, 0);
  } catch (e) {   // 画像が 別オリジンで 読めない ときは 従来どおり（安全側）
    r.globalCompositeOperation = 'multiply'; r.drawImage(br, 0, 0); r.globalCompositeOperation = 'source-over';
  }
  const br2 = mkCanvas(W, H), r2 = br2.getContext('2d');
  r2.filter = 'blur(14px)';
  r2.drawImage(br, 0, 0);
  r2.filter = 'none';
  o.globalCompositeOperation = 'lighter';
  o.globalAlpha = t.bloom;
  o.drawImage(br2, 0, 0);
  o.globalAlpha = 1;
  o.globalCompositeOperation = 'source-over';

  // 4) グレーディング：影を 青みに、光を 暖色に。うすく 2枚 重ねるだけ
  o.globalCompositeOperation = 'multiply';
  o.globalAlpha = 0.10;
  o.fillStyle = '#c8d6ff'; o.fillRect(0, 0, W, H);      // 影を すこし 青く
  o.globalCompositeOperation = 'lighter';
  o.globalAlpha = t.warm;
  o.fillStyle = '#ffd9a0'; o.fillRect(0, 0, W, H);      // 光を すこし 暖かく
  o.globalAlpha = 1;
  o.globalCompositeOperation = 'source-over';

  return out;
}

// 背景を えがく（焼いた 絵が あれば それを 使う）
function fxDrawBG(sc, key) {
  if (!fxOn || !sc.img || !sc.img.complete || !sc.img.naturalWidth) {
    ctx.drawImage(sc.img, 0, 0, W, H); return;
  }
  let c = fxCache.get(sc.src);
  if (!c) { try { c = fxBake(sc.img, key); fxCache.set(sc.src, c); } catch (e) { c = null; } }
  ctx.drawImage(c || sc.img, 0, 0, W, H);
}

// --- 光の すじ（窓・木もれ日）。ゆっくり 息を するように 濃さが 変わる
function fxRays(key, night) {
  const t = fxTune(key), ry = t.ray;
  const k = (1 - night) * ry.s;
  if (k <= 0.01) return;
  ctx.save();
  ctx.globalCompositeOperation = 'lighter';
  const xs = ry.xs || [ry.x || W/2];
  for (let i = 0; i < xs.length; i++) {
    // 本ごとに 太さと ゆらぎを 変える。そろえると 貼りものに 見える
    const w = ry.w * (0.7 + 0.5 * ((i * 37) % 10) / 10);
    const a = k * (0.55 + 0.45 * Math.sin(elapsed * 0.45 + i * 1.7));
    ctx.save();
    ctx.translate(xs[i], -70);
    ctx.rotate(ry.a);
    const g = ctx.createLinearGradient(0, 0, 0, H * 1.7);
    g.addColorStop(0, 'rgba(255,246,220,' + (a * 0.95).toFixed(3) + ')');
    g.addColorStop(0.45, 'rgba(255,242,210,' + (a * 0.45).toFixed(3) + ')');
    g.addColorStop(1, 'rgba(255,236,200,0)');
    ctx.fillStyle = g;
    // 帯は 先ほど ひろがる（台形）。平行だと 板に 見える
    ctx.beginPath();
    ctx.moveTo(-w * 0.35, 0); ctx.lineTo(w * 0.35, 0);
    ctx.lineTo(w * 0.75, H * 1.7); ctx.lineTo(-w * 0.75, H * 1.7);
    ctx.closePath(); ctx.fill();
    ctx.restore();
  }
  ctx.restore();
}

// --- 塵。光の 中を ゆっくり ただよう。よるは ほとんど 出さない
function fxDust(dt, night) {
  if (!dust) {
    dust = [];
    for (let i = 0; i < 46; i++)
      dust.push({ x: Math.random() * W, y: Math.random() * H, r: 0.6 + Math.random() * 1.2,
                  vx: 4 + Math.random() * 10, vy: -3 + Math.random() * 6, ph: Math.random() * 6.28 });
  }
  const k = 1 - night * 0.8;
  ctx.save();
  ctx.globalCompositeOperation = 'lighter';
  for (const p of dust) {
    p.x += p.vx * dt; p.y += (p.vy + Math.sin(elapsed * 0.6 + p.ph) * 4) * dt;
    if (p.x > W + 8) { p.x = -8; p.y = Math.random() * H; }
    if (p.y < -8) p.y = H + 8; else if (p.y > H + 8) p.y = -8;
    const a = (0.09 + 0.11 * (0.5 + 0.5 * Math.sin(elapsed * 1.1 + p.ph))) * k;
    ctx.fillStyle = 'rgba(255,248,226,' + a.toFixed(3) + ')';
    ctx.beginPath(); ctx.arc(p.x, p.y, p.r, 0, 6.283); ctx.fill();
  }
  ctx.restore();
}

// --- ヴィネット。四すみを おとして 目を 中央へ
let vigCache = null;
function fxVignette() {
  if (!vigCache) {
    vigCache = mkCanvas(W, H);
    const v = vigCache.getContext('2d');
    const g = v.createRadialGradient(W / 2, H * 0.52, H * 0.28, W / 2, H * 0.52, H * 0.92);
    g.addColorStop(0, 'rgba(0,0,0,0)');
    g.addColorStop(0.65, 'rgba(0,0,0,0.10)');
    g.addColorStop(1, 'rgba(0,0,0,0.42)');
    v.fillStyle = g; v.fillRect(0, 0, W, H);
  }
  ctx.drawImage(vigCache, 0, 0);
}

// キャラを えがいた あとに 呼ぶ 仕あげ（前に のせる ぶん）
function fxOver(dt, key, night) {
  if (!fxOn) return;
  fxRays(key, night);
  fxDust(dt, night);
  fxVignette();
}
