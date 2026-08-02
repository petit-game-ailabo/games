// 自動でわけたファイル。もとは photo/index.html 1枚だった
// ===== 土台。画面の大きさ・キャラの名まえ・こまかい計算 =====
'use strict';
const W = 960, H = 540;
const canvas = document.getElementById('c');
const ctx = canvas.getContext('2d');
ctx.imageSmoothingEnabled = false;

const qs = new URLSearchParams(location.search);
const EDIT = qs.has('edit');           // ?edit=1 で 歩ける範囲を手直しするための下書きモード

function resize() {
  const raw = Math.min(window.innerWidth / W, window.innerHeight / H);
  canvas.style.width = (W * raw) + 'px';
  canvas.style.height = (H * raw) + 'px';
}
window.addEventListener('resize', resize); resize();

const clamp = (v,a,b) => v<a?a:(v>b?b:v);
const lerp  = (a,b,t) => a + (b-a)*t;
const dist  = (x1,y1,x2,y2) => Math.hypot(x1-x2, y1-y2);

// ===== データ =====
// 中身は data/*.json にある。ツクールが 書きかえるのは そっち。
// fetch で よむので、**ファイルを 直接ひらく（file://）と うごかない。**
// かならず サーバごしに ひらくこと：  python -m http.server 8000
let dataErr = '';
function loadData(url, use, then) {
  fetch(url).then(r => r.json()).then(use)
    .catch(() => { dataErr = url + ' が よめない。サーバごしに ひらくこと'; })
    .then(then);
}

// 画面の表（data/screens.json）。歩ける範囲・出入口・だれが居るか・奥ゆき
let SC = {};
function setScreens(o) {
  SC = o;
  // npc.talks は かぎの文字れつ。ここで じっさいの セリフに つなぎかえる
  for (const k in SC) for (const n of (SC[k].npc || []))
    if (typeof n.talks === 'string') n.days = TALKS[n.talks] || {};
}

// キャラの表（data/cast.json）。名まえ・絵のこま番号・浮くか・影のこさ
let CAST = {};
const castOf = k => CAST[k] || {};
const ciOf   = k => CAST[k] ? CAST[k].ci : 0;
const nameOf = k => CAST[k] ? CAST[k].name : '';

const imgChars = new Image();
const KANSUJI = ['','一','二','三','四','五','六','七','八','九','十'];
function hiduke(n) {
  if (n <= 10) return KANSUJI[n];
  if (n < 20) return '十' + KANSUJI[n-10];
  if (n === 20) return '二十';
  if (n < 30) return '二十' + KANSUJI[n-20];
  return n === 30 ? '三十' : '三十一';
}

