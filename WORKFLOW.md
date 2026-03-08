# ゲーム公開ワークフロー

## 📅 スケジュール

### 前日（18:00-22:00）: 開発期間

```
18:00 - アイデア出し
       ↓
18:30 - WIP環境でプロトタイプ作成
       ↓
19:00 - テストプレイ（PC + スマホ）
       ↓
19:30 - 改善1回目
       ↓
20:00 - テストプレイ
       ↓
20:30 - 改善2回目
       ↓
21:00 - 最終調整
       ↓
21:30 - WIPに保存（翌朝デプロイ準備）
```

### 当日（06:00-07:00）: 公開準備

```
06:00 - 最終チェック
       ├─ スマホでWIPをテストプレイ
       ├─ PC でWIPをテストプレイ
       └─ バグチェック
       ↓
06:10 - デプロイ
       └─ ./deploy.sh wip/game-xxx "ゲーム名"
       └─ git push origin main
       ↓
06:15 - 動画録画
       ├─ スマホで録画（縦長・60秒以内）
       └─ Windows: Win+G or OBS Studio
       ↓
06:20 - YouTube Shorts アップロード
       └─ python3 youtube_uploader.py 動画 "ゲーム名" "URL"
       ↓
06:25 - Twitter下書き生成
       └─ python3 twitter_draft.py "ゲーム名" "URL"
       ↓
06:30 - 待機
       ↓
07:00 - Twitter投稿（手動承認）
       └─ twitter_draft.txt をコピペして投稿
```

---

## 🔄 WIP環境を使った開発フロー

### 1. 新しいゲームを作成

```bash
cd /home/ageha/.openclaw/workspace/todaysminigame-games/wip

# テンプレートをコピー
cp -r ../template-mobile-tap game-001

# 編集
code game-001/index.html
```

### 2. GitHub Pages にプッシュ

```bash
cd /home/ageha/.openclaw/workspace/todaysminigame-games
git add wip/game-001
git commit -m "WIP: game-001 開発中"
git push origin main
```

**1-2分後、GitHub Pages で公開される**:
```
https://petit-game-ailabo.github.io/games/wip/game-001/
```

### 3. スマホでテスト

- URLをスマホのブラウザに入力
- 本番環境と完全に同じ条件でテスト
- タップ判定、画面サイズ、効果音を確認

### 4. 改善ループ

```bash
# index.html を編集
code wip/game-001/index.html

# プッシュ
git add wip/game-001
git commit -m "WIP: game-001 改善"
git push origin main

# 1-2分後、再度スマホでテスト
```

### 5. 完成したらデプロイ

```bash
cd /home/ageha/.openclaw/workspace/todaysminigame-games
./deploy.sh wip/game-001 "ゲーム名"
git push origin main
```

**自動的に**:
- day00XXX フォルダが作成される
- ゲーム一覧に追加される
- 公開URL: `https://petit-game-ailabo.github.io/games/day00XXX/`

---

## 📱 スマホテストのポイント

### テストURL

```
https://petit-game-ailabo.github.io/games/wip/game-001/
```

- このURLは **ゲーム一覧に表示されない**
- URLを知っている人（あなた）だけアクセス可能
- 検索エンジンには登録されない（robots.txt で制御）

### チェック項目

- [ ] タップ判定が正確か
- [ ] 画面サイズが適切か（縦横両対応）
- [ ] 効果音が鳴るか
- [ ] エフェクトが見やすいか
- [ ] 文字が読みやすいか
- [ ] スクロールが発生しないか

---

## 📊 公開後の振り返り

### 当日夜（21:00）

improvements.md に記録:

```markdown
## dayXXXXX: ゲーム名（YYYY-MM-DD）

### 前回からの改善点
- 〇〇を追加
- △△を改善

### プレイ感想
- 良かった点: ...
- 改善点: ...

### 次回の改善候補
- [ ] ...
- [ ] ...
```

### 週次レビュー（日曜夜）

1週間のゲームを振り返り:
- 人気だったゲームタイプ
- 改善が効果的だった点
- 次週の方針

---

## 🎯 目標

- **毎朝7:00投稿** - 固定
- **1日1改善** - 最低1つは前日より進化
- **スマホ対応** - 全ゲーム必須
- **20日で10種類** - ゲームバリエーション拡大
