# セットアップガイド

## 📋 目次

1. GitHubリポジトリ作成
2. GitHub Pages 有効化
3. 毎日のゲーム公開フロー
4. テスト方法

---

## 1. GitHubリポジトリ作成

### ステップ1: GitHub でリポジトリ作成

1. https://github.com/new にアクセス
2. リポジトリ名: `todaysminigame.github.io`
   - **重要**: この形式にすると `https://todaysminigame.github.io/` がルートURLになる
3. Public に設定
4. 「Create repository」

### ステップ2: ローカルをプッシュ

```bash
cd /home/ageha/.openclaw/workspace/todaysminigame-games

# リモート追加
git remote add origin https://github.com/あなたのユーザー名/todaysminigame.github.io.git

# プッシュ
git branch -M main
git push -u origin main
```

---

## 2. GitHub Pages 有効化

### ステップ1: Settings に移動

1. GitHubリポジトリページ → 「Settings」タブ
2. 左メニュー → 「Pages」

### ステップ2: Source 設定

1. Source: `Deploy from a branch`
2. Branch: `main` / `/(root)`
3. 「Save」

### ステップ3: 確認

数分後に以下のURLでアクセス可能:
- トップページ: `https://todaysminigame.github.io/`
- テストゲーム: `https://todaysminigame.github.io/template-tap-game/`

---

## 3. 毎日のゲーム公開フロー

### 自動デプロイスクリプト使用

```bash
cd /home/ageha/.openclaw/workspace/todaysminigame-games

# テンプレートから今日のゲームを生成
./deploy.sh template-tap-game "タップゲーム"

# GitHub にプッシュ
git push origin main
```

これで `https://todaysminigame.github.io/2026-03-07/` に公開される。

### 手動デプロイ

```bash
# 1. 今日の日付フォルダ作成
TODAY=$(date +%Y-%m-%d)
cp -r template-tap-game $TODAY

# 2. ゲーム名を編集（任意）
# $TODAY/index.html を編集

# 3. Gitコミット
git add $TODAY
git commit -m "Add game: $TODAY"
git push origin main
```

---

## 4. テスト方法

### ローカルでテスト

**方法1: Python HTTPサーバー**
```bash
cd /home/ageha/.openclaw/workspace/todaysminigame-games
python3 -m http.server 8000
```
→ ブラウザで `http://localhost:8000/template-tap-game/`

**方法2: VS Code Live Server**
1. VS Codeで `template-tap-game/index.html` を開く
2. 右クリック → 「Open with Live Server」

### GitHub Pages でテスト

プッシュ後、数分待ってから:
- https://todaysminigame.github.io/template-tap-game/

---

## 5. コンプライアンス対応

### ✅ 必須対応

1. **Phaser.js クレジット表示**
   - ✅ 既に実装済み（各ゲームに「Powered by Phaser」表示）

2. **Twitter Bot 明記**
   - プロフィールに「🤖 自動投稿Bot」と記載

3. **プライバシーポリシー**（有料化時）
   - 今後作成予定

---

## 📝 チェックリスト

- [ ] GitHubアカウント作成
- [ ] リポジトリ作成（todaysminigame.github.io）
- [ ] ローカルをプッシュ
- [ ] GitHub Pages 有効化
- [ ] テストゲームが表示されることを確認
- [ ] Twitter/YouTube API設定
- [ ] 初回投稿テスト

---

## 🔗 関連ドキュメント

- [LICENSE_COMPLIANCE.md](../minigame-autopost/LICENSE_COMPLIANCE.md) - 利用規約調査
- [README.md](./README.md) - プロジェクト概要
- [../minigame-autopost/README.md](../minigame-autopost/README.md) - 投稿システム
