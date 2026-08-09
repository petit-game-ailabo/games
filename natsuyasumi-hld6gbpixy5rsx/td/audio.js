// なつやすみ 2D・環境音（手続き生成＝素材いらず／絵と同じで“借り物のしょぼさ”が出ない）
// 写真版 photo/js/audio.js の セミ・鈴虫・小鳥・カラス・葉ずれ を流用し、時間帯(tod)で鳴き分ける。
// 「ほんものの夏の外は、小さい音のあつまり」——鳴りっぱなしにせず、来ては去る。
'use strict';
let AC = null, ambGain = null, shortNoise = null, longNoise = null;
let ambTimer = 0, muted = false;
let rainGain = null;            // 雨の 常時音（ゲインで 出し入れ）
let brookGain = null;           // 川の せせらぎ（水に 近いほど 大きく）

function noiseBuf(sec) {
  const len = Math.ceil(AC.sampleRate * sec);
  const buf = AC.createBuffer(1, len, AC.sampleRate);
  const d = buf.getChannelData(0);
  for (let i = 0; i < len; i++) d[i] = Math.random()*2 - 1;
  return buf;
}
// 最初の キー／クリックで 呼ぶ（自動再生 ポリシー対策）
function initAudio() {
  if (AC) { if (AC.state === 'suspended') AC.resume(); return; }
  try { AC = new (window.AudioContext || window.webkitAudioContext)(); } catch (e) { return; }
  ambGain = AC.createGain(); ambGain.gain.value = 0.0; ambGain.connect(AC.destination);
  shortNoise = noiseBuf(0.3); longNoise = noiseBuf(3);
  ambGain.gain.linearRampToValueAtTime(muted ? 0 : 0.9, AC.currentTime + 1.6);
  // 雨の 常時音：ノイズを こもらせた「ざあ」。ふだんは ゲイン0
  const rs = AC.createBufferSource(); rs.buffer = longNoise; rs.loop = true;
  const rlp = AC.createBiquadFilter(); rlp.type = 'lowpass'; rlp.frequency.value = 1400;
  const rhp = AC.createBiquadFilter(); rhp.type = 'highpass'; rhp.frequency.value = 300;
  rainGain = AC.createGain(); rainGain.gain.value = 0;
  rs.connect(rlp); rlp.connect(rhp); rhp.connect(rainGain); rainGain.connect(AC.destination); rs.start();
}
// 雨の 音量（0=やむ）。game.js から 毎フレーム
function setRainLevel(v) {
  if (AC && rainGain) rainGain.gain.setTargetAtTime(muted ? 0 : v, AC.currentTime, 0.4);
}
// 川の せせらぎ（水に 近いほど 大きく）
function setBrookLevel(v) {
  if (!AC) return;
  if (!brookGain) {
    const s = AC.createBufferSource(); s.buffer = longNoise; s.loop = true; s.playbackRate.value = 1.4;
    const bp = AC.createBiquadFilter(); bp.type = 'bandpass'; bp.frequency.value = 900; bp.Q.value = 0.5;
    const hp = AC.createBiquadFilter(); hp.type = 'highpass'; hp.frequency.value = 500;
    brookGain = AC.createGain(); brookGain.gain.value = 0;
    s.connect(bp); bp.connect(hp); hp.connect(brookGain); brookGain.connect(AC.destination); s.start();
  }
  brookGain.gain.setTargetAtTime(muted ? 0 : v, AC.currentTime, 0.3);
}
function toggleMute() {
  muted = !muted;
  if (AC && ambGain) ambGain.gain.setTargetAtTime(muted ? 0 : 0.9, AC.currentTime, 0.2);
  return muted;
}

// --- セミ（ひる）／ひぐらし（ゆうがた・低め）。ざらついた ノイズを こまかく ふるわせる
function cicada(vol, freq, dur) {
  if (!AC) return;
  const t = AC.currentTime, len = Math.ceil(AC.sampleRate*dur);
  const buf = AC.createBuffer(1, len, AC.sampleRate);
  const d = buf.getChannelData(0);
  for (let i = 0; i < len; i++) {
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
// --- 小鳥（あさ）。ちゅんちゅん を 2〜4こ
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
// --- 鈴虫（ゆうがた〜よる）。高い「リーン」を こまかく ふるわせる
function suzumushi(vol) {
  if (!AC) return;
  const t0 = AC.currentTime, dur = 0.5 + Math.random()*0.35, f = 4300 + Math.random()*500;
  const o = AC.createOscillator(); o.type = 'sine'; o.frequency.value = f;
  const g = AC.createGain();
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
  const am = AC.createOscillator(); am.type = 'sine'; am.frequency.value = 40 + Math.random()*8;
  const amg = AC.createGain(); amg.gain.value = vol*0.6;
  am.connect(amg); amg.connect(g.gain);
  o.connect(g); g.connect(ambGain);
  o.start(t0); am.start(t0); o.stop(t0 + dur + 0.05); am.stop(t0 + dur + 0.05);
}
// --- カラス（よる）。しりさがりの「カー」を 2〜3回
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
// --- 葉ずれ（風）。ひくい うなりは 入れない（こわくなる）
function sawasawa(vol) {
  if (!AC || !longNoise) return;
  const t = AC.currentTime, dur = 1.4 + Math.random()*1.6;
  const s = AC.createBufferSource(); s.buffer = longNoise; s.loop = true;
  s.playbackRate.value = 0.8 + Math.random()*0.5;
  const bp = AC.createBiquadFilter(); bp.type='bandpass'; bp.frequency.value = 1600 + Math.random()*1700; bp.Q.value = 0.7;
  const hp = AC.createBiquadFilter(); hp.type='highpass'; hp.frequency.value = 950;
  const g = AC.createGain();
  g.gain.setValueAtTime(0.0001, t);
  g.gain.linearRampToValueAtTime(vol, t + dur*0.4);
  g.gain.linearRampToValueAtTime(vol*0.65, t + dur*0.65);
  g.gain.linearRampToValueAtTime(0.0001, t + dur);
  s.connect(bp); bp.connect(hp); hp.connect(g); g.connect(ambGain);
  s.start(t); s.stop(t + dur + 0.05);
}

// --- ラジオ体操の みじかい ジングル（あさ・スタンプ時）。ふえ風の 上りメロ
function taisoJingle() {
  if (!AC) return;
  const t0 = AC.currentTime, mel = [0, 4, 7, 12, 7, 12];   // ドミソド…
  mel.forEach((semi, i) => {
    const t = t0 + i*0.16, f = 523.25 * Math.pow(2, semi/12);
    const o = AC.createOscillator(); o.type = 'triangle'; o.frequency.value = f;
    const bp = AC.createBiquadFilter(); bp.type = 'lowpass'; bp.frequency.value = 2600;
    const g = AC.createGain();
    g.gain.setValueAtTime(0.0001, t);
    g.gain.linearRampToValueAtTime(0.14, t + 0.02);
    g.gain.exponentialRampToValueAtTime(0.0001, t + 0.16);
    o.connect(bp); bp.connect(g); g.connect(ambGain);
    o.start(t); o.stop(t + 0.2);
  });
}
// --- 水あそびの ぱしゃ（みじかい 水音）。ノイズの 破裂＋ひくい ぽちゃん
function mizuSfx() {
  if (!AC || !shortNoise) return;
  const t = AC.currentTime;
  const s = AC.createBufferSource(); s.buffer = shortNoise;
  const bp = AC.createBiquadFilter(); bp.type='bandpass'; bp.frequency.value=1400; bp.Q.value=0.6;
  const g = AC.createGain();
  g.gain.setValueAtTime(0.0001, t);
  g.gain.exponentialRampToValueAtTime(0.28, t + 0.01);
  g.gain.exponentialRampToValueAtTime(0.0001, t + 0.22);
  s.connect(bp); bp.connect(g); g.connect(ambGain); s.start(t); s.stop(t + 0.26);
  const o = AC.createOscillator(); o.type='sine';
  o.frequency.setValueAtTime(520, t); o.frequency.exponentialRampToValueAtTime(240, t + 0.16);
  const og = AC.createGain();
  og.gain.setValueAtTime(0.0001, t); og.gain.linearRampToValueAtTime(0.12, t + 0.02);
  og.gain.exponentialRampToValueAtTime(0.0001, t + 0.2);
  o.connect(og); og.connect(ambGain); o.start(t); o.stop(t + 0.24);
}
// --- いま 何が 鳴く 時間帯か（時計を 見なくても 耳で 夏が わかる）
function ambKindOf(tod) {
  if (tod >= 19 || tod < 5) return 'yoru';
  if (tod >= 16) return 'yugata';
  if (tod < 9) return 'asa';
  return 'hiru';
}
// game.js から 毎フレーム 呼ぶ。来ては 去る 音を まばらに 鳴らす
function ambientTick(dt, tod) {
  if (!AC || muted) return;
  ambTimer -= dt;
  if (ambTimer > 0) return;
  const vol = 0.06;
  ambTimer = 0.7 + Math.random()*1.8;
  if (Math.random() < 0.14) sawasawa(0.018);          // ときどき 葉ずれ
  const kind = ambKindOf(tod);
  if (kind === 'yoru') {
    if (Math.random() < 0.3) karasu(vol*1.1); else suzumushi(vol*0.9);
    ambTimer += 0.8;                                    // よるは まばら
  } else if (kind === 'yugata') {
    if (Math.random() < 0.45) suzumushi(vol*0.85);
    else cicada(vol*0.9, 1500 + Math.random()*400, 1.6 + Math.random()*1.4);   // ひぐらし
  } else if (kind === 'asa') {
    kotori(vol*0.9);
    ambTimer *= 0.7;                                    // あさは にぎやか
  } else {
    cicada(vol, 3000 + Math.random()*700, 0.8 + Math.random()*1.4);            // ひる＝セミ
  }
}
