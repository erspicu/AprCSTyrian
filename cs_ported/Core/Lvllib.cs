namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/lvllib.c —— **目前為最小占位**：關卡檔名/位置全域 + JE_analyzeLevel 占位。
/// 完整關卡資料分析/載入待後續（隨 tyrian2 主迴圈）。
/// </summary>
internal static class Lvllib
{
    public static readonly int[] lvlPos = new int[43]; // JE_LvlPosType
    public static readonly byte[] levelFile = new byte[13];
#pragma warning disable CS0649 // 由 JE_analyzeLevel（待移植）指派
    public static ushort lvlNum;
#pragma warning restore CS0649

    public static void JE_analyzeLevel() { /* 關卡結構分析待後續移植 */ }
}
