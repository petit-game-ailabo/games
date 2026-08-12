# 幻想郷のなつやすみ — 素材クレジット / ライセンス

『ぼくのなつやすみ』（2000年 / ミレニアムキッチン企画開発・SCE発売）へのオマージュとして作った、
東方Project二次創作の無料ブラウザミニゲームです。

> **⚠️ `photo/` と `td/` の配布は「無料・非商用」限定。** 下記キャラスプライト（Majstek）が**非商用ライセンス**のため、
> このままでは販売できない。**商用版を出す前に、キャラ絵を必ず差し替えること**（→ `REPLACE.md` R-001）。
> 世界タイル(ansimuz・CC0相当)・魚(CraftPix・OGA-BY)・虫(cutebugs・CC0)・音(自作/手続き生成)は商用OK。
>
> **✅ `unity/`（奥行き版）は 2026-08-13 に 本人が 用意した キャラ絵へ 差し替え済み**＝この版に Majstek は 入っていない。

## キャラクタースプライト（`unity/` 奥行き版）― 2026-08-13 差し替え

**本人が用意した 30体ぶんの ドット絵**（元画像 `unity/ArtSource/chars_source_10x3.png`）。
黒地だった 元画像から 背景を ぬいて 透過に し、1コマ 48x64・10列 x 3行の アトラス
`unity/Assets/Art/Sprites/chars_tall.png` に 組み直している（足もとを コマの 下端に そろえて 接地させる）。
**Majstek の 素材は 使っていない。**

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
