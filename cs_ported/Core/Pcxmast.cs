namespace AprCSTyrian.Core;

/// <summary>移植 sources/src/pcxmast.c —— tyrian.pic 各圖的調色盤索引與位置表。</summary>
internal static class Pcxmast
{
    public const int PCX_NUM = 13;

    public static readonly byte[] pcxpal = /* [1..PCXnum] */
        { 0, 7, 5, 8, 10, 5, 18, 19, 19, 20, 21, 22, 5 };

    public static readonly byte[] facepal = /* [1..12] */
        { 1, 2, 3, 4, 6, 9, 11, 12, 16, 13, 14, 15 };

    public static readonly int[] pcxpos = new int[PCX_NUM + 1];
}
