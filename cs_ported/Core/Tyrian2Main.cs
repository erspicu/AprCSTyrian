namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/tyrian2.c 的 JE_main —— 遊戲主迴圈。
/// **目前為骨架**：忠實移植關卡 setup（JE_loadMap + map 位置/玩家/背景初始化），
/// 主迴圈先做「捲動背景」最小版本；完整遊戲邏輯（事件系統/敵人 AI/玩家移動/射擊/碰撞/HUD）待後續。
/// </summary>
internal static unsafe partial class Tyrian2
{
    public static void JE_main()
    {
        var player = Players.player;

        // play_demo 播放待移植，先略過（回到標題）
        if (Varz.play_demo)
        {
            Varz.play_demo = false;
            return;
        }

        int levelEndGrace = 80;

    start_level_first: // 對應 opentyr.c/tyrian2.c 的 start_level（過關後載入下一關）
        // --- start_level_first ---
        Loudness.set_volume((byte)Nortsong.tyrMusicVolume, (byte)Nortsong.fxVolume);

        endLevel = false;
        reallyEndLevel = false;
        playerEndLevel = false;
        Config.extraGame = false;
        doNotSaveBackup = false;

        JE_loadMap();

        if (Config.mainLevel == 0)  // quit itemscreen
            return;                 // back to titlescreen

        // 地圖捲動起始位置（對應 tyrian2.c 778-806）
        Backgrnd.mapY = 300 - 8;
        Backgrnd.mapY2 = 600 - 8;
        Backgrnd.mapY3 = 600 - 8;
        Backgrnd.mapYPosIdx = Backgrnd.mapY * 14 - 1;
        Backgrnd.mapY2PosIdx = Backgrnd.mapY2 * 14 - 1;
        Backgrnd.mapY3PosIdx = Backgrnd.mapY3 * 15 - 1;
        Backgrnd.mapXPos = 0; Backgrnd.mapXOfs = 0;
        Backgrnd.mapX2Pos = 0; Backgrnd.mapX3Pos = 0; Backgrnd.mapX3Ofs = 0;
        Backgrnd.mapXbpPos = 0; Backgrnd.mapX2bpPos = 0; Backgrnd.mapX3bpPos = 0;
        Backgrnd.map1YDelay = 1; Backgrnd.map1YDelayMax = 1;
        Backgrnd.map2YDelay = 1; Backgrnd.map2YDelayMax = 1;
        Backgrnd.backPos = 0; Backgrnd.backPos2 = 0; Backgrnd.backPos3 = 0;
        Backgrnd.starfield_speed = 1;

        // 玩家船艦圖/初值
        if (Sprites.explosionSpriteSheet.data == null)
            Sprites.JE_loadCompShapes(ref Sprites.explosionSpriteSheet, '6'); // 爆炸圖

        Varz.JE_getShipInfo();
        for (int i = 0; i < 2; ++i)
        {
            player[i].x_velocity = 0;
            player[i].y_velocity = 0;
            player[i].invulnerable_ticks = 100;
            player[i].is_alive = true;
        }
        player[0].x = 100; player[0].y = 180;
        player[1].x = 200; player[1].y = 180;

        for (int i = 0; i < 100; ++i)
            Varz.enemyAvail[i] = 1; // 所有敵人槽初始為空（對應 memset enemyAvail,1）
        for (int i = 0; i < VarzConst.ENEMY_SHOT_MAX; ++i)
            Varz.enemyShotAvail[i] = true; // 所有敵彈槽初始為空

        // 護盾/裝甲初值（對應 tyrian2.c 880-888）
        for (int i = 0; i < 2; ++i)
        {
            player[i].shield = Episodes.shields[player[i].items.shield].mpwr;
            player[i].shield_max = player[i].shield * 2;
        }

        eventLoc = 1;
        curLoc = 0;
        Backgrnd.backMove = 1;
        Backgrnd.backMove2 = 2;
        Backgrnd.backMove3 = 3;
        Config.starActive = true;
        levelEnemyFrequency = 96;
        Config.cubeMax = 0;
        quitRequested = false;

        Loudness.play_song((uint)(levelSong > 0 ? levelSong - 1 : 0));
        Mainint.JE_drawPortConfigButtons();

        Config.JE_setNewGameSpeed();
        Loudness.set_volume((byte)Nortsong.tyrMusicVolume, (byte)Nortsong.fxVolume);

        // 套用目前調色盤（多為過場最後設定的關卡調色盤）
        Palette.set_palette(Palette.colors, 0, 255);

        Keyboard.keyboardClearInput();

        // === 最小遊戲主迴圈骨架：捲動三層背景 ===
        // TODO: 完整 JE_main 遊戲邏輯 —— 事件系統(eventRec/curLoc)、敵人生成/AI、
        //       玩家移動(player.c)、射擊(simulate_player_shots)、碰撞、HUD、關卡結束。
        levelEndGrace = 80;
        while (true)
        {
            Keyboard.handleSdlEvents();
            if (quitRequested || Keyboard.keysactive[SdlKeys.SDL_SCANCODE_ESCAPE])
                return; // ESC → 結束回標題

            Nortsong.setFrameCount(2);

            // --- 事件系統：觸發到期事件（對應 tyrian2.c 1252）---
            while (eventRec[eventLoc - 1].eventtime <= curLoc && eventLoc <= maxEvent)
                JE_eventSystem();

            // --- BACKGROUND 1 ---
            if (Backgrnd.map1YDelayMax > 1 && Backgrnd.backMove < 2)
                Backgrnd.backMove = (ushort)((Backgrnd.map1YDelay == 1) ? 1 : 0);

            Backgrnd.draw_background_1(Video.VGAScreen);

            if (--Backgrnd.map1YDelay == 0)
            {
                Backgrnd.map1YDelay = Backgrnd.map1YDelayMax;
                curLoc += Backgrnd.backMove;
                Backgrnd.backPos += Backgrnd.backMove;
                if (Backgrnd.backPos > 27)
                {
                    Backgrnd.backPos -= 28;
                    Backgrnd.mapY--;
                    Backgrnd.mapYPosIdx -= 14;
                }
            }

            // --- BACKGROUND 2 & 3 ---
            Backgrnd.draw_background_2(Video.VGAScreen);
            Backgrnd.draw_background_3(Video.VGAScreen);

            // === 敵人更新/繪製（homing/動畫/size 多格/立方加速/彈跳/砲塔發射）===
            Tyrian2.JE_updateEnemies();

            // === 簡化玩家控制（完整 JE_playerMovement 待移植：射擊/僚機/選項/爆炸/復活）===
            const int spd = 2; // CURRENT_KEY_SPEED=1，骨架略加快以利操作
            if (Keyboard.keysactive[Config.keySettings[Config.KEY_SETTING_UP]]) player[0].y -= spd;
            if (Keyboard.keysactive[Config.keySettings[Config.KEY_SETTING_DOWN]]) player[0].y += spd;
            if (Keyboard.keysactive[Config.keySettings[Config.KEY_SETTING_LEFT]]) player[0].x -= spd;
            if (Keyboard.keysactive[Config.keySettings[Config.KEY_SETTING_RIGHT]]) player[0].x += spd;
            if (player[0].x < 5) player[0].x = 5;
            if (player[0].x > 295) player[0].x = 295;
            if (player[0].y < 10) player[0].y = 10;
            if (player[0].y > 185) player[0].y = 185;

            // === 簡化玩家射擊（對應 JE_playerMovement 4186-4204 的前/後武器發射）===
            Mainint.button[0] = Keyboard.keysactive[Config.keySettings[Config.KEY_SETTING_FIRE]];
            player[0].mouseX = (ushort)player[0].x;
            player[0].mouseY = (ushort)player[0].y;

            for (int temp = 0; temp < 2; temp++)
            {
                int item = player[0].items.weapon[temp].id;
                if (item > 0)
                {
                    if (Config.shotRepeat[temp] > 0)
                    {
                        Config.shotRepeat[temp]--;
                    }
                    else if (Mainint.button[0])
                    {
                        int item_power = Config.galagaMode ? 0 : player[0].items.weapon[temp].power - 1;
                        int item_mode = (temp == Players.REAR_WEAPON) ? (int)player[0].weapon_mode - 1 : 0;
                        ushort wpNum = Episodes.weaponPort[item].op[item_mode * 11 + item_power];
                        Shots.player_shot_create((ushort)item, (uint)temp, (ushort)player[0].x, (ushort)player[0].y, player[0].mouseX, player[0].mouseY, wpNum, 1);
                    }
                }
            }

            // === 玩家子彈：移動/繪製 + 碰撞敵人（簡化 collision；完整版含 boss bar/連動敵人/edlevel 傷害態）===
            for (int z = 0; z < Shots.MAX_PWEAPON; z++)
            {
                if (Shots.shotAvail[z] == 0)
                    continue;
                if (!Shots.player_shot_move_and_draw(z, out _, out int sx, out int sy, out short dmg, out byte blastFilter, out _, out _, out _, out _))
                    continue;

                for (int b = 0; b < 100; b++)
                {
                    if (Varz.enemyAvail[b] != 0)
                        continue; // 只打 armored 活動敵人

                    int ax = Varz.enemy[b].ex + Varz.enemy[b].mapoffset;
                    bool collided =
                        (Varz.enemy[b].enemycycle == 0 && Math.Abs(ax - sx) < 25 && Math.Abs(Varz.enemy[b].ey - sy - 12) < 29) ||
                        (Varz.enemy[b].enemycycle > 0 && Math.Abs(ax - sx) < 13 && Math.Abs(Varz.enemy[b].ey - sy - 6) < 15);
                    if (!collided)
                        continue;

                    bool infiniteShot = false;
                    if (dmg == 99) { dmg = 0; Varz.enemy[b].iced = 40; }
                    else if (dmg >= 250) { dmg = (short)(dmg - 250); infiniteShot = true; }

                    // 受擊閃白（對應 tyrian2.c 1518-1519）
                    if (Varz.enemy[b].armorleft < 255 && Varz.enemy[b].enemyground)
                        Varz.enemy[b].filter = blastFilter;

                    int armorleft = Varz.enemy[b].armorleft;
                    if (armorleft != 255 && armorleft > dmg)
                    {
                        Varz.enemy[b].armorleft = (byte)(armorleft - dmg);
                        Varz.soundQueue[5] = (byte)Sndmast.S_ENEMY_HIT;
                        Varz.JE_setupExplosion(sx, sy, 0, 0, false, false);
                    }
                    else if (armorleft != 255)
                    {
                        // 敵人死亡：計分 + 爆炸 + 後繼敵人(enemydie)
                        if (Varz.enemy[b].evalue > 0 && Varz.enemy[b].evalue < 10000)
                        {
                            if (Varz.enemy[b].evalue == 1)
                                Config.cubeMax++;
                            else
                                Players.player[0].cash += (uint)Varz.enemy[b].evalue;
                        }

                        ushort edie = Varz.enemy[b].enemydie;
                        int deadEx = Varz.enemy[b].ex, deadEy = Varz.enemy[b].ey, deadOfs = Varz.enemy[b].mapoffset;

                        if (Episodes.enemyDat[Varz.enemy[b].enemytype].esize == 1)
                        {
                            Varz.JE_setupExplosionLarge(Varz.enemy[b].enemyground, Varz.enemy[b].explonum, ax, Varz.enemy[b].ey);
                            Varz.soundQueue[6] = (byte)Sndmast.S_EXPLOSION_9;
                        }
                        else
                        {
                            Varz.JE_setupExplosion(ax, Varz.enemy[b].ey, 0, 1, false, false);
                            Varz.soundQueue[6] = (byte)Sndmast.S_EXPLOSION_8;
                        }

                        Varz.enemyAvail[b] = 1;

                        // 生成後繼敵人（多階段敵人，如船艦被擊毀後分裂）
                        if (edie > 0 && !(Config.superArcadeMode != VarzConst.SA_NONE && Episodes.enemyDat[edie].value == 30000))
                        {
                            int offset = b - (b % 25);
                            if (Episodes.enemyDat[edie].value > 30000)
                                offset = 0;
                            int nb = Tyrian2.JE_newEnemy(offset, edie, 0);
                            if (nb != 0)
                            {
                                Varz.enemy[nb - 1].scoreitem = Varz.enemy[nb - 1].evalue != 0;
                                Varz.enemy[nb - 1].ex = (short)deadEx;
                                Varz.enemy[nb - 1].ey = (short)deadEy;
                                Varz.enemy[nb - 1].mapoffset = (ushort)deadOfs;
                            }
                        }
                    }

                    if (!infiniteShot)
                        Shots.shotAvail[z] = 0; // 移除子彈
                    break;
                }
            }

            // 繪製玩家船艦（對應 JE_playerMovement 的 shipGr blit）
            Sprites.blit_sprite2x2(Video.VGAScreen, player[0].x - 5, player[0].y - 7, Varz.shipGrPtr, Varz.shipGr);

            // === 敵彈：移動/繪製 + 擊中玩家 ===
            Tyrian2.simulateEnemyShots();

            // === 爆炸更新/繪製 ===
            Tyrian2.JE_drawExplosions();

            // === HUD：護盾/裝甲 bar + 分數/特殊武器/超級炸彈 ===
            Varz.JE_drawShield();
            Varz.JE_drawArmor();
            Mainint.JE_inGameDisplays();

            // === 關卡結束偵測（簡化）：所有事件處理完 + 場上無敵人/敵彈 → 倒數 → 過關 ===
            if (eventLoc > maxEvent)
            {
                bool anyEnemy = false;
                for (int z = 0; z < 100; z++)
                    if (Varz.enemyAvail[z] != 1) { anyEnemy = true; break; }
                bool anyShot = false;
                for (int z = 0; z < VarzConst.ENEMY_SHOT_MAX; z++)
                    if (!Varz.enemyShotAvail[z]) { anyShot = true; break; }

                if (!anyEnemy && !anyShot)
                {
                    if (--levelEndGrace <= 0)
                        reallyEndLevel = true;
                }
                else
                {
                    levelEndGrace = 80;
                }
            }

            Video.JE_showVGA();
            Nortsong.delayUntilElapsed();

            if (reallyEndLevel)
                break;
        }

        // 過關：載入下一關（對應 goto start_level）。mainLevel==0（章節結束）則回標題。
        Config.mainLevel = Config.nextLevel;
        if (Config.mainLevel != 0)
        {
            Palette.fade_black(15);
            goto start_level_first;
        }
    }
}
