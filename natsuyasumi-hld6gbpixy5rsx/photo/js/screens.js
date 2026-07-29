// 自動でわけたファイル。もとは photo/index.html 1枚だった
// ===== 画面 =====
// walk  … 歩ける範囲。写真の中の道や床をなぞった多角形（複数可）
// solid … その中でも入れないところ（いろり・あんどん など）。楕円 [x,y,rx,ry]
// yTop/yBot と hFar/hNear … 奥行き。足元のy座標で背の高さを決める
// npc   … その場に居るキャラと、日ごとの会話
const SC = {
  zashiki: {
    name:'ざしき', src:'bg/zashiki.jpg', amb:'in', haze:'#efe6d2',
    walk: [[[0,372],[130,352],[300,338],[470,330],[620,318],[780,326],[900,344],[960,360],[960,541],[0,541]]],
    yTop:318, yBot:540, hFar:54, hNear:132,
    start: { mae:[430,500], doma:[870,420], nedoko:[300,466] },
    exits: [
      { x0:0, x1:960, y0:530, y1:545, to:'rouka', at:'temae' },
      { x0:930, x1:960, y0:380, y1:541, to:'doma', at:'zashiki' },
    ],
    // ふとんの あるところ。ここで じっとしていると 1日が おわる。
    // 写真のうえに ふとんを かいて、どこで ねられるか 見てわかるようにする
    nedoko: { x:180, y:456, r:66,
              quad:[[124,418],[244,413],[276,504],[82,510]] },
    npc: [{
      who: [['dai', 300, 470]],
      days: TALKS.zashiki,
    }],
  },
  doma: {
    name:'どま', src:'bg/doma.jpg', amb:'in', haze:'#efe4cc',
    walk: [[[100,392],[560,368],[960,382],[960,541],[100,541]]],
    solid: [[735,468,148,48], [335,418,54,20]],   // いろり／あんどん
    yTop:366, yBot:540, hFar:60, hNear:130,
    start: { zashiki:[170,470], asa:[450,505] },
    exits: [ { x0:0, x1:135, y0:380, y1:541, to:'zashiki', at:'doma' } ],
    npc: [{
      // いろりの手前がわ。奥に置くと まわりが いろり（solid）で、近づける場所が なくなる
      who: [['keine', 560, 496]],
      days: TALKS.doma,
    }],
  },
  rouka: {
    name:'ろうか', src:'bg/rouka.jpg', amb:'in', haze:'#e6dfcd',
    walk: [[[838,180],[870,180],[900,240],[928,300],[948,380],[958,460],[958,541],[452,541],[566,460],[664,380],[742,300],[800,240]]],
    yTop:180, yBot:540, hFar:30, hNear:140,
    start: { oku:[850,232], temae:[720,520] },
    exits: [
      { x0:820, x1:900, y0:174, y1:214, to:'iemae', at:'ie' },
      { x0:440, x1:960, y0:530, y1:545, to:'zashiki', at:'mae' },
    ],
    npc: [{
      who: [['rumia', 790, 330]],
      days: TALKS.rouka,
    }],
  },
  iemae: {
    name:'いえのまえ', src:'bg/iemae.jpg', amb:'out', haze:'#e2edf4',
    walk: [[[10,394],[170,380],[300,402],[470,444],[640,430],[790,384],[950,388],[950,541],[10,541]]],
    yTop:380, yBot:540, hFar:62, hNear:126,
    start: { ie:[500,478], michi:[470,514] },
    exits: [
      { x0:390, x1:600, y0:432, y1:456, to:'rouka', at:'oku' },
      { x0:0, x1:960, y0:530, y1:545, to:'aze', at:'oku' },
    ],
    npc: [{
      who: [['marisa', 250, 480]],
      days: TALKS.iemae,
    }],
  },
  aze: {
    name:'あぜみち', src:'bg/azemichi.jpg', amb:'ki', haze:'#eaf1e2',
    walk: [[[452,192],[494,192],[558,250],[640,322],[706,402],[756,472],[800,541],
            [236,541],[274,472],[316,402],[360,322],[408,250]]],
    yTop:192, yBot:540, hFar:34, hNear:132,
    start: { oku:[473,244], temae:[500,505] },
    exits: [
      { x0:400, x1:560, y0:180, y1:228, to:'mori', at:'temae' },
      { x0:0, x1:960, y0:530, y1:545, to:'iemae', at:'ie' },
    ],
    npc: [{
      who: [['wriggle', 396, 424]],
      days: TALKS.aze,
    }],
  },
  mori: {
    name:'もりのみち', src:'bg/mori.jpg', amb:'ki', haze:'#dfe8d6',
    walk: [[[340,268],[500,268],[534,312],[566,368],[592,430],[612,541],
            [232,541],[252,430],[280,368],[304,312]]],
    yTop:268, yBot:540, hFar:46, hNear:150,
    start: { temae:[420,505] },
    exits: [ { x0:0, x1:960, y0:530, y1:545, to:'aze', at:'oku' } ],
    npc: [{
      who: [['reimu', 372, 402], ['mystia', 486, 386]],
      days: TALKS.mori,
    }],
  },
};
