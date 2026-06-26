/* keylog.h — 原版 OpenTyrian instrumentation：逐幀記錄輸入 + 截圖。
 * 對照基準用：與 C# 移植版比對同輸入序列下的畫面。
 *
 * 每幀記錄兩種輸入（選單與關卡讀的東西不同，兩者都要）：
 *   H: 該幀按住的鍵（keysactive 快照）—— 關卡移動/開火用
 *   Q: 該幀進入鍵盤佇列的 KeyDown 事件（含 SDL 鍵盤重複）—— 選單導航用
 *
 * keylog_keydown() 由 instr/keyboard.c 的 SDL_KEYDOWN 處理呼叫（緩衝當幀事件）。
 * keylog_frame()   由 instr/video.c   的 JE_showVGA() 每幀呼叫（快照 H + 沖出 Q + 截圖）。
 *
 * 以環境變數 KEYLOG=1 啟用；KEYLOG_DIR 指定輸出資料夾（預設 keylog）。 */
#ifndef KEYLOG_H
#define KEYLOG_H

void keylog_keydown(int sym, int scancode, int mod); /* 緩衝一個 KeyDown（含 repeat） */
void keylog_frame(void);                             /* 每幀：快照 + 記錄 + 截圖 */

#endif
