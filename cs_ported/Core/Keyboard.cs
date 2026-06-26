using AprCSTyrian.Core.Ports;

namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/keyboard.c —— **目前為最小橋接版本**。
/// 原 keyboard.c 直接處理 SDL 事件並維護大量遊戲輸入全域（newkey/keysactive…）。
/// 為保持 Core 不依賴 SDL，完整版將於遊戲邏輯需要時改以中性事件佇列移植；
/// 此處先提供 fade/show 迴圈所需的 waitUntilElapsed 與清除輸入。
/// </summary>
internal static class Keyboard
{
    /// <summary>對應 keyboard.c:waitUntilElapsed（原會 service 事件後 delay 到 frame 結束）。</summary>
    public static void waitUntilElapsed()
    {
        Globals.Input.Poll();      // 泵入平台事件，維持視窗回應與 quit
        Nortsong.delayUntilElapsed();
    }

    /// <summary>對應 keyboard.c:keyboardClearInput（暫行：透過平台 Poll 消化既有輸入）。</summary>
    public static void keyboardClearInput()
    {
        Globals.Input.Poll();
    }

    /// <summary>是否要求離開（視窗 X / Alt+F4）。</summary>
    public static bool QuitRequested => Globals.Input.QuitRequested;

    /// <summary>暫行：Escape 是否按住。</summary>
    public static bool EscapePressed => Globals.Input.IsKeyDown(GameKey.Escape);
}
