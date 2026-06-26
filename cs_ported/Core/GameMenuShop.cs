namespace AprCSTyrian.Core;

// 移植 sources/src/game_menu.c 的商店主迴圈與相關函式（JE_itemScreen / JE_menuFunction / 輔助）。
// 忠實逐行翻譯，僅做 C→C# 語法轉換。
internal static unsafe partial class GameMenu
{
    // 便捷別名
    private static SDL_Surface VGAScreen => Video.VGAScreen;
    private static SDL_Surface VGAScreenSeg => Video.VGAScreenSeg;
    private static SDL_Surface VGAScreen2 => Video.VGAScreen2;
    private static byte CS(int sel) => (byte)sel;

    private static string MiscText(int i1based) => Helptext.miscText[i1based - 1];

    private static void CopyScreen(SDL_Surface dst, SDL_Surface src)
    {
        long n = (long)src.pitch * src.h;
        Buffer.MemoryCopy(src.pixels, dst.pixels, n, n);
    }

    /// <summary>對應 game_menu.c:draw_ship_illustration —— 畫左側船艦插圖（船/發電機/前後武器/僚機/護盾）。</summary>
    public static void draw_ship_illustration()
    {
        var player = Players.player;

        // ship
        {
            int sprite_id = (player[0].items.ship < Episodes.ships.Length)
                            ? Episodes.ships[player[0].items.ship].bigshipgraphic - 1
                            : 31;
            int[] ship_x = { 31, 0, 0, 0, 35, 31 };
            int[] ship_y = { 36, 0, 0, 0, 33, 35 };
            int x = ship_x[sprite_id - 27];
            int y = ship_y[sprite_id - 27];
            Sprites.blit_sprite(VGAScreenSeg, x, y, (uint)Sprites.OPTION_SHAPES, (uint)sprite_id);
        }

        // generator
        {
            int sprite_id = (player[0].items.generator == 1)
                            ? player[0].items.generator + 15
                            : player[0].items.generator + 14;
            int[] generator_x = { 62, 64, 67, 66, 63 };
            int[] generator_y = { 84, 85, 86, 84, 97 };
            int x = generator_x[sprite_id - 16];
            int y = generator_y[sprite_id - 16];
            Sprites.blit_sprite(VGAScreenSeg, x, y, (uint)Sprites.WEAPON_SHAPES, (uint)sprite_id);
        }

        int[] weapon_sprites =
        {
            -1,  0,  1,  2,  3,  4,  5,  6,  7,  8,
             9, 10, 11, 21,  5, 13, -1, 14, 15,  0,
            14,  9,  8,  2, 15,  0, 13,  0,  8,  8,
            11,  1,  0,  0,  0,  0,  0,  0,  0,  0,
             0,  2,  1
        };

        // front weapon
        if (player[0].items.weapon[Players.FRONT_WEAPON].id > 0)
        {
            int[] front_weapon_xy_list =
            {
                 -1,  4,  9,  3,  8,  2,  5, 10,  1, -1,
                 -1, -1, -1,  7,  8, -1, -1,  0, -1,  4,
                  0, -1, -1,  3, -1,  4, -1,  4, -1, -1,
                 -1,  9,  0,  0,  0,  0,  0,  0,  0,  0,
                  0,  3,  9
            };
            int[] front_weapon_x = { 59, 66, 66, 54, 61, 51, 58, 51, 61, 52, 53, 58 };
            int[] front_weapon_y = { 38, 53, 41, 36, 48, 35, 41, 35, 53, 41, 39, 31 };
            int id = player[0].items.weapon[Players.FRONT_WEAPON].id;
            int x = front_weapon_x[front_weapon_xy_list[id]];
            int y = front_weapon_y[front_weapon_xy_list[id]];
            Sprites.blit_sprite(VGAScreenSeg, x, y, (uint)Sprites.WEAPON_SHAPES, (uint)weapon_sprites[id]);
        }

        // rear weapon
        if (player[0].items.weapon[Players.REAR_WEAPON].id > 0)
        {
            int[] rear_weapon_xy_list =
            {
                -1, -1, -1, -1, -1, -1, -1, -1, -1,  0,
                 1,  2,  3, -1,  4,  5, -1, -1,  6, -1,
                -1,  1,  0, -1,  6, -1,  5, -1,  0,  0,
                 3,  0,  0,  0,  0,  0,  0,  0,  0,  0,
                 0, -1, -1
            };
            int[] rear_weapon_x = { 41, 27,  49,  43, 51, 39, 41 };
            int[] rear_weapon_y = { 92, 92, 113, 102, 97, 96, 76 };
            int id = player[0].items.weapon[Players.REAR_WEAPON].id;
            int x = rear_weapon_x[rear_weapon_xy_list[id]];
            int y = rear_weapon_y[rear_weapon_xy_list[id]];
            Sprites.blit_sprite(VGAScreenSeg, x, y, (uint)Sprites.WEAPON_SHAPES, (uint)weapon_sprites[id]);
        }

        // sidekicks
        JE_drawItem(6, player[0].items.sidekick[Players.LEFT_SIDEKICK], 3, 84);
        JE_drawItem(7, player[0].items.sidekick[Players.RIGHT_SIDEKICK], 129, 84);

        // shield
        Sprites.blit_sprite_hv(VGAScreenSeg, 28, 23, (uint)Sprites.OPTION_SHAPES, 26, 15, (sbyte)(Episodes.shields[player[0].items.shield].mpwr - 10));
    }

    /// <summary>對應 game_menu.c:load_cubes。</summary>
    public static void load_cubes()
    {
        for (int cube_slot = 0; cube_slot < Config.cubeMax; ++cube_slot)
        {
            for (int i = 0; i < cube[cube_slot].text.Length; i++)
                cube[cube_slot].text[i] = "";
            load_cube(cube_slot, Config.cubeList[cube_slot]);
        }
    }

    private const int cube_line_chars = 31, cube_line_width = 110;

    private static string CubeStr(byte[] b)
    {
        int n = 0; while (n < b.Length && b[n] != 0) n++;
        var c = new char[n]; for (int i = 0; i < n; ++i) c[i] = (char)b[i];
        return new string(c);
    }

    private static int StrPopInt(string s, int start)
    {
        int i = start, val = 0; bool any = false;
        while (i < s.Length && s[i] >= '0' && s[i] <= '9') { val = val * 10 + (s[i] - '0'); i++; any = true; }
        return any ? val : 0;
    }

    /// <summary>對應 game_menu.c:load_cube —— 讀取並自動換行資料方塊文字。</summary>
    public static bool load_cube(int cube_slot, int cube_index)
    {
        Stream f = CFile.dir_fopen_die(CFile.data_dir(), CubeStr(Episodes.cube_file), "rb");

        string buf = "";

        // seek to the cube
        while (cube_index > 0)
        {
            buf = Helptext.ReadEncryptedPascalString(f, 256);
            if (buf.Length > 0 && buf[0] == '*')
                --cube_index;
        }

        // face_sprite 來自 buf[4..] 的數字
        cube[cube_slot].face_sprite = StrPopInt(buf, 4) - 1;

        cube[cube_slot].title = Helptext.ReadEncryptedPascalString(f, 256);
        cube[cube_slot].header = Helptext.ReadEncryptedPascalString(f, 256);

        int line = 0, line_chars = 0, line_width = 0;

        for (; ; )
        {
            string s = Helptext.ReadEncryptedPascalString(f, 256);

            if (s.Length > 0 && s[0] == '*')
                break;

            if (s.Length == 0)
            {
                if (line_chars == 0)
                    line += 4;
                else
                    ++line;
                line_chars = 0;
                line_width = 0;
                continue;
            }

            int word_start = 0;
            for (int i = 0; ; ++i)
            {
                bool end_of_line = (i >= s.Length);
                bool end_of_word = end_of_line || (s[i] == ' ');

                if (end_of_word)
                {
                    string word = s.Substring(word_start, i - word_start);
                    word_start = i + 1;

                    int word_chars = word.Length;
                    int word_width = Fonthand.JE_textWidth(word, (uint)Sprites.TINY_FONT);

                    if (word_chars > cube_line_chars || word_width > cube_line_width)
                        break;

                    bool prepend_space = true;

                    line_chars += word_chars + (prepend_space ? 1 : 0);
                    line_width += word_width + (prepend_space ? 6 : 0);

                    if (line_chars > cube_line_chars || line_width > cube_line_width)
                    {
                        ++line;
                        line_chars = word_chars;
                        line_width = word_width;
                        prepend_space = false;
                    }

                    if (line < cube[cube_slot].text.Length)
                    {
                        if (prepend_space)
                            cube[cube_slot].text[line] += " ";
                        cube[cube_slot].text[line] += word;
                        cube[cube_slot].last_line = line + 1;
                    }
                }

                if (end_of_line)
                    break;
            }
        }

        CFile.fclose(f);
        return true;
    }

    /// <summary>對應 game_menu.c:JE_drawMainMenuHelpText。</summary>
    public static void JE_drawMainMenuHelpText()
    {
        string tempStr;
        int temp = curSel[curMenu] - 2;
        if (curMenu == MENU_JOYSTICK_CONFIG)
        {
            int[] help = { 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 24, 11 };
            tempStr = Helptext.mainMenuHelp[help[curSel[curMenu] - 2]];
        }
        else if (curMenu < MENU_PLAY_NEXT_LEVEL ||
                 curMenu == MENU_2_PLAYER_ARCADE ||
                 curMenu > MENU_1_PLAYER_ARCADE)
        {
            tempStr = Helptext.mainMenuHelp[Helptext.menuHelp[curMenu, temp] - 1];
        }
        else if (curMenu == MENU_KEYBOARD_CONFIG &&
                 curSel[MENU_KEYBOARD_CONFIG] == 10)
        {
            tempStr = Helptext.mainMenuHelp[25 - 1];
        }
        else if (leftPower || rightPower)
        {
            tempStr = Helptext.mainMenuHelp[24 - 1];
        }
        else if (temp == menuChoices[curMenu] - 2 ||
                 (curMenu == MENU_DATA_CUBES && Config.cubeMax == 0))
        {
            tempStr = Helptext.mainMenuHelp[12 - 1];
        }
        else
        {
            tempStr = Helptext.mainMenuHelp[17 + curMenu - 3];
        }

        Fonthand.JE_textShade(VGAScreen, 10, 187, tempStr, 14, 1, Fonthand.DARKEN);
    }

    /// <summary>對應 game_menu.c:JE_quitRequest —— 離開確認對話框。</summary>
    public static bool JE_quitRequest()
    {
        bool quit_selected = true, done = false;

        Vga256d.JE_barShade(VGAScreen, 65, 55, 255, 155);

        while (!done)
        {
            byte qcol = 8;
            int qcolC = 1;

            while (true)
            {
                Nortsong.setFrameCount(4);

                Sprites.blit_sprite(VGAScreen, 50, 50, (uint)Sprites.OPTION_SHAPES, 35);
                Fonthand.JE_textShade(VGAScreen, 70, 66, MiscText(28), 0, 5, Fonthand.FULL_SHADE);
                Helptext.JE_helpBox(VGAScreen, 70, 90, MiscText(30), 30, 7, 12, 1, Fonthand.FULL_SHADE);

                qcol += (byte)qcolC;
                if (qcol > 8 || qcol < 2)
                    qcolC = -qcolC;

                int temp_x, temp_c;

                temp_x = 54 + 45 - (Fonthand.JE_textWidth(MiscText(9), (uint)Sprites.FONT_SHAPES) / 2);
                temp_c = quit_selected ? qcol - 12 : -5;
                Fonthand.JE_outTextAdjust(VGAScreen, temp_x, 128, MiscText(9), 15, temp_c, (uint)Sprites.FONT_SHAPES, true);

                temp_x = 149 + 45 - (Fonthand.JE_textWidth(MiscText(10), (uint)Sprites.FONT_SHAPES) / 2);
                temp_c = !quit_selected ? qcol - 12 : -5;
                Fonthand.JE_outTextAdjust(VGAScreen, temp_x, 128, MiscText(10), 15, temp_c, (uint)Sprites.FONT_SHAPES, true);

                Mouse.JE_mouseStart();
                Video.JE_showVGA();
                Mouse.JE_mouseReplace();

                Keyboard.waitUntilElapsed();

                if (Keyboard.hasInput(InputFlags.INPUT_NO_MOTION))
                    break;
            }

            if (Keyboard.mouseGetInput(InputFlags.INPUT_NO_MOTION, out MouseInput mouseInput))
            {
                if (mouseInput.y > 123 && mouseInput.y < 149)
                {
                    if (mouseInput.x > 56 && mouseInput.x < 142)
                    {
                        quit_selected = true;
                        done = true;
                    }
                    else if (mouseInput.x > 151 && mouseInput.x < 237)
                    {
                        quit_selected = false;
                        done = true;
                    }
                }
            }
            else if (Keyboard.keyboardGetInput(out KeyboardInput keyboardInput))
            {
                switch (keyboardInput.scancode)
                {
                    case SdlKeys.SDL_SCANCODE_LEFT:
                    case SdlKeys.SDL_SCANCODE_RIGHT:
                    case SdlKeys.SDL_SCANCODE_TAB:
                        quit_selected = !quit_selected;
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                        break;
                    case SdlKeys.SDL_SCANCODE_RETURN:
                    case SdlKeys.SDL_SCANCODE_SPACE:
                        done = true;
                        break;
                    case SdlKeys.SDL_SCANCODE_ESCAPE:
                        quit_selected = false;
                        done = true;
                        break;
                    default:
                        break;
                }
            }
        }

        Nortsong.JE_playSampleNum(quit_selected ? (byte)Sndmast.S_SPRING : (byte)Sndmast.S_CLICK);
        return quit_selected;
    }

    /// <summary>對應 game_menu.c:JE_menuFunction —— 選單項目被選取時的動作分派。</summary>
    public static void JE_menuFunction(int select)
    {
        var player = Players.player;
        int curSelect;

        col = 0;
        colC = -1;
        Nortsong.JE_playSampleNum((byte)Sndmast.S_CLICK);

        curSelect = curSel[curMenu];

        switch (curMenu)
        {
        case MENU_FULL_GAME:
            switch (select)
            {
            case 2: // cubes
                curMenu = MENU_DATA_CUBES;
                curSel[MENU_DATA_CUBES] = 2;
                break;
            case 3: // shipspecs
                JE_doShipSpecs();
                break;
            case 4: // upgradeship
                curMenu = MENU_UPGRADES;
                break;
            case 5: // options
                curMenu = MENU_OPTIONS;
                break;
            case 6: // nextlevel
                curMenu = MENU_PLAY_NEXT_LEVEL;
                newPal = 18;
                JE_computeDots();
                navX = planetX[Tyrian2.mapOrigin - 1];
                navY = planetY[Tyrian2.mapOrigin - 1];
                newNavX = navX;
                newNavY = navY;
                menuChoices[MENU_PLAY_NEXT_LEVEL] = (byte)(Tyrian2.mapPNum + 2);
                curSel[MENU_PLAY_NEXT_LEVEL] = 2;
                Helptext.menuInt[4][0] = "Next Level";
                {
                    int x;
                    for (x = 0; x < Tyrian2.mapPNum; x++)
                    {
                        int t = Tyrian2.mapPlanet[x];
                        Helptext.menuInt[4][x + 1] = Helptext.pName[t - 1];
                    }
                    Helptext.menuInt[4][x + 1] = MiscText(5);
                }
                break;
            case 7: // quit
                if (JE_quitRequest())
                {
                    Varz.gameLoaded = true;
                    Config.mainLevel = 0;
                }
                break;
            }
            break;

        case MENU_UPGRADES:
            if (select == 9) // done
            {
                curMenu = MENU_FULL_GAME;
            }
            else // selected item to upgrade
            {
                old_items[0] = player[0].items;

                lastDirection = 1;
                JE_genItemMenu(CS(select));
                JE_initWeaponView();
                curMenu = MENU_UPGRADE_SUB;
                lastCurSel = curSel[MENU_UPGRADE_SUB];
                player[0].cash = (uint)(player[0].cash * 2 - JE_cashLeft());
            }
            break;

        case MENU_OPTIONS:
            switch (select)
            {
            case 2:
                curMenu = MENU_LOAD_SAVE;
                Mainint.performSave = false;
                quikSave = false;
                break;
            case 3:
                curMenu = MENU_LOAD_SAVE;
                Mainint.performSave = true;
                quikSave = false;
                break;
            case 6:
                curMenu = MENU_JOYSTICK_CONFIG;
                break;
            case 7:
                curMenu = MENU_KEYBOARD_CONFIG;
                break;
            case 8:
                curMenu = MENU_FULL_GAME;
                break;
            }
            break;

        case MENU_PLAY_NEXT_LEVEL:
            if (select == menuChoices[MENU_PLAY_NEXT_LEVEL]) // exit
            {
                curMenu = MENU_FULL_GAME;
                newPal = 1;
            }
            else
            {
                Config.mainLevel = Tyrian2.mapSection[curSelect - 2];
                Tyrian2.jumpSection = true;
            }
            break;

        case MENU_UPGRADE_SUB:
            if (curSel[MENU_UPGRADE_SUB] < menuChoices[MENU_UPGRADE_SUB])
            {
                curSel[MENU_UPGRADE_SUB] = menuChoices[MENU_UPGRADE_SUB];
            }
            else // if done is selected
            {
                Nortsong.JE_playSampleNum((byte)Sndmast.S_ITEM);
                player[0].cash = (uint)JE_cashLeft();
                curMenu = MENU_UPGRADES;
            }
            break;

        case MENU_KEYBOARD_CONFIG:
            if (curSelect == 10) // reset to defaults
            {
                Array.Copy(Config.defaultKeySettings, Config.keySettings, Config.keySettings.Length);
            }
            else if (curSelect == 11) // done
            {
                curMenu = Config.isNetworkGame ? MENU_LIMITED_OPTIONS : MENU_OPTIONS;
            }
            else // change key
            {
                int temp2v = 254;
                int tempY = 38 + (curSelect - 2) * 12;
                Fonthand.JE_textShade(VGAScreen, 236, tempY, SdlKeys.SDL_GetScancodeName(Config.keySettings[curSelect - 2]), (uint)(temp2v / 16), temp2v % 16 - 8, Fonthand.DARKEN);
                Video.JE_showVGA();

                col = 248;
                colC = 1;

                bool joyHeld = Joystick.joydown;
                while (true)
                {
                    joyHeld &= Joystick.joydown;
                    Nortsong.setFrameCount(1);

                    col += colC;
                    if (col < 243 || col > 248)
                        colC *= -1;
                    Vga256d.JE_rectangle(VGAScreen, 230, tempY - 2, 300, tempY + 7, (byte)col);

                    Video.JE_showVGA();
                    Nortsong.delayUntilElapsed();

                    Joystick.poll_joysticks();
                    Keyboard.handleSdlEvents();

                    if (Keyboard.hasInput(InputFlags.INPUT_NO_MOTION) || (Joystick.joydown && !joyHeld))
                        break;
                }

                if (Keyboard.mouseGetInput(InputFlags.INPUT_NO_MOTION, out _))
                {
                    // Cancel.
                }
                else if (Keyboard.keyboardGetInput(out KeyboardInput keyboardInput))
                {
                    // already used? then swap
                    for (int i = 0; i < Config.keySettings.Length; ++i)
                    {
                        if (Config.keySettings[i] == keyboardInput.scancode)
                        {
                            Config.keySettings[i] = Config.keySettings[curSelect - 2];
                            break;
                        }
                    }

                    if (keyboardInput.scancode != SdlKeys.SDL_SCANCODE_ESCAPE &&
                        keyboardInput.scancode != SdlKeys.SDL_SCANCODE_F11 &&
                        keyboardInput.scancode != SdlKeys.SDL_SCANCODE_P)
                    {
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CLICK);
                        Config.keySettings[curSelect - 2] = keyboardInput.scancode;
                        ++curSelect;
                    }
                }
            }
            break;

        case MENU_LOAD_SAVE:
            if (curSelect == 13)
            {
                if (quikSave)
                {
                    curMenu = oldMenu;
                    newPal = oldPal;
                }
                else
                {
                    curMenu = MENU_OPTIONS;
                }
            }
            else
            {
                int temp = Config.twoPlayerMode ? 11 : 0;
                Mainint.JE_operation((byte)(curSelect - 1 + temp));
                if (quikSave)
                {
                    curMenu = oldMenu;
                    newPal = oldPal;
                }
            }
            break;

        case MENU_DATA_CUBES:
            if (curSelect == menuChoices[curMenu])
            {
                curMenu = MENU_FULL_GAME;
                newPal = 1;
            }
            else
            {
                if (Config.cubeMax > 0)
                {
                    firstMenu9 = true;
                    curMenu = MENU_DATA_CUBE_SUB;
                    yLoc = 0;
                    yChg = 0;
                    currentCube = curSel[MENU_DATA_CUBES] - 2;
                }
                else
                {
                    curMenu = MENU_FULL_GAME;
                    newPal = 1;
                }
            }
            break;

        case MENU_DATA_CUBE_SUB:
            curMenu = MENU_DATA_CUBES;
            break;

        case MENU_2_PLAYER_ARCADE:
            switch (curSel[curMenu])
            {
            case 2:
                Config.mainLevel = Tyrian2.mapSection[Tyrian2.mapPNum - 1];
                Tyrian2.jumpSection = true;
                break;
            case 3:
            case 4:
            {
                Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                int temp = curSel[curMenu] - 3;
                do
                {
                    if (Joystick.joysticks == 0)
                        Config.inputDevice[temp == 0 ? 1 : 0] = Config.inputDevice[temp];
                    if (Config.inputDevice[temp] >= 2 + Joystick.joysticks)
                        Config.inputDevice[temp] = 1;
                    else
                        Config.inputDevice[temp]++;
                } while (Config.inputDevice[temp] == Config.inputDevice[temp == 0 ? 1 : 0]);
                break;
            }
            case 5:
                curMenu = MENU_OPTIONS;
                break;
            case 6:
                if (JE_quitRequest())
                {
                    Varz.gameLoaded = true;
                    Config.mainLevel = 0;
                }
                break;
            }
            break;

        case MENU_1_PLAYER_ARCADE:
            switch (curSel[curMenu])
            {
            case 2:
                Config.mainLevel = Tyrian2.mapSection[Tyrian2.mapPNum - 1];
                Tyrian2.jumpSection = true;
                break;
            case 3:
                curMenu = Config.isNetworkGame ? MENU_LIMITED_OPTIONS : MENU_OPTIONS;
                break;
            case 4:
                if (JE_quitRequest())
                {
                    Varz.gameLoaded = true;
                    Config.mainLevel = 0;
                }
                break;
            }
            break;

        case MENU_LIMITED_OPTIONS:
            switch (select)
            {
            case 2:
                curMenu = MENU_JOYSTICK_CONFIG;
                break;
            case 3:
                curMenu = MENU_KEYBOARD_CONFIG;
                break;
            case 6:
                curMenu = MENU_1_PLAYER_ARCADE;
                break;
            }
            break;

        case MENU_JOYSTICK_CONFIG:
            if (Joystick.joysticks == 0 && select != 17)
                break;

            switch (select)
            {
            case 2:
                Joystick.joystick_config++;
                Joystick.joystick_config %= Joystick.joysticks;
                break;
            case 3:
                Joystick.joystick[Joystick.joystick_config].analog = !Joystick.joystick[Joystick.joystick_config].analog;
                break;
            case 4:
                if (Joystick.joystick[Joystick.joystick_config].analog)
                {
                    Joystick.joystick[Joystick.joystick_config].sensitivity++;
                    Joystick.joystick[Joystick.joystick_config].sensitivity %= 11;
                }
                break;
            case 5:
                if (Joystick.joystick[Joystick.joystick_config].analog)
                {
                    Joystick.joystick[Joystick.joystick_config].threshold++;
                    Joystick.joystick[Joystick.joystick_config].threshold %= 11;
                }
                break;
            case 16:
                Joystick.reset_joystick_assignments(Joystick.joystick_config);
                break;
            case 17:
                curMenu = Config.isNetworkGame ? MENU_LIMITED_OPTIONS : MENU_OPTIONS;
                break;
            default:
                if (Joystick.joysticks == 0)
                    break;

                Vga256d.JE_rectangle(VGAScreen, 235, 21 + select * 8, 310, 30 + select * 8, 248);

                if (Joystick.detect_joystick_assignment(Joystick.joystick_config, out Joystick_assignment tempA))
                {
                    bool assigned = false;
                    var asg = Joystick.joystick[Joystick.joystick_config].assignment[select - 6];
                    for (int i = 0; i < asg.Length; i++)
                    {
                        if (Joystick.joystick_assignment_cmp(ref tempA, ref asg[i]))
                        {
                            asg[i].type = Joystick_assignment_types.NONE;
                            assigned = true;
                            break;
                        }
                    }
                    if (!assigned)
                    {
                        for (int i = 0; i < asg.Length; i++)
                        {
                            if (asg[i].type == Joystick_assignment_types.NONE)
                            {
                                asg[i] = tempA;
                                assigned = true;
                                break;
                            }
                        }
                    }
                    if (!assigned)
                    {
                        for (int i = 0; i < asg.Length; i++)
                        {
                            if (i == asg.Length - 1)
                                asg[i] = tempA;
                            else
                                asg[i] = asg[i + 1];
                        }
                    }
                    curSelect++;
                    Joystick.poll_joysticks();
                }
                break;
            }
            break;

        case MENU_SUPER_TYRIAN:
            switch (curSel[curMenu])
            {
            case 2:
                Config.mainLevel = Tyrian2.mapSection[Tyrian2.mapPNum - 1];
                Tyrian2.jumpSection = true;
                break;
            case 3:
                JE_doShipSpecs();
                break;
            case 4:
                curMenu = MENU_OPTIONS;
                break;
            case 5:
                if (JE_quitRequest())
                {
                    Varz.gameLoaded = true;
                    Config.mainLevel = 0;
                }
                break;
            }
            break;
        }

        old_items[0] = player[0].items;
    }

    /// <summary>對應 game_menu.c:JE_itemScreen —— 商店/裝備/選單主迴圈。</summary>
    public static void JE_itemScreen()
    {
        var player = Players.player;
        bool quit = false;
        int temp, temp2, temp3, tempW;
        string tempStr;

        if (Sprites.shopSpriteSheet.data == null)
            Sprites.JE_loadCompShapes(ref Sprites.shopSpriteSheet, '1');

        load_cubes();

        Video.VGAScreen = Video.VGAScreenSeg;

        Array.Copy(menuChoicesDefault, menuChoices, menuChoices.Length);

        Loudness.play_song(Musmast.songBuy);

        Picload.JE_loadPic(Video.VGAScreen, 1, false);

        curPal = 1;
        newPal = 0;

        Video.JE_showVGA();

        Palette.set_palette(Palette.colors, 0, 255);

        col = 1;
        Varz.gameLoaded = false;
        curItemType = 1;
        cursor = 1;
        curItem = 0;

        for (int i = 0; i < curSel.Length; ++i)
            curSel[i] = 2;

        curMenu = MENU_FULL_GAME;

        int[] temp_weapon_power = new int[7];

        /* Check for where Pitems and Select match up - if no match then add to the itemavail list */
        for (int i = 0; i < 7; i++)
        {
            int item = playeritem_map(ref player[0].last_items, i);
            int slot = 0;
            for (; slot < Tyrian2.itemAvailMax[itemAvailMap[i] - 1]; ++slot)
            {
                if (Tyrian2.itemAvail[itemAvailMap[i] - 1, slot] == item)
                    break;
            }
            if (slot == Tyrian2.itemAvailMax[itemAvailMap[i] - 1])
            {
                Tyrian2.itemAvail[itemAvailMap[i] - 1, slot] = (byte)item;
                Tyrian2.itemAvailMax[itemAvailMap[i] - 1]++;
            }
        }

        CopyScreen(Video.VGAScreen2, Video.VGAScreen);

        firstMenu9 = false;
        backFromHelp = false;

        /* Sort items in merchant inventory */
        for (int x = 0; x < 9; x++)
        {
            if (Tyrian2.itemAvailMax[x] > 1)
            {
                for (temp = 0; temp < Tyrian2.itemAvailMax[x] - 1; temp++)
                {
                    for (temp2 = temp; temp2 < Tyrian2.itemAvailMax[x]; temp2++)
                    {
                        if (Tyrian2.itemAvail[x, temp] == 0 || (Tyrian2.itemAvail[x, temp] > Tyrian2.itemAvail[x, temp2] && Tyrian2.itemAvail[x, temp2] != 0))
                        {
                            temp3 = Tyrian2.itemAvail[x, temp];
                            Tyrian2.itemAvail[x, temp] = Tyrian2.itemAvail[x, temp2];
                            Tyrian2.itemAvail[x, temp2] = (byte)temp3;
                        }
                    }
                }
            }
        }

        byte mouseButtonsHeld = 0;

        do
        {
            quit = false;

            Varz.JE_getShipInfo();

            if (curMenu == MENU_FULL_GAME)
            {
                if (Config.twoPlayerMode)
                    curMenu = MENU_2_PLAYER_ARCADE;
                if (Config.isNetworkGame || Config.onePlayerAction)
                    curMenu = MENU_1_PLAYER_ARCADE;
                if (Config.superTyrian)
                    curMenu = MENU_SUPER_TYRIAN;
            }

            paletteChanged = false;
            leftPower = false;
            rightPower = false;

            if (firstMenu9)
                mouseButtonsHeld = Keyboard.mouseButtonsDown;

            if (curMenu != MENU_DATA_CUBE_SUB || firstMenu9)
            {
                CopyScreen(Video.VGAScreen, Video.VGAScreen2);
            }

            if (curMenu == MENU_UPGRADES &&
                (curSel[curMenu] == 3 || curSel[curMenu] == 4))
            {
                int item = player[0].items.weapon[curSel[MENU_UPGRADES] - 3].id;
                int item_power = player[0].items.weapon[curSel[MENU_UPGRADES] - 3].power;
                int ii = curSel[MENU_UPGRADES] - 2;

                for (int slot = 0; slot < Tyrian2.itemAvailMax[itemAvailMap[ii] - 1]; ++slot)
                {
                    if (Tyrian2.itemAvail[itemAvailMap[ii] - 1, slot] == item)
                        temp_weapon_power[slot] = item_power;
                    else
                        temp_weapon_power[slot] = 1;
                }
                temp_weapon_power[Tyrian2.itemAvailMax[itemAvailMap[ii] - 1]] = item_power;
            }

            if (curMenu == MENU_PLAY_NEXT_LEVEL)
            {
                planetAni = 0;
                currentDotNum = 0;
                currentDotWait = 8;
                planetAniWait = 3;
                JE_updateNavScreen();
            }

            if (curMenu != MENU_UPGRADE_SUB)
                JE_drawMenuHeader();

            if ((curMenu >= MENU_FULL_GAME && curMenu <= MENU_PLAY_NEXT_LEVEL) ||
                (curMenu >= MENU_2_PLAYER_ARCADE && curMenu <= MENU_LIMITED_OPTIONS) ||
                curMenu == MENU_SUPER_TYRIAN)
            {
                JE_drawMenuChoices();
            }

            /* Data cube icons */
            if (curMenu == MENU_FULL_GAME)
            {
                for (int i = 1; i <= Config.cubeMax; i++)
                {
                    Sprites.blit_sprite_dark(Video.VGAScreen, 190 + i * 18 + 2, 37 + 1, (uint)Sprites.OPTION_SHAPES, 34, false);
                    Sprites.blit_sprite(Video.VGAScreen, 190 + i * 18, 37, (uint)Sprites.OPTION_SHAPES, 34);
                }
            }

            /* load/save menu */
            if (curMenu == MENU_LOAD_SAVE)
            {
                int min, max;
                if (Config.twoPlayerMode) { min = 13; max = 24; }
                else { min = 2; max = 13; }

                for (int x = min; x <= max; x++)
                {
                    temp2 = (x - min + 2 == curSel[curMenu]) ? 15 : 28;

                    if (x == max)
                        tempStr = MiscText(6);
                    else if (Config.saveFiles[x - 2].level == 0)
                        tempStr = MiscText(3);
                    else
                        tempStr = CubeStr(Config.saveFiles[x - 2].name);

                    int tempY = 38 + (x - min) * 11;
                    Fonthand.JE_textShade(Video.VGAScreen, 163, tempY, tempStr, (uint)(temp2 / 16), temp2 % 16 - 8, Fonthand.DARKEN);

                    if (x < max)
                    {
                        temp2 = (x - min + 2 == curSel[curMenu]) ? 252 : 250;
                        if (Config.saveFiles[x - 2].level == 0)
                        {
                            tempStr = "-----";
                        }
                        else
                        {
                            tempStr = CubeStr(Config.saveFiles[x - 2].levelName);
                            string buf = Helptext.miscTextB[1 - 1] + Config.saveFiles[x - 2].episode;
                            Fonthand.JE_textShade(Video.VGAScreen, 297, tempY, buf, (uint)(temp2 / 16), temp2 % 16 - 8, Fonthand.DARKEN);
                        }
                        Fonthand.JE_textShade(Video.VGAScreen, 245, tempY, tempStr, (uint)(temp2 / 16), temp2 % 16 - 8, Fonthand.DARKEN);
                    }

                    JE_drawMenuHeader();
                }
            }

            if (curMenu == MENU_KEYBOARD_CONFIG)
            {
                for (int x = 2; x <= 11; x++)
                {
                    temp2 = (x == curSel[curMenu]) ? 15 : 28;
                    Fonthand.JE_textShade(Video.VGAScreen, 166, 38 + (x - 2) * 12, Helptext.menuInt[curMenu + 1][x - 1], (uint)(temp2 / 16), temp2 % 16 - 8, Fonthand.DARKEN);
                    if (x < 10)
                    {
                        temp2 = (x == curSel[curMenu]) ? 252 : 250;
                        Fonthand.JE_textShade(Video.VGAScreen, 236, 38 + (x - 2) * 12, SdlKeys.SDL_GetScancodeName(Config.keySettings[x - 2]), (uint)(temp2 / 16), temp2 % 16 - 8, Fonthand.DARKEN);
                    }
                }
                menuChoices[MENU_KEYBOARD_CONFIG] = 11;
            }

            if (curMenu == MENU_UPGRADE_SUB)
            {
                while (curSel[MENU_UPGRADE_SUB] < menuChoices[MENU_UPGRADE_SUB] &&
                       Mainint.JE_getCost((byte)curSel[MENU_UPGRADES], Tyrian2.itemAvail[itemAvailMap[curSel[MENU_UPGRADES] - 2] - 1, curSel[MENU_UPGRADE_SUB] - 2]) > player[0].cash)
                {
                    curSel[MENU_UPGRADE_SUB] = (byte)(curSel[MENU_UPGRADE_SUB] + lastDirection);
                    if (curSel[MENU_UPGRADE_SUB] < 2)
                        curSel[MENU_UPGRADE_SUB] = menuChoices[MENU_UPGRADE_SUB];
                    else if (curSel[MENU_UPGRADE_SUB] > menuChoices[MENU_UPGRADE_SUB])
                        curSel[MENU_UPGRADE_SUB] = 2;
                }

                if (curSel[MENU_UPGRADE_SUB] == menuChoices[MENU_UPGRADE_SUB])
                    playeritem_map(ref player[0].items, curSel[MENU_UPGRADES] - 2) = playeritem_map(ref old_items[0], curSel[MENU_UPGRADES] - 2);
                else
                    playeritem_map(ref player[0].items, curSel[MENU_UPGRADES] - 2) = Tyrian2.itemAvail[itemAvailMap[curSel[MENU_UPGRADES] - 2] - 1, curSel[MENU_UPGRADE_SUB] - 2];

                if ((curSel[MENU_UPGRADES] == 3 || curSel[MENU_UPGRADES] == 4) &&
                    curSel[MENU_UPGRADE_SUB] < menuChoices[MENU_UPGRADE_SUB] &&
                    Tyrian2.itemAvail[itemAvailMap[curSel[MENU_UPGRADES] - 2] - 1, curSel[MENU_UPGRADE_SUB] - 2] != 0)
                {
                    int port = curSel[MENU_UPGRADES] - 3;
                    int item_level = player[0].items.weapon[port].power;
                    Mainint.JE_getCost((byte)curSel[MENU_UPGRADES], Tyrian2.itemAvail[itemAvailMap[curSel[MENU_UPGRADES] - 2] - 1, curSel[MENU_UPGRADE_SUB] - 2]);
                    leftPower = item_level > 1;
                    rightPower = item_level < 11;
                    if (rightPower)
                        rightPowerAfford = JE_cashLeft() >= Mainint.upgradeCost;
                }
                else
                {
                    leftPower = false;
                    rightPower = false;
                }

                Fonthand.JE_dString(Video.VGAScreen, 74 + Fonthand.JE_fontCenter(Helptext.menuInt[2][curSel[MENU_UPGRADES] - 1], (uint)Sprites.FONT_SHAPES), 10, Helptext.menuInt[2][curSel[MENU_UPGRADES] - 1], (uint)Sprites.FONT_SHAPES);

                for (tempW = 1; tempW < menuChoices[curMenu]; tempW++)
                {
                    int tempY = 40 + (tempW - 1) * 26;
                    uint temp_cost;

                    if (tempW < menuChoices[MENU_UPGRADE_SUB] - 1)
                        temp_cost = (uint)Mainint.JE_getCost((byte)curSel[MENU_UPGRADES], Tyrian2.itemAvail[itemAvailMap[curSel[MENU_UPGRADES] - 2] - 1, tempW - 1]);
                    else
                        temp_cost = 0;

                    int afford_shade = (temp_cost > player[0].cash) ? 4 : 0;

                    temp = Tyrian2.itemAvail[itemAvailMap[curSel[MENU_UPGRADES] - 2] - 1, tempW - 1];
                    switch (curSel[MENU_UPGRADES] - 1)
                    {
                        case 1:
                            if (temp > 90) tempStr = $"Custom Ship {temp - 90}";
                            else tempStr = ShipName(temp);
                            break;
                        case 2:
                        case 3:
                            tempStr = WeaponName(temp);
                            break;
                        case 4:
                            tempStr = ShieldName(temp);
                            break;
                        case 5:
                            tempStr = PowerName(temp);
                            break;
                        case 6:
                        case 7:
                            tempStr = OptionName(temp);
                            break;
                        default:
                            tempStr = "";
                            break;
                    }
                    temp2 = (tempW == curSel[curMenu] - 1) ? 15 : 28;

                    Varz.JE_getShipInfo();

                    if (temp == playeritem_map(ref old_items[0], curSel[MENU_UPGRADES] - 2) && temp != 0 && tempW != menuChoices[curMenu] - 1)
                    {
                        Vga256d.fill_rectangle_xy(Video.VGAScreen, 160, tempY + 7, 300, tempY + 11, 227);
                        Sprites.blit_sprite2(Video.VGAScreen, 298, tempY + 2, Sprites.shopSpriteSheet, 247);
                    }

                    if (tempW == menuChoices[curMenu] - 1)
                        tempStr = MiscText(13);
                    Fonthand.JE_textShade(Video.VGAScreen, 185, tempY, tempStr, (uint)(temp2 / 16), temp2 % 16 - 8 - afford_shade, Fonthand.DARKEN);

                    if (tempW < menuChoices[curMenu] - 1)
                        JE_drawItem((byte)(curSel[MENU_UPGRADES] - 1), (ushort)temp, 160, tempY - 4);

                    temp2 = (tempW == curSel[curMenu] - 1) ? 15 : 28;

                    if (tempW < menuChoices[MENU_UPGRADE_SUB] - 1)
                    {
                        string buf = $"{temp_cost}";
                        Fonthand.JE_textShade(Video.VGAScreen, 187, tempY + 10, buf, (uint)(temp2 / 16), temp2 % 16 - 8 - afford_shade, Fonthand.DARKEN);
                    }
                }
            }

            /* Draw crap on the left side: ship illustration etc. */
            if ((curMenu >= MENU_FULL_GAME && curMenu <= MENU_OPTIONS) ||
                curMenu == MENU_KEYBOARD_CONFIG ||
                curMenu == MENU_LOAD_SAVE ||
                curMenu >= MENU_2_PLAYER_ARCADE ||
                (curMenu == MENU_UPGRADE_SUB &&
                 (curSel[MENU_UPGRADES] == 2 || curSel[MENU_UPGRADES] == 5)))
            {
                if (Config.twoPlayerMode)
                {
                    for (int i = 0; i < 2; ++i)
                    {
                        string buf = $"{Helptext.miscText[40 + i]} {player[i].cash}";
                        Fonthand.JE_textShade(Video.VGAScreen, 25, 50 + 10 * i, buf, 15, 0, Fonthand.FULL_SHADE);
                    }
                }
                else if (Config.superArcadeMode != VarzConst.SA_NONE || Config.superTyrian)
                {
                    if (!Config.superTyrian)
                        Helptext.JE_helpBox(Video.VGAScreen, 35, 25, Helptext.superShips[Config.superArcadeMode], 18, 7, 15, 4, Fonthand.FULL_SHADE);
                    else
                        Helptext.JE_helpBox(Video.VGAScreen, 35, 25, Helptext.superShips[VarzConst.SA + 3], 18, 7, 15, 4, Fonthand.FULL_SHADE);

                    Fonthand.JE_textShade(Video.VGAScreen, 25, 50, Helptext.superShips[VarzConst.SA + 1], 15, 0, Fonthand.FULL_SHADE);
                    Helptext.JE_helpBox(Video.VGAScreen, 25, 60, WeaponName(player[0].items.weapon[Players.FRONT_WEAPON].id), 22, 7, 12, 1, Fonthand.FULL_SHADE);
                    Fonthand.JE_textShade(Video.VGAScreen, 25, 120, Helptext.superShips[VarzConst.SA + 2], 15, 0, Fonthand.FULL_SHADE);
                    Helptext.JE_helpBox(Video.VGAScreen, 25, 130, SpecialName(player[0].items.special), 22, 7, 12, 1, Fonthand.FULL_SHADE);
                }
                else
                {
                    draw_ship_illustration();
                }
            }

            /* Changing the volume? */
            if (curMenu == MENU_OPTIONS || curMenu == MENU_LIMITED_OPTIONS)
            {
                Nortvars.JE_barDrawShadow(Video.VGAScreen, 225, 70, 1, Loudness.music_disabled ? 12 : 16, Nortsong.tyrMusicVolume / 12, 3, 13);
                Nortvars.JE_barDrawShadow(Video.VGAScreen, 225, 86, 1, Loudness.samples_disabled ? 12 : 16, Nortsong.fxVolume / 12, 3, 13);
            }

            /* data cubes */
            if (curMenu == MENU_DATA_CUBES ||
                (curMenu == MENU_DATA_CUBE_SUB && (firstMenu9 || backFromHelp)))
            {
                firstMenu9 = false;
                menuChoices[MENU_DATA_CUBES] = (byte)(Config.cubeMax + 2);
                Vga256d.fill_rectangle_xy(Video.VGAScreen, 1, 1, 145, 170, 0);
                Sprites.blit_sprite(Video.VGAScreenSeg, 1, 1, (uint)Sprites.OPTION_SHAPES, 20);

                if (curMenu == MENU_DATA_CUBES)
                {
                    if (Config.cubeMax == 0)
                    {
                        Helptext.JE_helpBox(Video.VGAScreen, 166, 80, MiscText(16), 30, 7, 12, 1, Fonthand.FULL_SHADE);
                        tempW = 160;
                        temp2 = 252;
                    }
                    else
                    {
                        for (int x = 1; x <= Config.cubeMax; x++)
                        {
                            Mainint.JE_drawCube(Video.VGAScreenSeg, 166, 38 + (x - 1) * 28, 13, 0);
                            temp2 = (x + 1 == curSel[curMenu]) ? 252 : 250;
                            Helptext.JE_helpBox(Video.VGAScreen, 192, 44 + (x - 1) * 28, cube[x - 1].title, 24, 7, (byte)(temp2 / 16), (byte)((temp2 % 16) - 8), Fonthand.DARKEN);
                        }
                        int x2 = Config.cubeMax + 1;
                        temp2 = (x2 + 1 == curSel[curMenu]) ? 252 : 250;
                        tempW = 44 + (x2 - 1) * 28;
                    }
                    Fonthand.JE_textShade(Video.VGAScreen, 172, tempW, MiscText(6), (uint)(temp2 / 16), (temp2 % 16) - 8, Fonthand.DARKEN);
                }

                if (curSel[MENU_DATA_CUBES] < menuChoices[MENU_DATA_CUBES])
                {
                    int face_sprite = cube[curSel[MENU_DATA_CUBES] - 2].face_sprite;
                    if (face_sprite != -1)
                    {
                        int face_x = 77 - (Sprites.sprite((uint)Sprites.FACE_SHAPES, (uint)face_sprite).width / 2);
                        int face_y = 92 - (Sprites.sprite((uint)Sprites.FACE_SHAPES, (uint)face_sprite).height / 2);
                        Sprites.blit_sprite(Video.VGAScreenSeg, face_x, face_y, (uint)Sprites.FACE_SHAPES, (uint)face_sprite);

                        paletteChanged = true;
                        temp2 = Pcxmast.facepal[face_sprite];
                        newPal = 0;
                        for (temp = 1; temp <= 255 - (3 * 16); temp++)
                            Palette.colors[temp] = Palette.palettes[temp2][temp];
                    }
                }
            }

            /* 2 player input devices */
            if (curMenu == MENU_2_PLAYER_ARCADE)
            {
                for (int i = 0; i < Config.inputDevice.Length; i++)
                {
                    if (Config.inputDevice[i] > 2 + Joystick.joysticks)
                        Config.inputDevice[i] = (byte)(Config.inputDevice[i == 0 ? 1 : 0] == 1 ? 2 : 1);

                    string t;
                    if (Joystick.joysticks > 1 && Config.inputDevice[i] > 2)
                        t = $"{Helptext.inputDevices[2]} {Config.inputDevice[i] - 2}";
                    else
                        t = Helptext.inputDevices[Config.inputDevice[i] - 1];
                    Fonthand.JE_dString(Video.VGAScreen, 186, 38 + 2 * (i + 1) * 16, t, (uint)Sprites.SMALL_FONT_SHAPES);
                }
            }

            flash = false;

            Array.Clear(Config.shotMultiPos, 0, Config.shotMultiPos.Length);

            JE_drawScore();
            JE_drawMainMenuHelpText();

            if (newPal > 0)
            {
                curPal = newPal;
                Array.Copy(Palette.palettes[newPal - 1], Palette.colors, Palette.colors.Length);
                Palette.set_palette(Palette.palettes[newPal - 1], 0, 255);
                newPal = 0;
            }

            if ((curMenu == MENU_DATA_CUBES || curMenu == MENU_DATA_CUBE_SUB) &&
                curSel[MENU_DATA_CUBES] < menuChoices[MENU_DATA_CUBES])
            {
                Fonthand.JE_textShade(Video.VGAScreen, 75 - Fonthand.JE_textWidth(cube[curSel[MENU_DATA_CUBES] - 2].header, (uint)Sprites.TINY_FONT) / 2, 173, cube[curSel[MENU_DATA_CUBES] - 2].header, 14, 3, Fonthand.DARKEN);
            }

            if (Params.constantPlay)
            {
                Config.mainLevel = Tyrian2.mapSection[Tyrian2.mapPNum - 1];
                Tyrian2.jumpSection = true;
            }
            else
            {
                while (true)
                {
                    Mouse.mouseCursor = Mouse.MOUSE_POINTER_NORMAL;

                    col += colC;
                    if (col < -2 || col > 6)
                        colC = -colC;

                    // data cube reading
                    if (curMenu == MENU_DATA_CUBE_SUB)
                    {
                        if (Keyboard.mouseX > 164 && Keyboard.mouseX < 299 && Keyboard.mouseY > 47 && Keyboard.mouseY < 153)
                        {
                            if (Keyboard.mouseY > 100)
                                Mouse.mouseCursor = Mouse.MOUSE_POINTER_DOWN;
                            else
                                Mouse.mouseCursor = Mouse.MOUSE_POINTER_UP;
                        }

                        Vga256d.fill_rectangle_xy(Video.VGAScreen, 160, 49, 310, 158, 228);
                        if (yLoc + yChg < 0)
                        {
                            yChg = 0;
                            yLoc = 0;
                        }

                        yLoc += yChg;
                        temp = yLoc / 12;
                        temp2 = yLoc % 12;
                        tempW = 38 + 12 - temp2;
                        temp3 = cube[curSel[MENU_DATA_CUBES] - 2].last_line;

                        for (int x = temp + 1; x <= temp + 10; x++)
                        {
                            if (x <= temp3)
                            {
                                Fonthand.JE_outTextAndDarken(Video.VGAScreen, 161, tempW, cube[curSel[MENU_DATA_CUBES] - 2].text[x - 1], 14, 3, (uint)Sprites.TINY_FONT);
                                tempW += 12;
                            }
                        }

                        Vga256d.fill_rectangle_xy(Video.VGAScreen, 160, 39, 310, 48, 228);
                        Vga256d.fill_rectangle_xy(Video.VGAScreen, 160, 157, 310, 166, 228);

                        int percent_read = (cube[currentCube].last_line <= 9)
                                           ? 100
                                           : (yLoc * 100) / ((cube[currentCube].last_line - 9) * 12);

                        string buf = $"{Helptext.miscText[11]} {percent_read}%";
                        Fonthand.JE_outTextAndDarken(Video.VGAScreen, 176, 160, buf, 14, 1, (uint)Sprites.TINY_FONT);
                        Fonthand.JE_dString(Video.VGAScreen, 260, 160, Helptext.miscText[12], (uint)Sprites.SMALL_FONT_SHAPES);

                        if (temp2 == 0)
                            yChg = 0;

                        Mouse.JE_mouseStart();
                        Video.JE_showVGA();

                        if (backFromHelp)
                        {
                            Palette.fade_palette(Palette.colors, 10, 0, 255);
                            backFromHelp = false;
                        }
                        Mouse.JE_mouseReplace();

                        Nortsong.setFrameCount(1);
                    }
                    else
                    {
                        if (curMenu == MENU_PLAY_NEXT_LEVEL)
                        {
                            JE_updateNavScreen();
                            JE_drawMainMenuHelpText();
                            JE_drawMenuHeader();
                            JE_drawMenuChoices();
                            if (Config.extraGame)
                                Fonthand.JE_dString(Video.VGAScreen, 170, 140, MiscText(68), (uint)Sprites.FONT_SHAPES);
                        }

                        if (curMenu == MENU_DATA_CUBES &&
                            curSel[MENU_DATA_CUBES] < menuChoices[MENU_DATA_CUBES])
                        {
                            Sprites.blit_sprite_hv_blend(Video.VGAScreenSeg, 166, 38 + (curSel[MENU_DATA_CUBES] - 2) * 28, (uint)Sprites.OPTION_SHAPES, 25, 13, (sbyte)col);
                        }

                        if (curMenu == MENU_UPGRADE_SUB &&
                            (curSel[MENU_UPGRADES] == 3 ||
                             curSel[MENU_UPGRADES] == 4 ||
                             (curSel[MENU_UPGRADES] >= 6 &&
                              curSel[MENU_UPGRADES] <= 8)))
                        {
                            Nortsong.setFrameCount(3);
                            JE_weaponSimUpdate();
                            JE_drawScore();

                            if (newPal > 0)
                            {
                                curPal = newPal;
                                Palette.set_palette(Palette.palettes[newPal - 1], 0, 255);
                                newPal = 0;
                            }

                            Mouse.JE_mouseStart();

                            if (paletteChanged)
                            {
                                Palette.set_palette(Palette.colors, 0, 255);
                                paletteChanged = false;
                            }

                            Video.JE_showVGA();

                            if (backFromHelp)
                            {
                                Palette.fade_palette(Palette.colors, 10, 0, 255);
                                backFromHelp = false;
                            }

                            Mouse.JE_mouseReplace();
                        }
                        else
                        {
                            Nortsong.setFrameCount(2);

                            JE_drawScore();

                            if (newPal > 0)
                            {
                                curPal = newPal;
                                Palette.set_palette(Palette.palettes[newPal - 1], 0, 255);
                                newPal = 0;
                            }

                            Mouse.JE_mouseStart();

                            if (paletteChanged)
                            {
                                Palette.set_palette(Palette.colors, 0, 255);
                                paletteChanged = false;
                            }

                            Video.JE_showVGA();

                            Mouse.JE_mouseReplace();

                            if (backFromHelp)
                            {
                                Palette.fade_palette(Palette.colors, 10, 0, 255);
                                backFromHelp = false;
                            }
                        }
                    }

                    Keyboard.waitUntilElapsed();

                    if (curMenu == MENU_DATA_CUBE_SUB)
                    {
                        mouseButtonsHeld &= Keyboard.mouseButtonsDown;

                        if ((Keyboard.mouseButtonsDown & ~mouseButtonsHeld) != 0 &&
                            Mouse.mouseCursor != Mouse.MOUSE_POINTER_NORMAL)
                        {
                            if (Mouse.mouseCursor == Mouse.MOUSE_POINTER_UP)
                                yChg = -1;
                            else
                                yChg = 1;
                        }

                        if (Keyboard.keysactive[SdlKeys.SDL_SCANCODE_PAGEUP]) yChg = -2;
                        if (Keyboard.keysactive[SdlKeys.SDL_SCANCODE_PAGEDOWN]) yChg = 2;
                        if (Keyboard.keysactive[SdlKeys.SDL_SCANCODE_UP]) yChg = -1;
                        if (Keyboard.keysactive[SdlKeys.SDL_SCANCODE_DOWN]) yChg = 1;

                        if (yChg < 0 && yLoc == 0) yChg = 0;
                        if (yChg > 0 && (yLoc / 12) > cube[currentCube].last_line - 10) yChg = 0;
                    }

                    if (Keyboard.hasInput(InputFlags.INPUT_NO_MOTION))
                        break;
                }
            }

            /* Input events */
            if (Keyboard.mouseGetInput(InputFlags.INPUT_NO_MOTION, out MouseInput mouseInput))
            {
                lastDirection = 1;

                if (curMenu == MENU_DATA_CUBES && Config.cubeMax == 0)
                {
                    curMenu = MENU_FULL_GAME;
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING);
                    newPal = 1;
                }

                if (curMenu == MENU_DATA_CUBE_SUB)
                {
                    if (mouseInput.x > 258 && mouseInput.x < 290 && mouseInput.y > 159 && mouseInput.y < 171)
                    {
                        curMenu = MENU_DATA_CUBES;
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING);
                    }
                }

                if (curMenu == MENU_OPTIONS || curMenu == MENU_LIMITED_OPTIONS)
                {
                    if (mouseInput.x >= 225 - 4 && mouseInput.y >= 70 && mouseInput.y <= 82)
                    {
                        if (Loudness.music_disabled) { Loudness.music_disabled = false; Loudness.restart_song(); }
                        curSel[MENU_OPTIONS] = 4;
                        Nortsong.tyrMusicVolume = (ushort)((mouseInput.x - (225 - 4)) / 4 * 12);
                        if (Nortsong.tyrMusicVolume > 255) Nortsong.tyrMusicVolume = 255;
                    }
                    if (mouseInput.x >= 225 - 4 && mouseInput.y >= 86 && mouseInput.y <= 98)
                    {
                        Loudness.samples_disabled = false;
                        curSel[MENU_OPTIONS] = 5;
                        Nortsong.fxVolume = (ushort)((mouseInput.x - (225 - 4)) / 4 * 12);
                        if (Nortsong.fxVolume > 255) Nortsong.fxVolume = 255;
                    }
                    Loudness.set_volume((byte)Nortsong.tyrMusicVolume, (byte)Nortsong.fxVolume);
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                }

                if (mouseInput.y > 20 && mouseInput.x > 170 && mouseInput.x < 308 && curMenu != MENU_DATA_CUBE_SUB)
                {
                    byte[] mouseSelectionY = { 16, 16, 16, 16, 26, 12, 11, 28, 0, 16, 16, 16, 8, 16 };
                    int selection = (mouseInput.y - 38) / mouseSelectionY[curMenu] + 2;

                    if (curMenu == MENU_2_PLAYER_ARCADE)
                    {
                        if (selection > 5) selection--;
                        if (selection > 3) selection--;
                    }
                    if (curMenu == MENU_FULL_GAME)
                    {
                        if (selection > 7) selection = 7;
                    }
                    if (curMenu == MENU_PLAY_NEXT_LEVEL)
                    {
                        if (selection == menuChoices[curMenu] + 1) selection = menuChoices[curMenu];
                    }

                    if (selection <= menuChoices[curMenu])
                    {
                        if (curMenu == MENU_UPGRADE_SUB && selection == menuChoices[MENU_UPGRADE_SUB])
                        {
                            player[0].cash = (uint)JE_cashLeft();
                            curMenu = MENU_UPGRADES;
                            Nortsong.JE_playSampleNum((byte)Sndmast.S_ITEM);
                        }
                        else
                        {
                            Nortsong.JE_playSampleNum((byte)Sndmast.S_CLICK);
                            if (curSel[curMenu] == selection)
                            {
                                JE_menuFunction(curSel[curMenu]);
                            }
                            else
                            {
                                if (curMenu == MENU_UPGRADE_SUB &&
                                    Mainint.JE_getCost((byte)curSel[MENU_UPGRADES], Tyrian2.itemAvail[itemAvailMap[curSel[MENU_UPGRADES] - 2] - 1, selection - 2]) > player[0].cash)
                                {
                                    Nortsong.JE_playSampleNum((byte)Sndmast.S_CLINK);
                                }
                                else
                                {
                                    if (curSel[MENU_UPGRADES] == 4)
                                        player[0].weapon_mode = 1;
                                    curSel[curMenu] = (byte)selection;
                                }

                                if (curMenu == MENU_UPGRADE_SUB && (curSel[MENU_UPGRADES] == 3 || curSel[MENU_UPGRADES] == 4))
                                    player[0].items.weapon[curSel[MENU_UPGRADES] - 3].power = (byte)temp_weapon_power[curSel[MENU_UPGRADE_SUB] - 2];
                            }
                        }
                    }
                }

                if (curMenu == MENU_UPGRADE_SUB && (curSel[MENU_UPGRADES] == 3 || curSel[MENU_UPGRADES] == 4))
                {
                    if (mouseInput.x >= 23 && mouseInput.x <= 36 && mouseInput.y >= 149 && mouseInput.y <= 168)
                    {
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                        if (leftPower)
                            player[0].items.weapon[curSel[MENU_UPGRADES] - 3].power = (byte)(--temp_weapon_power[curSel[MENU_UPGRADE_SUB] - 2]);
                        else
                            Nortsong.JE_playSampleNum((byte)Sndmast.S_CLINK);
                    }
                    if (mouseInput.x >= 119 && mouseInput.x <= 131 && mouseInput.y >= 149 && mouseInput.y <= 168)
                    {
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                        if (rightPower && rightPowerAfford)
                            player[0].items.weapon[curSel[MENU_UPGRADES] - 3].power = (byte)(++temp_weapon_power[curSel[MENU_UPGRADE_SUB] - 2]);
                        else
                            Nortsong.JE_playSampleNum((byte)Sndmast.S_CLINK);
                    }
                }
            }
            else if (Keyboard.keyboardGetInput(out KeyboardInput keyboardInput))
            {
                switch (keyboardInput.scancode)
                {
                case SdlKeys.SDL_SCANCODE_SLASH:
                    if (curMenu == MENU_UPGRADE_SUB && curSel[MENU_UPGRADES] == 4)
                    {
                        if (++player[0].weapon_mode > Episodes.weaponPort[player[0].items.weapon[Players.REAR_WEAPON].id].opnum)
                            player[0].weapon_mode = 1;
                    }
                    break;

                case SdlKeys.SDL_SCANCODE_SPACE:
                case SdlKeys.SDL_SCANCODE_RETURN:
                    if (curMenu == MENU_UPGRADE_SUB && (curSel[MENU_UPGRADES] == 3 || curSel[MENU_UPGRADES] == 4))
                        temp_weapon_power[Tyrian2.itemAvailMax[itemAvailMap[curSel[MENU_UPGRADES] - 2] - 1]] = player[0].items.weapon[curSel[MENU_UPGRADES] - 3].power;
                    JE_menuFunction(curSel[curMenu]);
                    break;

                case SdlKeys.SDL_SCANCODE_ESCAPE:
                    Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING);
                    if (curMenu == MENU_LOAD_SAVE && quikSave)
                    {
                        curMenu = oldMenu;
                        newPal = oldPal;
                    }
                    else if (menuEsc[curMenu] == 0)
                    {
                        if (JE_quitRequest())
                        {
                            Varz.gameLoaded = true;
                            Config.mainLevel = 0;
                        }
                    }
                    else
                    {
                        if (curMenu == MENU_UPGRADE_SUB)
                        {
                            player[0].items = old_items[0];
                            curSel[MENU_UPGRADE_SUB] = lastCurSel;
                            player[0].cash = (uint)JE_cashLeft();
                        }
                        if (curMenu != MENU_DATA_CUBE_SUB)
                            newPal = 1;
                        curMenu = menuEsc[curMenu] - 1;
                    }
                    break;

                case SdlKeys.SDL_SCANCODE_F1:
                    if (!Config.isNetworkGame)
                    {
                        Palette.fade_black(10);
                        Mainint.JE_helpSystem(2);
                        Loudness.play_song(Musmast.songBuy);
                        Picload.JE_loadPic(Video.VGAScreen, 1, false);
                        newPal = 1;
                        switch (curMenu)
                        {
                        case 3: newPal = 18; break;
                        case 7:
                        case 8: break;
                        }
                        CopyScreen(Video.VGAScreen2, Video.VGAScreen);
                        curPal = newPal;
                        Array.Copy(Palette.palettes[newPal - 1], Palette.colors, Palette.colors.Length);
                        Video.JE_showVGA();
                        newPal = 0;
                        backFromHelp = true;
                    }
                    break;

                case SdlKeys.SDL_SCANCODE_UP:
                    lastDirection = -1;
                    if (curMenu != MENU_DATA_CUBE_SUB)
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                    curSel[curMenu]--;
                    if (curSel[curMenu] < 2)
                        curSel[curMenu] = menuChoices[curMenu];
                    if (curMenu == MENU_UPGRADE_SUB && (curSel[MENU_UPGRADES] == 3 || curSel[MENU_UPGRADES] == 4))
                    {
                        player[0].items.weapon[curSel[MENU_UPGRADES] - 3].power = (byte)temp_weapon_power[curSel[MENU_UPGRADE_SUB] - 2];
                        if (curSel[MENU_UPGRADES] == 4)
                            player[0].weapon_mode = 1;
                    }
                    break;

                case SdlKeys.SDL_SCANCODE_DOWN:
                    lastDirection = 1;
                    if (curMenu != MENU_DATA_CUBE_SUB)
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                    curSel[curMenu]++;
                    if (curSel[curMenu] > menuChoices[curMenu])
                        curSel[curMenu] = 2;
                    if (curMenu == MENU_UPGRADE_SUB && (curSel[MENU_UPGRADES] == 3 || curSel[MENU_UPGRADES] == 4))
                    {
                        player[0].items.weapon[curSel[MENU_UPGRADES] - 3].power = (byte)temp_weapon_power[curSel[MENU_UPGRADE_SUB] - 2];
                        if (curSel[MENU_UPGRADES] == 4)
                            player[0].weapon_mode = 1;
                    }
                    break;

                case SdlKeys.SDL_SCANCODE_HOME:
                    if (curMenu == MENU_DATA_CUBE_SUB)
                        yLoc = 0;
                    break;

                case SdlKeys.SDL_SCANCODE_END:
                    if (curMenu == MENU_DATA_CUBE_SUB)
                        yLoc = (cube[currentCube].last_line - 9) * 12;
                    break;

                case SdlKeys.SDL_SCANCODE_LEFT:
                    if (curMenu == MENU_OPTIONS || curMenu == MENU_UPGRADE_SUB || curMenu == MENU_LIMITED_OPTIONS)
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                    switch (curMenu)
                    {
                    case MENU_OPTIONS:
                    case MENU_LIMITED_OPTIONS:
                        switch (curSel[curMenu])
                        {
                        case 4:
                            Nortsong.JE_changeVolume(ref Nortsong.tyrMusicVolume, -12, ref Nortsong.fxVolume, 0);
                            if (Loudness.music_disabled) { Loudness.music_disabled = false; Loudness.restart_song(); }
                            break;
                        case 5:
                            Nortsong.JE_changeVolume(ref Nortsong.tyrMusicVolume, 0, ref Nortsong.fxVolume, -12);
                            Loudness.samples_disabled = false;
                            break;
                        }
                        break;
                    case MENU_UPGRADE_SUB:
                        switch (curSel[MENU_UPGRADES])
                        {
                        case 3:
                        case 4:
                            if (leftPower)
                                player[0].items.weapon[curSel[MENU_UPGRADES] - 3].power = (byte)(--temp_weapon_power[curSel[MENU_UPGRADE_SUB] - 2]);
                            else
                                Nortsong.JE_playSampleNum((byte)Sndmast.S_CLINK);
                            break;
                        }
                        break;
                    }
                    break;

                case SdlKeys.SDL_SCANCODE_RIGHT:
                    if (curMenu == MENU_OPTIONS || curMenu == MENU_UPGRADE_SUB || curMenu == MENU_LIMITED_OPTIONS)
                        Nortsong.JE_playSampleNum((byte)Sndmast.S_CURSOR);
                    switch (curMenu)
                    {
                    case MENU_OPTIONS:
                    case MENU_LIMITED_OPTIONS:
                        switch (curSel[curMenu])
                        {
                        case 4:
                            Nortsong.JE_changeVolume(ref Nortsong.tyrMusicVolume, 12, ref Nortsong.fxVolume, 0);
                            if (Loudness.music_disabled) { Loudness.music_disabled = false; Loudness.restart_song(); }
                            break;
                        case 5:
                            Nortsong.JE_changeVolume(ref Nortsong.tyrMusicVolume, 0, ref Nortsong.fxVolume, 12);
                            Loudness.samples_disabled = false;
                            break;
                        }
                        break;
                    case MENU_UPGRADE_SUB:
                        switch (curSel[MENU_UPGRADES])
                        {
                        case 3:
                        case 4:
                            if (rightPower && rightPowerAfford)
                                player[0].items.weapon[curSel[MENU_UPGRADES] - 3].power = (byte)(++temp_weapon_power[curSel[MENU_UPGRADE_SUB] - 2]);
                            else
                                Nortsong.JE_playSampleNum((byte)Sndmast.S_CLINK);
                            break;
                        }
                        break;
                    }
                    break;

                default:
                    switch (keyboardInput.sym)
                    {
                    case SdlKeys.SDLK_s:
                        if ((keyboardInput.mod & SdlKeys.KMOD_ALT) != 0 && curMenu != MENU_LOAD_SAVE)
                        {
                            if (curMenu == MENU_DATA_CUBE_SUB || curMenu == MENU_DATA_CUBES)
                                curMenu = MENU_FULL_GAME;
                            quikSave = true;
                            oldMenu = curMenu;
                            curMenu = MENU_LOAD_SAVE;
                            Mainint.performSave = true;
                            newPal = 1;
                            oldPal = curPal;
                        }
                        break;
                    case SdlKeys.SDLK_l:
                        if ((keyboardInput.mod & SdlKeys.KMOD_ALT) != 0 && curMenu != MENU_LOAD_SAVE)
                        {
                            if (curMenu == MENU_DATA_CUBE_SUB || curMenu == MENU_DATA_CUBES)
                                curMenu = MENU_FULL_GAME;
                            quikSave = true;
                            oldMenu = curMenu;
                            curMenu = MENU_LOAD_SAVE;
                            Mainint.performSave = false;
                            newPal = 1;
                            oldPal = curPal;
                        }
                        break;
                    default:
                        break;
                    }
                    break;
                }
            }

        } while (!(quit || Varz.gameLoaded || Tyrian2.jumpSection));

        if (Varz.gameLoaded)
            Palette.fade_black(10);
    }

    // 物品名稱輔助（fixed byte name → string）
    private static string ShipName(int i) { fixed (byte* p = Episodes.ships[i].name) return NameStr(p); }
    private static string WeaponName(int i) { fixed (byte* p = Episodes.weaponPort[i].name) return NameStr(p); }
    private static string ShieldName(int i) { fixed (byte* p = Episodes.shields[i].name) return NameStr(p); }
    private static string PowerName(int i) { fixed (byte* p = Episodes.powerSys[i].name) return NameStr(p); }
    private static string OptionName(int i) { fixed (byte* p = Episodes.options[i].name) return NameStr(p); }
    private static string SpecialName(int i) { fixed (byte* p = Episodes.special[i].name) return NameStr(p); }
}
