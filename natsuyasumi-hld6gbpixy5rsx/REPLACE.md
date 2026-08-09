# 差し替えが必要なもの

**最終的に販売を考えている。** いま借りているもののうち、そのままでは売れないものを並べる。
テスト用に一時的に拝借したものも、**必ずここに足してから**作業を進めること。

## 売る前に必ず差し替えるもの

| # | もの | 場所 | なぜ | 差し替えかた |
|---|---|---|---|---|
| R-001 | **ドット絵スプライト**（Majstek） | `photo/js/chars.js`／`td/chars.js`（2D試作にも 流用） | **非商用ライセンス。** クレジット表記も必須 | 自作／購入素材へ。`data/cast.json`（第1期 A1）に絵の参照を集約し、表1枚の差し替えで済むようにする |

## そのまま使えるもの（記録として）

| もの | 場所 | 状態 |
|---|---|---|
| **東方のキャラクター**（チルノ・大妖精・ルーミア・リグル・ミスティア・慧音・霊夢・魔理沙） | 全編 | **本人が権利まわりを対応する。こちらでは扱わない。** キャラクターは使える前提で作る。差し替えが要るのは絵（R-001）だけ |
| 背景写真6枚 | `photo/bg/*.jpg` | **CC BY / CC0。商用可。** 撮影者名の表示だけ維持する（`CREDITS.md` とタイトル画面） |
| ラジオ体操のメロディ | `photo/js/audio.js` | **自作。** 原曲（服部正・1951）は著作権が生きているので使っていない |
| 環境音（セミ・風・ひぐらし） | `photo/js/audio.js` | **Web Audio の手続き生成。** 素材を使っていない |
| **見下ろしタイルセット**（Top Down Adventure Assets） | `td/assets/tileset-world.png` | **CC0（パブリックドメイン）。商用OK・クレジット任意・改変/再配布可。** 出所：OpenGameArt。ライセンス文 `td/assets/LICENSE-topdown-adventure.txt` 同梱。2D試作の 世界タイル。より綺麗な素材に 差し替える 可能性あり（本人が「まず無料で試作→後で判断」） |
| **魚スプライト**（釣り用） | `td/assets/fish.png` | **OGA-BY 3.0。商用OK・再配布OK・帰属必須。** 出所：OpenGameArt「Fishing Game Assets Pixel Art」by CraftPix.net。クレジット「CraftPix.net 2D Game Assets」を `CREDITS.md`／タイトルに 残す。ライセンス文 `td/assets/LICENSE-fish-craftpix.txt` 同梱。Catch部分のみ 抽出・川魚を 選別 |
| **虫スプライト**（虫取り用） | `td/assets/bugs.png` | **CC0（パブリックドメイン）。商用OK・クレジット任意・再配布可。** 出所：OpenGameArt「Ambient Pixel Art Insects」by MadameBerry。カブト/トンボ/ホタル/ハチ/ガ を 抽出。ライセンス文 `td/assets/LICENSE-cutebugs.txt` 同梱 |

## 足すときの決まり

新しく借りたら、**その作業の commit と同じタイミングで**上の表に足す。
「あとでまとめて書く」は必ず漏れる。
