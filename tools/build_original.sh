#!/usr/bin/env bash
# 用 clang 編譯原版 OpenTyrian（C 源碼）為 Win32 可執行檔，作為移植對照基準。
# 需求：clang（LLVM，windows-msvc target）、Visual Studio（提供 Windows SDK/MSVC libs）。
# 不需網路支援：不帶 -DWITH_NETWORK；network.c 仍編入（只提供 stub 全域變數，無 SDL2_net 依賴）。
set -e

SRC="C:/ai_project/AprCSTyrian/sources"
OUT="C:/ai_project/AprCSTyrian/Build_orig"
SDL="/c/ai_project/.net10/tool/SDL2"   # SDL2 開發包（include + lib/x64/SDL2.lib）

mkdir -p "$OUT"

clang "$SRC"/src/*.c \
  -DTARGET_WIN32 -DNDEBUG -DSDL_MAIN_HANDLED \
  -DTYRIAN_DIR='"."' \
  -DOPENTYRIAN_VERSION='"opentyrian-classic"' \
  -I"$SDL/include" \
  -Wno-everything \
  "$SDL/lib/x64/SDL2.lib" "$SDL/lib/x64/SDL2main.lib" \
  -o "$OUT/opentyrian.exe"

cp -f "$SDL/lib/x64"/../../../*/SDL2.dll "$OUT/" 2>/dev/null || cp -f "C:/ai_project/AprCSTyrian/Build/SDL2.dll" "$OUT/"
echo "Built: $OUT/opentyrian.exe"
echo "Run:   $OUT/run.bat   (data dir -> ../Build/data)"
