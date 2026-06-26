namespace AprCSTyrian.Core;

/// <summary>tyrian2.c 的關卡/事件相關全域（供 JE_loadMap 及遊戲主迴圈使用）。</summary>
internal static unsafe partial class Tyrian2
{
    public const int EVENT_MAXIMUM = 2500;

#pragma warning disable CS0649 // 由 JE_loadMap / 遊戲主迴圈指派
    public static bool loadLevelOk;
    public static bool jumpSection;
    public static bool useLastBank;
    public static bool haltGame;
    public static bool bonusLevelCurrent, normalBonusLevelCurrent;
    public static bool doNotSaveBackup;

    public static readonly JE_EventRecType[] eventRec = new JE_EventRecType[EVENT_MAXIMUM];
    public static ushort maxEvent, eventLoc;

    public static ushort levelEnemyMax, levelEnemyFrequency;
    public static readonly ushort[] levelEnemy = new ushort[40];

    public static readonly byte[,] itemAvail = new byte[9, 10];
    public static readonly byte[] itemAvailMax = new byte[9];

    public static byte lvlFileNum, levelSong;

    public static ushort mapOrigin, mapPNum;
    public static readonly byte[] mapPlanet = new byte[5];
    public static readonly byte[] mapSection = new byte[5];

    public static ushort levelTimerCountdown;
#pragma warning restore CS0649
}
