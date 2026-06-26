namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/varz.c —— 遊戲全域變數與少數工具函式。
/// 目前僅放入早期被依賴的 <see cref="JE_tyrianHalt"/>；其餘全域將隨各模組移植陸續補入。
/// </summary>
internal static partial class Varz
{
    /// <summary>結束遊戲（對應 varz.c:JE_tyrianHalt）。以例外解開呼叫堆疊，由組合根清理資源。</summary>
    public static void JE_tyrianHalt(byte code)
    {
        // 原始會在此釋放 audio/video/shape tables/sound samples。
        // 這些模組尚未全部移植；已移植者於此釋放，其餘待補。
        MtRand.Shutdown();

        throw new TyrianHaltException(code);
    }
}
