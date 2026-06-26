namespace AprCSTyrian.Core;

// 結構欄位由資料載入 (JE_loadItemDat，待移植) 填入；此處僅型別定義。
#pragma warning disable CS0649

/// <summary>對應 episodes.h:JE_WeaponType。</summary>
internal unsafe struct JE_WeaponType
{
    public ushort drain;
    public byte shotrepeat;
    public byte multi;
    public ushort weapani;
    public byte max;
    public byte tx, ty, aim;
    public fixed byte attack[8];
    public fixed byte del[8];
    public fixed sbyte sx[8];
    public fixed sbyte sy[8];
    public fixed sbyte bx[8];
    public fixed sbyte by[8];
    public fixed ushort sg[8];
    public sbyte acceleration, accelerationx;
    public byte circlesize;
    public byte sound;
    public byte trail;
    public byte shipblastfilter;
}

/// <summary>對應 episodes.h:JE_WeaponPortType 的單一 port 元素。</summary>
internal unsafe struct JE_WeaponPort
{
    public fixed byte name[31];      // string[30]
    public byte opnum;
    public fixed ushort op[2 * 11];  // op[2][11]
    public ushort cost;
    public ushort itemgraphic;
    public ushort poweruse;
}

/// <summary>對應 episodes.h:JE_PowerType 元素。</summary>
internal unsafe struct JE_Power
{
    public fixed byte name[31];
    public ushort itemgraphic;
    public byte power;
    public sbyte speed;
    public ushort cost;
}

/// <summary>對應 episodes.h:JE_SpecialType 元素。</summary>
internal unsafe struct JE_Special
{
    public fixed byte name[31];
    public ushort itemgraphic;
    public byte pwr;
    public byte stype;
    public ushort wpn;
}

/// <summary>對應 episodes.h:JE_OptionType。</summary>
internal unsafe struct JE_OptionType
{
    public fixed byte name[31];
    public byte pwr;
    public ushort itemgraphic;
    public ushort cost;
    public byte tr, option;
    public sbyte opspd;
    public byte ani;
    public fixed ushort gr[20];
    public byte wport;
    public ushort wpnum;
    public byte ammo;
    public bool stop;
    public byte icongr;
}

/// <summary>對應 episodes.h:JE_ShieldType 元素。</summary>
internal unsafe struct JE_Shield
{
    public fixed byte name[31];
    public byte tpwr, mpwr;
    public ushort itemgraphic;
    public ushort cost;
}

/// <summary>對應 episodes.h:JE_ShipType 元素。</summary>
internal unsafe struct JE_Ship
{
    public fixed byte name[31];
    public ushort shipgraphic;
    public ushort itemgraphic;
    public byte ani;
    public sbyte spd;
    public byte dmg;
    public ushort cost;
    public byte bigshipgraphic;
}

/// <summary>對應 episodes.h:JE_EnemyDatType 元素。</summary>
internal unsafe struct JE_EnemyDat
{
    public byte ani;
    public fixed byte tur[3];
    public fixed byte freq[3];
    public sbyte xmove, ymove;
    public sbyte xaccel, yaccel;
    public sbyte xcaccel, ycaccel;
    public short startx, starty;
    public sbyte startxc, startyc;
    public byte armor, esize;
    public fixed ushort egraphic[20];
    public byte explosiontype;
    public byte animate;     // 0:Not Yet 1:Always 2:When Firing Only
    public byte shapebank;
    public sbyte xrev, yrev;
    public ushort dgr;
    public sbyte dlevel, dani;
    public byte elaunchfreq;
    public ushort elaunchtype;
    public short value;
    public ushort eenemydie;
}

/// <summary>
/// 移植 sources/src/episodes.h —— 物品/敵人資料全域與 episode 狀態。
/// 資料載入/掃描函式 (JE_loadItemDat/initEpisode/findNextEpisode/scanForEpisodes) 待後續移植。
/// </summary>
internal static class Episodes
{
    public const int FIRST_LEVEL = 1;
    public const int EPISODE_MAX = 5;
    public const int EPISODE_AVAILABLE = 4;

    public static readonly JE_WeaponPort[] weaponPort = new JE_WeaponPort[Lvlmast.PORT_NUM + 1];
    public static readonly JE_WeaponType[] weapons = new JE_WeaponType[Lvlmast.WEAP_NUM + 1];
    public static readonly JE_Power[] powerSys = new JE_Power[Lvlmast.POWER_NUM + 1];
    public static readonly JE_Ship[] ships = new JE_Ship[Lvlmast.SHIP_NUM + 1];
    public static readonly JE_OptionType[] options = new JE_OptionType[Lvlmast.OPTION_NUM + 1];
    public static readonly JE_Shield[] shields = new JE_Shield[Lvlmast.SHIELD_NUM + 1];
    public static readonly JE_Special[] special = new JE_Special[Lvlmast.SPECIAL_NUM + 1];
    public static readonly JE_EnemyDat[] enemyDat = new JE_EnemyDat[Lvlmast.ENEMY_NUM + 1];

    public static byte initial_episode_num, episodeNum;
    public static readonly bool[] episodeAvail = new bool[EPISODE_MAX];

    public static readonly byte[] episode_file = new byte[13];
    public static readonly byte[] cube_file = new byte[13];

    public static int episode1DataLoc;
    public static bool bonusLevel;
    public static bool jumpBackToEpisode1;
}
