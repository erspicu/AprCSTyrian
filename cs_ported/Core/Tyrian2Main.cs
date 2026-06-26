namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/tyrian2.c 的 JE_main —— 遊戲主迴圈。
/// 逐行對照原版（行 624-2389）翻譯：start_level / start_level_first / level_loop 三段標籤、
/// 關卡 setup、事件系統、三層背景、敵人/子彈/敵彈/爆炸、玩家移動與碰撞、HUD、關卡結束流程。
/// 尚未移植之函式（濾鏡/gamma/暫停/遊戲中設定/分層 JE_drawEnemy/demo/網路）以 TODO 空殼或註記標示。
/// </summary>
internal static unsafe partial class Tyrian2
{
    /// <summary>對應 tyrian2.c:JE_starShowVGA —— 把 game_screen 的 playfield(寬264,偏移+24)合成到 VGAScreenSeg 並顯示；右側 ~56px 為 HUD 介面面板。</summary>
    public static void JE_starShowVGA()
    {
        if (!playerEndLevel && !Varz.skipStarShowVGA)
        {
            byte* s = Video.VGAScreenSeg.pixels;
            byte* src = Video.game_screen.pixels + 24;
            int segPitch = Video.VGAScreenSeg.pitch;
            int gsPitch = Video.game_screen.pitch;

            if (Config.smoothScroll)
            {
                Nortsong.delayUntilElapsed();
                Nortsong.setFrameCount(Nortsong.frameCountMax);
            }

            if (Config.starShowVGASpecialCode == 1)
            {
                src += gsPitch * 183;
                for (int y = 0; y < 184; y++)
                {
                    Buffer.MemoryCopy(src, s, 264, 264);
                    s += segPitch;
                    src -= gsPitch;
                }
            }
            else if (Config.starShowVGASpecialCode == 2 && Config.processorType >= 2)
            {
                int lighty = 172 - Players.player[0].y;
                int lightx = 281 - Players.player[0].x;
                for (int y = 184; y != 0; y--)
                {
                    if (lighty > y)
                    {
                        for (int x = 320 - 56; x != 0; x--)
                        {
                            *s = (byte)((*src & 0xf0) | ((*src >> 2) & 0x03));
                            s++; src++;
                        }
                    }
                    else
                    {
                        for (int x = 320 - 56; x != 0; x--)
                        {
                            int lightdist = Math.Abs(lightx - x) + lighty;
                            if (lightdist < y)
                                *s = *src;
                            else if (lightdist - y <= 5)
                                *s = (byte)((*src & 0xf0) | (((*src & 0x0f) + (3 * (5 - (lightdist - y)))) / 4));
                            else
                                *s = (byte)((*src & 0xf0) | ((*src & 0x0f) >> 2));
                            s++; src++;
                        }
                    }
                    s += 56 + segPitch - 320;
                    src += 56 + gsPitch - 320;
                }
            }
            else
            {
                for (int y = 0; y < 184; y++)
                {
                    Buffer.MemoryCopy(src, s, 264, 264);
                    s += segPitch;
                    src += gsPitch;
                }
            }

            Video.JE_showVGA();
        }

        Keyboard.handleSdlEvents();
    }

    private static string CStrBytes(byte[] b)
    {
        int n = 0; while (n < b.Length && b[n] != 0) n++;
        var c = new char[n]; for (int i = 0; i < n; ++i) c[i] = (char)b[i];
        return new string(c);
    }

    /// <summary>
    /// 逐行移植 sources/src/tyrian2.c:JE_main（行 624-2389）—— 遊戲主迴圈。
    /// 結構/順序/變數忠實對照原版；尚未移植者以 TODO 空殼呼叫標示，不自創行為。
    /// 注意：敵人以分層 JE_drawEnemy(50/100/25/75) 繪製，依原版位置呼叫（ground/sky/top）。
    /// </summary>
    public static void JE_main()
    {
        var player = Players.player;

        int lastEnemyOnScreen = 0;
        uint[] old_weapon_bar = { 0, 0 };  // only redrawn when they change

        /* We need to jump to the beginning to make space for the routines */
        goto start_level_first;

    start_level:

        Keyboard.keyboardClearInput();
        Keyboard.mouseClearInput();

        Keyboard.mouseSetRelative(false);

        if (Config.galagaMode)
            Config.twoPlayerMode = false;

        Sprites.free_sprite2s(ref Sprites.enemySpriteSheets[0]);
        Sprites.free_sprite2s(ref Sprites.enemySpriteSheets[1]);
        Sprites.free_sprite2s(ref Sprites.enemySpriteSheets[2]);
        Sprites.free_sprite2s(ref Sprites.enemySpriteSheets[3]);

        /* Normal speed */
        if (Config.fastPlay != 0)
        {
            Config.smoothScroll = true;
            Nortsong.setFrameSpeed(0x4300);
        }

        if (Varz.play_demo || Varz.record_demo)
        {
            // TODO: 待移植 demo 檔案 IO（fclose(demo_file)）
            if (Varz.play_demo)
            {
                Loudness.stop_song();
                Palette.fade_black(10);
            }
        }

        Config.difficultyLevel = Config.oldDifficultyLevel;   /*Return difficulty to normal*/

        if (!Varz.play_demo)
        {
            if ((!Players.all_players_dead() || normalBonusLevelCurrent || bonusLevelCurrent) && !playerEndLevel)
            {
                Config.mainLevel = Config.nextLevel;
                Mainint.JE_endLevelAni();

                Loudness.fade_song();
            }
            else
            {
                Loudness.fade_song();
                Palette.fade_black(10);

                Config.JE_loadGame(Config.twoPlayerMode ? (byte)22 : (byte)11);
                if (doNotSaveBackup)
                {
                    Config.superTyrian = false;
                    Config.onePlayerAction = false;
                    player[0].items.super_arcade_mode = (byte)VarzConst.SA_NONE;
                }
                if (bonusLevelCurrent && !playerEndLevel)
                {
                    Config.mainLevel = Config.nextLevel;
                }
            }
        }
        doNotSaveBackup = false;

        if (Varz.play_demo)
            return;

    start_level_first:

        Loudness.set_volume((byte)Nortsong.tyrMusicVolume, (byte)Nortsong.fxVolume);

        endLevel = false;
        reallyEndLevel = false;
        playerEndLevel = false;
        Config.extraGame = false;

        doNotSaveBackup = false;
        JE_loadMap();

        if (Config.mainLevel == 0)  // if quit itemscreen
            return;                 // back to titlescreen

        if (!Varz.play_demo)
            Keyboard.mouseSetRelative(true);

        Loudness.fade_song();

        for (int i = 0; i < 2; ++i)
            player[i].is_alive = true;

        Config.oldDifficultyLevel = Config.difficultyLevel;
        if (Episodes.episodeNum == Episodes.EPISODE_AVAILABLE)
            Config.difficultyLevel--;
        if (Config.difficultyLevel < Config.DIFFICULTY_EASY)
            Config.difficultyLevel = Config.DIFFICULTY_EASY;

        player[0].x = 100;
        player[0].y = 180;

        player[1].x = 190;
        player[1].y = 180;

        for (int i = 0; i < 2; ++i)
        {
            for (int j = 0; j < 20; ++j)
            {
                player[i].old_x[j] = player[i].x - (19 - j);
                player[i].old_y[j] = player[i].y - 18;
            }

            player[i].last_x_shot_move = player[i].x;
            player[i].last_y_shot_move = player[i].y;
        }

        Picload.JE_loadPic(Video.VGAScreen, Config.twoPlayerMode ? (byte)6 : (byte)3, false);

        Varz.JE_drawOptions();

        Fonthand.JE_outText(Video.VGAScreen, 268, Config.twoPlayerMode ? 76 : 118, CStrBytes(Config.levelName), 12, 4);

        Video.JE_showVGA();
        Mainint.JE_gammaCorrect(Palette.colors, Config.gammaCorrection); // TODO: 待移植 JE_gammaCorrect
        Palette.fade_palette(Palette.colors, 50, 0, 255);

        if (Sprites.explosionSpriteSheet.data == null)
            Sprites.JE_loadCompShapes(ref Sprites.explosionSpriteSheet, '6');

        /* MAPX will already be set correctly */
        Backgrnd.mapY = 300 - 8;
        Backgrnd.mapY2 = 600 - 8;
        Backgrnd.mapY3 = 600 - 8;
        Backgrnd.mapYPosIdx = Backgrnd.mapY * 14 - 1;
        Backgrnd.mapY2PosIdx = Backgrnd.mapY2 * 14 - 1;
        Backgrnd.mapY3PosIdx = Backgrnd.mapY3 * 15 - 1;
        Backgrnd.mapXPos = 0;
        Backgrnd.mapXOfs = 0;
        Backgrnd.mapX2Pos = 0;
        Backgrnd.mapX3Pos = 0;
        Backgrnd.mapX3Ofs = 0;
        Backgrnd.mapXbpPos = 0;
        Backgrnd.mapX2bpPos = 0;
        Backgrnd.mapX3bpPos = 0;

        Backgrnd.map1YDelay = 1;
        Backgrnd.map1YDelayMax = 1;
        Backgrnd.map2YDelay = 1;
        Backgrnd.map2YDelayMax = 1;

        musicFade = false;

        Backgrnd.backPos = 0;
        Backgrnd.backPos2 = 0;
        Backgrnd.backPos3 = 0;
        Config.power = 0;
        Backgrnd.starfield_speed = 1;

        /* Setup player ship graphics */
        Varz.JE_getShipInfo();

        for (int i = 0; i < 2; ++i)
        {
            player[i].x_velocity = 0;
            player[i].y_velocity = 0;

            player[i].invulnerable_ticks = 100;
        }

        /* Initialize Level Data and Debug Mode */
        Varz.levelEnd = 255;
        Varz.levelEndWarp = -4;
        Varz.levelEndFxWait = 0;
        Fonthand.warningCol = 120;
        Fonthand.warningColChange = 1;
        Fonthand.warningSoundDelay = 0;
        Fonthand.armorShipDelay = 50;

        Episodes.bonusLevel = false;
        readyToEndLevel = false;
        Varz.firstGameOver = true;
        eventLoc = 1;
        curLoc = 0;
        Backgrnd.backMove = 1;
        Backgrnd.backMove2 = 2;
        Backgrnd.backMove3 = 3;
        explodeMove = 2;
        enemiesActive = true;
        for (int t = 0; t < 3; t++)
            Mainint.button[t] = false;
        stopBackgrounds = false;
        stopBackgroundNum = 0;
        background3x1 = false;
        background3x1b = false;
        Config.background3over = 0;
        Config.background2over = 1;
        Config.topEnemyOver = false;
        Config.skyEnemyOverAll = false;
        smallEnemyAdjust = false;
        Config.starActive = true;
        enemyContinualDamage = false;
        levelEnemyFrequency = 96;
        quitRequested = false;

        for (int i = 0; i < 2; i++)
            Varz.boss_bar[i].link_num = 0;

        forceEvents = false;  /*Force events to continue if background movement = 0*/

        superEnemy254Jump = 0;   /*When Enemy with PL 254 dies*/

        /* Filter Status */
        Config.filterActive = true;
        Config.filterFade = true;
        Config.filterFadeStart = false;
        Config.levelFilter = -99;
        Config.levelBrightness = -14;
        Config.levelBrightnessChg = 1;

        Config.background2notTransparent = false;

        old_weapon_bar[0] = 0;
        old_weapon_bar[1] = 0;

        /* Initially erase power bars */
        Config.lastPower = Config.power / 10;

        /* Initial Text */
        Mainint.JE_drawTextWindow(Helptext.miscText[20]);

        /* Setup Armor/Shield Data */
        Config.shieldWait = 1;
        Config.shieldT = (byte)(Episodes.shields[player[0].items.shield].tpwr * 20);

        for (int i = 0; i < 2; ++i)
        {
            player[i].shield = Episodes.shields[player[i].items.shield].mpwr;
            player[i].shield_max = player[i].shield * 2;
        }

        Varz.JE_drawShield();
        Varz.JE_drawArmor();

        for (int i = 0; i < 2; ++i)
            player[i].superbombs = 0;

        /* Set cubes to 0 */
        Config.cubeMax = 0;

        /* Secret Level Display */
        flash = 0;
        flashChange = 1;
        Varz.displayTime = 0;

        Loudness.play_song((uint)(levelSong - 1));

        Mainint.JE_drawPortConfigButtons();

        /* --- MAIN LOOP --- */
        // 網路（isNetworkGame）不移植：JE_clearSpecialRequests / mt_srand 略過

        Backgrnd.initialize_starfield();

        Config.JE_setNewGameSpeed();

        Loudness.set_volume((byte)Nortsong.tyrMusicVolume, (byte)Nortsong.fxVolume);

        /*Save backup game*/
        if (!Varz.play_demo && !doNotSaveBackup)
        {
            Config.JE_saveGame(Config.twoPlayerMode ? (byte)22 : (byte)11, "LAST LEVEL    ");
        }

        // TODO: 待移植 demo 錄製（record_demo 寫檔，tyrian2.c 931-980）

        Config.twoPlayerLinked = false;
        Config.linkGunDirec = MathF.PI;

        for (int i = 0; i < 2; ++i)
            Players.calc_purple_balls_needed(player[i]);

        damageRate = 2;  /*Normal Rate for Collision Damage*/

        Varz.chargeWait   = 5;
        Varz.chargeLevel  = 0;
        Varz.chargeMax    = 5;
        Varz.chargeGr     = 0;
        Varz.chargeGrWait = 3;

        Config.portConfigChange = false;

        /*Destruction Ratio*/
        totalEnemy = 0;
        enemyKilled = 0;

        astralDuration = 0;

        Config.superArcadePowerUp = 1;

        yourInGameMenuRequest = false;

        Params.constantLastX = -1;

        for (int i = 0; i < 2; ++i)
            player[i].exploding_ticks = 0;

        // if (isNetworkGame) JE_loadItemDat();  // 網路不移植

        for (int i = 0; i < Varz.enemyAvail.Length; i++)
            Varz.enemyAvail[i] = 1;
        for (int i = 0; i < Varz.enemyShotAvail.Length; i++)
            Varz.enemyShotAvail[i] = true;

        /*Initialize Shots*/
        Array.Clear(Shots.playerShotData);
        Array.Clear(Shots.shotAvail);
        Array.Clear(Config.shotMultiPos);
        for (int i = 0; i < Config.shotRepeat.Length; i++)
            Config.shotRepeat[i] = 1;

        Array.Clear(Mainint.button);

        Array.Clear(globalFlags);

        Array.Clear(Varz.explosions);
        Array.Clear(Varz.rep_explosions);

        /* --- Clear Sound Queue --- */
        Array.Clear(Varz.soundQueue);
        Varz.soundQueue[3] = (byte)Sndmast.V_GOOD_LUCK;

        Array.Clear(Sprites.enemySpriteSheetIds);
        Array.Clear(Varz.enemy);

        Array.Clear(Varz.SFCurrentCode);
        Array.Clear(Varz.SFExecuted);

        zinglonDuration = 0;
        Varz.specialWait = 0;
        nextSpecialWait = 0;
        Varz.optionAttachmentMove  = 0;    /*Launch the Attachments!*/
        Varz.optionAttachmentLinked = true;

        editShip1 = false;
        editShip2 = false;

        Array.Clear(Config.smoothies);

        Varz.levelTimer = false;
        randomExplosions = false;

        Varz.last_superpixel = 0;
        Array.Clear(Varz.superpixels);

        returnActive = false;

        galagaShotFreq = 0;

        if (Config.galagaMode)
        {
            Config.difficultyLevel = Config.DIFFICULTY_NORMAL;
        }
        galagaLife = 10000;

        Varz.JE_drawOptionLevel();

        // keeps map from scrolling past the top
        Backgrnd.BKwrap1Idx = Backgrnd.BKwrap1toIdx = 1 * 14;   // &megaData1.mainmap[1][0]
        Backgrnd.BKwrap2Idx = Backgrnd.BKwrap2toIdx = 1 * 14;   // &megaData2.mainmap[1][0]
        Backgrnd.BKwrap3Idx = Backgrnd.BKwrap3toIdx = 1 * 15;   // &megaData3.mainmap[1][0]

    level_loop:

        // 網路 smoothies 同步（isNetworkGame）不移植；走 else 分支
        Config.starShowVGASpecialCode = (byte)((Config.smoothies[9 - 1] ? 1 : 0) + ((Config.smoothies[6 - 1] ? 1 : 0) << 1));

        /*Background Wrapping*/
        if (Backgrnd.mapYPosIdx <= Backgrnd.BKwrap1Idx)
            Backgrnd.mapYPosIdx = Backgrnd.BKwrap1toIdx;
        if (Backgrnd.mapY2PosIdx <= Backgrnd.BKwrap2Idx)
            Backgrnd.mapY2PosIdx = Backgrnd.BKwrap2toIdx;
        if (Backgrnd.mapY3PosIdx <= Backgrnd.BKwrap3Idx)
            Backgrnd.mapY3PosIdx = Backgrnd.BKwrap3toIdx;

        allPlayersGone = Players.all_players_dead() &&
                         ((player[0].Lives == 1 && player[0].exploding_ticks == 0) || (!Config.onePlayerAction && !Config.twoPlayerMode)) &&
                         ((player[1].Lives == 1 && player[1].exploding_ticks == 0) || !Config.twoPlayerMode);

        /*-----MUSIC FADE------*/
        if (musicFade)
        {
            if (Nortsong.tempVolume > 10)
            {
                Nortsong.tempVolume--;
                Loudness.set_volume((byte)Nortsong.tempVolume, (byte)Nortsong.fxVolume);
            }
            else
            {
                musicFade = false;
            }
        }

        if (!allPlayersGone && Varz.levelEnd > 0 && endLevel)
        {
            Loudness.play_song(9);
            musicFade = false;
        }
        else if (!Loudness.playing && Varz.firstGameOver)
        {
            Loudness.play_song((uint)(levelSong - 1));
        }

        if (!endLevel) // draw HUD
        {
            Video.VGAScreen = Video.VGAScreenSeg; /* side-effect of game_screen */

            /*-----------------------Message Bar------------------------*/
            if (Mainint.textErase > 0 && --Mainint.textErase == 0)
                Sprites.blit_sprite(Video.VGAScreenSeg, 16, 189, (uint)Sprites.OPTION_SHAPES, 36);  // in-game message area

            /*------------------------Shield Gen-------------------------*/
            if (Config.galagaMode)
            {
                for (int i = 0; i < 2; ++i)
                    player[i].shield = 0;

                // spawned dragonwing died :(
                if (player[1].Lives == 0 || player[1].armor == 0)
                    Config.twoPlayerMode = false;

                if (player[0].cash >= (uint)galagaLife)
                {
                    Varz.soundQueue[6] = (byte)Sndmast.S_EXPLOSION_11;
                    Varz.soundQueue[7] = (byte)Sndmast.S_SOUL_OF_ZINGLON;

                    if (player[0].Lives < 11)
                        player[0].Lives++;
                    else
                        player[0].cash += 1000;

                    if (galagaLife == 10000)
                        galagaLife = 20000;
                    else
                        galagaLife += 25000;
                }
            }
            else // not galagaMode
            {
                if (Config.twoPlayerMode)
                {
                    if (--Config.shieldWait == 0)
                    {
                        Config.shieldWait = 15;

                        for (int i = 0; i < 2; ++i)
                        {
                            if (player[i].shield < player[i].shield_max && player[i].is_alive)
                                ++player[i].shield;
                        }

                        Varz.JE_drawShield();
                    }
                }
                else if (player[0].is_alive && player[0].shield < player[0].shield_max && Config.power > Config.shieldT)
                {
                    if (--Config.shieldWait == 0)
                    {
                        Config.shieldWait = 15;

                        Config.power -= Config.shieldT;

                        ++player[0].shield;
                        if (player[1].shield < player[0].shield_max)
                            ++player[1].shield;

                        Varz.JE_drawShield();
                    }
                }
            }

            /*---------------------Weapon Display-------------------------*/
            for (int i = 0; i < 2; ++i)
            {
                uint item_power = player[Config.twoPlayerMode ? i : 0].items.weapon[i].power;

                if (old_weapon_bar[i] != item_power)
                {
                    old_weapon_bar[i] = item_power;

                    int x = Config.twoPlayerMode ? 286 : 289,
                        y = (i == 0) ? (Config.twoPlayerMode ? 6 : 17) : (Config.twoPlayerMode ? 100 : 38);

                    Vga256d.fill_rectangle_xy(Video.VGAScreenSeg, x, y, x + 1 + 10 * 2, y + 2, 0);

                    for (uint j = 1; j <= item_power; ++j)
                    {
                        Vga256d.JE_rectangle(Video.VGAScreen, x, y, x + 1, y + 2, (int)(115 + j)); /* SEGa000 */
                        x += 2;
                    }
                }
            }

            /*------------------------Power Bar-------------------------*/
            if (Config.twoPlayerMode || Config.onePlayerAction)
            {
                Config.power = 900;
            }
            else
            {
                Config.power += Config.powerAdd;
                if (Config.power > 900)
                    Config.power = 900;

                int tempPow = (int)(Config.power / 10);
                int lastP = (int)Config.lastPower;

                if (tempPow != lastP)
                {
                    if (tempPow > lastP)
                        Vga256d.fill_rectangle_xy(Video.VGAScreenSeg, 269, 113 - 11 - tempPow, 276, 114 - 11 - lastP, (byte)(113 + tempPow / 7));
                    else
                        Vga256d.fill_rectangle_xy(Video.VGAScreenSeg, 269, 113 - 11 - lastP, 276, 114 - 11 - tempPow, 0);

                    Config.lastPower = (uint)tempPow;
                }
            }

            Backgrnd.oldMapX3Ofs = Backgrnd.mapX3Ofs;

            enemyOnScreen = 0;
        }

        /* use game_screen for all the generic drawing functions */
        Video.VGAScreen = Video.game_screen;

        /*---------------------------EVENTS-------------------------*/
        while (eventRec[eventLoc - 1].eventtime <= curLoc && eventLoc <= maxEvent)
            JE_eventSystem();

        // if (isNetworkGame && reallyEndLevel) goto start_level;  // 網路不移植

        /* SMOOTHIES! */
        Mainint.JE_checkSmoothies();
        if (Backgrnd.anySmoothies)
            Video.VGAScreen = Video.VGAScreen2;  // this makes things complicated, but we do it anyway :(

        /* --- BACKGROUNDS --- */
        /* --- BACKGROUND 1 --- */

        if (forceEvents && Backgrnd.backMove == 0)
            curLoc++;

        if (Backgrnd.map1YDelayMax > 1 && Backgrnd.backMove < 2)
            Backgrnd.backMove = (ushort)((Backgrnd.map1YDelay == 1) ? 1 : 0);

        /*Draw background*/
        if (astralDuration == 0)
            Backgrnd.draw_background_1(Video.VGAScreen);
        else
            Video.JE_clr256(Video.VGAScreen);

        /*Set Movement of background 1*/
        if (--Backgrnd.map1YDelay == 0)
        {
            Backgrnd.map1YDelay = Backgrnd.map1YDelayMax;

            curLoc += Backgrnd.backMove;

            Backgrnd.backPos += Backgrnd.backMove;

            if (Backgrnd.backPos > 27)
            {
                Backgrnd.backPos -= 28;
                Backgrnd.mapY--;
                Backgrnd.mapYPosIdx -= 14;  /*Map Width*/
            }
        }

        if (Config.starActive || astralDuration > 0)
            Backgrnd.update_and_draw_starfield(Video.VGAScreen, Backgrnd.starfield_speed);

        if (Config.processorType > 1 && Config.smoothies[5 - 1])
        {
            Mainint.iced_blur_filter(Video.game_screen, Video.VGAScreen);
            Video.VGAScreen = Video.game_screen;
        }

        /*-----------------------BACKGROUNDS------------------------*/
        /*-----------------------BACKGROUND 2------------------------*/
        if (Config.background2over == 3)
        {
            Backgrnd.draw_background_2(Video.VGAScreen);
            Config.background2 = true;
        }

        if (Config.background2over == 0)
        {
            if (!(Config.smoothies[2 - 1] && Config.processorType < 4) && !(Config.smoothies[1 - 1] && Config.processorType == 3))
            {
                if (Config.wild && !Config.background2notTransparent)
                    Backgrnd.draw_background_2_blend(Video.VGAScreen);
                else
                    Backgrnd.draw_background_2(Video.VGAScreen);
            }
        }

        if (Config.smoothies[0] && Config.processorType > 2 && Backgrnd.smoothie_data[0] == 0)
        {
            Mainint.lava_filter(Video.game_screen, Video.VGAScreen);
            Video.VGAScreen = Video.game_screen;
        }
        if (Config.smoothies[2 - 1] && Config.processorType > 2)
        {
            Mainint.water_filter(Video.game_screen, Video.VGAScreen);
            Video.VGAScreen = Video.game_screen;
        }

        /*-----------------------Ground Enemy------------------------*/
        lastEnemyOnScreen = enemyOnScreen;

        Backgrnd.tempMapXOfs = Backgrnd.mapXOfs;
        Backgrnd.tempBackMove = Backgrnd.backMove;
        Tyrian2.JE_drawEnemy(50);
        Tyrian2.JE_drawEnemy(100);

        if (enemyOnScreen == 0 || enemyOnScreen == lastEnemyOnScreen)
        {
            if (stopBackgroundNum == 1)
                stopBackgroundNum = 9;
        }

        if (Config.smoothies[0] && Config.processorType > 2 && Backgrnd.smoothie_data[0] > 0)
        {
            Mainint.lava_filter(Video.game_screen, Video.VGAScreen);
            Video.VGAScreen = Video.game_screen;
        }

        if (Config.superWild)
        {
            Backgrnd.neat += 3;
            Backgrnd.JE_darkenBackground(Backgrnd.neat);
        }

        /*-----------------------BACKGROUNDS------------------------*/
        /*-----------------------BACKGROUND 2------------------------*/
        if (!(Config.smoothies[2 - 1] && Config.processorType < 4) &&
            !(Config.smoothies[1 - 1] && Config.processorType == 3))
        {
            if (Config.background2over == 1)
            {
                if (Config.wild && !Config.background2notTransparent)
                    Backgrnd.draw_background_2_blend(Video.VGAScreen);
                else
                    Backgrnd.draw_background_2(Video.VGAScreen);
            }
        }

        if (Config.superWild)
        {
            Backgrnd.neat++;
            Backgrnd.JE_darkenBackground(Backgrnd.neat);
        }

        if (Config.background3over == 2)
            Backgrnd.draw_background_3(Video.VGAScreen);

        /* New Enemy */
        if (enemiesActive && MtRand.mt_rand() % 100 > levelEnemyFrequency)
        {
            ushort tempW = levelEnemy[MtRand.mt_rand() % levelEnemyMax];
            if (tempW == 2)
                Varz.soundQueue[3] = (byte)Sndmast.S_WEAPON_7;
            JE_newEnemy(0, tempW, 0);
        }

        if (Config.processorType > 1 && Config.smoothies[3 - 1])
        {
            Mainint.iced_blur_filter(Video.game_screen, Video.VGAScreen);
            Video.VGAScreen = Video.game_screen;
        }
        if (Config.processorType > 1 && Config.smoothies[4 - 1])
        {
            Mainint.blur_filter(Video.game_screen, Video.VGAScreen);
            Video.VGAScreen = Video.game_screen;
        }

        /* Draw Sky Enemy */
        if (!Config.skyEnemyOverAll)
        {
            lastEnemyOnScreen = enemyOnScreen;

            Backgrnd.tempMapXOfs = Backgrnd.mapX2Ofs;
            Backgrnd.tempBackMove = 0;
            Tyrian2.JE_drawEnemy(25);

            if (enemyOnScreen == lastEnemyOnScreen)
            {
                if (stopBackgroundNum == 2)
                    stopBackgroundNum = 9;
            }
        }

        if (Config.background3over == 0)
            Backgrnd.draw_background_3(Video.VGAScreen);

        /* Draw Top Enemy */
        if (!Config.topEnemyOver)
        {
            Backgrnd.tempMapXOfs = (!background3x1) ? Backgrnd.oldMapX3Ofs : Backgrnd.mapXOfs;
            Backgrnd.tempBackMove = Backgrnd.backMove3;
            Tyrian2.JE_drawEnemy(75);
        }

        /* Player Shot Images */
        for (int z = 0; z < Shots.MAX_PWEAPON; z++)
        {
            if (Shots.shotAvail[z] != 0)
            {
                bool is_special;
                int tempShotX, tempShotY;
                byte chain;
                byte playerNum;
                ushort tempX2, tempY2;
                short damage;
                byte temp2;

                if (!Shots.player_shot_move_and_draw(z, out is_special, out tempShotX, out tempShotY, out damage, out temp2, out chain, out playerNum, out tempX2, out tempY2))
                {
                    goto draw_player_shot_loop_end;
                }

                for (int b = 0; b < 100; b++)
                {
                    if (Varz.enemyAvail[b] == 0)
                    {
                        bool collided;
                        int temp;

                        if (z == Shots.MAX_PWEAPON - 1)
                        {
                            temp = 25 - Math.Abs(zinglonDuration - 25);
                            collided = Math.Abs(Varz.enemy[b].ex + Varz.enemy[b].mapoffset - (player[0].x + 7)) < temp;
                            temp2 = 9;
                            chain = 0;
                            damage = 10;
                        }
                        else if (is_special)
                        {
                            collided = ((Varz.enemy[b].enemycycle == 0) &&
                                        (Math.Abs(Varz.enemy[b].ex + Varz.enemy[b].mapoffset - tempShotX - tempX2) < (25 + tempX2)) &&
                                        (Math.Abs(Varz.enemy[b].ey - tempShotY - 12 - tempY2)                       < (29 + tempY2))) ||
                                       ((Varz.enemy[b].enemycycle > 0) &&
                                        (Math.Abs(Varz.enemy[b].ex + Varz.enemy[b].mapoffset - tempShotX - tempX2) < (13 + tempX2)) &&
                                        (Math.Abs(Varz.enemy[b].ey - tempShotY - 6 - tempY2)                        < (15 + tempY2)));
                        }
                        else
                        {
                            collided = ((Varz.enemy[b].enemycycle == 0) &&
                                        (Math.Abs(Varz.enemy[b].ex + Varz.enemy[b].mapoffset - tempShotX) < 25) && (Math.Abs(Varz.enemy[b].ey - tempShotY - 12) < 29)) ||
                                       ((Varz.enemy[b].enemycycle > 0) &&
                                        (Math.Abs(Varz.enemy[b].ex + Varz.enemy[b].mapoffset - tempShotX) < 13) && (Math.Abs(Varz.enemy[b].ey - tempShotY - 6) < 15));
                        }

                        if (collided)
                        {
                            if (chain > 0)
                            {
                                Config.shotMultiPos[Config.SHOT_MISC] = 0;
                                b = Shots.player_shot_create(0, (uint)Config.SHOT_MISC, (ushort)tempShotX, (ushort)tempShotY, player[0].mouseX, player[0].mouseY, chain, playerNum);
                                Shots.shotAvail[z] = 0;
                                goto draw_player_shot_loop_end;
                            }

                            bool infiniteShot = false;
                            int doIced;

                            if (damage == 99)
                            {
                                damage = 0;
                                doIced = 40;
                                Varz.enemy[b].iced = 40;
                            }
                            else
                            {
                                doIced = 0;
                                if (damage >= 250)
                                {
                                    damage = (short)(damage - 250);
                                    infiniteShot = true;
                                }
                            }

                            int armorleft = Varz.enemy[b].armorleft;

                            temp = Varz.enemy[b].linknum;
                            if (temp == 0)
                                temp = 255;

                            if (Varz.enemy[b].armorleft < 255)
                            {
                                for (int i = 0; i < 2; i++)
                                    if (temp == Varz.boss_bar[i].link_num)
                                        Varz.boss_bar[i].color = 6;

                                if (Varz.enemy[b].enemyground)
                                    Varz.enemy[b].filter = temp2;

                                for (int e = 0; e < 100; e++)
                                {
                                    if (Varz.enemy[e].linknum == temp &&
                                        Varz.enemyAvail[e] != 1 &&
                                        Varz.enemy[e].enemyground)
                                    {
                                        if (doIced != 0)
                                            Varz.enemy[e].iced = (byte)doIced;
                                        Varz.enemy[e].filter = temp2;
                                    }
                                }
                            }

                            if (armorleft > damage)
                            {
                                if (z != Shots.MAX_PWEAPON - 1)
                                {
                                    if (Varz.enemy[b].armorleft != 255)
                                    {
                                        Varz.enemy[b].armorleft -= (byte)damage;
                                        Varz.JE_setupExplosion(tempShotX, tempShotY, 0, 0, false, false);
                                    }
                                    else
                                    {
                                        Varz.JE_doSP((ushort)(tempShotX + 6), (ushort)(tempShotY + 6), (ushort)(damage / 2 + 3), (byte)(damage / 4 + 2), temp2);
                                    }
                                }

                                Varz.soundQueue[5] = (byte)Sndmast.S_ENEMY_HIT;

                                if ((armorleft - damage <= Varz.enemy[b].edlevel) &&
                                    ((!Varz.enemy[b].edamaged) ^ (Varz.enemy[b].edani < 0)))
                                {
                                    for (int t3 = 0; t3 < 100; t3++)
                                    {
                                        if (Varz.enemyAvail[t3] != 1)
                                        {
                                            int linknum = Varz.enemy[t3].linknum;
                                            if (
                                                 (t3 == b) ||
                                                 (
                                                   (temp != 255) &&
                                                   (
                                                     ((Varz.enemy[t3].edlevel > 0) && (linknum == temp)) ||
                                                     (
                                                       (enemyContinualDamage && (temp - 100 == linknum)) ||
                                                       ((linknum > 40) && (linknum / 20 == temp / 20) && (linknum <= temp))
                                                     )
                                                   )
                                                 )
                                               )
                                            {
                                                Varz.enemy[t3].enemycycle = 1;

                                                Varz.enemy[t3].edamaged = !Varz.enemy[t3].edamaged;

                                                if (Varz.enemy[t3].edani != 0)
                                                {
                                                    Varz.enemy[t3].ani = (byte)Math.Abs(Varz.enemy[t3].edani);
                                                    Varz.enemy[t3].aniactive = 1;
                                                    Varz.enemy[t3].animax = 0;
                                                    Varz.enemy[t3].animin = (byte)Varz.enemy[t3].edgr;
                                                    Varz.enemy[t3].enemycycle = (byte)(Varz.enemy[t3].animin - 1);
                                                }
                                                else if (Varz.enemy[t3].edgr > 0)
                                                {
                                                    Varz.enemy[t3].egr[1 - 1] = Varz.enemy[t3].edgr;
                                                    Varz.enemy[t3].ani = 1;
                                                    Varz.enemy[t3].aniactive = 0;
                                                    Varz.enemy[t3].animax = 0;
                                                    Varz.enemy[t3].animin = 1;
                                                }
                                                else
                                                {
                                                    Varz.enemyAvail[t3] = 1;
                                                    enemyKilled++;
                                                }

                                                Varz.enemy[t3].aniwhenfire = 0;

                                                if (Varz.enemy[t3].armorleft > (byte)Varz.enemy[t3].edlevel)
                                                    Varz.enemy[t3].armorleft = (byte)Varz.enemy[t3].edlevel;

                                                int tempX = Varz.enemy[t3].ex + Varz.enemy[t3].mapoffset;
                                                int tempY = Varz.enemy[t3].ey;

                                                if (Episodes.enemyDat[Varz.enemy[t3].enemytype].esize != 1)
                                                    Varz.JE_setupExplosion(tempX, tempY - 6, 0, 1, false, false);
                                                else
                                                    Varz.JE_setupExplosionLarge(Varz.enemy[t3].enemyground, (byte)(Varz.enemy[t3].explonum / 2), tempX, tempY);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if ((temp == 254) && (superEnemy254Jump > 0))
                                    JE_eventJump(superEnemy254Jump);

                                for (int t2 = 0; t2 < 100; t2++)
                                {
                                    if (Varz.enemyAvail[t2] != 1)
                                    {
                                        int temp3 = Varz.enemy[t2].linknum;
                                        if ((t2 == b) || (temp == 254) ||
                                            ((temp != 255) && ((temp == temp3) || (temp - 100 == temp3) ||
                                                               ((temp3 > 40) && (temp3 / 20 == temp / 20) && (temp3 <= temp)))))
                                        {
                                            int enemy_screen_x = Varz.enemy[t2].ex + Varz.enemy[t2].mapoffset;

                                            if (Varz.enemy[t2].special)
                                            {
                                                globalFlags[Varz.enemy[t2].flagnum - 1] = Varz.enemy[t2].setto;
                                            }

                                            if ((Varz.enemy[t2].enemydie > 0) &&
                                                !((Config.superArcadeMode != VarzConst.SA_NONE) &&
                                                  (Episodes.enemyDat[Varz.enemy[t2].enemydie].value == 30000)))
                                            {
                                                int temp_b = b;
                                                ushort tempW = Varz.enemy[t2].enemydie;
                                                int enemy_offset = t2 - (t2 % 25);
                                                if (Episodes.enemyDat[tempW].value > 30000)
                                                {
                                                    enemy_offset = 0;
                                                }
                                                b = JE_newEnemy(enemy_offset, tempW, 0);
                                                if (b != 0)
                                                {
                                                    if ((Config.superArcadeMode != VarzConst.SA_NONE) && (Varz.enemy[b - 1].evalue > 30000))
                                                    {
                                                        Config.superArcadePowerUp++;
                                                        if (Config.superArcadePowerUp > 5)
                                                            Config.superArcadePowerUp = 1;
                                                        Varz.enemy[b - 1].egr[1 - 1] = (ushort)(5 + Config.superArcadePowerUp * 2);
                                                        Varz.enemy[b - 1].evalue = (short)(30000 + Config.superArcadePowerUp);
                                                    }

                                                    if (Varz.enemy[b - 1].evalue != 0)
                                                        Varz.enemy[b - 1].scoreitem = true;
                                                    else
                                                        Varz.enemy[b - 1].scoreitem = false;

                                                    Varz.enemy[b - 1].ex = Varz.enemy[t2].ex;
                                                    Varz.enemy[b - 1].ey = Varz.enemy[t2].ey;
                                                }
                                                b = temp_b;
                                            }

                                            if ((Varz.enemy[t2].evalue > 0) && (Varz.enemy[t2].evalue < 10000))
                                            {
                                                if (Varz.enemy[t2].evalue == 1)
                                                {
                                                    Config.cubeMax++;
                                                }
                                                else
                                                {
                                                    // in galaga mode player 2 is sidekick, so give cash to player 1
                                                    player[Config.galagaMode ? 0 : playerNum - 1].cash += (uint)Varz.enemy[t2].evalue;
                                                }
                                            }

                                            if ((Varz.enemy[t2].edlevel == -1) && (temp == temp3))
                                            {
                                                Varz.enemy[t2].edlevel = 0;
                                                Varz.enemyAvail[t2] = 2;
                                                Varz.enemy[t2].egr[1 - 1] = Varz.enemy[t2].edgr;
                                                Varz.enemy[t2].ani = 1;
                                                Varz.enemy[t2].aniactive = 0;
                                                Varz.enemy[t2].animax = 0;
                                                Varz.enemy[t2].animin = 1;
                                                Varz.enemy[t2].edamaged = true;
                                                Varz.enemy[t2].enemycycle = 1;
                                            }
                                            else
                                            {
                                                Varz.enemyAvail[t2] = 1;
                                                enemyKilled++;
                                            }

                                            if (Episodes.enemyDat[Varz.enemy[t2].enemytype].esize == 1)
                                            {
                                                Varz.JE_setupExplosionLarge(Varz.enemy[t2].enemyground, Varz.enemy[t2].explonum, enemy_screen_x, Varz.enemy[t2].ey);
                                                Varz.soundQueue[6] = (byte)Sndmast.S_EXPLOSION_9;
                                            }
                                            else
                                            {
                                                Varz.JE_setupExplosion(enemy_screen_x, Varz.enemy[t2].ey, 0, 1, false, false);
                                                Varz.soundQueue[6] = (byte)Sndmast.S_EXPLOSION_8;
                                            }
                                        }
                                    }
                                }
                            }

                            if (infiniteShot)
                            {
                                damage += 250;
                            }
                            else if (z != Shots.MAX_PWEAPON - 1)
                            {
                                if (damage <= armorleft)
                                {
                                    Shots.shotAvail[z] = 0;
                                    goto draw_player_shot_loop_end;
                                }
                                else
                                {
                                    Shots.playerShotData[z].shotDmg -= (byte)armorleft;
                                }
                            }
                        }
                    }
                }

            draw_player_shot_loop_end:
                ;
            }
        }

        /* Player movement indicators for shots that track your ship */
        for (int i = 0; i < 2; ++i)
        {
            player[i].last_x_shot_move = player[i].x;
            player[i].last_y_shot_move = player[i].y;
        }

        /*=================================*/
        /*=======Collisions Detection======*/
        /*=================================*/

        for (int i = 0; i < (Config.twoPlayerMode ? 2 : 1); ++i)
            if (player[i].is_alive && !endLevel)
                Mainint.JE_playerCollide(player[i], (byte)(i + 1));

        if (Varz.firstGameOver)
            Mainint.JE_mainGamePlayerFunctions();      /*--------PLAYER DRAW+MOVEMENT---------*/

        if (!endLevel)
        {    /*MAIN DRAWING IS STOPPED STARTING HERE*/

            /* Draw Enemy Shots */
            Tyrian2.simulateEnemyShots();
        }

        if (Config.background3over == 1)
            Backgrnd.draw_background_3(Video.VGAScreen);

        /* Draw Top Enemy */
        if (Config.topEnemyOver)
        {
            Backgrnd.tempMapXOfs = (!background3x1) ? Backgrnd.oldMapX3Ofs : Backgrnd.oldMapXOfs;
            Backgrnd.tempBackMove = Backgrnd.backMove3;
            Tyrian2.JE_drawEnemy(75);
        }

        /* Draw Sky Enemy */
        if (Config.skyEnemyOverAll)
        {
            lastEnemyOnScreen = enemyOnScreen;

            Backgrnd.tempMapXOfs = Backgrnd.mapX2Ofs;
            Backgrnd.tempBackMove = 0;
            Tyrian2.JE_drawEnemy(25);

            if (enemyOnScreen == lastEnemyOnScreen)
            {
                if (stopBackgroundNum == 2)
                    stopBackgroundNum = 9;
            }
        }

        /*------- Sequenced Explosions + Draw Explosions -------*/
        Tyrian2.JE_drawExplosions();

        if (!Config.portConfigChange)
            Config.portConfigDone = true;

        /*-----------------------BACKGROUNDS------------------------*/
        /*-----------------------BACKGROUND 2------------------------*/
        if (!(Config.smoothies[2 - 1] && Config.processorType < 4) &&
            !(Config.smoothies[1 - 1] && Config.processorType == 3))
        {
            if (Config.background2over == 2)
            {
                if (Config.wild && !Config.background2notTransparent)
                    Backgrnd.draw_background_2_blend(Video.VGAScreen);
                else
                    Backgrnd.draw_background_2(Video.VGAScreen);
            }
        }

        /*-------------------------Warning---------------------------*/
        if ((player[0].is_alive && player[0].armor < 6) ||
            (Config.twoPlayerMode && !Config.galagaMode && player[1].is_alive && player[1].armor < 6))
        {
            int armor_amount = (int)((player[0].is_alive && player[0].armor < 6) ? player[0].armor : player[1].armor);

            if (Fonthand.armorShipDelay > 0)
            {
                Fonthand.armorShipDelay--;
            }
            else
            {
                int b = JE_newEnemy(50, 560, 0);
                if (b > 0)
                {
                    Varz.enemy[b - 1].enemydie = (ushort)(560 + (MtRand.mt_rand() % 3) + 1);
                    Varz.enemy[b - 1].eyc -= (sbyte)Backgrnd.backMove3;
                    Varz.enemy[b - 1].armorleft = 4;
                }
                Fonthand.armorShipDelay = 500;
            }

            // 原版含 isNetworkGame/thisPlayerNum 條件；網路不移植 → 化簡為非網路情形
            if ((player[0].is_alive && player[0].armor < 6) ||
                (Config.twoPlayerMode && player[1].is_alive && player[1].armor < 6))
            {
                int tempW = armor_amount * 4 + 8;
                if (Fonthand.warningSoundDelay > tempW)
                    Fonthand.warningSoundDelay = (byte)tempW;

                if (Fonthand.warningSoundDelay > 1)
                {
                    Fonthand.warningSoundDelay--;
                }
                else
                {
                    Varz.soundQueue[7] = (byte)Sndmast.S_WARNING;
                    Fonthand.warningSoundDelay = (byte)tempW;
                }

                Fonthand.warningCol += (byte)Fonthand.warningColChange;
                if (Fonthand.warningCol > 113 + (14 - (armor_amount * 2)))
                {
                    Fonthand.warningColChange = (sbyte)-Fonthand.warningColChange;
                    Fonthand.warningCol = (byte)(113 + (14 - (armor_amount * 2)));
                }
                else if (Fonthand.warningCol < 113)
                {
                    Fonthand.warningColChange = (sbyte)-Fonthand.warningColChange;
                }
                Vga256d.fill_rectangle_xy(Video.VGAScreen, 24, 181, 138, 183, Fonthand.warningCol);
                Vga256d.fill_rectangle_xy(Video.VGAScreen, 175, 181, 287, 183, Fonthand.warningCol);
                Vga256d.fill_rectangle_xy(Video.VGAScreen, 24, 0, 287, 3, Fonthand.warningCol);

                Fonthand.JE_outText(Video.VGAScreen, 140, 178, "WARNING", 7, (Fonthand.warningCol % 16) / 2);
            }
        }

        /*------- Random Explosions --------*/
        if (randomExplosions && MtRand.mt_rand() % 10 == 1)
            Varz.JE_setupExplosionLarge(false, 20, (int)(MtRand.mt_rand() % 280), (int)(MtRand.mt_rand() % 180));

        /*=================================*/
        /*=======The Sound Routine=========*/
        /*=================================*/
        if (Varz.firstGameOver)
        {
            for (int t2 = 0; t2 < Varz.soundQueue.Length; t2++)
            {
                if (Varz.soundQueue[t2] != Sndmast.S_NONE)
                {
                    int snd = Varz.soundQueue[t2];
                    int vol;
                    if (t2 == 3)
                        vol = Nortsong.fxPlayVol;
                    else if (snd == 15)
                        vol = Nortsong.fxPlayVol / 4;
                    else   /*Lightning*/
                        vol = Nortsong.fxPlayVol / 2;

                    Loudness.multiSamplePlay(Nortsong.soundSamples[snd - 1], Nortsong.soundSampleCount[snd - 1], (byte)t2, (byte)vol);

                    Varz.soundQueue[t2] = (byte)Sndmast.S_NONE;
                }
            }
        }

        if (returnActive && enemyOnScreen == 0)
        {
            JE_eventJump(65535);
            returnActive = false;
        }

        /*-------      DEbug      ---------*/
        debugTime = Globals.Clock.Ticks;

        if (debug)
        {
            // TODO: 待移植 除錯顯示（smoothies/記憶體/FPS/座標 outText）
            debugHist += debugTime - lastDebugTime;
            debugHistCount++;
            lastDebugTime = debugTime;
        }

        if (Varz.displayTime > 0)
        {
            Varz.displayTime--;
            Fonthand.JE_outTextAndDarken(Video.VGAScreen, 90, 10, Helptext.miscText[59], 15, (uint)(byte)(flash - 8), (uint)Sprites.FONT_SHAPES);
            flash += (byte)flashChange;
            if (flash > 4 || flash == 0)
                flashChange = (sbyte)-flashChange;
        }

        /*Pentium Speed Mode?*/
        if (Config.pentiumMode)
        {
            Nortsong.frameCountMax = (ushort)((Nortsong.frameCountMax == 2) ? 3 : 2);
        }

        /*--------  Level Timer    ---------*/
        if (Varz.levelTimer && levelTimerCountdown > 0)
        {
            levelTimerCountdown--;
            if (levelTimerCountdown == 0)
                JE_eventJump(levelTimerJumpTo);

            if (levelTimerCountdown > 200)
            {
                if (levelTimerCountdown % 100 == 0)
                    Varz.soundQueue[7] = (byte)Sndmast.S_WARNING;

                if (levelTimerCountdown % 10 == 0)
                    Varz.soundQueue[6] = (byte)Sndmast.S_CLICK;
            }
            else if (levelTimerCountdown % 20 == 0)
            {
                Varz.soundQueue[7] = (byte)Sndmast.S_WARNING;
            }

            Fonthand.JE_textShade(Video.VGAScreen, 140, 6, Helptext.miscText[66], 7, (levelTimerCountdown % 20) / 3, (uint)Fonthand.FULL_SHADE);
            Fonthand.JE_dString(Video.VGAScreen, 100, 2, (levelTimerCountdown / 100.0f).ToString("0.0"), (uint)Sprites.SMALL_FONT_SHAPES);
        }

        /*GAME OVER*/
        if (!Params.constantPlay && !Params.constantDie)
        {
            if (allPlayersGone)
            {
                if (player[0].exploding_ticks > 0 || player[1].exploding_ticks > 0)
                {
                    if (Config.galagaMode)
                        player[1].exploding_ticks = 0;

                    musicFade = true;
                }
                else
                {
                    if (Varz.play_demo || normalBonusLevelCurrent || bonusLevelCurrent)
                        reallyEndLevel = true;
                    else
                        Fonthand.JE_dString(Video.VGAScreen, 120, 60, Helptext.miscText[21], (uint)Sprites.FONT_SHAPES); // game over

                    if (Varz.firstGameOver)
                    {
                        if (!Varz.play_demo)
                        {
                            Loudness.play_song(Musmast.SONG_GAMEOVER);
                            Loudness.set_volume((byte)Nortsong.tyrMusicVolume, (byte)Nortsong.fxVolume);
                        }
                        Varz.firstGameOver = false;
                    }

                    if (!Varz.play_demo)
                    {
                        Joystick.push_joysticks_as_keyboard();
                        Keyboard.handleSdlEvents();

                        if (Keyboard.hasInput(InputFlags.INPUT_NO_MOTION))
                            reallyEndLevel = true;
                    }
                }
            }
        }

        if (Varz.play_demo) // input kills demo
        {
            Joystick.push_joysticks_as_keyboard();
            Keyboard.handleSdlEvents();

            if (Keyboard.hasInput(InputFlags.INPUT_NO_MOTION))
            {
                reallyEndLevel = true;

                Varz.stopped_demo = true;
            }
        }
        else // input handling for pausing, menu, cheats
        {
            Keyboard.handleSdlEvents();

            // Ensure gameplay input does not affect pause or menu.
            Keyboard.mouseClearInput();

            if (Keyboard.keyboardHasInput())
            {
                // Pause, menu, and cheats are triggered on keysactive, so this is fine.
                Keyboard.keyboardClearInput();

                Varz.skipStarShowVGA = false;
                Mainint.JE_mainKeyboardInput();
                if (Varz.skipStarShowVGA)
                    goto level_loop;
            }

            if (Mainint.pause_pressed || !Keyboard.windowHasFocus)
            {
                Mainint.pause_pressed = false;

                Mainint.JE_pauseGame();
            }

            if (Mainint.ingamemenu_pressed)
            {
                Mainint.ingamemenu_pressed = false;

                yourInGameMenuRequest = true;
                Mainint.JE_doInGameSetup();
                Varz.skipStarShowVGA = true;
            }
        }

        /*Network Update*/  // 網路（WITH_NETWORK）不移植

        /** Test **/
        Varz.JE_drawSP();

        /*Filtration*/
        if (Config.filterActive)
        {
            Mainint.JE_filterScreen(Config.levelFilter, Config.levelBrightness);
        }

        Tyrian2.draw_boss_bar();

        Mainint.JE_inGameDisplays();

        Video.VGAScreen = Video.VGAScreenSeg; /* side-effect of game_screen */

        JE_starShowVGA();

        /*Start backgrounds if no enemies on screen
          End level if number of enemies left to kill equals 0.*/
        if (stopBackgroundNum == 9 && Backgrnd.backMove == 0 && !Varz.enemyStillExploding)
        {
            Backgrnd.backMove = 1;
            Backgrnd.backMove2 = 2;
            Backgrnd.backMove3 = 3;
            explodeMove = 2;
            stopBackgroundNum = 0;
            stopBackgrounds = false;
            if (waitToEndLevel)
            {
                endLevel = true;
                Varz.levelEnd = 40;
            }
            if (allPlayersGone)
            {
                reallyEndLevel = true;
            }
        }

        if (!endLevel && enemyOnScreen == 0)
        {
            if (readyToEndLevel && !Varz.enemyStillExploding)
            {
                if (levelTimerCountdown > 0)
                {
                    Varz.levelTimer = false;
                }
                readyToEndLevel = false;
                endLevel = true;
                Varz.levelEnd = 40;
                if (allPlayersGone)
                {
                    reallyEndLevel = true;
                }
            }
            if (stopBackgrounds)
            {
                stopBackgrounds = false;
                Backgrnd.backMove = 1;
                Backgrnd.backMove2 = 2;
                Backgrnd.backMove3 = 3;
                explodeMove = 2;
            }
        }

        /*Other Network Functions*/
        Mainint.JE_handleChat(); // 網路不移植（空殼）

        if (reallyEndLevel)
        {
            goto start_level;
        }
        goto level_loop;
    }
}
