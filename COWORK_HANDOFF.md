# Claude Code 引き継ぎメモ
> Coworkセッション（2026-04-05）で行った作業と続きのタスク

---

## プロジェクト概要
- **名前**: ぷちゲーム部-AIラボ
- **内容**: 毎日1本ミニゲームを作って公開するプロジェクト
- **公開URL**: https://petit-game-ailabo.github.io/games/
- **リポジトリ**: https://github.com/petit-game-ailabo/games.git

---

## ローカル環境
```
C:\Users\talashi\todaysminigame-games\   ← メインリポジトリ
├── day00001/ ~ day00022/               ← 公開済みゲーム
├── wip/                                 ← 開発中ゲーム多数
├── evn/                                 ← 認証情報（下記参照）
├── deploy.ps1                           ← Windowsデプロイスクリプト
├── youtube_upload.py                    ← YouTubeアップロードスクリプト
├── draft-tweet.txt                      ← ツイート下書き
└── GAME_DESIGN_PRINCIPLES.md           ← ゲーム設計原則
```

---

## 今回やったこと（Coworkで完了済み）

### ✅ day00022「バルーン割り」を作成・コミット済み
- Canvas API + Web Audio API（Phaser不使用）
- コンボシステム・パーティクルエフェクト・モバイル対応
- `index.html` のゲーム一覧にも追加済み
- `git commit` 済み、**git push はまだ**

### ✅ YouTube用動画を生成済み
- ファイル: `balloon_pop_day22.mp4`（縦型1080×1920、30秒）
- Pythonで自動生成したプレビュー動画（Win+G録画の代替）

### ✅ ツイート文を作成済み
- ファイル: `draft-tweet.txt`

### ✅ デプロイスクリプトを作成済み
- `deploy.ps1`: git push + YouTube upload + ツイート表示を一括実行
- `youtube_upload.py`: YouTube Data API v3でShortsアップロード

---

## 認証情報の場所（evn/フォルダ）
```
evn/
├── github.env.txt     → GITHUB_TOKEN, GITHUB_REPO
├── Twitter.env        → Twitter API Key/Secret/Token
├── ElevenLabs.env     → ELEVENLABS_API_KEY
├── client_secret.json → YouTube OAuth2クライアントシークレット
└── token.pickle       → YouTube OAuth2トークン（保存済み）
```

---

## Claude Codeですぐやること

### 1. git push（最優先）
```bash
cd C:\Users\talashi\todaysminigame-games
# evn/github.env.txtからトークンを読んでpush
$token = (Get-Content evn/github.env.txt | Select-String "GITHUB_TOKEN").ToString().Split("=")[1]
git remote set-url origin "https://$token@github.com/petit-game-ailabo/games.git"
git push origin main
git remote set-url origin "https://github.com/petit-game-ailabo/games.git"
```

### 2. YouTube アップロード
```bash
cd C:\Users\talashi\todaysminigame-games
pip install google-api-python-client google-auth-oauthlib
python youtube_upload.py \
  --video balloon_pop_day22.mp4 \
  --title "22日目「バルーン割り」風船をタップして割れ！#shorts" \
  --description "浮かぶ風船をタップ！小さいほど高得点💥\nhttps://petit-game-ailabo.github.io/games/day00022/" \
  --tags "ゲーム,ミニゲーム,shorts,gamedev"
```

### 3. 以降の毎日フローをフルオート化
Claude Codeでは全ステップが自動実行可能：
1. ゲームコード生成（Canvas API）
2. `day000XX/index.html` 作成
3. `index.html` のゲーム一覧更新
4. `git add/commit/push`
5. 動画生成（FFmpeg or Puppeteer）
6. YouTube自動アップロード
7. ツイート文生成 → 手動投稿（Bot対策）

---

## 技術スタック（重要）
- ゲーム実装: **Canvas API + Web Audio API**（Phaser.jsは非推奨）
- ホスティング: GitHub Pages
- 動画: FFmpeg + edge-tts（ナレーション）
- SNS: YouTube Data API v3 / Twitter API v2

---

## Coworkでわかった制限
- サンドボックスのネットワークは制限あり → GitHub/YouTube/Twitter API 不可
- ファイル生成・git commit・FFmpegは動く
- Claude Codeはこれらの制限がない

---

## 参考リンク
- ゲーム一覧: https://petit-game-ailabo.github.io/games/
- day00022: https://petit-game-ailabo.github.io/games/day00022/
- GitHub: https://github.com/petit-game-ailabo/games
