// 自動でわけたファイル。もとは photo/index.html 1枚だった
// ===== 歩ける範囲 =====
function inPoly(x, y, poly) {
  let c = false;
  for (let i=0, j=poly.length-1; i<poly.length; j=i++) {
    const xi=poly[i][0], yi=poly[i][1], xj=poly[j][0], yj=poly[j][1];
    if ((yi>y) !== (yj>y) && x < (xj-xi)*(y-yi)/(yj-yi) + xi) c = !c;
  }
  return c;
}
function walkable(x, y) {
  const sc = SC[cur];
  let ok = false;
  for (const p of sc.walk) if (inPoly(x, y, p)) { ok = true; break; }
  if (!ok) return false;
  for (const s of (sc.solid || [])) {
    const dx = (x-s[0])/s[2], dy = (y-s[1])/s[3];
    if (dx*dx + dy*dy < 1) return false;
  }
  return true;
}
function nearestFree(x, y) {
  if (walkable(x, y)) return { x, y };
  for (let r=8; r<=700; r+=8) {
    for (let i=0; i<24; i++) {
      const a = i/24*Math.PI*2;
      const nx = x + Math.cos(a)*r, ny = y + Math.sin(a)*r;
      if (walkable(nx, ny)) return { x:nx, y:ny };
    }
  }
  const k = Object.keys(SC[cur].start)[0];
  const p = SC[cur].start[k];
  return { x:p[0], y:p[1] };
}
function heightAt(y) {
  const sc = SC[cur];
  return lerp(sc.hFar, sc.hNear, clamp((y - sc.yTop) / (sc.yBot - sc.yTop), 0, 1));
}
// 画面のうえの距離ではなく、地面のうえの距離ではかる。
// 遠近があるので、奥へ10px 動くのと 横へ10px 動くのでは 実際に離れる量がぜんぜんちがう。
// 見かけの背の高さ h はカメラからの距離に反比例するので
//   奥ゆき z = FOCAL / h ／ 横 x = (画面のx - 中央) / h
// とすると、両方おなじ単位になる。単位は「そこに立ったときの背の高さ」ぶん。
const FOCAL = 900;   // 写真のおよその画角。奥ゆきと横の はかり方のつりあいを決める
const TALK_R = 1.6;  // はなしができる距離。単位は「背の高さ」ぶん
function ground(x, y) {
  const h = heightAt(y);
  return { x:(x - W/2)/h, z:(SC[cur].focal || FOCAL)/h };
}
function groundDist(x1, y1, x2, y2) {
  const a = ground(x1, y1), b = ground(x2, y2);
  return Math.hypot(a.x-b.x, a.z-b.z);
}
