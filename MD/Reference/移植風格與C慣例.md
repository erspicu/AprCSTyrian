# 移植風格與 C 慣例

目標：**忠實的語法轉換**，把 C 程式碼搬成可編譯的 C#，而非重新設計物件架構。

## 模組對應
- 每個 `sources/src/xxx.c` → `cs_ported/Core/Xxx.cs`，內容為 `internal static class Xxx`。
- C 全域變數 → `static` 欄位；C 函式 → `static` 方法。
- **保留原始識別字**（`mt_srand`、`JE_loadPic`、`JE_byte`…）以利逐行對照。
  （刻意違反 C# PascalCase 命名慣例，換取移植可追溯性。）

## 型別對應（Pascal/C → C#）
`JE_longint→int`、`JE_integer→short`、`JE_shortint→sbyte`、`JE_word→ushort`、
`JE_byte→byte`、`JE_boolean→bool`、`JE_real→float`、`char→byte`（位元組字串）、
`unsigned long→uint`（原始碼在 32-bit 語意下使用；MT 等須維持 32-bit 環繞）。

## 記憶體管理（重點：不可 leak）
- **盡量用非託管記憶體/指標**對應 C 的 `static` 陣列與指標運算，透過 `CMem` 包裝：
  - `CMem.malloc/calloc/realloc/free`：對應 C，內部用 `NativeMemory.*`。
  - DEBUG 下 `CMem` 追蹤所有配置，`CMem.AssertNoLeaks()` 可在結束時驗證全部已釋放。
- **配置策略**：
  - 全域固定表/緩衝（對應 C `static` 陣列）→ 啟動時配置一次，程式結束統一釋放。
  - 區域暫存 → `stackalloc`。
  - 不需指標運算之處，允許用託管 `T[]`（仍是扁平陣列、C 風格），由 GC 管理、零 leak。
- `unsafe` 自由使用；指標型別直接對應（`unsigned long *p` → `uint* p`）。

## 其他
- 巨集（COUNTOF/MIN/MAX）→ `Opentyr` 內的 `static` 方法或泛型 helper。
- 位元運算/溢位：C# 預設 `unchecked`，與 C 環繞一致（必要處明確標 `unchecked`）。
- 二進位讀寫務必 little-endian 並對齊型別寬度（存檔相容）。
