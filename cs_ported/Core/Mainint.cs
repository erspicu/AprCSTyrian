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

    public static bool performSave;
    public static ushort upgradeCost, downgradeCost;

    public static ushort textErase; // mainint.c: JE_word textErase

    // ===========================================================================
    // 以下為 JE_main 逐行移植所需、但尚未移植之函式的「空殼」（no-op），
    // 讓主迴圈可編譯。實際行為待日後填入；呼叫點皆受對應旗標守護（多數情況不會觸發）。
    // ===========================================================================
    /// <summary>對應 backgrnd.c:JE_checkSmoothies —— 依 processorType 決定是否啟用任何 smoothie 濾鏡。</summary>
    public static void JE_checkSmoothies()
    {
        Backgrnd.anySmoothies = (Config.processorType > 2 && (Config.smoothies[1 - 1] || Config.smoothies[2 - 1])) || (Config.processorType > 1 && (Config.smoothies[3 - 1] || Config.smoothies[4 - 1] || Config.smoothies[5 - 1]));
    }
    // network.h:JE_boolean pauseRequest, skipLevelRequest, helpRequest, nortShipRequest;
    // 網路未移植（守則 8），這些請求旗標僅供 isNetworkGame（const false）分支引用。
    public static bool pauseRequest, skipLevelRequest, helpRequest, nortShipRequest;

    /// <summary>對應 mainint.c:JE_gammaCheck —— F11 切換 gamma 等級(0-3)、重載調色盤並套用。</summary>
    public static bool JE_gammaCheck()
    {
        bool temp = Keyboard.keysactive[SdlKeys.SDL_SCANCODE_F11];
        if (temp)
        {
            Keyboard.keysactive[SdlKeys.SDL_SCANCODE_F11] = false;
            Config.gammaCorrection = (byte)((Config.gammaCorrection + 1) % 4);
            Array.Copy(Palette.palettes[Pcxmast.pcxpal[3 - 1]], Palette.colors, 256);
            JE_gammaCorrect(Palette.colors, Config.gammaCorrection);
            Palette.set_palette(Palette.colors, 0, 255);
        }
        return temp;
    }

    /// <summary>對應 mainint.c:JE_mainKeyboardInput —— 遊戲中按鍵分派（暫停/選單/說明/作弊碼）。</summary>
    public static void JE_mainKeyboardInput()
    {
        var player = Players.player;
        var keysactive = Keyboard.keysactive;

        JE_gammaCheck();

        /* { Network Request Commands } */

        if (!Config.isNetworkGame)
        {
            /* { Edited Ships } for Player 1 */
            if (Editship.extraAvail && keysactive[SdlKeys.SDL_SCANCODE_TAB] && !Config.isNetworkGame && !Config.superTyrian)
            {
                for (int x = SdlKeys.SDL_SCANCODE_1; x <= SdlKeys.SDL_SCANCODE_0; x++)
                {
                    if (keysactive[x])
                    {
                        int z = x - SdlKeys.SDL_SCANCODE_1 + 1;
                        player[0].items.ship = (byte)(90 + z);             /*Ships*/
                        z = (z - 1) * 15;
                        player[0].items.weapon[Players.FRONT_WEAPON].id = Editship.extraShips[z + 1];
                        player[0].items.weapon[Players.REAR_WEAPON].id = Editship.extraShips[z + 2];
                        player[0].items.special = Editship.extraShips[z + 3];
                        player[0].items.sidekick[Players.LEFT_SIDEKICK] = Editship.extraShips[z + 4];
                        player[0].items.sidekick[Players.RIGHT_SIDEKICK] = Editship.extraShips[z + 5];
                        player[0].items.generator = Editship.extraShips[z + 6];
                        /*Armor*/
                        player[0].items.shield = Editship.extraShips[z + 8];
                        Array.Clear(Config.shotMultiPos, 0, Config.shotMultiPos.Length);

                        if (player[0].weapon_mode > Varz.JE_portConfigs())
                            player[0].weapon_mode = 1;

                        Varz.tempW = (ushort)player[0].armor;
                        Varz.JE_getShipInfo();
                        if (player[0].armor > Varz.tempW && Tyrian2.editShip1)
                            player[0].armor = Varz.tempW;
                        else
                            Tyrian2.editShip1 = true;

                        SDL_Surface temp_surface = Video.VGAScreen;
                        Video.VGAScreen = Video.VGAScreenSeg;
                        Varz.JE_wipeShieldArmorBars();
                        Varz.JE_drawArmor();
                        Varz.JE_drawShield();
                        Video.VGAScreen = temp_surface;
                        Varz.JE_drawOptions();

                        keysactive[x] = false;
                    }
                }
            }

            /* for Player 2 */
            if (Editship.extraAvail && keysactive[SdlKeys.SDL_SCANCODE_CAPSLOCK] && !Config.isNetworkGame && !Config.superTyrian)
            {
                for (int x = SdlKeys.SDL_SCANCODE_1; x <= SdlKeys.SDL_SCANCODE_0; x++)
                {
                    if (keysactive[x])
                    {
                        int z = x - SdlKeys.SDL_SCANCODE_1 + 1;
                        player[1].items.ship = (byte)(90 + z);
                        z = (z - 1) * 15;
                        player[1].items.weapon[Players.FRONT_WEAPON].id = Editship.extraShips[z + 1];
                        player[1].items.weapon[Players.REAR_WEAPON].id = Editship.extraShips[z + 2];
                        player[1].items.special = Editship.extraShips[z + 3];
                        player[1].items.sidekick[Players.LEFT_SIDEKICK] = Editship.extraShips[z + 4];
                        player[1].items.sidekick[Players.RIGHT_SIDEKICK] = Editship.extraShips[z + 5];
                        player[1].items.generator = Editship.extraShips[z + 6];
                        /*Armor*/
                        player[1].items.shield = Editship.extraShips[z + 8];
                        Array.Clear(Config.shotMultiPos, 0, Config.shotMultiPos.Length);

                        if (player[1].weapon_mode > Varz.JE_portConfigs())
                            player[1].weapon_mode = 1;

                        Varz.tempW = (ushort)player[1].armor;
                        Varz.JE_getShipInfo();
                        if (player[1].armor > Varz.tempW && Tyrian2.editShip2)
                            player[1].armor = Varz.tempW;
                        else
                            Tyrian2.editShip2 = true;

                        SDL_Surface temp_surface = Video.VGAScreen;
                        Video.VGAScreen = Video.VGAScreenSeg;
                        Varz.JE_wipeShieldArmorBars();
                        Varz.JE_drawArmor();
                        Varz.JE_drawShield();
                        Video.VGAScreen = temp_surface;
                        Varz.JE_drawOptions();

                        keysactive[x] = false;
                    }
                }
            }
        }

        /* { In-Game Help } */
        if (keysactive[SdlKeys.SDL_SCANCODE_F1])
        {
            if (Config.isNetworkGame)
            {
                helpRequest = true;
            }
            else
            {
                JE_inGameHelp();
                Varz.skipStarShowVGA = true;
            }
        }

        /* {!Activate Nort Ship!} */
        if (keysactive[SdlKeys.SDL_SCANCODE_F2] && keysactive[SdlKeys.SDL_SCANCODE_F4] && keysactive[SdlKeys.SDL_SCANCODE_F6] && keysactive[SdlKeys.SDL_SCANCODE_F7] &&
            keysactive[SdlKeys.SDL_SCANCODE_F9] && keysactive[SdlKeys.SDL_SCANCODE_BACKSLASH] && keysactive[SdlKeys.SDL_SCANCODE_SLASH])
        {
            if (Config.isNetworkGame)
            {
                nortShipRequest = true;
            }
            else
            {
                player[0].items.ship = 12;                            // Nort Ship
                player[0].items.special = 13;                         // Astral Zone
                player[0].items.weapon[Players.FRONT_WEAPON].id = 36; // NortShip Super Pulse
                player[0].items.weapon[Players.REAR_WEAPON].id = 37;  // NortShip Spreader
                Varz.shipGr = 1;
            }
        }

        /* {Cheating} */
        if (!Config.isNetworkGame && !Config.twoPlayerMode && !Config.superTyrian && Config.superArcadeMode == VarzConst.SA_NONE)
        {
            if (keysactive[SdlKeys.SDL_SCANCODE_F2] && keysactive[SdlKeys.SDL_SCANCODE_F3] && keysactive[SdlKeys.SDL_SCANCODE_F6])
            {
                Config.youAreCheating = !Config.youAreCheating;
                keysactive[SdlKeys.SDL_SCANCODE_F2] = false;
            }

            if (keysactive[SdlKeys.SDL_SCANCODE_F2] && keysactive[SdlKeys.SDL_SCANCODE_F3] && (keysactive[SdlKeys.SDL_SCANCODE_F4] || keysactive[SdlKeys.SDL_SCANCODE_F5]))
            {
                for (uint i = 0; i < Players.player.Length; ++i)
                    player[i].armor = 0;

                Config.youAreCheating = !Config.youAreCheating;
                JE_drawTextWindow(Helptext.miscText[63 - 1]);
            }

            if (Params.constantPlay && keysactive[SdlKeys.SDL_SCANCODE_C])
            {
                Config.youAreCheating = !Config.youAreCheating;
                keysactive[SdlKeys.SDL_SCANCODE_C] = false;
            }
        }

        if (Config.superTyrian)
        {
            Config.youAreCheating = false;
        }

        /* {Personal Commands} */

        /* {DEBUG} */
        if (keysactive[SdlKeys.SDL_SCANCODE_F10] && keysactive[SdlKeys.SDL_SCANCODE_BACKSPACE])
        {
            keysactive[SdlKeys.SDL_SCANCODE_F10] = false;
            Tyrian2.debug = !Tyrian2.debug;

            Tyrian2.debugHist = 0;
            Tyrian2.debugHistCount = 0;

            /* YKS: clock ticks since midnight replaced by SDL_GetTicks */
            Tyrian2.lastDebugTime = Globals.Clock.Ticks;
        }

        /* {CHEAT-SKIP LEVEL} */
        if (keysactive[SdlKeys.SDL_SCANCODE_F2] && keysactive[SdlKeys.SDL_SCANCODE_F6] && (keysactive[SdlKeys.SDL_SCANCODE_F7] || keysactive[SdlKeys.SDL_SCANCODE_F8]) && !keysactive[SdlKeys.SDL_SCANCODE_F9] &&
            !Config.superTyrian && Config.superArcadeMode == VarzConst.SA_NONE)
        {
            if (Config.isNetworkGame)
            {
                skipLevelRequest = true;
            }
            else
            {
                Varz.levelTimer = true;
                Tyrian2.levelTimerCountdown = 0;
                Tyrian2.endLevel = true;
                Varz.levelEnd = 40;
            }
        }

        /* pause game */
        pause_pressed |= keysactive[SdlKeys.SDL_SCANCODE_P];

        /* in-game setup */
        ingamemenu_pressed |= keysactive[SdlKeys.SDL_SCANCODE_ESCAPE];

        if (keysactive[SdlKeys.SDL_SCANCODE_BACKSPACE])
        {
            /* toggle screenshot pause */
            if (keysactive[SdlKeys.SDL_SCANCODE_NUMLOCKCLEAR])
                Config.superPause = !Config.superPause;

            /* {SMOOTHIES} */
            if (keysactive[SdlKeys.SDL_SCANCODE_F12] && keysactive[SdlKeys.SDL_SCANCODE_SCROLLLOCK])
            {
                for (int temp = SdlKeys.SDL_SCANCODE_2; temp <= SdlKeys.SDL_SCANCODE_9; temp++)
                    if (keysactive[temp])
                        Config.smoothies[temp - SdlKeys.SDL_SCANCODE_2] = !Config.smoothies[temp - SdlKeys.SDL_SCANCODE_2];
                if (keysactive[SdlKeys.SDL_SCANCODE_0])
                    Config.smoothies[8] = !Config.smoothies[8];
            }
            else

            /* {CYCLE THROUGH FILTER COLORS} */
            if (keysactive[SdlKeys.SDL_SCANCODE_MINUS])
            {
                if (Config.levelFilter == -99)
                {
                    Config.levelFilter = 0;
                }
                else
                {
                    Config.levelFilter++;
                    if (Config.levelFilter == 16)
                        Config.levelFilter = -99;
                }
            }
            else

            /* {HYPER-SPEED} */
            if (keysactive[SdlKeys.SDL_SCANCODE_1])
            {
                Config.fastPlay++;
                if (Config.fastPlay > 2)
                    Config.fastPlay = 0;
                keysactive[SdlKeys.SDL_SCANCODE_1] = false;
                Config.JE_setNewGameSpeed();
            }

            /* {IN-GAME RANDOM MUSIC SELECTION} */
            if (keysactive[SdlKeys.SDL_SCANCODE_SCROLLLOCK])
                Loudness.play_song((byte)(MtRand.mt_rand() % Musmast.MUSIC_NUM));
        }
    }

    /// <summary>對應 mainint.c:JE_pauseGame —— 暫停畫面（顯示 PAUSE，等待按鍵，靜音音樂）。</summary>
    public static void JE_pauseGame()
    {
        Keyboard.mouseSetRelative(false);

        bool done = false;

        SDL_Surface temp_surface = Video.VGAScreen;
        Video.VGAScreen = Video.VGAScreenSeg; /* side-effect of game_screen */

        //tempScreenSeg = VGAScreenSeg; // sega000
        if (!Config.superPause)
        {
            Fonthand.JE_dString(Video.VGAScreenSeg, 120, 90, Helptext.miscText[22], (uint)Sprites.FONT_SHAPES);

            Video.VGAScreen = Video.VGAScreenSeg;
            Video.JE_showVGA();
        }

        Loudness.set_volume((byte)(Nortsong.tyrMusicVolume / 2), (byte)Nortsong.fxVolume);

        // WITH_NETWORK 分支略過（守則 8：網路不移植）

        do
        {
            Nortsong.setFrameCount(1);

            Keyboard.waitUntilHasInputOrElapsed();

            if ((Keyboard.keyboardGetInput(out KeyboardInput keyboardInput) &&
                 keyboardInput.scancode != SdlKeys.SDL_SCANCODE_LCTRL &&
                 keyboardInput.scancode != SdlKeys.SDL_SCANCODE_RCTRL &&
                 keyboardInput.scancode != SdlKeys.SDL_SCANCODE_LALT &&
                 keyboardInput.scancode != SdlKeys.SDL_SCANCODE_RALT) ||
                Keyboard.mouseGetInput(InputFlags.INPUT_NO_MOTION, out _))
            {
                // WITH_NETWORK 分支略過（守則 8）

                Keyboard.keysactive[SdlKeys.SDL_SCANCODE_P] = false;

                done = true;
            }

            // WITH_NETWORK 分支略過（守則 8）
        } while (!done);

        // WITH_NETWORK 分支略過（守則 8）

        Loudness.set_volume((byte)Nortsong.tyrMusicVolume, (byte)Nortsong.fxVolume);

        //skipStarShowVGA = true;

        Video.VGAScreen = temp_surface; /* side-effect of game_screen */

        Keyboard.mouseSetRelative(true);
    }

    /// <summary>對應 mainint.c:JE_doInGameSetup —— 遊戲中設定選單初始化。</summary>
    public static void JE_doInGameSetup()
    {
        Keyboard.mouseSetRelative(false);

        Tyrian2.haltGame = false;

        // WITH_NETWORK 分支略過（守則 8：網路不移植）

        if (Tyrian2.yourInGameMenuRequest)
        {
            if (JE_inGameSetup())
            {
                Tyrian2.reallyEndLevel = true;
                Tyrian2.playerEndLevel = true;
            }
            Tyrian2.quitRequested = false;

            Keyboard.keysactive[SdlKeys.SDL_SCANCODE_ESCAPE] = false;

            // WITH_NETWORK 分支略過（守則 8）
        }

        // WITH_NETWORK 分支略過（守則 8）

        Tyrian2.yourInGameMenuRequest = false;

        //skipStarShowVGA = true;

        Keyboard.mouseSetRelative(true);
    }

    /// <summary>對應 mainint.c:JE_inGameSetup —— 遊戲中設定選單主迴圈。</summary>
    public static bool JE_inGameSetup()
    {
        bool result = false;

        SDL_Surface temp_surface = Video.VGAScreen;
        Video.VGAScreen = Video.VGAScreenSeg; /* side-effect of game_screen */

        const int MENU_ITEM_MUSIC_VOLUME = 0;
        const int MENU_ITEM_EFFECTS_VOLUME = 1;
        const int MENU_ITEM_DETAIL_LEVEL = 2;
        const int MENU_ITEM_GAME_SPEED = 3;
        const int MENU_ITEM_RETURN_TO_GAME = 4;
        const int MENU_ITEM_QUIT = 5;

        int[] helpIndexes = { 14, 14, 27, 28, 25, 26 };

        if (Sprites.shopSpriteSheet.data == null)
            Sprites.JE_loadCompShapes(ref Sprites.shopSpriteSheet, '1');  // need mouse pointer sprites

        bool restart = true;

        int menuItemsCount = Helptext.inGameText.Length;
        int selectedIndex = MENU_ITEM_MUSIC_VOLUME;

        const int yMenuItems = 20;
        const int dyMenuItems = 20;
        const int xMenuItem = 10;
        const int xMenuItemName = xMenuItem;
        const int wMenuItemName = 110;
        const int xMenuItemValue = xMenuItemName + wMenuItemName;
        const int wMenuItemValue = 90;
        const int wMenuItem = wMenuItemName + wMenuItemValue;
        const int hMenuItem = 13;

        for (bool done = false; !done; )
        {
            Nortsong.setFrameCount(1);

            if (restart)
            {
                // Main box
                Vga256d.JE_barShade(Video.VGAScreen, 3, 13, 217, 137);
                Vga256d.JE_barShade(Video.VGAScreen, 5, 15, 215, 135);

                // Help box
                Vga256d.JE_barShade(Video.VGAScreen, 3, 143, 257, 157);
                Vga256d.JE_barShade(Video.VGAScreen, 5, 145, 255, 155);

                CopyScreen(Video.VGAScreen2, Video.VGAScreen);

                Mouse.mouseCursor = Mouse.MOUSE_POINTER_NORMAL;

                restart = false;
            }

            // Restore background.
            CopyScreen(Video.VGAScreen, Video.VGAScreen2);

            // Draw menu items.
            for (int i = 0; i < menuItemsCount; ++i)
            {
                int y = yMenuItems + dyMenuItems * i;

                string name = Helptext.inGameText[i];

                bool selected = i == selectedIndex;

                FontDraw.drawFontHvShadow(Video.VGAScreen, xMenuItemName, y, name, Font.FONT_NORMAL, 15, (sbyte)(-4 + (selected ? 2 : 0)), false, 2);

                switch (i)
                {
                case MENU_ITEM_MUSIC_VOLUME:
                {
                    Nortvars.JE_barDrawShadow(Video.VGAScreen, xMenuItemValue, y, 1, Loudness.music_disabled ? 12 : 16, (Nortsong.tyrMusicVolume + 6) / 12, 3, 13);
                    break;
                }
                case MENU_ITEM_EFFECTS_VOLUME:
                {
                    Nortvars.JE_barDrawShadow(Video.VGAScreen, xMenuItemValue, y, 1, Loudness.samples_disabled ? 12 : 16, (Nortsong.fxVolume + 6) / 12, 3, 13);
                    break;
                }
                case MENU_ITEM_DETAIL_LEVEL:
                {
                    FontDraw.drawFontHvShadow(Video.VGAScreen, xMenuItemValue, y, Helptext.detailLevel[Config.processorType - 1], Font.FONT_NORMAL, 15, (sbyte)(-4 + (selected ? 2 : 0)), false, 2);
                    break;
                }
                case MENU_ITEM_GAME_SPEED:
                {
                    FontDraw.drawFontHvShadow(Video.VGAScreen, xMenuItemValue, y, Helptext.gameSpeedText[Config.gameSpeed - 1], Font.FONT_NORMAL, 15, (sbyte)(-4 + (selected ? 2 : 0)), false, 2);
                    break;
                }
                }
            }

            // Draw help text.
            Fonthand.JE_outTextAdjust(Video.VGAScreen, 10, 147, Helptext.mainMenuHelp[helpIndexes[selectedIndex]], 14, 6, (uint)Sprites.TINY_FONT, true);

            Mouse.JE_mouseStart();
            Video.JE_showVGA();
            Mouse.JE_mouseReplace();

            Keyboard.waitUntilElapsed();
            Keyboard.waitUntilHasInput(InputFlags.INPUT_ANY);

            // Handle interaction.

            bool action = false;
            bool leftAction = false;
            bool rightAction = false;

            if (Keyboard.mouseGetInput(InputFlags.INPUT_ANY, out MouseInput mouseInput))
            {
                // Find menu item that was hovered or clicked.
                if (mouseInput.x >= xMenuItem && mouseInput.x < xMenuItem + wMenuItem)
                {
                    for (int i = 0; i < menuItemsCount; ++i)
                    {
                        int yMenuItem = yMenuItems + dyMenuItems * i;
                        if (mouseInput.y >= yMenuItem && mouseInput.y < yMenuItem + hMenuItem)
                        {
                            if (selectedIndex != i)
                            {
                                Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);

                                selectedIndex = i;
                            }

                            if (mouseInput.button == SdlKeys.SDL_BUTTON_LEFT &&
                                mouseInput.x >= xMenuItem && mouseInput.x < xMenuItem + wMenuItem &&
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
                                    switch (i)
                                    {
                                    case MENU_ITEM_MUSIC_VOLUME:
                                    {
                                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);

                                        int w = ((255 + 6) / 12) * (3 + 1) - 1;

                                        int value = (mouseInput.x - xMenuItemValue) * 255 / (w - 1);
                                        Nortsong.tyrMusicVolume = (ushort)Math.Min(Math.Max(0, value), 255);

                                        Loudness.set_volume((byte)Nortsong.tyrMusicVolume, (byte)Nortsong.fxVolume);
                                        break;
                                    }
                                    case MENU_ITEM_EFFECTS_VOLUME:
                                    {
                                        int w = ((255 + 6) / 12) * (3 + 1) - 1;

                                        int value = (mouseInput.x - xMenuItemValue) * 255 / (w - 1);
                                        Nortsong.fxVolume = (ushort)Math.Min(Math.Max(0, value), 255);

                                        Loudness.set_volume((byte)Nortsong.tyrMusicVolume, (byte)Nortsong.fxVolume);

                                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                                        break;
                                    }
                                    case MENU_ITEM_DETAIL_LEVEL:
                                    case MENU_ITEM_GAME_SPEED:
                                    {
                                        rightAction = true;
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

                    done = true;
                }
            }
            else if (Keyboard.keyboardGetInput(out KeyboardInput keyboardInput))
            {
                switch (keyboardInput.scancode)
                {
                case SdlKeys.SDL_SCANCODE_UP:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);

                    selectedIndex = selectedIndex == 0
                        ? menuItemsCount - 1
                        : selectedIndex - 1;
                    break;
                }
                case SdlKeys.SDL_SCANCODE_DOWN:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);

                    selectedIndex = selectedIndex == menuItemsCount - 1
                        ? 0
                        : selectedIndex + 1;
                    break;
                }
                case SdlKeys.SDL_SCANCODE_LEFT:
                {
                    leftAction = true;
                    break;
                }
                case SdlKeys.SDL_SCANCODE_RIGHT:
                {
                    rightAction = true;
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

                    done = true;
                    break;
                }
                case SdlKeys.SDL_SCANCODE_W:
                {
                    if (selectedIndex == MENU_ITEM_DETAIL_LEVEL)
                    {
                        Config.processorType = 6;
                        Config.JE_initProcessorType();
                    }
                    break;
                }
                default:
                    break;
                }
            }

            if (action)
            {
                switch (selectedIndex)
                {
                case MENU_ITEM_MUSIC_VOLUME:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_SELECT);

                    Loudness.music_disabled = !Loudness.music_disabled;
                    break;
                }
                case MENU_ITEM_EFFECTS_VOLUME:
                {
                    Loudness.samples_disabled = !Loudness.samples_disabled;

                    Nortsong.JE_playSampleNum((byte)Sndmast.S_SELECT);
                    break;
                }
                case MENU_ITEM_RETURN_TO_GAME:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_SELECT);

                    done = true;
                    break;
                }
                case MENU_ITEM_QUIT:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_SELECT);

                    if (Params.constantPlay)
                        Varz.JE_tyrianHalt(0);

                    if (Config.isNetworkGame)
                    {
                        /*Tell other computer to exit*/
                        Tyrian2.haltGame = true;
                        Tyrian2.playerEndLevel = true;
                    }

                    result = true;
                    done = true;
                    break;
                }
                default:
                    break;
                }
            }
            else if (leftAction)
            {
                switch (selectedIndex)
                {
                case MENU_ITEM_MUSIC_VOLUME:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);

                    Nortsong.JE_changeVolume(ref Nortsong.tyrMusicVolume, -12, ref Nortsong.fxVolume, 0);
                    break;
                }
                case MENU_ITEM_EFFECTS_VOLUME:
                {
                    Nortsong.JE_changeVolume(ref Nortsong.tyrMusicVolume, 0, ref Nortsong.fxVolume, -12);

                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                    break;
                }
                case MENU_ITEM_DETAIL_LEVEL:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);

                    Config.processorType = (byte)(Config.processorType > 1 ? Config.processorType - 1 : 4);
                    Config.JE_initProcessorType();
                    Config.JE_setNewGameSpeed();
                    break;
                }
                case MENU_ITEM_GAME_SPEED:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);

                    Config.gameSpeed = (byte)(Config.gameSpeed > 1 ? Config.gameSpeed - 1 : 5);
                    Config.JE_initProcessorType();
                    Config.JE_setNewGameSpeed();
                    break;
                }
                default:
                    break;
                }
            }
            else if (rightAction)
            {
                switch (selectedIndex)
                {
                case MENU_ITEM_MUSIC_VOLUME:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);

                    Nortsong.JE_changeVolume(ref Nortsong.tyrMusicVolume, 12, ref Nortsong.fxVolume, 0);
                    break;
                }
                case MENU_ITEM_EFFECTS_VOLUME:
                {
                    Nortsong.JE_changeVolume(ref Nortsong.tyrMusicVolume, 0, ref Nortsong.fxVolume, 12);

                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                    break;
                }
                case MENU_ITEM_DETAIL_LEVEL:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);

                    Config.processorType = (byte)(Config.processorType < 4 ? Config.processorType + 1 : 1);
                    Config.JE_initProcessorType();
                    Config.JE_setNewGameSpeed();
                    break;
                }
                case MENU_ITEM_GAME_SPEED:
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);

                    Config.gameSpeed = (byte)(Config.gameSpeed < 5 ? Config.gameSpeed + 1 : 1);
                    Config.JE_initProcessorType();
                    Config.JE_setNewGameSpeed();
                    break;
                }
                default:
                    break;
                }
            }
        }

        Video.VGAScreen = temp_surface; /* side-effect of game_screen */

        return result;
    }

    /// <summary>對應 mainint.c:JE_inGameHelp —— 遊戲中說明畫面。</summary>
    public static void JE_inGameHelp()
    {
        Keyboard.mouseSetRelative(false);

        Nortsong.setFrameCount(1);

        SDL_Surface temp_surface = Video.VGAScreen;
        Video.VGAScreen = Video.VGAScreenSeg; /* side-effect of game_screen */

        //tempScreenSeg = VGAScreenSeg;

        Vga256d.JE_barShade(Video.VGAScreen, 1, 1, 262, 182); /*Main Box*/
        Vga256d.JE_barShade(Video.VGAScreen, 3, 3, 260, 180);
        Vga256d.JE_barShade(Video.VGAScreen, 5, 5, 258, 178);
        Vga256d.JE_barShade(Video.VGAScreen, 7, 7, 256, 176);
        Vga256d.fill_rectangle_xy(Video.VGAScreen, 9, 9, 254, 174, 0);

        if (Config.twoPlayerMode)  // Two-Player Help
        {
            Helptext.JE_HBox(Video.VGAScreen, 20, 4, 36, 50, 7, 3, 3);

            // weapon help
            Sprites.blit_sprite(Video.VGAScreenSeg, 2, 21, (uint)Sprites.OPTION_SHAPES, 43);
            Helptext.JE_HBox(Video.VGAScreen, 55, 20, 37, 40, 7, 5, 3);

            // sidekick help
            Sprites.blit_sprite(Video.VGAScreenSeg, 5, 36, (uint)Sprites.OPTION_SHAPES, 41);
            Helptext.JE_HBox(Video.VGAScreen, 40, 43, 34, 44, 7, 5, 3);

            // shield/armor help
            Sprites.blit_sprite(Video.VGAScreenSeg, 2, 79, (uint)Sprites.OPTION_SHAPES, 42);
            Helptext.JE_HBox(Video.VGAScreen, 54, 84, 35, 40, 7, 5, 3);

            Helptext.JE_HBox(Video.VGAScreen, 5, 126, 38, 55, 7, 5, 3);
            Helptext.JE_HBox(Video.VGAScreen, 5, 160, 39, 55, 7, 5, 3);
        }
        else
        {
            // power bar help
            Sprites.blit_sprite(Video.VGAScreenSeg, 15, 5, (uint)Sprites.OPTION_SHAPES, 40);
            Helptext.JE_HBox(Video.VGAScreen, 40, 10, 31, 45, 7, 5, 3);

            // weapon help
            Sprites.blit_sprite(Video.VGAScreenSeg, 5, 37, (uint)Sprites.OPTION_SHAPES, 39);
            Helptext.JE_HBox(Video.VGAScreen, 40, 40, 32, 44, 7, 5, 3);
            Helptext.JE_HBox(Video.VGAScreen, 40, 60, 33, 44, 7, 5, 3);

            // sidekick help
            Sprites.blit_sprite(Video.VGAScreenSeg, 5, 98, (uint)Sprites.OPTION_SHAPES, 41);
            Helptext.JE_HBox(Video.VGAScreen, 40, 103, 34, 44, 7, 5, 3);

            // shield/armor help
            Sprites.blit_sprite(Video.VGAScreenSeg, 2, 138, (uint)Sprites.OPTION_SHAPES, 42);
            Helptext.JE_HBox(Video.VGAScreen, 54, 143, 35, 40, 7, 5, 3);
        }

        // "press a key"
        Sprites.blit_sprite(Video.VGAScreenSeg, 16, 189, (uint)Sprites.OPTION_SHAPES, 36);  // in-game text area
        Fonthand.JE_outText(Video.VGAScreenSeg, 120 - Fonthand.JE_textWidth(Helptext.miscText[5 - 1], (uint)Sprites.TINY_FONT) / 2 + 20, 190, Helptext.miscText[5 - 1], 0, 4);

        while (true)
        {
            Mouse.JE_mouseStart();
            Video.JE_showVGA();
            Mouse.JE_mouseReplace();

            Keyboard.waitUntilElapsed();
            Keyboard.waitUntilHasInput(InputFlags.INPUT_ANY);

            if (Keyboard.getInput())
                break;

            Nortsong.setFrameCount(1);
        }

        textErase = 1;

        Video.VGAScreen = temp_surface;

        Keyboard.mouseSetRelative(true);
    }
    public static void JE_handleChat() { /* TODO: 網路聊天（network.c 不移植） */ }
    /// <summary>對應 mainint.c:JE_gammaCorrect_func —— 單一色階乘 r 並夾 255。</summary>
    private static void JE_gammaCorrect_func(ref byte col, float r)
    {
        int temp = (int)MathF.Round(col * r, MidpointRounding.AwayFromZero);
        if (temp > 255)
            temp = 255;
        col = (byte)temp;
    }

    /// <summary>對應 mainint.c:JE_gammaCorrect —— 對整盤 256 色套 gamma(r=1+gamma/10)。</summary>
    public static void JE_gammaCorrect(SDL_Color[] colorBuffer, byte gamma)
    {
        float r = 1 + (float)gamma / 10;
        for (int x = 0; x < 256; x++)
        {
            JE_gammaCorrect_func(ref colorBuffer[x].r, r);
            JE_gammaCorrect_func(ref colorBuffer[x].g, r);
            JE_gammaCorrect_func(ref colorBuffer[x].b, r);
        }
    }
    /// <summary>對應 backgrnd.c:JE_filterScreen —— 濾鏡淡入淡出 + 全螢幕色相覆蓋 + 爆炸透明亮度。</summary>
    public static unsafe void JE_filterScreen(sbyte col, sbyte int_)
    {
        byte* s;
        int x, y;
        uint temp;

        if (Config.filterFade)
        {
            Config.levelBrightness += Config.levelBrightnessChg;
            if ((Config.filterFadeStart && Config.levelBrightness < -14) || Config.levelBrightness > 14)
            {
                Config.levelBrightnessChg = (sbyte)-Config.levelBrightnessChg;
                Config.filterFadeStart = false;
                Config.levelFilter = Config.levelFilterNew;
            }
            if (!Config.filterFadeStart && Config.levelBrightness == 0)
            {
                Config.filterFade = false;
                Config.levelBrightness = -99;
            }
        }

        if (col != -99 && Config.filtrationAvail)
        {
            s = Video.VGAScreen.pixels;
            s += 24;

            col = (sbyte)(col << 4);

            for (y = 184; y != 0; y--)
            {
                for (x = 264; x != 0; x--)
                {
                    *s = (byte)(col | (*s & 0x0f));
                    s++;
                }
                s += Video.VGAScreen.pitch - 264;
            }
        }

        if (int_ != -99 && Config.explosionTransparent)
        {
            s = Video.VGAScreen.pixels;
            s += 24;

            for (y = 184; y != 0; y--)
            {
                for (x = 264; x != 0; x--)
                {
                    temp = (uint)((*s & 0x0f) + int_);
                    *s = (byte)((*s & 0xf0) | (temp >= 0x1f ? 0 : (temp >= 0x0f ? 0x0f : temp)));
                    s++;
                }
                s += Video.VGAScreen.pitch - 264;
            }
        }
    }

    /// <summary>對應 backgrnd.c:lava_filter —— 熔岩波動濾鏡（紅色相，上下取樣平均，含 waver 位移）。</summary>
    public static unsafe void lava_filter(SDL_Surface dst, SDL_Surface src)
    {
        int dst_pitch = dst.pitch;
        byte* dst_pixel = dst.pixels + (185 * dst_pitch);
        byte* dst_pixel_ll = dst.pixels;

        int src_pitch = src.pitch;
        byte* src_pixel = src.pixels + (185 * src.pitch);
        byte* src_pixel_ll = src.pixels;

        int w = 320 * 185 - 1;

        for (int y = 185 - 1; y >= 0; --y)
        {
            dst_pixel -= (dst_pitch - 320);
            src_pixel -= (src_pitch - 320);

            for (int x = 320 - 1; x >= 0; x -= 8)
            {
                int waver = Math.Abs(((w >> 9) & 0x0f) - 8) - 1;
                w -= 8;

                for (int xi = 8 - 1; xi >= 0; --xi)
                {
                    --dst_pixel;
                    --src_pixel;

                    int value = 0;
                    if (src_pixel + waver >= src_pixel_ll)
                        value += (*(src_pixel + waver) & 0x0f) * 2;
                    value += *(dst_pixel + waver + dst_pitch) & 0x0f;
                    if (dst_pixel + waver - dst_pitch >= dst_pixel_ll)
                        value += *(dst_pixel + waver - dst_pitch) & 0x0f;

                    *dst_pixel = (byte)((value / 4) | 0x70);
                }
            }
        }
    }

    /// <summary>對應 backgrnd.c:water_filter —— 水面濾鏡（非藍直接複製，否則與下方取樣平均、套 smoothie 色相）。</summary>
    public static unsafe void water_filter(SDL_Surface dst, SDL_Surface src)
    {
        byte hue = (byte)(Backgrnd.smoothie_data[1] << 4);

        int dst_pitch = dst.pitch;
        byte* dst_pixel = dst.pixels + (185 * dst_pitch);

        byte* src_pixel = src.pixels + (185 * src.pitch);

        int w = 320 * 185 - 1;

        for (int y = 185 - 1; y >= 0; --y)
        {
            dst_pixel -= (dst_pitch - 320);
            src_pixel -= (src.pitch - 320);

            for (int x = 320 - 1; x >= 0; x -= 8)
            {
                int waver = Math.Abs(((w >> 10) & 0x07) - 4) - 1;
                w -= 8;

                for (int xi = 8 - 1; xi >= 0; --xi)
                {
                    --dst_pixel;
                    --src_pixel;

                    if ((*src_pixel & 0x30) == 0)
                    {
                        *dst_pixel = *src_pixel;
                    }
                    else
                    {
                        int value = *src_pixel & 0x0f;
                        value += *(dst_pixel + waver + dst_pitch) & 0x0f;
                        *dst_pixel = (byte)((value / 2) | hue);
                    }
                }
            }
        }
    }

    /// <summary>對應 backgrnd.c:iced_blur_filter —— 冰藍模糊（來源與目標 value 取平均，色相 0x80）。</summary>
    public static unsafe void iced_blur_filter(SDL_Surface dst, SDL_Surface src)
    {
        byte* dst_pixel = dst.pixels;
        byte* src_pixel = src.pixels;

        for (int y = 0; y < 184; ++y)
        {
            for (int x = 0; x < 320; ++x)
            {
                int value = (*src_pixel & 0x0f) + (*dst_pixel & 0x0f);
                *dst_pixel = (byte)((value / 2) | 0x80);

                ++dst_pixel;
                ++src_pixel;
            }

            dst_pixel += (dst.pitch - 320);
            src_pixel += (src.pitch - 320);
        }
    }

    /// <summary>對應 backgrnd.c:blur_filter —— 一般模糊（value 取平均，色相取來源高 nibble）。</summary>
    public static unsafe void blur_filter(SDL_Surface dst, SDL_Surface src)
    {
        byte* dst_pixel = dst.pixels;
        byte* src_pixel = src.pixels;

        for (int y = 0; y < 184; ++y)
        {
            for (int x = 0; x < 320; ++x)
            {
                int value = (*src_pixel & 0x0f) + (*dst_pixel & 0x0f);
                *dst_pixel = (byte)((value / 2) | (*src_pixel & 0xf0));

                ++dst_pixel;
                ++src_pixel;
            }

            dst_pixel += (dst.pitch - 320);
            src_pixel += (src.pitch - 320);
        }
    }

    /// <summary>對應 mainint.c:JE_drawTextWindow —— 遊戲內文字視窗。</summary>
    public static void JE_drawTextWindow(string text)
    {
        if (textErase > 0) // erase current text
            Sprites.blit_sprite(Video.VGAScreenSeg, 16, 189, (uint)Sprites.OPTION_SHAPES, 36); // in-game text area

        textErase = 100;
        Fonthand.JE_outText(Video.VGAScreen, 20, 190, text, 0, 4);
    }

    /// <summary>對應 mainint.c:weapon_upgrade_cost —— 武器升級成本（base × Σ1..power）。</summary>
    public static long weapon_upgrade_cost(long base_cost, uint power)
    {
        uint temp = 0;
        for (; power > 0; power--) temp += power;
        return base_cost * temp;
    }

    /// <summary>對應 mainint.c:JE_getCost —— 物品價格（武器另計算升/降級成本）。</summary>
    public static long JE_getCost(byte itemType, ushort itemNum)
    {
        long cost = 0;
        switch (itemType)
        {
            case 2:
                cost = (itemNum > 90) ? 100 : Episodes.ships[itemNum].cost;
                break;
            case 3:
            case 4:
                cost = Episodes.weaponPort[itemNum].cost;
                uint port = (uint)(itemType - 3);
                uint item_power = (uint)(Players.player[0].items.weapon[(int)port].power - 1);
                downgradeCost = (ushort)weapon_upgrade_cost(cost, item_power);
                upgradeCost = (ushort)weapon_upgrade_cost(cost, item_power + 1);
                break;
            case 5: cost = Episodes.shields[itemNum].cost; break;
            case 6: cost = Episodes.powerSys[itemNum].cost; break;
            case 7:
            case 8: cost = Episodes.options[itemNum].cost; break;
        }
        return cost;
    }

    /// <summary>對應 mainint.c:adjust_difficulty —— 依分數調整難度（取 max）。</summary>
    public static void adjust_difficulty()
    {
        float[] score_multiplier = { 0, 0.4f, 0.8f, 1.3f, 1.6f, 2, 2, 3, 3, 3 };
        var player = Players.player;
        int idiff = Config.initialDifficulty;
        if (idiff < 1 || idiff > 9) idiff = 2;

        ulong score = Config.twoPlayerMode ? ((ulong)player[0].cash + player[1].cash) : player[0].cash;
        ulong adjusted = (ulong)MathF.Round(score * score_multiplier[idiff]);

        int nd;
        if (Config.twoPlayerMode)
            nd = adjusted < 10000 ? Config.DIFFICULTY_EASY : adjusted < 20000 ? Config.DIFFICULTY_NORMAL :
                 adjusted < 50000 ? Config.DIFFICULTY_HARD : adjusted < 80000 ? Config.DIFFICULTY_IMPOSSIBLE :
                 adjusted < 125000 ? Config.DIFFICULTY_INSANITY : adjusted < 200000 ? Config.DIFFICULTY_SUICIDE :
                 adjusted < 400000 ? Config.DIFFICULTY_MANIACAL : adjusted < 600000 ? Config.DIFFICULTY_ZINGLON : Config.DIFFICULTY_NORTANEOUS;
        else
            nd = adjusted < 40000 ? Config.DIFFICULTY_EASY : adjusted < 70000 ? Config.DIFFICULTY_NORMAL :
                 adjusted < 150000 ? Config.DIFFICULTY_HARD : adjusted < 300000 ? Config.DIFFICULTY_IMPOSSIBLE :
                 adjusted < 600000 ? Config.DIFFICULTY_INSANITY : adjusted < 1000000 ? Config.DIFFICULTY_SUICIDE :
                 adjusted < 2000000 ? Config.DIFFICULTY_MANIACAL : adjusted < 3000000 ? Config.DIFFICULTY_ZINGLON : Config.DIFFICULTY_NORTANEOUS;

        Config.difficultyLevel = (sbyte)Math.Max((int)Config.difficultyLevel, nd);
    }

    /// <summary>
    /// 移植 mainint.c:JE_endLevelAni —— 過關摘要畫面（完成關卡名/現金/摧毀率/難度調整/ShipEdit 權限）。
    /// 簡化：略過 cube 收集動畫。
    /// </summary>
    public static void JE_endLevelAni()
    {
        var player = Players.player;
        var seg = Video.VGAScreenSeg;

        if (!Params.constantPlay)
        {
            // 開放 ShipEdit 權限
            for (int p = 0; p < 2; ++p)
            {
                for (int i = 0; i < 2; ++i)
                {
                    int e = player[p].items.weapon[i].id - 1;
                    if (e >= 0 && e < Config.editorItemAvail.Length) Config.editorItemAvail[e] = 1;
                }
                for (int i = 0; i < 2; ++i)
                {
                    int e = 50 + player[p].items.sidekick[i];
                    if (e >= 0 && e < Config.editorItemAvail.Length) Config.editorItemAvail[e] = 1;
                }
            }
            int es = 80 + player[0].items.special;
            if (es < Config.editorItemAvail.Length) Config.editorItemAvail[es] = 1;
        }

        adjust_difficulty();

        player[0].last_items = player[0].items;
        StrCpy(Config.lastLevelName, CStr(Config.levelName));

        Nortsong.frameCountMax = 4;
        Fonthand.textGlowFont = (byte)Sprites.SMALL_FONT_SHAPES;
        Palette.set_colors(new SDL_Color(255, 255, 255), 254, 254);

        if (!Varz.levelTimer || levelTimerCountdown > 0 || Episodes.episodeNum != 4)
            Nortsong.JE_playSampleNum((byte)Sndmast.V_LEVEL_END);
        else
            Loudness.play_song(21);

        string lvl = CStr(Config.levelName);
        if (Episodes.bonusLevel)
            Fonthand.JE_outTextGlow(seg, 20, 20, Helptext.miscText[16]);
        else if (Players.all_players_alive())
            Fonthand.JE_outTextGlow(seg, 20, 20, $"{Helptext.miscText[26]} {lvl}"); // Completed
        else
            Fonthand.JE_outTextGlow(seg, 20, 20, $"{Helptext.miscText[61]} {lvl}"); // Exiting

        if (Config.twoPlayerMode)
            for (int i = 0; i < 2; ++i)
                Fonthand.JE_outTextGlow(seg, 30, 50 + 20 * i, $"{Helptext.miscText[40 + i]} {player[i].cash}");
        else
            Fonthand.JE_outTextGlow(seg, 30, 50, $"{Helptext.miscText[27]} {player[0].cash}");

        int pct = totalEnemyZero() ? 0 : (int)MathF.Round(enemyKilled * 100f / totalEnemyOf());
        Fonthand.JE_outTextGlow(seg, 40, 90, $"{Helptext.miscText[62]} {pct}%");
        if (!Params.constantPlay)
            editorLevel += (ushort)(pct / 5);

        if (!Config.onePlayerAction && !Config.twoPlayerMode)
        {
            Fonthand.JE_outTextGlow(seg, 30, 120, Helptext.miscText[3]); // Cubes

            if (Config.cubeMax > 0)
            {
                if (Config.cubeMax > Config.cubeList.Length)
                    Config.cubeMax = (ushort)Config.cubeList.Length;

                if (Nortsong.frameCountMax != 0)
                    Nortsong.frameCountMax = 1;

                for (int t = 1; t <= Config.cubeMax; t++)
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_ITEM);
                    int x = 20 + 30 * t, y = 135;
                    JE_drawCube(seg, x, y, 9, 0);
                    Video.JE_showVGA();

                    for (int i = -15; i <= 10; i++)
                    {
                        Nortsong.setFrameCount(Nortsong.frameCountMax);
                        Sprites.blit_sprite_hv(seg, x, y, (uint)Sprites.OPTION_SHAPES, 25, 9, (sbyte)i);
                        Video.JE_showVGA();
                        if (Keyboard.waitUntilGetInputOrElapsed())
                            Nortsong.frameCountMax = 0;
                    }
                    for (int i = 10; i >= 0; i--)
                    {
                        Nortsong.setFrameCount(Nortsong.frameCountMax);
                        Sprites.blit_sprite_hv(seg, x, y, (uint)Sprites.OPTION_SHAPES, 25, 9, (sbyte)i);
                        Video.JE_showVGA();
                        if (Keyboard.waitUntilGetInputOrElapsed())
                            Nortsong.frameCountMax = 0;
                    }
                }
            }
            else
            {
                Fonthand.JE_outTextGlow(seg, 50, 135, Helptext.miscText[14]); // None
            }
        }

        Nortsong.frameCountMax = 6;
        Fonthand.JE_outTextGlow(seg, 90, Config.twoPlayerMode ? 150 : 160, Helptext.miscText[4]);

        if (!Params.constantPlay)
            Keyboard.waitUntilGetInput();

        Palette.fade_black(15);
        Video.JE_clr256(Video.VGAScreen);
    }

    /// <summary>對應 mainint.c:JE_drawCube —— 畫一個資料方塊（含陰影）。</summary>
    public static void JE_drawCube(SDL_Surface screen, int x, int y, byte filter, sbyte brightness)
    {
        Sprites.blit_sprite_dark(screen, x + 4, y + 4, (uint)Sprites.OPTION_SHAPES, 25, false);
        Sprites.blit_sprite_dark(screen, x + 3, y + 3, (uint)Sprites.OPTION_SHAPES, 25, false);
        Sprites.blit_sprite_hv(screen, x, y, (uint)Sprites.OPTION_SHAPES, 25, filter, brightness);
    }

    private static bool totalEnemyZero() => Tyrian2.totalEnemy == 0;
    private static int totalEnemyOf() => Tyrian2.totalEnemy;
    private static ushort enemyKilled => Tyrian2.enemyKilled;
    private static ushort levelTimerCountdown => Tyrian2.levelTimerCountdown;
    private static ushort editorLevel { get => Config.editorLevel; set => Config.editorLevel = value; }

    /// <summary>對應 mainint.c:JE_inGameDisplays —— 遊戲內 HUD（分數/特殊武器/超級炸彈）。街機 lives 待 lives 指標移植。</summary>
    public static void JE_inGameDisplays()
    {
        var player = Players.player;

        for (int i = 0; i < ((Config.twoPlayerMode && !Config.galagaMode) ? 2 : 1); ++i)
            Fonthand.JE_textShade(Video.VGAScreen, 30 + 200 * i, 175, $"{player[i].cash}", 2, 4, Fonthand.FULL_SHADE);

        if (player[0].items.special > 0)
            Sprites.blit_sprite2x2(Video.VGAScreen, 25, 1, Sprites.spriteSheet10, Episodes.special[player[0].items.special].itemgraphic);

        // 街機/雙人模式殘機顯示（對應 mainint.c 2945-2980）
        if (Config.onePlayerAction || Config.twoPlayerMode)
        {
            for (int temp = 0; temp < (Config.onePlayerAction ? 1 : 2); temp++)
            {
                uint extra_lives = (uint)(player[temp].Lives - 1);
                int y = (temp == 0 && player[0].items.special > 0) ? 35 : 15;
                int tw = (temp == 0) ? 30 : 270;

                if (extra_lives >= 5)
                {
                    Sprites.blit_sprite2(Video.VGAScreen, tw, y, Sprites.spriteSheet9, 285);
                    tw = (temp == 0) ? 45 : 250;
                    Fonthand.JE_textShade(Video.VGAScreen, tw, y + 3, $"{extra_lives}", 15, 1, Fonthand.FULL_SHADE);
                }
                else if (extra_lives >= 1)
                {
                    for (uint i = 0; i < extra_lives; ++i)
                    {
                        Sprites.blit_sprite2(Video.VGAScreen, tw, y, Sprites.spriteSheet9, 285);
                        tw += (temp == 0) ? 12 : -12;
                    }
                }

                string stemp = (temp == 0) ? Helptext.miscText[48] : Helptext.miscText[49];
                tw = (temp == 0) ? 28 : (285 - Fonthand.JE_textWidth(stemp, (uint)Sprites.TINY_FONT));
                Fonthand.JE_textShade(Video.VGAScreen, tw, y - 7, stemp, 2, 6, Fonthand.FULL_SHADE);
            }
        }

        for (int i = 0; i < 2; ++i)
        {
            int x = (i == 0) ? 30 : 270;
            for (uint j = player[i].superbombs; j > 0; --j)
            {
                Sprites.blit_sprite2(Video.VGAScreen, x, 160, Sprites.spriteSheet9, 304);
                x += (i == 0) ? 12 : -12;
            }
        }

        if (Config.youAreCheating)
            Fonthand.JE_outText(Video.VGAScreen, 90, 170, "Cheaters always prosper.", 3, 4);
    }

    /// <summary>mainint.c: button[4] — 開火 / 左火 / 右火 / 模式切換。</summary>
    public static readonly bool[] button = new bool[4];

    /// <summary>對應 mainint.c:JE_operation。目前僅移植 load 路徑；存檔命名對話框(performSave) 待 in-game 選單。</summary>
    public static void JE_operation(byte slot)
    {
        if (!performSave)
        {
            if (Config.saveFiles[slot - 1].level > 0)
            {
                Config.gameJustLoaded = true;
                Config.JE_loadGame(slot);
                Varz.gameLoaded = true;
            }
        }
        // else: 存檔命名輸入對話框（performSave==true，由 in-game 選單觸發）待後續移植
    }

    public static bool JE_loadScreen()
    {
        if (Sprites.shopSpriteSheet.data == null)
            Sprites.JE_loadCompShapes(ref Sprites.shopSpriteSheet, '1');

        bool restart = true;
        int playersIndex = 0;
        const int menuItemsCount = 12;
        int selectedIndex = 0;

        const int xCenter = 320 / 2, yMenuHeader = 5, xMenuItem = 10, xMenuItemName = 10,
                  xMenuItemLastLevel = 120, xMenuItemEpisode = 250, wMenuItem = 300,
                  yMenuItems = 30, dyMenuItems = 13, hMenuItem = 8,
                  xLeftControl = 83, xRightControl = 213, wControl = 24, yControls = 179;

        for (; ; )
        {
            Nortsong.setFrameCount(1);

            if (restart)
            {
                Picload.JE_loadPic(Video.VGAScreen2, 2, false);
                Vga256d.fill_rectangle_wh(Video.VGAScreen2, 0, 192, 320, 8, 0);
            }

            CopyScreen(Video.VGAScreen, Video.VGAScreen2);

            FontDraw.drawFontHvShadowAligned(Video.VGAScreen, xCenter, yMenuHeader, Helptext.miscText[38 + playersIndex], Font.FONT_LARGE, FontAlignment.ALIGN_CENTER, 15, -3, false, 2);

            for (int i = 0; i < menuItemsCount; ++i)
            {
                int y = yMenuItems + dyMenuItems * i;
                bool selected = i == selectedIndex;

                if (i == menuItemsCount - 1)
                {
                    Fonthand.JE_textShade(Video.VGAScreen, xMenuItemName, y, Helptext.miscText[33], 13, selected ? 6 : 2, Fonthand.FULL_SHADE);
                    continue;
                }

                var sf = Config.saveFiles[playersIndex * 11 + i];
                bool disabled = sf.level == 0;

                if (disabled)
                {
                    Fonthand.JE_textShade(Video.VGAScreen, xMenuItemName, y, Helptext.miscText[2], 13, selected ? 6 : 0, Fonthand.FULL_SHADE);
                    Fonthand.JE_textShade(Video.VGAScreen, xMenuItemLastLevel, y, $"{Helptext.miscTextB[2]} -----", 5, selected ? 6 : 0, Fonthand.FULL_SHADE);
                }
                else
                {
                    Fonthand.JE_textShade(Video.VGAScreen, xMenuItemName, y, CStr(sf.name), 13, selected ? 6 : 2, Fonthand.FULL_SHADE);
                    Fonthand.JE_textShade(Video.VGAScreen, xMenuItemLastLevel, y, $"{Helptext.miscTextB[2]} {CStr(sf.levelName)}", 5, selected ? 6 : 2, Fonthand.FULL_SHADE);
                    Fonthand.JE_textShade(Video.VGAScreen, xMenuItemEpisode, y, $"{Helptext.miscTextB[1]} {sf.episode}", 5, selected ? 6 : 2, Fonthand.FULL_SHADE);
                }
            }

            bool leftControlVisible = playersIndex > 0;
            bool rightControlVisible = playersIndex < 1;
            if (leftControlVisible) Sprites.blit_sprite2x2(Video.VGAScreen, xLeftControl, yControls, Sprites.shopSpriteSheet, 279);
            if (rightControlVisible) Sprites.blit_sprite2x2(Video.VGAScreen, xRightControl, yControls, Sprites.shopSpriteSheet, 281);

            Helptext.JE_helpBox(Video.VGAScreen, 103, 182, Helptext.miscText[55], 25, 7, 15, 1, Fonthand.FULL_SHADE);

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

            bool leftAction = false, rightAction = false, action = false, done = false;

            if (Keyboard.mouseGetInput(InputFlags.INPUT_ANY, out MouseInput mi))
            {
                if (leftControlVisible && mi.y >= yControls && mi.x >= xLeftControl && mi.x < xLeftControl + wControl)
                {
                    if (mi.button == SdlKeys.SDL_BUTTON_LEFT) { Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); leftAction = true; }
                }
                else if (rightControlVisible && mi.y >= yControls && mi.x >= xRightControl && mi.x < xRightControl + wControl)
                {
                    if (mi.button == SdlKeys.SDL_BUTTON_LEFT) { Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); rightAction = true; }
                }
                else if (mi.x >= xMenuItem && mi.x < xMenuItem + wMenuItem)
                {
                    for (int i = 0; i < menuItemsCount; ++i)
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
                    case SdlKeys.SDL_SCANCODE_LEFT: Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); leftAction = true; break;
                    case SdlKeys.SDL_SCANCODE_RIGHT: Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); rightAction = true; break;
                    case SdlKeys.SDL_SCANCODE_UP: Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); selectedIndex = selectedIndex == 0 ? menuItemsCount - 1 : selectedIndex - 1; break;
                    case SdlKeys.SDL_SCANCODE_DOWN: Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR); selectedIndex = selectedIndex == menuItemsCount - 1 ? 0 : selectedIndex + 1; break;
                    case SdlKeys.SDL_SCANCODE_SPACE: case SdlKeys.SDL_SCANCODE_RETURN: action = true; break;
                    case SdlKeys.SDL_SCANCODE_ESCAPE: Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING); done = true; break;
                }
            }

            if (leftAction)
                playersIndex = playersIndex == 0 ? 1 : 0;
            else if (rightAction)
                playersIndex = playersIndex == 1 ? 0 : 1;
            else if (action)
            {
                if (selectedIndex == menuItemsCount - 1)
                {
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_SELECT);
                    done = true;
                }
                else
                {
                    int saveFileIndex = playersIndex * 11 + selectedIndex;
                    if (Config.saveFiles[saveFileIndex].level == 0)
                    {
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CLINK);
                    }
                    else
                    {
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_SELECT);
                        performSave = false;
                        JE_operation((byte)(saveFileIndex + 1));
                        Palette.fade_black(15);
                        return Varz.gameLoaded;
                    }
                }
            }

            if (done)
            {
                Palette.fade_black(15);
                return false;
            }
        }
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
            player[p].livesPort = (byte)p; // lives 別名到 weapon[p].power
        }

        Config.mainLevel = Episodes.FIRST_LEVEL;
        Config.saveLevel = Episodes.FIRST_LEVEL;

        StrCpy(Config.lastLevelName, Helptext.miscText[19]);
    }
}
