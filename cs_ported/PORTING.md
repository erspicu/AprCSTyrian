# 移植進度 (PORTING)

OpenTyrian (C, `../sources/`) → .NET 10 / C# 移植追蹤。

## 架構
- `Core/`（class lib，零 SDL）：Ports 介面 + 遊戲邏輯
- `App/`（WinExe，參考 Core）：SDL Adapters + `Program.cs` 組合根
- 依賴單向 `App → Core`；換平台只改 Adapter。
- SDL 繫結：`Sayers.SDL2.Core`（SDL2-CS 的 NuGet 包裝，內含原生 SDL2.dll）。

## 已完成
- [x] **版控**：git 初始化 + 推送至 GitHub `erspicu/AprCSTyrian`（private）；`.gitignore` 排除
      `Build`/`private`/`temp`/`bin`/`obj` 與 VS C# 慣例檔；`sources/` 以對照參考提交（移除巢狀 .git）
- [x] **Phase A 基礎**：`CTypes`(JE_* 別名)、`CMem`(malloc/calloc/realloc/free + DEBUG leak 追蹤)、
      `Opentyr`(常數/巨集/版本)、`MtRand`(mtrand.c 指標式 MT，非託管 state)、
      `Globals`(平台/路徑橋接)、`TyrianHaltException`+`Varz.JE_tyrianHalt`
- [x] **Phase B 部分**：`CFile`(file.c：data_dir/dir_fopen/fread・fwrite_die/typed・scalar 讀寫，FILE*→Stream)、
      `MemIO`(memreader.c/memwriter.c：MemReader/MemWriter 指標式 LE)
- [x] solution + 雙專案骨架（`AprCSTyrian.slnx` / `Core` / `App`）
- [x] Core Ports：`IVideoBackend` `IAudioBackend` `IInputBackend` `IClock` `IFileSystem` `IGamePlatform`
- [x] Core 型別：`Color`(8-bit RGB, `FromVga`) / `GameKey` / `VgaScreen`(320×200)
- [x] SDL Adapters：`SdlVideo`(window+renderer+streaming texture，邏輯尺寸整數縮放) /
      `SdlInput`(scancode→GameKey、按住+邊緣) / `SdlClock` / `SdlAudio`(callback 模式) /
      `PhysicalFileSystem` / `SdlPlatform`(SDL 生命週期聚合)
- [x] `Program.cs` 組合根（WinExe 無 console，crash 寫 `crash.log`）
- [x] **里程碑 0**：`TyrianGame` 顯示動態測試畫面，視窗開啟、迴圈執行、ESC/X 關閉，建置→`Build/` 並實機跑通

## 待移植（依原始 C 模組，順序待定）
- [ ] 資料檔載入：`file.c` / `lvllib.c` / `pcxload.c` / `picload.c`（讀 Tyrian 2.1 資料）
- [ ] 調色盤與繪圖：`palette.c` / `vga256d.c` / `video.c` / `sprite.c` / `font.c` `fonthand.c`
- [ ] 亂數：`mtrand.c`（需與原版序列一致）
- [ ] 設定/存檔：`config.c` / `config_file.c`（二進位，注意型別寬度與 little-endian）
- [ ] 音訊：`loudness.c`(混音) / `opl.c`(FM 模擬) / `lds_play.c` / `nortsong.c` / `jukebox.c`
- [ ] 選單與主流程：`opentyr.c`(進入點) / `setup`/`menus.c` / `game_menu.c` / `mainint.c`
- [ ] 遊戲核心：`tyrian2.c` / `player.c` / `shots.c` / `destruct.c` / `backgrnd.c` / `starlib.c`
- [ ] 輸入：`keyboard.c` / `joystick.c` / `mouse.c`
- [~] 網路 `network.c`：**不移植**（決議跳過）

## 備註
- 遊戲需 Tyrian 2.1 freeware 資料檔：https://camanis.net/tyrian/tyrian21.zip
  （放入 `Build/data/` 或以執行參數指定資料根目錄）
- 建置指令：`dotnet build App/App.csproj -c Release -o C:\ai_project\AprCSTyrian\Build`
  （建置 App 即會帶入 Core 與原生 DLL；勿對 solution 用 `-o`，會有 NETSDK1194 警告）
