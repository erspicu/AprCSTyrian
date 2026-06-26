namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/mouse.c —— 目前為最小橋接版本（完整滑鼠座標/按鍵待後續）。
/// </summary>
internal static class Mouse
{
    /// <summary>對應 mouse.c:mouseClearInput（暫行 no-op）。</summary>
    public static void mouseClearInput() { }
}
