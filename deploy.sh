#!/bin/bash
#
# GitHub Pages 自動デプロイスクリプト
#

set -e

# 今日の日付
TODAY=$(date +%Y-%m-%d)

echo "📅 今日の日付: $TODAY"

# 引数チェック
if [ "$#" -lt 2 ]; then
    echo "使い方: ./deploy.sh <テンプレート名> <ゲーム名>"
    echo "例: ./deploy.sh template-tap-game タップゲーム"
    exit 1
fi

TEMPLATE=$1
GAME_NAME=$2

# テンプレートが存在するか確認
if [ ! -d "$TEMPLATE" ]; then
    echo "❌ エラー: テンプレート '$TEMPLATE' が見つかりません"
    exit 1
fi

# 今日のゲームフォルダ作成
GAME_DIR="$TODAY"

if [ -d "$GAME_DIR" ]; then
    echo "⚠️  警告: $GAME_DIR は既に存在します"
    read -p "上書きしますか？ (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
    rm -rf "$GAME_DIR"
fi

echo "📁 ゲームフォルダ作成中: $GAME_DIR"
cp -r "$TEMPLATE" "$GAME_DIR"

# タイトルを更新
echo "📝 ゲーム名を更新: $GAME_NAME"
sed -i "s/タップゲーム/$GAME_NAME/g" "$GAME_DIR/index.html"

# Gitコミット
echo "📤 Gitにコミット中..."
git add "$GAME_DIR"
git commit -m "Add game: $GAME_NAME ($TODAY)"

echo "✅ デプロイ準備完了"
echo ""
echo "次のステップ:"
echo "1. git push origin main"
echo "2. GitHub Pagesで公開"
echo "3. URL: https://todaysminigame.github.io/$TODAY/"
