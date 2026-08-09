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

// --- 夏休みの 田舎（決まった 乱数で 毎回おなじ）。**森が ベース＝入れない**。
//   道と ひらけた場所だけを 切り開く。うち→田んぼ道→川(飛び石)→分岐、と 出かける
const MW = 56, MH = 64;
const STONE = 990;              // 飛び石（水の上・歩ける）。タイルには 無い＝コードで えがく
const PADDY = 991;              // 水田（水＋稲）。コードで えがく＝田んぼらしく。入れない
let seed = 20260809;
function rnd() { seed = (seed * 1103515245 + 12345) & 0x7fffffff; return seed / 0x7fffffff; }
const map = [];
for (let r = 0; r < MH; r++) { const row = []; for (let c = 0; c < MW; c++) row.push(TREE); map.push(row); }  // ぜんぶ 森
function set(c, r, v) { if (c>=0&&r>=0&&c<MW&&r<MH) map[r][c] = v; }
function rect(c0, r0, w, h, v) { for (let r=r0;r<r0+h;r++) for (let c=c0;c<c0+w;c++) set(c,r,v); }
function clearing(c0, r0, w, h) { for (let r=r0;r<r0+h;r++) for (let c=c0;c<c0+w;c++) set(c,r, rnd()<0.16?G2:G); }  // ひらけた 草地
function vpath(c, r0, r1, w) { for (let r=r0;r<=r1;r++) for (let k=0;k<w;k++) set(c+k,r,PATH); }
function hpath(r, c0, c1, w) { for (let c=c0;c<=c1;c++) for (let k=0;k<w;k++) set(c,r+k,PATH); }
// 田んぼ（あぜ道で 区切られた 水田）。あぜ＝歩ける・水＝入れない
function paddy(c0, r0, w, h) { for (let r=r0;r<r0+h;r++) for (let c=c0;c<c0+w;c++) set(c,r, (((c-c0)%5===0)||((r-r0)%4===0)) ? PATH : PADDY); }

clearing(13, 2, 30, 13);        // ① うち（スタート地点の 庭）
clearing(5, 15, 46, 17);        // ② 田んぼ ゾーン
clearing(5, 37, 46, 24);        // ③ 川むこう（分岐）
paddy(8, 16, 15, 14);           // 田んぼ・左
paddy(30, 16, 16, 14);          // 田んぼ・右
vpath(26, 13, 61, 2);           // 背骨の道（うち→田んぼ道→川→むこう）
hpath(48, 10, 27, 2);           // 分岐・左（原っぱへ）
hpath(49, 26, 46, 2);           // 分岐・右（川べり・池へ）
vpath(12, 48, 58, 2);           // 原っぱへ 下る
vpath(44, 49, 57, 2);           // 池へ 下る
rect(2, 33, MW-4, 4, WATER);    // 川（よこ一文字）
[33,34,35,36].forEach(r => set(26, r, STONE));   // 飛び石（1れつ）で わたる
set(26,32,PATH); set(26,37,PATH);                // 岸に つなぐ
rect(38, 51, 6, 5, WATER);      // 川むこうの 小さな池（釣り）
// はたけ（うちの庭・ひまわり）
const FIELD = { c: 30, r: 6, w: 7, h: 5 };
rect(FIELD.c, FIELD.r, FIELD.w, FIELD.h, PATH);
function inField(c, r) { return c>=FIELD.c && c<FIELD.c+FIELD.w && r>=FIELD.r && r<FIELD.r+FIELD.h; }
const CROPS = ['himawari', 'asagao', 'tomato'];   // ひまわり／朝顔／トマト（畑の セルごとに 種類）
// 木・花・やぶを ちらす（草の上だけ）
for (let i=0;i<300;i++){
  const c = 2 + (rnd()*(MW-4)|0), r = 2 + (rnd()*(MH-4)|0);
  if (map[r][c] !== G && map[r][c] !== G2) continue;
  const k = rnd();
  map[r][c] = k < 0.4 ? TREE : (k < 0.55 ? BUSH : (k < 0.78 ? FLOWER : PLANT));
}
// 原っぱ（川むこう左）は 花を おおめに
for (let r=50;r<59;r++) for (let c=8;c<19;c++) { if ((map[r][c]===G||map[r][c]===G2) && rnd()<0.5) map[r][c]=FLOWER; }
// うち まわりを ととのえる（ちらしの あと）：広場を 草に もどして 物を おく
const RADIO = { c: 22, r: 5 };              // ラジオ体操の 広場（あさ→スタンプ）
const REST  = { c: 18, r: 6 };              // 縁台（ひとやすみ→時間すすむ）
rect(14, 3, 12, 9, G);                      // うちの 広場を きれいに
const HOME_OBJS = [[17,4,37],[19,4,48],[16,4,28],[20,4,33]];  // 壺・木桶・木箱・たる
for (const [c,r,t] of HOME_OBJS) set(c,r,t);
[28,33,37,48].forEach(t => SOLID.add(t));   // 置いた ものは とおれない
const SHRINE = { c: 12, r: 55 };            // 神社（原っぱの おく）。鳥居＋祠、おまいり できる
rect(SHRINE.c-2, SHRINE.r-2, 6, 5, G);      // 神社の 庭を きれいに
const SCARECROW = { c: 24, r: 23 };         // 案山子（田んぼの ふち）
const SIGN = { c: 29, r: 39 };              // 道しるべ（川むこうの 分かれ道）
const JIZO = { c: 24, r: 31 };              // お地蔵さん（川の 北岸・道ばた）
const HIDDEN = { c: 8, r: 57 };             // 隠しスポット：ご神木（原っぱの おく）。夜に 光る蝶
SOLID.add(PADDY);                           // 水田は 入れない（あぜ道を あるく）
function solidAtCell(c, r) { if (c<0||r<0||c>=MW||r>=MH) return true; return SOLID.has(map[r][c]); }
function solidAt(px, py) { return solidAtCell(Math.floor(px/TS), Math.floor(py/TS)); }
// --- ミニマップ（静的マップを 1回だけ 小さく 焼く）。広い田舎の 迷子ふせぎ
function mmColor(t) {
  if (t === TREE) return '#2f5a2a';
  if (t === WATER || t === PADDY) return '#4a78c0';
  if (t === STONE) return '#9aa2ab';
  if (t === PATH) return '#d8b878';
  return '#7fae55';
}
const mmCanvas = document.createElement('canvas'); mmCanvas.width = MW; mmCanvas.height = MH;
(function () { const c = mmCanvas.getContext('2d'); for (let r = 0; r < MH; r++) for (let cc = 0; cc < MW; cc++) { c.fillStyle = mmColor(map[r][cc]); c.fillRect(cc, r, 1, 1); } })();

// --- 画像
const tiles = new Image(); tiles.src = 'assets/tileset-world.png';
const chars = new Image(); chars.src = 'data:image/png;base64,' + CHARS_B64;
const fishImg = new Image(); fishImg.src = 'assets/fish.png';   // 釣り（CraftPix OGA-BY）
const bugsImg = new Image(); bugsImg.src = 'assets/bugs.png';   // 虫取り（cutebugs CC0）
let ready = 0; const need = 4;
tiles.onload = () => ready++; chars.onload = () => ready++; fishImg.onload = () => ready++; bugsImg.onload = () => ready++;
// 魚（fish.png：64x28 の 6マス）と 虫（bugs.png：16x16 の 5マス）
const FISH_CW = 64, FISH_CH = 28;
const FISH = [ {n:'メダカ',w:22,size:4}, {n:'フナ',w:20,size:22}, {n:'ワカサギ',w:16,size:12}, {n:'ナマズ',w:12,size:55}, {n:'コイ',w:9,size:65}, {n:'なぞの さかな',w:4,size:95} ];
const BUG_CW = 16;   // s = bugs.png の 列（0カブト 1トンボ 2ホタル 3ハチ 4ガ）。蛍は 別システムなので 除外
const BUGS = [ {n:'カブトムシ',s:0,night:true}, {n:'トンボ',s:1}, {n:'ハチ',s:3}, {n:'ガ',s:4,night:true} ];

// 名まえ（ci → 表示名）。data/cast.json と 同じ ならび
const NAMES = { 0:'れいむ', 1:'まりさ', 2:'チルノ', 3:'だいようせい', 4:'ルーミア', 5:'リグル', 6:'ミスティア', 7:'けーね' };
// 話し手ID → 表示名（会話の [who, ことば] の who）
const WHO = { cirno:'チルノ', dai:'だいようせい', marisa:'まりさ', rumia:'ルーミア', wriggle:'リグル', mystia:'ミスティア', keine:'けーね', reimu:'れいむ' };

// --- プレイヤー（足もと＝下中央）と 立ってる 仲間。道の 交点あたりから。
// 仲間は そばで キーを 押すと 話す（P1と 同じ：近づいただけでは 始めない）。あたたかいトーン
const player = { x: 20 * TS, y: 9 * TS, ci: 2, face: 1, bob: 0, moving: false };
let stepAcc = 0;                 // 足音の 歩幅カウンタ
// 仲間の 会話は **時間帯で かわる**（asa／hiru／yugata／yoru）。同じ夏でも 一日で 表情が うつる
const npcs = [
  { ci: 3, x: 24 * TS, y: 4 * TS, sets: {   // だいようせい（うちの 庭）
    asa:    [['dai','おはよう、チルノちゃん'],['cirno','ん、おはよ'],['dai','きょうは なにを する？'],['cirno','うーん、かんがえ中！']],
    hiru:   [['dai','この にわ、ひろいねえ'],['cirno','ぜんぶ あたいの ばしょ！'],['dai','じゃあ てつだうよ'],['cirno','うん、たのむ！']],
    yugata: [['dai','そろそろ ゆうがた だね'],['cirno','もう そんな じかん？'],['dai','よるは はやめに ね'],['cirno','わかってるよ〜']],
    yoru:   [['dai','まだ おきてたの？'],['cirno','ほたるが きれいで'],['dai','わたしも みたいな'],['cirno','じゃあ いっしょに！']],
  } },
  { ci: 1, x: 24 * TS, y: 20 * TS, sets: {   // まりさ（田んぼ道）
    asa:    [['marisa','よお、はやいな'],['cirno','ラジオたいそう した？'],['marisa','これから いくとこ'],['cirno','いっしょに いこ！']],
    hiru:   [['marisa','いい てんきだ'],['cirno','おさんぽ びより！'],['marisa','はたけ、なんか うえたか？'],['cirno','ひまわり うえたいな']],
    yugata: [['marisa','ゆうやけ、きれいだな'],['cirno','うん、あかいね'],['marisa','なつって かんじだ'],['cirno','ずっと つづけば いいのに']],
    yoru:   [['marisa','よるは しずかだな'],['cirno','むしの こえが する'],['marisa','こわく ないか？'],['cirno','へ、へいきだもん！']],
  } },
  { ci: 5, x: 24 * TS, y: 44 * TS, sets: {   // リグル（川むこう）
    asa:    [['wriggle','あさは むしが げんきだ'],['cirno','ほんと？'],['wriggle','くさむらを のぞいて みな'],['cirno','うん、みてみる']],
    hiru:   [['wriggle','このあたり、むしが おおいぞ'],['cirno','つかまえて いい？'],['wriggle','あみが あればな'],['cirno','あみ、ほしいなあ']],
    yugata: [['wriggle','ゆうがたは ひぐらしが なく'],['cirno','かなかな…って やつ？'],['wriggle','そう、それ'],['cirno','なんだか せつない ね']],
    yoru:   [['wriggle','よるは かぶとむしの じかん'],['cirno','え、ほんと！？'],['wriggle','でも もう おそいぞ'],['cirno','うう、また こんど…']],
  } },
];
function timeKey(t) { if (t < 5) return 'yoru'; if (t < 11) return 'asa'; if (t < 16) return 'hiru'; if (t < 19) return 'yugata'; return 'yoru'; }
// 会話の えらび：まず **その時の できごと**（ひまわり・体操…）を 見て、なければ 時間帯
const flags = { daiThanked: false, marisaStamp: false, wriggleHotaru: false, introDone: false, everFish: false, everMushi: false, sawHanabi: false, everSumo: false, everOmairi: false, kenkyuDone: false, helped: 0, harvested: 0, hikaricho: false, kingyo: 0, hints: {}, bond: {}, lastNight: false };
function bestBondCi() { let b = 3, mx = -1; for (const k in flags.bond) { if (flags.bond[k] > mx) { mx = flags.bond[k]; b = +k; } } return b; }
function hint(key, text) { if (flags.hints && !flags.hints[key]) { flags.hints[key] = true; dayMsg = text; daySub = ''; dayMsgT = 2.6; save(); } }
// 自由研究の チェックリスト（drawDiaryと 終盤ナッジで 共用）
function kenkyuList() {
  return [
    ['ひまわりを さかせた', bloomTotal > 0],
    ['ほたるを つかまえた', caughtHotaru > 0],
    ['はなびを みた', flags.sawHanabi],
    ['ラジオたいそう', taisoStamps > 0],
    ['さかなを つった', flags.everFish],
    ['むしを つかまえた', flags.everMushi],
    ['むしずもうで かった', flags.everSumo],
    ['じんじゃに おまいり', flags.everOmairi],
    ['ひかりちょうを みつけた', flags.hikaricho],
  ];
}
let hikari = null;              // 隠しスポットの 光る蝶（夜・未捕獲のときだけ）
// --- きょうの おねがい（NPCが 1日1個。達成で お礼＋ありがとう数）。「今日これをやろう」の 芯
const REQS = [
  { ci: 3, who: 'dai',     ask: 'ほたるを 3びき つかまえて みせて', check: () => today.hotaru >= 3, ok: 'わあ、ありがとう！ きれいだね' },
  { ci: 1, who: 'marisa',  ask: 'さかなを つって みせてよ',         check: () => today.fish,        ok: 'おっ やるな！ ごちそうさま' },
  { ci: 5, who: 'wriggle', ask: 'むしを つかまえて みせて',         check: () => today.mushi,       ok: 'いい むしだ！ ありがとな' },
  { ci: 3, who: 'dai',     ask: 'いっしょに ラジオたいそう しよ',   check: () => today.taiso,       ok: 'いっしょに できて うれしい' },
  { ci: 1, who: 'marisa',  ask: 'ひまわりに みずを あげてきて',     check: () => today.watered,     ok: 'えらい！ おおきく なるね' },
];
let request = null, reqDone = false;
function makeRequest() { request = REQS[day % REQS.length]; reqDone = false; }
// 自由研究（やること）が ぜんぶ 済んだか＝ご褒美の 条件
function kenkyuDone() { return bloomTotal>0 && caughtHotaru>0 && flags.sawHanabi && taisoStamps>0 && flags.everFish && flags.everMushi && flags.everSumo && flags.everOmairi; }
function pickLines(npc) {
  flags.bond[npc.ci] = (flags.bond[npc.ci] || 0) + 1;   // 話すたび 絆が すこし ふかまる
  // 仲よし（絆が じゅうぶん）なら ときどき 親密な ひとこと
  if ((flags.bond[npc.ci] || 0) >= 6 && rnd() < 0.3) {
    const who = npc.ci === 1 ? 'marisa' : (npc.ci === 5 ? 'wriggle' : 'dai');
    return [[who, 'チルノと いると たのしいな'], ['cirno', 'あたいも！'], [who, 'この なつ、わすれないね']];
  }
  // 終盤（のこり3日以下）は ときどき さみしい ひとこと（ぼくなつの 情感）
  if (nokori() <= 3 && rnd() < 0.4) {
    const who = npc.ci === 1 ? 'marisa' : (npc.ci === 5 ? 'wriggle' : 'dai');
    return [[who, 'なつやすみも もうすぐ おわりだね'], ['cirno', 'え、もう…？'], [who, 'さいごまで たのしもう']];
  }
  // きょうの おねがい（達成してたら お礼／まだなら たのむ）。ふだんの 会話より 優先
  if (request && npc.ci === request.ci && !reqDone) {
    if (request.check()) { reqDone = true; flags.helped = (flags.helped||0) + 1; flags.bond[npc.ci] = (flags.bond[npc.ci]||0) + 2; save();
      return [[request.who, request.ok], ['cirno', 'えへへ']]; }
    return [[request.who, 'おねがい！ ' + request.ask], ['cirno', 'やってみる！']];
  }
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
  // リグル：虫を もっていたら 虫相撲に さそう（1日1回だけ。ふだんは 時間帯の 会話）
  if (npc.ci === 5 && dexCount(bugDex) > 0 && !sumoToday) {
    talkThen = 'sumo';
    return [['wriggle','いい 虫 つかまえたな'],['cirno','つよいんだから！'],['wriggle','じゃあ 虫相撲、しようぜ'],['cirno','うけて たつ！']];
  }
  const s = npc.sets; return s[timeKey(tod)] || s.hiru || s.asa;
}
const cam = { x: 0, y: 0 };
const TALK_R = TS * 1.4;                 // これより 近ければ 話しかけられる
let talkNpc = null, talkIdx = 0, talkLines = null, sayT = 0;   // 相手・何行目・台本・文字送り経過

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
let calT = 0;                     // 朝の こよみめくり 演出タイマー
// --- 自由研究の きろく（えにっき＋ずかん）。テキストだけ＝絵が いらない
let diary = [];                  // [{d, text}]  その日の しめくくり
let today = { hotaru: 0, planted: 0, watered: 0, bloomed: 0, taiso: false, fish: false, mushi: false, harvest: false };  // 今日 やったこと
let bloomTotal = 0;             // これまで さかせた ひまわり
let diaryOpen = false;          // Nキーで えにっきを ひらく
let dexOpen = false;            // Cキーで いきもの図鑑
let fishDex = {}, bugDex = {};  // つった魚・とった虫 の かず（index→数）
let fishMax = {};               // 魚ごとの さいだい サイズ（cm）＝長期目標
let lastSummer = null;          // きょねんの なつの 思い出（別キー・年を またいで のこる）
let fishing = null;            // 釣りの さいちゅう {phase,t,biteAt,win,fish,x,y}
const critters = [];          // 世界を とぶ 虫（cutebugs）{x,y,bi,vx,vy,ph,life}
let sumo = null, sumoWins = 0, sumoToday = false; // 虫相撲 {pos,my,op,phase,result,t}／その日 挑んだか
let matsuri = null;            // 金魚すくい（祭りの夜）{t,caught,cool,poiX,poiY,fish[],phase}
const STALL = { c: 28, r: 4 }; // 屋台（うちの庭・祭りの夜だけ 出る）
let talkThen = null;          // 会話が おわった あとに する こと（'sumo' など）
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
// --- 天気（その日で 決まる）。晴れ／くもり／あめ／猛暑。日々の 表情を つける
function weatherOf(d) {
  const x = Math.sin(d * 127.1) * 43758.5453, f = x - Math.floor(x);
  return f < 0.20 ? 'rain' : (f < 0.34 ? 'cloudy' : (f < 0.46 ? 'hot' : 'sunny'));
}
function isRainy() { return weatherOf(day) === 'rain'; }
const WEATHER_NAME = { rain: 'あめ', cloudy: 'くもり', hot: '猛暑', sunny: '晴れ' };
// にわか雨：晴れの日の 一部で 午後(14〜16時)さっと降る→あがると 虹(16〜17.8時)
function showerDay() { return weatherOf(day) === 'sunny' && (Math.floor(Math.abs(Math.sin(day*77.7))*997) % 4 === 0); }
function showerNow() { return showerDay() && tod >= 14 && tod < 16; }
function rainbowNow() { return showerDay() && tod >= 16 && tod < 17.8; }
// --- 夏まつりの 花火（5日ごとの 晴れた夜）。花火は 粒子＝コードで きれいに 描ける
function isFestival() { return day % 5 === 0 && !isRainy(); }
const fireworks = [];           // {x,y,peakY,state,parts,hue}
let fwTimer = 0;
function launchFirework() {
  fireworks.push({ x: VW*(0.2 + rnd()*0.6), y: VH*0.92, peakY: VH*(0.12 + rnd()*0.26),
                   state: 'rise', parts: [], hue: rnd()*360 });
  if (typeof fireworkLaunch === 'function') fireworkLaunch();
}
// 時間を すすめる（行動した ぶんだけ）。よなかを またいだら つぎの日へ。
// **夜おそく（22時）に なると 慧音が お迎え＝門限**（夜中も ずっとは 遊べない）
function passTime(h) {
  tod += h;
  while (tod >= 24) { tod -= 24; newDay(); }
  if (tod >= 22 && !mukaeShown && !sleepPhase && !talkNpc) { mukaeShown = true; talkNpc = MUKAE; talkIdx = 0; talkLines = MUKAE.lines; sayT = 0; }
}
function newDay() {
  recordDiary();                 // その日の しめくくりを えにっきへ
  day++;
  today = { hotaru: 0, planted: 0, watered: 0, bloomed: 0, taiso: false, fish: false, mushi: false, harvest: false };
  taisoToday = false; mukaeShown = false; sumoToday = false;   // あたらしい日：体操・お迎え・相撲も リセット
  makeRequest();                                                // きょうの おねがいを えらぶ
  growGarden();                  // 朝、みずやりした 苗が のびる（さいたら 今日の えにっきに のる）
  if (isRainy()) for (const p of garden) p.watered = true;   // 雨の日は 畑に みずが やれる
  const morning = tod >= 5 && tod < 10;
  if (morning) calT = 2.4;                      // 朝は こよみめくり
  dayMsg = `${day}日目`;
  const wt = weatherOf(day);
  daySub = day >= SUMMER_DAYS ? 'なつやすみ さいごの日…'
         : nokori() <= 3 ? `なつやすみも あと ${nokori()}日`
         : isFestival() ? 'きょうは なつまつり！ よるに はなびが あがる'
         : wt === 'rain' ? 'あめ ふり。はたけには めぐみの あめ'
         : wt === 'hot' ? 'きょうは 猛暑。みずあそびが きもちいい'
         : wt === 'cloudy' ? 'くもりぞら。すずしくて すごしやすい'
         : (morning ? 'あさごはんを たべた ・ そとへ でよう' : `なつやすみ のこり ${nokori()}日`);
  dayMsgT = 3.4;
  save();
}
function recordDiary() {
  const p = [];
  if (today.taiso) p.push('ラジオたいそうを した');
  if (today.fish) p.push('いけで さかなを つった');
  if (today.mushi) p.push('むしを つかまえた');
  if (today.hotaru) p.push(`ほたるを ${today.hotaru}ひき つかまえた`);
  if (today.planted) p.push(`たねを ${today.planted}つ まいた`);
  if (today.watered) p.push('はたけに みずを あげた');
  if (today.harvest) p.push('さくもつを しゅうかくした');
  if (today.bloomed) p.push('ひまわりが さいた！');
  if (!p.length) p.push('のんびり すごした');
  diary.push({ d: day, text: p.join('。') + '。', photo: pendingPhoto });
  pendingPhoto = null;
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
// 縁台で ひとやすみ→つぎの 時間帯へ（昼→夕方→夜 と 一気に すすむ＝夜演出への 近道）
function doRest() {
  const marks = [12, 17, 19.5, 21.5];
  let target = marks.find(m => m > tod + 0.1);
  passTime(target != null ? target - tod : 2.0);   // 夜おそくは +2h
  dayMsg = 'ひとやすみ…'; daySub = 'なつの においが する'; dayMsgT = 2.0; save();
}
// 神社で おまいり
const OMIKUJI = ['大きち！ ことしの なつは さいこう', 'ちゅうきち。むしとりが うまくいく かも', 'すえきち。あわてず のんびり いこう', 'きち。あたらしい ことに いい 日'];
function doOmairi() {
  flags.everOmairi = true;
  dayMsg = 'おまいり した'; daySub = OMIKUJI[(rnd()*OMIKUJI.length)|0]; dayMsgT = 3.0;
  passTime(0.5); save();
}
// --- 釣り（池のふちで）。うき＝コードの さざ波、魚＝本物スプライト。あたりで スペース
const ripples = [];             // {x,y,t}  ひろがって 消える 輪
function waterNextTo(pc, pr) {
  for (const [dc, dr] of [[1,0],[-1,0],[0,1],[0,-1]]) {
    const c = pc+dc, r = pr+dr;
    if (r>=0 && r<MH && c>=0 && c<MW && map[r][c] === WATER) return { x: (c+0.5)*TS, y: (r+0.5)*TS };
  }
  return null;
}
function startFishing(spot) {
  fishing = { phase: 'wait', t: 0, biteAt: 1.2 + rnd()*2.6, x: spot.x, y: spot.y };
  ripples.push({ x: spot.x, y: spot.y, t: 0 });
  if (typeof mizuSfx === 'function') mizuSfx();
}
function fishW(i) {                                   // 天気で 釣果が かわる
  let w = FISH[i].w; const wt = weatherOf(day), n = FISH[i].n;
  if (wt === 'rain') { if (n === 'ナマズ') w *= 3; if (n === 'なぞの さかな') w *= 1.6; }
  else if (wt === 'hot') { if (n === 'メダカ') w *= 1.8; if (n === 'ナマズ' || n === 'コイ') w *= 0.5; }
  return w;
}
function pickFish() {
  let tot = 0; for (let i = 0; i < FISH.length; i++) tot += fishW(i);
  let r = rnd()*tot; for (let i = 0; i < FISH.length; i++) { r -= fishW(i); if (r <= 0) return i; } return 0;
}
// 釣りの 進行（毎フレーム・playのとき）。act＝スペースを 受けとって さばく
function tickFishing(dt, pressed) {
  const f = fishing;
  f.t += dt;
  if (f.phase === 'wait') {
    if (pressed) { fishing = null; return; }            // はやアワセ→やめる
    if (f.t >= f.biteAt) { f.phase = 'bite'; f.t = 0; ripples.push({ x: f.x, y: f.y, t: 0 }); if (typeof mizuSfx === 'function') mizuSfx(); }
  } else if (f.phase === 'bite') {
    if (pressed) {                                       // アワセ 成功
      const fi = pickFish(); fishDex[fi] = (fishDex[fi]||0) + 1; today.fish = true; flags.everFish = true;
      const sz = Math.round(FISH[fi].size * (0.6 + rnd()*0.8));   // 大きさ（cm・ばらつき）
      f.record = sz > (fishMax[fi]||0); if (f.record) fishMax[fi] = sz;
      f.phase = 'result'; f.t = 0; f.fish = fi; f.size = sz; f.win = true; passTime(1.0); save();
    } else if (f.t > 0.85) { f.phase = 'result'; f.t = 0; f.win = false; }  // にげられた
  } else {                                                // result
    if (pressed || f.t > 2.6) fishing = null;
  }
}
// 虫を つかまえる（世界の critter を 1匹）。図鑑に 記録
function catchBug(cr) {
  bugDex[cr.bi] = (bugDex[cr.bi]||0) + 1; today.mushi = true; flags.everMushi = true;
  critters.splice(critters.indexOf(cr), 1);
  dayMsg = `${BUGS[cr.bi].n}を つかまえた！`; daySub = ''; dayMsgT = 1.8;
  passTime(0.5); save();
}
function dexCount(d) { let n = 0; for (const k in d) if (d[k] > 0) n++; return n; }
// --- 金魚すくい（祭りの夜）。ポイ(網)を うごかして 金魚を すくう。12秒
function startMatsuri() {
  const fish = []; for (let i = 0; i < 6; i++) fish.push({ x: VW/2 + (rnd()-0.5)*260, y: VH/2 + (rnd()-0.5)*120, vx: (rnd()-0.5)*60, vy: (rnd()-0.5)*40 });
  matsuri = { t: 0, caught: 0, cool: 0, poiX: VW/2, poiY: VH/2, fish, phase: 'play' };
}
function tickMatsuri(dt, pressed) {
  const m = matsuri;
  if (m.phase === 'result') { if (pressed || m.t > 4) { flags.kingyo = (flags.kingyo||0) + m.caught; matsuri = null; } m.t += dt; return; }
  m.t += dt; m.cool -= dt;
  const sp = 260 * dt;
  if (keys['arrowleft']||keys['a']) m.poiX -= sp; if (keys['arrowright']||keys['d']) m.poiX += sp;
  if (keys['arrowup']||keys['w']) m.poiY -= sp; if (keys['arrowdown']||keys['s']) m.poiY += sp;
  m.poiX = clamp(m.poiX, VW/2-150, VW/2+150); m.poiY = clamp(m.poiY, VH/2-90, VH/2+90);
  for (const f of m.fish) {
    if (rnd() < 0.03) { f.vx = (rnd()-0.5)*70; f.vy = (rnd()-0.5)*50; }
    f.x += f.vx*dt; f.y += f.vy*dt;
    if (f.x < VW/2-150 || f.x > VW/2+150) f.vx *= -1; if (f.y < VH/2-90 || f.y > VH/2+90) f.vy *= -1;
  }
  if (pressed && m.cool <= 0) {                       // すくう
    m.cool = 0.4;
    for (let i = m.fish.length-1; i >= 0; i--) { if (Math.hypot(m.fish[i].x - m.poiX, m.fish[i].y - m.poiY) < 22) { m.fish.splice(i,1); m.caught++; if (typeof mizuSfx==='function') mizuSfx(); } }
  }
  if (m.t > 12 || m.fish.length === 0) { m.phase = 'result'; m.t = 0; }
}
// --- 虫相撲。つかまえた 虫で リグルと 勝負。スペース連打で おし返す
function bugPower(bi) { return [5,3,3,2][bi] || 3; }         // カブトが つよい
function bestBug() { let b=-1, p=-1; for (const k in bugDex) { if (bugDex[k]>0) { const bi=+k, pw=bugPower(bi); if (pw>p){p=pw;b=bi;} } } return b; }
function sumoStart() {
  const my = bestBug(); if (my < 0) return;
  sumoToday = true;              // その日は もう さそわれない（ふだんの 会話に もどる）
  sumo = { pos: 0, my, op: (rnd()*BUGS.length)|0, phase: 'fight', result: null, t: 0 };
}
function tickSumo(dt, pressed) {
  const s = sumo; s.t += dt;
  if (s.phase === 'fight') {
    s.pos -= bugPower(s.op) * 0.13 * dt;         // 相手が おしてくる
    if (pressed) s.pos += bugPower(s.my) * 0.05; // 連打で おし返す
    s.pos = clamp(s.pos, -1.1, 1.1);
    if (s.pos >= 1) { s.phase='result'; s.result='win'; s.t=0; sumoWins++; flags.everSumo=true; save(); }
    else if (s.pos <= -1) { s.phase='result'; s.result='lose'; s.t=0; }
  } else if (pressed || s.t > 2.8) {
    const w = s.result; sumo = null; if (w === 'win') passTime(1.0);
  }
}
// 虫（世界を とぶ cutebugs）
const BUG_R = TS * 0.95;
function matchBugTime(bi) { return BUGS[bi].night ? isNight() : !isNight(); }  // 夜の虫は夜・昼の虫は昼
function spawnCritter() {
  const cand = BUGS.map((b,i)=>i).filter(matchBugTime);
  if (!cand.length) return;
  const bi = cand[(rnd()*cand.length)|0];
  const wantTree = !!BUGS[bi].night;   // 夜の虫（カブト・ガ）は 木のそば＝森の際に
  for (let tr = 0; tr < 12; tr++) {
    const c = Math.floor(player.x/TS) + ((rnd()*18|0)-9), r = Math.floor(player.y/TS) + ((rnd()*12|0)-6);
    if (c<1||r<1||c>=MW-1||r>=MH-1) continue;
    const t = map[r][c];
    if (!(t===G || t===G2 || t===FLOWER || t===PLANT)) continue;
    const treeAdj = [[1,0],[-1,0],[0,1],[0,-1]].some(([dc,dr]) => (map[r+dr] && map[r+dr][c+dc]) === TREE);
    if (wantTree && !treeAdj) continue;
    critters.push({ x:c*TS+TS/2, y:r*TS+TS/2, bi, vx:0, vy:0, ph:rnd()*6.28, life:0 }); return;
  }
}
// --- ねむる（Zキー）。まっくらに とけて つぎの朝へ
let sleepPhase = 0;              // 0=起きてる。2.0→0 へ。1.0で 朝に とぶ
let pendingPhoto = null;        // 「きょうの一枚」（ねる前の 画面を 縮小スナップ＝絵日記の 魂）
function snapshot() {
  try { const o = document.createElement('canvas'); o.width = 176; o.height = 99;
    o.getContext('2d').drawImage(cv, 0, 0, 176, 99); return o.toDataURL('image/jpeg', 0.5); } catch (e) { return null; }
}
function startSleep() { if (sleepPhase <= 0 && !talkNpc) { pendingPhoto = snapshot(); sleepPhase = 2.0; } }
// --- セーブ／ロード（この夏が つづいてる 感じ）
function save() {
  try { localStorage.setItem('natsuyasumi_td',
    JSON.stringify({ day, tod, caughtHotaru, garden, diary, today, bloomTotal, taisoStamps, taisoToday, flags, fishDex, bugDex, fishMax, sumoWins, sumoToday, reqDone, px: player.x, py: player.y })); } catch (e) {}
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
    taisoStamps = s.taisoStamps || 0; taisoToday = !!s.taisoToday; sumoWins = s.sumoWins || 0; sumoToday = !!s.sumoToday;
    if (s.flags) Object.assign(flags, s.flags);
    if (s.fishDex) fishDex = s.fishDex;
    if (s.bugDex) bugDex = s.bugDex;
    if (s.fishMax) fishMax = s.fishMax;
    if (s.px != null) { player.x = s.px; player.y = s.py; }
    if (solidAt(player.x, player.y)) { player.x = 20 * TS; player.y = 9 * TS; }  // 旧マップの 位置が 壁なら 家へ
  } catch (e) {}
}
function loadMemory() { try { lastSummer = JSON.parse(localStorage.getItem('natsuyasumi_td_memory') || 'null'); } catch (e) { lastSummer = null; } }
// 夏の おわりに「去年の なつ」として のこす（年を またいで つみ重なる）
function saveMemory() {
  try {
    const prev = JSON.parse(localStorage.getItem('natsuyasumi_td_memory') || 'null');
    localStorage.setItem('natsuyasumi_td_memory', JSON.stringify({
      year: ((prev && prev.year) || 0) + 1,
      hotaru: caughtHotaru, bloom: bloomTotal, taiso: taisoStamps,
      fish: dexCount(fishDex), bug: dexCount(bugDex), hakase: flags.kenkyuDone,
    }));
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
let pauseOpen = false;            // ポーズ／せってい（音量・あそびかた・クレジット）
let endT = 0;                     // エンディングの 経過（フェード用）
addEventListener('keydown', e => {
  initAudio();                    // 最初の キーで 夏の音を 起こす（自動再生ポリシー対策）
  if (e.key.startsWith('Arrow')||e.key===' ') e.preventDefault();
  if (!e.repeat && (e.key==='p'||e.key==='P'||e.key==='Escape')) { pauseOpen = !pauseOpen; return; }
  if (!e.repeat && (e.key==='m'||e.key==='M')) { const l = cycleVolume(); dayMsg = 'おと：' + l; daySub = ''; dayMsgT = 1.2; }
  if (pauseOpen) return;          // ポーズ中は ほかの キーは 無効
  if (!e.repeat && (e.key===' '||e.key==='Enter')) act = true;
  if (!e.repeat && (e.key==='z'||e.key==='Z')) startSleep();
  if (!e.repeat && (e.key==='h'||e.key==='H')) showHud = !showHud;
  if (!e.repeat && (e.key==='n'||e.key==='N')) diaryOpen = !diaryOpen;
  if (!e.repeat && (e.key==='c'||e.key==='C')) dexOpen = !dexOpen;
  keys[e.key.toLowerCase()] = true;
});
addEventListener('keyup',   e => { keys[e.key.toLowerCase()] = false; });

// --- タッチUI（スマホ）。仮想スティック＋ボタンを 既存の keys/act に 橋わたし
let touchMode = false, stickId = null, stickKX = 0, stickKY = 0;
const STICK = { x: 96, y: VH - 96, r: 60 };
const BTN = {
  act:   { x: VW - 74,  y: VH - 82,  r: 42, label: '▶' },
  sleep: { x: VW - 156, y: VH - 60,  r: 28, label: 'Z' },
  diary: { x: VW - 66,  y: VH - 178, r: 27, label: 'N' },
  dex:   { x: VW - 142, y: VH - 158, r: 27, label: 'C' },
};
function canvasXY(e) { const r = cv.getBoundingClientRect(); return [(e.clientX - r.left) * (VW / r.width), (e.clientY - r.top) * (VH / r.height)]; }
function clearArrows() { keys['arrowleft'] = keys['arrowright'] = keys['arrowup'] = keys['arrowdown'] = false; }
function setStick(x, y) {
  const dx = x - STICK.x, dy = y - STICK.y, dead = 12;
  keys['arrowleft'] = dx < -dead; keys['arrowright'] = dx > dead; keys['arrowup'] = dy < -dead; keys['arrowdown'] = dy > dead;
  const m = Math.max(1, Math.hypot(dx, dy)), cl = Math.min(1, STICK.r / m);
  stickKX = dx * cl; stickKY = dy * cl;
}
cv.addEventListener('pointerdown', e => {
  touchMode = true; initAudio(); const [x, y] = canvasXY(e);
  if (mode === 'play' && pauseOpen) {                     // ポーズ中：左タップ=音量 / 右タップ=とじる
    if (x < VW*0.5) { const l = cycleVolume(); dayMsg = 'おと：' + l; daySub = ''; dayMsgT = 1.0; } else pauseOpen = false;
    e.preventDefault(); return;
  }
  if (mode === 'play' && Math.hypot(x - (VW-28), y - 24) < 22) { pauseOpen = true; e.preventDefault(); return; }  // ⚙
  if (mode !== 'play') { act = true; e.preventDefault(); return; }        // タイトル/エンディングは タップで
  for (const k in BTN) { const bd = BTN[k]; if (Math.hypot(x - bd.x, y - bd.y) < bd.r + 8) {
    if (k === 'act') act = true; else if (k === 'sleep') startSleep(); else if (k === 'diary') diaryOpen = !diaryOpen; else if (k === 'dex') dexOpen = !dexOpen;
    e.preventDefault(); return; } }
  if (x < VW * 0.5) { stickId = e.pointerId; setStick(x, y); }             // 左半分＝スティック
  else act = true;                                                          // 右半分タップ＝決定／会話送り
  e.preventDefault();
});
cv.addEventListener('pointermove', e => { if (e.pointerId === stickId) { const [x, y] = canvasXY(e); setStick(x, y); e.preventDefault(); } });
function endStick(e) { if (e.pointerId === stickId) { stickId = null; stickKX = stickKY = 0; clearArrows(); } }
cv.addEventListener('pointerup', endStick); cv.addEventListener('pointercancel', endStick);
function drawTouchControls() {
  g.save();
  // スティック
  g.fillStyle = 'rgba(20,26,36,0.28)'; g.beginPath(); g.arc(STICK.x, STICK.y, STICK.r, 0, 6.283); g.fill();
  g.fillStyle = 'rgba(236,242,250,0.4)'; g.beginPath(); g.arc(STICK.x + stickKX, STICK.y + stickKY, 22, 0, 6.283); g.fill();
  // ボタン
  for (const k in BTN) { const bd = BTN[k];
    g.fillStyle = k === 'act' ? 'rgba(120,180,120,0.34)' : 'rgba(20,26,36,0.30)';
    g.beginPath(); g.arc(bd.x, bd.y, bd.r, 0, 6.283); g.fill();
    g.fillStyle = 'rgba(246,250,242,0.85)'; g.font = `600 ${bd.r>30?22:16}px system-ui`; g.textAlign = 'center';
    g.fillText(bd.label, bd.x, bd.y + (bd.r>30?7:5)); }
  g.textAlign = 'left'; g.restore();
}

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
  if (ready < need) { g.fillStyle = '#0d120b'; g.fillRect(0,0,VW,VH); requestAnimationFrame(loop); return; }

  // タイトル／エンディングでは 世界を うしろに 見せる だけ（更新しない）
  if (mode === 'title') { if (act) { mode = 'play'; initAudio(); if (!flags.introDone) { flags.introDone = true; talkNpc = INTRO; talkIdx = 0; talkLines = INTRO.lines; sayT = 0; save(); } act = false; } }
  if (mode === 'ending') { endT += dt; if (act && endT > 1.2) { saveMemory(); removeEventListener('beforeunload', save); try { localStorage.removeItem('natsuyasumi_td'); } catch (e) {} location.reload(); return; } }

  let near = null, nearFly = null, onField = false, fieldPlot = null, nearRadio = false, waterSpot = null, nearRest = false, nearShrine = false, nearStall = false, nearBug = null, nearBugD = 1e9, nearHikari = false, pc = 0, pr = 0;
  if (mode === 'play') {
    // 時間は **勝手には 進まない**（急かさない）。ねむり中だけ つぎの朝へ とぶ
    if (sleepPhase > 0) {
      const before = sleepPhase; sleepPhase -= dt;
      if (before > 1.0 && sleepPhase <= 1.0) { tod = 7; newDay(); }
      if (sleepPhase < 0) sleepPhase = 0;
    }
    if (dayMsgT > 0) dayMsgT -= dt;
    if (calT > 0) calT -= dt;       // こよみめくり
    if (talkNpc) sayT += dt;        // 文字送り
    ambientTick(dt, tod);           // 夏の音（時間帯で 鳴き分け）
    // 最終夜：いちばん 仲よくなった 子と ふたりの 場面（絆の 回収）
    if (day >= SUMMER_DAYS && isNight() && !flags.lastNight && !talkNpc && !sleepPhase && !fishing && !sumo && !matsuri && !mukaeShown) {
      flags.lastNight = true;
      const ci = bestBondCi(), who = ci === 1 ? 'marisa' : (ci === 5 ? 'wriggle' : 'dai');
      talkNpc = { onEnd: 'none', lines: [[who, 'あしたで なつやすみ おわりだね'], ['cirno', 'ずっと なつだと いいのに…'], [who, 'たくさん あそんだね。ありがとう'], ['cirno', 'また らいねんも、ぜったい！']] };
      talkIdx = 0; talkLines = talkNpc.lines; sayT = 0; save();
    }

    // うごく（8方向）。足もとで あたり判定、軸ごとに 止める。話している あいだは 足を とめる
    let ax = 0, ay = 0;
    if (!talkNpc && !sleepPhase && !diaryOpen && !dexOpen && !fishing && !sumo && !matsuri && !pauseOpen) {
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
    // 足音（踏んだ タイルで 音色）。歩幅ごとに 1回
    if (player.moving) { stepAcc += dt * 11;
      if (stepAcc > 3.1) { stepAcc = 0;
        const ft = map[Math.floor(player.y/TS)] && map[Math.floor(player.y/TS)][Math.floor(player.x/TS)];
        const kind = ft === STONE ? 'water' : (ft === PATH ? 'path' : 'grass');
        if (typeof footstep === 'function') footstep(kind);
      }
    } else stepAcc = 2.6;

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
    // 世界を とぶ 虫（cutebugs）。昼＝トンボ/ハチ、夜＝カブト/ガ。草・木のそばに わく（天気で 増減）
    const bugRate = weatherOf(day) === 'hot' ? 0.075 : (weatherOf(day) === 'cloudy' ? 0.03 : 0.05);
    if (!isRainy() && !showerNow() && critters.length < 6 && rnd() < bugRate) spawnCritter();
    for (let i = critters.length - 1; i >= 0; i--) {
      const cr = critters[i]; cr.ph += dt * 2;
      if (rnd() < 0.04) { cr.vx = (rnd()-0.5)*46; cr.vy = (rnd()-0.5)*46; }
      cr.x += cr.vx*dt; cr.y += cr.vy*dt;
      cr.life += dt * (matchBugTime(cr.bi) ? 0.5 : -1.0);
      if (cr.life <= 0 && !matchBugTime(cr.bi)) { critters.splice(i, 1); continue; }
      cr.life = clamp(cr.life, 0, 1);
      const d = Math.hypot(cr.x - player.x, cr.y - player.y);
      if (d < BUG_R && (!nearBug || d < nearBugD)) { nearBug = cr; nearBugD = d; }
    }
    // 隠しスポットの 光る蝶（夜・ご神木の そば・未捕獲）。ふわり ただよう
    const hdx = (HIDDEN.c+0.5)*TS - player.x, hdy = (HIDDEN.r+0.5)*TS - player.y, nearHome = Math.hypot(hdx, hdy) < TS*6;
    if (isNight() && !flags.hikaricho && nearHome) {
      if (!hikari) hikari = { x: (HIDDEN.c+0.5)*TS + (rnd()-0.5)*TS*3, y: (HIDDEN.r+0.5)*TS + (rnd()-0.5)*TS*2, ph: rnd()*6.28, vx: 0, vy: 0 };
      hikari.ph += dt*2.2;
      if (rnd() < 0.04) { hikari.vx = (rnd()-0.5)*30; hikari.vy = (rnd()-0.5)*30; }
      hikari.x += hikari.vx*dt; hikari.y += hikari.vy*dt;
      if (Math.hypot(hikari.x - player.x, hikari.y - player.y) < TS*0.95) nearHikari = true;
    } else if (!nearHome || !isNight()) hikari = null;
    // そばの 仲間（話しかけ用）。夜は みんな 家に かえる（門限と 同じ）＝昼だけ
    let bestD = TALK_R;
    if (!isNight()) for (const n of npcs) { const d = Math.hypot(n.x - player.x, n.y - player.y); if (d < bestD) { bestD = d; near = n; } }
    // 足もとの はたけ／体操の 広場
    pc = Math.floor(player.x/TS); pr = Math.floor(player.y/TS);
    onField = inField(pc, pr);
    fieldPlot = onField ? plotAt(pc, pr) : null;
    nearRadio = Math.hypot((RADIO.c+0.5)*TS - player.x, (RADIO.r+0.5)*TS - player.y) < TS*1.3;
    nearRest = Math.hypot((REST.c+0.5)*TS - player.x, (REST.r+0.5)*TS - player.y) < TS*1.3;
    nearShrine = Math.hypot((SHRINE.c+0.5)*TS - player.x, (SHRINE.r+0.5)*TS - player.y) < TS*1.5;
    nearStall = isFestival() && isNight() && Math.hypot((STALL.c+0.5)*TS - player.x, (STALL.r+0.5)*TS - player.y) < TS*1.6;
    waterSpot = waterNextTo(pc, pr);          // 池の ふちに いるか
    // 雨は 水面を たたく＝画面内の 水セルに 波紋を ちらす
    if ((isRainy() || showerNow()) && ripples.length < 24) for (let k = 0; k < 2; k++) {
      const c = Math.floor((cam.x + rnd()*VW)/TS), r = Math.floor((cam.y + rnd()*VH)/TS);
      if (map[r] && map[r][c] === WATER) ripples.push({ x: (c+0.5)*TS, y: (r+0.5)*TS, t: 0 });
    }
    // さざ波を すすめる（ひろがって 消える）
    for (let i = ripples.length - 1; i >= 0; i--) { ripples[i].t += dt; if (ripples[i].t > 1.1) ripples.splice(i, 1); }
    // 夏まつりの 花火（晴れた 祭りの夜）。あがっては ひらいて 消える
    if (isFestival() && isNight()) { fwTimer -= dt; if (fwTimer <= 0) { launchFirework(); fwTimer = 1.3 + rnd()*1.7; } }
    for (let i = fireworks.length - 1; i >= 0; i--) {
      const fw = fireworks[i];
      if (fw.state === 'rise') {
        fw.y -= 260 * dt;
        if (fw.y <= fw.peakY) {
          fw.state = 'burst'; flags.sawHanabi = true; if (typeof fireworkBoom === 'function') fireworkBoom();
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
    // 初回だけの ヒント（増えた あそびを 取りこぼさせない）
    if (!talkNpc && !fishing && !sumo && !matsuri) {
      if (nearStall) hint('stall', 'おまつりの 屋台！ スペースで きんぎょすくい');
      else if (nearShrine) hint('shrine', 'じんじゃ：スペースで おまいり できる');
      else if (waterSpot) hint('pond', 'みずべ：スペースで つりが できる');
      else if (onField && !fieldPlot) hint('field', 'はたけ：スペースで たねを うえる');
      else if (nearBug) hint('bug', 'むしに ちかづいて スペースで つかまえる');
      else if (nearRest) hint('rest', 'えんだい：スペースで ひとやすみ（時間が すすむ）');
      else if (isNight() && nearHome && !flags.hikaricho) hint('goshinboku', 'よるの ご神木… なにか いる？');
    }
    if (fishing) { tickFishing(dt, act); act = false; }     // 釣り中は スペースを 釣りへ
    else if (sumo) { tickSumo(dt, act); act = false; }      // 虫相撲中は スペースを 相撲へ
    else if (matsuri) { tickMatsuri(dt, act); act = false; } // 金魚すくい中
    else if (!pauseOpen) {
      if (act && diaryOpen) { diaryOpen = false; act = false; }
      if (act && dexOpen) { dexOpen = false; act = false; }
      if (act) {
        if (talkNpc) {
          const full = talkLines[talkIdx][1].length;
          if (sayT * 34 < full) { sayT = 99; }        // まだ 出しきってない→ いちど 全部だす
          else if (++talkIdx >= talkLines.length) {
            const end = talkNpc.onEnd, then = talkThen; talkNpc = null; talkIdx = 0; talkLines = null; talkThen = null;
            if (end === 'sleep') startSleep();
            else if (then === 'sumo') sumoStart();
            else if (end === 'none') { /* 導き・独白は 時間を 使わない */ }
            else passTime(1.0);
          } else { sayT = 0; }
        }
        else if (near) { talkNpc = near; talkIdx = 0; talkThen = null; talkLines = pickLines(near); sayT = 0; }
        else if (nearRadio && canTaiso()) { doTaiso(); }
        else if (onField) {
          if (!fieldPlot) { garden.push({ c: pc, r: pr, stage: 0, watered: false, crop: CROPS[(pc+pr)%3] }); today.planted++; passTime(1.0); save(); }
          else if (fieldPlot.stage >= 4) {           // 収穫（そだて切りで 終わらせない）
            const nm = fieldPlot.crop === 'tomato' ? 'トマト' : (fieldPlot.crop === 'asagao' ? 'あさがおの たね' : 'ひまわりの たね');
            flags.harvested = (flags.harvested||0) + 1; today.harvest = true;
            garden.splice(garden.indexOf(fieldPlot), 1);
            dayMsg = 'しゅうかく！ ' + nm; daySub = 'また うえられる'; dayMsgT = 2.2;
            passTime(0.5); save();
          }
          else if (!fieldPlot.watered) { fieldPlot.watered = true; today.watered++; passTime(1.0); save(); }
        }
        else if (nearStall) { startMatsuri(); }
        else if (nearHikari) { flags.hikaricho = true; hikari = null; dayMsg = '★ ひかりちょうを つかまえた！'; daySub = 'よるの もりの ひみつ'; dayMsgT = 3.2; passTime(0.5); save(); }
        else if (nearShrine) { doOmairi(); }
        else if (waterSpot) { startFishing(waterSpot); }
        else if (nearBug) { catchBug(nearBug); }
        else if (nearRest) { doRest(); }
        else if (nearFly) { flies.splice(flies.indexOf(nearFly), 1); caughtHotaru++; today.hotaru++; nearFly = null; passTime(0.5); save(); }
        act = false;
      }
    }
    // 自由研究 コンプの ご褒美（1回だけ・お祝い花火＋称号）
    if (!flags.kenkyuDone && kenkyuDone()) {
      flags.kenkyuDone = true;
      dayMsg = '★ じゆうけんきゅう かんせい！'; daySub = 'すごい！ なつやすみ はかせ だね'; dayMsgT = 4.2;
      for (let i = 0; i < 4; i++) launchFirework();
      save();
    }
    // 夏の おわり（さいごの日を こえたら）
    if (day > SUMMER_DAYS) { mode = 'ending'; endT = 0; buildAlbum(); }
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
      if (t === STONE) { drawTile(WATER, dx, dy); drawStone(dx, dy); }   // 飛び石＝水の上に 石
      else if (t === PADDY) { drawPaddy(dx, dy, c, r); }                 // 水田＝水＋稲
      else {
        if (t !== G && t !== G2) drawTile(G, dx, dy); // 透ける絵は 下に 草
        drawTile(t, dx, dy);
      }
      if (inField(c, r)) drawFurrow(dx, dy);          // 畑は うねを ひく
      if (t === WATER && !isNight()) {                // 昼の 水面は きらめく
        const tw = 0.5 + 0.5*Math.sin(now/280 + c*1.7 + r*0.9);
        if (tw > 0.72) { g.fillStyle = `rgba(255,255,255,${(tw-0.72)*1.1})`; g.fillRect(dx + (c*13%(TS-4)) + 2, dy + (r*7%(TS-4)) + 2, 2, 2); }
      }
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
  // 釣りの うき（水の上）。あたりで 「！」
  if (fishing) {
    const fbx = fishing.x - cam.x, fby = fishing.y - cam.y;
    g.save();
    g.fillStyle = '#f4f4f4'; g.fillRect(fbx-3, fby-1, 6, 3);
    g.fillStyle = '#e24a4a'; g.beginPath(); g.arc(fbx, fby-1, 3.4, Math.PI, 0); g.fill();
    if (fishing.phase === 'bite') {
      g.fillStyle = '#ffe23a'; g.font = '700 22px system-ui'; g.textAlign = 'center';
      g.fillText('！', fbx, fby - 16 - Math.abs(Math.sin(now/70))*4); g.textAlign = 'left';
    }
    g.restore();
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
  drawShrine();                          // 神社（鳥居＋祠＋狛犬）
  drawProps();                           // 案山子・道しるべ看板
  if (isFestival() && isNight()) {       // 祭りの夜だけ 屋台
    const sx = (STALL.c+0.5)*TS - cam.x, sy = (STALL.r+1)*TS - cam.y;
    if (sx > -60 && sx < VW+60) {
      g.save();
      g.fillStyle = '#8a4a3a'; g.fillRect(sx-28, sy-40, 56, 6);            // 屋根
      g.fillStyle = '#b5563f'; for (let i=0;i<7;i++){ if(i%2){ g.fillRect(sx-28+i*8, sy-40, 8, 6);} }  // 紅白しま
      g.fillStyle = '#6b4a2a'; g.fillRect(sx-26, sy-34, 4, 34); g.fillRect(sx+22, sy-34, 4, 34);  // 柱
      g.fillStyle = '#d9c07a'; g.fillRect(sx-24, sy-18, 48, 10);            // 台
      g.fillStyle = '#3aa0d0'; g.fillRect(sx-20, sy-16, 40, 6);            // 水そう（金魚）
      // 提灯
      for (const s of [-1,1]) { g.fillStyle = '#e24a4a'; g.beginPath(); g.arc(sx + s*22, sy-36, 5, 0, 6.283); g.fill(); }
      g.fillStyle = 'rgba(255,246,220,0.95)'; g.font = '600 10px system-ui'; g.textAlign='center'; g.fillText('きんぎょすくい', sx, sy-44); g.textAlign='left';
      g.restore();
    }
  }
  // 世界を とぶ 虫（cutebugs）。ふわっと 出て 消える
  for (const cr of critters) {
    const cx = cr.x - cam.x, cy = cr.y - cam.y;
    if (cx < -20 || cy < -20 || cx > VW+20 || cy > VH+20) continue;
    const bob = Math.sin(cr.ph) * 2;
    g.save(); g.globalAlpha = cr.life;
    g.drawImage(bugsImg, BUGS[cr.bi].s*BUG_CW, 0, BUG_CW, BUG_CW, Math.round(cx-BUG_CW), Math.round(cy-BUG_CW+bob), BUG_CW*2, BUG_CW*2);
    g.restore();
  }
  // y で ならべて 前後（キャラ＋ひまわり を 足もとで ソート）
  const plants = garden.map(p => ({ x: p.c*TS + TS/2, y: p.r*TS + TS, plant: p }));
  const ents = [...(isNight() ? [] : npcs), player, ...plants].sort((a,b) => a.y - b.y);  // 夜は NPC 帰宅
  for (const e of ents) {
    const ex = e.x - cam.x, ey = e.y - cam.y;
    if (e.plant) { drawPlant(e.plant.stage, ex, ey, e.plant.watered, e.plant.crop); continue; }
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
  // 朝もや（あさ 5〜8時・6時ごろ 濃い）。しっとり した 夏の あさ
  if (weatherOf(day) !== 'rain' && tod >= 5 && tod < 8) {
    const fog = (1 - Math.abs(tod - 6.2) / 1.8) * 0.33;
    if (fog > 0) { g.fillStyle = `rgba(232,238,240,${fog})`; g.fillRect(0, 0, VW, VH); }
  }
  // 終盤（のこり3日）の 夕方は 郷愁の 金色（長く 名残おしい 夕焼け）
  if (nokori() <= 3 && tod >= 15.5 && tod < 19) {
    g.fillStyle = 'rgba(255,168,86,0.12)'; g.fillRect(0, 0, VW, VH);
  }
  // くもり：うっすら 灰色。猛暑：まひるに あつい 陽射し＋陽炎
  const wx = weatherOf(day);
  if (wx === 'cloudy') { g.fillStyle = 'rgba(120,126,138,0.16)'; g.fillRect(0, 0, VW, VH); }
  if (wx === 'hot' && tod >= 10 && tod < 16) {
    g.fillStyle = 'rgba(255,238,170,0.10)'; g.fillRect(0, 0, VW, VH);
    g.save(); g.globalCompositeOperation = 'lighter';   // 陽炎（地面ぎわの ゆらぎ）
    for (let i = 0; i < 8; i++) { const yy = VH - 30 - i*14 + Math.sin(now/200 + i)*3;
      g.fillStyle = `rgba(255,250,220,0.03)`; g.fillRect(0, yy, VW, 6); }
    g.restore();
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
  // 神社の 灯籠の あかり（夜・加算）。原っぱの おくへの 導線
  if (isNight()) {
    const ly = (SHRINE.r+1)*TS - 26 - cam.y;
    for (const s of [-1, 1]) {
      const lx = (SHRINE.c+0.5)*TS + s*(TS*0.85+14) - cam.x;
      if (lx < -30 || lx > VW+30 || ly < -30 || ly > VH+30) continue;
      g.save(); g.globalCompositeOperation = 'lighter';
      const gr = g.createRadialGradient(lx, ly, 0, lx, ly, TS*1.1);
      gr.addColorStop(0, 'rgba(255,200,110,0.7)'); gr.addColorStop(1, 'rgba(255,180,90,0)');
      g.fillStyle = gr; g.beginPath(); g.arc(lx, ly, TS*1.1, 0, 6.283); g.fill(); g.restore();
    }
  }
  // 光る蝶（隠しスポット・夜）。加算で ふんわり 光る
  if (hikari) {
    const hx = hikari.x - cam.x, hy = hikari.y - cam.y, pulse = 0.6 + 0.4*Math.sin(hikari.ph);
    g.save(); g.globalCompositeOperation = 'lighter';
    const gr = g.createRadialGradient(hx, hy, 0, hx, hy, TS*0.9);
    gr.addColorStop(0, `rgba(180,230,255,${0.8*pulse})`); gr.addColorStop(1, 'rgba(120,180,255,0)');
    g.fillStyle = gr; g.beginPath(); g.arc(hx, hy, TS*0.9, 0, 6.283); g.fill();
    g.fillStyle = `rgba(230,245,255,${pulse})`;            // 羽（2枚・ひらひら）
    const w = 3 + Math.abs(Math.sin(hikari.ph*3))*2;
    g.beginPath(); g.ellipse(hx-3, hy, w, 4, 0.4, 0, 6.283); g.fill();
    g.beginPath(); g.ellipse(hx+3, hy, w, 4, -0.4, 0, 6.283); g.fill();
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

  // 雨：空を くもらせ、ななめの すじを ふらせる（本降り＋にわか雨）。畑には めぐみ
  const raining = isRainy() || showerNow();
  if (raining) {
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
  // 虹（にわか雨の あと）。空に 円弧グラデ 1本
  if (rainbowNow()) {
    g.save(); g.globalCompositeOperation = 'lighter';
    const rc = ['rgba(255,80,80,0.16)','rgba(255,160,60,0.16)','rgba(240,220,70,0.16)','rgba(90,200,110,0.16)','rgba(80,150,230,0.16)','rgba(150,100,220,0.16)'];
    for (let i = 0; i < rc.length; i++) { g.strokeStyle = rc[i]; g.lineWidth = 7; g.beginPath(); g.arc(VW*0.5, VH*1.15, 380 - i*7, Math.PI*1.15, Math.PI*1.85); g.stroke(); }
    g.restore();
  }
  if (typeof setRainLevel === 'function') setRainLevel(raining && mode === 'play' ? 0.05 : 0);
  // 川の せせらぎ：まわり(5x5)の 水に 近いほど 大きく
  if (typeof setBrookLevel === 'function') {
    let wd = 99;
    if (mode === 'play') { const pcc = Math.floor(player.x/TS), prr = Math.floor(player.y/TS);
      for (let dr=-3;dr<=3;dr++) for (let dc=-3;dc<=3;dc++) { const c=pcc+dc, r=prr+dr;
        if (r>=0&&r<MH&&c>=0&&c<MW && map[r][c]===WATER) { const d=Math.hypot(dc,dr); if (d<wd) wd=d; } } }
    setBrookLevel(wd < 3.5 ? 0.05 * (1 - wd/3.5) : 0);
  }

  // --- HUD（ひかりの上。いつも 読める）。**showHud=false で ぜんぶ 消える**（Hキーで 切替・最後は 既定オフに）
  if (showHud && mode === 'play') {
    g.fillStyle = 'rgba(230,238,220,0.9)'; g.font = '600 15px system-ui';
    g.fillText('うらの にわ', 14, 26);
    g.fillStyle = 'rgba(230,238,220,0.5)'; g.font = '12px system-ui';
    g.fillText('Zねる ・ Nえにっき ・ Cずかん ・ Hけす', 14, 44);
    // ミニマップ（左上）。道/水/家/神社＋自分＋きょうの おねがい先
    const s = 1.5, mx = 14, my = 54, mw = MW*s, mh = MH*s;
    g.save();
    g.fillStyle = 'rgba(0,0,0,0.35)'; g.fillRect(mx-2, my-2, mw+4, mh+4);
    g.drawImage(mmCanvas, mx, my, mw, mh);
    const dot = (c, r, col, sz) => { g.fillStyle = col; g.fillRect(mx + c*s - sz/2, my + r*s - sz/2, sz, sz); };
    dot(19, 5, '#ffd24a', 4);                    // 家
    dot(SHRINE.c, SHRINE.r, '#e24a4a', 4);       // 神社
    if (!flags.hikaricho) { g.fillStyle = '#fff'; g.font = '9px system-ui'; g.textAlign = 'center'; g.fillText('?', mx + (HIDDEN.c+0.5)*s, my + (HIDDEN.r+0.5)*s + 3); g.textAlign = 'left'; }  // 隠しスポット
    if (request && !reqDone) { const n = npcs.find(x => x.ci === request.ci); if (n) dot(Math.floor(n.x/TS), Math.floor(n.y/TS), '#ff9a3a', 4 + (Math.sin(now/200)>0?1:0)); }  // おねがい先（点滅）
    dot(Math.floor(player.x/TS), Math.floor(player.y/TS), '#ffffff', 4);   // 自分
    g.restore();
    // とけい（右上）：時刻と じかんたい
    const hh = Math.floor(tod), mm = Math.floor((tod % 1) * 60);
    const clk = `${hh}:${String(mm).padStart(2,'0')}  ${todName(tod)} ・ ${WEATHER_NAME[weatherOf(day)]}`;
    g.font = '600 15px system-ui'; g.textAlign = 'right';
    g.fillStyle = 'rgba(8,12,9,0.45)';
    const cw = g.measureText(clk).width; g.fillRect(VW - cw - 26, 10, cw + 16, 24);
    g.fillStyle = 'rgba(246,250,242,0.95)'; g.fillText(clk, VW - 14, 27); g.textAlign = 'left';
    // こよみ：なつやすみ のこり N日（時計の下）
    g.font = '600 13px system-ui'; g.textAlign = 'right';
    g.fillStyle = 'rgba(255,236,190,0.92)';
    g.fillText(`${day}日目 ・ のこり ${nokori()}日`, VW - 14, 47); g.textAlign = 'left';
    // きょうの おねがい（未達なら 出す）
    if (request && !reqDone) {
      g.font = '12px system-ui'; g.textAlign = 'right'; g.fillStyle = 'rgba(255,220,150,0.85)';
      g.fillText(`おねがい：${request.ask}`, VW - 14, 84); g.textAlign = 'left';
    }
    // 終盤（のこり5日以下）の やりのこし ナッジ（自由研究の 未達を そっと）
    if (nokori() <= 5) {
      const undone = kenkyuList().find(it => !it[1]);
      if (undone) { g.font = '12px system-ui'; g.textAlign = 'right'; g.fillStyle = 'rgba(255,180,120,0.9)';
        g.fillText(`まだ：${undone[0]} ・ あと ${nokori()}日`, VW - 14, 102); g.textAlign = 'left'; }
    }
    // つかまえた 蛍の かず（夜／持っていれば）
    if (caughtHotaru > 0 || isNight()) {
      g.font = '600 13px system-ui'; g.textAlign = 'right';
      g.fillStyle = 'rgba(220,255,150,0.9)';
      g.fillText(`ほたる ${caughtHotaru}`, VW - 14, 66); g.textAlign = 'left';
    }
  }

  if (mode === 'play') {
    // 会話の まど／釣り／虫相撲／足もとの したこと
    if (talkNpc) drawSay(talkLines[talkIdx]);
    else if (sumo) drawSumo(sumo);
    else if (matsuri) drawMatsuri();
    else if (fishing) {
      if (fishing.phase === 'result') drawFishResult(fishing);
      else {
        const m = fishing.phase === 'bite' ? '！ いま！ スペース！' : '…あたりを まつ（スペースで やめる）';
        g.fillStyle = 'rgba(8,12,9,0.5)'; g.fillRect(0, VH-40, VW, 40);
        g.fillStyle = fishing.phase === 'bite' ? '#ffe23a' : 'rgba(246,250,242,0.95)';
        g.font = '600 17px system-ui'; g.textAlign = 'center'; g.fillText(m, VW/2, VH-15); g.textAlign = 'left';
      }
    }
    else {
      let lbl = null;
      if (near) lbl = '▶ はなす';
      else if (nearStall) lbl = '▶ きんぎょすくい';
      else if (nearHikari) lbl = '▶ つかまえる';
      else if (nearShrine) lbl = '▶ おまいり';
      else if (nearRadio && canTaiso()) lbl = '▶ たいそうする';
      else if (onField) lbl = !fieldPlot ? '▶ うえる' : (fieldPlot.stage >= 4 ? '▶ しゅうかく' : (!fieldPlot.watered ? '▶ みずやり' : 'すくすく…'));
      else if (waterSpot) lbl = '▶ つる';
      else if (nearBug) lbl = '▶ むしとり';
      else if (nearRest) lbl = '▶ ひとやすみ';
      else if (nearFly) lbl = '▶ つかまえる';
      if (lbl) {
        g.fillStyle = 'rgba(8,12,9,0.5)'; g.fillRect(0, VH-40, VW, 40);
        g.fillStyle = 'rgba(246,250,242,0.95)'; g.font = '600 17px system-ui'; g.textAlign = 'center';
        g.fillText(lbl, VW/2, VH-15); g.textAlign = 'left';
      }
    }
    if (dexOpen) drawDex();
    if (diaryOpen) drawDiary();        // えにっき／ずかん（Nで ひらく）
    // 朝の こよみめくり（ぼくなつ感）
    if (calT > 0) drawCalendar();
    // 「◯日目」の しらせ（すこし 出て 消える）。こよみめくり中は 出さない
    if (dayMsgT > 0 && calT <= 0) {
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

  if (touchMode && mode === 'play') drawTouchControls();   // スマホの 操作UI（上に のせる）
  if (mode === 'play') {                                    // ⚙ ボタン（右上）
    g.fillStyle = 'rgba(20,26,36,0.35)'; g.beginPath(); g.arc(VW-28, 24, 14, 0, 6.283); g.fill();
    g.fillStyle = 'rgba(246,250,242,0.8)'; g.font = '16px system-ui'; g.textAlign = 'center'; g.fillText('⚙', VW-28, 30); g.textAlign = 'left';
  }
  if (pauseOpen) drawPause();
  act = false;                         // 1フレームで つかいきる
  requestAnimationFrame(loop);
}
function drawSay(line) {
  const bx = 70, by = VH - 122, bw = VW - 140, bh = 96, r = 14;
  g.save();
  g.fillStyle = 'rgba(10,14,26,0.82)';
  g.beginPath(); g.moveTo(bx+r,by); g.arcTo(bx+bw,by,bx+bw,by+bh,r); g.arcTo(bx+bw,by+bh,bx,by+bh,r);
  g.arcTo(bx,by+bh,bx,by,r); g.arcTo(bx,by,bx+bw,by,r); g.fill();
  g.strokeStyle = 'rgba(180,200,230,0.28)'; g.lineWidth = 1; g.stroke();
  g.fillStyle = '#ffe6a8'; g.font = '600 18px system-ui'; g.fillText(WHO[line[0]] || line[0], bx+24, by+30);
  // 文字送り＋折返し（枠に おさめる）
  g.font = '20px system-ui'; g.fillStyle = '#eef3ff';
  const shown = line[1].slice(0, Math.max(0, Math.floor(sayT * 34)));
  const maxw = bw - 48; let lineStr = '', yy = by + 60; const lines = [];
  for (const ch of shown) { if (g.measureText(lineStr + ch).width > maxw) { lines.push(lineStr); lineStr = ch; } else lineStr += ch; }
  lines.push(lineStr);
  for (const s of lines.slice(0, 2)) { g.fillText(s, bx+24, yy); yy += 26; }
  if (shown.length >= line[1].length) {   // 出しきったら 「つぎへ」
    g.fillStyle = 'rgba(230,238,250,0.5)'; g.font = '13px system-ui';
    g.fillText('スペースで つぎへ', bx+bw-150, by+bh-12);
  }
  g.restore();
}
// 水田（おだやかな 水＋稲の 束）。コードで えがく＝田んぼらしく くずれない
function drawPaddy(dx, dy, c, r) {
  g.fillStyle = '#6a9fae'; g.fillRect(dx, dy, TS, TS);                 // 水
  g.fillStyle = 'rgba(255,255,255,0.07)'; g.fillRect(dx, dy + TS*0.28, TS, 2);  // 反射
  g.fillStyle = nokori() <= 10 ? '#cba63a' : '#4f9440';               // 稲（晩夏は 黄金）
  const seed = ((c*73 + r*131) % 5) - 2;
  for (let ry = 0; ry < 2; ry++) for (let rx = 0; rx < 2; rx++) {
    const x = dx + TS*0.3 + rx*TS*0.4 + seed, y = dy + TS*0.34 + ry*TS*0.36;
    g.fillRect(x, y, 2, 7); g.fillRect(x-3, y+1, 2, 6); g.fillRect(x+3, y+1, 2, 6);
  }
}
// 神社（鳥居＋祠）。コードで えがく
function drawShrine() {
  const cx = (SHRINE.c+0.5)*TS - cam.x, gy = (SHRINE.r+1)*TS - cam.y;   // gy＝地面
  if (cx < -80 || cx > VW+80 || gy < -80 || gy > VH+120) return;
  g.save();
  // 鳥居（あかい）
  const tw = TS*1.7, th = TS*1.7, px = 5;
  g.fillStyle = '#c0392b';
  g.fillRect(cx-tw/2, gy-th, px, th); g.fillRect(cx+tw/2-px, gy-th, px, th);   // 柱
  g.fillStyle = '#a83224';
  g.fillRect(cx-tw/2-6, gy-th-8, tw+12, 7);                                     // 笠木
  g.fillStyle = '#c0392b';
  g.fillRect(cx-tw/2-2, gy-th+8, tw+4, 5);                                      // 貫
  // 祠（おくの 小さな お社）
  const sx = cx, sy = gy-6;
  g.fillStyle = '#6b4a2a'; g.fillRect(sx-11, sy-16, 22, 16);                    // 本体
  g.fillStyle = '#4a3320'; g.beginPath(); g.moveTo(sx-15, sy-16); g.lineTo(sx, sy-27); g.lineTo(sx+15, sy-16); g.closePath(); g.fill();  // 屋根
  g.fillStyle = '#2a1c10'; g.fillRect(sx-4, sy-10, 8, 10);                      // 入口
  // 灯籠（左右）。石の 柱＋火袋
  for (const s of [-1, 1]) {
    const lx = cx + s*(tw/2 + 14);
    g.fillStyle = '#7c7f86'; g.fillRect(lx-4, gy-18, 8, 18);                    // 柱
    g.fillStyle = '#8f9298'; g.fillRect(lx-7, gy-28, 14, 11);                   // 火袋
    g.fillStyle = isNight() ? '#ffd27a' : '#3a2a12'; g.fillRect(lx-4, gy-25, 8, 6);  // 火（夜は ともる）
    g.fillStyle = '#6c6f76'; g.beginPath(); g.moveTo(lx-9, gy-28); g.lineTo(lx, gy-34); g.lineTo(lx+9, gy-28); g.closePath(); g.fill();  // 笠
  }
  // 狛犬（左右）
  for (const s of [-1, 1]) {
    const kx = cx + s*(tw/2 + 30);
    g.fillStyle = '#9a9da3'; g.fillRect(kx-5, gy-11, 10, 11);                   // 体（すわり）
    g.beginPath(); g.arc(kx, gy-13, 5, 0, 6.283); g.fill();                     // 頭
    g.fillStyle = '#6c6f76'; g.fillRect(kx-6, gy-1, 12, 2);                     // 台石
  }
  g.restore();
}
// 案山子・道しるべ（コード図形で 密度を 出す）
function drawProps() {
  // 案山子
  let x = (SCARECROW.c+0.5)*TS - cam.x, gy = (SCARECROW.r+1)*TS - cam.y;
  if (x > -40 && x < VW+40 && gy > -60 && gy < VH+40) {
    g.save();
    g.fillStyle = 'rgba(10,20,8,0.2)'; g.beginPath(); g.ellipse(x, gy, 10, 3, 0, 0, 6.283); g.fill();
    g.fillStyle = '#7a5230'; g.fillRect(x-2, gy-34, 4, 34); g.fillRect(x-14, gy-26, 28, 3);   // 支柱＋腕
    g.fillStyle = '#4a6ea0'; g.beginPath(); g.moveTo(x, gy-28); g.lineTo(x-11, gy-8); g.lineTo(x+11, gy-8); g.closePath(); g.fill();  // 服
    g.fillStyle = '#d9c37a'; g.beginPath(); g.arc(x, gy-32, 6, 0, 6.283); g.fill();           // わら頭
    g.fillStyle = '#b79a5a'; g.beginPath(); g.moveTo(x-10, gy-33); g.lineTo(x, gy-43); g.lineTo(x+10, gy-33); g.closePath(); g.fill();  // 笠
    g.restore();
  }
  // 道しるべ看板
  x = (SIGN.c+0.5)*TS - cam.x; gy = (SIGN.r+1)*TS - cam.y;
  if (x > -60 && x < VW+60 && gy > -50 && gy < VH+40) {
    g.save();
    g.fillStyle = '#6b4a2a'; g.fillRect(x-2, gy-30, 4, 30);                     // 柱
    g.fillStyle = '#8a6a3a'; g.fillRect(x-26, gy-36, 52, 22);
    g.strokeStyle = '#5c3d22'; g.lineWidth = 1.5; g.strokeRect(x-26, gy-36, 52, 22);
    g.fillStyle = '#33240f'; g.font = '600 10px system-ui'; g.textAlign = 'center';
    g.fillText('← はらっぱ・神社', x, gy-25); g.fillText('いけ →', x, gy-18); g.textAlign = 'left';
    g.restore();
  }
  // ご神木（隠しスポット）。大きな 木＋しめ縄
  x = (HIDDEN.c+0.5)*TS - cam.x; gy = (HIDDEN.r+1)*TS - cam.y;
  if (x > -60 && x < VW+60 && gy > -80 && gy < VH+40) {
    g.save();
    g.fillStyle = 'rgba(10,20,8,0.25)'; g.beginPath(); g.ellipse(x, gy, 20, 6, 0, 0, 6.283); g.fill();
    g.fillStyle = '#5c3d22'; g.fillRect(x-7, gy-30, 14, 30);                   // みき
    g.fillStyle = '#eae0c0'; g.fillRect(x-9, gy-24, 18, 4);                    // しめ縄
    g.fillStyle = '#2f6a34'; g.beginPath(); g.arc(x, gy-46, 26, 0, 6.283); g.fill();  // こずえ
    g.fillStyle = '#3a7a3e'; g.beginPath(); g.arc(x-14, gy-40, 15, 0, 6.283); g.arc(x+14, gy-42, 15, 0, 6.283); g.fill();
    g.restore();
  }
  // お地蔵さん（道ばた）。石＋赤い よだれかけ
  x = (JIZO.c+0.5)*TS - cam.x; gy = (JIZO.r+1)*TS - cam.y;
  if (x > -30 && x < VW+30 && gy > -40 && gy < VH+30) {
    g.save();
    g.fillStyle = 'rgba(10,20,8,0.2)'; g.beginPath(); g.ellipse(x, gy, 9, 3, 0, 0, 6.283); g.fill();
    g.fillStyle = '#9a9da3'; g.beginPath(); g.moveTo(x-7, gy); g.lineTo(x-7, gy-16); g.arc(x, gy-16, 7, Math.PI, 0); g.lineTo(x+7, gy); g.closePath(); g.fill();  // 石体
    g.fillStyle = '#c0392b'; g.fillRect(x-6, gy-9, 12, 5);                     // よだれかけ
    g.fillStyle = '#5a5d63'; g.beginPath(); g.arc(x-2.4, gy-17, 1, 0, 6.283); g.arc(x+2.4, gy-17, 1, 0, 6.283); g.fill();  // 目
    g.restore();
  }
}
// 飛び石（水の上の 石）
function drawStone(dx, dy) {
  g.save();
  g.fillStyle = 'rgba(10,20,30,0.25)'; g.beginPath(); g.ellipse(dx+TS/2, dy+TS/2+4, TS*0.4, TS*0.24, 0, 0, 6.283); g.fill();
  g.fillStyle = '#8a8f96'; g.beginPath(); g.ellipse(dx+TS/2, dy+TS/2, TS*0.38, TS*0.3, 0, 0, 6.283); g.fill();
  g.fillStyle = '#aab0b7'; g.beginPath(); g.ellipse(dx+TS/2-3, dy+TS/2-3, TS*0.22, TS*0.16, 0, 0, 6.283); g.fill();
  g.restore();
}
// 畑の うね（土に ほそい 線）
function drawFurrow(dx, dy) {
  g.save(); g.strokeStyle = 'rgba(74,48,26,0.35)'; g.lineWidth = 2;
  for (let i = 1; i <= 2; i++) { const yy = dy + TS*i/3; g.beginPath(); g.moveTo(dx+4, yy); g.lineTo(dx+TS-4, yy); g.stroke(); }
  g.restore();
}
// 作物（コードで えがく＝絵柄が くずれない）。(x,y)＝足もと。0種→1芽→2葉→3つぼみ→4みのり
// crop: himawari(ひまわり) / asagao(朝顔) / tomato(トマト)
function drawPlant(stage, x, y, watered, crop) {
  crop = crop || 'himawari';
  g.save();
  g.fillStyle = 'rgba(10,20,8,0.22)'; g.beginPath(); g.ellipse(x, y-2, 8, 3, 0, 0, 6.283); g.fill();
  if (stage === 0) {                                  // 種（つち の もり）
    g.fillStyle = watered ? '#553720' : '#6b4a2a';
    g.beginPath(); g.ellipse(x, y-3, 6, 4, 0, 0, 6.283); g.fill();
    g.restore(); return;
  }
  const topH = crop === 'tomato' ? 30 : (crop === 'asagao' ? 42 : 38);
  const H = [0, 12, 24, topH - 6, topH][stage];
  g.strokeStyle = '#3f7a2e'; g.lineWidth = crop === 'asagao' ? 2 : 3; g.lineCap = 'round';
  g.beginPath(); g.moveTo(x, y-2); g.lineTo(x, y-H); g.stroke();
  if (stage === 1) {                                  // 双葉
    g.fillStyle = '#6cbf4a';
    g.beginPath(); g.ellipse(x-3, y-H+2, 4, 2.5, -0.6, 0, 6.283); g.fill();
    g.beginPath(); g.ellipse(x+3, y-H+2, 4, 2.5,  0.6, 0, 6.283); g.fill();
  } else if (stage >= 2) {                             // 葉
    g.fillStyle = '#5aa83e';
    g.beginPath(); g.ellipse(x-6, y-H*0.5,  7, 3.5, -0.5, 0, 6.283); g.fill();
    g.beginPath(); g.ellipse(x+6, y-H*0.62, 7, 3.5,  0.5, 0, 6.283); g.fill();
    if (crop === 'tomato') { g.beginPath(); g.ellipse(x-6, y-H*0.85, 6, 3, -0.5, 0, 6.283); g.fill(); g.beginPath(); g.ellipse(x+6, y-H*0.95, 6, 3, 0.5, 0, 6.283); g.fill(); }
  }
  if (stage === 3) {                                  // つぼみ／実の まえ
    const bc = crop === 'asagao' ? '#8a6fd0' : (crop === 'tomato' ? '#6fae4a' : '#f0b429');
    g.fillStyle = '#4f9a37'; g.beginPath(); g.arc(x, y-H, 6, 0, 6.283); g.fill();
    g.fillStyle = bc; g.beginPath(); g.arc(x, y-H, 2.6, 0, 6.283); g.fill();
  }
  if (stage === 4) {
    if (crop === 'himawari') {
      const cx = x, cy = y-H, R = 11; g.fillStyle = '#f7c948';
      for (let i = 0; i < 12; i++) { const a = i/12*6.283; g.beginPath(); g.ellipse(cx+Math.cos(a)*R, cy+Math.sin(a)*R, 5, 3, a, 0, 6.283); g.fill(); }
      g.fillStyle = '#7a4a1e'; g.beginPath(); g.arc(cx, cy, 7, 0, 6.283); g.fill();
      g.fillStyle = '#5c3413'; for (let i = 0; i < 6; i++) { const a = i/6*6.283; g.beginPath(); g.arc(cx+Math.cos(a)*3, cy+Math.sin(a)*3, 1.2, 0, 6.283); g.fill(); }
    } else if (crop === 'asagao') {                    // 朝顔：あおむらさきの ラッパ花 3つ
      const spots = [[0, -H], [-7, -H*0.7], [7, -H*0.55]];
      for (const [dx, dy] of spots) { g.fillStyle = '#7a5fd0'; g.beginPath(); g.arc(x+dx, y+dy, 5, 0, 6.283); g.fill();
        g.fillStyle = '#c9b8f0'; g.beginPath(); g.arc(x+dx, y+dy, 2, 0, 6.283); g.fill(); }
    } else {                                           // トマト：あかい 実 4つ
      const spots = [[-5, -H*0.55], [6, -H*0.7], [-3, -H*0.9], [5, -H*0.95]];
      for (const [dx, dy] of spots) { g.fillStyle = '#d83b2a'; g.beginPath(); g.arc(x+dx, y+dy, 3.4, 0, 6.283); g.fill();
        g.fillStyle = 'rgba(255,255,255,0.5)'; g.fillRect(x+dx-1, y+dy-2, 1, 1); }
    }
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
  const chk = kenkyuList();
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
  if (lastSummer) {                    // 去年の なつの 思い出（つみ重なる）
    g.fillStyle = 'rgba(255,236,190,0.8)'; g.font = '13px system-ui';
    g.fillText(`きょねん(${lastSummer.year}年目)：ほたる${lastSummer.hotaru}・ひまわり${lastSummer.bloom}・ずかん 魚${lastSummer.fish}/${FISH.length} 虫${lastSummer.bug}/${BUGS.length}${lastSummer.hakase ? ' ★はかせ' : ''}`, VW/2, VH/2 + 92);
  }
  g.fillStyle = 'rgba(230,238,220,0.4)'; g.font = '12px system-ui';
  g.fillText('東方Project 二次創作 ・ タイル: CC0 Top Down Adventure Assets', VW/2, VH - 16);
  g.textAlign = 'left'; g.restore();
}
// 夏の アルバム（えにっきの 写真を Imageに）。エンディングで 見せる
let albumImgs = [];
function buildAlbum() {
  albumImgs = diary.filter(e => e.photo).slice(-5).map(e => { const im = new Image(); im.src = e.photo; return { im, d: e.d }; });
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
  g.fillStyle = '#eef3ff'; g.font = '16px system-ui';
  g.fillText(`ほたる ${caughtHotaru}・ひまわり ${bloomTotal}・たいそう ${taisoStamps}・ずかん 魚${dexCount(fishDex)}/${FISH.length} 虫${dexCount(bugDex)}/${BUGS.length}`, VW/2, 214);
  if (flags.kenkyuDone) { g.fillStyle = '#ffe23a'; g.font = '700 18px system-ui'; g.fillText('★ しょうごう：なつやすみ はかせ', VW/2, 244); }
  if (flags.bond && Object.keys(flags.bond).length) { g.fillStyle = 'rgba(255,210,220,0.9)'; g.font = '15px system-ui';
    g.fillText(`いちばん なかよし：${NAMES[bestBondCi()]}`, VW/2, flags.kenkyuDone ? 268 : 244); }
  // 夏の アルバム（きょうの一枚たち）
  if (albumImgs.length) {
    g.fillStyle = 'rgba(255,236,190,0.85)'; g.font = '600 15px system-ui'; g.fillText('なつの アルバム', VW/2, 286);
    const tw = 92, th = 52, gap = 8, n = albumImgs.length, x0 = VW/2 - (n*(tw+gap)-gap)/2;
    for (let i = 0; i < n; i++) {
      const a2 = albumImgs[i], px = x0 + i*(tw+gap), py = 300;
      g.save();
      g.fillStyle = '#fbf7ec'; g.fillRect(px-3, py-3, tw+6, th+12);          // ポラロイド枠
      if (a2.im.complete && a2.im.naturalWidth) g.drawImage(a2.im, px, py, tw, th);
      else { g.fillStyle = '#c9d0c0'; g.fillRect(px, py, tw, th); }
      g.fillStyle = '#5a4a2e'; g.font = '9px system-ui'; g.fillText(`${a2.d}日目`, px+tw/2, py+th+8);
      g.restore();
    }
  }
  g.fillStyle = 'rgba(230,238,250,0.85)'; g.font = '16px system-ui';
  g.fillText('また、らいねんの なつに。', VW/2, 404);
  if (endT > 1.2) {
    g.fillStyle = `rgba(255,255,255,${0.4 + 0.4*Math.sin(now/400)})`; g.font = '600 16px system-ui';
    g.fillText('スペースで もう いちど', VW/2, 446);
  }
  g.textAlign = 'left'; g.restore();
}
// 魚・虫の スプライトを 中央に えがく
function drawFishSprite(fi, cx, cy, sc) {
  g.drawImage(fishImg, fi*FISH_CW, 0, FISH_CW, FISH_CH, Math.round(cx-FISH_CW*sc/2), Math.round(cy-FISH_CH*sc/2), FISH_CW*sc, FISH_CH*sc);
}
function drawBugSprite(bi, cx, cy, sc) {
  const s = BUGS[bi].s;
  g.drawImage(bugsImg, s*BUG_CW, 0, BUG_CW, BUG_CW, Math.round(cx-BUG_CW*sc/2), Math.round(cy-BUG_CW*sc/2), BUG_CW*sc, BUG_CW*sc);
}
// 釣った ときの カード（本物の 魚スプライト＋名まえ）
function drawFishResult(f) {
  const bx = 70, by = VH-146, bw = VW-140, bh = 124, r = 14;
  g.save();
  g.fillStyle = 'rgba(10,14,26,0.86)';
  g.beginPath(); g.moveTo(bx+r,by); g.arcTo(bx+bw,by,bx+bw,by+bh,r); g.arcTo(bx+bw,by+bh,bx,by+bh,r);
  g.arcTo(bx,by+bh,bx,by,r); g.arcTo(bx,by,bx+bw,by,r); g.fill();
  g.strokeStyle = 'rgba(180,200,230,0.28)'; g.lineWidth = 1; g.stroke();
  if (f.win) {
    drawFishSprite(f.fish, bx+90, by+bh/2, 2.6);
    g.fillStyle = '#ffe6a8'; g.font = '700 22px system-ui'; g.fillText(`${FISH[f.fish].n}  ${f.size}cm！`, bx+180, by+bh/2-6);
    g.fillStyle = f.record ? '#ffe23a' : 'rgba(230,238,250,0.75)'; g.font = '14px system-ui';
    g.fillText(f.record ? '★ さいだい きろく こうしん！' : `さいだい ${fishMax[f.fish]||f.size}cm`, bx+180, by+bh/2+22);
  } else {
    g.fillStyle = '#eef3ff'; g.font = '700 22px system-ui'; g.textAlign = 'center';
    g.fillText('にげられた…', VW/2, by+bh/2+2); g.textAlign = 'left';
  }
  g.fillStyle = 'rgba(230,238,250,0.5)'; g.font = '13px system-ui'; g.textAlign = 'right';
  g.fillText('スペースで つづける', bx+bw-20, by+bh-14); g.textAlign = 'left';
  g.restore();
}
// いきもの ずかん（C）。とった 魚・虫を 本物スプライトで ならべる
function drawDex() {
  g.save();
  g.fillStyle = 'rgba(6,8,14,0.6)'; g.fillRect(0, 0, VW, VH);
  const bx = 70, by = 44, bw = VW-140, bh = VH-88, r = 16;
  g.fillStyle = '#eef1e6';
  g.beginPath(); g.moveTo(bx+r,by); g.arcTo(bx+bw,by,bx+bw,by+bh,r); g.arcTo(bx+bw,by+bh,bx,by+bh,r);
  g.arcTo(bx,by+bh,bx,by,r); g.arcTo(bx,by,bx+bw,by,r); g.fill();
  g.strokeStyle = 'rgba(90,110,70,0.4)'; g.lineWidth = 2; g.stroke();
  g.fillStyle = '#3f5230'; g.font = '700 24px system-ui';
  const full = dexCount(fishDex) === FISH.length && dexCount(bugDex) === BUGS.length;
  g.fillText(`いきもの ずかん   さかな ${dexCount(fishDex)}/${FISH.length}・むし ${dexCount(bugDex)}/${BUGS.length}${full ? '   ★コンプリート！' : ''}`, bx+28, by+40);
  // さかな
  g.fillStyle = '#4a6038'; g.font = '600 17px system-ui'; g.fillText('さかな', bx+28, by+78);
  FISH.forEach((f, i) => {
    const cw = (bw-56)/6, cx = bx+28 + cw*i + cw/2, cy = by+118, got = (fishDex[i]||0) > 0;
    g.fillStyle = 'rgba(120,140,90,0.14)'; g.fillRect(cx-cw/2+4, by+92, cw-8, 78);
    if (got) {
      drawFishSprite(i, cx, cy, 1.4);
      g.fillStyle = '#3f5230'; g.font = '13px system-ui'; g.textAlign = 'center';
      g.fillText(f.n, cx, by+156); g.fillStyle='#7a8a5c'; g.fillText(`×${fishDex[i]}${fishMax[i]?' ・'+fishMax[i]+'cm':''}`, cx, by+172);
    } else { g.fillStyle = '#b7bfa6'; g.font = '700 22px system-ui'; g.textAlign = 'center'; g.fillText('？', cx, cy+6); }
    g.textAlign = 'left';
  });
  // むし
  g.fillStyle = '#4a6038'; g.font = '600 17px system-ui'; g.fillText('むし', bx+28, by+206);
  BUGS.forEach((bug, i) => {
    const cw = (bw-56)/6, cx = bx+28 + cw*i + cw/2, cy = by+250, got = (bugDex[i]||0) > 0;
    g.fillStyle = 'rgba(120,140,90,0.14)'; g.fillRect(cx-cw/2+4, by+220, cw-8, 78);
    if (got) {
      drawBugSprite(i, cx, cy, 2.4);
      g.fillStyle = '#3f5230'; g.font = '13px system-ui'; g.textAlign = 'center';
      g.fillText(bug.n, cx, by+284); g.fillStyle='#7a8a5c'; g.fillText(`×${bugDex[i]}`, cx, by+300);
    } else { g.fillStyle = '#b7bfa6'; g.font = '700 22px system-ui'; g.textAlign = 'center'; g.fillText('？', cx, cy+6); }
    g.textAlign = 'left';
  });
  g.fillStyle = 'rgba(70,80,50,0.6)'; g.font = '13px system-ui'; g.textAlign = 'right';
  g.fillText('Cか スペースで とじる', bx+bw-24, by+bh-16); g.textAlign = 'left';
  g.restore();
}
// 金魚すくいの えがき
function drawMatsuri() {
  const m = matsuri;
  g.save();
  g.fillStyle = 'rgba(8,10,20,0.55)'; g.fillRect(0, 0, VW, VH);
  // 水そう
  g.fillStyle = 'rgba(70,160,210,0.5)'; g.fillRect(VW/2-158, VH/2-98, 316, 196);
  g.strokeStyle = 'rgba(255,255,255,0.4)'; g.lineWidth = 3; g.strokeRect(VW/2-158, VH/2-98, 316, 196);
  g.fillStyle = '#ffe6a8'; g.font = '700 18px system-ui'; g.textAlign = 'center';
  g.fillText(`きんぎょすくい　すくった：${m.caught}`, VW/2, VH/2-112);
  // 金魚（コイ=赤 スプライトを 小さく）
  for (const f of m.fish) drawFishSprite(4, f.x, f.y, 0.7);
  // ポイ（網）
  if (m.phase === 'play') {
    g.strokeStyle = '#eee'; g.lineWidth = 2; g.beginPath(); g.arc(m.poiX, m.poiY, 20, 0, 6.283); g.stroke();
    g.fillStyle = 'rgba(255,255,255,0.12)'; g.fill();
    g.strokeStyle = '#c08a4a'; g.lineWidth = 3; g.beginPath(); g.moveTo(m.poiX+14, m.poiY+14); g.lineTo(m.poiX+34, m.poiY+34); g.stroke();
    g.fillStyle = 'rgba(246,250,242,0.9)'; g.font = '14px system-ui';
    g.fillText(`のこり ${Math.max(0, Math.ceil(12-m.t))}秒　やじるしで うごかす・スペースで すくう`, VW/2, VH/2+118);
  } else {
    g.fillStyle = '#ffe23a'; g.font = '700 24px system-ui';
    g.fillText(`${m.caught}ひき すくった！`, VW/2, VH/2+4);
    g.fillStyle = 'rgba(230,238,250,0.7)'; g.font = '14px system-ui'; g.fillText('スペースで つづける', VW/2, VH/2+40);
  }
  g.textAlign = 'left'; g.restore();
}
// 虫相撲の えがき（土俵の上で 2匹が おしあう）
function drawSumo(s) {
  g.save();
  g.fillStyle = 'rgba(8,10,16,0.62)'; g.fillRect(0, VH/2-96, VW, 192);
  g.fillStyle = '#ffe6a8'; g.font = '700 20px system-ui'; g.textAlign = 'center';
  g.fillText('むしずもう！', VW/2, VH/2-58);
  const cx = VW/2 + s.pos*150, cy = VH/2+4;
  g.strokeStyle = 'rgba(255,255,255,0.28)'; g.lineWidth = 2;
  g.beginPath(); g.moveTo(VW/2-172, cy+34); g.lineTo(VW/2+172, cy+34); g.stroke();
  drawBugSprite(s.my, cx-28, cy, 3.2);   // 自分
  drawBugSprite(s.op, cx+28, cy, 3.2);   // 相手
  // ゲージ
  g.fillStyle = 'rgba(255,255,255,0.15)'; g.fillRect(VW/2-150, VH/2+52, 300, 10);
  g.fillStyle = '#8fdc5a'; g.fillRect(VW/2-150, VH/2+52, 150 + s.pos*150, 10);
  if (s.phase === 'result') {
    g.fillStyle = s.result === 'win' ? '#ffe23a' : '#dfe6f0'; g.font = '700 26px system-ui';
    g.fillText(s.result === 'win' ? 'かった！' : 'まけた…', VW/2, VH/2+92);
    g.fillStyle = 'rgba(230,238,250,0.6)'; g.font = '13px system-ui'; g.fillText('スペースで つづける', VW/2, VH/2+114);
  } else {
    g.fillStyle = '#fff'; g.font = '600 16px system-ui'; g.fillText('スペース れんだ！', VW/2, VH/2+92);
  }
  g.textAlign = 'left'; g.restore();
}
// 朝の こよみめくり（1枚 すべり込む）
function drawCalendar() {
  const p = 2.4 - calT, slide = Math.min(1, p/0.35), alpha = calT < 0.5 ? Math.max(0, calT/0.5) : 1;
  const w = 200, h = 148, cx = VW/2, y = -h + (h + 84) * slide;
  g.save(); g.globalAlpha = alpha;
  g.fillStyle = '#fbf7ec'; g.fillRect(cx-w/2, y, w, h);
  g.strokeStyle = 'rgba(120,95,60,0.4)'; g.lineWidth = 2; g.strokeRect(cx-w/2, y, w, h);
  g.fillStyle = '#c0392b'; g.fillRect(cx-w/2, y, w, 30);
  g.fillStyle = '#fff'; g.font = '600 15px system-ui'; g.textAlign = 'center'; g.fillText('なつやすみ', cx, y+20);
  g.fillStyle = '#3a2a1a'; g.font = '700 46px system-ui'; g.fillText(`${day}`, cx, y+94);
  g.fillStyle = '#8a6a3a'; g.font = '14px system-ui'; g.fillText(`日目 ・ のこり ${nokori()}日`, cx, y+122);
  g.fillStyle = 'rgba(0,0,0,0.18)'; g.beginPath(); g.arc(cx-42, y+9, 3, 0, 6.28); g.arc(cx+42, y+9, 3, 0, 6.28); g.fill();
  g.textAlign = 'left'; g.restore();
}
// ポーズ／せってい（音量・あそびかた・クレジット）
function drawPause() {
  g.save();
  g.fillStyle = 'rgba(6,8,14,0.72)'; g.fillRect(0, 0, VW, VH);
  const bx = 160, by = 70, bw = VW-320, bh = VH-140, r = 16;
  g.fillStyle = '#f2eedf'; g.beginPath();
  g.moveTo(bx+r,by); g.arcTo(bx+bw,by,bx+bw,by+bh,r); g.arcTo(bx+bw,by+bh,bx,by+bh,r); g.arcTo(bx,by+bh,bx,by,r); g.arcTo(bx,by,bx+bw,by,r); g.fill();
  g.fillStyle = '#3f4a30'; g.font = '700 24px system-ui'; g.textAlign = 'center';
  g.fillText('せってい', VW/2, by+42);
  g.fillStyle = '#4a5238'; g.font = '17px system-ui';
  g.fillText(`おと：${volLabel()}    （Mキー／左タップで きりかえ）`, VW/2, by+86);
  g.font = '15px system-ui'; g.fillStyle = '#5a6048';
  const lines = [
    '― あそびかた ―',
    '　うごく：やじるし／WASD／左スティック',
    '　きめる・はなす・つる・とる：スペース／右タップ',
    '　ねる：Z　えにっき：N　ずかん：C',
    '',
    '― クレジット ―',
    '　東方Project 二次創作（キャラ絵は 非商用・差し替え前提）',
    '　タイル ansimuz(CC0) / 魚 CraftPix(OGA-BY) / 虫 madameberry(CC0)',
    '　音：手続き生成（自作）',
  ];
  let yy = by+120; for (const s of lines) { g.fillText(s, VW/2, yy); yy += 24; }
  g.fillStyle = 'rgba(90,96,72,0.7)'; g.font = '14px system-ui';
  g.fillText('Pか Escか 右タップで とじる', VW/2, by+bh-16);
  g.textAlign = 'left'; g.restore();
}
function clamp(v,a,b){ return v<a?a:(v>b?b:v); }
load();                               // つづきの 夏から
makeRequest();                        // きょうの おねがい（load後の day で）
try { const s = JSON.parse(localStorage.getItem('natsuyasumi_td')||'null'); if (s && s.reqDone) reqDone = true; } catch(e) {}
loadMemory();                         // 去年の なつの 思い出（タイトルに 出す）
addEventListener('beforeunload', save);
requestAnimationFrame(loop);
