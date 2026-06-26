# 原版 OpenTyrian — Instrumentation（KEY LOG + 截圖）

移植**對照基準**用：在原版 OpenTyrian 逐幀記錄「鍵盤輸入 + 當下畫面」，
之後可拿同一組輸入序列在 C# 移植版重播，逐幀比對畫面是否一致。

`sources/` 為唯讀基準，**不修改**；本目錄是 instrumented 副本：
- `keylog.c` / `keylog.h` —— 記錄機制（新增）。
- `video.c` —— 從 `sources/src/video.c` 複製，僅在 `JE_showVGA()` 末尾加一行 `keylog_frame();`。

## 機制

- **frame 編號** = `JE_showVGA()` 的絕對呼叫次數（= 已呈現幀數）。每幀都 +1。
- **有輸入才記錄**（沒按鍵的幀只前進計數器，零資料量）：
  - `keylog.txt` 追加一行：`<frame>\t<scancode:name>[,<scancode:name>...]`
  - 同幀存截圖 `frame_<frame>.bmp`（320×200 真彩，由 VGAScreen 索引色 + `rgb_palette` 還原）。

## 環境變數

| 變數 | 作用 |
|---|---|
| `KEYLOG=1` | 啟用（未設則完全 no-op，行為同原版） |
| `KEYLOG_DIR=<dir>` | 輸出資料夾（預設 `keylog`） |
| `KEYLOG_NOSHOT=1` | 只記 keylog.txt、不存截圖 |
| `KEYLOG_FORCE=1` | 每幀都捕捉（不限有無輸入）——測試 / 定頻參考用，資料量大 |

## 建置與執行

```bash
bash tools/build_keylog.sh          # 產出 Build_orig/opentyrian_keylog.exe
```
```
Build_orig\run_keylog.bat     # KEYLOG=1，資料 -> Build\data，截圖+keylog 輸出到 temp\orig\
```

## 對照流程（與 C# 移植版比對）

1. 原版錄製：`Build_orig\run_keylog.bat`，玩一段（按鍵的幀才會記）→ `temp\orig\`（keylog.txt + frame_*.bmp）。
2. C# 重播：`Build\run_replay.bat` → 讀 `temp\orig` 的 log 注入輸入、於相同 frame 截圖 → `temp\cs\`。
3. 比對：`python tools\compare_frames.py` → 列出最分歧的幀（= 移植還沒對上的地方）+ 並排對照圖到 `temp\diff\`。

實測：開場「ECLIPSE」logo / C# 標題畫面截圖顏色正確（調色盤還原無誤）；REPLAY 模式於 log 記錄的相同 frame 精準截圖。
