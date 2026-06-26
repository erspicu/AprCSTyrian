/* keylog.h — 原版 OpenTyrian instrumentation：逐幀記錄鍵盤輸入 + 截圖。
 * 對照基準用：與 C# 移植版比對同輸入序列下的畫面。
 * 由 instr/video.c 的 JE_showVGA() 每幀呼叫 keylog_frame()。
 * 以環境變數 KEYLOG=1 啟用；KEYLOG_DIR 指定輸出資料夾（預設 keylog）。 */
#ifndef KEYLOG_H
#define KEYLOG_H

void keylog_frame(void);

#endif
