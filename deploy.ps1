# ============================================================
# ぷちゲーム部 - ワンクリック デプロイスクリプト
# 使い方: 右クリック → PowerShellで実行
# ============================================================

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  ぷちゲーム部 デプロイ開始 🚀" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

# ---- 環境変数読み込み ----
$envFiles = @("evn/github.env.txt", "evn/Twitter.env", "evn/ElevenLabs.env")
foreach ($f in $envFiles) {
    if (Test-Path $f) {
        Get-Content $f | Where-Object { $_ -match "^[^#].*=.*" } | ForEach-Object {
            $parts = $_ -split "=", 2
            [Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim(), "Process")
        }
    }
}

$token = $env:GITHUB_TOKEN
$repo  = $env:GITHUB_REPO

if (-not $token -or -not $repo) {
    Write-Host "❌ GitHub トークンが見つかりません (evn/github.env.txt を確認)" -ForegroundColor Red
    Read-Host "Enterで終了"
    exit 1
}

# ---- STEP 1: Git Push ----
Write-Host "`n[1/3] GitHub Push..." -ForegroundColor Yellow
git remote set-url origin "https://$token@github.com/$repo.git"
git push origin main
git remote set-url origin "https://github.com/$repo.git"  # トークンをURLから除去
Write-Host "✅ GitHub Push 完了" -ForegroundColor Green
Write-Host "    → https://petit-game-ailabo.github.io/games/" -ForegroundColor Gray

# ---- STEP 2: YouTube アップロード ----
Write-Host "`n[2/3] YouTube Shorts アップロード..." -ForegroundColor Yellow

$videoFile = Get-ChildItem -Filter "balloon_pop_day22.mp4" | Select-Object -First 1
if (-not $videoFile) {
    Write-Host "⚠️  動画ファイルが見つかりません（スキップ）" -ForegroundColor DarkYellow
} else {
    python youtube_upload.py --video $videoFile.FullName `
        --title "22日目「バルーン割り」風船をタップして割れ！#ゲーム #shorts" `
        --description "浮かぶカラフルな風船をタップして割ろう！小さいほど高得点💥 コンボでスコア爆発🔥

▶️ ブラウザで無料プレイ
https://petit-game-ailabo.github.io/games/day00022/

毎日新しいミニゲームを公開中 🎮
#ぷちゲーム部 #ブラウザゲーム #ミニゲーム #shorts" `
        --tags "ゲーム,ミニゲーム,ブラウザゲーム,shorts,gamedev" `
        --category 20
    Write-Host "✅ YouTube アップロード完了" -ForegroundColor Green
}

# ---- STEP 3: ツイート文を表示 ----
Write-Host "`n[3/3] ツイート文（手動投稿用）" -ForegroundColor Yellow
Write-Host "--------------------------------------------" -ForegroundColor Gray
if (Test-Path "draft-tweet.txt") {
    Get-Content "draft-tweet.txt"
}
Write-Host "--------------------------------------------" -ForegroundColor Gray
Write-Host "↑ Twitterに手動投稿してください" -ForegroundColor Cyan

Write-Host "`n============================================" -ForegroundColor Green
Write-Host "  デプロイ完了 🎉" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Read-Host "`nEnterで終了"
