// ===== 世界の じょうたい =====
// **日を またいで のこるもの だけ** ここに 入れる。
// いま どこに 立っているか・場面の とちゅう などは のこさない（そっちは state.js）。
// ここが そのまま セーブの中身。ふえるときは この形に 足すこと。
const SAVE_KEY = 'natsuyasumi.photo.v1';

const WORLD = {
  day: 1,
  steps: 0,          // きょう 画面を 移った回数。時計の かわり
  mukaeDone: false,  // きょう もう むかえが 来たか
  yoruDone: false,   // きょう もう 晩ごはんと 縁側が すんだか
  duskFired: false,  // きょう もう 日ぐれの ひきがねを ひいたか
  dekakeDone: false, // きょう もう 家を 出たか（出かけの 曲は 1日1回）
  fired: {},         // ひいた ひきがね  { ひきがねのID: ひいた日 }
  flags: {},         // 立てた しるし     { なまえ: 立った日 }
  items: {},         // もちもの         { なまえ: true }
  placed: {},        // 場所に 置かれた物  { 場所: [なまえ, ...] }
  visited: {},       // どこに いつ 行ったか { 場所: [日, ...] }
  num: {},           // ずっと のこる 数   { 'zukan':3, 'stamp':12 }
  today: {},         // **その日だけの 数。朝に からに なる** { 'mushi:aze':18 }
  queue: [],         // あとで 効く よやく
  npcAdd: [],        // あとから ふえた NPC  [{place, who, x, y, talks}]
  npcGone: [],       // 居なくなった NPC     ['場所:だれ']
  hold: '',          // いま 手に 持っている 道具（あみ／さお。数字キーで 持ちかえ・P8）
};

const LAST_DAY  = 31;                                  // 八月三十一日で なつやすみは おわる
// これだけ 画面を移ると 日ぐれ。**24は 多すぎた**（最遠 往復10歩の 地図で 往復しても
// 使い切れず、「今日は どこまで 行くか」の 判断が 生まれなかった／D11）。16に して 引きしめる
const DAY_STEPS = 16;
const dayT = () => clamp(WORLD.steps / DAY_STEPS, 0, 1);   // 0=あさ 1=日ぐれ

// --- しるし
const hasFlag = k => WORLD.flags[k] !== undefined;
const flagDay = k => WORLD.flags[k];                       // いつ 立ったか（何日目か）
function setFlag(k) { if (!hasFlag(k)) WORLD.flags[k] = WORLD.day; }

// --- もちもの と 置かれた物
// placed は 場所ごとの ならび： { doma: [{item:'sao', x:250, y:470}] }
const hasItem = k => !!WORLD.items[k];
// 道具（tool）を 手に 入れて、手が 空いていれば その場で 持つ（すぐ つかえるように・P8）。
// wake トリガー（あみを 配る）でも 拾い物でも ここを 通るので、どちらでも 持てる
function giveItem(k) { WORLD.items[k] = true; autoEquip(k); }
function putItem(place, k, x, y) {
  const a = WORLD.placed[place] = WORLD.placed[place] || [];
  if (!a.some(o => o.item === k)) a.push({ item:k, x:x, y:y });
}
function takeItem(place, k) {
  const a = WORLD.placed[place] || [];
  const i = a.findIndex(o => o.item === k);
  if (i >= 0) a.splice(i, 1);
  giveItem(k);
}
const itemsAt = place => WORLD.placed[place] || [];

// --- 手に 持つ 道具（P8）。数字キーで 持ちかえる。あみ／さお など tool の 物だけ
function toolsHeld() { return Object.keys(WORLD.items).filter(k => (ITEMS[k] || {}).tool); }
function holding(k) { return WORLD.hold === k && hasItem(k); }
function equip(k) { if (hasItem(k)) WORLD.hold = k; }
// 道具を 手に 入れたら、手が 空いていれば その場で 持つ（すぐ つかえるように）
function autoEquip(k) { if ((ITEMS[k] || {}).tool && !holdingAnyTool()) WORLD.hold = k; }
function holdingAnyTool() { return WORLD.hold && hasItem(WORLD.hold) && (ITEMS[WORLD.hold] || {}).tool; }

// --- 数。**ずっと のこる もの**と **その日だけの もの**を 分ける。
// ずっと のこる … 図鑑の 数・スタンプの 数・こわした 回数
// その日だけ   … その日 その場所の 虫の のこり
const numOf = k => WORLD.num[k] || 0;
function addNum(k, n) {
  WORLD.num[k] = (WORLD.num[k] || 0) + (n === undefined ? 1 : n);
  return WORLD.num[k];
}
function setNum(k, n) { WORLD.num[k] = n; }

// DESIGN.md §6 の 教訓：**「その日その場所の 虫は 有限」を かならず 守ること。**
// のこりが 無限だと 逃がしても すぐ 補充され、乱獲が いちばん とくに なる。
// はじめて 引いた ときに max を 入れ、そこから 減らしていく
function leftToday(k, max) {
  if (WORLD.today[k] === undefined) WORLD.today[k] = max;
  return WORLD.today[k];
}
function useToday(k, max, n) {
  WORLD.today[k] = Math.max(0, leftToday(k, max) - (n === undefined ? 1 : n));
  return WORLD.today[k];
}

// --- どこに いつ 行ったか。おなじ日に 何度 来ても 1回だけ かぞえる
function noteVisit(place) {
  const v = WORLD.visited[place] = WORLD.visited[place] || [];
  if (v[v.length-1] !== WORLD.day) v.push(WORLD.day);
}
const visitedOn = (place, d) => (WORLD.visited[place] || []).indexOf(d) >= 0;
const everVisited = place => (WORLD.visited[place] || []).length > 0;

// --- 1日ぶんを まっさらに（日が変わるとき）
function newDay(d) {
  WORLD.day = d; WORLD.steps = 0;
  WORLD.mukaeDone = false; WORLD.yoruDone = false; WORLD.duskFired = false;
  WORLD.dekakeDone = false;
  WORLD.today = {};        // その日だけの 数は 朝に もどる
}

// --- はじめから。しるしも もちものも すてる
function resetWorld() {
  newDay(1);
  WORLD.flags = {}; WORLD.items = {}; WORLD.placed = {}; WORLD.visited = {};
  WORLD.queue = []; WORLD.fired = {}; WORLD.num = {};
  WORLD.npcAdd = []; WORLD.npcGone = [];
  applyNpcChanges();     // SC に つけた ぶんも もとに もどす
}

// --- セーブ。ねたときに 書く。立ち位置は のこさない（つぎの日の 朝から はじまる）
function saveWorld() {
  try { localStorage.setItem(SAVE_KEY, JSON.stringify(WORLD)); } catch (e) {}
}
function loadWorld() {
  try {
    const s = localStorage.getItem(SAVE_KEY);
    if (!s) return false;
    const o = JSON.parse(s);
    if (!o || typeof o.day !== 'number') return false;
    Object.assign(WORLD, o);
    return true;
  } catch (e) { return false; }
}
function wipeSave() { try { localStorage.removeItem(SAVE_KEY); } catch (e) {} }
function savedDay() {
  try {
    const o = JSON.parse(localStorage.getItem(SAVE_KEY) || 'null');
    return (o && typeof o.day === 'number' && o.day > 1) ? o.day : 0;
  } catch (e) { return 0; }
}
