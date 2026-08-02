// 自動でわけたファイル。もとは photo/index.html 1枚だった
// ===== 音（環境音だけ。曲は 朝の たいそう のときだけ）=====
let AC = null, ambGain = null, ambTimer = 0;
let windLP = null, windGain = null, mizuGain = null;

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

// --- 風鈴。家のなかで ときどき。二つの音が すこし ずれて 鳴る
function fuurin(vol) {
  if (!AC) return;
  const t0 = AC.currentTime;
  for (const [f, dt, v] of [[2637, 0, 1], [3520, 0.035, 0.55]]) {
    const o = AC.createOscillator(); o.type = 'sine'; o.frequency.value = f * (0.99 + Math.random()*0.02);
    const g = AC.createGain();
    g.gain.setValueAtTime(0.0001, t0+dt);
    g.gain.linearRampToValueAtTime(vol*v, t0+dt+0.006);
    g.gain.exponentialRampToValueAtTime(0.0001, t0+dt+1.5);
    o.connect(g); g.connect(ambGain);
    o.start(t0+dt); o.stop(t0+dt+1.6);
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
  const o = AC.createOscillator(); o.type = 'sine';
  o.frequency.value = 4300 + Math.random()*500;
  const g = AC.createGain();
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
  if (inside && Math.random() < 0.30) { fuurin(0.055); lastPlace2 = 'fuurin'; }
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
