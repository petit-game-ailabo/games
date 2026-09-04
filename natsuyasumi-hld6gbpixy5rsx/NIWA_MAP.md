# 庭（Niwa）のコード地図

庭を直すときは **この地図 → `grep -n "見出しの文字" ファイル` → その関数の 30〜60行だけ読む**。
`sed -n 1,200p` で丸ごと出さない（1回で 1万トークン。以後の呼び出し全部に乗る）。
行番号は目安（ずれる）。見出しの文字で探す。

## ファイルと役目

| ファイル | 役目 | 入口 |
|---|---|---|
| `Editor/BuildNiwa.cs` | 庭の場面を全部コードで組む。1つの巨大な `Build()`（下の節） | `NiwaAll.Win / WinWeb` から |
| `Editor/NiwaAll.cs` | 組む→焼く→ビルドの段どり | `tools/rebuild.ps1 -Only NiwaAll.Win` |
| `Editor/NiwaIe.cs` | 母屋（L字・玄関・戸袋・雨どい・庇・汚れ） | `NiwaIe.Build(ie)`。寸法定数 `X0 X1 ZN ZM ZS KX YUKA H1 DOSHI H2 NOKI GNOKI GENKAN_X` |
| `Editor/HouseRoof.cs` | 寄棟の屋根メッシュ（`Opt`・`eaveDrop`・軒桁） | `HouseRoof.Build(parent, Opt, tile, wood, plaster)` |
| `Editor/NiwaKawara.cs` | 軒瓦の一列（9点断面・垂れ） | `Ichimai` |
| `Editor/NiwaJimenE.cs` | 地面の高さ関数と一枚絵の焼き | `Takasa(x,z)`＝高台 `Takadai`＋段 `Dan`。定数 `NH=0.6 SAKA_Z0/Z1/HABA TX TZ TH HABA=32`。`Yaku` `Ita` `Ami` `MichiAmi` |
| `Editor/NiwaJimen.cs` | 接地の影・座らせ・浮き検査 | `Setchi` `Ki` `Uki(oya, 名)`（名に Kabetsuki を含む物は飛ばす） `Kaku` |
| `Editor/TakeV1.cs` | 竹藪 `Mure`／四ツ目垣 `Kaki`／丸太 `Maruta`／岩 `Iwa`／石垣 `Ishigaki`／生垣 `Ikegaki`／折れ線 `Kizamu` `SotoHousen` | `Yabu` が筒メッシュの入れ物（材質 0緑1黄2茶3杭4皮） |
| `Editor/KiV5.cs` | 筒の木（幹16角・`Suji/Futo` 背骨・`FUTOSA_BAI`） | `new KiV5.Hayashi(root)` → `Ueru` → `Katameru` |
| `Editor/NiwaNaya.cs` `NiwaMizu.cs` `NiwaBuhin.cs` | 納屋・水まわり(立水栓 `SUI`・ヒマワリ)・材質と箱の共通部品 | `Build(root)` |
| `Editor/SetupURP.cs` | 取り込み規則（`/mushi/` `/shashin/`） | |
| `Scripts/NiwaMushi.cs` | 虫（種の表 `Shurui`・`Perch`・動き `*Ugoki`・影・寄りカード） | `-mushi` でデバッグ配置 |
| `Scripts/MuraMove.cs` `MuraDay.cs` | 主人公の移動・`-tour` `-noboru`・`-hour=N` | |
| `Scripts/NiwaHimawari.cs` | ヒマワリの水やり日数 | |
| `ArtSource/make_take.py` `make_mushi.py` `make_kusaki.py` `make_himawari.py` `make_ie_yogore.py` | 絵の生成（写真は `Art/Textures/shashin/`＝本人が Codex で作った物） | |

## `BuildNiwa.Build()` の節（grep 用の見出し）

順に並んでいる。`grep -n "^        // ----" Assets/Editor/BuildNiwa.cs` で全部出る。

1. `環境光は Flat` … ライティングの型
2. `地めん（草）と 門の外の 道（土）` … `NiwaJimenE.Ami` 凸凹の網＋`MichiAmi`。色 `JIMEN_IRO` `TSUCHI_IRO`（ファイル冒頭）
3. `家（megakit` … `ie.position = (0, NH, 4 - MINAMI)` → `NiwaIe.Build`
4. `屋敷の 囲い` … 石垣2本の折れ線（`SW = SAKA_HABA+0.38`, `ZK`, `zSakaMoto`）→ `Ishigaki`。生垣 → `Kizamu(...,0.2)` → `Ikegaki`
5. `見えない かべ` … `BLK_S1/S2/E/W/N/Road*`
6. `玄関→門の 飛び石` … 飛び石・くつぬぎ石
7. `木（本人` … `KiV5.Hayashi`。庭の木は1本。西・北・東・南の木立ち。竹 `TakeV1.Mure` ×2
8. `草・花` … `Kusa1` は空（草の房は置かない D-197）。岩 `Iwa` ×2・丸太 `Maruta`
9. `納屋（庭の 西）と 水まわり` … `NiwaNaya.Build` `NiwaMizu.Build`
10. `遠景の 描き割り` … `Kakiwari` `KakiwariCam`（空・里山・雲、Unlit）
11. `高台` … `TX TZ TH` の丘、上の岩と丸太、`BLK_Higashi*`
12. `主人公` … Player (0, NH+0.3, -1.5)、`足もとの 影`、`撮影ツアーの たちば`（`Mise_*`）
13. `カメラ` … 望遠 FOV26・追従 HD-2D 型
14. `太陽と 1日` `ポストFX`
15. `虫` … `NiwaMushi` 配線（材質 `Mushi_<id>.mat`、幹の背骨 `hayashi.Suji/Futo`）
16. `物を 地ばんに すわらせる` … **`nuki` 一覧に載る名前は座らせない**（自分で高さを決めた物：Take Ishigaki Ikegaki Iwa Mushi Mise_ Ie …）。新しい物を足して浮く/沈むならまずここ
17. `庭の 地面の 一枚絵` … 物が全部置かれてから `Yaku`。板 `JimenE` は y=0.05（物が 0.10 より低いと隠れる）
18. `接地の影` … `Setchi` `Ki` `Uki`（最後）

## 検証（数字→絵の順）

```
unity/tools/rebuild.ps1 -Only NiwaAll.Win      # 組む＋Win ビルド
niwa.exe -tour -hour=14 [-mushi]               # 撮影ツアー（Player.log に [Probe] と Uki の結果）
unity/tools/rebuild.ps1 -Only NiwaAll.WinWeb   # 公開用（Web も焼く）
```
Player.log: `%USERPROFILE%/AppData/LocalLow/petit-game-ailabo/niwa/Player.log`。
浮き・埋まりは `Uki` の行で確かめる。絵は最後に 1〜2枚、読む前に 960 幅へ縮める。

## よくある落とし穴（詳しくは SKILL.md と DECISIONS.md）

- 面が見えない→まず目印の色で「描かれているか」。裏返し（巻き順）が定番。
- 実行時 `Shader.Find` はビルドで失敗する。材質はビルド時にアセットで作る。
- 筒の UV は継ぎ目に頂点を1つ余分に（sides+1）。
- 段（`Dan`）で高さを変えたら、座らせ直しの `nuki` を確かめる。
