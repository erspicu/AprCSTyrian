namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/tyrian2.c —— **逐步移植中**。本檔先移植 titleScreen（標題畫面/主選單）。
/// 選單動作的子畫面（newGame/loadScreen/highScore/helpSystem/setupMenu/super* 等）暫為 stub，
/// 選取後回到標題；Quit/ESC/右鍵 可正常結束。遊戲主迴圈 JE_main、tyrian2 其餘待後續。
/// </summary>
internal static unsafe partial class Tyrian2
{
    private static void CopyScreen(SDL_Surface dst, SDL_Surface src)
    {
        long n = (long)src.pitch * src.h;
        Buffer.MemoryCopy(src.pixels, dst.pixels, n, n);
    }

    public static unsafe void intro_logos()
    {
        Varz.moveTyrianLogoUp = true;

        Sdl.SDL_FillRect(Video.VGAScreen, null, 0);
        Palette.fade_white(25);

        Picload.JE_loadPic(Video.VGAScreen, 10, false);
        Video.JE_showVGA();
        Palette.fade_palette(Palette.colors, 25, 0, 255);

        Nortsong.setFrameCount(200);
        Keyboard.waitUntilGetInputOrElapsed();

        Palette.fade_black(10);

        Picload.JE_loadPic(Video.VGAScreen, 12, false);
        Video.JE_showVGA();
        Palette.fade_palette(Palette.colors, 10, 0, 255);

        Nortsong.setFrameCount(200);
        Keyboard.waitUntilGetInputOrElapsed();

        Palette.fade_black(10);
    }

    public static bool titleScreen()
    {
        const int MENU_ITEM_NEW_GAME = 0, MENU_ITEM_LOAD_GAME = 1, MENU_ITEM_HIGH_SCORES = 2,
                  MENU_ITEM_INSTRUCTIONS = 3, MENU_ITEM_SETUP = 4, MENU_ITEM_DEMO = 5, MENU_ITEM_QUIT = 6;

        Helptext.menuText[4] = "Setup"; // override "Ordering Info"

        if (Sprites.shopSpriteSheet.data == null)
            Sprites.JE_loadCompShapes(ref Sprites.shopSpriteSheet, '1'); // mouse pointer sprites

        bool restart = true;
        int selectedIndex = MENU_ITEM_NEW_GAME;
        int[] specialNameProgress = new int[VarzConst.SA_ENGAGE];

        int xCenter = Video.VGAScreen.w / 2;
        const int yMenuItems = 104, hMenuItem = 13;
        int[] wMenuItem = new int[Helptext.menuText.Length];

        for (; ; )
        {
            Nortsong.setFrameCount(1);

            if (restart)
            {
                Loudness.play_song(Musmast.SONG_TITLE);

                Picload.JE_loadPic(Video.VGAScreen, 4, false);

                FontDraw.drawFontHvShadow(Video.VGAScreen, 2, 192, Opentyr.opentyrian_version, Font.FONT_SMALL, 15, 0, false, 1);

                if (Varz.moveTyrianLogoUp)
                {
                    CopyScreen(Video.VGAScreen2, Video.VGAScreen);
                    Sprites.blit_sprite(Video.VGAScreenSeg, 11, 62, Sprites.PLANET_SHAPES, 146); // tyrian logo
                    Palette.fade_palette(Palette.colors, 10, 0, 255 - 16);

                    for (int y = 60; y >= 4; y -= 2)
                    {
                        Nortsong.setFrameCount(2);
                        CopyScreen(Video.VGAScreen, Video.VGAScreen2);
                        Sprites.blit_sprite(Video.VGAScreenSeg, 11, y, Sprites.PLANET_SHAPES, 146);
                        Video.JE_showVGA();
                        Keyboard.waitUntilElapsed();
                    }
                    Varz.moveTyrianLogoUp = false;
                }
                else
                {
                    Sprites.blit_sprite(Video.VGAScreenSeg, 11, 4, Sprites.PLANET_SHAPES, 146);
                    Palette.fade_palette(Palette.colors, 10, 0, 255 - 16);
                }

                // Draw menu items.
                for (int i = 0; i < Helptext.menuText.Length; ++i)
                {
                    string text = Helptext.menuText[i];
                    wMenuItem[i] = Fonthand.JE_textWidth(text, (uint)Font.FONT_NORMAL);
                    int x = xCenter - wMenuItem[i] / 2;
                    int y = yMenuItems + hMenuItem * i;

                    FontDraw.drawFontHv(Video.VGAScreen, x - 1, y - 1, text, Font.FONT_NORMAL, 15, -10);
                    FontDraw.drawFontHv(Video.VGAScreen, x + 1, y + 1, text, Font.FONT_NORMAL, 15, -10);
                    FontDraw.drawFontHv(Video.VGAScreen, x + 1, y - 1, text, Font.FONT_NORMAL, 15, -10);
                    FontDraw.drawFontHv(Video.VGAScreen, x - 1, y + 1, text, Font.FONT_NORMAL, 15, -10);
                    FontDraw.drawFontHv(Video.VGAScreen, x, y, text, Font.FONT_NORMAL, 15, -3);
                }

                CopyScreen(Video.VGAScreen2, Video.VGAScreen);
                Mouse.mouseCursor = Mouse.MOUSE_POINTER_NORMAL;
                Palette.fade_palette(Palette.colors, 20, 255 - 16 + 1, 255);
                restart = false;
            }

            CopyScreen(Video.VGAScreen, Video.VGAScreen2);

            // Highlight selected menu item.
            FontDraw.drawFontHvAligned(Video.VGAScreen, Video.VGAScreen.w / 2, yMenuItems + hMenuItem * selectedIndex,
                Helptext.menuText[selectedIndex], Font.FONT_NORMAL, FontAlignment.ALIGN_CENTER, 15, -1);

            Mouse.JE_mouseStartFilter(0xF0);
            Video.JE_showVGA();
            Mouse.JE_mouseReplace();

            uint idleStartTick = Globals.Clock.Ticks;

            while (true)
            {
                if (Globals.Clock.Ticks - idleStartTick > 30000) // demo after 30s idle
                {
                    Palette.fade_black(15);
                    Varz.play_demo = true;
                    return true;
                }

                Keyboard.waitUntilElapsed();
                if (Keyboard.hasInput(InputFlags.INPUT_ANY))
                    break;
                Nortsong.setFrameCount(1);
            }

            bool action = false, done = false;

            if (Keyboard.mouseGetInput(InputFlags.INPUT_ANY, out MouseInput mouseInput))
            {
                for (int i = 0; i < Helptext.menuText.Length; ++i)
                {
                    int xMenuItem = xCenter - wMenuItem[i] / 2;
                    if (mouseInput.x >= xMenuItem && mouseInput.x < xMenuItem + wMenuItem[i])
                    {
                        int yMenuItem = yMenuItems + hMenuItem * i;
                        if (mouseInput.y >= yMenuItem && mouseInput.y < yMenuItem + hMenuItem)
                        {
                            if (selectedIndex != i)
                            {
                                Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                                selectedIndex = i;
                            }
                            if (mouseInput.button == SdlKeys.SDL_BUTTON_LEFT)
                                action = true;
                            break;
                        }
                    }
                }

                if (mouseInput.button == SdlKeys.SDL_BUTTON_RIGHT)
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING);
                    done = true;
                }
            }
            else if (Keyboard.keyboardGetInput(out KeyboardInput keyboardInput))
            {
                switch (keyboardInput.scancode)
                {
                    case SdlKeys.SDL_SCANCODE_UP:
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                        selectedIndex = selectedIndex == 0 ? Helptext.menuText.Length - 1 : selectedIndex - 1;
                        break;
                    case SdlKeys.SDL_SCANCODE_DOWN:
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                        selectedIndex = selectedIndex == Helptext.menuText.Length - 1 ? 0 : selectedIndex + 1;
                        break;
                    case SdlKeys.SDL_SCANCODE_SPACE:
                    case SdlKeys.SDL_SCANCODE_RETURN:
                        action = true;
                        break;
                    case SdlKeys.SDL_SCANCODE_ESCAPE:
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING);
                        done = true;
                        break;
                }

                char sym = char.ToUpperInvariant((char)keyboardInput.sym);
                for (int i = 0; i < VarzConst.SA_ENGAGE; i++)
                {
                    string sn = Helptext.specialName[i] ?? "";
                    char exp = specialNameProgress[i] < sn.Length ? sn[specialNameProgress[i]] : '\0';
                    if (specialNameProgress[i] >= 9 || sym != exp)
                    {
                        specialNameProgress[i] = 0;
                        continue;
                    }
                    specialNameProgress[i]++;
                    char nxt = specialNameProgress[i] < sn.Length ? sn[specialNameProgress[i]] : '\0';
                    if (nxt == '\0')
                    {
                        if (i + 1 == VarzConst.SA_DESTRUCT)
                        {
                            Palette.fade_black(10);
                            Varz.loadDestruct = true;
                            return true;
                        }
                        else if (i + 1 == VarzConst.SA_ENGAGE)
                        {
                            Nortsong.JE_playSampleNum((byte)Sndmast.V_DATA_CUBE);
                            JE_whoa();
                            Palette.set_colors(new SDL_Color(0, 0, 0), 0, 255);
                            newSuperTyrianGame();
                            return true;
                        }
                        else
                        {
                            Palette.fade_black(10);
                            if (newSuperArcadeGame(i))
                                return true;
                            restart = true;
                        }
                    }
                }
            }

            if (action)
            {
                Nortsong.JE_playSampleNum((byte)Sndmast.S_SELECT);

                switch (selectedIndex)
                {
                    case MENU_ITEM_NEW_GAME:
                        Palette.fade_black(15);
                        if (newGame()) return true;
                        restart = true;
                        break;
                    case MENU_ITEM_LOAD_GAME:
                        Palette.fade_black(15);
                        if (Mainint.JE_loadScreen()) return true;
                        restart = true;
                        break;
                    case MENU_ITEM_HIGH_SCORES:
                        Palette.fade_black(15);
                        Mainint.JE_highScoreScreen();
                        restart = true;
                        break;
                    case MENU_ITEM_INSTRUCTIONS:
                        Palette.fade_black(15);
                        Mainint.JE_helpSystem(1);
                        restart = true;
                        break;
                    case MENU_ITEM_SETUP:
                        Palette.fade_black(15);
                        setupMenu();
                        restart = true;
                        break;
                    case MENU_ITEM_DEMO:
                        Palette.fade_black(15);
                        Varz.play_demo = true;
                        return true;
                    case MENU_ITEM_QUIT:
                        Palette.fade_black(15);
                        return false;
                }
            }

            if (done)
            {
                Palette.fade_black(15);
                return false;
            }
        }
    }

    public static bool newGame()
    {
        var player = Players.player;

        if (Menus.gameplaySelect())
        {
            if (Menus.episodeSelect() && Menus.difficultySelect())
                Varz.gameLoaded = true;

            Config.initialDifficulty = Config.difficultyLevel;

            if (Config.onePlayerAction)
            {
                player[0].cash = 0;
                player[0].items.ship = 8;  // Stalker
            }
            else if (Config.twoPlayerMode)
            {
                for (int i = 0; i < 2; ++i)
                    player[i].cash = 0;
                player[0].items.ship = 11;  // Silver Ship
                Config.difficultyLevel++;   // one step harder for 2-player
                Config.inputDevice[0] = 1;
                Config.inputDevice[1] = 2;
            }
            else if (Params.richMode)
            {
                player[0].cash = 1000000;
            }
            else if (Varz.gameLoaded)
            {
                uint[] initial_cash = { 10000, 15000, 20000, 30000 };
                player[0].cash = initial_cash[Episodes.episodeNum - 1];
            }
        }

        return Varz.gameLoaded;
    }

    // === setupMenu —— 移植 sources/src/opentyr.c:setupMenu（部分修改） ===
    // 修改點：只實作 MENU_SETUP 與 MENU_SOUND；MENU_GRAPHICS 子選單整個略過
    //         （依賴被 Scale3x 取代掉的 SDL scaler 基建）。"Graphics..." 仍顯示但畫成
    //         disabled（較暗，brightness -4），選取時僅播 S_CLICK 不進子選單。
    private const int MENU_NONE = 0, MENU_SETUP = 1, MENU_SOUND = 2;
    private const int MENU_COUNT = 3;

    private const int MI_NONE = 0, MI_DONE = 1, MI_GRAPHICS = 2, MI_SOUND = 3,
                      MI_JUKEBOX = 4, MI_MUSIC_VOLUME = 5, MI_SOUND_VOLUME = 6;

    private readonly struct SetupMenuItem
    {
        public readonly int id;
        public readonly string name;
        public readonly string description;
        public SetupMenuItem(int id, string name, string description)
        {
            this.id = id; this.name = name; this.description = description;
        }
    }

    private readonly struct SetupMenu
    {
        public readonly string header;
        public readonly SetupMenuItem[] items;
        public SetupMenu(string header, SetupMenuItem[] items)
        {
            this.header = header; this.items = items;
        }
    }

    private static void setupMenu()
    {
        SetupMenu[] menus = new SetupMenu[MENU_COUNT];
        menus[MENU_SETUP] = new SetupMenu("Setup", new[]
        {
            new SetupMenuItem(MI_GRAPHICS, "Graphics...", "Change the graphics settings."),
            new SetupMenuItem(MI_SOUND, "Sound...", "Change the sound settings."),
            new SetupMenuItem(MI_JUKEBOX, "Jukebox", "Listen to the music of Tyrian."),
            new SetupMenuItem(MI_DONE, "Done", "Return to the main menu."),
        });
        menus[MENU_SOUND] = new SetupMenu("Sound", new[]
        {
            new SetupMenuItem(MI_MUSIC_VOLUME, "Music Volume", "Change volume with the left/right arrow keys."),
            new SetupMenuItem(MI_SOUND_VOLUME, "Sound Volume", "Change volume with the left/right arrow keys."),
            new SetupMenuItem(MI_DONE, "Done", "Return to the previous menu."),
        });

        if (Sprites.shopSpriteSheet.data == null)
            Sprites.JE_loadCompShapes(ref Sprites.shopSpriteSheet, '1');  // need mouse pointer sprites

        bool restart = true;

        int[] menuParents = new int[MENU_COUNT];
        int[] selectedMenuItemIndexes = new int[MENU_COUNT];
        int currentMenu = MENU_SETUP;

        const int xCenter = 320 / 2;
        const int yMenuHeader = 4;
        const int xMenuItem = 45;
        const int xMenuItemName = xMenuItem;
        const int wMenuItemName = 135;
        const int xMenuItemValue = xMenuItemName + wMenuItemName;
        const int wMenuItemValue = 95;
        const int wMenuItem = wMenuItemName + wMenuItemValue;
        const int yMenuItems = 37;
        const int dyMenuItems = 21;
        const int hMenuItem = 13;

        for (; ; )
        {
            Nortsong.setFrameCount(1);

            if (restart)
            {
                Picload.JE_loadPic(Video.VGAScreen2, 2, false);
                Vga256d.fill_rectangle_wh(Video.VGAScreen2, 0, 192, 320, 8, 0);
            }

            // Restore background.
            CopyScreen(Video.VGAScreen, Video.VGAScreen2);

            SetupMenu menu = menus[currentMenu];

            // Draw header.
            FontDraw.drawFontHvShadowAligned(Video.VGAScreen, xCenter, yMenuHeader, menu.header, Font.FONT_LARGE, FontAlignment.ALIGN_CENTER, 15, -3, false, 2);

            ref int selectedMenuItemIndex = ref selectedMenuItemIndexes[currentMenu];
            SetupMenuItem[] menuItems = menu.items;
            int menuItemsCount = menuItems.Length;

            // Draw menu items.
            for (int i = 0; i < menuItemsCount; ++i)
            {
                SetupMenuItem menuItem = menuItems[i];

                int y = yMenuItems + dyMenuItems * i;

                bool selected = i == selectedMenuItemIndex;
                // 修改：Graphics 子選單不可用，永遠畫成 disabled（較暗）。
                bool disabled = menuItem.id == MI_GRAPHICS;

                string name = menuItem.name;

                FontDraw.drawFontHvShadow(Video.VGAScreen, xMenuItemName, y, name, Font.FONT_NORMAL, 15, (sbyte)(-3 + (selected ? 2 : 0) + (disabled ? -4 : 0)), false, 2);

                switch (menuItem.id)
                {
                case MI_MUSIC_VOLUME:
                    Nortvars.JE_barDrawShadow(Video.VGAScreen, xMenuItemValue, y, 1, Loudness.music_disabled ? 170 : 174, (Nortsong.tyrMusicVolume + 4) / 8, 2, 10);
                    Vga256d.JE_rectangle(Video.VGAScreen, xMenuItemValue - 2, y - 2, xMenuItemValue + 96, y + 11, 242);
                    break;

                case MI_SOUND_VOLUME:
                    Nortvars.JE_barDrawShadow(Video.VGAScreen, xMenuItemValue, y, 1, Loudness.samples_disabled ? 170 : 174, (Nortsong.fxVolume + 4) / 8, 2, 10);
                    Vga256d.JE_rectangle(Video.VGAScreen, xMenuItemValue - 2, y - 2, xMenuItemValue + 96, y + 11, 242);
                    break;

                default:
                    break;
                }
            }

            // Draw status text.
            Fonthand.JE_textShade(Video.VGAScreen, xMenuItemName, 190, menuItems[selectedMenuItemIndex].description, 15, 4, (uint)Fonthand.PART_SHADE);

            if (restart)
            {
                Mouse.mouseCursor = Mouse.MOUSE_POINTER_NORMAL;

                Palette.fade_palette(Palette.colors, 10, 0, 255);

                restart = false;
            }

            Mouse.JE_mouseStart();
            Video.JE_showVGA();
            Mouse.JE_mouseReplace();

            while (true)
            {
                Keyboard.waitUntilElapsed();

                if (Keyboard.hasInput(InputFlags.INPUT_ANY))
                    break;

                Nortsong.setFrameCount(1);
            }

            // Handle menu item interaction.

            bool action = false;

            if (Keyboard.mouseGetInput(InputFlags.INPUT_ANY, out MouseInput mouseInput))
            {
                // Find menu item name or value that was hovered or clicked.
                if (mouseInput.x >= xMenuItem && mouseInput.x < xMenuItem + wMenuItem)
                {
                    for (int i = 0; i < menuItemsCount; ++i)
                    {
                        int yMenuItem = yMenuItems + dyMenuItems * i;
                        if (mouseInput.y >= yMenuItem && mouseInput.y < yMenuItem + hMenuItem)
                        {
                            if (selectedMenuItemIndex != i)
                            {
                                Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);

                                selectedMenuItemIndex = i;
                            }

                            if (mouseInput.button == SdlKeys.SDL_BUTTON_LEFT &&
                                mouseInput.y >= yMenuItem && mouseInput.y < yMenuItem + hMenuItem)
                            {
                                // Act on menu item via name.
                                if (mouseInput.x >= xMenuItemName && mouseInput.x < xMenuItemName + wMenuItemName)
                                {
                                    action = true;
                                }

                                // Act on menu item via value.
                                else if (mouseInput.x >= xMenuItemValue && mouseInput.x < xMenuItemValue + wMenuItemValue)
                                {
                                    switch (menuItems[selectedMenuItemIndex].id)
                                    {
                                    case MI_MUSIC_VOLUME:
                                    {
                                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);

                                        int value = (mouseInput.x - xMenuItemValue) * 255 / (wMenuItemValue - 1);
                                        Nortsong.tyrMusicVolume = (ushort)Math.Min(Math.Max(0, value), 255);

                                        Loudness.set_volume((byte)Nortsong.tyrMusicVolume, (byte)Nortsong.fxVolume);
                                        break;
                                    }
                                    case MI_SOUND_VOLUME:
                                    {
                                        int value = (mouseInput.x - xMenuItemValue) * 255 / (wMenuItemValue - 1);
                                        Nortsong.fxVolume = (ushort)Math.Min(Math.Max(0, value), 255);

                                        Loudness.set_volume((byte)Nortsong.tyrMusicVolume, (byte)Nortsong.fxVolume);

                                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                                        break;
                                    }
                                    default:
                                        break;
                                    }
                                }
                            }

                            break;
                        }
                    }
                }

                if (mouseInput.button == SdlKeys.SDL_BUTTON_RIGHT)
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING);

                    currentMenu = menuParents[currentMenu];
                }
            }
            else if (Keyboard.keyboardGetInput(out KeyboardInput keyboardInput))
            {
                switch (keyboardInput.scancode)
                {
                case SdlKeys.SDL_SCANCODE_UP:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);

                    selectedMenuItemIndex = selectedMenuItemIndex == 0
                        ? menuItemsCount - 1
                        : selectedMenuItemIndex - 1;
                    break;
                }
                case SdlKeys.SDL_SCANCODE_DOWN:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);

                    selectedMenuItemIndex = selectedMenuItemIndex == menuItemsCount - 1
                        ? 0
                        : selectedMenuItemIndex + 1;
                    break;
                }
                case SdlKeys.SDL_SCANCODE_LEFT:
                {
                    switch (menuItems[selectedMenuItemIndex].id)
                    {
                    case MI_MUSIC_VOLUME:
                    {
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);

                        Nortsong.JE_changeVolume(ref Nortsong.tyrMusicVolume, -8, ref Nortsong.fxVolume, 0);
                        break;
                    }
                    case MI_SOUND_VOLUME:
                    {
                        Nortsong.JE_changeVolume(ref Nortsong.tyrMusicVolume, 0, ref Nortsong.fxVolume, -8);

                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                        break;
                    }
                    default:
                        break;
                    }
                    break;
                }
                case SdlKeys.SDL_SCANCODE_RIGHT:
                {
                    switch (menuItems[selectedMenuItemIndex].id)
                    {
                    case MI_MUSIC_VOLUME:
                    {
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);

                        Nortsong.JE_changeVolume(ref Nortsong.tyrMusicVolume, 8, ref Nortsong.fxVolume, 0);
                        break;
                    }
                    case MI_SOUND_VOLUME:
                    {
                        Nortsong.JE_changeVolume(ref Nortsong.tyrMusicVolume, 0, ref Nortsong.fxVolume, 8);

                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                        break;
                    }
                    default:
                        break;
                    }
                    break;
                }
                case SdlKeys.SDL_SCANCODE_SPACE:
                case SdlKeys.SDL_SCANCODE_RETURN:
                {
                    action = true;
                    break;
                }
                case SdlKeys.SDL_SCANCODE_ESCAPE:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING);

                    currentMenu = menuParents[currentMenu];
                    break;
                }
                default:
                    break;
                }
            }

            if (action)
            {
                int selectedMenuItemId = menuItems[selectedMenuItemIndex].id;

                switch (selectedMenuItemId)
                {
                case MI_DONE:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_SELECT);

                    currentMenu = menuParents[currentMenu];
                    break;
                }
                case MI_GRAPHICS:
                {
                    // 修改：Graphics 子選單暫時無法進入（依賴被取代的 SDL scaler 基建）。
                    // 僅播 S_CLICK 給回饋，不切換選單。
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CLICK);
                    break;
                }
                case MI_SOUND:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_SELECT);

                    menuParents[MENU_SOUND] = currentMenu;
                    currentMenu = MENU_SOUND;
                    selectedMenuItemIndexes[currentMenu] = 0;
                    break;
                }
                case MI_JUKEBOX:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_SELECT);

                    Palette.fade_black(10);

                    Jukebox.jukebox();

                    restart = true;
                    break;
                }
                case MI_MUSIC_VOLUME:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CLICK);

                    Loudness.music_disabled = !Loudness.music_disabled;
                    if (!Loudness.music_disabled)
                        Loudness.restart_song();
                    break;
                }
                case MI_SOUND_VOLUME:
                {
                    Loudness.samples_disabled = !Loudness.samples_disabled;

                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CLICK);
                    break;
                }
                default:
                    break;
                }
            }

            if (currentMenu == MENU_NONE)
            {
                Palette.fade_black(10);

                return;
            }
        }
    }
    /// <summary>對應 tyrian2.c:JE_whoa —— 輸入 'engage' 的螢幕暈染淡出特效（像素滲透）。</summary>
    private static unsafe void JE_whoa()
    {
        byte* TempScreen1 = Video.game_screen.pixels;
        byte* TempScreen2 = Video.VGAScreen2.pixels;
        byte* seg = Video.VGAScreenSeg.pixels;

        int pitch = Video.VGAScreenSeg.pitch;
        int screenSize = Video.VGAScreenSeg.h * pitch;
        int topBorder = pitch * 4;     /* Seems an arbitrary number of lines */
        int bottomBorder = pitch * 7;

        /* Clear the top and bottom borders. */
        new Span<byte>(seg, topBorder).Clear();
        new Span<byte>(seg + screenSize - bottomBorder, bottomBorder).Clear();

        /* Copy our test subject to one temp buffer.  Blank the other. */
        new Span<byte>(TempScreen1, screenSize).Clear();
        Buffer.MemoryCopy(seg, TempScreen2, screenSize, screenSize);

        for (int loops = 300; loops > 0; --loops)
        {
            Nortsong.setFrameCount(1);

            /* 'whoa' effect with pixel bleeding magic. */
            for (int i = screenSize - bottomBorder, j = topBorder / 2; i > 0; i--, j++)
            {
                int offset = j + i / 8192 - 4;
                int color = (TempScreen2[offset] * 12 +
                             TempScreen1[offset - pitch] +
                             TempScreen1[offset - 1] +
                             TempScreen1[offset + 1] +
                             TempScreen1[offset + pitch]) / 16;

                TempScreen1[j] = (byte)color;
            }

            /* Now copy that mess to the buffer. */
            Buffer.MemoryCopy(TempScreen1 + topBorder, seg + topBorder, screenSize - topBorder, screenSize - bottomBorder);

            Video.JE_showVGA();

            Keyboard.waitUntilElapsed();

            if ((Keyboard.keyboardGetInput(out KeyboardInput keyboardInput) &&
                 keyboardInput.scancode != SdlKeys.SDL_SCANCODE_SCROLLLOCK) ||
                Keyboard.mouseGetInput(InputFlags.INPUT_NO_MOTION, out _))
            {
                break;
            }

            /* Flip the buffer. */
            byte* TempScreenSwap = TempScreen1;
            TempScreen1 = TempScreen2;
            TempScreen2 = TempScreenSwap;
        }

        Fonthand.levelWarningLines = 4;
    }
    private static bool newSuperArcadeGame(int i)
    {
        Players.player[0].items.ship = Varz.SAShip[i];

        if (Menus.episodeSelect() && Menus.difficultySelect())
        {
            /* Start special mode! */
            Picload.JE_loadPic(Video.VGAScreen, 1, false);
            Video.JE_clr256(Video.VGAScreen);
            Fonthand.JE_dString(Video.VGAScreen, Fonthand.JE_fontCenter(Helptext.superShips[0], (uint)Sprites.FONT_SHAPES), 30, Helptext.superShips[0], (uint)Sprites.FONT_SHAPES);
            Fonthand.JE_dString(Video.VGAScreen, Fonthand.JE_fontCenter(Helptext.superShips[i + 1], (uint)Sprites.SMALL_FONT_SHAPES), 100, Helptext.superShips[i + 1], (uint)Sprites.SMALL_FONT_SHAPES);
            Varz.tempW = Episodes.ships[Players.player[0].items.ship].shipgraphic;
            if (Varz.tempW != 1)
                Sprites.blit_sprite2x2(Video.VGAScreen, 148, 70, Sprites.spriteSheet9, Varz.tempW);

            Video.JE_showVGA();
            Palette.fade_palette(Palette.colors, 50, 0, 255);

            Keyboard.waitUntilGetInput();

            Config.twoPlayerMode = false;
            Config.onePlayerAction = true;
            Config.superArcadeMode = (byte)(i + 1);
            Varz.gameLoaded = true;
            Config.initialDifficulty = ++Config.difficultyLevel;

            Players.player[0].cash = 0;

            Players.player[0].items.weapon[Players.FRONT_WEAPON].id = (byte)Varz.SAWeapon[i, 0];
            Players.player[0].items.special = (byte)Varz.SASpecialWeapon[i];
            if (Config.superArcadeMode == VarzConst.SA_NORTSHIPZ)
            {
                for (int j = 0; j < 2; ++j)  // COUNTOF(sidekick)
                    Players.player[0].items.sidekick[j] = 24;  // Companion Ship Quicksilver
            }

            Palette.fade_black(10);
        }

        return Varz.gameLoaded;
    }

    private static void newSuperTyrianGame()
    {
        /* SuperTyrian */

        Config.initialDifficulty = (sbyte)(Keyboard.keysactive[SdlKeys.SDL_SCANCODE_SCROLLLOCK] ? Config.DIFFICULTY_SUICIDE : Config.DIFFICULTY_ZINGLON);

        Video.JE_clr256(Video.VGAScreen);
        Fonthand.JE_outText(Video.VGAScreen, 10, 10, "Cheat codes have been disabled.", 15, 4);
        if (Config.initialDifficulty == Config.DIFFICULTY_ZINGLON)
            Fonthand.JE_outText(Video.VGAScreen, 10, 20, "Difficulty level has been set to Lord of Game.", 15, 4);
        else
            Fonthand.JE_outText(Video.VGAScreen, 10, 20, "Difficulty level has been set to Suicide.", 15, 4);
        Fonthand.JE_outText(Video.VGAScreen, 10, 30, "It is imperative that you discover the special codes.", 15, 4);
        if (Config.initialDifficulty == Config.DIFFICULTY_ZINGLON)
            Fonthand.JE_outText(Video.VGAScreen, 10, 40, "(Next time, for an easier challenge hold down SCROLL LOCK.)", 15, 4);
        Fonthand.JE_outText(Video.VGAScreen, 10, 60, "Prepare to play...", 15, 4);

        string buf = $"{Helptext.miscTextB[4]} {Helptext.pName[0]}";
        Fonthand.JE_dString(Video.VGAScreen, Fonthand.JE_fontCenter(buf, (uint)Sprites.FONT_SHAPES), 110, buf, (uint)Sprites.FONT_SHAPES);

        Loudness.play_song(16);
        Nortsong.JE_playSampleNum((byte)Sndmast.V_DANGER);

        Video.JE_showVGA();
        Palette.fade_palette(Palette.colors, 10, 0, 255);

        while (true)
        {
            Keyboard.waitUntilHasInput(InputFlags.INPUT_NO_MOTION);

            if ((Keyboard.keyboardGetInput(out KeyboardInput keyboardInput) &&
                 keyboardInput.scancode != SdlKeys.SDL_SCANCODE_SCROLLLOCK) ||
                Keyboard.mouseGetInput(InputFlags.INPUT_NO_MOTION, out _))
            {
                break;
            }
        }

        Episodes.JE_initEpisode(1);
        Params.constantDie = false;
        Config.superTyrian = true;
        Config.onePlayerAction = true;
        Varz.gameLoaded = true;
        Config.difficultyLevel = Config.initialDifficulty;

        Players.player[0].cash = 0;

        Players.player[0].items.ship = 13;                            // The Stalker 21.126
        Players.player[0].items.weapon[Players.FRONT_WEAPON].id = 39;  // Atomic RailGun

        Palette.fade_black(10);
    }
}
