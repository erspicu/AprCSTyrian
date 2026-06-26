#!/usr/bin/env bash
# 建置 instrumented 版原版 OpenTyrian（逐幀 KEY LOG + 截圖），作為移植對照基準。
# 用 sources/src/*.c（除 video.c）+ instr/video.c（含 keylog 鉤點）+ instr/keylog.c。
# sources/ 保持唯讀不改。詳見 instr/README.md。
set -e

ROOT="C:/ai_project/AprCSTyrian"
SDL="/c/ai_project/.net10/tool/SDL2"
OUT="$ROOT/Build_orig"
mkdir -p "$OUT"

SRCS=$(ls "$ROOT"/sources/src/*.c | grep -v '/video.c$')

clang $SRCS "$ROOT/instr/video.c" "$ROOT/instr/keylog.c" \
  -DTARGET_WIN32 -DNDEBUG -DSDL_MAIN_HANDLED \
  -DTYRIAN_DIR='"."' -DOPENTYRIAN_VERSION='"opentyrian-keylog"' \
  -I"$ROOT/sources/src" -I"$ROOT/instr" -I"$SDL/include" \
  -Wno-everything \
  "$SDL/lib/x64/SDL2.lib" "$SDL/lib/x64/SDL2main.lib" \
  -o "$OUT/opentyrian_keylog.exe"

cp -f "$ROOT/Build/SDL2.dll" "$OUT/" 2>/dev/null || true
echo "Built: $OUT/opentyrian_keylog.exe"
echo "Run:   $OUT/run_keylog.bat   (KEYLOG=1 -> $OUT/keylog/)"
