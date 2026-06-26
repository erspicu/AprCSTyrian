namespace AprCSTyrian.Core;

/// <summary>對應 sources/src/musmast.h —— 歌曲編號常數（部分；其餘隨需要補入）。</summary>
internal static class Musmast
{
    public const int MUSIC_NUM = 41; // musmast.h: #define MUSIC_NUM 41

    public const int SONG_TITLE = 29;
    public const int SONG_MAPVIEW = 5; // 參考
    public const int SONG_GAMEOVER = 10; // musmast.h: SONG_GAMEOVER
    public const int DEFAULT_SONG_BUY = 2;

    public static byte songBuy; // 物品/商店畫面背景音樂

    /// <summary>對應 musmast.c:musicTitle[MUSIC_NUM][48]（曲名字串，逐項照抄）。</summary>
    public static readonly string[] musicTitle =
    {
        "Asteroid Dance Part 2",
        "Asteroid Dance Part 1",
        "Buy/Sell Music",
        "CAMANIS",
        "CAMANISE",
        "Deli Shop Quartet",
        "Deli Shop Quartet No. 2",
        "Ending Number 1",
        "Ending Number 2",
        "End of Level",
        "Game Over Solo",
        "Gryphons of the West",
        "Somebody pick up the Gryphone",
        "Gyges, Will You Please Help Me?",
        "I speak Gygese",
        "Halloween Ramble",
        "Tunneling Trolls",
        "Tyrian, The Level",
        "The MusicMan",
        "The Navigator",
        "Come Back to Me, Savara",
        "Come Back again to Savara",
        "Space Journey 1",
        "Space Journey 2",
        "The final edge",
        "START5",
        "Parlance",
        "Torm - The Gathering",
        "TRANSON",
        "Tyrian: The Song",
        "ZANAC3",
        "ZANACS",
        "Return me to Savara",
        "High Score Table",
        "One Mustn't Fall",
        "Sarah's Song",
        "A Field for Mag",
        "Rock Garden",
        "Quest for Peace",
        "Composition in Q",
        "BEER",
    };
}
