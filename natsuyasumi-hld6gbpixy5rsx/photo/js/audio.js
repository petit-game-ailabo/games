// 自動でわけたファイル。もとは photo/index.html 1枚だった
// ===== 音（環境音だけ。曲は 朝の たいそう のときだけ）=====
let AC = null, ambGain = null, ambTimer = 0;
let windLP = null, windGain = null, mizuGain = null;
// 足音は 1秒に 3回ほど 鳴る。そのたび 音の もとを 作ると ごみが たまるので 使いまわす
let shortNoise = null;

// 場所ごとの 風の きこえかた。
//   in  家のなか … こもって 小さい
//   out いえのまえ … ふつう
//   ki  そとの みち … 木の葉ずれで つよめ
const WIND = { in:{ f:300, g:0.05 }, out:{ f:480, g:0.15 }, ki:{ f:640, g:0.22 } };

function noiseBuf(sec) {
  const len = AC.sampleRate * sec;
  const buf = AC.createBuffer(1, len, AC.sampleRate);
  const d = buf.getChannelData(0);
  for (let i=0;i<len;i++) d[i] = Math.random()*2-1;
  return buf;
}
function initAudio() {
  if (AC) return;
  try { AC = new (window.AudioContext || window.webkitAudioContext)(); } catch (e) { return; }
  ambGain = AC.createGain(); ambGain.gain.value = 0.0; ambGain.connect(AC.destination);
  shortNoise = noiseBuf(0.3);

  // 風。ずっと 鳴っている。場所で こもりぐあいと 大きさを 変える
  const src = AC.createBufferSource(); src.buffer = noiseBuf(3); src.loop = true;
  windLP = AC.createBiquadFilter(); windLP.type='lowpass'; windLP.frequency.value=420;
  windGain = AC.createGain(); windGain.gain.value = 0.16;
  src.connect(windLP); windLP.connect(windGain); windGain.connect(ambGain); src.start();

  // 水の音。**水のそばの 画面でだけ** 大きくなる（screens.json の mizu）
  const w = AC.createBufferSource(); w.buffer = noiseBuf(3); w.loop = true;
  const wbp = AC.createBiquadFilter(); wbp.type='bandpass'; wbp.frequency.value=900; wbp.Q.value=0.6;
  const whp = AC.createBiquadFilter(); whp.type='highpass'; whp.frequency.value=320;
  mizuGain = AC.createGain(); mizuGain.gain.value = 0;
  w.connect(wbp); wbp.connect(whp); whp.connect(mizuGain); mizuGain.connect(ambGain); w.start();

  ambGain.gain.linearRampToValueAtTime(0.9, AC.currentTime + 1.6);
}

// --- 足音。画面ごとに ふみごこちを 変える（screens.json の ashi）。
//   f/q  … どのあたりの 音か。ひくいほど にぶい
//   hp   … これより ひくい音を けずる。じゃりじゃりした ものは 高く
//   sweep… 鳴っている あいだに 音が さがる（草を かき分ける ような 音）
//   kishi… 板が きしむ。音程が すこし 上がって 下がる
const ASHI = {
  tatami: { f: 260,  q: 0.9, dur: 0.10 },                          // たたみ
  ita:    { f: 420,  q: 1.2, dur: 0.13, kishi: 1 },                // 板の ろうか
  tsuchi: { f: 340,  q: 0.8, dur: 0.09 },                          // どま
  jari:   { f: 2600, q: 0.7, dur: 0.12, hp: 900 },                 // じゃり
  ishi:   { f: 1800, q: 1.6, dur: 0.09, hp: 700 },                 // 石だたみ
  kusa:   { f: 3400, q: 0.5, dur: 0.22, hp: 1100, sweep: 0.45 },   // 草を かき分ける
};
let lastFoot = 0, lastFootKind = '', footCount = 0;
function ashioto(kind, vol) {
  if (!AC || !shortNoise) return;
  const a = ASHI[kind] || ASHI.tsuchi, t = AC.currentTime, dur = a.dur;
  lastFootKind = kind; footCount++;
  const s = AC.createBufferSource(); s.buffer = shortNoise;
  const bp = AC.createBiquadFilter(); bp.type = 'bandpass'; bp.Q.value = a.q;
  const f0 = a.f * (0.9 + Math.random()*0.2);
  bp.frequency.setValueAtTime(f0, t);
  if (a.sweep) bp.frequency.linearRampToValueAtTime(f0*a.sweep, t + dur);
  const g = AC.createGain();
  g.gain.setValueAtTime(0.0001, t);
  g.gain.linearRampToValueAtTime(vol, t + 0.008);
  g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
  let tail = bp;
  if (a.hp) {
    const hp = AC.createBiquadFilter(); hp.type='highpass'; hp.frequency.value = a.hp;
    bp.connect(hp); tail = hp;
  }
  s.connect(bp); tail.connect(g); g.connect(ambGain);
  s.start(t); s.stop(t + dur + 0.05);
  if (a.kishi) {
    const o = AC.createOscillator(); o.type = 'sine';
    const k = 380 + Math.random()*160;
    o.frequency.setValueAtTime(k, t);
    o.frequency.linearRampToValueAtTime(k*1.25, t + 0.05);
    o.frequency.linearRampToValueAtTime(k*0.9,  t + 0.16);
    const og = AC.createGain();
    og.gain.setValueAtTime(0.0001, t);
    og.gain.linearRampToValueAtTime(vol*0.6, t + 0.02);
    og.gain.exponentialRampToValueAtTime(0.0001, t + 0.18);
    o.connect(og); og.connect(ambGain); o.start(t); o.stop(t + 0.2);
  }
}
// 一歩ごとに 1回。歩幅（player.bob）が π を またぐたび 鳴らす。
// **奥に いるほど 小さく**。走ると すこし 強い
function footTick() {
  if (!AC) return;
  const ph = Math.floor(player.bob / Math.PI);
  if (ph === lastFoot) return;
  lastFoot = ph;
  if (!player.moving) return;
  const sc = SC[cur];
  const depth = Math.max(0.45, heightAt(player.y) / sc.hNear);
  ashioto(sc.ashi || 'tsuchi', 0.085 * depth * (player.running ? 1.25 : 1));
}

// --- 風鈴（江戸風鈴＝うすい ガラスの おわん）。
// ワイングラスと おなじ なかま。しらべ直して 分かったのは この4つ：
//  1. たたくと **いちばん低い モード（ふちに はらが 4つ）だけが のこる。** ほぼ ひとつの音
//  2. その上の モード（比 2.83／5.42）も 出るが **0.1秒ほどで 消える**。
//     ここを のばすと 持続する にごりに なる ← 前の 実装の まちがいは 比ではなく **のばしすぎ**
//  3. ガラスは 完全な まるでは ないので、その モードが **2つに わずかに 割れる**。
//     数Hz の ずれが ゆっくりした ゆらぎ（うなり）に なる。鐘の warble と 同じ機構。
//     **これが 風鈴らしさの 正体。** 前の 実装には これが なかった
//  4. 舌が あたる 瞬間だけ 雑音が のる
function fuurin(vol) {
  if (!AC) return;
  const t0 = AC.currentTime;
  const f    = 1950 + Math.random()*420;
  const beat = 1.4 + Math.random()*2.4;      // うなり。1〜4Hz が ここちよい
  const ring = 1.6 + Math.random()*1.2;

  // いちばん低い モード。**わずかに ずれた 2つ**で ゆらぎを 作る
  for (const df of [0, beat]) {
    const o = AC.createOscillator(); o.type = 'sine';
    o.frequency.value = f + df;
    const g = AC.createGain();
    g.gain.setValueAtTime(0.0001, t0);
    g.gain.linearRampToValueAtTime(vol*0.5, t0 + 0.006);
    g.gain.exponentialRampToValueAtTime(0.0001, t0 + ring);
    o.connect(g); g.connect(ambGain);
    o.start(t0); o.stop(t0 + ring + 0.1);
  }
  // 上の モード。**すぐ 消える**。たたいた しゅんかんの 明るさ だけを 出す
  for (const [m, v, d] of [[2.83, 0.15, 0.10], [5.42, 0.05, 0.05]]) {
    const o = AC.createOscillator(); o.type = 'sine';
    o.frequency.value = f * m * (0.995 + Math.random()*0.01);
    const g = AC.createGain();
    g.gain.setValueAtTime(0.0001, t0);
    g.gain.linearRampToValueAtTime(vol*v, t0 + 0.003);
    g.gain.exponentialRampToValueAtTime(0.0001, t0 + d);
    o.connect(g); g.connect(ambGain);
    o.start(t0); o.stop(t0 + d + 0.05);
  }
  if (shortNoise) {                          // 舌が あたる 音
    const s = AC.createBufferSource(); s.buffer = shortNoise;
    const hp = AC.createBiquadFilter(); hp.type='highpass'; hp.frequency.value = 2800;
    const g = AC.createGain();
    g.gain.setValueAtTime(vol*0.35, t0);
    g.gain.exponentialRampToValueAtTime(0.0001, t0 + 0.035);
    s.connect(hp); hp.connect(g); g.connect(ambGain);
    s.start(t0); s.stop(t0 + 0.06);
  }
}

// --- 木の葉ずれ。風が ひとしきり 強く なる
function kaze(vol, dur) {
  if (!AC || !windGain) return;
  const t = AC.currentTime, base = windGain.gain.value;
  windGain.gain.cancelScheduledValues(t);
  windGain.gain.setValueAtTime(base, t);
  windGain.gain.linearRampToValueAtTime(base + vol, t + dur*0.35);
  windGain.gain.linearRampToValueAtTime(base, t + dur);
}

// --- 画面が 変わったら 風と 水を つけ変える。**ゆっくり 変える**（ぶつ切りだと 不自然）
let lastPlace = '';
function setPlaceSound() {
  if (!AC || cur === lastPlace) return;
  lastPlace = cur; lastPlace2 = '';    // 前の画面の のこりが 検査で 嘘に 見えないように
  const sc = SC[cur] || {}, w = WIND[sc.amb] || WIND.out, t = AC.currentTime;
  windLP.frequency.cancelScheduledValues(t);
  windLP.frequency.setValueAtTime(windLP.frequency.value, t);
  windLP.frequency.linearRampToValueAtTime(w.f, t + 0.8);
  windGain.gain.cancelScheduledValues(t);
  windGain.gain.setValueAtTime(windGain.gain.value, t);
  windGain.gain.linearRampToValueAtTime(w.g, t + 0.8);
  mizuGain.gain.cancelScheduledValues(t);
  mizuGain.gain.setValueAtTime(mizuGain.gain.value, t);
  mizuGain.gain.linearRampToValueAtTime((sc.mizu || 0) * 0.09, t + 0.8);
}
function cicada(vol, freq, dur) {
  if (!AC) return;
  const t = AC.currentTime, len = Math.ceil(AC.sampleRate*dur);
  const buf = AC.createBuffer(1, len, AC.sampleRate);
  const d = buf.getChannelData(0);
  for (let i=0;i<len;i++) {
    const ph = i / AC.sampleRate;
    const trill = 0.55 + 0.45*Math.sign(Math.sin(ph*2*Math.PI*36));
    d[i] = (Math.random()*2-1) * trill;
  }
  const s = AC.createBufferSource(); s.buffer = buf;
  const bp = AC.createBiquadFilter(); bp.type='bandpass'; bp.frequency.value=freq; bp.Q.value=6;
  const g = AC.createGain();
  g.gain.setValueAtTime(0, t);
  g.gain.linearRampToValueAtTime(vol, t+dur*0.25);
  g.gain.linearRampToValueAtTime(0, t+dur);
  s.connect(bp); bp.connect(g); g.connect(ambGain); s.start(t); s.stop(t+dur+0.05);
}
// --- 小鳥。あさの ちゅんちゅん。みじかいのを 2〜4こ つづけて 鳴らす
function kotori(vol) {
  if (!AC) return;
  const t0 = AC.currentTime, n = 2 + Math.floor(Math.random()*3);
  for (let i = 0; i < n; i++) {
    const t = t0 + i*(0.09 + Math.random()*0.07), f = 3200 + Math.random()*1400;
    const o = AC.createOscillator(); o.type = 'triangle';
    o.frequency.setValueAtTime(f*0.75, t);
    o.frequency.exponentialRampToValueAtTime(f, t + 0.025);
    o.frequency.exponentialRampToValueAtTime(f*0.72, t + 0.07);
    const bp = AC.createBiquadFilter(); bp.type='bandpass'; bp.frequency.value=3800; bp.Q.value=2;
    const g = AC.createGain();
    g.gain.setValueAtTime(0.0001, t);
    g.gain.linearRampToValueAtTime(vol, t + 0.012);
    g.gain.exponentialRampToValueAtTime(0.0001, t + 0.08);
    o.connect(bp); bp.connect(g); g.connect(ambGain);
    o.start(t); o.stop(t + 0.12);
  }
}

// --- 鈴虫。ゆうがた〜よるの「リーン」。高い音を こまかく ふるわせる
function suzumushi(vol) {
  if (!AC) return;
  const t0 = AC.currentTime, dur = 0.5 + Math.random()*0.35;
  const f = 4300 + Math.random()*500;
  const o = AC.createOscillator(); o.type = 'sine'; o.frequency.value = f;
  const g = AC.createGain();
  // すこし ざらつきを まぜる。**まったくの 純音は 電子音に きこえる**
  if (shortNoise) {
    const s = AC.createBufferSource(); s.buffer = shortNoise; s.loop = true;
    const bp = AC.createBiquadFilter(); bp.type='bandpass'; bp.frequency.value = f; bp.Q.value = 14;
    const ng = AC.createGain(); ng.gain.value = 0.5;
    s.connect(bp); bp.connect(ng); ng.connect(g);
    s.start(t0); s.stop(t0 + dur + 0.05);
  }
  g.gain.setValueAtTime(0.0001, t0);
  g.gain.linearRampToValueAtTime(vol, t0 + 0.12);
  g.gain.setValueAtTime(vol, t0 + dur*0.7);
  g.gain.exponentialRampToValueAtTime(0.0001, t0 + dur);
  // ふるえ。これが ないと ただの 電子音に なる
  const am = AC.createOscillator(); am.type = 'sine'; am.frequency.value = 40 + Math.random()*8;
  const amg = AC.createGain(); amg.gain.value = vol*0.6;
  am.connect(amg); amg.connect(g.gain);
  o.connect(g); g.connect(ambGain);
  o.start(t0); am.start(t0); o.stop(t0 + dur + 0.05); am.stop(t0 + dur + 0.05);
}

// --- カラス。よるの「カー」。しりさがりに 2〜3回
function karasu(vol) {
  if (!AC) return;
  const t0 = AC.currentTime, n = 2 + Math.floor(Math.random()*2);
  for (let i = 0; i < n; i++) {
    const t = t0 + i*(0.42 + Math.random()*0.2), dur = 0.34, f = 620 + Math.random()*120;
    const o = AC.createOscillator(); o.type = 'sawtooth';
    o.frequency.setValueAtTime(f, t);
    o.frequency.linearRampToValueAtTime(f*0.72, t + dur);
    const bp = AC.createBiquadFilter(); bp.type='bandpass'; bp.frequency.value=1100; bp.Q.value=1.4;
    const g = AC.createGain();
    g.gain.setValueAtTime(0.0001, t);
    g.gain.linearRampToValueAtTime(vol, t + 0.05);
    g.gain.setValueAtTime(vol, t + dur*0.6);
    g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    o.connect(bp); bp.connect(g); g.connect(ambGain);
    o.start(t); o.stop(t + dur + 0.05);
  }
}

// --- いま 何が 鳴く 時間帯か。時計は 出さないので、**耳でも 時間が わかる**ようにする
//   asa    ちゅんちゅん（小鳥）
//   hiru   セミ
//   yugata ひぐらし と 鈴虫
//   yoru   カラス と 鈴虫
function ambKind() {
  if (WORLD.yoruDone) return 'yoru';       // 晩ごはんが すんだら よる
  const t = dayT();
  if (t > 0.62) return 'yugata';
  if (t < 0.12) return 'asa';
  return 'hiru';
}

let lastAmb = '', lastPlace2 = '';
function ambientTick(dt) {
  if (!AC) return;
  setPlaceSound();              // 画面が 変わっていたら 風と 水を つけ変える
  ambTimer -= dt;
  if (ambTimer > 0) return;
  const sc = SC[cur], inside = (sc.amb === 'in'), ki = (sc.amb === 'ki');
  const vol = ki ? 0.13 : (inside ? 0.035 : 0.06);
  ambTimer = (inside ? 1.6 : 0.7) + Math.random()*1.8;
  // 場所の音。虫や鳥とは べつに、その場所らしい 音を まぜる
  if (inside && Math.random() < 0.18) { fuurin(0.035); lastPlace2 = 'fuurin'; }
  else if (ki && Math.random() < 0.28) { kaze(0.10, 2.2 + Math.random()*1.6); lastPlace2 = 'kaze'; }

  const kind = ambKind();
  lastAmb = kind;
  if (kind === 'yoru') {
    if (Math.random() < 0.35) karasu(vol*1.1); else suzumushi(vol*0.9);
    ambTimer += 0.8;                       // よるは まばら
  } else if (kind === 'yugata') {
    if (Math.random() < 0.45) suzumushi(vol*0.85);
    else cicada(vol*0.9, 1500 + Math.random()*400, 1.6 + Math.random()*1.4);  // ひぐらし
  } else if (kind === 'asa') {
    kotori(vol*0.9);
    ambTimer *= 0.7;                       // あさは にぎやか
  } else {
    cicada(vol, ki ? 2300 + Math.random()*900 : 3000 + Math.random()*700,
           0.8 + Math.random()*1.4);
  }
}

// ラジオたいそうの曲。原曲は著作権があるので 雰囲気だけ似せた じさくの メロディ。
// バンドパスと ハイパスを とおして「ラジオから鳴っている」音にする
const TAISO_BPM = 132;
const TAISO_MEL = [
  [0,1],[4,1],[7,1],[4,1],  [5,1],[4,1],[2,2],
  [0,1],[4,1],[7,1],[12,1], [7,4],
  [5,1],[7,1],[9,1],[7,1],  [4,1],[2,1],[0,2],
  [2,1],[4,1],[5,1],[7,1],  [0,4],
];
function beep(freq, t0, dur, vol, type, out) {
  const o = AC.createOscillator(), g = AC.createGain();
  o.type = type; o.frequency.value = freq;
  g.gain.setValueAtTime(0.0001, t0);
  g.gain.linearRampToValueAtTime(vol, t0+0.02);
  g.gain.exponentialRampToValueAtTime(0.0001, t0+dur);
  o.connect(g); g.connect(out); o.start(t0); o.stop(t0+dur+0.06);
}
function playTaiso() {
  const e = 60/TAISO_BPM/2;                       // 八分音符の長さ
  let n = 0; for (const m of TAISO_MEL) n += m[1];
  if (!AC) return n*e + 0.8;
  const bp = AC.createBiquadFilter(); bp.type='bandpass'; bp.frequency.value=1500; bp.Q.value=0.8;
  const hp = AC.createBiquadFilter(); hp.type='highpass'; hp.frequency.value=430;
  const g  = AC.createGain(); g.gain.value = 0.55;
  bp.connect(hp); hp.connect(g); g.connect(AC.destination);
  const t0 = AC.currentTime + 0.2;
  let at = 0;
  for (const [semi, dur] of TAISO_MEL) {
    beep(523.25*Math.pow(2, semi/12), t0+at*e, dur*e*0.92, 0.15, 'square', bp);
    at += dur;
  }
  for (let i=0; i<n; i+=2) beep(165, t0+i*e, 0.07, 0.09, 'sine', bp);
  return n*e + 0.8;
}
