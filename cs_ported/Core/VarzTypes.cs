// 這些結構的欄位由尚未移植的遊戲邏輯（tyrian2/mainint 等）指派；此處僅為型別定義。
#pragma warning disable CS0649

namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/varz.h 的常數、enum 與資料結構（遊戲核心資料層的「結構」部分）。
/// const 資料表與 varz.c 的函式需 player/episodes 與遊戲邏輯，待後續移植。
/// 結構沿用原始欄位順序與寬度；小陣列以 fixed buffer 對應 C 的內嵌陣列。
/// </summary>
internal static class VarzConst
{
    public const int SA = 7;

    public const int SA_NONE = 0;
    public const int SA_NORTSHIPZ = 7;
    // only used for code entry
    public const int SA_DESTRUCT = 8;
    public const int SA_ENGAGE = 9;
    // only used in pItems[P_SUPERARCADE]
    public const int SA_SUPERTYRIAN = 254;
    public const int SA_ARCADE = 255;

    public const int ENEMY_SHOT_MAX = 60;

    public const int CURRENT_KEY_SPEED = 1; // Keyboard/Joystick movement rate

    public const int MAX_EXPLOSIONS = 200;
    public const int MAX_REPEATING_EXPLOSIONS = 20;
    public const int MAX_SUPERPIXELS = 101;

    // typedef 對應的尺寸常數
    public const int JE_DanCShape_SIZE = 24 * 28;        // JE_byte[24*28]
    public const int JE_CharString_SIZE = 256;           // JE_char[256]
    public const int JE_Map1Buffer_SIZE = 24 * 28 * 13 * 4;
}

/// <summary>對應 varz.h:struct JE_SingleEnemyType（單一敵人狀態）。</summary>
internal unsafe struct JE_SingleEnemyType
{
    public byte fillbyte;
    public short ex, ey;        // POSITION
    public sbyte exc, eyc;      // CURRENT SPEED
    public sbyte exca, eyca;    // RANDOM ACCELERATION
    public sbyte excc, eycc;    // FIXED ACCELERATION WAITTIME
    public sbyte exccw, eyccw;
    public byte armorleft;
    public fixed byte eshotwait[3];
    public fixed byte eshotmultipos[3];
    public byte enemycycle;
    public byte ani;
    public fixed ushort egr[20];
    public byte size;
    public byte linknum;
    public byte aniactive;
    public byte animax;
    public byte aniwhenfire;
    public Sprite2_array* sprite2s;
    public sbyte exrev, eyrev;
    public short exccadd, eyccadd;
    public byte exccwmax, eyccwmax;
    public void* enemydatofs;
    public bool edamaged;
    public ushort enemytype;
    public byte animin;
    public ushort edgr;
    public sbyte edlevel;
    public sbyte edani;
    public byte fill1;
    public byte filter;
    public short evalue;
    public short fixedmovey;
    public fixed byte freq[3];
    public byte launchwait;
    public ushort launchtype;
    public byte launchfreq;
    public byte xaccel;
    public byte yaccel;
    public fixed byte tur[3];
    public ushort enemydie;     // Enemy created when this one dies
    public bool enemyground;
    public byte explonum;
    public ushort mapoffset;
    public bool scoreitem;

    public bool special;
    public byte flagnum;
    public bool setto;

    public byte iced;           // Duration

    public byte launchspecial;

    public short xminbounce;
    public short xmaxbounce;
    public short yminbounce;
    public short ymaxbounce;
    public fixed byte fill[3];
}

/// <summary>對應 varz.h:struct JE_EventRecType。</summary>
internal struct JE_EventRecType
{
    public ushort eventtime;
    public byte eventtype;
    public short eventdat, eventdat2;
    public sbyte eventdat3, eventdat5, eventdat6;
    public byte eventdat4;
}

/// <summary>對應 varz.h:EnemyShotType。</summary>
internal unsafe struct EnemyShotType
{
    public short sx, sy;
    public short sxm, sym;
    public sbyte sxc, syc;
    public byte tx, ty;
    public ushort sgr;
    public byte sdmg;
    public byte duration;
    public ushort animate;
    public ushort animax;
    public fixed byte fill[12];
}

/// <summary>對應 varz.h:Explosion。</summary>
internal struct Explosion
{
    public byte ttl;
    public short x, y;
    public ushort sprite;
    public bool followPlayer;
    public bool fixedPosition;
    public short deltaY;
}

/// <summary>對應 varz.h:rep_explosion_type。</summary>
internal struct rep_explosion_type
{
    public uint delay;
    public uint ttl;
    public uint x, y;
    public bool big;
}

/// <summary>對應 varz.h:superpixel_type。</summary>
internal struct superpixel_type
{
    public uint x, y, z;
    public int delta_x, delta_y;
    public byte color;
}
