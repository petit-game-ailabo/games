// 自動でわけたファイル。もとは photo/index.html 1枚だった
// ===== 朝のながれを すすめる =====
function endScene() {
  scene = null; state = 'play'; cast = []; sceneSay = null; walkTo = null;
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
      case 'free':  endScene(); return;
    }
  }
  scene.t += dt;
  let done = false;
  switch (st.k) {
    case 'card':  done = scene.t >= st.s; break;
    case 'wait':  done = scene.t >= st.s; break;
    case 'put':   done = true; break;
    case 'cast':  done = true; break;
    case 'say':   done = scene.t >= sayDur(st.text) || (advance && scene.t > 0.3);
                  if (done) sceneSay = null; break;
    case 'to':
      // まっくらにしてから 画面を入れかえ、また あかるくする
      if (scene.t < TO_OUT) veil = scene.t / TO_OUT;
      else {
        if (!scene.flags[scene.i]) { scene.flags[scene.i] = 1; enter(st.sc, st.at); }
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
