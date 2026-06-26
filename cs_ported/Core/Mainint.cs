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
