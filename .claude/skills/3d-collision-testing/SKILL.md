---
name: 3d-collision-testing
description: 3Dゲームの当たり判定のオーサリング定石（部品側に1回・プリミティブ近似・レイヤー分け）と、広いマップの機械検査メニュー（NavMesh・グリッド走査・lint・到達可能性）。当たり判定を作る/直す/検査するときに読む
---

# 当たり判定の作り方と、広いマップの機械検査（一次資料からの書き起こし）

出典：
- Epic「FBX Static Mesh Pipeline」（UCX_慣行） https://dev.epicgames.com/documentation/unreal-engine/fbx-static-mesh-pipeline-in-unreal-engine
- Unity公式：MeshCollider / Layer-based collision / CCD / ComputePenetration
- Rare「Automated Testing of Gameplay Features in Sea of Thieves」GDC 2019
  https://gdcvault.com/play/1026366/Automated-Testing-of-Gameplay-Features
- セガ 龍が如くスタジオ「全自動バグ取りシステム」CEDEC 2020（バグの68.5%＝29,242件を自動発見）
  https://speakerdeck.com/segadevtech/long-garu-kusutazio-falseqaenziniaringuji-shu-wojie-ji-sitaquan-zi-dong-baguqu-risisutemu
- BotW CEDEC 2017 エンジニアリング（目視と自動チェックの分担・ZELDA_ERROR 6000超・座標自動添付）
  https://game.watch.impress.co.jp/docs/news/1078888.html

## 作り方の定石（壊さないためのチェックリスト）

1. **素のFBXをシーンに置かない。** 「FBX＋調整済みコライダー＋レイヤー」をまとめた**プレハブを1回**作り、
   配置は常にそのプレハブで。直すのも1回。これがAAAの標準（UnrealのUCX_＝アセット側にコリジョン内蔵、
   と同じ思想）。「置くと当たりがぐちゃぐちゃ」の根治策。
2. **コリジョンはプリミティブ数個で近似**（箱1〜4個が基本）。描画メッシュを衝突に使わない。
   凸Meshは255三角形上限／非convex同士は衝突しない／非convexは動く物に使えない。
3. 歩行用コリジョンは見た目より**単純・なめらか・少し甘め**。細部の引っかかりはプレイヤー体験を壊す。
4. **レイヤーを分ける**：Ground／Wall／CameraBlocker／Interact／Player／NPC。
   Collision Matrix で不要なペアを切る。「見えない壁はカメラに当てない」だけで事故の大半が消える。
5. 動かすコライダーには必ず **Kinematic Rigidbody**（Rigidbodyなしの静的コライダーを動かさない）。
6. スケールはインポートで焼き込み、シーン内は (1,1,1)。負スケール禁止（megakitの100倍事故の一般化）。
7. 薄い壁（数cm）を作らない。高速物体だけ CCD（Continuous）。
8. **意図的な見えない壁は名前で機械判別できるようにする**（BLK_ 等の接頭辞＋専用レイヤー）。
   lint の誤検知がゼロになる。

## 広いマップの機械検査メニュー（安い順）

1. **NavMeshベイクの目視**（ほぼタダ）：歩行コライダーからベイクすると、コライダー穴・孤島が
   「NavMeshの穴」として一目で見える。ベイクは無料の可視化デバッガ。
2. **シーンlint**（半日）：コライダー無しRenderer／Renderer無しコライダー／スケール異常／レイヤー未設定を列挙。
   → なつやすみでは `Kensa.Butsu` が実装。
3. **重要地点の到達可能性**（半日）：`NavMesh.CalculatePath` で拠点→全イベント地点の経路が
   `PathComplete` かを検査。
4. **グリッド走査**（1日）：全域を0.5〜1m間隔でレイキャスト＋プレイヤーカプセル `CheckCapsule`。
   穴・急斜面・めり込み・「立てるのに歩いて行けない飛び地」を検出。
   → なつやすみでは `Kensa.Aruku` が実装（塗りつぶしで到達可能性まで）。
5. **貫通検査**（1日）：`OverlapBox`＋`Physics.ComputePenetration` で物同士のめり込みを閾値検出。
6. **リプレイ・ウォークスルー**（数日）：入力記録再生で巡回し、落下・スタック（一定時間座標が動かない）を
   スクショ付きで自動報告（龍が如く方式の最小版）。
7. ランダム操作ボット：一晩歩かせて異常検知。学習ボット（Ubisoft/EA方式）は個人開発では割に合わない。

## 大規模事例から個人開発に効く考え方

- **龍が如く方式**：バグ業務を「探索→報告→仕分け→修正→修正確認」に分けて自動化。
  自動テストがバグの7割を見つけた。個人開発でも「探索（ボット歩行）と報告（座標付きログ）」だけで真似できる。
- **BotW方式**：目視チェックと自動チェックを分担。規格検査（モデル・テクスチャ）は機械、
  絵の良し悪しは人。バグ報告に座標を自動添付。
- **Sea of Thieves方式**：開発初日から自動テスト。誰でもテストを書ける形にする。
  フレーキー（不安定テスト）対策を仕組みに入れる（なつやすみの `-bugs` 自動テストが
  「虫の方を向かず網を振っていた」のはまさにフレーキーの実例）。

## なつやすみでの運用

- 検査ツールは `unity/Assets/Editor/Kensa.cs`：
  - `rebuild.ps1 -Only Kensa.Aruku` … 全域グリッド走査＋到達可能性の塗りつぶし。
    地図は `%TEMP%\natsuyasumi\kensa_map.txt`
  - `rebuild.ps1 -Only Kensa.Butsu` … 見た目とあたりの棚おろし（lint）
- 物を置いたり地形を触ったら `Kensa.Aruku`、アセットを足したら `Kensa.Butsu` を回す。
- 今後の配置物は「あたり付きプレハブを1回作る」方式に移行する（PLAN.md 当たり判定の節）。
