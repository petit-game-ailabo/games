// 自動でわけたファイル。もとは photo/index.html 1枚だった
// ===== 状態 =====
// 日を またいで のこるもの（日づけ・しるし・もちもの）は world.js の W のほう。
// ここに あるのは **いま この瞬間だけの もの**。セーブされない。
let state = 'load';
let cur = 'zashiki';
const player = { x:430, y:500, face:1, bob:0, moving:false, running:false };
let fade = 0, fadeTo = null, nameT = 0, elapsed = 0, exitLock = false;
let talkNpc = null, lineT = 0;
let scene = null;          // {q, i, t, entered, flags}
// その場面だけのキャラ。ひとりずつ じぶんの じょうたい を持つ
//   pose … 'idle'（ふよふよ）／'walk'（歩いている）／'taiso'（たいそう中）／'gone'（もう居ない）
let cast = [];
let playerPose = 'idle';
let sceneSay = null;       // 場面のセリフ [who, text]
let sceneSel = null;       // えらんでいる とちゅう { st, i, n }
let selRect = null;        // えらぶ まどの 場所。タップの あたり判定に つかう
let walkTo = null;         // じどうで あるく先
let taisoT0 = -99, taisoBeats = 0;
let veil = 0;              // 場面の切りかわりの 黒い幕
let talkLock = false;      // 場面が おわった直後に かってに 会話が はじまらないように
let nedokoT = 0, nedokoArmed = false;
let nightT = 0;            // よるの ぐあい 0〜1。晩ごはんが すむと ゆっくり 1 へ
let firedScreen = null;    // 'enter' の ひきがねを もう ひいた画面。じぶんで移ると 空にもどす
let nearSpot = null;       // いま そばに ある 点（画面の中の 調べられる ところ）
let nearItem = null;       // いま そばに ある 置かれた物（キーで ひろう）
let gateSaid = null;       // わけを 言った せき止め。はなれるまで 言いなおさない

function moveMove(x, y, spd, dt) {   // 目的地へ 一歩ぶん すすむ（壁ぞいにすべる）
  const dx = x - player.x, dy = y - player.y, d = Math.hypot(dx, dy);
  if (d < 6) return true;
  const step = Math.min(spd*dt, d);
  const nx = player.x + dx/d*step, ny = player.y + dy/d*step;
  let mx = false, my = false;
  if (walkable(nx, player.y)) { player.x = nx; mx = true; }
  if (walkable(player.x, ny)) { player.y = ny; my = true; }
  const slide = Math.max(4, step*2.6);
  if (!my && Math.abs(dy) > 0.5) {
    for (let k=1; k<=slide; k+=0.5) {
      if (walkable(player.x-k, ny)) { player.x -= k; player.y = ny; break; }
      if (walkable(player.x+k, ny)) { player.x += k; player.y = ny; break; }
    }
  }
  if (!mx && Math.abs(dx) > 0.5) {
    for (let k=1; k<=slide; k+=0.5) {
      if (walkable(nx, player.y-k)) { player.y -= k; player.x = nx; break; }
      if (walkable(nx, player.y+k)) { player.y += k; player.x = nx; break; }
    }
  }
  if (dx > 1) player.face = 1; else if (dx < -1) player.face = -1;
  player.bob += dt*9;
  return false;
}

function enter(id, at) {
  cur = id;
  // どこに いつ 行ったかを のこす。あとの よやくが これを 見る。
  // **場面の 自動移動は かぞえない。** 朝のながれで 通るだけの どまや いえのまえを
  // 「自分で 行った」に すると、よやくが かってに 起きてしまう
  if (state !== 'scene') noteVisit(id);
  const st = SC[id].start;
  const p = st[at] || st[Object.keys(st)[0]];
  const f = nearestFree(p[0], p[1]);
  player.x = f.x; player.y = f.y;
  nameT = 3.2; exitLock = true; talkNpc = null; lineT = 0;
}
// その日の セリフを えらぶ。**どれを えらんだか**（key）も いっしょに 返す。
//   L  … セリフの ならび（なければ null）
//   key … 'flat'＝ふつうの ならび／数字＝えらんだ かたまりの 番号／-1＝どれにも あわず
// key を 返すのは、会話の とちゅうで 竿を もった等で かたまりが 差しかわったのを
// 気づくため（game.js の talk が 見て、idx を もどして 話しなおす）。
// place … その NPC の いる 画面。省くと いまの画面（cur）。resetDay は 全画面を
// まとめて 見るので、when:{place} が その画面で 正しく 効くよう 画面を わたす（G4）。
// 日づけの キーは ふつう "1" "2" の ような ぴったりの 数。だが 29日ぶんを ぜんぶ
// 手で 書くのは 多いので、**範囲キー**も 引ける ようにした（D0）：
//   "3-7"  … 3日から 7日まで   ／  "8+"  … 8日いこう ずっと
// ぴったりの 日が あれば それを 優先。無ければ 範囲キーの うち あう さいしょを つかう。
function pickDayKey(t) {
  if (t[WORLD.day] !== undefined) return WORLD.day;
  for (const k in t) {
    const m = /^(\d+)-(\d+)$/.exec(k) || /^(\d+)\+$/.exec(k);
    if (!m) continue;
    const a = +m[1], b = m[2] !== undefined ? +m[2] : Infinity;
    if (WORLD.day >= a && WORLD.day <= b) return k;
  }
  return null;
}
function linesPick(n, place) {
  const t = talksOf(n);
  if (!t) return { L:null, key:null };
  const dk = pickDayKey(t);
  const v = dk === null ? null : t[dk];
  if (!v) return { L:null, key:null };
  // ふつうは [話し手, ことば] の ならび（v[0] は 配列）。
  // でも v[0] が オブジェクトなら、それは {when, lines} の かたまりの ならび。
  // 竿を もったら／お手伝いを したら セリフが 変わる、を データだけで 書くための もの。
  // じょうけんに あう さいしょの かたまりを えらぶ（when を 書かなければ いつでも）。
  if (v.length && v[0] && !Array.isArray(v[0]) && typeof v[0] === 'object') {
    const p = place || cur;
    const ctx = { day: WORLD.day, place: p, home: p === 'zashiki' };
    for (let i = 0; i < v.length; i++)
      if (matchWhen(v[i].when, ctx)) return { L: v[i].lines || null, key:i };
    return { L:null, key:-1 };
  }
  return { L:v, key:'flat' };
}
function linesOf(n, place) { return linesPick(n, place).L; }
function resetDay() {
  applyNpcChanges();   // あとから ふえた／消えた NPC を つけ直す
  for (const k in SC) for (const n of (SC[k].npc || [])) {
    n.idx = 0; n.done = !linesOf(n, k); n.engaged = false;   // その NPC の 画面 k で 見る
  }
  talkNpc = null; nedokoT = 0; nedokoArmed = false;
  WORLD.steps = 0; WORLD.mukaeDone = false; WORLD.yoruDone = false;
  nightT = 0;              // 朝は かならず 明るいところから。だんだん 明るくは しない
}
function runScene(q) {
  scene = { q, i:0, t:0, entered:-1, flags:{} };
  sceneSay = null; sceneSel = null;
  walkTo = null; cast = []; playerPose = 'idle'; taisoT0 = -99; veil = 0;
}
function startMorning(head) {
  resetDay();
  // 朝の はじまり。ここで 場面を はじめると すぐ下の 朝のながれで 上書きされるので、
  // しるしや よやくを つける ためだけの ひきがね
  fireTriggers('wake', {}, false);
  runScene([...(head || []), ...morningScript(WORLD.day)]);
  state = 'scene';
}
// はじめから。**額縁（いまの話）から はじまる**
function start()  { resetWorld(); startMorning(buildScene('gakubuchi', { day:1 })); fade = 1; }
// つづきから。ねたときの ぶんを よみ、その日の 朝から はじめる。額縁は 出さない
function resume() { loadWorld(); startMorning(); fade = 1; }
// ねる。日が変わって、そこで セーブする
function sleepNow() {
  // ねる ひきがねは 場面を はじめない（すぐ下で よるの場面に なるので こわしてしまう）。
  // ねぎわの 出しものは B3 の よやくで あつかう
  fireTriggers('sleep', {}, false);
  // **きょうの** よるの場面を、日が 変わる まえに 組んでおく。
  // newDay の あとで 組むと day が もう 進んでいて、絵日記など 日別の 出しものが
  // 翌日づけに なってしまう（いまの night は 日別分岐が ないので 見た目は 同じだが、D5 の 前提）
  const night = nightScript();
  // 絵日記（D5）は **その日づけの うちに** 中身を 確定させる。newDay の あとに 綴じると
  // 翌日づけを 拾ってしまう（G3）。VIEW.nikki は この diaryData を 読む
  diaryData = { day: WORLD.day, lines: diaryToday(WORLD.day) };
  // 八月三十一日の よるで なつやすみは おわる。つぎの朝は 来ない
  if (WORLD.day >= LAST_DAY) {
    runScene([...night, ...buildScene('owari', { day:WORLD.day })]);
    state = 'scene';
    return;
  }
  newDay(WORLD.day + 1);
  // あたらしい 日の はじまり。**wake は 毎朝の フック**（ラジオ体操の 判こ など・D4）。
  // start()／resume() の startMorning だけでなく、ふつうに ねて 起きた 日にも 要る
  fireTriggers('wake', {}, false);
  saveWorld();
  runScene([...night, ...morningScript(WORLD.day)]);
  state = 'scene'; resetDay();
}
