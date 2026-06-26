// Pascal/C 型別別名（對應 sources/src/opentyr.h 的 JE_* typedef）。
// 以 global using 讓全 Core 的移植碼可直接沿用原始型別名，維持逐行對照。
global using JE_longint = System.Int32;
global using JE_integer = System.Int16;
global using JE_shortint = System.SByte;
global using JE_word = System.UInt16;
global using JE_byte = System.Byte;
global using JE_boolean = System.Boolean;
global using JE_char = System.Byte;   // C 的 char 為位元組字串
global using JE_real = System.Single;
