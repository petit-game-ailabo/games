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

const CI = { cirno:2, dai:3, rumia:4, wriggle:5, mystia:6, keine:7, reimu:0, marisa:1 };
const NAME = { cirno:'チルノ', dai:'だいようせい', rumia:'ルーミア', wriggle:'リグル',
               mystia:'ミスティア', keine:'けーね', reimu:'れいむ', marisa:'まりさ' };
const imgChars = new Image();
const KANSUJI = ['','一','二','三','四','五','六','七','八','九','十'];
function hiduke(n) {
  if (n <= 10) return KANSUJI[n];
  if (n < 20) return '十' + KANSUJI[n-10];
  if (n === 20) return '二十';
  if (n < 30) return '二十' + KANSUJI[n-20];
  return n === 30 ? '三十' : '三十一';
}

