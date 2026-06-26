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
        while (!quitRequested)
        {
            Keyboard.handleSdlEvents();
            if (Keyboard.keysactive[SdlKeys.SDL_SCANCODE_ESCAPE])
                break;

            Nortsong.setFrameCount(2);

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

            // 移動 + 繪製所有玩家子彈
            Shots.simulate_player_shots();

            // 繪製玩家船艦（對應 JE_playerMovement 的 shipGr blit）
            Sprites.blit_sprite2x2(Video.VGAScreen, player[0].x - 5, player[0].y - 7, Varz.shipGrPtr, Varz.shipGr);

            Video.JE_showVGA();
            Nortsong.delayUntilElapsed();
        }
    }
}
