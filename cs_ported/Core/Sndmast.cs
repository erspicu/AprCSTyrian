namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/sndmast.h —— 音效編號常數。
/// （soundTitle/windowTextSamples 資料表待音訊 Phase D 一併補入。）
/// </summary>
internal static class Sndmast
{
    public const int SFX_COUNT = 29;
    public const int VOICE_COUNT = 9;
    public const int SOUND_COUNT = SFX_COUNT + VOICE_COUNT;

    public const int S_NONE = 0;
    public const int S_WEAPON_1 = 1;
    public const int S_WEAPON_2 = 2;
    public const int S_ENEMY_HIT = 3;
    public const int S_EXPLOSION_4 = 4;
    public const int S_WEAPON_5 = 5;
    public const int S_WEAPON_6 = 6;
    public const int S_WEAPON_7 = 7;
    public const int S_SELECT = 8;
    public const int S_EXPLOSION_8 = 8;
    public const int S_EXPLOSION_9 = 9;
    public const int S_WEAPON_10 = 10;
    public const int S_EXPLOSION_11 = 11;
    public const int S_EXPLOSION_12 = 12;
    public const int S_WEAPON_13 = 13;
    public const int S_WEAPON_14 = 14;
    public const int S_WEAPON_15 = 15;
    public const int S_SPRING = 16;
    public const int S_WARNING = 17;
    public const int S_ITEM = 18;
    public const int S_HULL_HIT = 19;
    public const int S_MACHINE_GUN = 20;
    public const int S_SOUL_OF_ZINGLON = 21;
    public const int S_EXPLOSION_22 = 22;
    public const int S_CLINK = 23;
    public const int S_CLICK = 24;
    public const int S_WEAPON_25 = 25;
    public const int S_WEAPON_26 = 26;
    public const int S_SHIELD_HIT = 27;
    public const int S_CURSOR = 28;
    public const int S_POWERUP = 29;
    public const int V_CLEARED_PLATFORM = 30;
    public const int V_BOSS = 31;
    public const int V_ENEMIES = 32;
    public const int V_GOOD_LUCK = 33;
    public const int V_LEVEL_END = 34;
    public const int V_DANGER = 35;
    public const int V_SPIKES = 36;
    public const int V_DATA_CUBE = 37;
    public const int V_ACCELERATE = 38;

    /// <summary>對應 sndmast.c:windowTextSamples[9]（[1..9]，case 16 用）。</summary>
    public static readonly byte[] windowTextSamples =
    {
        V_DANGER,
        V_BOSS,
        V_ENEMIES,
        V_CLEARED_PLATFORM,
        V_DANGER,
        V_SPIKES,
        V_ACCELERATE,
        V_DANGER,
        V_ENEMIES,
    };
}
