// なつやすみ 2D見下ろし・試作（td）
// 実写をやめ、**同じ絵柄で揃う 見下ろしドット**へ。目標形は「8方向で動ける ほの暮しの庭」。
//  ・世界タイル … CC0 Top Down Adventure Assets（assets/tileset-world.png・16x16・7列）
//  ・キャラ    … いまの 東方ドット（chars.js・16x16・上段8人／チルノ=2）。向きは 左右反転のみ（当面）
//  ・カメラ    … プレイヤーを 追って スクロール（広い庭）。移動は 8方向。
'use strict';
const cv = document.getElementById('c'), g = cv.getContext('2d');
g.imageSmoothingEnabled = false;
const S = 3, T = 16, TS = T * S;             // 3倍・16pxタイル → 48px
const COLS = 7;                               // タイルセットの 横の枚数
const VW = cv.width, VH = cv.height;

// タイル idx = row*7 + col
const G = 0, G2 = 2, PATH = 22, WATER = 43, TREE = 44, PLANT = 73, FLOWER = 74, BUSH = 1;
const SOLID = new Set([WATER, TREE, BUSH]);

// --- 広い庭を つくる（決まった 乱数で 毎回おなじ）。ほの暮しの庭ふうに スクロールする 広さ
const MW = 44, MH = 34;
let seed = 20260809;
function rnd() { seed = (seed * 1103515245 + 12345) & 0x7fffffff; return seed / 0x7fffffff; }
const map = [];
for (let r = 0; r < MH; r++) {
  const row = [];
  for (let c = 0; c < MW; c++) row.push(rnd() < 0.16 ? G2 : G);   // 草（すこし ムラ）
  map.push(row);
}
function set(c, r, v) { if (c>=0&&r>=0&&c<MW&&r<MH) map[r][c] = v; }
function rect(c0, r0, w, h, v) { for (let r=r0;r<r0+h;r++) for (let c=c0;c<c0+w;c++) set(c,r,v); }
// まわりを 木で 囲う（2重）
for (let c=0;c<MW;c++){ set(c,0,TREE); set(c,1,TREE); set(c,MH-1,TREE); set(c,MH-2,TREE); }
for (let r=0;r<MH;r++){ set(0,r,TREE); set(1,r,TREE); set(MW-1,r,TREE); set(MW-2,r,TREE); }
// 池
rect(6, 6, 5, 4, WATER);
// 道（十字＋まわり道）
rect(3, 16, MW-6, 2, PATH);
rect(20, 3, 2, MH-6, PATH);
// はたけ（土。ひまわりを うえる 場所）
const FIELD = { c: 28, r: 20, w: 8, h: 6 };
rect(FIELD.c, FIELD.r, FIELD.w, FIELD.h, PATH);
function inField(c, r) { return c>=FIELD.c && c<FIELD.c+FIELD.w && r>=FIELD.r && r<FIELD.r+FIELD.h; }
// 木・花を すこし ちらす（道・水・畑の 上には 置かない）
for (let i=0;i<120;i++){
  const c = 2 + (rnd()*(MW-4)|0), r = 2 + (rnd()*(MH-4)|0);
  if (map[r][c] !== G && map[r][c] !== G2) continue;
  const k = rnd();
  map[r][c] = k < 0.45 ? TREE : (k < 0.6 ? BUSH : (k < 0.8 ? FLOWER : PLANT));
}

// ラジオ体操の 広場（あさ ここで 体操→スタンプ）。まわりを 草に して 木を どける
const RADIO = { c: 24, r: 14 };
rect(RADIO.c-1, RADIO.r-1, 3, 3, G);
// うちの まわり（拠点の 気配）。きれいな 単体タイルだけ 置く＝絵が くずれない
// 37=壺 47=木桶 28=木箱 33=たる。まわりを 草に して きれいに 置く
rect(16, 13, 6, 4, G);
const HOME_OBJS = [[17,14,37],[19,14,48],[16,14,28],[20,14,33]];  // 壺・木桶・木箱・たる
for (const [c,r,t] of HOME_OBJS) set(c,r,t);
[28,33,37,48].forEach(t => SOLID.add(t));   // 置いた ものは とおれない
const REST = { c: 18, r: 15 };              // 縁台（ひとやすみ→時間が すすむ）
function solidAtCell(c, r) { if (c<0||r<0||c>=MW||r>=MH) return true; return SOLID.has(map[r][c]); }
function solidAt(px, py) { return solidAtCell(Math.floor(px/TS), Math.floor(py/TS)); }

// --- 画像
const tiles = new Image(); tiles.src = 'assets/tileset-world.png';
const chars = new Image(); chars.src = 'data:image/png;base64,' + CHARS_B64;
let ready = 0; tiles.onload = () => ready++; chars.onload = () => ready++;

// 名まえ（ci → 表示名）。data/cast.json と 同じ ならび
const NAMES = { 0:'れいむ', 1:'まりさ', 2:'チルノ', 3:'だいようせい', 4:'ルーミア', 5:'リグル', 6:'ミスティア', 7:'けーね' };
// 話し手ID → 表示名（会話の [who, ことば] の who）
const WHO = { cirno:'チルノ', dai:'だいようせい', marisa:'まりさ', rumia:'ルーミア', wriggle:'リグル', mystia:'ミスティア', keine:'けーね', reimu:'れいむ' };

// --- プレイヤー（足もと＝下中央）と 立ってる 仲間。道の 交点あたりから。
// 仲間は そばで キーを 押すと 話す（P1と 同じ：近づいただけでは 始めない）。あたたかいトーン
const player = { x: 21 * TS, y: 17 * TS, ci: 2, face: 1, bob: 0, moving: false };
// 仲間の 会話は **時間帯で かわる**（asa／hiru／yugata／yoru）。同じ夏でも 一日で 表情が うつる
const npcs = [
  { ci: 3, x: 24 * TS, y: 12 * TS, sets: {   // だいようせい
    asa:    [['dai','おはよう、チルノちゃん'],['cirno','ん、おはよ'],['dai','きょうは なにを する？'],['cirno','うーん、かんがえ中！']],
    hiru:   [['dai','この にわ、ひろいねえ'],['cirno','ぜんぶ あたいの ばしょ！'],['dai','じゃあ てつだうよ'],['cirno','うん、たのむ！']],
    yugata: [['dai','そろそろ ゆうがた だね'],['cirno','もう そんな じかん？'],['dai','よるは はやめに ね'],['cirno','わかってるよ〜']],
    yoru:   [['dai','まだ おきてたの？'],['cirno','ほたるが きれいで'],['dai','わたしも みたいな'],['cirno','じゃあ いっしょに！']],
  } },
  { ci: 1, x: 14 * TS, y: 22 * TS, sets: {   // まりさ
    asa:    [['marisa','よお、はやいな'],['cirno','ラジオたいそう した？'],['marisa','これから いくとこ'],['cirno','いっしょに いこ！']],
    hiru:   [['marisa','いい てんきだ'],['cirno','おさんぽ びより！'],['marisa','はたけ、なんか うえたか？'],['cirno','ひまわり うえたいな']],
    yugata: [['marisa','ゆうやけ、きれいだな'],['cirno','うん、あかいね'],['marisa','なつって かんじだ'],['cirno','ずっと つづけば いいのに']],
    yoru:   [['marisa','よるは しずかだな'],['cirno','むしの こえが する'],['marisa','こわく ないか？'],['cirno','へ、へいきだもん！']],
  } },
  { ci: 5, x: 31 * TS, y: 23 * TS, sets: {   // リグル（はたけの そば）
    asa:    [['wriggle','あさは むしが げんきだ'],['cirno','ほんと？'],['wriggle','くさむらを のぞいて みな'],['cirno','うん、みてみる']],
    hiru:   [['wriggle','このあたり、むしが おおいぞ'],['cirno','つかまえて いい？'],['wriggle','あみが あればな'],['cirno','あみ、ほしいなあ']],
    yugata: [['wriggle','ゆうがたは ひぐらしが なく'],['cirno','かなかな…って やつ？'],['wriggle','そう、それ'],['cirno','なんだか せつない ね']],
    yoru:   [['wriggle','よるは かぶとむしの じかん'],['cirno','え、ほんと！？'],['wriggle','でも もう おそいぞ'],['cirno','うう、また こんど…']],
  } },
];
function timeKey(t) { if (t < 5) return 'yoru'; if (t < 11) return 'asa'; if (t < 16) return 'hiru'; if (t < 19) return 'yugata'; return 'yoru'; }
// 会話の えらび：まず **その時の できごと**（ひまわり・体操…）を 見て、なければ 時間帯
const flags = { daiThanked: false, marisaStamp: false, wriggleHotaru: false, introDone: false, everMizu: false, sawHanabi: false };
function pickLines(npc) {
  // だいようせい：はじめて ひまわりが さいた あとに 気づいて よろこぶ
  if (npc.ci === 3 && bloomTotal > 0 && !flags.daiThanked) {
    flags.daiThanked = true; save();
    return [['dai','わあ、ひまわりが さいてる！'],['cirno','あたいが そだてたの'],['dai','すごい！ おひさま みたい'],['cirno','えへへ、まいにち みずやり したから']];
  }
  // まりさ：体操を つづけたら ほめる
  if (npc.ci === 1 && taisoStamps >= 3 && !flags.marisaStamp) {
    flags.marisaStamp = true; save();
    return [['marisa','まいあさ たいそう、えらいな'],['cirno','スタンプ たまってきた！'],['marisa','なつやすみの かがみだ'],['cirno','へへーん']];
  }
  // リグル：蛍を たくさん つかまえたら
  if (npc.ci === 5 && caughtHotaru >= 5 && !flags.wriggleHotaru) {
    flags.wriggleHotaru = true; save();
    return [['wriggle','ほたる、いっぱい つかまえたな'],['cirno','よるの にわで ひかってた'],['wriggle','にがして やると また 光るぞ'],['cirno','うん、そうする']];
  }
  const s = npc.sets; return s[timeKey(tod)] || s.hiru || s.asa;
}
const cam = { x: 0, y: 0 };
const TALK_R = TS * 1.4;                 // これより 近ければ 話しかけられる
let talkNpc = null, talkIdx = 0, talkLines = null;   // 相手・何行目・いま 表示中の 台本

// --- 昼夜（ほの暮しの庭：昼は ほっこり／夜は すこし 不安）。時間で 空気の色が かわる
// tod = 時刻(0〜24)。**時間は 勝手に 流れない**。行動した ぶんだけ passTime() で すすむ。朝8時から
let tod = 8;
// ひかりの色（かける＝multiply。255で そのまま・小さいほど 暗い/色づく）
const LIGHT = [
  { t: 0,  c: [58, 68, 120] },   // まよなか（ふかい あお・くらい）
  { t: 5,  c: [70, 78, 122] },   // よあけ前
  { t: 6.5,c: [205, 150, 138] }, // よあけ（あかね）
  { t: 8,  c: [255, 250, 240] }, // あさ
  { t: 12, c: [255, 255, 255] }, // まひる
  { t: 16, c: [255, 244, 224] }, // ひるさがり
  { t: 18, c: [255, 168, 108] }, // ゆうやけ
  { t: 19.5,c:[150, 110, 145] }, // たそがれ
  { t: 21, c: [70, 80, 132] },   // よる
  { t: 24, c: [58, 68, 120] },   // → まよなかへ つなぐ
];
function ambient(h) {
  for (let i = 0; i < LIGHT.length - 1; i++) {
    const a = LIGHT[i], b = LIGHT[i + 1];
    if (h >= a.t && h <= b.t) {
      const k = (h - a.t) / (b.t - a.t);
      return [0,1,2].map(j => Math.round(a.c[j] + (b.c[j] - a.c[j]) * k));
    }
  }
  return [255, 255, 255];
}
function todName(h) {
  if (h < 5) return 'まよなか'; if (h < 7) return 'よあけ'; if (h < 11) return 'あさ';
  if (h < 15) return 'ひる'; if (h < 17) return 'ひるさがり'; if (h < 19) return 'ゆうがた';
  if (h < 21) return 'よる'; return 'よる';
}
function isNight() { return tod >= 19 || tod < 4.5; }

// --- 蛍（よるだけ）。ひかりの点なので コードで きれいに 描ける＝絵柄が くずれない。
//   夜の 不安と 幻想の 両方。近づいて スペースで つかまえる（P1と 同じ：キーで）
const flies = [];               // {x,y,ph,vx,vy,life}  life:0→1 で ふわっと 出て 消える
const FLY_MAX = 16;
const FLY_R = TS * 0.95;        // これより 近ければ つかまえられる
let caughtHotaru = 0;
function spawnFly() {
  // プレイヤーの まわり（1画面ぶん）で 草の上に わく
  for (let tries = 0; tries < 12; tries++) {
    const c = Math.floor(player.x/TS) + ((rnd()*20|0) - 10);
    const r = Math.floor(player.y/TS) + ((rnd()*14|0) - 7);
    if (c<1||r<1||c>=MW-1||r>=MH-1) continue;
    const t = map[r][c];
    if (t === G || t === G2 || t === WATER || t === FLOWER || t === PLANT) {
      flies.push({ x: c*TS + TS/2, y: r*TS + TS/2, ph: rnd()*6.28, vx: 0, vy: 0, life: 0 });
      return;
    }
  }
}

// --- こよみ（ぼくなつの 魂：夏休みは すぎてゆく）
let day = 1;                       // なつやすみ 何日目
const SUMMER_DAYS = 31;
let garden = [];                  // はたけ（{c,r,stage,watered}）
let dayMsg = '', dayMsgT = 0;     // 「◯日目」の 短い しらせ
// --- 自由研究の きろく（えにっき＋ずかん）。テキストだけ＝絵が いらない
let diary = [];                  // [{d, text}]  その日の しめくくり
let today = { hotaru: 0, planted: 0, watered: 0, bloomed: 0, taiso: false, mizu: false };  // 今日 やったこと
let bloomTotal = 0;             // これまで さかせた ひまわり
let diaryOpen = false;          // Nキーで えにっきを ひらく
let daySub = '';                // 日の しらせの 2行目（あさごはん／のこり日数）
// --- その日の リズム：あさ 体操→スタンプ、よる おそくなると けーねが お迎え（門限）
let taisoToday = false, taisoStamps = 0;   // 今日 体操したか／スタンプ 総数
let mukaeShown = false;         // その夜 いちど お迎えが 来たか
const MUKAE = { onEnd: 'sleep', lines: [   // 夜ふかしすると 慧音が むかえに 来る
  ['keine', 'チルノ、こんな じかんまで そとに いたの？'],
  ['cirno', 'ほたるが きれいで、つい…'],
  ['keine', 'もう おそいよ。さあ、おうちへ かえろう'],
] };
const INTRO = { onEnd: 'none', lines: [    // はじめの 導き（初回だけ）
  ['cirno', 'わ〜、なつやすみだ！'],
  ['cirno', 'ことしは おじいちゃんちの うらの にわ'],
  ['cirno', 'ひまわり うえたり、むしとったり…'],
  ['cirno', '（スペース＝はなす／しらべる、Z＝ねる、N＝えにっき）'],
] };
function nokori() { return Math.max(0, SUMMER_DAYS - day + 1); }
// --- 天気（その日で 決まる・晴れ／雨）。雨は 空気を かえ、**畑に みずを やる**
function weatherOf(d) { const x = Math.sin(d * 127.1) * 43758.5453; return (x - Math.floor(x)) < 0.28; }
function isRainy() { return weatherOf(day); }
// --- 夏まつりの 花火（5日ごとの 晴れた夜）。花火は 粒子＝コードで きれいに 描ける
function isFestival() { return day % 5 === 0 && !isRainy(); }
const fireworks = [];           // {x,y,peakY,state,parts,hue}
let fwTimer = 0;
function launchFirework() {
  fireworks.push({ x: VW*(0.2 + rnd()*0.6), y: VH*0.92, peakY: VH*(0.12 + rnd()*0.26),
                   state: 'rise', parts: [], hue: rnd()*360 });
}
// 時間を すすめる（行動した ぶんだけ）。よなかを またいだら つぎの日へ。
// **夜おそく（22時）に なると 慧音が お迎え＝門限**（夜中も ずっとは 遊べない）
function passTime(h) {
  tod += h;
  while (tod >= 24) { tod -= 24; newDay(); }
  if (tod >= 22 && !mukaeShown && !sleepPhase && !talkNpc) { mukaeShown = true; talkNpc = MUKAE; talkIdx = 0; talkLines = MUKAE.lines; }
}
function newDay() {
  recordDiary();                 // その日の しめくくりを えにっきへ
  day++;
  today = { hotaru: 0, planted: 0, watered: 0, bloomed: 0, taiso: false, mizu: false };
  taisoToday = false; mukaeShown = false;      // あたらしい日：体操やりなおし・お迎えも リセット
  growGarden();                  // 朝、みずやりした 苗が のびる（さいたら 今日の えにっきに のる）
  if (isRainy()) for (const p of garden) p.watered = true;   // 雨の日は 畑に みずが やれる
  const morning = tod >= 5 && tod < 10;
  dayMsg = `${day}日目`;
  daySub = isFestival() ? 'きょうは なつまつり！ よるに はなびが あがる'
         : isRainy() ? 'あめ ふり。はたけには めぐみの あめ'
         : (morning ? 'あさごはんを たべた ・ そとへ でよう' : `なつやすみ のこり ${nokori()}日`);
  dayMsgT = 3.4;
  save();
}
function recordDiary() {
  const p = [];
  if (today.taiso) p.push('ラジオたいそうを した');
  if (today.mizu) p.push('いけで みずあそびを した');
  if (today.hotaru) p.push(`ほたるを ${today.hotaru}ひき つかまえた`);
  if (today.planted) p.push(`たねを ${today.planted}つ まいた`);
  if (today.watered) p.push('はたけに みずを あげた');
  if (today.bloomed) p.push('ひまわりが さいた！');
  if (!p.length) p.push('のんびり すごした');
  diary.push({ d: day, text: p.join('。') + '。' });
  if (diary.length > 40) diary.shift();
}
// ラジオ体操（あさ 5〜9時に 広場で）→ スタンプ
function doTaiso() {
  taisoToday = true; today.taiso = true; taisoStamps++;
  if (typeof taisoJingle === 'function') taisoJingle();
  dayMsg = 'ラジオたいそう！ スタンプ ゲット'; daySub = `スタンプ ${taisoStamps}こ`; dayMsgT = 2.6;
  passTime(0.5); save();
}
function canTaiso() { return tod >= 5 && tod < 9 && !taisoToday; }
// 縁台で ひとやすみ（時間を すこし すすめる＝ゆうがた・よるへ 行ける 手だて）
function doRest() { passTime(2.0); dayMsg = 'ひとやすみ…'; daySub = 'なつの においが する'; dayMsgT = 2.0; save(); }
// --- 水あそび（池のふちで）。さざ波は コードで えがく＝絵が くずれない
const ripples = [];             // {x,y,t}  ひろがって 消える 輪
function doMizu(wx, wy) {
  for (let i = 0; i < 3; i++) ripples.push({ x: wx + (rnd()-0.5)*14, y: wy + (rnd()-0.5)*10, t: -i*0.18 });
  today.mizu = true; flags.everMizu = true;
  if (typeof mizuSfx === 'function') mizuSfx();
  dayMsg = 'つめたくて きもちいい！'; daySub = ''; dayMsgT = 2.0;
  passTime(0.5); save();
}
// 足もとの となりに 水が あるか（あれば その 水セルの 中心を かえす）
function waterNextTo(pc, pr) {
  for (const [dc, dr] of [[1,0],[-1,0],[0,1],[0,-1]]) {
    const c = pc+dc, r = pr+dr;
    if (r>=0 && r<MH && c>=0 && c<MW && map[r][c] === WATER) return { x: (c+0.5)*TS, y: (r+0.5)*TS };
  }
  return null;
}
// --- ねむる（Zキー）。まっくらに とけて つぎの朝へ
let sleepPhase = 0;              // 0=起きてる。2.0→0 へ。1.0で 朝に とぶ
function startSleep() { if (sleepPhase <= 0 && !talkNpc) sleepPhase = 2.0; }
// --- セーブ／ロード（この夏が つづいてる 感じ）
function save() {
  try { localStorage.setItem('natsuyasumi_td',
    JSON.stringify({ day, tod, caughtHotaru, garden, diary, today, bloomTotal, taisoStamps, taisoToday, flags, px: player.x, py: player.y })); } catch (e) {}
}
function load() {
  try {
    const s = JSON.parse(localStorage.getItem('natsuyasumi_td') || 'null');
    if (!s) return;
    day = s.day || 1; tod = s.tod == null ? 8 : s.tod; caughtHotaru = s.caughtHotaru || 0;
    if (Array.isArray(s.garden)) garden = s.garden;
    if (Array.isArray(s.diary)) diary = s.diary;
    if (s.today) today = s.today;
    bloomTotal = s.bloomTotal || 0;
    taisoStamps = s.taisoStamps || 0; taisoToday = !!s.taisoToday;
    if (s.flags) Object.assign(flags, s.flags);
    if (s.px != null) { player.x = s.px; player.y = s.py; }
  } catch (e) {}
}
// はたけの成長：まえの日に みずを あげた 苗が ひと段階 のびる（0種→1芽→2葉→3つぼみ→4さいた）
function growGarden() {
  for (const p of garden) {
    if (p.watered && p.stage < 4) { p.stage++; if (p.stage === 4) { bloomTotal++; today.bloomed++; } }
    p.watered = false;             // あたらしい日：また みずやりが いる
  }
}
function plotAt(c, r) { return garden.find(p => p.c === c && p.r === r); }

// --- 入力。act は 決定（スペース／エンター）。おしっぱなしでは 進まない（1回ぶん）
const keys = {};
let act = false;
let showHud = true;               // 時計・こよみの 表示。**最後に false にすれば 消える**（Hキーで 切替）
let mode = 'title';               // 'title'（はじめる前）| 'play'（あそぶ）| 'ending'（夏の おわり）
let endT = 0;                     // エンディングの 経過（フェード用）
addEventListener('keydown', e => {
  initAudio();                    // 最初の キーで 夏の音を 起こす（自動再生ポリシー対策）
  if (e.key.startsWith('Arrow')||e.key===' ') e.preventDefault();
  if (!e.repeat && (e.key===' '||e.key==='Enter')) act = true;
  if (!e.repeat && (e.key==='z'||e.key==='Z')) startSleep();
  if (!e.repeat && (e.key==='h'||e.key==='H')) showHud = !showHud;
  if (!e.repeat && (e.key==='n'||e.key==='N')) diaryOpen = !diaryOpen;
  if (!e.repeat && (e.key==='m'||e.key==='M')) toggleMute();
  keys[e.key.toLowerCase()] = true;
});
addEventListener('keyup',   e => { keys[e.key.toLowerCase()] = false; });

function drawTile(idx, dx, dy) {
  const sx = (idx % COLS) * T, sy = Math.floor(idx / COLS) * T;
  g.drawImage(tiles, sx, sy, T, T, dx, dy, TS, TS);
}
function drawShadow(x, y) {
  g.save(); g.fillStyle = 'rgba(10,20,8,0.28)';
  g.beginPath(); g.ellipse(x, y - 3, TS*0.28, TS*0.12, 0, 0, Math.PI*2); g.fill(); g.restore();
}
function drawChar(ci, x, y, face) {
  const sx = (ci % 8) * 16, sy = Math.floor(ci / 8) * 16, h = TS + 8;
  g.save(); g.translate(Math.round(x), Math.round(y));
  if (face < 0) g.scale(-1, 1);
  g.drawImage(chars, sx, sy, 16, 16, -h/2, -h + 2, h, h);
  g.restore();
}

let last = performance.now();
function loop(now) {
  const dt = Math.min(0.05, (now - last) / 1000); last = now;
  if (ready < 2) { g.fillStyle = '#0d120b'; g.fillRect(0,0,VW,VH); requestAnimationFrame(loop); return; }

  // タイトル／エンディングでは 世界を うしろに 見せる だけ（更新しない）
  if (mode === 'title') { if (act) { mode = 'play'; initAudio(); if (!flags.introDone) { flags.introDone = true; talkNpc = INTRO; talkIdx = 0; talkLines = INTRO.lines; save(); } act = false; } }
  if (mode === 'ending') { endT += dt; if (act && endT > 1.2) { removeEventListener('beforeunload', save); try { localStorage.removeItem('natsuyasumi_td'); } catch (e) {} location.reload(); return; } }

  let near = null, nearFly = null, onField = false, fieldPlot = null, nearRadio = false, waterSpot = null, nearRest = false, pc = 0, pr = 0;
  if (mode === 'play') {
    // 時間は **勝手には 進まない**（急かさない）。ねむり中だけ つぎの朝へ とぶ
    if (sleepPhase > 0) {
      const before = sleepPhase; sleepPhase -= dt;
      if (before > 1.0 && sleepPhase <= 1.0) { tod = 7; newDay(); }
      if (sleepPhase < 0) sleepPhase = 0;
    }
    if (dayMsgT > 0) dayMsgT -= dt;
    ambientTick(dt, tod);           // 夏の音（時間帯で 鳴き分け）

    // うごく（8方向）。足もとで あたり判定、軸ごとに 止める。話している あいだは 足を とめる
    let ax = 0, ay = 0;
    if (!talkNpc && !sleepPhase && !diaryOpen) {
      if (keys['arrowleft']||keys['a']) ax -= 1;
      if (keys['arrowright']||keys['d']) ax += 1;
      if (keys['arrowup']||keys['w']) ay -= 1;
      if (keys['arrowdown']||keys['s']) ay += 1;
    }
    if (ax || ay) { const m = Math.hypot(ax, ay); ax/=m; ay/=m; }
    const spd = 175 * dt;
    const nx = player.x + ax*spd, ny = player.y + ay*spd;
    if (ax && !solidAt(nx, player.y)) player.x = nx;
    if (ay && !solidAt(player.x, ny)) player.y = ny;
    if (ax > 0.1) player.face = 1; else if (ax < -0.1) player.face = -1;
    player.moving = !!(ax || ay);
    player.bob += dt * (player.moving ? 11 : 2);

    // 蛍：よるだけ ふわふわ わく（雨の 夜は でない）。昼は しずかに 消える
    if (isNight() && !isRainy() && flies.length < FLY_MAX && rnd() < 0.06) spawnFly();
    let flyD = FLY_R;
    for (let i = flies.length - 1; i >= 0; i--) {
      const f = flies[i];
      f.ph += dt * 1.6;
      if (rnd() < 0.03) { f.vx = (rnd()-0.5)*18; f.vy = (rnd()-0.5)*18; }
      f.x += f.vx * dt; f.y += f.vy * dt;
      f.life += dt * (isNight() ? 0.8 : -1.2);
      if (f.life <= 0 && !isNight()) { flies.splice(i, 1); continue; }
      f.life = clamp(f.life, 0, 1);
      const d = Math.hypot(f.x - player.x, f.y - player.y);
      if (d < flyD) { flyD = d; nearFly = f; }
    }
    // そばの 仲間（話しかけ用）。いちばん 近いのを ひとり
    let bestD = TALK_R;
    for (const n of npcs) { const d = Math.hypot(n.x - player.x, n.y - player.y); if (d < bestD) { bestD = d; near = n; } }
    // 足もとの はたけ／体操の 広場
    pc = Math.floor(player.x/TS); pr = Math.floor(player.y/TS);
    onField = inField(pc, pr);
    fieldPlot = onField ? plotAt(pc, pr) : null;
    nearRadio = Math.hypot((RADIO.c+0.5)*TS - player.x, (RADIO.r+0.5)*TS - player.y) < TS*1.3;
    nearRest = Math.hypot((REST.c+0.5)*TS - player.x, (REST.r+0.5)*TS - player.y) < TS*1.3;
    waterSpot = waterNextTo(pc, pr);          // 池の ふちに いるか
    // さざ波を すすめる（ひろがって 消える）
    for (let i = ripples.length - 1; i >= 0; i--) { ripples[i].t += dt; if (ripples[i].t > 1.1) ripples.splice(i, 1); }
    // 夏まつりの 花火（晴れた 祭りの夜）。あがっては ひらいて 消える
    if (isFestival() && isNight()) { fwTimer -= dt; if (fwTimer <= 0) { launchFirework(); fwTimer = 1.3 + rnd()*1.7; } }
    for (let i = fireworks.length - 1; i >= 0; i--) {
      const fw = fireworks[i];
      if (fw.state === 'rise') {
        fw.y -= 260 * dt;
        if (fw.y <= fw.peakY) {
          fw.state = 'burst'; flags.sawHanabi = true;
          const n = 34 + (rnd()*16|0);
          for (let k = 0; k < n; k++) { const a = rnd()*6.283, sp = 40 + rnd()*130;
            fw.parts.push({ x: fw.x, y: fw.y, vx: Math.cos(a)*sp, vy: Math.sin(a)*sp, life: 1 }); }
        }
      } else {
        let alive = 0;
        for (const p of fw.parts) { p.x += p.vx*dt; p.y += p.vy*dt; p.vy += 60*dt; p.vx *= 0.985; p.life -= dt*0.7; if (p.life > 0) alive++; }
        if (!alive) fireworks.splice(i, 1);
      }
    }
    // キーで 会話／体操／うえる・みずやり／水あそび／蛍つかまえ（近づいただけでは 始めない・P1）
    if (act && diaryOpen) { diaryOpen = false; act = false; }
    if (act) {
      if (talkNpc) {
        if (++talkIdx >= talkLines.length) {
          const end = talkNpc.onEnd; talkNpc = null; talkIdx = 0; talkLines = null;
          if (end === 'sleep') startSleep();
          else if (end === 'none') { /* 導き・独白は 時間を 使わない */ }
          else passTime(1.0);
        }
      }
      else if (near) { talkNpc = near; talkIdx = 0; talkLines = pickLines(near); }
      else if (nearRadio && canTaiso()) { doTaiso(); }
      else if (onField) {
        if (!fieldPlot) { garden.push({ c: pc, r: pr, stage: 0, watered: false }); today.planted++; passTime(1.0); save(); }
        else if (!fieldPlot.watered) { fieldPlot.watered = true; today.watered++; passTime(1.0); save(); }
      }
      else if (waterSpot) { doMizu(waterSpot.x, waterSpot.y); }
      else if (nearRest) { doRest(); }
      else if (nearFly) { flies.splice(flies.indexOf(nearFly), 1); caughtHotaru++; today.hotaru++; nearFly = null; passTime(0.5); save(); }
      act = false;
    }
    // 夏の おわり（さいごの日を こえたら）
    if (day > SUMMER_DAYS) { mode = 'ending'; endT = 0; }
  }

  // カメラ：いつも プレイヤーを 中央に（タイトルでも 主人公が 見える）
  cam.x = clamp(Math.round(player.x - VW/2), 0, MW*TS - VW);
  cam.y = clamp(Math.round(player.y - VH/2), 0, MH*TS - VH);

  // えがく（見えている ぶんだけ）
  g.fillStyle = '#0d120b'; g.fillRect(0,0,VW,VH);
  const c0 = Math.floor(cam.x/TS), r0 = Math.floor(cam.y/TS);
  for (let r = r0; r <= r0 + VH/TS + 1 && r < MH; r++) {
    for (let c = c0; c <= c0 + VW/TS + 1 && c < MW; c++) {
      if (r < 0 || c < 0) continue;
      const t = map[r][c], dx = c*TS - cam.x, dy = r*TS - cam.y;
      if (t !== G && t !== G2) drawTile(G, dx, dy);   // 透ける絵は 下に 草
      drawTile(t, dx, dy);
      if (inField(c, r)) drawFurrow(dx, dy);          // 畑は うねを ひく
    }
  }
  // ラジオ体操の 広場（丸い ゴザ）。あさ・未済なら 「たいそう」の ふだ
  {
    const mx = (RADIO.c+0.5)*TS - cam.x, my = (RADIO.r+0.5)*TS - cam.y;
    g.save();
    g.fillStyle = 'rgba(210,180,120,0.5)'; g.strokeStyle = 'rgba(150,115,60,0.5)'; g.lineWidth = 2;
    g.beginPath(); g.ellipse(mx, my, TS*0.9, TS*0.55, 0, 0, 6.283); g.fill(); g.stroke();
    if (canTaiso() && mode === 'play') {
      g.fillStyle = 'rgba(20,26,40,0.7)';
      g.fillRect(mx - 52, my - TS - 20, 104, 22);
      g.fillStyle = '#ffe6a8'; g.font = '600 13px system-ui'; g.textAlign = 'center';
      g.fillText('ラジオたいそう', mx, my - TS - 4); g.textAlign = 'left';
    }
    g.restore();
  }
  // 水あそびの さざ波（水の 上に ひろがる 輪）
  for (const rp of ripples) {
    if (rp.t < 0) continue;
    const rx = rp.x - cam.x, ry = rp.y - cam.y, rad = 4 + rp.t*22, a = (1 - rp.t/1.1) * 0.5;
    g.save(); g.strokeStyle = `rgba(220,240,255,${a})`; g.lineWidth = 2;
    g.beginPath(); g.ellipse(rx, ry, rad, rad*0.6, 0, 0, 6.283); g.stroke(); g.restore();
  }
  // 縁台（木の ベンチ）。ひとやすみの 場所
  {
    const bx = (REST.c+0.5)*TS - cam.x, by = (REST.r+0.6)*TS - cam.y;
    g.save();
    g.fillStyle = 'rgba(10,20,8,0.22)'; g.beginPath(); g.ellipse(bx, by+9, TS*0.5, TS*0.15, 0, 0, 6.283); g.fill();
    g.fillStyle = '#7a5230'; g.fillRect(bx - TS*0.5, by - 6, TS, 8);
    g.fillStyle = '#5c3d22'; g.fillRect(bx - TS*0.42, by + 2, 4, 9); g.fillRect(bx + TS*0.42 - 4, by + 2, 4, 9);
    g.restore();
  }
  // y で ならべて 前後（キャラ＋ひまわり を 足もとで ソート）
  const plants = garden.map(p => ({ x: p.c*TS + TS/2, y: p.r*TS + TS, plant: p }));
  const ents = [...npcs, player, ...plants].sort((a,b) => a.y - b.y);
  for (const e of ents) {
    const ex = e.x - cam.x, ey = e.y - cam.y;
    if (e.plant) { drawPlant(e.plant.stage, ex, ey, e.plant.watered); continue; }
    drawShadow(ex, ey);
    const off = e === player && player.moving ? Math.abs(Math.sin(player.bob)) * 3 : 0;
    drawChar(e.ci, ex, ey - off, e.face || 1);
  }
  // 昼夜：ひかりの色を かける（キャラ・地面ぜんぶ 染める）。UIより 下、世界より 上
  const [lr, lg, lb] = ambient(tod);
  if (lr < 255 || lg < 255 || lb < 255) {
    g.save(); g.globalCompositeOperation = 'multiply';
    g.fillStyle = `rgb(${lr},${lg},${lb})`; g.fillRect(0, 0, VW, VH);
    g.restore();
  }
  // 夜は すみを すこし くらく（不安げな 気配。まわりが 見えにくい）
  const dark = tod >= 19 || tod < 5 ? 0.34 : (tod >= 18 || tod < 6 ? 0.16 : 0);
  if (dark > 0) {
    const vg = g.createRadialGradient(VW/2, VH/2, VH*0.28, VW/2, VH/2, VH*0.78);
    vg.addColorStop(0, 'rgba(4,6,16,0)'); vg.addColorStop(1, `rgba(4,6,16,${dark})`);
    g.fillStyle = vg; g.fillRect(0, 0, VW, VH);
  }
  // 蛍の あかり（くらさの上で 光る）。lighter で ふわっと 加算
  if (flies.length) {
    g.save(); g.globalCompositeOperation = 'lighter';
    for (const f of flies) {
      const fx = f.x - cam.x, fy = f.y - cam.y;
      if (fx < -20 || fy < -20 || fx > VW+20 || fy > VH+20) continue;
      const pulse = 0.55 + 0.45 * Math.sin(f.ph);
      const a = f.life * pulse;
      const rad = TS * (0.5 + 0.25 * pulse);
      const gr = g.createRadialGradient(fx, fy, 0, fx, fy, rad);
      gr.addColorStop(0, `rgba(220,255,150,${0.9*a})`);
      gr.addColorStop(0.4, `rgba(150,230,90,${0.45*a})`);
      gr.addColorStop(1, 'rgba(120,200,70,0)');
      g.fillStyle = gr; g.beginPath(); g.arc(fx, fy, rad, 0, 6.284); g.fill();
      g.fillStyle = `rgba(245,255,210,${a})`; g.beginPath(); g.arc(fx, fy, 1.6, 0, 6.284); g.fill();
    }
    g.restore();
  }
  // 花火（空に・加算で 光る）
  if (fireworks.length) {
    g.save(); g.globalCompositeOperation = 'lighter';
    for (const fw of fireworks) {
      if (fw.state === 'rise') {
        g.fillStyle = 'rgba(255,240,200,0.9)';
        g.beginPath(); g.arc(fw.x, fw.y, 2.2, 0, 6.283); g.fill();
      } else {
        for (const p of fw.parts) {
          if (p.life <= 0) continue;
          g.fillStyle = `hsla(${fw.hue},90%,65%,${Math.max(0,p.life)})`;
          g.beginPath(); g.arc(p.x, p.y, 2.4, 0, 6.283); g.fill();
        }
      }
    }
    g.restore();
  }

  // 雨：空を くもらせ、ななめの すじを ふらせる（コード描画）。畑には めぐみ
  if (isRainy()) {
    g.fillStyle = 'rgba(70,90,120,0.20)'; g.fillRect(0, 0, VW, VH);
    g.save(); g.strokeStyle = 'rgba(185,205,230,0.32)'; g.lineWidth = 1;
    const sp = now * 0.7;
    for (let i = 0; i < 120; i++) {
      const x = ((i*89 + sp) % (VW + 40)) - 20;
      const y = ((i*57 + sp*1.7) % (VH + 40)) - 20;
      g.beginPath(); g.moveTo(x, y); g.lineTo(x - 5, y + 13); g.stroke();
    }
    g.restore();
  }
  if (typeof setRainLevel === 'function') setRainLevel(isRainy() && mode === 'play' ? 0.05 : 0);

  // --- HUD（ひかりの上。いつも 読める）。**showHud=false で ぜんぶ 消える**（Hキーで 切替・最後は 既定オフに）
  if (showHud && mode === 'play') {
    g.fillStyle = 'rgba(230,238,220,0.9)'; g.font = '600 15px system-ui';
    g.fillText('うらの にわ', 14, 26);
    g.fillStyle = 'rgba(230,238,220,0.5)'; g.font = '12px system-ui';
    g.fillText('Zで ねる ・ Nで えにっき ・ Hで 表示けし', 14, 44);
    // とけい（右上）：時刻と じかんたい
    const hh = Math.floor(tod), mm = Math.floor((tod % 1) * 60);
    const clk = `${hh}:${String(mm).padStart(2,'0')}  ${todName(tod)}${isRainy() ? ' ・ あめ' : ''}`;
    g.font = '600 15px system-ui'; g.textAlign = 'right';
    g.fillStyle = 'rgba(8,12,9,0.45)';
    const cw = g.measureText(clk).width; g.fillRect(VW - cw - 26, 10, cw + 16, 24);
    g.fillStyle = 'rgba(246,250,242,0.95)'; g.fillText(clk, VW - 14, 27); g.textAlign = 'left';
    // こよみ：なつやすみ のこり N日（時計の下）
    g.font = '600 13px system-ui'; g.textAlign = 'right';
    g.fillStyle = 'rgba(255,236,190,0.92)';
    g.fillText(`${day}日目 ・ のこり ${nokori()}日`, VW - 14, 47); g.textAlign = 'left';
    // つかまえた 蛍の かず（夜／持っていれば）
    if (caughtHotaru > 0 || isNight()) {
      g.font = '600 13px system-ui'; g.textAlign = 'right';
      g.fillStyle = 'rgba(220,255,150,0.9)';
      g.fillText(`ほたる ${caughtHotaru}`, VW - 14, 66); g.textAlign = 'left';
    }
  }

  if (mode === 'play') {
    // 会話の まど／足もとの したこと（はなす・たいそう・うえる・みずやり・つかまえる）
    if (talkNpc) drawSay(talkLines[talkIdx]);
    else {
      let lbl = null;
      if (near) lbl = '▶ はなす';
      else if (nearRadio && canTaiso()) lbl = '▶ たいそうする';
      else if (onField) lbl = !fieldPlot ? '▶ うえる' : (!fieldPlot.watered ? '▶ みずやり' : (fieldPlot.stage >= 4 ? 'さいた！' : 'すくすく…'));
      else if (waterSpot) lbl = '▶ みずあそび';
      else if (nearRest) lbl = '▶ ひとやすみ';
      else if (nearFly) lbl = '▶ つかまえる';
      if (lbl) {
        g.fillStyle = 'rgba(8,12,9,0.5)'; g.fillRect(0, VH-40, VW, 40);
        g.fillStyle = 'rgba(246,250,242,0.95)'; g.font = '600 17px system-ui'; g.textAlign = 'center';
        g.fillText(lbl, VW/2, VH-15); g.textAlign = 'left';
      }
    }
    if (diaryOpen) drawDiary();        // えにっき／ずかん（Nで ひらく）
    // 「◯日目」の しらせ（すこし 出て 消える）
    if (dayMsgT > 0) {
      const a = Math.min(1, dayMsgT) * Math.min(1, (3.4 - dayMsgT) * 3);
      g.save(); g.globalAlpha = Math.max(0, a);
      g.fillStyle = 'rgba(8,10,20,0.7)'; g.fillRect(0, VH/2 - 40, VW, 80);
      g.fillStyle = '#ffe6a8'; g.font = '600 26px system-ui'; g.textAlign = 'center';
      g.fillText(dayMsg, VW/2, VH/2 - 2);
      if (daySub) { g.fillStyle = 'rgba(240,244,255,0.85)'; g.font = '15px system-ui'; g.fillText(daySub, VW/2, VH/2 + 24); }
      g.textAlign = 'left'; g.restore();
    }
    // ねむり：まっくらに とけて つぎの朝へ
    if (sleepPhase > 0) {
      const a = 1 - Math.abs(sleepPhase - 1.0);
      g.fillStyle = `rgba(0,0,0,${a})`; g.fillRect(0, 0, VW, VH);
      if (a > 0.6) {
        g.fillStyle = `rgba(230,238,250,${(a-0.6)/0.4*0.8})`;
        g.font = '600 20px system-ui'; g.textAlign = 'center';
        g.fillText('…zzz', VW/2, VH/2); g.textAlign = 'left';
      }
    }
  }
  else if (mode === 'title') drawTitle(now);
  else if (mode === 'ending') drawEnding(now);

  act = false;                         // 1フレームで つかいきる
  requestAnimationFrame(loop);
}
function drawSay(line) {
  const bx = 70, by = VH - 118, bw = VW - 140, bh = 92, r = 14;
  g.save();
  g.fillStyle = 'rgba(10,14,26,0.82)';
  g.beginPath(); g.moveTo(bx+r,by); g.arcTo(bx+bw,by,bx+bw,by+bh,r); g.arcTo(bx+bw,by+bh,bx,by+bh,r);
  g.arcTo(bx,by+bh,bx,by,r); g.arcTo(bx,by,bx+bw,by,r); g.fill();
  g.strokeStyle = 'rgba(180,200,230,0.28)'; g.lineWidth = 1; g.stroke();
  g.fillStyle = '#ffe6a8'; g.font = '600 18px system-ui'; g.fillText(WHO[line[0]] || line[0], bx+24, by+30);
  g.fillStyle = '#eef3ff'; g.font = '20px system-ui'; g.fillText(line[1], bx+24, by+62);
  g.fillStyle = 'rgba(230,238,250,0.5)'; g.font = '13px system-ui';
  g.fillText('スペースで つぎへ', bx+bw-150, by+bh-12);
  g.restore();
}
// 畑の うね（土に ほそい 線）
function drawFurrow(dx, dy) {
  g.save(); g.strokeStyle = 'rgba(74,48,26,0.35)'; g.lineWidth = 2;
  for (let i = 1; i <= 2; i++) { const yy = dy + TS*i/3; g.beginPath(); g.moveTo(dx+4, yy); g.lineTo(dx+TS-4, yy); g.stroke(); }
  g.restore();
}
// ひまわり（コードで えがく＝絵柄が くずれない）。(x,y)＝足もと。0種→1芽→2葉→3つぼみ→4さいた
function drawPlant(stage, x, y, watered) {
  g.save();
  g.fillStyle = 'rgba(10,20,8,0.22)'; g.beginPath(); g.ellipse(x, y-2, 8, 3, 0, 0, 6.283); g.fill();
  if (stage === 0) {                                  // 種（つち の もり）
    g.fillStyle = watered ? '#553720' : '#6b4a2a';
    g.beginPath(); g.ellipse(x, y-3, 6, 4, 0, 0, 6.283); g.fill();
    g.restore(); return;
  }
  const H = [0, 12, 24, 32, 38][stage];
  g.strokeStyle = '#3f7a2e'; g.lineWidth = 3; g.lineCap = 'round';
  g.beginPath(); g.moveTo(x, y-2); g.lineTo(x, y-H); g.stroke();
  if (stage === 1) {                                  // 双葉
    g.fillStyle = '#6cbf4a';
    g.beginPath(); g.ellipse(x-3, y-H+2, 4, 2.5, -0.6, 0, 6.283); g.fill();
    g.beginPath(); g.ellipse(x+3, y-H+2, 4, 2.5,  0.6, 0, 6.283); g.fill();
  } else if (stage >= 2) {                             // 葉
    g.fillStyle = '#5aa83e';
    g.beginPath(); g.ellipse(x-6, y-H*0.5,  7, 3.5, -0.5, 0, 6.283); g.fill();
    g.beginPath(); g.ellipse(x+6, y-H*0.62, 7, 3.5,  0.5, 0, 6.283); g.fill();
  }
  if (stage === 3) {                                  // つぼみ
    g.fillStyle = '#4f9a37'; g.beginPath(); g.arc(x, y-H, 6, 0, 6.283); g.fill();
    g.fillStyle = '#f0b429'; g.beginPath(); g.arc(x, y-H, 2.5, 0, 6.283); g.fill();
  }
  if (stage === 4) {                                  // ひまわり さいた
    const cx = x, cy = y-H, R = 11;
    g.fillStyle = '#f7c948';
    for (let i = 0; i < 12; i++) { const a = i/12*6.283; g.beginPath(); g.ellipse(cx+Math.cos(a)*R, cy+Math.sin(a)*R, 5, 3, a, 0, 6.283); g.fill(); }
    g.fillStyle = '#7a4a1e'; g.beginPath(); g.arc(cx, cy, 7, 0, 6.283); g.fill();
    g.fillStyle = '#5c3413';
    for (let i = 0; i < 6; i++) { const a = i/6*6.283; g.beginPath(); g.arc(cx+Math.cos(a)*3, cy+Math.sin(a)*3, 1.2, 0, 6.283); g.fill(); }
  }
  g.restore();
}
// えにっき／ずかん（自由研究の きろく）。紙のような まど＝絵が いらない
function drawDiary() {
  g.save();
  g.fillStyle = 'rgba(6,8,14,0.55)'; g.fillRect(0, 0, VW, VH);   // うしろを 暗く
  const bx = 90, by = 46, bw = VW - 180, bh = VH - 92, r = 16;
  g.fillStyle = '#f3ecd8';                                        // 画用紙いろ
  g.beginPath(); g.moveTo(bx+r,by); g.arcTo(bx+bw,by,bx+bw,by+bh,r); g.arcTo(bx+bw,by+bh,bx,by+bh,r);
  g.arcTo(bx,by+bh,bx,by,r); g.arcTo(bx,by,bx+bw,by,r); g.fill();
  g.strokeStyle = 'rgba(120,95,60,0.4)'; g.lineWidth = 2; g.stroke();
  const cx = bx + 34;
  g.fillStyle = '#5a4a2e'; g.font = '700 24px system-ui';
  g.fillText('えにっき ・ じゆうけんきゅう', cx, by + 42);
  // ずかん（あつめた もの）
  g.font = '600 16px system-ui'; g.fillStyle = '#6b5836';
  g.fillText(`なつやすみ ${day}日目 ・ のこり ${nokori()}日`, cx, by + 78);
  g.fillText(`つかまえた ほたる：${caughtHotaru} ひき`, cx, by + 104);
  g.fillText(`さかせた ひまわり：${bloomTotal} りん`, cx, by + 130);
  // じゆうけんきゅう チェックリスト（やったこと）。右がわに ならべる
  const chk = [
    ['ひまわりを さかせた', bloomTotal > 0],
    ['ほたるを つかまえた', caughtHotaru > 0],
    ['はなびを みた', flags.sawHanabi],
    ['ラジオたいそう', taisoStamps > 0],
    ['みずあそび', flags.everMizu],
  ];
  const cx2 = bx + bw*0.52;
  g.font = '600 15px system-ui'; g.fillStyle = '#6b5836';
  g.fillText('じゆうけんきゅう', cx2, by + 78);
  g.font = '15px system-ui';
  chk.forEach((it, i) => {
    g.fillStyle = it[1] ? '#3f7a2e' : '#b3a888';
    g.fillText(`${it[1] ? '☑' : '☐'} ${it[0]}`, cx2, by + 104 + i*24);
  });
  g.strokeStyle = 'rgba(120,95,60,0.3)'; g.lineWidth = 1;
  g.beginPath(); g.moveTo(cx, by + 148); g.lineTo(bx + bw - 34, by + 148); g.stroke();
  // えにっき（あたらしい順に 数日ぶん）
  g.font = '15px system-ui'; g.fillStyle = '#4a3d24';
  const recent = diary.slice(-6).reverse();
  let yy = by + 178;
  if (!recent.length) { g.fillStyle = '#8a7a58'; g.fillText('（まだ なにも かいてない。ねると その日の ことが のる）', cx, yy); }
  for (const e of recent) {
    g.fillStyle = '#7a6540'; g.font = '700 15px system-ui'; g.fillText(`${e.d}日目`, cx, yy);
    g.fillStyle = '#4a3d24'; g.font = '15px system-ui'; g.fillText(e.text, cx + 72, yy);
    yy += 26;
    if (yy > by + bh - 30) break;
  }
  g.fillStyle = 'rgba(90,74,46,0.6)'; g.font = '13px system-ui'; g.textAlign = 'right';
  g.fillText('Nか スペースで とじる', bx + bw - 24, by + bh - 16); g.textAlign = 'left';
  g.restore();
}
// タイトル（世界を うしろに、そっと 문字を のせる）
function drawTitle(now) {
  g.save();
  g.fillStyle = 'rgba(6,10,18,0.5)'; g.fillRect(0, 0, VW, VH);
  g.textAlign = 'center';
  g.fillStyle = '#fdfbf4'; g.font = '700 56px system-ui';
  g.fillText('なつやすみ', VW/2, VH/2 - 22);
  g.fillStyle = 'rgba(255,236,190,0.92)'; g.font = '600 18px system-ui';
  g.fillText('— ちいさな ひと夏 —', VW/2, VH/2 + 14);
  const bl = 0.5 + 0.5*Math.sin(now/400);
  g.fillStyle = `rgba(255,255,255,${0.35 + 0.55*bl})`; g.font = '600 20px system-ui';
  g.fillText('スペースで はじめる', VW/2, VH/2 + 64);
  g.fillStyle = 'rgba(230,238,220,0.4)'; g.font = '12px system-ui';
  g.fillText('東方Project 二次創作 ・ タイル: CC0 Top Down Adventure Assets', VW/2, VH - 16);
  g.textAlign = 'left'; g.restore();
}
// 夏の おわり（しずかに 暗くして、その夏の きろくを 見せる）
function drawEnding(now) {
  const a = Math.min(1, endT/2.0);
  g.fillStyle = `rgba(4,6,12,${0.9*a})`; g.fillRect(0, 0, VW, VH);
  if (endT < 0.4) return;
  g.save(); g.globalAlpha = Math.min(1, (endT-0.4)/1.2); g.textAlign = 'center';
  g.fillStyle = '#fdfbf4'; g.font = '700 40px system-ui';
  g.fillText('なつやすみが おわった', VW/2, 140);
  g.fillStyle = 'rgba(255,236,190,0.92)'; g.font = '600 18px system-ui';
  g.fillText(`${SUMMER_DAYS}日の なつを すごした`, VW/2, 184);
  g.fillStyle = '#eef3ff'; g.font = '17px system-ui';
  g.fillText(`つかまえた ほたる：${caughtHotaru} ひき`, VW/2, 244);
  g.fillText(`さかせた ひまわり：${bloomTotal} りん`, VW/2, 276);
  g.fillText(`ラジオたいそう：${taisoStamps} かい`, VW/2, 308);
  g.fillStyle = 'rgba(230,238,250,0.85)'; g.font = '16px system-ui';
  g.fillText('また、らいねんの なつに。', VW/2, 366);
  if (endT > 1.2) {
    g.fillStyle = `rgba(255,255,255,${0.4 + 0.4*Math.sin(now/400)})`; g.font = '600 16px system-ui';
    g.fillText('スペースで もう いちど', VW/2, 424);
  }
  g.textAlign = 'left'; g.restore();
}
function clamp(v,a,b){ return v<a?a:(v>b?b:v); }
load();                               // つづきの 夏から
addEventListener('beforeunload', save);
requestAnimationFrame(loop);
