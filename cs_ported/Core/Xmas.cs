namespace AprCSTyrian.Core;

/// <summary>
/// 對應 sources/src/xmas.c —— **目前為最小占位**：xmas 旗標 + 偵測/提示 stub。
/// 完整聖誕模式（日期偵測、提示畫面）待後續。
/// </summary>
internal static class Xmas
{
    public static bool xmas;

    public static bool xmas_time() => false; // 完整日期偵測待後續
    public static bool xmas_prompt() => false;
}
