# ref/ — 見て学ぶための借りもの置き場

**ここに置いたものは Unity に取りこまれない**（`Assets/` の外なので AssetDatabase が見ない）。
**リポジトリにも入らない**（重いので `.gitignore` 済み。このファイルだけ commit する）。

「見て学ぶ」ための資料と、「そのまま使う」素材は分ける。
そのまま使うものは `Assets/Art/` に入れて `CREDITS.md` / `REPLACE.md` に書く。
ここに置くのは **形の作りかたを盗むための手本** で、成果物には1バイトも入らない。

---

## quaternius-medieval-village

- 出どころ：Quaternius「Medieval Village MegaKit」Standard（無料版）
  https://quaternius.com
- ライセンス：**CC0 1.0**（商用可・クレジット不要・再配布可）。`License_Standard.txt` 同梱
- 中身：FBX / OBJ / glTF ×176点、4K PBR テクスチャ 54枚。合計 169MB

### なぜ `Assets/` に入れないか

1. **絵柄が合わない。** 中世ヨーロッパのハーフティンバー＋赤い素焼き瓦＋石積み。
   こちらは**日本の農家**（田の字型・土間・縁側・入母屋・障子）。建物としては別ものなので
   モデルをそのまま置いても村に混ざらない。
2. **こまかさが合わない。** 向こうは 4K の PBR、こちらは **1mあたり32ドット**。
   並べると「写真の家の横にドット絵の家」になる。
3. **重い。** 169MB。同じ形が FBX/OBJ/glTF の3通りで入っていて、実際に要るのは多くて数点。
   公開リポジトリは GitHub Pages がそのまま配信するので、置けば配信物が169MB増える。

### 何を学んだか（2026-08-16・家の形の作り直しで実際に使った）

**部品の名まえの付けかたが、そのまま建てかたの説明になっている。**

```
Wall_Window / Wall_Door / Corner_Exterior / Corner_Interior /
DoorFrame_Round / Overhang_Roof / Overhang_Side_L / Overhang_Side_R /
Balcony_Simple_Straight / Balcony_Cross_Corner / Floor_WoodDark_OverhangCorner / ...
```

つまり **壁は「壁」ではなく「窓つきの壁」「戸口つきの壁」「隅」「軒の出」「軒の横」**に
分かれている。1枚の面を張るのではなく、**役目のちがう部品を組む**。
`Preview.jpg` の家が立体に見えるのは、テクスチャではなく
**軒が出ていて／隅に柱があって／庇に横板がついている**から。

この考えかたを日本の農家に置きかえたのが、いまの `BuildHouse.cs` / `HouseRoof.cs`：

| MegaKit | こちらの言いかた |
|---|---|
| `Corner_Exterior` | 隅柱 |
| `Overhang_Roof` / `Overhang_Side_L/R` | 軒の出・けらば・破風板 |
| `Wall_Window` | 障子＋桟／2階の窓＋わく |
| （壁の腰に横板） | 下見板の腰壁＋水切り |
| 屋根が面ではなく厚みのある部品 | 軒の小口・軒天・垂木 |

### あとで実際に使うなら

小物（樽・木箱・柵・荷車・石）は文化圏をまたいでも成立するものがある。
使うときは **FBX だけを `Assets/Art/Models/` に取りこみ、テクスチャは捨てて
こちらの 48px 素材を貼りなおす**（4K のまま入れると絵柄が壊れる）。
そのときは `CREDITS.md` と `REPLACE.md` に必ず書く（CC0 なので記載義務はないが、
出どころが追えなくなるのを防ぐため、この企画では全部書く決まり）。
