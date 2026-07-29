// 自動でわけたファイル。もとは photo/index.html 1枚だった
// ===== 音（環境音だけ。曲は 朝の たいそう のときだけ）=====
let AC = null, ambGain = null, ambTimer = 0;
function initAudio() {
  if (AC) return;
  try { AC = new (window.AudioContext || window.webkitAudioContext)(); } catch (e) { return; }
  ambGain = AC.createGain(); ambGain.gain.value = 0.0; ambGain.connect(AC.destination);
  const len = AC.sampleRate * 3;
  const buf = AC.createBuffer(1, len, AC.sampleRate);
  const d = buf.getChannelData(0);
  for (let i=0;i<len;i++) d[i] = Math.random()*2-1;
  const src = AC.createBufferSource(); src.buffer = buf; src.loop = true;
  const lp = AC.createBiquadFilter(); lp.type='lowpass'; lp.frequency.value=420;
  const wg = AC.createGain(); wg.gain.value = 0.16;
  src.connect(lp); lp.connect(wg); wg.connect(ambGain); src.start();
  ambGain.gain.linearRampToValueAtTime(0.9, AC.currentTime + 1.6);
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
