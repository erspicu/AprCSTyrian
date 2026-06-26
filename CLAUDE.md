# AprCSTyrian — 專案守則

將 **OpenTyrian**（原始 C，源自 Turbo Pascal 的 DOS 遊戲 Tyrian）移植為 **.NET 10 / C#**，
保持遊戲行為與原版一致。

## 核心守則（務必遵守）
1. **目錄職責**：`sources/` 原始 C 碼 **唯讀禁改**（僅供對照）；`cs_ported/` 放 C# 移植碼；
   `Build/` 為建置產出（可重建、不手改）；`MD/` 放文件；`private/` 不納入移植。
2. **架構鐵則**：依賴方向單向 **`App` → `Core`**；**`Core` 絕不引用 SDL** 或任何平台組件。
   平台/多媒體只能進 `App` 的 Adapter 層。（詳見 `MD/Design/架構設計.md`）
3. **逐模組對照移植**：以原始 `.c/.h` 為準，C# 端維持相近檔名/模組邊界，方便比對。
4. **行為一致優先**：先求與原版等價（整數溢位、定點運算、亂數序列），再談 C# 慣用重構。
5. **型別寬度對應**：Pascal 型別 `JE_*` 需對應正確 C# 寬度（見 `MD/Reference/原始碼結構.md`）。
6. **二進位相容**：設定/存檔/關卡為二進位，務必對齊型別寬度與 little-endian，避免不相容。
7. **指標/記憶體**：C 指標、buffer 優先以 `Span<T>`/`Memory<T>`/陣列表達，必要時才用 `unsafe`。
8. **網路不移植**：跳過 `network.c` / SDL2_net。

## 文件與筆記（MD 目錄）
- `MD/` 下依性質分子目錄存放各類 `*.md`，需要時再新增子目錄。
- `MD/Note/` = **重要筆記**。檔名格式 `年月日時分-主題.md`（12 位時間戳，
  例 `202606261401-某某主題.md`；時間戳用 `date "+%Y%m%d%H%M"`）。

## 索引
- 架構設計：`MD/Design/架構設計.md`
- 環境與技術基準：`MD/Reference/環境與技術基準.md`
- 原始碼結構與 Pascal 型別對應：`MD/Reference/原始碼結構.md`
- 開發與建置指令：`MD/Reference/開發與建置指令.md`
- 移植進度：`cs_ported/PORTING.md`
- 決策與里程碑記錄：`MD/Note/`
