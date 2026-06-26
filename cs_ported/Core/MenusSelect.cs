namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/menus.c 的開新遊戲選單：gameplaySelect / episodeSelect / difficultySelect。
/// 與 titleScreen 同模式（選單導航 + 滑鼠/鍵盤）。
/// </summary>
internal static unsafe partial class Menus
{
    private static void CopyScreen(SDL_Surface dst, SDL_Surface src)
    {
        long n = (long)src.pitch * src.h;
        Buffer.MemoryCopy(src.pixels, dst.pixels, n, n);
    }

    public static bool gameplaySelect()
    {
        const int MENU_ITEM_1_PLAYER_FULL_GAME = 0, MENU_ITEM_1_PLAYER_ARCADE = 1,
                  MENU_ITEM_2_PLAYER_ARCADE = 2, MENU_ITEM_NETWORK = 3;

        if (Sprites.shopSpriteSheet.data == null)
            Sprites.JE_loadCompShapes(ref Sprites.shopSpriteSheet, '1');

        bool restart = true;
        int menuItemsCount = gameplay_name.Length - 1;
        int selectedIndex = MENU_ITEM_1_PLAYER_FULL_GAME;

        const int xCenter = 320 / 2, yMenuHeader = 20, yMenuItems = 54, dyMenuItems = 24, hMenuItem = 13;
        int[] wMenuItem = new int[gameplay_name.Length - 1];

        for (; ; )
        {
            Nortsong.setFrameCount(1);

            if (restart)
            {
                Picload.JE_loadPic(Video.VGAScreen2, 2, false);
                FontDraw.drawFontHvShadowAligned(Video.VGAScreen2, xCenter, yMenuHeader, gameplay_name[0], Font.FONT_LARGE, FontAlignment.ALIGN_CENTER, 15, -3, false, 2);
            }

            CopyScreen(Video.VGAScreen, Video.VGAScreen2);

            for (int i = 0; i < menuItemsCount; ++i)
            {
                string text = gameplay_name[i + 1];
                wMenuItem[i] = Fonthand.JE_textWidth(text, (uint)Font.FONT_NORMAL);
                int x = xCenter - wMenuItem[i] / 2;
                int y = yMenuItems + dyMenuItems * i;
                bool selected = i == selectedIndex;
                bool disabled = i == MENU_ITEM_NETWORK;
                FontDraw.drawFontHvShadow(Video.VGAScreen, x, y, text, Font.FONT_NORMAL, 15, (sbyte)(-4 + (selected ? 2 : 0) + (disabled ? -4 : 0)), false, 2);
            }

            if (restart)
            {
                Mouse.mouseCursor = Mouse.MOUSE_POINTER_NORMAL;
                Palette.fade_palette(Palette.colors, 10, 0, 255);
                restart = false;
            }

            Mouse.JE_mouseStart();
            Video.JE_showVGA();
            Mouse.JE_mouseReplace();

            Keyboard.waitUntilElapsed();
            Keyboard.waitUntilHasInput(InputFlags.INPUT_ANY);

            bool action = false, cancel = false;

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
                if (mi.button == SdlKeys.SDL_BUTTON_RIGHT) { Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING); cancel = true; }
            }
            else if (Keyboard.keyboardGetInput(out KeyboardInput ki))
            {
                switch (ki.scancode)
                {
                    case SdlKeys.SDL_SCANCODE_UP: Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); selectedIndex = selectedIndex == 0 ? menuItemsCount - 1 : selectedIndex - 1; break;
                    case SdlKeys.SDL_SCANCODE_DOWN: Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); selectedIndex = selectedIndex == menuItemsCount - 1 ? 0 : selectedIndex + 1; break;
                    case SdlKeys.SDL_SCANCODE_SPACE: case SdlKeys.SDL_SCANCODE_RETURN: action = true; break;
                    case SdlKeys.SDL_SCANCODE_ESCAPE: Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING); cancel = true; break;
                }
            }

            if (action)
            {
                switch (selectedIndex)
                {
                    case MENU_ITEM_1_PLAYER_FULL_GAME:
                    case MENU_ITEM_1_PLAYER_ARCADE:
                    case MENU_ITEM_2_PLAYER_ARCADE:
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_SELECT);
                        Palette.fade_black(10);
                        Config.onePlayerAction = selectedIndex == MENU_ITEM_1_PLAYER_ARCADE;
                        Config.twoPlayerMode = selectedIndex == MENU_ITEM_2_PLAYER_ARCADE;
                        return true;
                    case MENU_ITEM_NETWORK:
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING);
                        break;
                }
            }

            if (cancel) { Palette.fade_black(15); return false; }
        }
    }

    public static bool episodeSelect()
    {
        if (Sprites.shopSpriteSheet.data == null)
            Sprites.JE_loadCompShapes(ref Sprites.shopSpriteSheet, '1');

        bool restart = true;
        int menuItemsCount = Episodes.EPISODE_AVAILABLE;
        int selectedIndex = 0;

        const int xCenter = 320 / 2, yMenuHeader = 20, xMenuItem = 20, yMenuItems = 50, dyMenuItems = 30, hMenuItem = 13;
        int[] wMenuItem = new int[Episodes.EPISODE_AVAILABLE];

        for (; ; )
        {
            Nortsong.setFrameCount(1);

            if (restart)
            {
                Picload.JE_loadPic(Video.VGAScreen2, 2, false);
                FontDraw.drawFontHvShadowAligned(Video.VGAScreen2, xCenter, yMenuHeader, episode_name[0], Font.FONT_LARGE, FontAlignment.ALIGN_CENTER, 15, -3, false, 2);
            }

            CopyScreen(Video.VGAScreen, Video.VGAScreen2);

            for (int i = 0; i < menuItemsCount; ++i)
            {
                string text = episode_name[i + 1];
                wMenuItem[i] = Fonthand.JE_textWidth(text, (uint)Font.FONT_NORMAL);
                int y = yMenuItems + dyMenuItems * i;
                bool selected = i == selectedIndex;
                bool disabled = !Episodes.episodeAvail[i];
                FontDraw.drawFontHvShadow(Video.VGAScreen, xMenuItem, y, text, Font.FONT_NORMAL, 15, (sbyte)(-4 + (selected ? 2 : 0) + (disabled ? -4 : 0)), false, 2);
            }

            if (restart)
            {
                Mouse.mouseCursor = Mouse.MOUSE_POINTER_NORMAL;
                Palette.fade_palette(Palette.colors, 10, 0, 255);
                restart = false;
            }

            Mouse.JE_mouseStart();
            Video.JE_showVGA();
            Mouse.JE_mouseReplace();

            Keyboard.waitUntilElapsed();
            Keyboard.waitUntilHasInput(InputFlags.INPUT_ANY);

            bool action = false, cancel = false;

            if (Keyboard.mouseGetInput(InputFlags.INPUT_ANY, out MouseInput mi))
            {
                for (int i = 0; i < menuItemsCount; ++i)
                {
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
                if (mi.button == SdlKeys.SDL_BUTTON_RIGHT) { Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING); cancel = true; }
            }
            else if (Keyboard.keyboardGetInput(out KeyboardInput ki))
            {
                switch (ki.scancode)
                {
                    case SdlKeys.SDL_SCANCODE_UP: Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); selectedIndex = selectedIndex == 0 ? menuItemsCount - 1 : selectedIndex - 1; break;
                    case SdlKeys.SDL_SCANCODE_DOWN: Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); selectedIndex = selectedIndex == menuItemsCount - 1 ? 0 : selectedIndex + 1; break;
                    case SdlKeys.SDL_SCANCODE_SPACE: case SdlKeys.SDL_SCANCODE_RETURN: action = true; break;
                    case SdlKeys.SDL_SCANCODE_ESCAPE: Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING); cancel = true; break;
                }
            }

            if (action)
            {
                if (Episodes.episodeAvail[selectedIndex])
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_SELECT);
                    Palette.fade_black(10);
                    Episodes.JE_initEpisode(selectedIndex + 1);
                    Episodes.initial_episode_num = Episodes.episodeNum;
                    return true;
                }
                else
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING);
                }
            }

            if (cancel) { Palette.fade_black(15); return false; }
        }
    }

    public static bool difficultySelect()
    {
        if (Sprites.shopSpriteSheet.data == null)
            Sprites.JE_loadCompShapes(ref Sprites.shopSpriteSheet, '1');

        bool restart = true;
        int menuItemsCount = difficulty_name.Length - 1;
        int menuItemsVisibleCount = menuItemsCount - 3;
        int selectedIndex = 1;
        int lordProgress = 0;

        const int xCenter = 320 / 2, yMenuHeader = 20, yMenuItems = 54, dyMenuItems = 24, hMenuItem = 13;
        int[] wMenuItem = new int[difficulty_name.Length - 1];

        for (; ; )
        {
            Nortsong.setFrameCount(1);

            if (restart)
            {
                Picload.JE_loadPic(Video.VGAScreen2, 2, false);
                FontDraw.drawFontHvShadowAligned(Video.VGAScreen2, xCenter, yMenuHeader, difficulty_name[0], Font.FONT_LARGE, FontAlignment.ALIGN_CENTER, 15, -3, false, 2);
            }

            CopyScreen(Video.VGAScreen, Video.VGAScreen2);

            for (int i = 0; i < menuItemsVisibleCount; ++i)
            {
                string text = difficulty_name[i + 1];
                wMenuItem[i] = Fonthand.JE_textWidth(text, (uint)Font.FONT_NORMAL);
                int x = xCenter - wMenuItem[i] / 2;
                int y = yMenuItems + dyMenuItems * i;
                bool selected = i == selectedIndex;
                FontDraw.drawFontHvShadow(Video.VGAScreen, x, y, text, Font.FONT_NORMAL, 15, (sbyte)(-4 + (selected ? 2 : 0)), false, 2);
            }

            if (restart)
            {
                Mouse.mouseCursor = Mouse.MOUSE_POINTER_NORMAL;
                Palette.fade_palette(Palette.colors, 10, 0, 255);
                restart = false;
            }

            Mouse.JE_mouseStart();
            Video.JE_showVGA();
            Mouse.JE_mouseReplace();

            Keyboard.waitUntilElapsed();
            Keyboard.waitUntilHasInput(InputFlags.INPUT_ANY);

            bool action = false, cancel = false;

            if (Keyboard.mouseGetInput(InputFlags.INPUT_ANY, out MouseInput mi))
            {
                for (int i = 0; i < menuItemsVisibleCount; ++i)
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
                if (mi.button == SdlKeys.SDL_BUTTON_RIGHT) { Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING); cancel = true; }
            }
            else if (Keyboard.keyboardGetInput(out KeyboardInput ki))
            {
                switch (ki.scancode)
                {
                    case SdlKeys.SDL_SCANCODE_UP: Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); selectedIndex = selectedIndex == 0 ? menuItemsVisibleCount - 1 : selectedIndex - 1; break;
                    case SdlKeys.SDL_SCANCODE_DOWN: Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); selectedIndex = selectedIndex == menuItemsVisibleCount - 1 ? 0 : selectedIndex + 1; break;
                    case SdlKeys.SDL_SCANCODE_SPACE: case SdlKeys.SDL_SCANCODE_RETURN: action = true; break;
                    case SdlKeys.SDL_SCANCODE_ESCAPE: Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING); cancel = true; break;
                }

                // 隱藏難度解鎖（Lord of the Game）
                switch (menuItemsVisibleCount)
                {
                    case 3:
                        if ((ki.mod & SdlKeys.KMOD_SHIFT) != 0 && ki.sym == SdlKeys.SDLK_g)
                            menuItemsVisibleCount = 4;
                        break;
                    case 4:
                        if ((ki.mod & SdlKeys.KMOD_SHIFT) != 0 && ki.sym == SdlKeys.SDLK_RIGHTBRACKET)
                            menuItemsVisibleCount = 5;
                        break;
                    case 5:
                        bool allDown = true;
                        for (int i = 0; i < Keyboard.lordKeySymsDown.Length; ++i)
                            if (!Keyboard.lordKeySymsDown[i]) { allDown = false; break; }
                        if (allDown) menuItemsVisibleCount = 6;

                        if (lordProgress < Keyboard.lordKeySyms.Length && ki.sym == Keyboard.lordKeySyms[lordProgress])
                        {
                            lordProgress += 1;
                            if (lordProgress == Keyboard.lordKeySyms.Length)
                                menuItemsVisibleCount = 6;
                        }
                        else
                        {
                            lordProgress = 0;
                        }
                        break;
                }
            }

            if (action)
            {
                Nortsong.JE_playSampleNum((byte)Sndmast.S_SELECT);
                Config.difficultyLevel = selectedIndex switch
                {
                    0 => (sbyte)Config.DIFFICULTY_EASY,
                    1 => (sbyte)Config.DIFFICULTY_NORMAL,
                    2 => (sbyte)Config.DIFFICULTY_HARD,
                    3 => (sbyte)Config.DIFFICULTY_IMPOSSIBLE,
                    4 => (sbyte)Config.DIFFICULTY_SUICIDE,
                    5 => (sbyte)Config.DIFFICULTY_ZINGLON,
                    _ => Config.difficultyLevel,
                };
                Palette.fade_black(10);
                return true;
            }

            if (cancel) { Palette.fade_black(15); return false; }
        }
    }
}
