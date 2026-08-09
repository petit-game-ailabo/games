# 幻想郷のなつやすみ — 素材クレジット / ライセンス

『ぼくのなつやすみ』（2000年 / ミレニアムキッチン企画開発・SCE発売）へのオマージュとして作った、
東方Project二次創作の無料ブラウザミニゲームです。

> **⚠️ 現在の配布は「無料・非商用」限定。** 下記キャラスプライト（Majstek）が**非商用ライセンス**のため、
> このままでは販売できない。**商用版を出す前に、キャラ絵を必ず差し替えること**（→ `REPLACE.md` R-001）。
> 世界タイル(ansimuz・CC0相当)・魚(CraftPix・OGA-BY)・虫(cutebugs・CC0)・音(自作/手続き生成)は商用OK。

## キャラクタースプライト

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

## 背景写真（`photo/` 版）

実写の背景は Wikimedia Commons から、**CC0 / パブリックドメイン / CC BY** のものだけを選んでいます。
**CC BY-SA は継承義務があるため採用していません。**
いずれも 960x540 にトリミングし、色調整・ブルーム・ぼかしを加えています。

| ファイル | 画面 | 元画像 | ライセンス | 撮影者 |
|---|---|---|---|---|
| `photo/bg/azemichi.jpg` | あぜみち | [Path(あぜ道) - panoramio.jpg](https://commons.wikimedia.org/wiki/File:Path(%E3%81%82%E3%81%9C%E9%81%93)_-_panoramio.jpg) | **CC BY 3.0** | **Fumihiko Ueno** |
| `photo/bg/mori.jpg` | もりのみち | [Japan, Tochigi - Nikko Takinoo shrine 2014 1.jpg](https://commons.wikimedia.org/wiki/File:Japan,_Tochigi_-_Nikko_Takinoo_shrine_2014_1.jpg) | **CC BY 2.0** | **Guilhem Vellut** |
| `photo/bg/iemae.jpg` | いえのまえ | [200michinoku folk village3872.jpg](https://commons.wikimedia.org/wiki/File:200michinoku_folk_village3872.jpg) | **CC BY 2.5** | **663highland** |
| `photo/bg/zashiki.jpg` | ざしき | [Youkoukan06n4592.jpg](https://commons.wikimedia.org/wiki/File:Youkoukan06n4592.jpg) | **CC BY 2.5** | **663highland** |
| `photo/bg/rouka.jpg` | ろうか | [141122 Kozanji Shimonoseki Yamaguchi pref Japan12n.jpg](https://commons.wikimedia.org/wiki/File:141122_Kozanji_Shimonoseki_Yamaguchi_pref_Japan12n.jpg) | **CC BY 2.5** | **663highland** |
| `photo/bg/doma.jpg` | どま | [Doshin-kaoku07s3200.jpg](https://commons.wikimedia.org/wiki/File:Doshin-kaoku07s3200.jpg) | **CC BY 2.5** | **663highland** |

すべて **CC BY**（表示必須）。撮影者名はタイトル画面にも出している。
※ 旧 `michi.jpg`（Tōzaki Shrine / CC0 / 先従隗始）は「人の気配が強すぎる」ため差し替え・削除。

CC BY のものは**撮影者名の表示が必須**なので、写真を差し替えたときはこの表を必ず更新すること。

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
