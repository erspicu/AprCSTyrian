#!/usr/bin/env bash
#
# AprCSTyrian 發佈打包工作流
# ---------------------------------------------------------------------------
# 用法: bash tools/release.sh
#
# 產出:
#   1) AprCSTyrianRelease/                         ← 可直接執行的資料夾(點 AprCSTyrian.exe 就能玩)
#   2) AprCSTyrianRelease-YYYYMMDD-<shorthash>.zip ← 發佈壓縮檔
#   3) GitHub release                              ← tag = YYYYMMDD-<shorthash>
#
# 版號機制: 日期(YYYYMMDD) + git 短 commit hash。例: AprCSTyrianRelease-20260627-7b91d1a.zip
# 內容: self-contained win-x64(含 .NET runtime + SDL2.dll,無需安裝 .NET) + Tyrian 2.1 freeware 資料
# ---------------------------------------------------------------------------
set -euo pipefail

ROOT="$(git -C "$(dirname "$0")" rev-parse --show-toplevel)"
cd "$ROOT"

DATE="$(date +%Y%m%d)"
HASH="$(git rev-parse --short HEAD)"
VER="${DATE}-${HASH}"
RELDIR="$ROOT/AprCSTyrianRelease"
ZIP="$ROOT/AprCSTyrianRelease-${VER}.zip"

echo "===================================================================="
echo "  AprCSTyrian 發佈   版本 ${VER}"
echo "===================================================================="

# 確認工作區無未提交變更(發佈版號要對得上 commit)
if [ -n "$(git status --porcelain)" ]; then
    echo "  ⚠  警告: 工作區有未提交的變更，發佈的 commit hash(${HASH}) 不含這些改動。"
    echo "     建議先 commit 再發佈。是否仍要繼續? (Ctrl-C 取消，Enter 繼續)"
    read -r _ || true
fi

# --- 1/5 自包含發佈 ---
echo "[1/5] dotnet publish (self-contained win-x64)..."
rm -rf "$RELDIR"
dotnet publish cs_ported/App/App.csproj -c Release -r win-x64 --self-contained true \
    -p:PlatformTarget=x64 -p:KeyLogOff=true \
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:DebugType=none -p:DebugSymbols=false \
    -o "$RELDIR" >/dev/null
# 註:
#  -p:KeyLogOff=true                          → 關閉 KeyLog 除錯機制(發佈版死碼消除)
#  -p:PublishSingleFile=true                  → 所有受管理 DLL + .NET runtime 併入單一 AprCSTyrian.exe
#  -p:IncludeNativeLibrariesForSelfExtract    → 連原生 SDL2.dll 也併入 exe(執行時自解壓載入)
#  App.csproj 的 TrimUnusedSdlNatives target  → 只保留 SDL2.dll,排除未用的 SDL2_image/mixer/ttf 及 lib*.dll
# 結果: 發佈資料夾只有「AprCSTyrian.exe + data/」,點 exe 即玩(免裝 .NET、免散佈一堆 DLL)。
echo "      → $RELDIR"

# --- 2/5 帶入遊戲資料(Tyrian 2.1 freeware) ---
echo "[2/5] 複製遊戲資料 (Build/data → AprCSTyrianRelease/data)..."
if [ ! -d "$ROOT/Build/data" ]; then
    echo "  ✗ 錯誤: $ROOT/Build/data 不存在(需 Tyrian 2.1 freeware 資料)。" >&2
    exit 1
fi
cp -r "$ROOT/Build/data" "$RELDIR/data"

# --- 3/5 版本資訊檔 ---
{
    echo "AprCSTyrian Release"
    echo "version: ${VER}"
    echo "date:    ${DATE}"
    echo "commit:  $(git rev-parse HEAD)"
} > "$RELDIR/VERSION.txt"

# --- 4/5 打包 zip(以 AprCSTyrianRelease/ 為頂層資料夾) ---
echo "[3/5] 打包 ${ZIP##*/}..."
rm -f "$ZIP"
powershell -NoProfile -Command "Compress-Archive -Path '$RELDIR' -DestinationPath '$ZIP' -Force"
echo "      → $ZIP  ($(du -h "$ZIP" | cut -f1))"

# --- 5/5 GitHub release ---
echo "[4/5] 建立 GitHub release (tag ${VER})..."
if gh release view "$VER" >/dev/null 2>&1; then
    echo "      (release ${VER} 已存在 → 先刪除重建)"
    gh release delete "$VER" --yes --cleanup-tag >/dev/null 2>&1 || true
fi
NOTES=$(cat <<EOF
AprCSTyrian 發佈 ${VER}

- 自包含 win-x64（已含 .NET runtime + SDL2.dll，**無需安裝 .NET**）
- 已內含 Tyrian 2.1 freeware 遊戲資料
- 解壓後執行 \`AprCSTyrian.exe\` 即可遊玩

commit: $(git rev-parse --short HEAD)
EOF
)
gh release create "$VER" "$ZIP" --title "AprCSTyrian ${VER}" --notes "$NOTES"

echo "[5/5] 完成 ✓"
echo "  可玩資料夾 : $RELDIR  (點 AprCSTyrian.exe)"
echo "  發佈壓縮檔 : $ZIP"
echo "  GitHub     : $(gh release view "$VER" --json url -q .url 2>/dev/null || echo '(見 gh release list)')"
