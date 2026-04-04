#!/bin/bash
# Read IMPROVEMENTS.md, find next uncompleted item, return it
FILE="/home/ageha/.openclaw/workspace/todaysminigame-games/wip/dungeon-merchant/IMPROVEMENTS.md"
NEXT=$(grep -n '^\d*\. \[ \]' "$FILE" | head -1)
if [ -z "$NEXT" ]; then
  echo "ALL_DONE"
else
  echo "$NEXT"
fi
