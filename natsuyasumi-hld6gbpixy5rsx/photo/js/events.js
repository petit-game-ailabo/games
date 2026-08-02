// ===== 場面を データから 組み立てる =====
// 中身は data/events.json。ここは その読みかただけ。
// 場面を すすめる 仕組みは scene.js のほう。

let EVENTS = {};

// --- じょうけん。when に 書けるものを ここで 見る。
// あとで ふえるときは ここに 足す（イベントの ひきがねも これを つかう）
function matchWhen(w, ctx) {
  if (!w) return true;
  ctx = ctx || {};
  if (w.day     !== undefined && ctx.day !== w.day)      return false;
  if (w.dayFrom !== undefined && !(ctx.day >= w.dayFrom)) return false;
  if (w.dayTo   !== undefined && !(ctx.day <= w.dayTo))   return false;
  if (w.home    !== undefined && !!ctx.home !== !!w.home) return false;
  if (w.place   !== undefined && cur !== w.place)         return false;
  if (w.who     !== undefined && ctx.who !== w.who)       return false;
  if (w.at      !== undefined && ctx.at  !== w.at)        return false;
  if (w.visited !== undefined && !everVisited(w.visited)) return false;
  if (w.today   !== undefined && !visitedOn(w.today, WORLD.day)) return false;
  if (w.flag    !== undefined && !hasFlag(w.flag))        return false;
  if (w.item    !== undefined && !hasItem(w.item))        return false;
  if (w.not     !== undefined && matchWhen(w.not, ctx))   return false;
  return true;
}

// --- '@なまえ' の 場所。**組み立てる ときの チルノの 立ち位置から 決める。**
// むかえは どの画面でも なりたつ必要があるので、座標を 画面ごとに 書かない
function anchors() {
  const sc = SC[cur];
  return {
    // すこし はなれた よこ。ここから 歩いてくる／ここへ 帰っていく
    '@yoko':   nearestFree(clamp(player.x - 175, 30, W-30),
                           Math.min(sc.yBot - 6, player.y + 42)),
    // チルノの となり
    '@tonari': nearestFree(clamp(player.x - 80, 30, W-30), player.y + 6),
  };
}

// --- 1手を 組み立てる。'@なまえ' と {hiduke} を ここで ほんとうの値に する
function buildStep(st, ctx, an) {
  const o = {};
  for (const k in st) o[k] = st[k];

  if (typeof o.text === 'string' && o.text.indexOf('{hiduke}') >= 0)
    o.text = o.text.split('{hiduke}').join(hiduke(ctx.day || 1));

  // put / walk の { at:'@なまえ' }
  if (typeof o.at === 'string' && o.at[0] === '@') {
    const p = an[o.at];
    if (p) { o.x = p.x; o.y = p.y; delete o.at; }
  }
  // cast / move の [だれ, '@なまえ', 消えるか] を [だれ, x, y, 消えるか] に のばす
  if (o.list) o.list = o.list.map(e => {
    if (typeof e[1] !== 'string' || e[1][0] !== '@') return e;
    const p = an[e[1]] || { x:player.x, y:player.y };
    return [e[0], p.x, p.y, e[2]];
  });
  return o;
}

// --- 場面ぜんたいを 組み立てる。when の かたまりは ひらいて つなげる
function buildScene(name, ctx) {
  const src = (EVENTS.scenes || {})[name] || [];
  const an = anchors();
  const out = [];
  const walk = list => {
    for (const st of list) {
      if (st.steps) { if (matchWhen(st.when, ctx)) walk(st.steps); }
      else if (matchWhen(st.when, ctx)) out.push(buildStep(st, ctx, an));
    }
  };
  walk(src);
  return out;
}

// これまでの 名まえの ままで 呼べるようにしておく
const morningScript = d => buildScene('morning', { day:d });
const mukaeScript   = () => buildScene('mukae',  { day:WORLD.day, home: cur === 'zashiki' });
const nightScript   = () => buildScene('night',  { day:WORLD.day });
const yoruScript    = () => buildScene('yoru',   { day:WORLD.day });

// ===== ひきがね =====
// data/events.json の triggers を 見て、その時が来たら do を はしらせる。
//   on     … 'enter'（画面に 入った）／'talk'（はなしが 尽きた）／'meal'（ごはん）
//            ／'dusk'（日ぐれ）／'sleep'（ねた）
//   when   … matchWhen と おなじ。書かなければ いつでも
//   repeat … 書かなければ **一度きり**。'day' なら 1日1回。'always' なら まいかい
//   do     … いまは flag（しるしを立てる）と scene（場面をはじめる）だけ。B4 でふえる
function firedKey(t) { return t.id || (t.on + JSON.stringify(t.when || {})); }

function canFire(t) {
  const f = WORLD.fired[firedKey(t)];
  if (f === undefined) return true;
  if (t.repeat === 'always') return true;
  if (t.repeat === 'day')    return f !== WORLD.day;
  return false;
}

// --- よやく。**いま 起きたことを、あとの ごはんの ときに 効かせる。**
//   later:{ at:'dinner'|'breakfast', after:N, scene:'…', flag:'…' }
//   at:'dinner'    … その日の 晩ごはん（after を 書かなければ きょう）
//   at:'breakfast' … つぎの日の 朝ごはん（after を 書かなければ あした）
function reserve(later) {
  const after = later.after !== undefined ? later.after
              : (later.at === 'breakfast' ? 1 : 0);
  WORLD.queue.push({ at:later.at, day:WORLD.day + after,
                     scene:later.scene, flag:later.flag });
}

// その ごはんで 出す ぶんを 取りだす。**日が すぎた ぶんも 出す**
// （その日 ごはんを とばしても、よやくが 消えてしまわないように）
function dueQueue(at) {
  const out = [];
  WORLD.queue = WORLD.queue.filter(q => {
    if (q.at !== at || q.day > WORLD.day) return true;
    out.push(q); return false;
  });
  return out;
}

// よやくを セリフの ならびに ひらく。**場面の とちゅうに 差しこむ ためのもの**なので、
// ここで つくる 場面には to や free を 入れないこと
function serveQueue(at, ctx) {
  const steps = [];
  for (const q of dueQueue(at)) {
    if (q.flag) setFlag(q.flag);
    if (q.scene) steps.push(...buildScene(q.scene, ctx));
  }
  return steps;
}

function runActions(dos, ctx, sceneOk) {
  for (const a of [].concat(dos || [])) {
    if (a.flag) setFlag(a.flag);
    if (a.later) reserve(a.later);
    // 場面を はじめる。**いまの場面を こわさないよう、場面の とちゅうでは 出さない。**
    // ごはん中や ねるときの 出しものは B3 の よやくで あつかう
    if (a.scene && sceneOk !== false && state !== 'scene') {
      const q = buildScene(a.scene, ctx);
      if (q.length) { runScene(q); state = 'scene'; }
    }
  }
}

function fireTriggers(on, ctx, sceneOk) {
  ctx = Object.assign({ day: WORLD.day, place: cur }, ctx || {});
  for (const t of (EVENTS.triggers || [])) {
    if (t.on !== on) continue;
    if (!canFire(t)) continue;
    if (!matchWhen(t.when, ctx)) continue;
    WORLD.fired[firedKey(t)] = WORLD.day;
    runActions(t.do, ctx, sceneOk);
  }
}
