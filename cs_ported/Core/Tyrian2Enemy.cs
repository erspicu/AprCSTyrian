namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/tyrian2.c 的敵人工廠/生成：JE_makeEnemy / JE_createNewEventEnemy / JE_eventJump。
/// JE_makeEnemy 以「敵人索引」操作 Varz.enemy[en]（C 原為指標）；sprite2s/enemydatofs 改值/索引。
/// </summary>
internal static unsafe partial class Tyrian2
{
    /// <summary>對應 JE_makeEnemy：由 enemyDat 初始化一個敵人。回傳要設給 enemyAvail 的值。</summary>
    public static uint JE_makeEnemy(int en, ushort eDatI, short uniqueShapeTableI)
    {
        uint avail;
        byte shapeTableI;

        if (Config.superArcadeMode != VarzConst.SA_NONE && eDatI == 534)
            eDatI = 533;
        int d = eDatI;

        shapeTableI = uniqueShapeTableI > 0 ? (byte)uniqueShapeTableI : Episodes.enemyDat[d].shapebank;

        // 未找到 sprite 表時沿用敵人槽舊值（同 C）
        if (shapeTableI == 21)
        {
            Varz.enemy[en].sprite2s = Sprites.spriteSheet11; // Coins&Gems
        }
        else if (shapeTableI == 26)
        {
            Varz.enemy[en].sprite2s = Sprites.spriteSheet10; // Two-Player Stuff
        }
        else
        {
            for (int i = 0; i < Sprites.enemySpriteSheetIds.Length; ++i)
                if (shapeTableI == Sprites.enemySpriteSheetIds[i])
                {
                    Varz.enemy[en].sprite2s = Sprites.enemySpriteSheets[i];
                   
                }
        }
        // 若未找到：沿用敵人槽先前的 sprite2s（與 C 同：保留舊值）

        Varz.enemy[en].enemydatofs = d;
        Varz.enemy[en].mapoffset = 0;

        for (int i = 0; i < 3; ++i)
            Varz.enemy[en].eshotmultipos[i] = 0;

        Varz.enemy[en].enemyground = (Episodes.enemyDat[d].explosiontype & 1) == 0;
        Varz.enemy[en].explonum = (byte)(Episodes.enemyDat[d].explosiontype >> 1);

        Varz.enemy[en].launchfreq = Episodes.enemyDat[d].elaunchfreq;
        Varz.enemy[en].launchwait = Episodes.enemyDat[d].elaunchfreq;
        Varz.enemy[en].launchtype = (ushort)(Episodes.enemyDat[d].elaunchtype % 1000);
        Varz.enemy[en].launchspecial = (byte)(Episodes.enemyDat[d].elaunchtype / 1000);

        Varz.enemy[en].xaccel = (byte)Episodes.enemyDat[d].xaccel;
        Varz.enemy[en].yaccel = (byte)Episodes.enemyDat[d].yaccel;

        Varz.enemy[en].xminbounce = -10000;
        Varz.enemy[en].xmaxbounce = 10000;
        Varz.enemy[en].yminbounce = -10000;
        Varz.enemy[en].ymaxbounce = 10000;

        for (int i = 0; i < 3; ++i)
            Varz.enemy[en].tur[i] = Episodes.enemyDat[d].tur[i];

        Varz.enemy[en].ani = Episodes.enemyDat[d].ani;
        Varz.enemy[en].animin = 1;

        switch (Episodes.enemyDat[d].animate)
        {
            case 0:
                Varz.enemy[en].enemycycle = 1; Varz.enemy[en].aniactive = 0;
                Varz.enemy[en].animax = 0; Varz.enemy[en].aniwhenfire = 0;
                break;
            case 1:
                Varz.enemy[en].enemycycle = 0; Varz.enemy[en].aniactive = 1;
                Varz.enemy[en].animax = 0; Varz.enemy[en].aniwhenfire = 0;
                break;
            case 2:
                Varz.enemy[en].enemycycle = 1; Varz.enemy[en].aniactive = 2;
                Varz.enemy[en].animax = Varz.enemy[en].ani; Varz.enemy[en].aniwhenfire = 2;
                break;
        }

        if (Episodes.enemyDat[d].startxc != 0)
            Varz.enemy[en].ex = (short)(Episodes.enemyDat[d].startx + (MtRand.mt_rand() % (Episodes.enemyDat[d].startxc * 2)) - Episodes.enemyDat[d].startxc + 1);
        else
            Varz.enemy[en].ex = (short)(Episodes.enemyDat[d].startx + 1);

        if (Episodes.enemyDat[d].startyc != 0)
            Varz.enemy[en].ey = (short)(Episodes.enemyDat[d].starty + (MtRand.mt_rand() % (Episodes.enemyDat[d].startyc * 2)) - Episodes.enemyDat[d].startyc + 1);
        else
            Varz.enemy[en].ey = (short)(Episodes.enemyDat[d].starty + 1);

        Varz.enemy[en].exc = Episodes.enemyDat[d].xmove;
        Varz.enemy[en].eyc = Episodes.enemyDat[d].ymove;
        Varz.enemy[en].excc = Episodes.enemyDat[d].xcaccel;
        Varz.enemy[en].eycc = Episodes.enemyDat[d].ycaccel;
        Varz.enemy[en].exccw = (sbyte)Math.Abs(Varz.enemy[en].excc);
        Varz.enemy[en].exccwmax = (byte)Varz.enemy[en].exccw;
        Varz.enemy[en].eyccw = (sbyte)Math.Abs(Varz.enemy[en].eycc);
        Varz.enemy[en].eyccwmax = (byte)Varz.enemy[en].eyccw;
        Varz.enemy[en].exccadd = (short)(Varz.enemy[en].excc > 0 ? 1 : -1);
        Varz.enemy[en].eyccadd = (short)(Varz.enemy[en].eycc > 0 ? 1 : -1);
        Varz.enemy[en].special = false;
        Varz.enemy[en].iced = 0;

        if (Episodes.enemyDat[d].xrev == 0) Varz.enemy[en].exrev = 100;
        else if (Episodes.enemyDat[d].xrev == -99) Varz.enemy[en].exrev = 0;
        else Varz.enemy[en].exrev = Episodes.enemyDat[d].xrev;

        if (Episodes.enemyDat[d].yrev == 0) Varz.enemy[en].eyrev = 100;
        else if (Episodes.enemyDat[d].yrev == -99) Varz.enemy[en].eyrev = 0;
        else Varz.enemy[en].eyrev = Episodes.enemyDat[d].yrev;

        Varz.enemy[en].exca = (sbyte)(Varz.enemy[en].xaccel > 0 ? 1 : -1);
        Varz.enemy[en].eyca = (sbyte)(Varz.enemy[en].yaccel > 0 ? 1 : -1);

        Varz.enemy[en].enemytype = eDatI;

        for (int i = 0; i < 3; ++i)
        {
            if (Varz.enemy[en].tur[i] == 252) Varz.enemy[en].eshotwait[i] = 1;
            else if (Varz.enemy[en].tur[i] > 0) Varz.enemy[en].eshotwait[i] = 20;
            else Varz.enemy[en].eshotwait[i] = 255;
        }

        for (int i = 0; i < 20; ++i)
            Varz.enemy[en].egr[i] = Episodes.enemyDat[d].egraphic[i];
        Varz.enemy[en].size = Episodes.enemyDat[d].esize;
        Varz.enemy[en].linknum = 0;
        Varz.enemy[en].edamaged = Episodes.enemyDat[d].dani < 0;
        Varz.enemy[en].enemydie = Episodes.enemyDat[d].eenemydie;

        for (int i = 0; i < 3; ++i)
            Varz.enemy[en].freq[i] = Episodes.enemyDat[d].freq[i];

        Varz.enemy[en].edani = Episodes.enemyDat[d].dani;
        Varz.enemy[en].edgr = Episodes.enemyDat[d].dgr;
        Varz.enemy[en].edlevel = Episodes.enemyDat[d].dlevel;

        Varz.enemy[en].fixedmovey = 0;
        Varz.enemy[en].filter = 0x00;

        short val = Episodes.enemyDat[d].value;
        int tempValue = 0;
        if (val > 1 && val < 10000)
        {
            tempValue = (int)difficultyValue(val);
            if (tempValue > 10000) tempValue = 10000;
            Varz.enemy[en].evalue = (short)tempValue;
        }
        else
        {
            Varz.enemy[en].evalue = val;
        }

        int tempArmor = 1;
        byte armor = Episodes.enemyDat[d].armor;
        if (armor > 0)
        {
            tempArmor = armor != 255 ? difficultyArmor(armor) : 255;
            if (armor != 255 && tempArmor > 254) tempArmor = 254;
            Varz.enemy[en].armorleft = (byte)tempArmor;
            avail = 0;
            Varz.enemy[en].scoreitem = false;
        }
        else
        {
            avail = 2;
            Varz.enemy[en].armorleft = 255;
            Varz.enemy[en].scoreitem = Varz.enemy[en].evalue != 0;
        }

        if (!Varz.enemy[en].scoreitem)
            totalEnemy++;

        return avail;
    }

    private static float difficultyValue(short value)
    {
        switch ((int)Config.difficultyLevel)
        {
            case -1:
            case Config.DIFFICULTY_WIMP: return value * 0.75f;
            case Config.DIFFICULTY_EASY:
            case Config.DIFFICULTY_NORMAL: return value;
            case Config.DIFFICULTY_HARD: return value * 1.125f;
            case Config.DIFFICULTY_IMPOSSIBLE: return value * 1.5f;
            case Config.DIFFICULTY_INSANITY: return value * 2;
            case Config.DIFFICULTY_SUICIDE: return value * 2.5f;
            case Config.DIFFICULTY_MANIACAL:
            case Config.DIFFICULTY_ZINGLON: return value * 4;
            case Config.DIFFICULTY_NORTANEOUS:
            case Config.DIFFICULTY_10: return value * 8;
            default: return 0;
        }
    }

    private static int difficultyArmor(byte armor)
    {
        switch ((int)Config.difficultyLevel)
        {
            case -1:
            case Config.DIFFICULTY_WIMP: return (int)(armor * 0.5f + 1);
            case Config.DIFFICULTY_EASY: return (int)(armor * 0.75f + 1);
            case Config.DIFFICULTY_NORMAL: return armor;
            case Config.DIFFICULTY_HARD: return (int)(armor * 1.2f);
            case Config.DIFFICULTY_IMPOSSIBLE: return (int)(armor * 1.5f);
            case Config.DIFFICULTY_INSANITY: return (int)(armor * 1.8f);
            case Config.DIFFICULTY_SUICIDE: return armor * 2;
            case Config.DIFFICULTY_MANIACAL: return armor * 3;
            case Config.DIFFICULTY_ZINGLON: return armor * 4;
            case Config.DIFFICULTY_NORTANEOUS:
            case Config.DIFFICULTY_10: return armor * 8;
            default: return 1;
        }
    }
}
