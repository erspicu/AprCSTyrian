# AprCSTyrian

A faithful **.NET 10 / C#** port of [OpenTyrian](https://github.com/opentyrian/opentyrian) — the open-source engine for the classic DOS vertical shoot-'em-up **Tyrian** (1995) — rebuilt with a clean **Ports & Adapters (Hexagonal)** architecture.

> 🇬🇧 English below · 🇹🇼 中文在下半部

---

## English

### What is this?

AprCSTyrian translates the original OpenTyrian C source (itself derived from the Turbo Pascal DOS game) into modern **.NET 10 / C#**, line-by-line and faithfully, so the gameplay matches the original (integer overflow, fixed-point math, RNG sequence, binary save/level formats all preserved).

The codebase is split into two projects:

- **`Core`** — pure game logic, a class library with **zero SDL / platform dependencies**. It only defines *Ports* (interfaces).
- **`App`** — the WinExe entry point and the **SDL2 adapter** layer. The only project allowed to touch SDL.

Dependency flows one way: **`App → Core`**. Swapping the platform means rewriting only the adapters.

### Status

**The migration is complete.** Every part that could be faithfully translated has been ported and verified in-game, with **0% self-invented logic**:

- Core gameplay: event system, enemy AI (layered drawing + sub-ship launching), player movement/firing, the full main loop, collisions, special weapons, screen filters, gamma
- Shell: title screen, shop (`JE_itemScreen`), menus, save/load, high-score entry, cutscenes & episode flow, screen-transition animations, attract-mode **demo playback & recording**
- The **Destruct** bonus mini-game (all 59 functions), seasonal Christmas mode
- Skipped by design: networking (per project rules)

**Improvement phase** (post-migration enhancements, built to fit our architecture):

- 🎮 **Joystick / gamepad support** via Ports & Adapters (`IJoystickBackend` + SDL adapter), Core stays SDL-free
- ⚙️ **Setup menu** on the title screen (Sound / Jukebox / Done)
- 💾 **Settings persistence** to `opentyrian.cfg`: joystick assignments, key bindings, scaler choice
- 🖼️ **Scale3x display filter** (selectable None / Scale2x / Scale3x) for crisper, smoother upscaling
- 🖱️ Mouse is no longer locked to the window

### Build & Run

Requires the **.NET 10 SDK** and the **Tyrian 2.1 freeware data** (`tyrian21.zip`, released for free in 2004).

```bash
# Build
dotnet build cs_ported/App/App.csproj -c Release -o Build

# Put the Tyrian 2.1 data into Build/data/ , then run
./Build/AprCSTyrian.exe
```

### Download (ready to play)

Grab a packaged build from [**Releases**](https://github.com/erspicu/AprCSTyrian/releases) — it is self-contained (no .NET install needed) and already bundles the freeware data. Just unzip and run `AprCSTyrian.exe`.

### Credits & License

- Original game **Tyrian** © Eclipse Software / World Tree Games. Tyrian 2.1 data is **freeware**.
- Engine based on **OpenTyrian** (the OpenTyrian Development Team).
- As a derivative of OpenTyrian, this project is licensed under the **GNU GPL v2 or later**.

---

## 中文

### 這是什麼？

AprCSTyrian 把 OpenTyrian 的原始 C 碼（源自 DOS 經典直式射擊遊戲 **Tyrian**）忠實逐行移植成現代 **.NET 10 / C#**，力求行為與原版一致（整數溢位、定點運算、亂數序列、二進位存檔/關卡格式全部保留）。

專案分成兩塊：

- **`Core`** —— 純遊戲邏輯，類別庫，**完全不依賴 SDL／任何平台組件**，只定義 *Ports*（介面）。
- **`App`** —— WinExe 進入點與 **SDL2 配接器（Adapter）**層，是唯一可碰 SDL 的專案。

依賴方向單向 **`App → Core`**；換平台只需改寫 Adapter。

### 進度

**移植任務已完成。** 所有「能忠實照搬」的部分都已移植並逐一實機驗證，**自創邏輯 0%**：

- 核心 gameplay：事件系統、敵人 AI（分層繪製 + 發射子機）、玩家移動/射擊、完整主迴圈、碰撞、特殊武器、畫面濾鏡、gamma
- 外殼：標題畫面、商店（`JE_itemScreen`）、選單、存讀檔、高分輸入、過場與章節流程、過場平移動畫、待機 **demo 播放與錄製**
- **毀滅模式（Destruct）** bonus 迷你遊戲（全 59 函式）、聖誕季節模式
- 依守則略過：網路對戰

**改進階段**（移植完成後、依本專案架構做的功能強化）：

- 🎮 **搖桿支援**：以 Ports & Adapters 機制補上（`IJoystickBackend` + SDL 配接器），Core 維持 SDL-free
- ⚙️ **首頁 SETUP 選單**（Sound / Jukebox / Done）
- 💾 **設定持久化** 到 `opentyrian.cfg`：搖桿指派、按鍵綁定、放大濾鏡選擇
- 🖼️ **Scale3x 顯示濾鏡**（可選 None / Scale2x / Scale3x），畫面更細緻平滑
- 🖱️ 滑鼠不再被鎖在視窗內

### 建置與執行

需要 **.NET 10 SDK** 與 **Tyrian 2.1 freeware 資料**（`tyrian21.zip`，2004 年釋出為免費）。

```bash
# 建置
dotnet build cs_ported/App/App.csproj -c Release -o Build

# 把 Tyrian 2.1 資料放進 Build/data/ ，然後執行
./Build/AprCSTyrian.exe
```

### 下載即玩

到 [**Releases**](https://github.com/erspicu/AprCSTyrian/releases) 下載打包好的版本——自包含（**免裝 .NET**）且已內含 freeware 資料，解壓後執行 `AprCSTyrian.exe` 即可遊玩。

### 致謝與授權

- 原作 **Tyrian** © Eclipse Software / World Tree Games；Tyrian 2.1 資料為 **freeware**。
- 引擎基於 **OpenTyrian**（OpenTyrian 開發團隊）。
- 作為 OpenTyrian 的衍生作品，本專案採 **GNU GPL v2 或更新版本** 授權。
