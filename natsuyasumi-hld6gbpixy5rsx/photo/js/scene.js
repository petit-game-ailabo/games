// 自動でわけたファイル。もとは photo/index.html 1枚だった
// ===== 朝のながれを すすめる =====
// --- 分かれ道。`{k:'label', id}` へ とぶ。
// 見つからなければ とばずに つぎへ すすむ（書きまちがいで 場面が 止まらないように）
function labelIdx(id) {
  return scene.q.findIndex(s => s.k === 'label' && s.id === id);
}
function sceneJump(id) {
  const i = labelIdx(id);
  if (i < 0) return false;
  scene.i = i; scene.entered = -1; scene.t = 0;
  return true;
}

function endScene() {
  scene = null; state = 'play'; cast = []; sceneSay = null; sceneSel = null; walkTo = null;
  playerPose = 'idle'; taisoT0 = -99; veil = 0;
  nedokoArmed = false; talkLock = true;   // 場面あけに かってに 会話が はじまらないように
}
const TO_OUT = 0.55, TO_IN = 0.75;        // 場面の切りかわり：くらくなる／あかるくなる
function stepScene(dt) {
  const st = scene.q[scene.i];
  if (!st) { endScene(); return; }
  if (scene.entered !== scene.i) {
    scene.entered = scene.i; scene.t = 0;
    switch (st.k) {
      case 'put':   { const f = nearestFree(st.x, st.y); player.x=f.x; player.y=f.y; break; }
      case 'cast':  cast = st.list.map(c => ({
                      k:c[0], x:c[1], y:c[2], ph:(c[1]*0.013+c[2]*0.007),
                      pose:'idle', face:1, wbob:0, tx:0, ty:0, gone:false }));
                    break;
      case 'say':   sceneSay = [st.who, st.text]; break;
      case 'sel':   sceneSel = { st, i:0, n:st.opts.length }; break;
      // べつの あそびに 入る。おわると けっかが WORLD.num[out] に 入る。
      // 知らない 名まえなら 素通り（場面を 止めない）
      case 'mini':  if (startMini(st.name, st.cfg, st.out)) return; break;
      // ただ 見るだけの 画面（虫かご・図鑑・スタンプ・絵日記）。とじたら つづきへ
      case 'view':  if (openView(st.name, st.cfg)) return; break;
      // 書いてある ところへ とぶ。**組み立てるときでは なく、いま 見て 決める**
      case 'goto':  if (sceneJump(st.id)) return; break;
      case 'if':    if (matchWhen(st.when, { day:WORLD.day }) && sceneJump(st.go)) return;
                    break;
      case 'walk':  walkTo = { x:st.x, y:st.y }; break;
      case 'move':
        for (const [who, x, y, gone] of st.list) {
          const c = cast.find(c => c.k === who);
          if (c) { c.tx = x; c.ty = y; c.gone = !!gone; c.pose = 'walk'; }
        }
        break;
      case 'taiso':
        st.s = playTaiso(); taisoT0 = elapsed;
        taisoBeats = st.s * (TAISO_BPM/60);
        for (const c of cast) if (c.pose !== 'gone') c.pose = 'taiso';
        playerPose = 'taiso';
        break;
      // ごはんの ふし目。ここに よやくが 落ちてくる（B3）。
      // 場面の とちゅうなので、ひきがねから 場面を はじめることは できない
      case 'meal': {
        // ひきがねの ぶんと よやくの ぶんを **いまの ならびの すぐ うしろに 差しこむ**。
        // 場面を あたらしく はじめると いまの場面を こわすので、そうしない
        const add = mealSteps(st.at, { day:WORLD.day, at:st.at });
        if (add.length) scene.q.splice(scene.i + 1, 0, ...add);
        break;
      }
      case 'free':  endScene(); return;
      // なつやすみが おわった。タイトルに もどる。**つづきからは 出さない**
      case 'title': wipeSave(); endScene(); state = 'title'; fade = 0; return;
    }
  }
  scene.t += dt;
  let done = false;
  switch (st.k) {
    case 'card':  done = scene.t >= st.s; break;
    case 'wait':  done = scene.t >= st.s; break;
    case 'put':   done = true; break;
    case 'cast':  done = true; break;
    case 'meal':  done = true; break;
    case 'label': done = true; break;
    case 'mini':  done = (state !== 'mini'); break;   // あそびが おわったら つぎへ
    case 'view':  done = (state !== 'view'); break;   // とじたら つぎへ
    case 'goto':  done = true; break;     // とび先が 見つからなかった ときだけ ここ
    case 'if':    done = true; break;
    // えらぶ。**ここは 時間では 進まない。**えらぶまで 待つ
    case 'sel':
      if (!advance || scene.t < 0.25) break;
      {
        const opt = st.opts[sceneSel.i];
        sceneSel = null; advance = false;
        if (opt.do) runActions(opt.do, { day:WORLD.day }, false);
        if (opt.go && sceneJump(opt.go)) return;
        done = true;
      }
      break;
    case 'say':   done = scene.t >= sayDur(st.text) || (advance && scene.t > 0.3);
                  if (done) sceneSay = null; break;
    case 'to':
      // まっくらにしてから 画面を入れかえ、また あかるくする
      if (scene.t < TO_OUT) veil = scene.t / TO_OUT;
      else {
        if (!scene.flags[scene.i]) {
          scene.flags[scene.i] = 1;
          enter(st.sc, st.at);
          // **場面の 自動移動は「じぶんで 来た」に しない。**
          // ここを 空のままに すると、場面が おわった つぎのフレームに
          // 'enter' の ひきがねが かってに ひかれる（毎朝 どまで 起きてしまう）
          firedScreen = st.sc;
        }
        veil = clamp(1 - (scene.t - TO_OUT) / TO_IN, 0, 1);
      }
      done = scene.t >= TO_OUT + TO_IN;
      if (done) veil = 0;
      break;
    case 'taiso':
      done = scene.t >= st.s;
      if (done) {   // たいそうが おわったら ちゃんと ふだんの すがたに もどす
        for (const c of cast) if (c.pose === 'taiso') c.pose = 'idle';
        playerPose = 'idle'; taisoT0 = -99;
      }
      break;
    case 'move': {
      let all = true;
      for (const c of cast) {
        if (c.pose !== 'walk') continue;
        const dx = c.tx-c.x, dy = c.ty-c.y, dd = Math.hypot(dx, dy);
        if (dd < 5) { c.pose = c.gone ? 'gone' : 'idle'; continue; }
        all = false;
        const depth = Math.max(0.45, heightAt(c.y) / SC[cur].hNear);
        const sp = Math.min(130*depth*dt, dd);
        c.x += dx/dd*sp; c.y += dy/dd*sp;
        c.face = dx > 0 ? 1 : -1; c.wbob += dt*9;
      }
      done = all || scene.t > (st.s || 4);
      if (done) for (const c of cast) if (c.pose === 'walk') c.pose = c.gone ? 'gone' : 'idle';
      break;
    }
    case 'walk':
      done = moveMove(walkTo.x, walkTo.y, 150, dt) || scene.t > 6;
      if (done) walkTo = null;
      break;
    default: done = true;
  }
  if (done) scene.i++;
}
