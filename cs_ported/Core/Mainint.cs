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

    private const int MAX_PAGE = 8, TOPICS = 6;
    private static readonly byte[] topicStart = { 0, 1, 2, 3, 7, 255 };

    public static void JE_helpSystem(byte startTopic)
    {
        if (Sprites.shopSpriteSheet.data == null)
            Sprites.JE_loadCompShapes(ref Sprites.shopSpriteSheet, '1');

        byte topic = startTopic;
        bool restart = true;

        int menuItemsCount = Helptext.topicName.Length - 1;
        int selectedIndex = 0;

        const int xCenter = 320 / 2, yMenuHeader = 30, yMenuItems = 60, dyMenuItems = 20, hMenuItem = 13;
        int[] wMenuItem = new int[Helptext.topicName.Length - 1];

        for (; ; )
        {
            Nortsong.setFrameCount(1);

            if (restart)
            {
                Loudness.play_song(Musmast.SONG_MAPVIEW);
                Picload.JE_loadPic(Video.VGAScreen2, 2, false);
            }

            if (topic > 1)
            {
                if (!helpSystemPage(ref topic, ref restart))
                    return;
                selectedIndex = topic - 1;
                topic = 1;
                continue;
            }

            CopyScreen(Video.VGAScreen, Video.VGAScreen2);

            FontDraw.drawFontHvShadowAligned(Video.VGAScreen, xCenter, yMenuHeader, Helptext.topicName[0], Font.FONT_LARGE, FontAlignment.ALIGN_CENTER, 15, -3, false, 2);

            for (int i = 0; i < menuItemsCount; ++i)
            {
                string text = Helptext.topicName[i + 1];
                wMenuItem[i] = Fonthand.JE_textWidth(text, (uint)Font.FONT_NORMAL);
                int y = yMenuItems + dyMenuItems * i;
                bool selected = i == selectedIndex;
                FontDraw.drawFontHvShadowAligned(Video.VGAScreen, xCenter, y, text, Font.FONT_NORMAL, FontAlignment.ALIGN_CENTER, 15, (sbyte)(-3 + (selected ? 2 : 0)), false, 2);
            }

            Mouse.mouseCursor = Mouse.MOUSE_POINTER_NORMAL;

            if (restart)
            {
                Palette.fade_palette(Palette.colors, 10, 0, 255);
                restart = false;
            }

            Mouse.JE_mouseStart();
            Video.JE_showVGA();
            Mouse.JE_mouseReplace();

            Keyboard.waitUntilElapsed();
            Keyboard.waitUntilHasInput(InputFlags.INPUT_ANY);

            bool action = false, done = false;

            if (Keyboard.mouseGetInput(InputFlags.INPUT_ANY, out MouseInput mi))
            {
                for (int i = 0; i < menuItemsCount; ++i)
                {
                    int xMenuItem = xCenter - wMenuItem[i] / 2;
                    if (mi.x >= xMenuItem && mi.x < xMenuItem + wMenuItem[i])
                    {
                        int yMenuItem = yMenuItems + dyMenuItems * i;
                        if (mi.y >= yMenuItem && mi.y < yMenuItem + hMenuItem)
                        {
                            if (selectedIndex != i) { Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); selectedIndex = i; }
                            if (mi.button == SdlKeys.SDL_BUTTON_LEFT) action = true;
                            break;
                        }
                    }
                }
                if (mi.button == SdlKeys.SDL_BUTTON_RIGHT) { Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING); done = true; }
            }
            else if (Keyboard.keyboardGetInput(out KeyboardInput ki))
            {
                switch (ki.scancode)
                {
                    case SdlKeys.SDL_SCANCODE_UP: Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); selectedIndex = selectedIndex == 0 ? menuItemsCount - 1 : selectedIndex - 1; break;
                    case SdlKeys.SDL_SCANCODE_DOWN: Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); selectedIndex = selectedIndex == menuItemsCount - 1 ? 0 : selectedIndex + 1; break;
                    case SdlKeys.SDL_SCANCODE_SPACE: case SdlKeys.SDL_SCANCODE_RETURN: action = true; break;
                    case SdlKeys.SDL_SCANCODE_ESCAPE: Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING); done = true; break;
                }
            }

            if (action)
            {
                Nortsong.JE_playSampleNum((byte)Sndmast.S_SELECT);
                topic = (byte)(selectedIndex + 2);
                if (selectedIndex == menuItemsCount - 1)
                    done = true;
            }

            if (done)
            {
                Palette.fade_black(15);
                return;
            }
        }
    }

    private static bool helpSystemPage(ref byte topic, ref bool restart)
    {
        byte page = topicStart[topic - 1];
        const int xCenter = 320 / 2;

        for (; ; )
        {
            if (page == 0)
            {
                topic = 1;
                return true;
            }
            else if (page > MAX_PAGE)
            {
                topic = (byte)(Helptext.topicName.Length - 1);
                return true;
            }

            for (int temp = 0; temp < Helptext.topicName.Length; ++temp)
            {
                if (topicStart[temp] <= page)
                    topic = (byte)(temp + 1);
                else
                    break;
            }

            Nortsong.setFrameCount(1);

            CopyScreen(Video.VGAScreen, Video.VGAScreen2);
            Vga256d.fill_rectangle_wh(Video.VGAScreen, 0, 192, 320, 8, 0);

            string text = Helptext.topicName[topic - 1];
            FontDraw.drawFontHvShadowAligned(Video.VGAScreen, xCenter, 1, text, Font.FONT_NORMAL, FontAlignment.ALIGN_CENTER, 15, -3, false, 2);

            FontDraw.drawFontHvAligned(Video.VGAScreen, 10, 192, $"{Helptext.miscText[24]} {page - topicStart[topic - 1] + 1}", Font.FONT_SMALL, FontAlignment.ALIGN_LEFT, 13, 5);
            FontDraw.drawFontHvAligned(Video.VGAScreen, 320 - 10, 192, $"{Helptext.miscText[25]} {page} of {MAX_PAGE}", Font.FONT_SMALL, FontAlignment.ALIGN_RIGHT, 13, 5);

            switch (page)
            {
                case 1:
                    HBox(10, 20, 2); HBox(10, 50, 5); HBox(10, 80, 21); HBox(10, 110, 1); HBox(10, 140, 28);
                    break;
                case 2:
                    HBox(10, 20, 1); HBox(10, 60, 2); HBox(10, 100, 21); HBox(10, 140, 28);
                    break;
                case 3:
                    HBox(10, 20, 5); HBox(10, 70, 6); HBox(10, 110, 7);
                    break;
                case 4:
                    HBox(10, 20, 8); HBox(10, 55, 9); HBox(10, 87, 10); HBox(10, 120, 11); HBox(10, 170, 13);
                    break;
                case 5:
                    HBox(10, 20, 14); HBox(10, 80, 15); HBox(10, 120, 16);
                    break;
                case 6:
                    HBox(10, 20, 17); HBox(10, 40, 18); HBox(10, 130, 20);
                    break;
                case 7:
                    HBox(10, 20, 21); HBox(10, 70, 22); HBox(10, 110, 23); HBox(10, 140, 24);
                    break;
                case 8:
                    HBox(10, 20, 25); HBox(10, 60, 26); HBox(10, 100, 27); HBox(10, 140, 28); HBox(10, 170, 29);
                    break;
            }

            if (restart)
            {
                Palette.fade_palette(Palette.colors, 10, 0, 255);
                restart = false;
            }

            while (true)
            {
                Mouse.mouseCursor = Keyboard.mouseX < xCenter ? Mouse.MOUSE_POINTER_LEFT : Mouse.MOUSE_POINTER_RIGHT;
                Mouse.JE_mouseStart();
                Video.JE_showVGA();
                Mouse.JE_mouseReplace();
                Keyboard.waitUntilElapsed();
                Keyboard.waitUntilHasInput(InputFlags.INPUT_ANY);
                if (Keyboard.hasInput(InputFlags.INPUT_NO_MOTION))
                    break;
                Nortsong.setFrameCount(1);
            }

            bool done = false;

            if (Keyboard.mouseGetInput(InputFlags.INPUT_NO_MOTION, out MouseInput mi))
            {
                switch (mi.button)
                {
                    case SdlKeys.SDL_BUTTON_LEFT:
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                        if (mi.x < xCenter) page -= 1; else page += 1;
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
                    case SdlKeys.SDL_SCANCODE_LEFT: Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); page -= 1; break;
                    case SdlKeys.SDL_SCANCODE_RIGHT:
                    case SdlKeys.SDL_SCANCODE_SPACE:
                    case SdlKeys.SDL_SCANCODE_RETURN:
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); page += 1; break;
                    case SdlKeys.SDL_SCANCODE_ESCAPE: Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING); done = true; break;
                }
            }

            if (done)
            {
                Palette.fade_black(15);
                return false;
            }
        }
    }

    private static void HBox(int x, int y, int msgNum) =>
        Helptext.JE_HBox(Video.VGAScreen, x, y, (byte)msgNum, 60, 8, 12, 3);

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
