// 自動でわけたファイル。もとは photo/index.html 1枚だった
// ===== 入力 =====
const keys = {};
const MOVE = { ArrowUp:[0,-1], KeyW:[0,-1], ArrowDown:[0,1], KeyS:[0,1],
               ArrowLeft:[-1,0], KeyA:[-1,0], ArrowRight:[1,0], KeyD:[1,0] };
window.addEventListener('keydown', e => {
  if (e.code in MOVE || e.code === 'Space' || e.code.startsWith('Shift')) e.preventDefault();
  keys[e.code] = true; initAudio();
  // 見るだけの 画面は Esc でも とじられる
  if (state === 'view' && e.code === 'Escape') { advance = true; return; }
  // えらんでいる あいだは 上下で うつる。おしっぱなしでは 進まない（e.repeat）
  if (sceneSel && !e.repeat) {
    const up = (e.code === 'ArrowUp' || e.code === 'KeyW');
    const dn = (e.code === 'ArrowDown' || e.code === 'KeyS');
    if (up || dn) { sceneSel.i = (sceneSel.i + (up ? sceneSel.n - 1 : 1)) % sceneSel.n; }
  }
  if (state === 'title' && e.code === 'KeyR') { wipeSave(); start(); }
  else if (state === 'title' && (e.code === 'Space' || e.code === 'Enter')) titlePress(null);
  else if (!e.repeat && (e.code === 'Space' || e.code === 'Enter')) advance = true;
  // 移動キーを 押したら HUD を ちらっと 出す（P8）
  if (state === 'play' && (e.code in MOVE)) hudPeek();
  // 数字キーで 道具を 持ちかえ（1=あみ 2=さお…）。HUD も 出す
  if (state === 'play' && !e.repeat && /^Digit[1-9]$/.test(e.code)) {
    const tools = toolsHeld(), n = +e.code.slice(5);
    if (n <= tools.length) { equip(tools[n - 1]); hudPeek(); }
  }
  // C で 虫かごの 中身を 見る（EDIT の コンソール出力とは かぶらないよう play のときだけ）
  if (state === 'play' && !EDIT && e.code === 'KeyC') openView('mushikago');
  if (EDIT && e.code === 'Backspace') { editPts.pop(); e.preventDefault(); }
  if (EDIT && e.code === 'KeyC') console.log(JSON.stringify(editPts));
});
window.addEventListener('keyup', e => { keys[e.code] = false; });
window.addEventListener('blur', () => { for (const k in keys) keys[k] = false; stick.on = false; });

// タイトルで おされたとき。セーブが あれば つづきから。
// 「はじめから」の もじの ところを おしたときだけ まっさら
const RESTART_BOX = { x0:320, x1:640, y0:460, y1:492 };
function titlePress(p) {
  if (!savedDay()) { start(); return; }
  if (p && p.x > RESTART_BOX.x0 && p.x < RESTART_BOX.x1
        && p.y > RESTART_BOX.y0 && p.y < RESTART_BOX.y1) { wipeSave(); start(); }
  else resume();
}

let usingTouch = false;
let advance = false;   // セリフを つぎへ送る（スペース／タップ）。おしっぱなしでは 進まない
const stick = { on:false, id:null, ox:0, oy:0, x:0, y:0, moved:false, t0:0 };
function toCanvas(t) {
  const r = canvas.getBoundingClientRect();
  return { x:(t.clientX-r.left)/r.width*W, y:(t.clientY-r.top)/r.height*H };
}
canvas.addEventListener('touchstart', e => {
  e.preventDefault(); usingTouch = true; initAudio();
  const t = e.changedTouches[0], p = toCanvas(t);
  if (state === 'title') { titlePress(p); return; }
  // えらんでいる あいだは、その行を タップ → もう一度 タップで きめる
  if (sceneSel && selRect) {
    const r = Math.floor((p.y - selRect.top + 22) / 34);
    if (r >= 0 && r < sceneSel.n) {
      if (sceneSel.i === r) advance = true; else sceneSel.i = r;
    }
    return;
  }
  if (!stick.on) { stick.on=true; stick.id=t.identifier; stick.ox=p.x; stick.oy=p.y;
                   stick.x=p.x; stick.y=p.y; stick.moved=false; stick.t0=performance.now(); }
}, { passive:false });
canvas.addEventListener('touchmove', e => {
  e.preventDefault();
  for (const t of e.changedTouches) {
    if (t.identifier !== stick.id) continue;
    const p = toCanvas(t); stick.x = p.x; stick.y = p.y;
    if (Math.hypot(p.x-stick.ox, p.y-stick.oy) > 14) stick.moved = true;
  }
}, { passive:false });
function endTouch(e) {
  e.preventDefault();
  for (const t of e.changedTouches) if (t.identifier === stick.id) {
    // 指を ちょっと つけただけ＝タップ なら セリフを つぎへ
    if (!stick.moved && performance.now()-stick.t0 < 400) advance = true;
    stick.on=false; stick.id=null;
  }
}
canvas.addEventListener('touchend', endTouch, { passive:false });
canvas.addEventListener('touchcancel', endTouch, { passive:false });

const editPts = [];
let mouse = { x:0, y:0 };
canvas.addEventListener('mousemove', e => { mouse = toCanvas(e); });
canvas.addEventListener('mousedown', e => {
  if (usingTouch) return;
  initAudio();
  if (state === 'title') { titlePress(toCanvas(e)); return; }
  if (EDIT) { const p = toCanvas(e); editPts.push([Math.round(p.x), Math.round(p.y)]); }
  else advance = true;
});
