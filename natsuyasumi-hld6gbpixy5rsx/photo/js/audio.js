// 自動でわけたファイル。もとは photo/index.html 1枚だった
// ===== 音（環境音だけ。曲は 朝の たいそう のときだけ）=====
let AC = null, ambGain = null, ambTimer = 0;
let mizuGain = null;
// 音の もとは 使いまわす。そのたび 作ると ごみが たまる
let shortNoise = null, longNoise = null;

// **ずっと 鳴りつづける 音は 作らない。**
// ひとつの 音を 鳴らしっぱなしに すると 耳が つかれるし、
// ひくい うなりは こわい 風に きこえる。
// ほんものの 夏の 外は、セミと、ときどき 来る 葉ずれと、鳥。**小さい音の あつまり**。
//   p … 葉ずれが 来る ぐあい   v … その 大きさ
//
// **切れ目が あることを 数で たしかめること。**
// 鳴る間隔は だいたい 1.6/p 秒、ひとつの 長さは 平均2.2秒。
// 1.6/p が 2.2 より 小さいと かさなり つづけ、常時ノイズに 逆もどりする。
//   ki  1.6/0.25 = 6.4秒 おきに 2.2秒 → 3分の1 くらいしか 鳴っていない
//   out 1.6/0.14 = 11秒 おきに 2.2秒
const SAWA = { in:{ p:0.04, v:0.008 }, out:{ p:0.14, v:0.018 }, ki:{ p:0.25, v:0.028 } };

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
  longNoise  = noiseBuf(3);

  // 水の音。**水のそばの 画面でだけ**。ここだけは ほんとうに 鳴りつづける ものなので
  // のこすが、うんと 小さく、しゃーっと した 高い成分は けずる
  const w = AC.createBufferSource(); w.buffer = longNoise; w.loop = true;
  const wbp = AC.createBiquadFilter(); wbp.type='bandpass'; wbp.frequency.value=760; wbp.Q.value=0.5;
  const wlp = AC.createBiquadFilter(); wlp.type='lowpass';  wlp.frequency.value=1800;
  mizuGain = AC.createGain(); mizuGain.gain.value = 0;
  w.connect(wbp); wbp.connect(wlp); wlp.connect(mizuGain); mizuGain.connect(ambGain); w.start();

  ambGain.gain.linearRampToValueAtTime(0.9, AC.currentTime + 1.6);
}

// --- 葉ずれ。**さわさわと 来て、さわさわと 去る。**
// ひくい音は 入れない。ひくい うなりが こわい 風の しょうたい
function sawasawa(vol) {
  if (!AC || !longNoise) return;
  const t = AC.currentTime, dur = 1.4 + Math.random()*1.6;
  const s = AC.createBufferSource(); s.buffer = longNoise; s.loop = true;
  s.playbackRate.value = 0.8 + Math.random()*0.5;
  const bp = AC.createBiquadFilter(); bp.type = 'bandpass';
  bp.frequency.value = 1600 + Math.random()*1700;     // 葉ずれは 高い
  bp.Q.value = 0.7;
  const hp = AC.createBiquadFilter(); hp.type = 'highpass'; hp.frequency.value = 950;
  const g = AC.createGain();
  g.gain.setValueAtTime(0.0001, t);
  g.gain.linearRampToValueAtTime(vol, t + dur*0.4);           // ゆっくり 来て
  g.gain.linearRampToValueAtTime(vol*0.65, t + dur*0.65);
  g.gain.linearRampToValueAtTime(0.0001, t + dur);            // ゆっくり 去る
  s.connect(bp); bp.connect(hp); hp.connect(g); g.connect(ambGain);
  s.start(t); s.stop(t + dur + 0.05);
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
// **この家の 風鈴は ずっと 同じ もの。だから 音程は 変わらない。**
// たたくたび 音程を ふると、毎回 べつの物を たたいている ことに なり、鐘に きこえる
const FUURIN_F    = 3520;   // その ガラスの 音程。**うすい ガラスは かなり 高い**
const FUURIN_BEAT = 2.3;    // ゆがみで 割れた ぶんの ずれ（Hz）。うなりに なる

// 一回ぶん。**「ちりーん」の ーん は 長い。**
// はじめに すっと おちて、そのあと 3〜4秒 かけて 消えていく
function fuurinHit(t, vol) {
  const ring = 3.4 + vol*22;               // つよく たたくほど 長く のこる
  for (const df of [0, FUURIN_BEAT]) {
    const o = AC.createOscillator(); o.type = 'sine';
    o.frequency.value = FUURIN_F + df;
    const g = AC.createGain();
    g.gain.setValueAtTime(0.0001, t);
    g.gain.linearRampToValueAtTime(vol*0.5, t + 0.005);
    g.gain.exponentialRampToValueAtTime(vol*0.15, t + 0.25);   // はじめの おち
    g.gain.exponentialRampToValueAtTime(0.0001, t + ring);      // 長い しっぽ
    o.connect(g); g.connect(ambGain);
    o.start(t); o.stop(t + ring + 0.1);
  }
  // 上の モード。**ごく かすかに、すぐ 消える。**ここが 出すぎると 鐘に なる
  for (const [m, v, d] of [[2.83, 0.06, 0.06], [5.42, 0.02, 0.03]]) {
    const o = AC.createOscillator(); o.type = 'sine';
    o.frequency.value = FUURIN_F * m;
    const g = AC.createGain();
    g.gain.setValueAtTime(0.0001, t);
    g.gain.linearRampToValueAtTime(vol*v, t + 0.003);
    g.gain.exponentialRampToValueAtTime(0.0001, t + d);
    o.connect(g); g.connect(ambGain);
    o.start(t); o.stop(t + d + 0.05);
  }
  if (shortNoise) {                          // 舌が あたる 音。ごく かすかに
    const s = AC.createBufferSource(); s.buffer = shortNoise;
    const hp = AC.createBiquadFilter(); hp.type='highpass'; hp.frequency.value = 3200;
    const g = AC.createGain();
    g.gain.setValueAtTime(vol*0.22, t);
    g.gain.exponentialRampToValueAtTime(0.0001, t + 0.025);
    s.connect(hp); hp.connect(g); g.connect(ambGain);
    s.start(t); s.stop(t + 0.05);
  }
}

// **風に ゆられるので 不ぞろいに なる。** ひと吹きで 2〜5回、
// 間隔も つよさも ばらばら。おなじ 音程なので かさなっても にごらない
function fuurin(vol) {
  if (!AC) return;
  const t0 = AC.currentTime;
  const n = 1 + Math.floor(Math.random()*4);   // ひと吹きで 1〜4回。これも 不ぞろい
  let at = 0;
  for (let i = 0; i < n; i++) {
    fuurinHit(t0 + at, vol * (0.4 + Math.random()*0.8));
    // かけ算で 短い間隔が 多くなる。ときどき 長く あく＝不ぞろい
    at += 0.08 + Math.random()*Math.random()*0.95;
  }
}

// --- 家を 出た しゅんかん だけ 短く 鳴る（DESIGN.md §1）。**その日 はじめて 外に 出たとき だけ。**
// 出入りの たび 鳴らすと わずらわしく、「きょうの ぼうけんが はじまる」感じが 消える
const DEKAKE_MEL = [
  [0,1],[4,1],[7,1],[12,2],
  [9,1],[7,1],[9,1],[12,3],
];
let dekakeCount = 0;
function dekake() {
  if (!AC) return;
  dekakeCount++;
  const e = 0.19;
  const lp = AC.createBiquadFilter(); lp.type = 'lowpass'; lp.frequency.value = 3600;
  const g  = AC.createGain(); g.gain.value = 0.5;
  lp.connect(g); g.connect(AC.destination);
  const t0 = AC.currentTime + 0.05;
  let at = 0;
  for (const [semi, d] of DEKAKE_MEL) {
    beep(1046.5 * Math.pow(2, semi/12), t0 + at*e, d*e*0.9, 0.12, 'triangle', lp);
    at += d;
  }
  for (let i = 0; i < at; i += 2) beep(261.6, t0 + i*e, 0.10, 0.06, 'sine', lp);
}

// --- 画面が 変わったとき。水だけ ゆっくり つけ変える（ぶつ切りだと 不自然）
let lastPlace = '', prevAmb = '';
function setPlaceSound() {
  if (!AC || cur === lastPlace) return;
  lastPlace = cur; lastPlace2 = '';    // 前の画面の のこりが 検査で 嘘に 見えないように
  const sc = SC[cur] || {}, t = AC.currentTime;
  // 家のなか から 外へ 出た
  if (state === 'play' && prevAmb === 'in' && sc.amb !== 'in' && !WORLD.dekakeDone) {
    WORLD.dekakeDone = true;
    dekake();
  }
  prevAmb = sc.amb;
}

// --- 水の音は **音源からの 距離**で 決まる（DESIGN.md §1）。
// 画面ごとの 定数だと、あぜみちの どこに 立っても 同じ 大きさに なる。
// 音源は screens.json の spot に `oto:"mizu"` で 置く
function mizuTick() {
  if (!AC || !mizuGain) return;
  let v = 0;
  for (const sp of (SC[cur].spot || [])) {
    if (sp.oto !== 'mizu') continue;
    const d = groundDist(player.x, player.y, sp.x, sp.y);
    v = Math.max(v, (sp.v || 1) / (1 + d*d*0.6));
  }
  mizuGain.gain.setTargetAtTime(v * 0.05, AC.currentTime, 0.3);
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

// ===== 遠くから きこえる 音 =====
// 地図は 出さない。かわりに **耳で さがす**。
// 遠いほど 小さく、こもって きこえる。近づくほど はっきりする。
// うたっているのは おなじ子なので、こえの たかさは 変わらない（D-037）
const UTA_SCALE = [0, 2, 4, 7, 9, 12, 14];      // ペンタトニック。さまよう ふしに なる
// 遠さ → [大きさ, どれくらい こもるか(Hz)]
const UTA_FAR = [[0.085, 6000], [0.040, 1500], [0.016, 700]];

function uta(base, vol, cut) {
  if (!AC) return;
  const t0 = AC.currentTime + 0.05;
  const lp = AC.createBiquadFilter(); lp.type = 'lowpass'; lp.frequency.value = cut;
  const out = AC.createGain(); out.gain.value = 1; lp.connect(out); out.connect(ambGain);
  const n = 3 + Math.floor(Math.random()*3);
  let at = 0;
  for (let i = 0; i < n; i++) {
    const semi = UTA_SCALE[Math.floor(Math.random()*UTA_SCALE.length)];
    const dur = 0.18 + Math.random()*0.30;
    const f = base * Math.pow(2, semi/12);
    const o = AC.createOscillator(); o.type = 'triangle'; o.frequency.value = f;
    // ふるえ（ビブラート）。これが ないと 笛に きこえる
    const vib = AC.createOscillator(); vib.type = 'sine'; vib.frequency.value = 5.4 + Math.random();
    const vibg = AC.createGain(); vibg.gain.value = f * 0.012;
    vib.connect(vibg); vibg.connect(o.frequency);
    const g = AC.createGain();
    g.gain.setValueAtTime(0.0001, t0 + at);
    g.gain.linearRampToValueAtTime(vol, t0 + at + 0.05);
    g.gain.setValueAtTime(vol, t0 + at + dur*0.7);
    g.gain.exponentialRampToValueAtTime(0.0001, t0 + at + dur);
    o.connect(g); g.connect(lp);
    o.start(t0 + at); vib.start(t0 + at);
    o.stop(t0 + at + dur + 0.05); vib.stop(t0 + at + dur + 0.05);
    at += dur * (0.85 + Math.random()*0.4);
  }
}

// 画面の つながりを たどって、いくつ 先か を かぞえる
function screenDist(from, to) {
  if (from === to) return 0;
  const seen = { [from]: 0 };
  let q = [from];
  while (q.length) {
    const next = [];
    for (const k of q) for (const e of (SC[k].exits || [])) {
      if (seen[e.to] !== undefined) continue;
      seen[e.to] = seen[k] + 1;
      if (e.to === to) return seen[e.to];
      next.push(e.to);
    }
    q = next;
  }
  return 99;
}

let utaTimer = 0, utaNow = null;
function utaTick(dt) {
  if (!AC || state !== 'play') return;
  utaTimer -= dt;
  if (utaTimer > 0) return;
  utaTimer = 4.5 + Math.random()*5.5;
  utaNow = null;
  for (const t of (EVENTS.tooi || [])) {
    if (!matchWhen(t.when, { day: WORLD.day })) continue;
    const d = screenDist(cur, t.place);
    if (d >= UTA_FAR.length) continue;
    utaNow = { id: t.id, dist: d };
    uta(t.base || 880, UTA_FAR[d][0], UTA_FAR[d][1]);
    break;
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
// いまの 時間帯が toki に あうか。文字れつ でも ならび でも よい。
// じょうけん（when）でも、夜だけの 戸締り（gate）でも つかう
function tokiIs(toki) {
  const k = ambKind();
  return Array.isArray(toki) ? toki.indexOf(k) >= 0 : toki === k;
}

let lastAmb = '', lastPlace2 = '', ambCount = 0;
function ambientTick(dt) {
  if (!AC) return;
  setPlaceSound();              // 画面が 変わっていたら 水を つけ変える
  // **黒画面（額縁・日づけ・よる）では 夏の音を 止める。**
  // 「幻想郷の今」で セミが 鳴っていては 回想の 枠に ならない
  const st = scene && scene.q[scene.i];
  if (st && st.k === 'card') return;
  ambTimer -= dt;
  if (ambTimer > 0) return;
  const sc = SC[cur], inside = (sc.amb === 'in'), ki = (sc.amb === 'ki');
  const vol = ki ? 0.13 : (inside ? 0.035 : 0.06);
  ambTimer = (inside ? 1.6 : 0.7) + Math.random()*1.8;
  // 場所の音。**どれも 来ては 去る。鳴りっぱなしの ものは ひとつも 置かない**
  if (inside && Math.random() < 0.42) { fuurin(0.026); lastPlace2 = 'fuurin'; }
  const sw = SAWA[sc.amb] || SAWA.out;
  if (Math.random() < sw.p) { sawasawa(sw.v); lastPlace2 = 'sawa'; }

  const kind = ambKind();
  lastAmb = kind; ambCount++;
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
