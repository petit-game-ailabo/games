# 幻想郷のなつやすみ — 素材クレジット / ライセンス

『ぼくのなつやすみ』（2000年 / ミレニアムキッチン企画開発・SCE発売）へのオマージュとして作った、
東方Project二次創作の無料ブラウザミニゲームです。

> **⚠️ `photo/` と `td/` の配布は「無料・非商用」限定。** 下記キャラスプライト（Majstek）が**非商用ライセンス**のため、
> このままでは販売できない。**商用版を出す前に、キャラ絵を必ず差し替えること**（→ `REPLACE.md` R-001）。
> 世界タイル(ansimuz・CC0相当)・魚(CraftPix・OGA-BY)・虫(cutebugs・CC0)・音(自作/手続き生成)は商用OK。
>
> **✅ `unity/`（奥行き版）は 2026-08-13 に 本人が 用意した キャラ絵へ 差し替え済み**＝この版に Majstek は 入っていない。

## 建物のテクスチャ（`unity/` 奥行き版）― 2026-08-16 追加

**ambientCG**（https://ambientcg.com）の素材。**すべて CC0**
（パブリックドメイン相当・商用可・クレジット不要・改変可・**再配布可**）。
アカウント不要で誰でも同じものを取得できる。

| もと素材 | 使い道 | 出力 |
|---|---|---|
| `RoofingTiles001` | 母屋の瓦 | `unity/Assets/Art/Textures/roof_tile.png` |
| `WoodSiding009` | 柱・板壁 | `unity/Assets/Art/Textures/wood_beam.png` |
| `Rock030` | 石（沓ぬぎ石・井戸・待避所） | `unity/Assets/Art/Textures/stone.png` |
| `ThatchedRoof001A` | 納屋のわらぶき屋根 | `unity/Assets/Art/Textures/thatch.png` |

こちらでの手あて（`unity/ArtSource/make_house_tex.py`）：写真のままだとドット絵のキャラや
草木と質感が食いちがうので、**256pxに落として14色前後にまとめ、木立ちの20色へ4割ほど寄せた**。
※**選びかたが9割。**小さくしても形が残るものしか使えない。平たいスレート屋根や
のっぺりした漆喰・砂は256pxにするとただのざらざらになって、何の材質か読めなくなった。
漆喰と道の土は**手で描いたものを残している**（草の絵と色がそろっているため）。

## 主人公（`unity/` Niwa）― 2026-08-30 差し替え

**本人が 用意した 魔理沙の 立ち絵と 歩き差分**
- もと: `unity/ArtSource/marisa_tachie_source.png`（立ち絵）／`marisa_walk_source.png`（歩き 8コマ x 7方向）
- こちらでの 手あて（`unity/ArtSource/make_marisa_walk.py`）：市松の 地を ふちから 塗りつぶして 抜き
  （エプロンの 白は 囲まれて いるので 残る）、足もとの まん中で そろえて 1コマ 128x160 に 詰め、
  **8方向(列) x 8コマ(行)** の アトラス `Assets/Art/Sprites/marisa_walk.png` に 組みなおした。
  もらった 絵は 7方向 だったので、「右」は 「左」を 鏡に して 埋めて いる
- 立ち絵（目あき／目とじ）は 透過して `Assets/Art/Sprites/tachie/marisa_tachie.png` と
  `marisa_tachie_me.png`（**同じ 枠で 切って あるので 重ねると まばたきに なる**）
- ★2026-08-30 夜：**8方向 ぜんぶ そろった**（1方向＝4列x2行の シート 1枚 x 8枚）。
  わりつけ＝**8列(向き) x 10行**（行0..7:走り / 行8:立ち / 行9:目とじ）。
  列 = 0:正面 1:左ななめ前 2:左 3:左ななめ奥 4:奥 5:右ななめ奥 6:右 7:右ななめ前。
  止まると **その 向きの まま** 静止（正面だけ 本物の 立ち絵＋まばたき、
  ほかの 向きは 足の そろった コマを 機械で えらぶ）。
  ★背たけは 方向ごとに ばらつく ので **いちばん 高い 方向に そろえて** 縮尺を 統一
- ※2026-08-30 夕2（旧）：**正面の 走り8コマ**（4列x2行の 1枚）に 差し替え（6コマ版は 動きが いまいち）。
  ならび＝**列0..6:走りの 8コマ（行）／列7:立ち(行0)・目とじ(行1)**。
  ★立ち絵と 走りシートは **座標系が ちがう**（1024x1536 と 214x491）ので、
  出どころ ごとに 枠を とって **背たけを そろえて から** コマに 詰める
  （同じ 枠で 切ると 走りだけ 極端に 小さく なった）
- ※2026-08-30 夕1（旧）：正面の 走り6コマ。
  ならび＝行0..5:走り / 6:立ち / 7:目とじ。1コマ **256x320**（前は 160x200 で つぶれて いた）。
  **キャラ絵は 点フィルタの 対象外**に した（ドット絵では ないので、縮小の たびに
  ぎざぎざに なって いた＝本人「解像度が悪くて荒い」の 正体）。
  向きは まだ 正面だけ（左右の 列は 鏡）。向きごとの 絵が そろったら 8方向に もどす

## 主人公（`unity/` 奥行き版）― 2026-08-15 差し替え

**本人が用意した 魔理沙の 8方向 x 8状態**（元画像 `unity/ArtSource/marisa_source_8x8.png`）。

- 方向：正面／左ななめ前／左／左ななめ奥／奥／右ななめ奥／右／右ななめ前
- 状態：立ち／歩き／走り／喜／怒／哀／楽／目をとじた
- こちらでの 手あて（`unity/ArtSource/make_marisa.py`）：
  もらった 画像は **RGB のままで 透過して いない**（「透明に 見える 市松もよう」が 絵として
  塗ってある）。魔理沙の エプロンと 帽子の リボンは **本物の 白**なので、白を 消すと 体に 穴が あく。
  → 画の ふちから **塗りつぶしで たどれる ところ だけ** を 抜いた。
  升目も そろって いなかった ので、中みの ある 帯から 割りだし、
  **足もとの まん中**で そろえて 1コマ 115x167 に 詰めなおした
  （絵ぜんたいの まん中で そろえると、帽子の つばの 出っぱりで 向きを 変える たびに 体が 横へ とぶ）。
- 出力：`unity/Assets/Art/Sprites/marisa_8x8.png`

## そのほかのキャラクター（`unity/` 奥行き版）― 2026-08-13 差し替え

**本人が用意した 30体ぶんの ドット絵**（元画像 `unity/ArtSource/chars_source_10x3.png`）。
黒地だった 元画像から 背景を ぬいて 透過に し、1コマ 48x64・10列 x 3行の アトラス
`unity/Assets/Art/Sprites/chars_tall.png` に 組み直している（足もとを コマの 下端に そろえて 接地させる）。
**Majstek の 素材は 使っていない。**

## 草木（`unity/` 奥行き版）― 2026-08-14 自作をやめて 差し替え

**Trees & Bushes** — 作者: **Luis Zuno (@ansimuz)**
- 配布元: https://opengameart.org/content/trees-bushes
- ライセンス: **CC0 1.0**（商用可・改変可・**再配布可**・クレジット任意）。ライセンス文 `unity/Assets/Art/Sprites/LICENSE-trees-and-bushes-ansimuz.txt` 同梱
- 配布物の PSD から **vegetation レイヤーだけ**を 抜いて（草地と 影は 使わない）8体に 切りわけ、
  **32px＝1m** の 尺で 144px の コマに 詰めなおして `unity/Assets/Art/Sprites/nature.png`（576x288・4列2行）に した。
  影は Unity に 落とさせるので、**元の 絵に 焼かれていた 影は 捨てている**
- ※ `td/` の 世界タイルと 同じ 作者なので 絵の 肌ざわりが そろう

## 家の テクスチャ・虫の 絵・画面の 枠（`unity/` 奥行き版）― 2026-08-14 自作

**すべて こちらで 描き起こした**（外部素材なし）。生成する 手順ごと 残してある：
- `unity/ArtSource/make_textures.py` … 畳・板・柱・土壁・障子紙・かわら・草地
- `unity/ArtSource/make_bugs.py` … 虫 8しゅるい（1コマ 16x16）
- `unity/ArtSource/make_ui.py` … 画面の 枠（9スライス）・あみの 記号

**色は 木立ち(ansimuz・CC0)の 絵から 実際に 使われている 20色を 吸いだして、その 系統だけで 描いた。**
前は 写真から 起こした テクスチャを 貼っていて、ドット絵の 草木と ならぶと 材質が ちぐはぐ だった。
※ 直しかたは「3Dを やめる」ことでは ない。本家も 建物は 3D で、2Dなのは キャラと 小物だけ。
　直すべきは **貼る 絵を そろえる** ことだった。

## 書体（`unity/` 奥行き版）― 2026-08-14

**PixelMplus12 Regular** — 作者: **itouhiro**（M+ BITMAP FONTS を もとに した ドット書体）
- 配布元: https://itouhiro.hatenablog.com/entry/20130602/font
- ライセンス: **M+ FONT LICENSE**。原文どおり
  "Unlimited permission is granted to use, copy, and distribute them, with or without modification,
  either commercially or noncommercially."
  ＝**商用可・改変可・再配布可・ゲームへの 同梱可**。ライセンス文 `unity/Assets/Art/Fonts/LICENSE-PixelMplus-*.txt` 同梱
- JIS第1・第2水準の 漢字を ふくむ。点で 描かれた 書体なので 12px の 整数倍で 出せば ドット絵と 目が そろう

## 遠景の 描き割り（`unity/` Niwa の 山・空・雲）― 2026-08-30

**山（重なる 稜線）／空（入道雲）** — **本人が 生成した 絵**
- もと: `unity/ArtSource/ref/photos/ChatGPT Image 2026年8月30日 14_03_38.png`（山・2048x768）
  `ChatGPT Image 2026年8月30日 14_07_15.png`（空・2048x768）
- こちらでの 手あて: 山＝上の 暗い ぼかしを 明るさから アルファに して 抜いた（`satoyama.png`）／
  空＝そのまま 幕に（`sora.png`）＋青空を 抜いて 雲を 3つ 切りだし（`kumo_a/b/c.png`）、
  1つずつ ちがう 速さで 横に ながす
- ※ 前に つかって いた CC0/PDM の 写真（Flickr・WordPress Photo Directory）は
  `ref/photos/` に 残して あるが **いまは 未使用**

## 庭シーンの 3Dモデル（`unity/` Niwa）― 2026-08-29

**Nature Kit (2.1)** — 作者: **Kenney (kenney.nl)**
- 配布元: https://kenney.nl/assets/nature-kit
- ライセンス: **CC0 1.0**（商用可・改変可・再配布可・クレジット任意）
- つかいどころ: 庭シーンの 木・草・花・塀・門・飛び石・岩・鉢（`unity/Assets/Art/Models/kenney/`）

## つぶ（雨・もや）（`unity/` 奥行き版）― 2026-08-14

**Particle Pack** — 作者: **Kenney (kenney.nl)**
- 配布元: https://kenney.nl/assets/particle-pack
- ライセンス: **CC0 1.0**（商用可・改変可・再配布可・クレジット任意）
- `circle_05`（雨つぶ）／`smoke_09`（もや）ほかを `unity/Assets/Art/Particles/` に そのまま 置いている

## UI（`unity/` 奥行き版）― 2026-08-14 素材だけ 用意

**UI Pack - Pixel Adventure** — 作者: **Kenney (kenney.nl)**
- 配布元: https://kenney.nl/assets/ui-pack-pixel-adventure
- ライセンス: **CC0 1.0**（商用可・改変可・再配布可・クレジット任意）。ライセンス文 `unity/Assets/Art/UI/LICENSE-kenney-ui-pack.txt` 同梱
- タイルシートを `unity/Assets/Art/UI/` に 置いた。
- ※ **画面の 枠は これを 使っていない。** Kenney の UI は 西洋の 木わくで、いまの
  木立ち・家の 色みと そろわなかった ため、枠だけ `make_ui.py` で 描き起こした。
  Kenney の ぶんは 丸・矢印などの 記号として 残して ある

## キャラクタースプライト（`photo/` ・ `td/` 版）

**Touhou 16x16 Mini Pack** — 作者: **Majstek** (@majstek3)
- 配布元: https://reale-ly.itch.io/touhou-mini-pack
- 条件: 非商用プロジェクトでの使用可 / 改変可 / **クレジット表記「Majstek」必須** / R-18用途は不可
- 本作では 30 体分の 16x16 スプライトを 1 枚のアトラスに再構成して埋め込んでいます

## タイルセット（`td/` 2D版：地面・木・やぶ・水・木造物）

**Top Down Adventure Assets（"A Meta Data Game"）** — 作者: **Luis Zuno (@ansimuz)**
- 配布元: https://ansimuz.itch.io/ ／ http://www.pixelgameart.org
- ライセンス: **CC0 相当**（本人明記：個人/商用OK・改変OK・**再配布OK**）。ライセンス文 `td/assets/LICENSE-topdown-adventure.txt` 同梱
- `td/assets/tileset-world.png` として そのまま使用。より綺麗な素材への 差し替えは 後で判断
- ※ 旧記載の「Mana Seed（Seliel／再配布不可）」は 実体と異なるため 訂正した

## 生き物スプライト（`td/` 2D版：魚・虫）

**Fishing Game Assets Pixel Art** — 作者: **CraftPix.net**
- 配布元: https://opengameart.org/content/fishing-game-assets-pixel-art
- ライセンス: **OGA-BY 3.0**（商用可・再配布可・**帰属必須**）
- クレジット表記: **「CraftPix.net 2D Game Assets」**
- 本作では Catch（釣果）の魚フレームだけを抽出し、川魚を選んで `td/assets/fish.png` に再構成

**Ambient Pixel Art Insects（cutebugs）** — 作者: **MadameBerry**
- 配布元: https://opengameart.org/content/ambient-pixel-art-insects
- ライセンス: **CC0 1.0**（パブリックドメイン・商用可・クレジット任意・再配布可）
- カブト/トンボ/ホタル/ハチ/ガ を抽出し `td/assets/bugs.png` に再構成

## 背景（`photo/` 版）― 2026-08-10 実写 → HD-2D に差し替え

**下表の Wikimedia Commons の CC BY 実写を素材に、2D（HD-2D調イラスト）へ AI 変換したもの**を背景に使用（本人が変換）。
元が **CC BY**（継承義務のない CC BY のみ・CC BY-SA は不採用）なので、**改変・商用利用は許諾範囲内**。ただし CC BY は
**(1) 原著作者=撮影者名の表示、(2) 改変した旨の明示** が条件。→ タイトル画面に
「背景: Wikimedia Commons CC BY（撮影者名）を 2D化・改変」と明記し、下表で元画像も辿れるようにしている。
960x540 に整形（16:9 センタークロップ→リサイズ）。HD 原本は `photo/bg/_hd_src/*.png`。

> ⚠️ 商用化チェック（オーナー判断）：CC BY の 2次的著作物としての体裁は満たしているが、**AI 変換で元写真の
> 「創作的表現」がどれだけ残っているか**で、そもそも元著作物の二次的著作物に当たるか／独自の別著作物かが変わる。
> 販売前に、各画像が元写真と十分に別物か（=CC BY 表示だけで足りるか、むしろ表示不要な別物か）を要確認。安全側で表示は残す。

| ファイル | 画面 | 元にした実写（元画像） | ライセンス | 撮影者 |
|---|---|---|---|---|
| `photo/bg/azemichi.jpg` | あぜみち | [Path(あぜ道) - panoramio.jpg](https://commons.wikimedia.org/wiki/File:Path(%E3%81%82%E3%81%9C%E9%81%93)_-_panoramio.jpg) | **CC BY 3.0** | **Fumihiko Ueno** |
| `photo/bg/mori.jpg` | もりのみち | [Japan, Tochigi - Nikko Takinoo shrine 2014 1.jpg](https://commons.wikimedia.org/wiki/File:Japan,_Tochigi_-_Nikko_Takinoo_shrine_2014_1.jpg) | **CC BY 2.0** | **Guilhem Vellut** |
| `photo/bg/iemae.jpg` | いえのまえ | [200michinoku folk village3872.jpg](https://commons.wikimedia.org/wiki/File:200michinoku_folk_village3872.jpg) | **CC BY 2.5** | **663highland** |
| `photo/bg/zashiki.jpg` | ざしき | [Youkoukan06n4592.jpg](https://commons.wikimedia.org/wiki/File:Youkoukan06n4592.jpg) | **CC BY 2.5** | **663highland** |
| `photo/bg/rouka.jpg` | ろうか | [141122 Kozanji Shimonoseki Yamaguchi pref Japan12n.jpg](https://commons.wikimedia.org/wiki/File:141122_Kozanji_Shimonoseki_Yamaguchi_pref_Japan12n.jpg) | **CC BY 2.5** | **663highland** |
| `photo/bg/doma.jpg` | どま | [Doshin-kaoku07s3200.jpg](https://commons.wikimedia.org/wiki/File:Doshin-kaoku07s3200.jpg) | **CC BY 2.5** | **663highland** |

すべて元素材は **CC BY**（表示必須）。撮影者名はタイトル画面にも出している。改変（2D化）した旨も明示。
※ 旧 `michi.jpg`（Tōzaki Shrine / CC0 / 先従隗始）は「人の気配が強すぎる」ため差し替え・削除。
※ 2026-08-10：実写 6 枚を **HD-2D にAI変換して差し替え**（人間プレイヤーの試遊で「HD-2D の方がよい」との結論）。
　実写原本の JPG は git 履歴に残存。HD-2D の元(16:9クロップ済 PNG)は `photo/bg/_hd_src/`。

CC BY のものは**撮影者名の表示＋改変明示が必須**なので、背景を差し替えたときはこの表とタイトル画面を必ず更新すること。

## 原作

**東方Project** (C) 上海アリス幻樂団 / ZUN
- 二次創作ガイドラインに従い、**無料**のブラウザゲームとして公開しています
- https://touhou-project.news/guideline/

## その他

- 虫（セミ・カブトムシ・オニヤンマ等 10 種）と魚、鳥居、網、UI はすべて Canvas 描画による自作
- BGM・セミの声・効果音は Web Audio API による自作（外部音源なし）

## 商用利用について

上記スプライト素材は非商用条件のため、**このゲームを有償配布・商用利用することはできません。**
ZUN 氏のガイドライン上も、二次創作ブラウザゲームは無料公開のみが対象です。

## AI生成の 画像（本人・Codex）

`unity/Assets/Art/Textures/shashin/` 以下（地面・木の 葉と 幹・家の 壁と 瓦と 木）と 虫の 絵の もとは、
本人が Codex(OpenAI) で 生成した もの（2026-09-02 記録）。外部の 写真は ふくまない。

## 主人公の 3Dモデル（Meshy・2026-09-05〜）

`unity/Assets/Art/Sprites/marisa_meshy.png`（8方向x10状態の スプライトシート）は、
**Meshy AI の image-to-3D で 作った 3Dモデルを こちらで 8方向に 焼いた もの**。

- ツール：Meshy（https://www.meshy.ai/）／**無料プラン**
- **無料プランの 生成物は CC BY 4.0** ＝ 商用利用は 可だが **Meshy の 表示が 必要**
  https://help.meshy.ai/en/articles/9992001-can-i-use-my-generated-assets-for-commercial-projects
- **表示（この まま 配布物にも 載せる）：**
  > 3D model generated with Meshy (https://www.meshy.ai/) — CC BY 4.0

- 焼いた もの（スプライト）は 派生物 なので **同じ 表示義務が かかる**。
- もとの GLB は `unity/ArtSource/ref/meshy/`（gitignore・リポジトリに 入れない）。
- ★表示を したくない場合は Meshy の 有料プラン（生成物が 自分の ものに なる）が 要る。
  **課金の 判断は 本人。こちらから 進めない。**
