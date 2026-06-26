namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/tyrian2.c 的 JE_eventSystem —— 關卡事件分派器（依 curLoc 觸發 eventRec）。
/// 已逐行對照移植 case 1-82（原版未定義的 58/59 等落入 default）。
/// </summary>
internal static unsafe partial class Tyrian2
{
    public static void JE_eventSystem()
    {
        ref JE_EventRecType ev = ref eventRec[eventLoc - 1];

        switch (ev.eventtype)
        {
            case 1:
                Backgrnd.starfield_speed = ev.eventdat;
                break;

            case 2:
                Backgrnd.map1YDelay = 1; Backgrnd.map1YDelayMax = 1;
                Backgrnd.map2YDelay = 1; Backgrnd.map2YDelayMax = 1;
                Backgrnd.backMove = (ushort)ev.eventdat;
                Backgrnd.backMove2 = (ushort)ev.eventdat2;
                explodeMove = Backgrnd.backMove2 > 0 ? Backgrnd.backMove2 : Backgrnd.backMove;
                Backgrnd.backMove3 = (ushort)ev.eventdat3;
                if (Backgrnd.backMove > 0)
                    stopBackgroundNum = 0;
                break;

            case 3:
                Backgrnd.backMove = 1; Backgrnd.map1YDelay = 3; Backgrnd.map1YDelayMax = 3;
                Backgrnd.backMove2 = 1; Backgrnd.map2YDelay = 2; Backgrnd.map2YDelayMax = 2;
                Backgrnd.backMove3 = 1;
                break;

            case 4:
                stopBackgrounds = true;
                switch (ev.eventdat)
                {
                    case 0:
                    case 1: stopBackgroundNum = 1; break;
                    case 2: stopBackgroundNum = 2; break;
                    case 3: stopBackgroundNum = 3; break;
                }
                break;

            case 5: // load enemy shape banks
                {
                    byte[] tabs =
                    {
                        ev.eventdat  > 0 ? (byte)ev.eventdat  : (byte)0,
                        ev.eventdat2 > 0 ? (byte)ev.eventdat2 : (byte)0,
                        ev.eventdat3 > 0 ? (byte)ev.eventdat3 : (byte)0,
                        ev.eventdat4 > 0 ? (byte)ev.eventdat4 : (byte)0,
                    };
                    for (int i = 0; i < tabs.Length; ++i)
                    {
                        if (Sprites.enemySpriteSheetIds[i] != tabs[i])
                        {
                            if (tabs[i] > 0)
                                Sprites.JE_loadCompShapes(ref Sprites.enemySpriteSheets[i], Lvlmast.shapeFile[tabs[i] - 1]);
                            else
                                Sprites.free_sprite2s(ref Sprites.enemySpriteSheets[i]);
                            Sprites.enemySpriteSheetIds[i] = tabs[i];
                        }
                    }
                }
                break;

            case 6: // Ground Enemy
                JE_createNewEventEnemy(0, 25, 0);
                break;

            case 7: // Top Enemy
                JE_createNewEventEnemy(0, 50, 0);
                break;

            case 8:
                Config.starActive = false;
                break;

            case 9:
                Config.starActive = true;
                break;

            case 10: // Ground Enemy 2
                JE_createNewEventEnemy(0, 75, 0);
                break;

            case 11:
                if (allPlayersGone || ev.eventdat == 1)
                {
                    reallyEndLevel = true;
                }
                else if (!endLevel)
                {
                    readyToEndLevel = false;
                    endLevel = true;
                    Varz.levelEnd = 40;
                }
                break;

            case 12: // Custom 4x4 Ground Enemy
                {
                    uint temp = 0;
                    switch (ev.eventdat6)
                    {
                        case 0:
                        case 1: temp = 25; break;
                        case 2: temp = 0; break;
                        case 3: temp = 50; break;
                        case 4: temp = 75; break;
                    }
                    ev.eventdat6 = 0;   // We use EVENTDAT6 for the background
                    JE_createNewEventEnemy(0, (ushort)temp, 0);
                    JE_createNewEventEnemy(1, (ushort)temp, 0);
                    if (Varz.b > 0)
                        Varz.enemy[Varz.b - 1].ex += 24;
                    JE_createNewEventEnemy(2, (ushort)temp, 0);
                    if (Varz.b > 0)
                        Varz.enemy[Varz.b - 1].ey -= 28;
                    JE_createNewEventEnemy(3, (ushort)temp, 0);
                    if (Varz.b > 0)
                    {
                        Varz.enemy[Varz.b - 1].ex += 24;
                        Varz.enemy[Varz.b - 1].ey -= 28;
                    }
                }
                break;

            case 13:
                enemiesActive = false;
                break;

            case 14:
                enemiesActive = true;
                break;

            case 15: // Sky Enemy
                JE_createNewEventEnemy(0, 0, 0);
                break;

            case 16:
                if (ev.eventdat > 9)
                {
                    // warning: event 16: bad event data
                }
                else
                {
                    Mainint.JE_drawTextWindow(Helptext.outputs[ev.eventdat - 1]);
                    Varz.soundQueue[3] = Sndmast.windowTextSamples[ev.eventdat - 1];
                }
                break;

            case 17: // Ground Bottom
                JE_createNewEventEnemy(0, 25, 0);
                if (Varz.b > 0)
                    Varz.enemy[Varz.b - 1].ey = (short)(190 + ev.eventdat5);
                break;

            case 18: // Sky Enemy on Bottom
                JE_createNewEventEnemy(0, 0, 0);
                if (Varz.b > 0)
                    Varz.enemy[Varz.b - 1].ey = (short)(190 + ev.eventdat5);
                break;

            case 19: // Enemy Global Move
                {
                    int initial_i = 0, max_i = 0;
                    bool all_enemies = false;

                    if (ev.eventdat3 > 79 && ev.eventdat3 < 90)
                    {
                        initial_i = 0;
                        max_i = 100;
                        all_enemies = false;
                        ev.eventdat4 = newPL[ev.eventdat3 - 80];
                    }
                    else
                    {
                        switch (ev.eventdat3)
                        {
                            case 0: initial_i = 0; max_i = 100; all_enemies = false; break;
                            case 2: initial_i = 0; max_i = 25; all_enemies = true; break;
                            case 1: initial_i = 25; max_i = 50; all_enemies = true; break;
                            case 3: initial_i = 50; max_i = 75; all_enemies = true; break;
                            case 99: initial_i = 0; max_i = 100; all_enemies = true; break;
                        }
                    }

                    for (int i = initial_i; i < max_i; i++)
                    {
                        if (all_enemies || Varz.enemy[i].linknum == ev.eventdat4)
                        {
                            if (ev.eventdat != -99)
                                Varz.enemy[i].exc = (sbyte)ev.eventdat;

                            if (ev.eventdat2 != -99)
                                Varz.enemy[i].eyc = (sbyte)ev.eventdat2;

                            if (ev.eventdat6 != 0)
                                Varz.enemy[i].fixedmovey = ev.eventdat6;

                            if (ev.eventdat6 == -99)
                                Varz.enemy[i].fixedmovey = 0;

                            if (ev.eventdat5 > 0)
                                Varz.enemy[i].enemycycle = (byte)ev.eventdat5;
                        }
                    }
                }
                break;

            case 20: // Enemy Global Accel
                {
                    if (ev.eventdat3 > 79 && ev.eventdat3 < 90)
                        ev.eventdat4 = newPL[ev.eventdat3 - 80];

                    for (int temp = 0; temp < 100; temp++)
                    {
                        if (Varz.enemyAvail[temp] != 1 &&
                            (Varz.enemy[temp].linknum == ev.eventdat4 || ev.eventdat4 == 0))
                        {
                            if (ev.eventdat != -99)
                            {
                                Varz.enemy[temp].excc = (sbyte)ev.eventdat;
                                Varz.enemy[temp].exccw = (sbyte)Math.Abs((int)ev.eventdat);
                                Varz.enemy[temp].exccwmax = (byte)Math.Abs((int)ev.eventdat);
                                if (ev.eventdat > 0)
                                    Varz.enemy[temp].exccadd = 1;
                                else
                                    Varz.enemy[temp].exccadd = -1;
                            }

                            if (ev.eventdat2 != -99)
                            {
                                Varz.enemy[temp].eycc = (sbyte)ev.eventdat2;
                                Varz.enemy[temp].eyccw = (sbyte)Math.Abs((int)ev.eventdat2);
                                Varz.enemy[temp].eyccwmax = (byte)Math.Abs((int)ev.eventdat2);
                                if (ev.eventdat2 > 0)
                                    Varz.enemy[temp].eyccadd = 1;
                                else
                                    Varz.enemy[temp].eyccadd = -1;
                            }

                            if (ev.eventdat5 > 0)
                            {
                                Varz.enemy[temp].enemycycle = (byte)ev.eventdat5;
                            }
                            if (ev.eventdat6 > 0)
                            {
                                Varz.enemy[temp].ani = (byte)ev.eventdat6;
                                Varz.enemy[temp].animin = (byte)ev.eventdat5;
                                Varz.enemy[temp].animax = 0;
                                Varz.enemy[temp].aniactive = 1;
                            }
                        }
                    }
                }
                break;

            case 21:
                Config.background3over = 1;
                break;

            case 22:
                Config.background3over = 0;
                break;

            case 23: // Sky Enemy on Bottom
                JE_createNewEventEnemy(0, 50, 0);
                if (Varz.b > 0)
                    Varz.enemy[Varz.b - 1].ey = (short)(180 + ev.eventdat5);
                break;

            case 24: // Enemy Global Animate
                {
                    for (int temp = 0; temp < 100; temp++)
                    {
                        if (Varz.enemy[temp].linknum == ev.eventdat4)
                        {
                            Varz.enemy[temp].aniactive = 1;
                            Varz.enemy[temp].aniwhenfire = 0;
                            if (ev.eventdat2 > 0)
                            {
                                Varz.enemy[temp].enemycycle = (byte)ev.eventdat2;
                                Varz.enemy[temp].animin = Varz.enemy[temp].enemycycle;
                            }
                            else
                            {
                                Varz.enemy[temp].enemycycle = 0;
                            }

                            if (ev.eventdat > 0)
                                Varz.enemy[temp].ani = (byte)ev.eventdat;

                            if (ev.eventdat3 == 1)
                            {
                                Varz.enemy[temp].animax = Varz.enemy[temp].ani;
                            }
                            else if (ev.eventdat3 == 2)
                            {
                                Varz.enemy[temp].aniactive = 2;
                                Varz.enemy[temp].animax = Varz.enemy[temp].ani;
                                Varz.enemy[temp].aniwhenfire = 2;
                            }
                        }
                    }
                }
                break;

            case 25: // Enemy Global Damage change
                {
                    for (int temp = 0; temp < 100; temp++)
                    {
                        if (ev.eventdat4 == 0 || Varz.enemy[temp].linknum == ev.eventdat4)
                        {
                            if (Config.galagaMode)
                                Varz.enemy[temp].armorleft = (byte)MathF.Round(ev.eventdat * (Config.difficultyLevel / 2));
                            else
                                Varz.enemy[temp].armorleft = (byte)ev.eventdat;
                        }
                    }
                }
                break;

            case 26:
                smallEnemyAdjust = ev.eventdat != 0;
                break;

            case 27: // Enemy Global AccelRev
                {
                    if (ev.eventdat3 > 79 && ev.eventdat3 < 90)
                        ev.eventdat4 = newPL[ev.eventdat3 - 80];

                    for (int temp = 0; temp < 100; temp++)
                    {
                        if (ev.eventdat4 == 0 || Varz.enemy[temp].linknum == ev.eventdat4)
                        {
                            if (ev.eventdat != -99)
                                Varz.enemy[temp].exrev = (sbyte)ev.eventdat;
                            if (ev.eventdat2 != -99)
                                Varz.enemy[temp].eyrev = (sbyte)ev.eventdat2;
                            if (ev.eventdat3 != 0 && ev.eventdat3 < 17)
                                Varz.enemy[temp].filter = (byte)ev.eventdat3;
                        }
                    }
                }
                break;

            case 28:
                Config.topEnemyOver = false;
                break;

            case 29:
                Config.topEnemyOver = true;
                break;

            case 30:
                Backgrnd.map1YDelay = 1; Backgrnd.map1YDelayMax = 1;
                Backgrnd.map2YDelay = 1; Backgrnd.map2YDelayMax = 1;

                Backgrnd.backMove = (ushort)ev.eventdat;
                Backgrnd.backMove2 = (ushort)ev.eventdat2;
                explodeMove = Backgrnd.backMove2;
                Backgrnd.backMove3 = (ushort)ev.eventdat3;
                break;

            case 31: // Enemy Fire Override
                {
                    for (int temp = 0; temp < 100; temp++)
                    {
                        if (ev.eventdat4 == 99 || Varz.enemy[temp].linknum == ev.eventdat4)
                        {
                            Varz.enemy[temp].freq[1 - 1] = (byte)ev.eventdat;
                            Varz.enemy[temp].freq[2 - 1] = (byte)ev.eventdat2;
                            Varz.enemy[temp].freq[3 - 1] = (byte)ev.eventdat3;
                            for (int temp2 = 0; temp2 < 3; temp2++)
                            {
                                Varz.enemy[temp].eshotwait[temp2] = 1;
                            }
                            if (Varz.enemy[temp].launchtype > 0)
                            {
                                Varz.enemy[temp].launchfreq = (byte)ev.eventdat5;
                                Varz.enemy[temp].launchwait = 1;
                            }
                        }
                    }
                }
                break;

            case 32: // create enemy
                JE_createNewEventEnemy(0, 50, 0);
                if (Varz.b > 0)
                    Varz.enemy[Varz.b - 1].ey = 190;
                break;

            case 33: // Enemy From other Enemies
                {
                    if (!((ev.eventdat == 512 || ev.eventdat == 513) && (Config.twoPlayerMode || Config.onePlayerAction || Config.superTyrian)))
                    {
                        if (Config.superArcadeMode != VarzConst.SA_NONE)
                        {
                            if (ev.eventdat == 534)
                                ev.eventdat = 827;
                        }
                        else if (!Config.superTyrian)
                        {
                            byte lives = Players.player[0].Lives;

                            if (ev.eventdat == 533 && (lives == 11 || (MtRand.mt_rand() % 15) < lives))
                            {
                                // enemy will drop random special weapon
                                ev.eventdat = (short)(829 + (MtRand.mt_rand() % 6));
                            }
                        }
                        if (ev.eventdat == 534 && Config.superTyrian)
                            ev.eventdat = (short)(828 + Varz.superTyrianSpecials[MtRand.mt_rand() % 4]);

                        for (int temp = 0; temp < 100; temp++)
                        {
                            if (Varz.enemy[temp].linknum == ev.eventdat4)
                                Varz.enemy[temp].enemydie = (ushort)ev.eventdat;
                        }
                    }
                }
                break;

            case 34: // Start Music Fade
                if (Varz.firstGameOver)
                {
                    musicFade = true;
                    Nortsong.tempVolume = Nortsong.tyrMusicVolume;
                }
                break;

            case 35: // Play new song
                if (Varz.firstGameOver)
                {
                    Loudness.play_song((uint)(ev.eventdat - 1));
                    Loudness.set_volume((byte)Nortsong.tyrMusicVolume, (byte)Nortsong.fxVolume);
                }
                musicFade = false;
                break;

            case 36:
                readyToEndLevel = true;
                break;

            case 37:
                levelEnemyFrequency = (ushort)ev.eventdat;
                break;

            case 38:
                {
                    curLoc = (ushort)ev.eventdat;
                    int new_event_loc = 1;
                    for (int tempW = 0; tempW < maxEvent; tempW++)
                    {
                        if (eventRec[tempW].eventtime <= curLoc)
                            new_event_loc = tempW + 1 - 1;
                    }
                    eventLoc = (ushort)new_event_loc;
                }
                break;

            case 39: // Enemy Global Linknum Change
                {
                    for (int temp = 0; temp < 100; temp++)
                    {
                        if (Varz.enemy[temp].linknum == ev.eventdat)
                            Varz.enemy[temp].linknum = (byte)ev.eventdat2;
                    }
                }
                break;

            case 40: // Enemy Continual Damage
                enemyContinualDamage = true;
                break;

            case 41:
                if (ev.eventdat == 0)
                {
                    Array.Fill(Varz.enemyAvail, (byte)1);
                }
                else
                {
                    for (int x = 0; x <= 24; x++)
                        Varz.enemyAvail[x] = 1;
                }
                break;

            case 42:
                Config.background3over = 2;
                break;

            case 43:
                Config.background2over = (byte)ev.eventdat;
                break;

            case 44:
                Config.filterActive    = (ev.eventdat > 0);
                Config.filterFade      = (ev.eventdat == 2);
                Config.levelFilter     = (sbyte)ev.eventdat2;
                Config.levelBrightness = (sbyte)ev.eventdat3;
                Config.levelFilterNew  = (sbyte)ev.eventdat4;
                Config.levelBrightnessChg = (sbyte)ev.eventdat5;
                Config.filterFadeStart = (ev.eventdat6 == 0);
                break;

            case 45: // arcade-only enemy from other enemies
                {
                    if (!Config.superTyrian)
                    {
                        byte lives = Players.player[0].Lives;

                        if (ev.eventdat == 533 && (lives == 11 || (MtRand.mt_rand() % 15) < lives))
                        {
                            ev.eventdat = (short)(829 + (MtRand.mt_rand() % 6));
                        }
                        if (Config.twoPlayerMode || Config.onePlayerAction)
                        {
                            for (int temp = 0; temp < 100; temp++)
                            {
                                if (Varz.enemy[temp].linknum == ev.eventdat4)
                                    Varz.enemy[temp].enemydie = (ushort)ev.eventdat;
                            }
                        }
                    }
                }
                break;

            case 46: // change difficulty
                if (ev.eventdat3 != 0)
                    damageRate = (byte)ev.eventdat3;

                if (ev.eventdat2 == 0 || Config.twoPlayerMode || Config.onePlayerAction)
                {
                    Config.difficultyLevel = (sbyte)(Config.difficultyLevel + ev.eventdat);
                    if (Config.difficultyLevel < Config.DIFFICULTY_EASY)
                        Config.difficultyLevel = (sbyte)Config.DIFFICULTY_EASY;
                    if (Config.difficultyLevel > Config.DIFFICULTY_10)
                        Config.difficultyLevel = (sbyte)Config.DIFFICULTY_10;
                }
                break;

            case 47: // Enemy Global AccelRev
                {
                    for (int temp = 0; temp < 100; temp++)
                    {
                        if (ev.eventdat4 == 0 || Varz.enemy[temp].linknum == ev.eventdat4)
                            Varz.enemy[temp].armorleft = (byte)ev.eventdat;
                    }
                }
                break;

            case 48: // Background 2 Cannot be Transparent
                Config.background2notTransparent = true;
                break;

            case 49:
            case 50:
            case 51:
            case 52:
                {
                    short tempDat2 = ev.eventdat;
                    ev.eventdat = 0;
                    sbyte tempDat = ev.eventdat3;
                    ev.eventdat3 = 0;
                    sbyte tempDat3 = ev.eventdat6;
                    ev.eventdat6 = 0;
                    Episodes.enemyDat[0].armor = (byte)tempDat3;
                    Episodes.enemyDat[0].egraphic[1 - 1] = (ushort)tempDat2;
                    uint temp = 0;
                    switch (ev.eventtype - 48)
                    {
                        case 1: temp = 25; break;
                        case 2: temp = 0; break;
                        case 3: temp = 50; break;
                        case 4: temp = 75; break;
                    }
                    JE_createNewEventEnemy(0, (ushort)temp, tempDat);
                    ev.eventdat = tempDat2;
                    ev.eventdat3 = tempDat;
                    ev.eventdat6 = tempDat3;
                }
                break;

            case 53:
                forceEvents = (ev.eventdat != 99);
                break;

            case 54:
                JE_eventJump((ushort)ev.eventdat);
                break;

            case 55: // Enemy Global AccelRev
                {
                    if (ev.eventdat3 > 79 && ev.eventdat3 < 90)
                        ev.eventdat4 = newPL[ev.eventdat3 - 80];

                    for (int temp = 0; temp < 100; temp++)
                    {
                        if (ev.eventdat4 == 0 || Varz.enemy[temp].linknum == ev.eventdat4)
                        {
                            if (ev.eventdat != -99)
                                Varz.enemy[temp].xaccel = (byte)ev.eventdat;
                            if (ev.eventdat2 != -99)
                                Varz.enemy[temp].yaccel = (byte)ev.eventdat2;
                        }
                    }
                }
                break;

            case 56: // Ground2 Bottom
                JE_createNewEventEnemy(0, 75, 0);
                if (Varz.b > 0)
                    Varz.enemy[Varz.b - 1].ey = 190;
                break;

            case 57:
                superEnemy254Jump = (ushort)ev.eventdat;
                break;

            case 60: // Assign Special Enemy
                {
                    for (int temp = 0; temp < 100; temp++)
                    {
                        if (Varz.enemy[temp].linknum == ev.eventdat4)
                        {
                            Varz.enemy[temp].special = true;
                            Varz.enemy[temp].flagnum = (byte)ev.eventdat;
                            Varz.enemy[temp].setto = (ev.eventdat2 == 1);
                        }
                    }
                }
                break;

            case 61: // if specific flag set to specific value, skip events
                if ((globalFlags[ev.eventdat - 1] ? 1 : 0) == ev.eventdat2)
                    eventLoc = (ushort)(eventLoc + ev.eventdat3);
                break;

            case 62: // Play sound effect
                Varz.soundQueue[3] = (byte)ev.eventdat;
                break;

            case 63: // skip events if not in 2-player mode
                if (!Config.twoPlayerMode && !Config.onePlayerAction)
                    eventLoc = (ushort)(eventLoc + ev.eventdat);
                break;

            case 64:
                if (!(ev.eventdat == 6 && Config.twoPlayerMode && Config.difficultyLevel > Config.DIFFICULTY_NORMAL))
                {
                    Config.smoothies[ev.eventdat - 1] = (ev.eventdat2 != 0);
                    int temp = ev.eventdat;
                    if (temp == 5)
                        temp = 3;
                    Backgrnd.smoothie_data[temp - 1] = (byte)ev.eventdat3;
                }
                break;

            case 65:
                background3x1 = (ev.eventdat == 0);
                break;

            case 66: // If not on this difficulty level or higher then...
                if (Config.initialDifficulty <= ev.eventdat)
                    eventLoc = (ushort)(eventLoc + ev.eventdat2);
                break;

            case 67:
                Varz.levelTimer = (ev.eventdat == 1);
                levelTimerCountdown = (ushort)(ev.eventdat3 * 100);
                levelTimerJumpTo = (ushort)ev.eventdat2;
                break;

            case 68:
                randomExplosions = (ev.eventdat == 1);
                break;

            case 69:
                for (int i = 0; i < Players.player.Length; ++i)
                    Players.player[i].invulnerable_ticks = (uint)ev.eventdat;
                break;

            case 70:
                if (ev.eventdat2 == 0)
                {  // 1-10
                    bool found = false;

                    for (int temp = 1; temp <= 19; temp++)
                        found = found || JE_searchFor((byte)temp);

                    if (!found)
                        JE_eventJump((ushort)ev.eventdat);
                }
                else if (!JE_searchFor((byte)ev.eventdat2) &&
                         (ev.eventdat3 == 0 || !JE_searchFor((byte)ev.eventdat3)) &&
                         (ev.eventdat4 == 0 || !JE_searchFor(ev.eventdat4)))
                {
                    JE_eventJump((ushort)ev.eventdat);
                }
                break;

            case 71:
                if ((uint)(Backgrnd.mapYPosIdx * 2) <= (uint)ev.eventdat2)
                    JE_eventJump((ushort)ev.eventdat);
                break;

            case 72:
                background3x1b = (ev.eventdat == 1);
                break;

            case 73:
                Config.skyEnemyOverAll = (ev.eventdat == 1);
                break;

            case 74: // Enemy Global BounceParams
                {
                    for (int temp = 0; temp < 100; temp++)
                    {
                        if (ev.eventdat4 == 0 || Varz.enemy[temp].linknum == ev.eventdat4)
                        {
                            if (ev.eventdat5 != -99)
                                Varz.enemy[temp].xminbounce = ev.eventdat5;

                            if (ev.eventdat6 != -99)
                                Varz.enemy[temp].yminbounce = ev.eventdat6;

                            if (ev.eventdat != -99)
                                Varz.enemy[temp].xmaxbounce = ev.eventdat;

                            if (ev.eventdat2 != -99)
                                Varz.enemy[temp].ymaxbounce = ev.eventdat2;
                        }
                    }
                }
                break;

            case 75:
                {
                    bool temp_no_clue = false; // TODO: figure out what this is doing

                    for (int temp = 0; temp < 100; temp++)
                    {
                        if (Varz.enemyAvail[temp] == 0 &&
                            Varz.enemy[temp].eyc == 0 &&
                            Varz.enemy[temp].linknum >= ev.eventdat &&
                            Varz.enemy[temp].linknum <= ev.eventdat2)
                        {
                            temp_no_clue = true;
                        }
                    }

                    if (temp_no_clue)
                    {
                        int temp;
                        byte enemy_i;
                        do
                        {
                            temp = (int)(MtRand.mt_rand() % (uint)(ev.eventdat2 + 1 - ev.eventdat)) + ev.eventdat;
                        } while (!(JE_searchFor((byte)temp, out enemy_i) && Varz.enemy[enemy_i].eyc == 0));

                        newPL[ev.eventdat3 - 80] = (byte)temp;
                    }
                    else
                    {
                        newPL[ev.eventdat3 - 80] = 255;
                        if (ev.eventdat4 > 0)
                        { // Skip
                            curLoc = (ushort)(eventRec[eventLoc - 1 + ev.eventdat4].eventtime - 1);
                            eventLoc = (ushort)(eventLoc + ev.eventdat4 - 1);
                        }
                    }
                }
                break;

            case 76:
                returnActive = true;
                break;

            case 77:
                Backgrnd.mapYPosIdx = ev.eventdat / 2;
                if (ev.eventdat2 > 0)
                {
                    Backgrnd.mapY2PosIdx = ev.eventdat2 / 2;
                }
                else
                {
                    Backgrnd.mapY2PosIdx = ev.eventdat / 2;
                }
                break;

            case 78:
                if (galagaShotFreq < 10)
                    galagaShotFreq++;
                break;

            case 79: // 設定 boss 血條
                Varz.boss_bar[0].link_num = (byte)ev.eventdat;
                Varz.boss_bar[1].link_num = (byte)ev.eventdat2;
                break;

            case 80: // skip events if in 2-player mode
                if (Config.twoPlayerMode)
                    eventLoc = (ushort)(eventLoc + ev.eventdat);
                break;

            case 81: // WRAP2
                Backgrnd.BKwrap2Idx   = ev.eventdat / 2;
                Backgrnd.BKwrap2toIdx = ev.eventdat2 / 2;
                break;

            case 82: // Give SPECIAL WEAPON
                Players.player[0].items.special = (byte)ev.eventdat;
                Config.shotMultiPos[Config.SHOT_SPECIAL] = 0;
                Config.shotRepeat[Config.SHOT_SPECIAL] = 0;
                Config.shotMultiPos[Config.SHOT_SPECIAL2] = 0;
                Config.shotRepeat[Config.SHOT_SPECIAL2] = 0;
                break;

            default:
                // TODO: 其餘事件型別（敵人生成 5/6/7、地面敵人、boss、背景 wrap…）待敵人系統移植
                break;
        }

        eventLoc++;
    }
}
