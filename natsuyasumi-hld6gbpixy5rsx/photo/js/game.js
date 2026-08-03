// 自動でわけたファイル。もとは photo/index.html 1枚だった
// ===== ループ =====
let last = performance.now();
function loop(now) {
  const dt = Math.min(0.05, (now - last)/1000); last = now;
  elapsed += dt;

  if (state === 'load') {
    ctx.fillStyle = '#07100b'; ctx.fillRect(0,0,W,H);
    text('よみこみちゅう…', W/2, H/2, 20, '#7fa87a', 'center');
    requestAnimationFrame(loop); return;
  }
  // データが よめなかったとき。だまって うごかすと 原因が わからなくなる
  if (state === 'error') {
    ctx.fillStyle = '#1b0e0c'; ctx.fillRect(0,0,W,H);
    text(dataErr, W/2, H/2 - 12, 19, '#ffb3a2', 'center');
    text('れい:  python -m http.server 8000  →  http://127.0.0.1:8000/photo/',
         W/2, H/2 + 22, 15, '#d8bcb4', 'center', 'normal');
    requestAnimationFrame(loop); return;
  }
  if (state === 'title') {
    ctx.drawImage(SC.aze.img, 0, 0, W, H);
    ctx.fillStyle = 'rgba(6,14,8,0.45)'; ctx.fillRect(0,0,W,H);
    text('なつやすみ', W/2, 200, 46, '#ffffff', 'center');
    text('しゃしんの なかを あるく', W/2, 240, 18, '#dbead2', 'center');
    text('八月一日から 三十一日まで', W/2, 272, 15, '#a8c0a1', 'center', 'normal');
    text('やじるし / WASD で あるく　　Shift で はしる', W/2, 340, 16, '#c9dcc0', 'center');
    text('だれかや ものの そばで スペース / タップ で はなす・しらべる', W/2, 368, 16, '#c9dcc0', 'center');
    text('スペース / タップ で セリフを つぎへ', W/2, 396, 16, '#c9dcc0', 'center');
    text('ざしきの ふとんで じっとしていると 1にちが おわる', W/2, 424, 16, '#c9dcc0', 'center');
    // ねた ところから つづけられる。だまって つづきに するのは わかりにくいので、
    // ある ときだけ 出して、はじめから やりなおす道も のこす
    const sd = savedDay();
    if (sd) {
      text(usingTouch ? 'タップで つづきから（八月' + hiduke(sd) + '日）'
                      : 'スペースキーで つづきから（八月' + hiduke(sd) + '日）',
           W/2, 452, 16, '#ffe6a8', 'center');
      text(usingTouch ? 'ここを タップで はじめから' : 'R キーで はじめから',
           W/2, 476, 15, '#a9bfa2', 'center');   // この もじの ところが RESTART_BOX
    } else {
      text(usingTouch ? 'がめんを ドラッグ ではじまる' : 'スペースキー ではじまる', W/2, 456, 16, '#c9dcc0', 'center');
    }
    text('背景写真: Guilhem Vellut / 663highland / Fumihiko Ueno（CC BY・Wikimedia Commons）',
         W/2, 502, 12, 'rgba(226,238,220,0.72)', 'center', 'normal');
    text('キャラ: Majstek — 非商用　／　ラジオたいそうの曲は じさく（原曲は使っていません）',
         W/2, 520, 12, 'rgba(226,238,220,0.72)', 'center', 'normal');
    requestAnimationFrame(loop); return;
  }

  // 見るだけの 画面の あいだ
  if (state === 'view') {
    viewStep(dt);
    if (state === 'view') { viewDraw(); advance = false; requestAnimationFrame(loop); return; }
  }

  // べつの あそびの あいだは、画面ぜんぶを あそびに わたす
  if (state === 'mini') {
    miniStep(dt);
    if (state === 'mini') {
      miniDraw(); advance = false; requestAnimationFrame(loop); return;
    }
    // この フレームで おわった ときは、そのまま 下の 場面の つづきへ
  }

  const inScene = (state === 'scene');
  if (inScene) stepScene(dt);
  const sc = SC[cur];

  // 釣り（P4b）。あたりの あいだに 押すと つれる。**ほかの 反応より さきに** advance を うけとる
  // （その場での 釣りなので、話しかけ・調べる・網ふりに 取られないように）
  if (!inScene && !fadeTo) fishingTick(dt);

  if (!fadeTo && !walkable(player.x, player.y)) {
    const f = nearestFree(player.x, player.y);
    player.x = f.x; player.y = f.y;
  }

  // --- うごく（自由行動のときだけ）。釣りの あいだは 足を とめる
  let ax=0, ay=0, tilt=0;
  if (!fadeTo && !inScene && !fishing) {
    for (const c in MOVE) if (keys[c]) { ax += MOVE[c][0]; ay += MOVE[c][1]; }
    if (ax || ay) { const m = Math.hypot(ax,ay); ax/=m; ay/=m; tilt = 1; }
    if (stick.on) {
      const dx = stick.x-stick.ox, dy = stick.y-stick.oy, dd = Math.hypot(dx,dy);
      if (dd > 12) { tilt = Math.min(1, (dd-12)/62); ax = dx/dd; ay = dy/dd; }
    }
  }
  player.running = !inScene && (!!(keys.ShiftLeft || keys.ShiftRight) || (stick.on && tilt > 0.72));
  player.moving = (ax !== 0 || ay !== 0) || !!walkTo;

  if (ax !== 0 || ay !== 0) {
    const depth = Math.max(0.45, heightAt(player.y) / sc.hNear);
    const spd = (player.running ? 300 : 172) * depth * (stick.on ? Math.max(0.35, tilt) : 1);
    const step = spd*dt;
    // 横は 遅く 感じる との こと（P3）。奥ゆきの つりあい（D-017）は 変えず、**横だけ 速く**する
    const stepX = step * 1.55;
    const nx = player.x + ax*stepX, ny = player.y + ay*step;
    let movedX = false, movedY = false;
    if (ax !== 0 && walkable(nx, player.y)) { player.x = nx; movedX = true; }
    if (ay !== 0 && walkable(player.x, ny)) { player.y = ny; movedY = true; }
    const slide = Math.max(4, step * 2.6);
    if (ay !== 0 && !movedY) {
      for (let k = 1; k <= slide; k += 0.5) {
        if (walkable(player.x - k, ny)) { player.x -= k; player.y = ny; break; }
        if (walkable(player.x + k, ny)) { player.x += k; player.y = ny; break; }
      }
    }
    if (ax !== 0 && !movedX) {
      for (let k = 1; k <= slide; k += 0.5) {
        if (walkable(nx, player.y - k)) { player.y -= k; player.x = nx; break; }
        if (walkable(nx, player.y + k)) { player.y += k; player.x = nx; break; }
      }
    }
    if (ax > 0.2) player.face = 1; else if (ax < -0.2) player.face = -1;
    player.bob += dt * (player.running ? 15 : 9);
  } else if (!walkTo) {
    player.bob += dt * 1.6;
  }

  // --- 画面のはしへ（自由行動のときだけ）
  const onExit = e => player.x > e.x0 && player.x < e.x1 && player.y > e.y0 && player.y < e.y1;
  if (exitLock && !sc.exits.some(onExit)) exitLock = false;
  if (!fadeTo && !inScene && !exitLock) {
    for (const e of sc.exits) if (onExit(e)) { fadeTo = e; break; }
  }
  if (fadeTo) {
    // くらくする → 入れかえる → あかるくする。ふたつを 同じフレームで やると
    // 増える量と 減る量が 打ち消しあって 抜けられなくなるので、はっきり分ける
    if (!fadeTo.done) {
      fade += dt * 3.4;
      if (fade >= 1) {
        fade = 1;
        // 画面を移ると 時間がすすむ。**じぶんで 移ったときだけ** 'enter' を ひきなおす
        if (fadeTo.to) { enter(fadeTo.to, fadeTo.at); WORLD.steps++; firedScreen = null; }
        fadeTo = { done:true };
      }
    } else {
      fade -= dt * 2.6;
      if (fade <= 0) { fade = 0; fadeTo = null; }
    }
  } else if (fade > 0) {
    fade = Math.max(0, fade - dt*1.4);
  }

  // --- ひきがね：画面に 入ったとき。1つの画面に つき 1回
  if (!inScene && !fadeTo && !talkNpc && state !== 'scene' && firedScreen !== cur) {
    firedScreen = cur;
    fireTriggers('enter');
  }

  // --- ひきがね：日ぐれ。むかえより さきに ひく
  if (!inScene && !fadeTo && !talkNpc && state !== 'scene'
      && !WORLD.duskFired && WORLD.steps >= DAY_STEPS) {
    WORLD.duskFired = true;
    fireTriggers('dusk');
  }

  // --- 日がくれたら けーねが むかえに来る。はなしの とちゅうでは 割りこまない
  if (!inScene && !fadeTo && !WORLD.mukaeDone && !talkNpc && state !== 'scene'
      && WORLD.steps >= DAY_STEPS) {
    WORLD.mukaeDone = true;
    runScene(mukaeScript());
    state = 'scene';
  }

  // --- 日がくれて うちに かえったら、晩ごはんと 縁側。1日1回だけ。
  // むかえ（さきに 発火する）で かえってきても、じぶんで かえってきても なりたつ
  if (!inScene && !fadeTo && !WORLD.yoruDone && !talkNpc && state !== 'scene'
      && WORLD.steps >= DAY_STEPS && cur === 'zashiki') {
    WORLD.yoruDone = true;
    runScene(yoruScript());
    state = 'scene';
  }

  // --- 画面の中の 点。そばに 行くと 名まえが 出て、はじめてなら 場面が うごく。
  // 会話でも 拾い物でも ない 相手（こよみ・水べ・賽銭箱…）は ここが 受け口
  // --- せき止めの まえに 立ったら わけを 言う。**理由は 子供っぽく**（DESIGN.md §1）
  if (!inScene && !fadeTo && !talkNpc && state !== 'scene') {
    const g = gateAt(player.x, player.y, 26);
    if (g && g !== gateSaid) {
      gateSaid = g;
      if (g.why) { runScene([{ k:'say', who:'cirno', text:g.why }]); state = 'scene'; }
    } else if (!g) gateSaid = null;
  }

  // 画面の中の 点。そばに 行くと 名まえが 出る。**調べるのは キーを 押したとき**（P1）。
  // ここでは そばの 点を 見つけるだけ。ひきがねは 下の「調べる／ひろう」で ひく
  nearSpot = null;
  if (!inScene && !fadeTo && !talkNpc && state !== 'scene') {
    for (const sp of (SC[cur].spot || [])) {
      if (!sp.name && !sp.scene && !sp.use) continue;      // 音だけの 点は 相手に しない
      if (groundDist(player.x, player.y, sp.x, sp.y) > (sp.r || 1.2)) continue;
      nearSpot = sp;
      break;
    }
  }

  // 置かれた物も、近づいただけでは ひろわない。そばで キーを 押したら ひろう（P1）
  nearItem = null;
  if (!inScene && !fadeTo && !talkNpc && state !== 'scene') {
    for (const o of itemsAt(cur)) {
      if (groundDist(player.x, player.y, o.x, o.y) < 1.1) { nearItem = o; break; }
    }
  }

  // --- ふとんで じっとしていたら 1にちが おわる
  if (!inScene && !fadeTo && sc.nedoko) {
    const inBed = dist(player.x, player.y, sc.nedoko.x, sc.nedoko.y) < sc.nedoko.r;
    if (!inBed) nedokoArmed = true;
    if (inBed && nedokoArmed && !player.moving) {
      nedokoT += dt;
      if (nedokoT > 1.0) sleepNow();
    } else nedokoT = 0;
  }

  // --- はなし。**そばで キーを 押すと はじまる**（P1。近づいただけでは はじめない）。
  // いちど はなしかけた 相手は はなれても 覚えていて、戻れば つづきから すすむ。
  // キャラ同士の 自動会話（場面）は これとは 別で、そのまま
  let nearNpc = null, anyNear = false;
  for (const n of (sc.npc || [])) {
    for (const w of n.who) {
      if (groundDist(player.x, player.y, w[1], w[2]) < TALK_R) {
        anyNear = true;
        if (!n.done && !nearNpc) nearNpc = n;
      }
    }
  }
  // 場面が おわった直後に となりに 居あわせただけで はじまらないように、
  // いちど はなれてから でないと 会話しない
  if (!anyNear) talkLock = false;
  if (fadeTo || inScene || talkLock || fishing) nearNpc = null;

  let near = null;
  if (nearNpc) {
    if (nearNpc.engaged) near = nearNpc;                 // もう 話しかけた 相手＝つづける
    else if (advance) { nearNpc.engaged = true; near = nearNpc; advance = false; }  // キーで はじめる
  }
  if (near !== talkNpc) { talkNpc = near; lineT = 0; }
  if (talkNpc) {
    const pick = linesPick(talkNpc);
    const L = pick.L;
    if (!L) {
      // その日の セリフが ない のに 会話に 入ってしまった（日づけだけ 変わった ときなど）。
      // ここで 落ちると ループ ごと 止まるので、だまって 閉じる
      talkNpc.done = true; talkNpc = null;
    } else {
      // 会話の とちゅうで 竿を もった等で セリフの かたまりが 差しかわったら、
      // はじめから 話しなおす。古い idx の まま だと 短い かたまりで はみ出して 落ちる
      if (talkNpc._blk !== pick.key) { talkNpc._blk = pick.key; talkNpc.idx = 0; lineT = 0; }
      const idx = Math.min(talkNpc.idx || 0, L.length - 1);   // 念のための 頭打ち
      const li = L[idx];
      lineT += dt;
      if (lineT >= sayDur(li[1]) || (advance && lineT > 0.3)) {
        talkNpc.idx = idx + 1;
        lineT = 0;
        if (talkNpc.idx >= L.length) {
          // はなしが 尽きた。だれとの はなしだったかを ひきがねに わたす
          const who = talkNpc.who[0][0];
          talkNpc.done = true; talkNpc = null;
          fireTriggers('talk', { who });
        }
      }
    }
  }

  // --- そばの 点を 調べる／物を ひろう。**キーを 押したときだけ**（P1）。
  // 会話を はじめた フレームは advance を 使いきっているので ここには 来ない
  if (!inScene && !fadeTo && !talkNpc && !fishing && state !== 'scene' && advance) {
    if (nearSpot && nearSpot.fish && hasItem('sao')) {
      // 水べで 竿を ふる → その場で 釣り（P4b・別画面に とばない）
      advance = false;
      startFishing();
    } else if (nearSpot) {
      advance = false;
      fireTriggers('near', { spot:nearSpot.id });
      // その点で つかう 道具を 持っていたら「つかった」も ひく
      if (nearSpot.use && hasItem(nearSpot.use))
        fireTriggers('use', { spot:nearSpot.id, item:nearSpot.use });
      if (nearSpot.scene && !hasFlag('spot:' + nearSpot.id) && state !== 'scene') {
        setFlag('spot:' + nearSpot.id);
        const q = buildScene(nearSpot.scene, { day:WORLD.day });
        if (q.length) { runScene(q); state = 'scene'; }
      }
    } else if (nearItem) {
      advance = false;
      const o = nearItem, it = ITEMS[o.item] || {};
      takeItem(cur, o.item);
      // ひろったことを 起点に できるように、**ひきがねを 通す**。
      // ひきがねが 足した セリフは ひとことの うしろに つなげる
      const q = [{ k:'say', who:'cirno',
                   text: it.found || ((it.name||o.item) + ' を みつけた') }];
      q.push(...collectTriggers('take', { item:o.item }));
      runScene(q);
      state = 'scene';
    } else if (hasItem('ami') && bugsActive()) {
      // あみを 持って 野に いる ときは、キーで その場で あみを ふる（P4・別画面に とばない）
      advance = false;
      swingNet();
    }
  }

  // --- 虫や 鳥。時間帯で 鳴くものが 変わる（audio.js の ambientTick）
  ambientTick(dt);
  footTick();                    // 足音。画面ごとに ふみごこちが ちがう
  utaTick(dt);                   // 遠くの うた。地図の かわりに 耳で さがす
  mizuTick();                    // 水の音。**音源に 近いほど 大きい**
  if (!inScene && !fadeTo) updateBugs(dt);   // 画面の中を とぶ 蝶（P4）
  else bugs = [];                            // 場面の あいだは 出さない
  toastTick(dt);                 // 画面下の 短い しらせ

  // --- えがく
  ctx.drawImage(sc.img, 0, 0, W, H);
  if (sc.nedoko && sc.nedoko.quad) drawFuton(sc.nedoko);
  for (const o of itemsAt(cur)) drawItem(o);   // 置かれた物は 地めんの上。キャラより さき
  drawSpots('ground');                         // すいか・賽銭箱 など 地めんの物（P5）。キャラより 後ろ

  // ラジオたいそうの ひょうし。曲が おわったら もう はずまない
  const tb = (elapsed - taisoT0) * (TAISO_BPM/60);
  const beat = (taisoT0 > -90 && tb >= 0 && tb < taisoBeats) ? tb : -1;
  const hop  = beat >= 0 ? Math.abs(Math.sin(beat*Math.PI)) : 0;
  const nobi = beat >= 0 && (Math.floor(beat) % 8) >= 4 ? 1 : 0;

  // だれが どの すがたで居るかは、そのキャラ じしんが 持っている
  const actors = [];
  if (inScene) {
    for (const c of cast) if (c.pose !== 'gone')
      actors.push({ k:c.k, x:c.x, y:c.y, ph:c.ph, pose:c.pose, face:c.face, wbob:c.wbob });
  } else {
    for (const n of (sc.npc || [])) for (const w of n.who)
      actors.push({ k:w[0], x:w[1], y:w[2], ph:(w[1]*0.013 + w[2]*0.007), pose:'idle', face:1 });
  }
  actors.push({ k:'cirno', x:player.x, y:player.y, me:true,
                pose:inScene ? playerPose : 'idle', face:player.face });
  actors.sort((a,b) => a.y - b.y);
  for (const a of actors) {
    let h = heightAt(a.y);
    let off;
    if (a.pose === 'taiso') {
      h *= 1 + 0.10*nobi*hop;
      off = h*0.14*hop;
    } else if (a.me) {
      off = player.moving ? Math.abs(Math.sin(player.bob)) * h*0.035
                          : Math.sin(player.bob) * h*0.008;
    } else if (a.pose === 'walk') {
      off = Math.abs(Math.sin(a.wbob)) * h*0.05 + h*0.05;
    } else if (castOf(a.k).float) {
      // ふよふよ。妖精や妖怪なので すこし浮いている
      off = Math.sin(elapsed*1.7 + a.ph*7) * h*0.055 + h*0.06;
    } else {
      off = 0;                       // 人は 浮かない。地面に立つ
    }
    const sh = castOf(a.k).shadow;
    shadow(a.x, a.y, h, a.me ? 1 : (sh === undefined ? 0.65 : sh));
    drawChar(ciOf(a.k), a.x, a.y - off, h, a.face < 0, hazeOf(a.y));
  }
  drawSpots('hang');   // 風鈴・日めくり など ぶらさがる物（P5）。キャラより 前
  // 蝶と あみは キャラより 前（とんでいるので）。場面の あいだは 出さない
  if (!inScene && !fadeTo) { drawBugs(); drawNet(); drawFishing(); }

  // よるの ぐあい。晩ごはんが すむと ゆっくり よるに なる。
  // **ゆうがた（あたたかい）と よる（つめたい）は 別のもの。**
  // ゆうがたを こくして いっても よるには ならない
  const wantNight = WORLD.yoruDone ? 1 : 0;
  nightT += clamp(wantNight - nightT, -dt*0.5, dt*0.5);

  // ゆうがた。時計は出さず、光の色だけで 時間の ながれを 見せる
  const ev = clamp((dayT() - 0.34) / 0.66, 0, 1) * (1 - nightT);
  if (ev > 0.01) {
    ctx.save();
    ctx.globalCompositeOperation = 'multiply';
    ctx.globalAlpha = 0.92*ev;
    ctx.fillStyle = 'rgb(255,' + Math.round(176 - 44*ev) + ',' + Math.round(122 - 52*ev) + ')';
    ctx.fillRect(0,0,W,H);
    ctx.globalCompositeOperation = 'source-over';
    ctx.globalAlpha = 1;
    ctx.fillStyle = 'rgba(28,22,52,' + (0.30*ev*ev).toFixed(3) + ')';   // 日がおちて くらくなる
    ctx.fillRect(0,0,W,H);
    ctx.restore();
  }
  // よる。青に 寄せて 沈める。ふちを おとすと 目が 慣れていない 感じが 出る
  if (nightT > 0.01) {
    ctx.save();
    ctx.globalCompositeOperation = 'multiply';
    ctx.globalAlpha = 0.94*nightT;
    ctx.fillStyle = 'rgb(92,112,166)';
    ctx.fillRect(0,0,W,H);
    ctx.globalCompositeOperation = 'source-over';
    ctx.globalAlpha = 1;
    ctx.fillStyle = 'rgba(8,12,32,' + (0.34*nightT).toFixed(3) + ')';
    ctx.fillRect(0,0,W,H);
    ctx.globalAlpha = 0.55*nightT;
    ctx.fillStyle = yoruVig();
    ctx.fillRect(0,0,W,H);
    ctx.restore();
  }

  // 場所の名まえ
  if (nameT > 0) {
    nameT -= dt;
    const a = clamp(nameT > 2.6 ? (3.2-nameT)/0.6 : Math.min(1, nameT/0.9), 0, 1);
    ctx.save(); ctx.globalAlpha = a;
    ctx.fillStyle = 'rgba(8,16,10,0.42)'; ctx.fillRect(0, 40, 250, 44);
    text(sc.name, 24, 71, 25, '#ffffff');
    ctx.restore();
  }

  // はなしの まど
  if (inScene && sceneSel) selRect = selBox(sceneSel);
  else if (inScene && sceneSay) sayBox(sceneSay[0], sceneSay[1]);
  else if (talkNpc) {
    const li = linesOf(talkNpc)[talkNpc.idx || 0];
    sayBox(li[0], li[1]);
  }
  // そばに 相手が いるとき、何が できるかを そっと 出す（P1）。**写真に 光は のせない。**
  // 実写の うえに 目じるしを かくと 貼りものに 見えるので、画面下の もじだけで 知らせる
  else if (!fadeTo && !fishing) {
    let hint = null;
    if (nearNpc && !nearNpc.engaged) hint = '▶ はなす';
    else if (nearSpot && nearSpot.fish && hasItem('sao')) hint = '▶ さおを ふる';
    else if (nearSpot && nearSpot.name) hint = nearSpot.name + '　　▶ しらべる';
    else if (nearItem) {
      const it = ITEMS[nearItem.item] || {};
      hint = (it.name || nearItem.item) + '　　▶ ひろう';
    }
    if (hint) {
      ctx.save();
      ctx.fillStyle = 'rgba(8,12,9,0.42)'; ctx.fillRect(0, H-46, W, 46);
      text(hint, W/2, H-18, 17, 'rgba(246,250,242,0.92)', 'center');
      ctx.restore();
    }
  }
  drawToast();   // 「つかまえた！」などの 短い しらせ（P4）

  if (fade > 0) {
    // 白い光だと 目に いたい。くらくして 切りかえる
    ctx.fillStyle = 'rgba(7,10,8,' + clamp(fade,0,1)*0.94 + ')';
    ctx.fillRect(0,0,W,H);
  }
  // 場面の切りかわり。いちど まっくらに してから つぎの場所へ
  if (veil > 0) {
    ctx.fillStyle = 'rgba(6,9,7,' + clamp(veil,0,1) + ')';
    ctx.fillRect(0,0,W,H);
  }

  // 日づけ／よる の 黒い画面
  if (inScene) {
    const st = scene && scene.q[scene.i];
    if (st && st.k === 'card') {
      const a = clamp(Math.min(scene.t/0.5, (st.s-scene.t)/0.6), 0, 1);
      ctx.fillStyle = 'rgba(8,10,9,' + (0.94*Math.min(1, a*1.6)) + ')'; ctx.fillRect(0,0,W,H);
      ctx.save(); ctx.globalAlpha = a;
      // 日づけは 大きく。額縁や おわりの かたりは 小さく（size で 変える）
      text(st.text, W/2, H/2 + 10, st.size || 40, '#f2f6ee', 'center');
      ctx.restore();
    }
  }

  // --- ?edit=1 の下書き
  if (EDIT) {
    ctx.save();
    ctx.strokeStyle = 'rgba(255,60,60,0.95)'; ctx.lineWidth = 2;
    for (const p of sc.walk) {
      ctx.beginPath(); ctx.moveTo(p[0][0], p[0][1]);
      for (let i=1;i<p.length;i++) ctx.lineTo(p[i][0], p[i][1]);
      ctx.closePath(); ctx.stroke();
    }
    ctx.strokeStyle = 'rgba(255,180,60,0.95)';
    for (const s of (sc.solid || [])) {
      ctx.beginPath(); ctx.ellipse(s[0], s[1], s[2], s[3], 0, 0, Math.PI*2); ctx.stroke();
    }
    ctx.strokeStyle = 'rgba(90,220,255,0.8)';
    for (const e of sc.exits) ctx.strokeRect(e.x0, e.y0, e.x1-e.x0, e.y1-e.y0);
    if (sc.nedoko) {
      ctx.strokeStyle = 'rgba(255,140,220,0.9)';
      ctx.beginPath(); ctx.arc(sc.nedoko.x, sc.nedoko.y, sc.nedoko.r, 0, Math.PI*2); ctx.stroke();
    }
    ctx.strokeStyle = 'rgba(120,255,150,0.85)';
    for (const n of (sc.npc || [])) for (const w of n.who) {
      ctx.beginPath();
      for (let i=0; i<=48; i++) {
        const a = i/48*Math.PI*2;
        let lo = 0, hi = 900;
        for (let k=0; k<18; k++) {
          const mid = (lo+hi)/2;
          if (groundDist(w[1]+Math.cos(a)*mid, w[2]+Math.sin(a)*mid, w[1], w[2]) < TALK_R) lo = mid;
          else hi = mid;
        }
        const px = w[1]+Math.cos(a)*lo, py = w[2]+Math.sin(a)*lo;
        if (i === 0) ctx.moveTo(px, py); else ctx.lineTo(px, py);
      }
      ctx.stroke();
    }
    ctx.fillStyle = 'rgba(0,0,0,0.6)'; ctx.fillRect(0, H-58, W, 58);
    text(cur + '  八月' + WORLD.day + '日  [' + Math.round(mouse.x) + ', ' + Math.round(mouse.y)
       + ']  せ:' + Math.round(heightAt(player.y)) + '  ' + (walkable(player.x,player.y)?'ゆか':'そと'),
       12, H-34, 15, '#ffe08a');
    text('クリックで点を置く／Backspaceで戻す／Cでコンソールに出力：' + JSON.stringify(editPts).slice(0,84),
       12, H-12, 13, '#bcd6ff');
    ctx.restore();
  }

  advance = false;   // 1フレームで つかいきる
  requestAnimationFrame(loop);
}
// ===== よみこみ =====
let pending = 0;
function done() { if (--pending === 0) { resetDay(); state = dataErr ? 'error' : 'title'; } }
function load() {
  pending = 1;
  imgChars.onload = done;
  imgChars.src = 'data:image/png;base64,' + CHARS_B64;
  pending++;
  loadData('data/cast.json', j => { CAST = j.cast; }, done);
  // 画面が よめてから でないと、どの写真を よむかが わからない。
  // 写真の ぶんを 足すのは この中。screens ぶんの done() は そのあとに 来る
  pending++;
  loadData('data/talks.json', j => { TALKS = j.talks; }, done);
  pending++;
  loadData('data/events.json', j => { EVENTS = j; }, done);
  pending++;
  loadData('data/items.json', j => { ITEMS = j.items; }, done);
  pending++;
  loadData('data/screens.json', j => { SC = j.screens; loadPhotos(); }, done);
}
function loadPhotos() {
  for (const k in SC) {
    pending++;
    const im = new Image();
    im.onload = done; im.onerror = done;
    im.src = SC[k].src;
    SC[k].img = im;
  }
}
load();
requestAnimationFrame(loop);

// 録画・検証用
if (qs.has('record') || EDIT) {
  window._ctrl = {
    start: () => { if (state === 'title') start(); },
    goto: (id, at) => { if (SC[id]) enter(id, at); },
    put: (x,y) => { player.x = x; player.y = y; },
    free: () => { endScene(); talkLock = false; },
    // そばの 相手に「はなす／しらべる／ひろう」を する（キー入力の 代わり・P1）
    act: () => { advance = true; },
    near: () => ({ spot: nearSpot ? nearSpot.id : null,
                   item: nearItem ? nearItem.item : null }),
    bugs: () => bugs.map(b => ({ kind:b.kind, gx:Math.round(b.gx), gy:Math.round(b.gy) })),
    fishing: () => fishing ? { phase:fishing.phase, t:+fishing.t.toFixed(2), result:fishing.result } : null,
    sleep: () => sleepNow(),
    mini: () => mini ? { name:mini.name, t:+mini.t.toFixed(1), out:mini.out, phase:mini.d.phase, d:mini.d } : null,
    view: () => view ? { name:view.name, t:+view.t.toFixed(1) } : null,
    viewOpen: (n, c) => openView(n, c),
    stamp: d => setFlag('stamp:' + d),
    miniEnd: n => { if (mini) { mini.result = n; mini.done = true; } },
    sel: () => sceneSel ? { i:sceneSel.i, n:sceneSel.n,
                            opts:sceneSel.st.opts.map(o => o.t), text:sceneSel.st.text } : null,
    pick: n => { if (sceneSel) { sceneSel.i = n; advance = true; } },
    scene: () => scene ? { i:scene.i, n:scene.q.length,
                           k:(scene.q[scene.i]||{}).k, say:sceneSay, veil:+veil.toFixed(2) } : null,
    poses: () => ({ me:playerPose,
                    cast:cast.map(c => ({ k:c.k, pose:c.pose, x:Math.round(c.x), y:Math.round(c.y) })),
                    taisoT0: +(elapsed - taisoT0).toFixed(1), lock:talkLock }),
    talk: () => talkNpc ? { idx:talkNpc.idx||0, n:linesOf(talkNpc).length,
                            line:linesOf(talkNpc)[talkNpc.idx||0] } : null,
    gdist: () => (SC[cur].npc||[]).map(n => n.who.map(
      w => +groundDist(player.x, player.y, w[1], w[2]).toFixed(2))),
    npcState: () => (SC[cur].npc||[]).map(n => ({ idx:n.idx||0, done:!!n.done,
                                                  n:(linesOf(n)||[]).length })),
    R: TALK_R,
    steps: () => ({ steps:WORLD.steps, dayT:+dayT().toFixed(2), mukae:WORLD.mukaeDone,
                    night:+nightT.toFixed(2) }),
    rgb: () => { const d = ctx.getImageData(300, 200, 60, 60).data;
                 let r=0,g=0,b=0; for (let i=0;i<d.length;i+=4){r+=d[i];g+=d[i+1];b+=d[i+2];}
                 const n = d.length/4;
                 return { r:Math.round(r/n), g:Math.round(g/n), b:Math.round(b/n) }; },
    setSteps: n => { WORLD.steps = n; },
    setMukae: v => { WORLD.mukaeDone = !!v; },
    setYoru: v => { WORLD.yoruDone = !!v; },
    // 日づけを 変えたら 会話の すすみ具合も 入れ直す（ねたときと おなじ）
    setDay: n => { newDay(n); resetDay(); },
    amb: () => ({ kind:ambKind(), last:lastAmb, n:ambCount,
                  dayT:+dayT().toFixed(2), on:!!AC }),
    foot: () => ({ want:SC[cur].ashi || 'tsuchi', last:lastFootKind, n:footCount }),
    uta: () => ({ now:utaNow, dist:(EVENTS.tooi || []).map(t => [t.id, screenDist(cur, t.place)]) }),
    utaNow: () => { utaTimer = 0; },
    dekake: () => ({ n:dekakeCount, done:WORLD.dekakeDone }),
    place: () => ({ cur, amb:SC[cur].amb, mizu:SC[cur].mizu || 0, lastPlace2,
                    sawa: (SAWA[SC[cur].amb] || SAWA.out).p,
                    water: mizuGain ? +mizuGain.gain.value.toFixed(3) : null }),
    lastDay: LAST_DAY,
    world: () => JSON.parse(JSON.stringify(WORLD)),
    queue: () => JSON.parse(JSON.stringify(WORLD.queue)),
    items: () => ({ mochi:Object.keys(WORLD.items), oki:JSON.parse(JSON.stringify(WORLD.placed)) }),
    spot: () => ({ near: nearSpot ? nearSpot.id : null,
                   all: (SC[cur].spot || []).map(s => [s.id,
                        +groundDist(player.x, player.y, s.x, s.y).toFixed(2)]),
                   mizu: mizuGain ? +mizuGain.gain.value.toFixed(4) : null }),
    num: () => ({ zutto:JSON.parse(JSON.stringify(WORLD.num)),
                  kyou:JSON.parse(JSON.stringify(WORLD.today)) }),
    wipe: () => wipeSave(),
    resume: () => { if (state === 'title') resume(); },
    dbg: () => ({ state, cur, day:WORLD.day, steps:WORLD.steps,
                  mukae:WORLD.mukaeDone, yoru:WORLD.yoruDone,
                  x:Math.round(player.x), y:Math.round(player.y),
                  h:Math.round(heightAt(player.y)), on:walkable(player.x, player.y),
                  lock:exitLock, talking:!!talkNpc, scene:!!scene }),
  };
}
