// 自動でわけたファイル。もとは photo/index.html 1枚だった
// ===== 場面（朝のながれ・むかえ・よる）=====
// ===== 朝のながれ =====
// k:'card' 黒い画面に日づけ／'to' 画面をかえる／'put' 立ち位置／'cast' その場面のキャラ
// 'say' セリフ（じどうで すすむ）／'walk' じどうで あるく／'taiso' ラジオたいそう／'free' 自由行動へ
function morningScript(d) {
  const head = [
    { k:'card', text:'八月　' + hiduke(d) + '日', s:2.4 },
    { k:'to', sc:'zashiki', at:'nedoko' },
    { k:'cast', list:[['dai', 220, 430]] },
    { k:'wait', s:1.0 },
  ];
  const wake = d === 1 ? [
    { k:'say', who:'dai',   text:'チルノちゃん。あさだよ' },
    { k:'say', who:'cirno', text:'……あと ごふん' },
    { k:'say', who:'dai',   text:'ラジオたいそう はじまっちゃう' },
    { k:'say', who:'cirno', text:'……いく' },
  ] : d === 2 ? [
    { k:'say', who:'dai',   text:'きょうは じぶんで おきたね' },
    { k:'say', who:'cirno', text:'あたいは やれば できるの' },
  ] : [
    { k:'say', who:'dai',   text:'おはよう' },
    { k:'say', who:'cirno', text:'おはよ' },
  ];
  const niwa = [
    // 大妖精が さきに 部屋を出て、チルノが ついていく
    { k:'move', list:[['dai', 470, 536, 1]], s:2.6 },
    { k:'walk', x:470, y:524 },
    { k:'to', sc:'iemae', at:'ie' },
    { k:'cast', list:[['dai', 330, 480], ['marisa', 620, 468], ['wriggle', 770, 486]] },
    { k:'put', x:470, y:500 },
    { k:'wait', s:0.8 },
  ];
  const before = d === 1
    ? [{ k:'say', who:'marisa', text:'おそいぜ' }]
    : [{ k:'say', who:'wriggle', text:'きょうは かぜが ぬるいな' }];
  const taiso = [
    { k:'taiso' },
    ...(d === 1 ? [
      { k:'say', who:'cirno', text:'……ねむい' },
      { k:'say', who:'dai',   text:'でも きもち よかったね' },
    ] : []),
  ];
  const gohan = [
    // たいそうが おわって、みんなが 家のほうへ もどっていく
    { k:'say', who:'keine', text:'ごはんに するよ' },
    { k:'move', list:[['dai', 452, 448, 1], ['marisa', 506, 446, 1], ['wriggle', 560, 446, 1]], s:3.2 },
    { k:'walk', x:498, y:450 },
    { k:'wait', s:0.5 },
    { k:'to', sc:'doma', at:'asa' },
    { k:'cast', list:[['keine', 600, 428], ['dai', 890, 470], ['marisa', 700, 532]] },
    { k:'put', x:450, y:500 },
    { k:'wait', s:0.9 },
  ];
  const table = d === 1 ? [
    { k:'say', who:'keine', text:'おはよう。ごはんが できているよ' },
    { k:'say', who:'cirno', text:'いただきます' },
    { k:'say', who:'keine', text:'きょうは どこへ いくんだい' },
    { k:'say', who:'marisa',text:'きめてねえ' },
    { k:'say', who:'keine', text:'きめないで あるくのが いちばん いいよ' },
    { k:'say', who:'dai',   text:'せんせいが いうと せっとくりょくが あるね' },
    { k:'say', who:'keine', text:'せんせいじゃ ない'},
  ] : d === 2 ? [
    { k:'say', who:'keine', text:'きのう、もりの ほうへ いっただろう' },
    { k:'say', who:'cirno', text:'なんで わかるの' },
    { k:'say', who:'keine', text:'くつに すぎの はが ついてる' },
    { k:'say', who:'marisa',text:'めざといんだよ この ひとは' },
  ] : [
    { k:'say', who:'keine', text:'いってらっしゃい' },
  ];
  // ごはんが おわって、みんなが 出ていく。けーねだけ いろりの手前に のこる。
  // ぜんぶ 動きおわってから やっと チルノが うごけるようにする
  const dekake = [
    { k:'say', who:'cirno', text:'ごちそうさま' },
    { k:'move', list:[['marisa', 190, 512, 1], ['dai', 176, 470, 1], ['keine', 560, 496]], s:3.4 },
    { k:'walk', x:392, y:526 },
    { k:'wait', s:0.9 },
  ];
  return [...head, ...wake, ...niwa, ...before, ...taiso,
          ...gohan, ...table, ...dekake, { k:'free' }];
}
// 日がくれると けーねが むかえに来る。いま居る場所に 歩いて入ってきて、
// いっしょに ざしきへ かえる。どこに居ても なりたつように その場で組み立てる
function mukaeScript() {
  const sc = SC[cur], home = (cur === 'zashiki');
  const from = nearestFree(clamp(player.x - 175, 30, W-30),
                           Math.min(sc.yBot - 6, player.y + 42));
  const to   = nearestFree(clamp(player.x - 80, 30, W-30), player.y + 6);
  const q = [
    { k:'cast', list:[['keine', from.x, from.y]] },
    { k:'move', list:[['keine', to.x, to.y]], s:3.4 },
    { k:'say', who:'keine', text: home ? 'ここに いたのか' : 'こんなところに いたのか' },
    { k:'say', who:'keine', text:'もう くらいよ。かえろう' },
    { k:'say', who:'cirno', text:'……もう ちょっとだけ' },
    { k:'say', who:'keine', text:'あしたも あるだろう' },
    { k:'say', who:'cirno', text:'……はぁい' },
  ];
  if (!home) {
    q.push({ k:'move', list:[['keine', from.x, from.y, 1]], s:3.4 });
    q.push({ k:'walk', x:from.x, y:from.y });
  }
  q.push({ k:'to', sc:'zashiki', at:'mae' });
  q.push({ k:'cast', list:[['keine', 420, 496]] });
  q.push({ k:'put', x:290, y:492 });
  q.push({ k:'wait', s:0.7 });
  q.push({ k:'say', who:'keine', text:'ふとん、しいてあるよ' });
  q.push({ k:'say', who:'cirno', text:'……うん' });
  q.push({ k:'say', who:'keine', text:'おやすみ' });
  q.push({ k:'move', list:[['keine', 566, 538, 1]], s:3.2 });
  q.push({ k:'wait', s:0.6 });
  q.push({ k:'free' });
  return q;
}
const NIGHT = [
  { k:'say', who:'cirno', text:'……ねむい。ねよ' },
  { k:'card', text:'よる', s:2.0 },
];
