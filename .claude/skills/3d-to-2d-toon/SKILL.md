---
name: 3d-to-2d-toon
description: 3Dモデルを2D（手描きアニメ）に見せる技法。ギルティギアXrdの法線編集・頂点カラー・背面法アウトライン・リミテッドアニメ・カメラ前提の絵作り。3Dキャラをアニメ調で出すとき、トゥーンシェーダを選ぶときに読む
---

# 3Dを2Dに見せる技法（ギルティギアXrd系・一次資料からの書き起こし)

出典：
- GDC 2015 本村・C・純也「GuiltyGearXrd's Art Style: The X Factor Between 2D and 3D」
  https://www.gdcvault.com/play/1022031/GuiltyGearXrd-s-Art-Style-The
  ハンドアウトPDF https://www.ggxrd.com/Motomura_Junya_GuiltyGearXrd.pdf
- ASW Academy「GG Toon Line Control」 https://www.docswell.com/s/ASW_Academy/5LVY67-GG-Toonline-Eng
- 4Gamer 西川善司 GGXrd解説 前編 https://www.4gamer.net/games/216/G021678/20140703095/
- Hi-Fi RUSH UNREAL FEST 2023 https://gamemakers.jp/article/2023_08_01_46072/

## 基本思想（ここが核心）

> 「3DCGアニメーションを作る」のではなく「**2Dアニメを3DCGで再現する**」。
> カメラがほぼ固定なら、**そのカメラから見た1枚の絵として最高になるように**法線・変形・線を調整する。
> 汎用的な正しさより「今のカメラで正しい絵」を優先する。

## 技法一覧（効果が大きい順）

1. **法線編集**——最大の要因。素の法線だと陰影が細かく出すぎてCGに見える。頂点法線を手で
   大きな面に揃える（顔はほぼ球体の法線に転写）。Blenderのデータ転送モディファイアで球から転写→FBXに焼く。
2. **2値セルシェーディング＋陰バイアス**。明/陰の2値。頂点カラーで「陰になりやすさ」を塗り、
   動いたときの影のチラつきを抑える。ilmテクスチャ（スペキュラ強度・陰バイアス・内側線）で場所を支配する。
3. **リミテッドアニメ**——シェーダを変えずに「動きが2D」に見える。費用対効果が最も高い。
   キー間の補間をしない（ステップキー）。カットシーンは約15fps、戦闘は「2F,3F,3F,1F…」の可変コマ打ち。
   物理シミュは使わず髪も服も手付け。Unityではキーを Constant 補間にするか Animator の更新を間引くだけで雰囲気が出る。
4. **アウトライン＝背面法**（バックフェース押し出し）。頂点カラーA＝線の太さ（0=なし/0.5=標準/1=2倍）、
   B＝Zオフセット（内側の不要な線を隠す）。太さはカメラ距離とFOVで補正。
   内側の線はテクスチャ描き込みで、**縦線・横線だけで構成**（本村式ライン）＝拡縮してもジャギらない。
5. **カメラ前提のフレーム単位変形**。ボーンの非一様スケールで腕を伸ばす・輪郭を潰す・嘘パース。
   固定カメラのゲームでのみ使えるが効果絶大。
6. **キャラ専用ライト**。グローバル1灯ではなくキャラごとに「絵として正しい方向」から照らす。
7. **エフェクトの記号化**（風のタクト）。風＝白線、煙＝渦、炎＝なめる動き。物理でなくアニメの記号で描く。
8. **ハーフトーン・ハッチング**（Hi-Fi RUSH）。仕上げの「印刷物/アニメ感」。
   Hi-Fi RUSHの線の使い分け：**背景＝ポストプロセスのエッジ検出／キャラ＝背面法**。

## Unity URP での現実的な選択肢（推奨順）

1. **Unity Toon Shader (UTS3)**——公式・日本語ドキュメントあり。ランプ・アウトライン・リムまで設定だけで入口に立てる。
   https://docs.unity3d.com/ja/Packages/com.unity.toonshader@0.9/manual/index.html
2. **lilToon**——軽量・OSS。キャラ数体の小規模ゲームなら十分。 https://liltoon.org/
3. **自作（Shader Graph＋Render Objects）**——頂点カラーで線太さ・陰バイアスを制御するXrd式を本格的にやる場合。
   設計図：NiloCat https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample ／
   Daniel Ilett https://danielilett.com/2020-03-21-tut5-1-urp-cel-shading/
- 背面法アウトラインはURPでは Render Objects（Renderer Feature）で Front カリングの追加パス。
- 教訓「DCCとエンジンの見た目一致」：Blenderで作り込むより**最初からUnityのプレビューで法線・線を確認**しながら作る。

## なつやすみへの示唆

- 「リアルな背景の中を歩く3D東方キャラ」（DESIGN.md §4-b）をやるならこの技法群が本命。
- 最優先は 3（リミテッドアニメ・コマ打ち）と 1（法線編集）。トゥーンシェーダを入れるだけでは
  「CGっぽい3D」にしかならない。**コマを落とすだけで一気に2Dに見える**のが最安の一手。
- カメラが固定気味の企画なので、5（カメラ前提の絵作り）がそのまま使える。
