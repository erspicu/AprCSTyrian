namespace AprCSTyrian.Core;

/// <summary>
/// 對應 sources/src/menus.c 的部分全域 —— **目前僅放入 helptext 載入所需的文字表**；
/// 完整選單邏輯待後續移植。
/// </summary>
internal static partial class Menus
{
    public static readonly string[] episode_name = new string[6];
    public static readonly string[] difficulty_name = new string[7];
    public static readonly string[] gameplay_name = new string[5];
}
