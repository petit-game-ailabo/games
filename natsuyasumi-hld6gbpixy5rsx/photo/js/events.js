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
