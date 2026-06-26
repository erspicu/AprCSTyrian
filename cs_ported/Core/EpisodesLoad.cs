namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/episodes.c 的資料載入：JE_loadItemDat（從 tyrian.hdt 載入完整物品/敵人資料庫）、
/// JE_initEpisode、JE_scanForEpisodes、JE_findNextEpisode。
/// </summary>
internal static unsafe partial class Episodes
{
    private static void SetCStr(byte[] dst, string s)
    {
        int n = Math.Min(s.Length, dst.Length - 1);
        for (int i = 0; i < n; ++i) dst[i] = (byte)s[i];
        dst[n] = 0;
    }

    public static void JE_loadItemDat()
    {
        Stream f;

        if (episodeNum <= 3)
        {
            f = CFile.dir_fopen_die(CFile.data_dir(), "tyrian.hdt", "rb");
            episode1DataLoc = CFile.read_s32(f);
            f.Seek(episode1DataLoc, SeekOrigin.Begin);
        }
        else
        {
            // episode 4 stores item data in the level file
            f = CFile.dir_fopen_die(CFile.data_dir(), CStr(Lvllib.levelFile), "rb");
            f.Seek(Lvllib.lvlPos[Lvllib.lvlNum - 1], SeekOrigin.Begin);
        }

        // JE_word itemNum[7]（忽略）
        for (int k = 0; k < 7; ++k) CFile.read_u16(f);

        for (int i = 0; i < Lvlmast.WEAP_NUM + 1; ++i)
        {
            ref JE_WeaponType w = ref weapons[i];
            w.drain = CFile.read_u16(f);
            w.shotrepeat = CFile.read_u8(f);
            w.multi = CFile.read_u8(f);
            w.weapani = CFile.read_u16(f);
            w.max = CFile.read_u8(f);
            w.tx = CFile.read_u8(f);
            w.ty = CFile.read_u8(f);
            w.aim = CFile.read_u8(f);
            fixed (byte* p = weapons[i].attack) CFile.fread_u8_die(p, 8, f);
            fixed (byte* p = weapons[i].del) CFile.fread_u8_die(p, 8, f);
            fixed (sbyte* p = weapons[i].sx) CFile.fread_s8_die(p, 8, f);
            fixed (sbyte* p = weapons[i].sy) CFile.fread_s8_die(p, 8, f);
            fixed (sbyte* p = weapons[i].bx) CFile.fread_s8_die(p, 8, f);
            fixed (sbyte* p = weapons[i].by) CFile.fread_s8_die(p, 8, f);
            fixed (ushort* p = weapons[i].sg) CFile.fread_u16_die(p, 8, f);
            w.acceleration = CFile.read_s8(f);
            w.accelerationx = CFile.read_s8(f);
            w.circlesize = CFile.read_u8(f);
            w.sound = CFile.read_u8(f);
            w.trail = CFile.read_u8(f);
            w.shipblastfilter = CFile.read_u8(f);
        }

        for (int i = 0; i < Lvlmast.PORT_NUM + 1; ++i)
        {
            byte nameLen = CFile.read_u8(f);
            fixed (byte* p = weaponPort[i].name) { CFile.fread_die(p, 1, 30, f); p[Math.Min((int)nameLen, 30)] = 0; }
            weaponPort[i].opnum = CFile.read_u8(f);
            fixed (ushort* p = weaponPort[i].op) { CFile.fread_u16_die(p, 11, f); CFile.fread_u16_die(p + 11, 11, f); }
            weaponPort[i].cost = CFile.read_u16(f);
            weaponPort[i].itemgraphic = CFile.read_u16(f);
            weaponPort[i].poweruse = CFile.read_u16(f);
        }

        for (int i = 0; i < Lvlmast.SPECIAL_NUM + 1; ++i)
        {
            byte nameLen = CFile.read_u8(f);
            fixed (byte* p = special[i].name) { CFile.fread_die(p, 1, 30, f); p[Math.Min((int)nameLen, 30)] = 0; }
            special[i].itemgraphic = CFile.read_u16(f);
            special[i].pwr = CFile.read_u8(f);
            special[i].stype = CFile.read_u8(f);
            special[i].wpn = CFile.read_u16(f);
        }

        for (int i = 0; i < Lvlmast.POWER_NUM + 1; ++i)
        {
            byte nameLen = CFile.read_u8(f);
            fixed (byte* p = powerSys[i].name) { CFile.fread_die(p, 1, 30, f); p[Math.Min((int)nameLen, 30)] = 0; }
            powerSys[i].itemgraphic = CFile.read_u16(f);
            powerSys[i].power = CFile.read_u8(f);
            powerSys[i].speed = CFile.read_s8(f);
            powerSys[i].cost = CFile.read_u16(f);
        }

        for (int i = 0; i < Lvlmast.SHIP_NUM + 1; ++i)
        {
            byte nameLen = CFile.read_u8(f);
            fixed (byte* p = ships[i].name) { CFile.fread_die(p, 1, 30, f); p[Math.Min((int)nameLen, 30)] = 0; }
            ships[i].shipgraphic = CFile.read_u16(f);
            ships[i].itemgraphic = CFile.read_u16(f);
            ships[i].ani = CFile.read_u8(f);
            ships[i].spd = CFile.read_s8(f);
            ships[i].dmg = CFile.read_u8(f);
            ships[i].cost = CFile.read_u16(f);
            ships[i].bigshipgraphic = CFile.read_u8(f);
        }

        for (int i = 0; i < Lvlmast.OPTION_NUM + 1; ++i)
        {
            byte nameLen = CFile.read_u8(f);
            fixed (byte* p = options[i].name) { CFile.fread_die(p, 1, 30, f); p[Math.Min((int)nameLen, 30)] = 0; }
            options[i].pwr = CFile.read_u8(f);
            options[i].itemgraphic = CFile.read_u16(f);
            options[i].cost = CFile.read_u16(f);
            options[i].tr = CFile.read_u8(f);
            options[i].option = CFile.read_u8(f);
            options[i].opspd = CFile.read_s8(f);
            options[i].ani = CFile.read_u8(f);
            fixed (ushort* p = options[i].gr) CFile.fread_u16_die(p, 20, f);
            options[i].wport = CFile.read_u8(f);
            options[i].wpnum = CFile.read_u16(f);
            options[i].ammo = CFile.read_u8(f);
            options[i].stop = CFile.read_bool(f);
            options[i].icongr = CFile.read_u8(f);
        }

        for (int i = 0; i < Lvlmast.SHIELD_NUM + 1; ++i)
        {
            byte nameLen = CFile.read_u8(f);
            fixed (byte* p = shields[i].name) { CFile.fread_die(p, 1, 30, f); p[Math.Min((int)nameLen, 30)] = 0; }
            shields[i].tpwr = CFile.read_u8(f);
            shields[i].mpwr = CFile.read_u8(f);
            shields[i].itemgraphic = CFile.read_u16(f);
            shields[i].cost = CFile.read_u16(f);
        }

        for (int i = 0; i < Lvlmast.ENEMY_NUM + 1; ++i)
        {
            ref JE_EnemyDat e = ref enemyDat[i];
            e.ani = CFile.read_u8(f);
            fixed (byte* p = enemyDat[i].tur) CFile.fread_u8_die(p, 3, f);
            fixed (byte* p = enemyDat[i].freq) CFile.fread_u8_die(p, 3, f);
            e.xmove = CFile.read_s8(f);
            e.ymove = CFile.read_s8(f);
            e.xaccel = CFile.read_s8(f);
            e.yaccel = CFile.read_s8(f);
            e.xcaccel = CFile.read_s8(f);
            e.ycaccel = CFile.read_s8(f);
            e.startx = CFile.read_s16(f);
            e.starty = CFile.read_s16(f);
            e.startxc = CFile.read_s8(f);
            e.startyc = CFile.read_s8(f);
            e.armor = CFile.read_u8(f);
            e.esize = CFile.read_u8(f);
            fixed (ushort* p = enemyDat[i].egraphic) CFile.fread_u16_die(p, 20, f);
            e.explosiontype = CFile.read_u8(f);
            e.animate = CFile.read_u8(f);
            e.shapebank = CFile.read_u8(f);
            e.xrev = CFile.read_s8(f);
            e.yrev = CFile.read_s8(f);
            e.dgr = CFile.read_u16(f);
            e.dlevel = CFile.read_s8(f);
            e.dani = CFile.read_s8(f);
            e.elaunchfreq = CFile.read_u8(f);
            e.elaunchtype = CFile.read_u16(f);
            e.value = CFile.read_s16(f);
            e.eenemydie = CFile.read_u16(f);
        }

        CFile.fclose(f);
    }

    public static void JE_initEpisode(int newEpisode)
    {
        if (newEpisode == episodeNum)
            return;

        episodeNum = (byte)newEpisode;

        SetCStr(Lvllib.levelFile, $"tyrian{episodeNum}.lvl");
        SetCStr(cube_file, $"cubetxt{episodeNum}.dat");
        SetCStr(episode_file, $"levels{episodeNum}.dat");

        Lvllib.JE_analyzeLevel();
        JE_loadItemDat();
    }

    public static void JE_scanForEpisodes()
    {
        for (int i = 0; i < EPISODE_MAX; ++i)
            episodeAvail[i] = CFile.dir_file_exists(CFile.data_dir(), $"tyrian{i + 1}.lvl");
    }

    public static uint JE_findNextEpisode()
    {
        uint newEpisode = episodeNum;
        jumpBackToEpisode1 = false;

        while (true)
        {
            newEpisode++;
            if (newEpisode > EPISODE_MAX)
            {
                newEpisode = 1;
                jumpBackToEpisode1 = true;
                Config.gameHasRepeated = true;
            }
            if (episodeAvail[newEpisode - 1] || newEpisode == episodeNum)
                break;
        }
        return newEpisode;
    }

    private static string CStr(byte[] s)
    {
        int n = 0;
        while (n < s.Length && s[n] != 0) n++;
        var chars = new char[n];
        for (int i = 0; i < n; ++i) chars[i] = (char)s[i];
        return new string(chars);
    }
}
