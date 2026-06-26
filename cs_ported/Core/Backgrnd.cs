namespace AprCSTyrian.Core;

/// <summary>
/// 對應 sources/src/backgrnd.c —— 遊戲內三層捲動背景。
/// 目前先放入背景捲動座標全域（mapX/mapY…），繪製函式待後續。
/// </summary>
internal static unsafe partial class Backgrnd
{
#pragma warning disable CS0649 // 由遊戲主迴圈/JE_loadMap 指派
    public static ushort mapX, mapY, mapX2, mapX3, mapY2, mapY3;
#pragma warning restore CS0649
}
