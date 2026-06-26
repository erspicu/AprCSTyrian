namespace AprCSTyrian.Core;

/// <summary>移植 sources/src/lvlmast.h —— 物品/敵人等資料表的維度常數。</summary>
internal static class Lvlmast
{
    public const int EVENT_MAXIMUM = 2500;

    public const int WEAP_NUM = 780;
    public const int PORT_NUM = 42;
    public const int ARMOR_NUM = 4;
    public const int POWER_NUM = 6;
    public const int ENGINE_NUM = 6;
    public const int OPTION_NUM = 30;
    public const int SHIP_NUM = 13;
    public const int SHIELD_NUM = 10;
    public const int SPECIAL_NUM = 46;

    public const int ENEMY_NUM = 850;

    /// <summary>對應 lvlmast.c:shapeFile[34] —— 敵人 shape 檔對應字元（[25] 原碼註記應為 '&'）。</summary>
    public static readonly char[] shapeFile =
    {
        '2', '4', '7', '8', 'A', 'B', 'C', 'D', 'E', 'F',
        'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
        'Q', 'R', 'S', 'T', 'U', '5', '#', 'V', '0', '@',
        '3', '^', '5', '9',
    };
}
