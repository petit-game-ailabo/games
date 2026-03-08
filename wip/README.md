# WIP（Work In Progress）

開発中のゲームテスト環境

## アクセス方法

各ゲームは以下のURLでアクセス可能:
```
https://petit-game-ailabo.github.io/games/wip/game-001/
https://petit-game-ailabo.github.io/games/wip/game-002/
```

## 使い方

### 1. 新しいゲームを作成

```bash
cd wip
cp -r ../template-mobile-tap game-001
# game-001/index.html を編集
```

### 2. コミット＆プッシュ

```bash
git add wip/game-001
git commit -m "WIP: game-001 開発中"
git push origin main
```

### 3. GitHub Pages で確認（1-2分後）

```
https://petit-game-ailabo.github.io/games/wip/game-001/
```

### 4. スマホでテスト

URLをスマホでアクセス（本番環境と完全に同じ）

### 5. 完成したらデプロイ

```bash
cd ..
./deploy.sh wip/game-001 "ゲーム名"
git push origin main
```

## 注意

- wip/ 内のゲームは **ゲーム一覧に表示されない**
- URLを知っている人だけアクセス可能
- 検索エンジンにはインデックスされる可能性あり（robots.txt で制御可能）

