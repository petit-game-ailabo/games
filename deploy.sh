#!/bin/bash
#
# GitHub Pages 自動デプロイスクリプト
#

set -e

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

# 次のゲーム番号を計算（day00001, day00002, ...）
LAST_DAY=$(ls -d day* 2>/dev/null | sort -V | tail -1)
if [ -z "$LAST_DAY" ]; then
    # 最初のゲーム
    DAY_NUM=1
else
    # 最後のゲーム番号を取得して+1
    DAY_NUM=$(echo "$LAST_DAY" | sed 's/day0*//')
    DAY_NUM=$((DAY_NUM + 1))
fi

# 5桁でゼロパディング
GAME_DIR=$(printf "day%05d" $DAY_NUM)

echo "📅 ゲーム番号: $GAME_DIR"

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
git commit -m "Add game: $GAME_NAME ($GAME_DIR)"

# index.html のゲーム一覧を更新
echo "📝 index.html のゲーム一覧を更新中..."
if [ -f "index.html" ]; then
    # games配列に新しいゲームを追加（JavaScriptの配列を自動更新）
    node -e "
    const fs = require('fs');
    const html = fs.readFileSync('index.html', 'utf8');
    const gamesMatch = html.match(/const games = \[(.*?)\];/s);
    if (gamesMatch) {
        const gamesArray = gamesMatch[1].trim();
        const newGame = \`{ day: '$GAME_DIR', title: '$GAME_NAME' }\`;
        let newGamesArray = gamesArray ? gamesArray + ',\\n            ' + newGame : newGame;
        const updatedHtml = html.replace(
            /const games = \[(.*?)\];/s,
            'const games = [\\n            ' + newGamesArray + '\\n        ];'
        );
        fs.writeFileSync('index.html', updatedHtml);
        console.log('✅ index.html 更新完了');
    }
    " || echo "⚠️  index.html の自動更新に失敗（手動更新してください）"
    
    git add index.html
    git commit -m "Update game list: $GAME_NAME ($GAME_DIR)"
fi

echo "✅ デプロイ準備完了"
echo ""
echo "次のステップ:"
echo "1. git push origin main"
echo "2. GitHub Pagesで公開"
echo "3. ゲームURL: https://petit-game-ailabo.github.io/games/$GAME_DIR/"
echo "4. トップページ: https://petit-game-ailabo.github.io/games/"
