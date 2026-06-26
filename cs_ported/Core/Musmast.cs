namespace AprCSTyrian.Core;

/// <summary>對應 sources/src/musmast.h —— 歌曲編號常數（部分；其餘隨需要補入）。</summary>
internal static class Musmast
{
    public const int SONG_TITLE = 29;
    public const int SONG_MAPVIEW = 5; // 參考
    public const int SONG_GAMEOVER = 10; // musmast.h: SONG_GAMEOVER
    public const int DEFAULT_SONG_BUY = 2;

    public static byte songBuy; // 物品/商店畫面背景音樂
}
