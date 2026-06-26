using System.Collections.Generic;

namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/tyrian2.c 的 JE_loadMap —— 關卡載入。
/// Part 1：解析 episode 檔的 ]X 命令（過場/設定/跳關）。Part 2：讀 LEVELS.DAT/shapes?.dat → megaData。
/// 未移植的重型子函式（JE_itemScreen 商店、JE_nextEpisode、JE_displayText）暫 stub，
/// 但仍忠實讀取其消耗的檔案資料以維持檔案位置正確；過場的平移/滑動動畫先簡化為直接載圖。
/// </summary>
internal static unsafe partial class Tyrian2
{
    private static char Ch(string s, int i) => i < s.Length ? s[i] : '\0';

    private static int Atoi(string s, int off)
    {
        if (off >= s.Length) return 0;
        int i = off;
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        int sign = 1;
        if (i < s.Length && (s[i] == '+' || s[i] == '-')) { if (s[i] == '-') sign = -1; i++; }
        long v = 0;
        while (i < s.Length && s[i] >= '0' && s[i] <= '9') { v = v * 10 + (s[i] - '0'); i++; }
        return (int)(sign * v);
    }

    private static List<int> PopInts(string s)
    {
        var r = new List<int>();
        int i = 0;
        while (i < s.Length)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            int sign = 1, mark = i;
            if (i < s.Length && (s[i] == '+' || s[i] == '-')) { if (s[i] == '-') sign = -1; i++; }
            int ds = i; long v = 0;
            while (i < s.Length && s[i] >= '0' && s[i] <= '9') { v = v * 10 + (s[i] - '0'); i++; }
            if (i == ds) { i = mark; break; }
            r.Add((int)(sign * v));
        }
        return r;
    }

    private static string CStr(byte[] s)
    {
        int n = 0;
        while (n < s.Length && s[n] != 0) n++;
        var c = new char[n];
        for (int i = 0; i < n; ++i) c[i] = (char)s[i];
        return new string(c);
    }

    private static void CopyToBytes(byte[] dst, string src)
    {
        int n = Math.Min(src.Length, dst.Length - 1);
        for (int i = 0; i < n; ++i) dst[i] = (byte)src[i];
        dst[n] = 0;
    }

    // === 待移植的重型子函式（暫 stub） ===
    // JE_itemScreen 商店主迴圈已移植 → GameMenu.JE_itemScreen()（'I' 命令直接呼叫）

    /// <summary>對應 mainint.c:JE_nextEpisode —— 進入下一章節（過關後）。忠實移植 mainint.c 950-1008。</summary>
    private static void JE_nextEpisode()
    {
        CopyToBytes(Config.lastLevelName, "Completed");

        if (Episodes.episodeNum == Episodes.initial_episode_num && !Config.gameHasRepeated && Episodes.episodeNum != Episodes.EPISODE_AVAILABLE &&
            !Config.isNetworkGame && !Params.constantPlay)
        {
            Mainint.JE_highScoreCheck();
        }

        uint newEpisode = Episodes.JE_findNextEpisode();

        if (Episodes.jumpBackToEpisode1)
        {
            if (Episodes.episodeNum > 2 &&
                !Params.constantPlay)
            {
                Mainint.JE_playCredits();
            }

            // randomly give player the SuperCarrot
            if ((MtRand.mt_rand() % 6) == 0)
            {
                Players.player[0].items.ship = 2;                                  // SuperCarrot
                Players.player[0].items.weapon[Players.FRONT_WEAPON].id = 23;      // Banana Blast
                Players.player[0].items.weapon[Players.REAR_WEAPON].id = 24;       // Banana Blast Rear

                for (uint i = 0; i < 2 /* COUNTOF(player[0].items.weapon) */; ++i)
                    Players.player[0].items.weapon[(int)i].power = 1;

                Players.player[1].items.weapon[Players.REAR_WEAPON].id = 24;        // Banana Blast Rear

                Players.player[0].last_items = Players.player[0].items;
            }
        }

        if (newEpisode != Episodes.episodeNum)
            Episodes.JE_initEpisode((int)newEpisode);

        Varz.gameLoaded = true;
        Config.mainLevel = Episodes.FIRST_LEVEL;
        Config.saveLevel = Episodes.FIRST_LEVEL;

        Loudness.play_song(26);

        Video.JE_clr256(Video.VGAScreen);
        Array.Copy(Palette.palettes[6 - 1], Palette.colors, Palette.colors.Length);

        Fonthand.JE_dString(Video.VGAScreen, Fonthand.JE_fontCenter(Menus.episode_name[Episodes.episodeNum], (uint)Sprites.SMALL_FONT_SHAPES), 130, Menus.episode_name[Episodes.episodeNum], (uint)Sprites.SMALL_FONT_SHAPES);
        Fonthand.JE_dString(Video.VGAScreen, Fonthand.JE_fontCenter(Helptext.miscText[5 - 1], (uint)Sprites.SMALL_FONT_SHAPES), 185, Helptext.miscText[5 - 1], (uint)Sprites.SMALL_FONT_SHAPES);

        Video.JE_showVGA();
        Palette.fade_palette(Palette.colors, 15, 0, 255);

        if (!Params.constantPlay)
            Keyboard.waitUntilGetInput();

        Palette.fade_black(15);
    }

    /// <summary>對應 tyrian2.c:JE_displayText —— 過場警告文字捲動顯示。忠實移植 tyrian2.c 3745-3799。</summary>
    private static void JE_displayText()
    {
        /* Display Warning Text */
        ushort tempY = 55;
        if (Fonthand.warningRed)
        {
            tempY = 2;
        }
        for (Varz.temp = 0; Varz.temp < Fonthand.levelWarningLines; Varz.temp++)
        {
            if (!Keyboard.ESCPressed)
            {
                Mainint.JE_outCharGlow(10, tempY, CStr(Fonthand.levelWarningText[Varz.temp]));

                if (haltGame)
                {
                    Varz.JE_tyrianHalt(5);
                }

                tempY += 10;
            }
        }

        bool slow;
        if (Nortsong.frameCountMax != 0)
        {
            Nortsong.frameCountMax = 6;
            slow = true;
        }
        else
        {
            slow = false;
        }
        Varz.tempW = 184;
        if (Fonthand.warningRed)
            Varz.tempW = 7 * 16 + 6;

        Mainint.JE_outCharGlow((ushort)Fonthand.JE_fontCenter(Helptext.miscText[4], (uint)Sprites.TINY_FONT), Varz.tempW, Helptext.miscText[4]);

        while (true)
        {
            Nortsong.setFrameCount(1);

            if (Fonthand.levelWarningDisplay)
                Fonthand.JE_updateWarning(Video.VGAScreen);

            if (Keyboard.waitUntilGetInputOrElapsed())
                break;

            if ((Nortsong.frameCountMax == 0 && slow) || Keyboard.ESCPressed)
                break;
        }

        Fonthand.levelWarningDisplay = false;
    }

    private static void load_next_demo() { /* TODO: demo 載入 */ }
    private static uint JE_totalScore(int p) => Players.player[p].cash; // 近似（JE_getValue 未移植）

    public static void JE_loadMap()
    {
        var player = Players.player;

        Config.lastCubeMax = Config.cubeMax;
        Musmast.songBuy = Musmast.DEFAULT_SONG_BUY; // 物品畫面預設音樂
        Config.saveLevel = Config.mainLevel;

    new_game:
        Config.galagaMode = false;
        useLastBank = false;
        Config.extraGame = false;
        haltGame = false;
        Varz.gameLoaded = false;

        if (!Varz.play_demo)
        {
            do
            {
                Stream ep_f = CFile.dir_fopen_die(CFile.data_dir(), CStr(Episodes.episode_file), "rb");

                jumpSection = false;
                loadLevelOk = false;

                // Seek section #mainLevel
                int xs = 0;
                while (xs < Config.mainLevel)
                {
                    string s2 = Helptext.ReadEncryptedPascalString(ep_f, 256);
                    if (Ch(s2, 0) == '*')
                        xs++;
                }

                Keyboard.ESCPressed = false;

                do
                {
                    if (Varz.gameLoaded)
                    {
                        ep_f.Dispose();
                        if (Config.mainLevel == 0)  // quit itemscreen
                            return;
                        else
                            goto new_game;
                    }

                    string s = Helptext.ReadEncryptedPascalString(ep_f, 256);

                    if (Ch(s, 0) == ']')
                    {
                        switch (Ch(s, 1))
                        {
                            case 'A':  // Show animation.
                                Animlib.playAnim("tyrend.anm", 0, 7);
                                break;

                            case 'G':  // Set next level choices.
                                mapOrigin = (ushort)Atoi(s, 4);
                                mapPNum = (ushort)Atoi(s, 7);
                                for (int i = 0; i < mapPNum; i++)
                                {
                                    mapPlanet[i] = (byte)Atoi(s, 1 + (i + 1) * 8);
                                    mapSection[i] = (byte)Atoi(s, 4 + (i + 1) * 8);
                                }
                                break;

                            case '?':  // Set data cubes.
                                {
                                    int t = Atoi(s, 4);
                                    if (t > Config.cubeList.Length) t = Config.cubeList.Length;
                                    for (int i = 0; i < t; i++)
                                        Config.cubeList[i] = (ushort)Atoi(s, 3 + (i + 1) * 4);
                                    if (Config.cubeMax > t) Config.cubeMax = (ushort)t;
                                }
                                break;

                            case '!':  // Set number of data cubes acquired.
                                Config.cubeMax = (ushort)Atoi(s, 4);
                                if (Config.cubeMax > Config.cubeList.Length) Config.cubeMax = (ushort)Config.cubeList.Length;
                                break;

                            case '+':  // Increase number of data cubes.
                                Config.cubeMax += (ushort)Atoi(s, 4);
                                if (Config.cubeMax > Config.cubeList.Length) Config.cubeMax = (ushort)Config.cubeList.Length;
                                break;

                            case 'g':  // Enable GALAGA mode.
                                Config.galagaMode = true;
                                player[1].items = player[0].items;
                                player[1].items.weapon[Players.REAR_WEAPON].id = 15; // Vulcan Cannon
                                for (int i = 0; i < 2; ++i)
                                    player[1].items.sidekick[i] = 0;
                                break;

                            case 'x':  // Enable bonus game.
                                Config.extraGame = true;
                                break;

                            case 'e':  // Enable ENGAGE mode.
                                doNotSaveBackup = true;
                                Params.constantDie = false;
                                Config.onePlayerAction = true;
                                Config.superTyrian = true;
                                Config.twoPlayerMode = false;
                                player[0].cash = 0;
                                player[0].items.ship = 13;                       // Stalker 21.126
                                player[0].items.weapon[Players.FRONT_WEAPON].id = 39; // Atomic RailGun
                                player[0].items.weapon[Players.REAR_WEAPON].id = 0;
                                for (int i = 0; i < 2; ++i)
                                    player[0].items.sidekick[i] = 0;
                                player[0].items.generator = 2;
                                player[0].items.shield = 4;
                                player[0].items.special = 0;
                                player[0].items.weapon[Players.FRONT_WEAPON].power = 3;
                                player[0].items.weapon[Players.REAR_WEAPON].power = 1;
                                break;

                            case 'J':  // Jump to section.
                                Config.mainLevel = (byte)Atoi(s, 3);
                                jumpSection = true;
                                break;

                            case '2':  // Jump to section in two-player or one-player arcade.
                                if (Config.twoPlayerMode || Config.onePlayerAction)
                                {
                                    Config.mainLevel = (byte)Atoi(s, 3);
                                    jumpSection = true;
                                }
                                break;

                            case 'w':  // Jump if player has Stalker 21.126.
                                if (player[0].items.ship == 13)
                                {
                                    Config.mainLevel = (byte)Atoi(s, 3);
                                    jumpSection = true;
                                }
                                break;

                            case 't':  // Jump if level timer expired.
                                if (Varz.levelTimer && levelTimerCountdown == 0)
                                {
                                    Config.mainLevel = (byte)Atoi(s, 3);
                                    jumpSection = true;
                                }
                                break;

                            case 'l':  // Jump if player died.
                                if (!Players.all_players_alive())
                                {
                                    Config.mainLevel = (byte)Atoi(s, 3);
                                    jumpSection = true;
                                }
                                break;

                            case 's':  // Store savepoint.
                                Config.saveLevel = Config.mainLevel;
                                break;

                            case 'b':  // Explicit auto-save.
                                Config.JE_saveGame(11, "LAST LEVEL    ");
                                break;

                            case 'i':  // Set menu music track.
                                Musmast.songBuy = (byte)(Atoi(s, 3) - 1);
                                break;

                            case 'I':  // Menu (shop).
                                Array.Clear(itemAvail);
                                for (int i = 0; i < 9; ++i)
                                {
                                    string si = Helptext.ReadEncryptedPascalString(ep_f, 256);
                                    string buf = si.Length > 8 ? si.Substring(8) : "";
                                    int j = 0;
                                    foreach (int v in PopInts(buf))
                                    {
                                        if (j >= 10) break;
                                        itemAvail[i, j++] = (byte)v;
                                    }
                                    itemAvailMax[i] = (byte)j;
                                }
                                GameMenu.JE_itemScreen();
                                break;

                            case 'L':  // Play level.
                                Config.nextLevel = (byte)Atoi(s, 9);
                                CopyToBytes(Config.levelName, s.Length > 13 ? s.Substring(13, Math.Min(9, s.Length - 13)) : "");
                                levelSong = (byte)Atoi(s, 22);
                                if (Config.nextLevel == 0)
                                    Config.nextLevel = (byte)(Config.mainLevel + 1);
                                lvlFileNum = (byte)Atoi(s, 25);
                                loadLevelOk = true;
                                bonusLevelCurrent = s.Length > 28 && s[28] == '$';
                                normalBonusLevelCurrent = s.Length > 27 && s[27] == '$';
                                Config.gameJustLoaded = false;
                                break;

                            case '@':  // Toggle text color bank.
                                useLastBank = !useLastBank;
                                break;

                            case 'Q':  // End of episode.
                                {
                                    Keyboard.ESCPressed = false;
                                    int qcount = Config.secretHint + (int)(MtRand.mt_rand() % 3) * 3;

                                    if (Config.twoPlayerMode)
                                    {
                                        for (int i = 0; i < 2; ++i)
                                            CopyToBytes(Fonthand.levelWarningText[i], $"{Helptext.miscText[40 + i]} {player[i].cash}");
                                        CopyToBytes(Fonthand.levelWarningText[2], "");
                                        Fonthand.levelWarningLines = 3;
                                    }
                                    else
                                    {
                                        CopyToBytes(Fonthand.levelWarningText[0], $"{Helptext.miscText[37]} {JE_totalScore(0)}");
                                        CopyToBytes(Fonthand.levelWarningText[1], "");
                                        Fonthand.levelWarningLines = 2;
                                    }

                                    string sq;
                                    for (int qi = 0; qi < qcount - 1; qi++)
                                        do { sq = Helptext.ReadEncryptedPascalString(ep_f, 256); } while (Ch(sq, 0) != '#');

                                    do
                                    {
                                        sq = Helptext.ReadEncryptedPascalString(ep_f, 256);
                                        CopyToBytes(Fonthand.levelWarningText[Fonthand.levelWarningLines], sq);
                                        Fonthand.levelWarningLines++;
                                    } while (Ch(sq, 0) != '#');
                                    Fonthand.levelWarningLines--;

                                    Nortsong.frameCountMax = 4;
                                    if (!Params.constantPlay)
                                        JE_displayText();

                                    Palette.fade_black(15);
                                    JE_nextEpisode();

                                    jumpSection = true;
                                    if (Config.superTyrian)
                                    {
                                        Palette.fade_black(10);
                                        Config.mainLevel = 0; // back to titlescreen
                                        return;
                                    }
                                    // TODO: jumpBackToEpisode1 的 super-arcade 密碼顯示畫面
                                }
                                break;

                            case 'P':  // Show picture or clear and set palette.
                                if (!Params.constantPlay)
                                {
                                    int tempX = Atoi(s, 3);
                                    if (tempX > 900)
                                    {
                                        Array.Copy(Palette.palettes[Pcxmast.pcxpal[tempX - 1 - 900]], Palette.colors, 256);
                                        Video.JE_clr256(Video.VGAScreen);
                                        Video.JE_showVGA();
                                        Palette.fade_palette(Palette.colors, 1, 0, 255);
                                    }
                                    else
                                    {
                                        if (tempX == 0)
                                            Pcxload.JE_loadPCX("tshp2.pcx");
                                        else
                                            Picload.JE_loadPic(Video.VGAScreen, (byte)tempX, false);
                                        Video.JE_showVGA();
                                        Palette.fade_palette(Palette.colors, 10, 0, 255);
                                    }
                                }
                                break;

                            case 'U':  // Pan up to picture.（簡化：直接載圖）
                            case 'V':  // Slide picture up.
                            case 'R':  // Pan right to picture.
                                if (!Params.constantPlay)
                                {
                                    int tempX = Atoi(s, 3);
                                    Picload.JE_loadPic(Video.VGAScreen, (byte)tempX, false);
                                    Video.JE_showVGA();
                                    Palette.fade_palette(Palette.colors, 10, 0, 255);
                                    // TODO: 平移/滑動進場動畫
                                }
                                break;

                            case 'C':  // Fade to black, clear, reset palette.
                                Palette.fade_black(10);
                                Video.JE_clr256(Video.VGAScreen);
                                Video.JE_showVGA();
                                Array.Copy(Palette.palettes[7], Palette.colors, 256);
                                Palette.set_palette(Palette.colors, 0, 255);
                                break;

                            case 'B':  // Fade to black.
                                Palette.fade_black(10);
                                break;

                            case 'F':  // Flash and clear.
                                Palette.fade_white(100);
                                Palette.fade_black(30);
                                Video.JE_clr256(Video.VGAScreen);
                                Video.JE_showVGA();
                                break;

                            case 'W':  // Show text.（簡化：讀取文字後 stub 顯示）
                                if (!Params.constantPlay && !Keyboard.ESCPressed)
                                {
                                    Fonthand.levelWarningLines = 0;
                                    Nortsong.frameCountMax = (ushort)(Atoi(s, 4) % 10);
                                    string sw;
                                    do
                                    {
                                        sw = Helptext.ReadEncryptedPascalString(ep_f, 256);
                                        if (Ch(sw, 0) != '#')
                                        {
                                            CopyToBytes(Fonthand.levelWarningText[Fonthand.levelWarningLines], sw);
                                            Fonthand.levelWarningLines++;
                                        }
                                    } while (Ch(sw, 0) != '#');
                                    JE_displayText();
                                }
                                break;

                            case 'H':  // Jump if difficulty < hard.
                                if (Config.initialDifficulty < Config.DIFFICULTY_HARD)
                                {
                                    Config.mainLevel = (byte)Atoi(s, 4);
                                    jumpSection = true;
                                }
                                break;

                            case 'h':  // Skip next line if difficulty >= hard.
                                if (Config.initialDifficulty > Config.DIFFICULTY_NORMAL)
                                    Helptext.ReadEncryptedPascalString(ep_f, 256);
                                break;

                            case 'n':  // End of scene.
                                Keyboard.ESCPressed = false;
                                break;

                            case 'M':  // Play music track.
                                Loudness.play_song((uint)(Atoi(s, 3) - 1));
                                break;
                        }
                    }
                } while (!(loadLevelOk || jumpSection));

                ep_f.Dispose();

            } while (!loadLevelOk);
        }

        if (Varz.play_demo)
            load_next_demo();
        else
            Palette.fade_black(50);

        // === Part 2：載入關卡地圖資料 ===
        Stream level_f = CFile.dir_fopen_die(CFile.data_dir(), CStr(Lvllib.levelFile), "rb");
        level_f.Seek(Lvllib.lvlPos[(lvlFileNum - 1) * 2], SeekOrigin.Begin);

        CFile.read_u8(level_f);                         // char_mapFile（未使用）
        byte char_shapeFile = CFile.read_u8(level_f);
        Backgrnd.mapX = CFile.read_u16(level_f);
        Backgrnd.mapX2 = CFile.read_u16(level_f);
        Backgrnd.mapX3 = CFile.read_u16(level_f);

        levelEnemyMax = CFile.read_u16(level_f);
        for (int i = 0; i < levelEnemyMax; i++)
            levelEnemy[i] = CFile.read_u16(level_f);

        maxEvent = CFile.read_u16(level_f);
        int ev;
        for (ev = 0; ev < maxEvent; ev++)
        {
            eventRec[ev].eventtime = CFile.read_u16(level_f);
            eventRec[ev].eventtype = CFile.read_u8(level_f);
            eventRec[ev].eventdat = CFile.read_s16(level_f);
            eventRec[ev].eventdat2 = CFile.read_s16(level_f);
            eventRec[ev].eventdat3 = CFile.read_s8(level_f);
            eventRec[ev].eventdat5 = CFile.read_s8(level_f);
            eventRec[ev].eventdat6 = CFile.read_s8(level_f);
            eventRec[ev].eventdat4 = CFile.read_u8(level_f);
        }
        eventRec[ev].eventtime = 65500;

        // MAP SHAPE LOOKUP TABLE（big-endian，故讀後 byteswap）
        ushort* mapSh = stackalloc ushort[3 * 128];
        for (int t = 0; t < 3; t++)
            for (int t2 = 0; t2 < 128; t2++)
            {
                ushort v = CFile.read_u16(level_f);
                mapSh[t * 128 + t2] = (ushort)((v << 8) | (v >> 8));
            }

        // Read shapes?.dat
        Stream shpFile = CFile.dir_fopen_die(CFile.data_dir(), $"shapes{char.ToLowerInvariant((char)char_shapeFile)}.dat", "rb");

        byte* shape = stackalloc byte[JE_MegaData.DAN_C_SHAPE];
        byte** ref0 = stackalloc byte*[128];
        byte** ref1 = stackalloc byte*[128];
        byte** ref2 = stackalloc byte*[128];

        var md1 = Varz.megaData1; var md2 = Varz.megaData2; var md3 = Varz.megaData3;
        Varz.allocMegaData();

        for (int z = 0; z < 600; z++)
        {
            bool shapeBlank = CFile.read_bool(shpFile);
            if (shapeBlank)
                new Span<byte>(shape, JE_MegaData.DAN_C_SHAPE).Clear();
            else
                CFile.fread_u8_die(shape, JE_MegaData.DAN_C_SHAPE, shpFile);

            for (int x = 0; x <= 71; ++x)
                if (mapSh[0 * 128 + x] == z + 1)
                {
                    Buffer.MemoryCopy(shape, md1.Shape(x), JE_MegaData.DAN_C_SHAPE, JE_MegaData.DAN_C_SHAPE);
                    ref0[x] = md1.Shape(x);
                }

            for (int x = 0; x <= 71; ++x)
                if (mapSh[1 * 128 + x] == z + 1)
                {
                    if (x != 71 && !shapeBlank)
                    {
                        Buffer.MemoryCopy(shape, md2.Shape(x), JE_MegaData.DAN_C_SHAPE, JE_MegaData.DAN_C_SHAPE);
                        byte yy = 1;
                        for (int q = 0; q < (24 * 28) >> 1; q++)
                            if (shape[q] == 0) yy = 0;
                        md2.fill[x] = yy;
                        ref1[x] = md2.Shape(x);
                    }
                    else
                        ref1[x] = null;
                }

            for (int x = 0; x <= 71; ++x)
                if (mapSh[2 * 128 + x] == z + 1)
                {
                    if (x < 70 && !shapeBlank)
                    {
                        Buffer.MemoryCopy(shape, md3.Shape(x), JE_MegaData.DAN_C_SHAPE, JE_MegaData.DAN_C_SHAPE);
                        byte yy = 1;
                        for (int q = 0; q < (24 * 28) >> 1; q++)
                            if (shape[q] == 0) yy = 0;
                        md3.fill[x] = yy;
                        ref2[x] = md3.Shape(x);
                    }
                    else
                        ref2[x] = null;
                }
        }
        shpFile.Dispose();

        byte* mapBuf = stackalloc byte[15 * 600];

        CFile.fread_u8_die(mapBuf, 14 * 300, level_f);
        int bufLoc = 0;
        for (int y = 0; y < 300; y++)
            for (int x = 0; x < 14; x++)
                md1.SetMap(y, x, ref0[mapBuf[bufLoc++]]);

        CFile.fread_u8_die(mapBuf, 14 * 600, level_f);
        bufLoc = 0;
        for (int y = 0; y < 600; y++)
            for (int x = 0; x < 14; x++)
                md2.SetMap(y, x, ref1[mapBuf[bufLoc++]]);

        CFile.fread_u8_die(mapBuf, 15 * 600, level_f);
        bufLoc = 0;
        for (int y = 0; y < 600; y++)
            for (int x = 0; x < 15; x++)
                md3.SetMap(y, x, ref2[mapBuf[bufLoc++]]);

        level_f.Dispose();
    }
}
