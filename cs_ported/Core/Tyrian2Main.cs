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

            Video.JE_showVGA();
            Nortsong.delayUntilElapsed();
        }
    }
}
