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

    public static bool endLevel, reallyEndLevel, playerEndLevel, readyToEndLevel, quitRequested;
    public static ushort curLoc;
    public static byte astralDuration;

    // 事件/背景狀態
    public static ushort explodeMove, returnLoc;
    public static bool stopBackgrounds, enemiesActive, forceEvents, background3x1, background3x1b;
    public static byte stopBackgroundNum;
    public static ushort totalEnemy; // 摧毀率計數
    public static ushort enemyOnScreen, lastEnemyOnScreen;
    public static ushort enemyKilled, superEnemy254Jump;
    public static bool enemyContinualDamage;
    public static readonly bool[] globalFlags = new bool[10];
    public static bool smallEnemyAdjust;
    public static int explosionFollowAmountX, explosionFollowAmountY;

    // 事件系統用全域（對應 varz.h / musmast.h）
    public static readonly byte[] newPL = new byte[10];   // varz.h: JE_byte newPL[10]
    public static bool returnActive;                       // varz.h: JE_boolean returnActive
    public static ushort galagaShotFreq;                   // varz.h: JE_word galagaShotFreq
    public static byte damageRate;                         // varz.h: JE_byte damageRate
    public static ushort levelTimerJumpTo;                 // varz.h: JE_word levelTimerJumpTo
    public static bool randomExplosions;                   // varz.h: JE_boolean randomExplosions
    public static bool allPlayersGone;                     // varz.h: JE_boolean allPlayersGone
    public static bool musicFade;                          // musmast.h: JE_boolean musicFade
#pragma warning restore CS0649
}
