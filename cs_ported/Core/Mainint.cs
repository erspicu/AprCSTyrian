namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/mainint.c —— 主流程（標題畫面/遊戲流程）。**逐步移植中**：
/// 先放入可獨立切割的函式（JE_initPlayerData 等），titleScreen/JE_main 等大型流程待後續。
/// </summary>
internal static unsafe partial class Mainint
{
    private static void StrCpy(byte[] dst, string src)
    {
        int n = Math.Min(src.Length, dst.Length - 1);
        for (int i = 0; i < n; ++i) dst[i] = (byte)src[i];
        dst[n] = 0;
    }

    private static string CStr(byte[] s)
    {
        int n = 0;
        while (n < s.Length && s[n] != 0) n++;
        var c = new char[n];
        for (int i = 0; i < n; ++i) c[i] = (char)s[i];
        return new string(c);
    }

    private static void CopyScreen(SDL_Surface dst, SDL_Surface src)
    {
        long n = (long)src.pitch * src.h;
        Buffer.MemoryCopy(src.pixels, dst.pixels, n, n);
    }

    public static void JE_highScoreScreen()
    {
        if (Sprites.shopSpriteSheet.data == null)
            Sprites.JE_loadCompShapes(ref Sprites.shopSpriteSheet, '1');

        bool restart = true;
        int episodeIndex = 0;
        const int episodeCount = 3; // 存檔只有 3 章節的高分空間

        const int xCenter = 320 / 2, yMenuHeader = 3, yEpisodeHeader = 30,
                  xLeftControl = 83, xRightControl = 213, wControl = 24, yControls = 179;

        for (; ; )
        {
            Nortsong.setFrameCount(1);

            if (restart)
            {
                Picload.JE_loadPic(Video.VGAScreen2, 2, false);
                Vga256d.fill_rectangle_wh(Video.VGAScreen2, 0, 192, 320, 8, 0);
                FontDraw.drawFontHvShadowAligned(Video.VGAScreen2, xCenter, yMenuHeader, Helptext.miscText[50], Font.FONT_LARGE, FontAlignment.ALIGN_CENTER, 15, -3, false, 2);
            }

            CopyScreen(Video.VGAScreen, Video.VGAScreen2);

            bool disabled = !Episodes.episodeAvail[episodeIndex];
            FontDraw.drawFontHvShadowAligned(Video.VGAScreen, xCenter, yEpisodeHeader, Menus.episode_name[episodeIndex + 1], Font.FONT_NORMAL, FontAlignment.ALIGN_CENTER, 15, (sbyte)(-3 + (disabled ? -4 : 0)), false, 2);

            // 1-player scores
            FontDraw.drawFontHvShadowAligned(Video.VGAScreen, xCenter, 55, Helptext.miscText[46], Font.FONT_NORMAL, FontAlignment.ALIGN_CENTER, 15, -3, false, 2);
            for (int i = 0; i < 3; ++i)
            {
                int y = 75 + 10 * i;
                var sf = Config.saveFiles[episodeIndex * 6 + i];
                int rank = Math.Min(sf.highScoreDiff, Helptext.difficultyNameB.Length - 1);
                Fonthand.JE_textShade(Video.VGAScreen, 20, y, $"~#{i + 1}:~  {sf.highScore1}", 15, 0, Fonthand.FULL_SHADE);
                Fonthand.JE_textShade(Video.VGAScreen, 110, y, CStr(sf.highScoreName), 15, 2, Fonthand.FULL_SHADE);
                Fonthand.JE_textShade(Video.VGAScreen, 250, y, Helptext.difficultyNameB[rank] ?? "", 15, rank + (rank == 0 ? 0 : -1), Fonthand.FULL_SHADE);
            }

            // 2-player scores
            FontDraw.drawFontHvShadowAligned(Video.VGAScreen, xCenter, 120, Helptext.miscText[47], Font.FONT_NORMAL, FontAlignment.ALIGN_CENTER, 15, -3, false, 2);
            for (int i = 0; i < 3; ++i)
            {
                int y = 135 + 10 * i;
                var sf = Config.saveFiles[episodeIndex * 6 + 3 + i];
                int rank = Math.Min(sf.highScoreDiff, Helptext.difficultyNameB.Length - 1);
                Fonthand.JE_textShade(Video.VGAScreen, 20, y, $"~#{i + 1}:~  {sf.highScore1}", 15, 0, Fonthand.FULL_SHADE);
                Fonthand.JE_textShade(Video.VGAScreen, 110, y, CStr(sf.highScoreName), 15, 2, Fonthand.FULL_SHADE);
                Fonthand.JE_textShade(Video.VGAScreen, 250, y, Helptext.difficultyNameB[rank] ?? "", 15, rank + (rank == 0 ? 0 : -1), Fonthand.FULL_SHADE);
            }

            bool leftControlVisible = episodeIndex > 0;
            bool rightControlVisible = episodeIndex < episodeCount - 1;
            if (leftControlVisible)
                Sprites.blit_sprite2x2(Video.VGAScreen, xLeftControl, yControls, Sprites.shopSpriteSheet, 279);
            if (rightControlVisible)
                Sprites.blit_sprite2x2(Video.VGAScreen, xRightControl, yControls, Sprites.shopSpriteSheet, 281);

            Helptext.JE_helpBox(Video.VGAScreen, 103, 182, Helptext.miscText[56], 25, 7, 15, 1, Fonthand.FULL_SHADE);

            if (restart)
            {
                Mouse.mouseCursor = Mouse.MOUSE_POINTER_NORMAL;
                Palette.fade_palette(Palette.colors, 10, 0, 255);
                restart = false;
            }

            while (true)
            {
                Mouse.JE_mouseStart();
                Video.JE_showVGA();
                Mouse.JE_mouseReplace();
                Keyboard.waitUntilElapsed();
                Keyboard.waitUntilHasInput(InputFlags.INPUT_ANY);
                if (Keyboard.hasInput(InputFlags.INPUT_NO_MOTION))
                    break;
                Nortsong.setFrameCount(1);
            }

            bool leftAction = false, rightAction = false, done = false;

            if (Keyboard.mouseGetInput(InputFlags.INPUT_NO_MOTION, out MouseInput mi))
            {
                switch (mi.button)
                {
                    case SdlKeys.SDL_BUTTON_LEFT:
                        if (leftControlVisible && mi.y >= yControls && mi.x >= xLeftControl && mi.x < xLeftControl + wControl)
                        { Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); leftAction = true; }
                        else if (rightControlVisible && mi.y >= yControls && mi.x >= xRightControl && mi.x < xRightControl + wControl)
                        { Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); rightAction = true; }
                        break;
                    case SdlKeys.SDL_BUTTON_RIGHT:
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING); done = true;
                        break;
                }
            }
            else if (Keyboard.keyboardGetInput(out KeyboardInput ki))
            {
                switch (ki.scancode)
                {
                    case SdlKeys.SDL_SCANCODE_LEFT: Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); leftAction = true; break;
                    case SdlKeys.SDL_SCANCODE_RIGHT: Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); rightAction = true; break;
                    case SdlKeys.SDL_SCANCODE_SPACE:
                    case SdlKeys.SDL_SCANCODE_RETURN:
                    case SdlKeys.SDL_SCANCODE_ESCAPE:
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING); done = true; break;
                }
            }

            if (leftAction)
                episodeIndex = episodeIndex == 0 ? episodeCount - 1 : episodeIndex - 1;
            else if (rightAction)
                episodeIndex = episodeIndex == episodeCount - 1 ? 0 : episodeIndex + 1;

            if (done)
            {
                Palette.fade_black(15);
                return;
            }
        }
    }

    public static void JE_drawPortConfigButtons() // rear weapon pattern indicator
    {
        if (Config.twoPlayerMode)
            return;

        if (Players.player[0].weapon_mode == 1)
        {
            Sprites.blit_sprite(Video.VGAScreenSeg, 285, 44, Sprites.OPTION_SHAPES, 18); // lit
            Sprites.blit_sprite(Video.VGAScreenSeg, 302, 44, Sprites.OPTION_SHAPES, 19); // unlit
        }
        else // == 2
        {
            Sprites.blit_sprite(Video.VGAScreenSeg, 285, 44, Sprites.OPTION_SHAPES, 19); // unlit
            Sprites.blit_sprite(Video.VGAScreenSeg, 302, 44, Sprites.OPTION_SHAPES, 18); // lit
        }
    }

    public static void JE_initPlayerData()
    {
        var player = Players.player;

        // New Game Items/Data
        player[0].items.ship = 1;                              // USP Talon
        player[0].items.weapon[Players.FRONT_WEAPON].id = 1;   // Pulse Cannon
        player[0].items.weapon[Players.REAR_WEAPON].id = 0;    // None
        player[0].items.shield = 4;                            // Gencore High Energy Shield
        player[0].items.generator = 2;                         // Advanced MR-12
        for (int i = 0; i < 2; ++i)
            player[0].items.sidekick[i] = 0;                   // None
        player[0].items.special = 0;                           // None

        player[0].last_items = player[0].items;

        player[1].items = player[0].items;
        player[1].items.weapon[Players.REAR_WEAPON].id = 15;   // Vulcan Cannon
        player[1].items.sidekick_level = 101;                  // 101, 102, 103
        player[1].items.sidekick_series = 0;                   // None

        Config.gameHasRepeated = false;
        Config.onePlayerAction = false;
        Config.superArcadeMode = VarzConst.SA_NONE;
        Config.superTyrian = false;
        Config.twoPlayerMode = false;

        Config.secretHint = (byte)((MtRand.mt_rand() % 3) + 1);

        for (int p = 0; p < 2; ++p)
        {
            for (int i = 0; i < 2; ++i)
                player[p].items.weapon[i].power = 1;

            player[p].weapon_mode = 1;
            player[p].armor = Episodes.ships[player[p].items.ship].dmg;

            player[p].is_dragonwing = (p == 1);
            // TODO: 原為 player[p].lives = &player[p].items.weapon[p].power（C 指標別名 hack）；
            //       class 欄位無法穩定取址，待生命值系統移植時改為等價存取。
        }

        Config.mainLevel = Episodes.FIRST_LEVEL;
        Config.saveLevel = Episodes.FIRST_LEVEL;

        StrCpy(Config.lastLevelName, Helptext.miscText[19]);
    }
}
