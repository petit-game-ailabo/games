#!/usr/bin/env python3
"""
YouTube Shorts 自動アップロードスクリプト
使い方: python youtube_upload.py --video <file> --title <title> --description <desc>
"""
import os, sys, pickle, argparse, json
from pathlib import Path
import requests as req_lib

# Google API
try:
    from google.oauth2.credentials import Credentials
    from google_auth_oauthlib.flow import InstalledAppFlow
    from google.auth.transport.requests import Request
except ImportError:
    print("必要なライブラリをインストール中...")
    os.system(f"{sys.executable} -m pip install google-auth google-auth-oauthlib requests --quiet")
    from google.oauth2.credentials import Credentials
    from google_auth_oauthlib.flow import InstalledAppFlow
    from google.auth.transport.requests import Request

SCOPES = ["https://www.googleapis.com/auth/youtube.upload"]
BASE_DIR = Path(__file__).parent
TOKEN_FILE = BASE_DIR / "evn" / "token.pickle"
SECRET_FILE = BASE_DIR / "evn" / "client_secret.json"


def get_credentials():
    creds = None
    if TOKEN_FILE.exists():
        with open(TOKEN_FILE, "rb") as f:
            creds = pickle.load(f)
    if not creds or not creds.valid:
        if creds and creds.expired and creds.refresh_token:
            creds.refresh(Request())
        else:
            flow = InstalledAppFlow.from_client_secrets_file(str(SECRET_FILE), SCOPES)
            creds = flow.run_local_server(port=0)
        with open(TOKEN_FILE, "wb") as f:
            pickle.dump(creds, f)
    return creds


def upload_video(video_path, title, description, tags, category_id="20"):
    """requests ベースの YouTube Data API v3 resumable upload"""
    print(f"📹 アップロード準備中: {video_path}")
    creds = get_credentials()

    tag_list = tags.split(",") if isinstance(tags, str) else tags

    metadata = {
        "snippet": {
            "title": title[:100],
            "description": description,
            "tags": tag_list,
            "categoryId": str(category_id),
            "defaultLanguage": "ja",
        },
        "status": {
            "privacyStatus": "public",
            "selfDeclaredMadeForKids": False,
        },
    }

    file_size = os.path.getsize(video_path)

    # Step 1: resumable upload を開始
    headers = {
        "Authorization": f"Bearer {creds.token}",
        "Content-Type": "application/json; charset=UTF-8",
        "X-Upload-Content-Length": str(file_size),
        "X-Upload-Content-Type": "video/mp4",
    }
    init_resp = req_lib.post(
        "https://www.googleapis.com/upload/youtube/v3/videos?uploadType=resumable&part=snippet,status",
        headers=headers,
        data=json.dumps(metadata),
        timeout=30,
    )
    init_resp.raise_for_status()
    upload_url = init_resp.headers["Location"]
    print(f"  Upload URL取得OK")

    # Step 2: 動画データを送信
    with open(video_path, "rb") as f:
        upload_headers = {
            "Content-Type": "video/mp4",
            "Content-Length": str(file_size),
        }
        print(f"  アップロード中... ({file_size / 1024 / 1024:.1f} MB)")
        upload_resp = req_lib.put(upload_url, headers=upload_headers, data=f, timeout=300)
        upload_resp.raise_for_status()

    result = upload_resp.json()
    video_id = result["id"]
    print(f"✅ アップロード完了！")
    print(f"   動画ID: {video_id}")
    print(f"   URL: https://www.youtube.com/watch?v={video_id}")
    return video_id


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="YouTube Shorts アップロード")
    parser.add_argument("--video", required=True, help="動画ファイルパス")
    parser.add_argument("--title", required=True, help="タイトル（100文字以内）")
    parser.add_argument("--description", default="", help="説明文")
    parser.add_argument("--tags", default="ゲーム,shorts", help="タグ（カンマ区切り）")
    parser.add_argument("--category", default="20", help="カテゴリID（20=ゲーム）")
    args = parser.parse_args()

    if not Path(args.video).exists():
        print(f"❌ 動画ファイルが見つかりません: {args.video}")
        sys.exit(1)

    upload_video(args.video, args.title, args.description, args.tags, args.category)
