namespace AprCSTyrian.Core;

/// <summary>
/// varz.c 的 const 資料表與目前已移植函式所需的少數 scalar 全域。
/// 其餘 ~100 個 varz 全域隨各自的消費函式（mainint/tyrian2/shots…）移植時陸續補入。
/// </summary>
internal static partial class Varz
{
    // === const 資料表（對應 varz.c 開頭）===
    public static readonly byte[] SANextShip = { 3, 9, 6, 2, 5, 1, 4, 3, 7 }; // [0..SA+1]
    public static readonly ushort[] SASpecialWeapon = { 7, 8, 9, 10, 11, 12, 13 };
    public static readonly ushort[] SASpecialWeaponB = { 37, 6, 15, 40, 16, 14, 41 };
    public static readonly byte[] SAShip = { 3, 1, 5, 10, 2, 11, 12 };
    public static readonly ushort[,] SAWeapon = /* [SA][5] */
    {   /*  R   Bl  Bk  G   P */
        {  9, 31, 32, 33, 34 }, // Stealth Ship
        { 19,  8, 22, 41, 34 }, // StormWind
        { 27,  5, 20, 42, 31 }, // Techno
        { 15,  3, 28, 22, 12 }, // Enemy
        { 23, 35, 25, 14,  6 }, // Weird
        {  2,  5, 21,  4,  7 }, // Unknown
        { 40, 38, 37, 41, 36 }, // NortShip Z
    };

    public static readonly byte[] specialArcadeWeapon = /* [PORT_NUM] */
    {
        17,17,18,0,0,0,10,0,0,0,0,0,44,0,10,0,19,0,0,0,0,0,0,0,0,0,
        0,0,0,0,45,0,0,0,0,0,0,0,0,0,0,0,
    };

    public static readonly byte[,,] optionSelect = /* [16][3][2] */
    {   /*  MAIN    OPT    FRONT */
        { { 0, 0}, { 0, 0}, { 0, 0} },
        { { 1, 1}, {16,16}, {30,30} }, // Single Shot
        { { 2, 2}, {29,29}, {29,20} }, // Dual Shot
        { { 3, 3}, {21,21}, {12, 0} }, // Charge Cannon
        { { 4, 4}, {18,18}, {16,23} }, // Vulcan
        { { 0, 0}, { 0, 0}, { 0, 0} },
        { { 6, 6}, {29,16}, { 0,22} }, // Super Missile
        { { 7, 7}, {19,19}, {19,28} }, // Atom Bomb
        { { 0, 0}, { 0, 0}, { 0, 0} },
        { { 0, 0}, { 0, 0}, { 0, 0} },
        { {10,10}, {21,21}, {21,27} }, // Mini Missile
        { { 0, 0}, { 0, 0}, { 0, 0} },
        { { 0, 0}, { 0, 0}, { 0, 0} },
        { {13,13}, {17,17}, {13,26} }, // MicroBomb
        { { 0, 0}, { 0, 0}, { 0, 0} },
        { {15,15}, {15,16}, {15,16} }, // Post-It
    };

    public static readonly ushort[] PGR = /* [21] */
    {
        4,
        1, 2, 3,
        41 - 21, 57 - 21, 73 - 21, 89 - 21, 105 - 21,
        121 - 21, 137 - 21, 153 - 21,
        151, 151, 151, 151, 73 - 21, 73 - 21, 1, 2, 4,
    };
    public static readonly byte[] PAni = { 1, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1 };

    public static readonly ushort[] linkGunWeapons = /* [38] */
    {
        0,0,0,0,0,0,0,0,444,445,446,447,0,448,449,0,0,0,0,0,450,451,0,506,0,564,
        445,446,447,448,449,445,446,447,448,449,450,451,
    };
    public static readonly ushort[] chargeGunWeapons = /* [38] */
    {
        0,0,0,0,0,0,0,0,476,458,464,482,0,488,470,0,0,0,0,0,494,500,0,528,0,558,
        458,458,458,458,458,458,458,458,458,458,458,458,
    };
    public static readonly byte[] randomEnemyLaunchSounds = { 13, 6, 26 };

    public static readonly byte[,] keyboardCombos = /* [26][8] */
    {
        { 2, 1,   2,   5, 137,   0, 0, 0 },
        { 4, 3,   2,   5, 138,   0, 0, 0 },
        { 3, 4,   6, 139,   0,   0, 0, 0 },
        { 2, 5, 142,   0,   0,   0, 0, 0 },
        { 6, 2,   6, 143,   0,   0, 0, 0 },
        { 6, 7,   5,   8,   6,   7, 5, 112 },
        { 7, 8, 101,   0,   0,   0, 0, 0 },
        { 1, 7,   6, 146,   0,   0, 0, 0 },
        { 8, 6,   7,   1, 120,   0, 0, 0 },
        { 3, 6,   8,   5, 121,   0, 0, 0 },
        { 1, 2,   7,   8, 119,   0, 0, 0 },
        { 3, 4,   3,   6, 123,   0, 0, 0 },
        { 6, 7,   5,   8, 124,   0, 0, 0 },
        { 1, 6, 125,   0,   0,   0, 0, 0 },
        { 9, 5, 126,   0,   0,   0, 0, 0 },
        { 1, 7, 127,   0,   0,   0, 0, 0 },
        { 1, 8, 128,   0,   0,   0, 0, 0 },
        { 9, 7, 129,   0,   0,   0, 0, 0 },
        { 9, 8, 130,   0,   0,   0, 0, 0 },
        { 4, 2,   3,   5, 131,   0, 0, 0 },
        { 3, 1,   2,   8, 132,   0, 0, 0 },
        { 2, 4,   5, 133,   0,   0, 0, 0 },
        { 3, 4,   2,   8, 134,   0, 0, 0 },
        { 1, 4,   6, 135,   0,   0, 0, 0 },
        { 1, 3,   6, 137,   0,   0, 0, 0 },
        { 1, 4,   3,   4,   7, 136, 0, 0 },
    };

    public static readonly byte[] shipCombosB = { 15,16,17,18,19,20,21,22,23,24, 7, 8, 5,25,14, 4, 6, 3, 9, 2,26 };
    public static readonly byte[] superTyrianSpecials = { 1, 2, 4, 5 };

    public static readonly byte[,] shipCombos = /* [14][3] */
    {
        { 5, 4, 7}, { 1, 2, 0}, {14, 4, 0}, { 4, 5, 0}, { 6, 5, 0}, { 7, 8, 0}, { 7, 9, 0},
        {10, 3, 5}, { 5, 8, 9}, { 1, 3, 0}, { 7,16,17}, { 2,11,12}, { 3, 8,10}, { 0, 0, 0},
    };

    // 對應 varz.h 的 static const hud_sidekick_y[2][2]
    public static readonly int[,] hud_sidekick_y =
    {
        {  64,  82 }, // one player HUD
        { 108, 126 }, // two player HUD
    };

    // === 已移植函式所需之 scalar 全域（其餘隨函式陸續補入；部分由遊戲邏輯指派）===
#pragma warning disable CS0649
    public const uint shadowYDist = 10;
    public static uint last_superpixel;
    public static byte temp, temp2, temp3;
    public static ushort tempW;
    public static int b;
    public static ushort levelEnd;
    public static bool levelTimer;
    public static ushort x, y;
    public static readonly byte[] soundQueue = new byte[8]; // [0..7]
    public static bool play_demo, record_demo, stopped_demo;
    public static bool gameLoaded, firstGameOver, enemyStillExploding;
    public static bool moveTyrianLogoUp, skipStarShowVGA;
    public static bool loadDestruct;
#pragma warning restore CS0649
}
