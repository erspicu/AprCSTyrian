namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/game_menu.c —— 商店 (JE_itemScreen) / 設定選單。**逐步移植中**：
/// 先放入自足的繪製 helper（JE_drawItem 等）；完整選單狀態機與 JE_itemScreen 主迴圈待後續。
/// </summary>
internal static unsafe partial class GameMenu
{
    // 選單型別（對應 game_menu.c enum）
    public const int MENU_FULL_GAME = 0, MENU_UPGRADES = 1, MENU_OPTIONS = 2, MENU_PLAY_NEXT_LEVEL = 3,
                     MENU_UPGRADE_SUB = 4, MENU_KEYBOARD_CONFIG = 5, MENU_LOAD_SAVE = 6, MENU_DATA_CUBES = 7,
                     MENU_DATA_CUBE_SUB = 8, MENU_2_PLAYER_ARCADE = 9, MENU_1_PLAYER_ARCADE = 10,
                     MENU_LIMITED_OPTIONS = 11, MENU_JOYSTICK_CONFIG = 12, MENU_SUPER_TYRIAN = 13;
    public const int MENU_MAX = 14;
    private const int SAVE_FILES_NUM = 11 * 2;

#pragma warning disable CS0649 // 由尚未移植的 JE_itemScreen 選單迴圈指派
    internal sealed class CubeStruct
    {
        public string title = "";
        public string header = "";
        public int face_sprite;
        public readonly string[] text = new string[90];
        public int last_line;
    }

    // 選單狀態
    public static readonly byte[] menuChoices = new byte[MENU_MAX];
    public static readonly byte[] menuChoicesDefault = { 7, 9, 8, 0, 0, 11, (SAVE_FILES_NUM / 2) + 2, 0, 0, 6, 4, 6, 7, 5 };
    public static readonly byte[] menuEsc = { 0, 1, 1, 1, 2, 3, 3, 1, 8, 0, 0, 11, 3, 0 };
    public static readonly byte[] itemAvailMap = { 1, 2, 3, 9, 4, 6, 7 };
    public static int curMenu;
    public static readonly byte[] curSel = new byte[MENU_MAX];
    public static byte curItemType, curItem, cursor;
    public static readonly CubeStruct[] cube = { new(), new(), new(), new() };
    public static readonly PlayerItems[] old_items = new PlayerItems[2];
    public static bool leftPower, rightPower, rightPowerAfford;
#pragma warning restore CS0649

    /// <summary>對應 game_menu.c:JE_drawMenuHeader —— 畫選單標題。</summary>
    public static void JE_drawMenuHeader()
    {
        string tempStr = curMenu switch
        {
            MENU_DATA_CUBE_SUB => cube[curSel[MENU_DATA_CUBES] - 2].header,
            MENU_DATA_CUBES => Helptext.menuInt[1][1],
            MENU_LOAD_SAVE => Helptext.menuInt[3][(Mainint.performSave ? 1 : 0) + 1],
            _ => Helptext.menuInt[curMenu + 1][0],
        };
        Fonthand.JE_dString(Video.VGAScreen, 74 + Fonthand.JE_fontCenter(tempStr, (uint)Sprites.FONT_SHAPES), 10, tempStr, (uint)Sprites.FONT_SHAPES);
    }

    /// <summary>對應 game_menu.c:JE_drawMenuChoices —— 畫選單項目（選中者前綴 ~）。</summary>
    public static void JE_drawMenuChoices()
    {
        for (int x = 2; x <= menuChoices[curMenu]; x++)
        {
            int tempY = 38 + (x - 1) * 16;

            if (curMenu == MENU_FULL_GAME && x == 7)
                tempY += 16;

            if (curMenu == MENU_2_PLAYER_ARCADE)
            {
                if (x > 3) tempY += 16;
                if (x > 4) tempY += 16;
            }

            if (!(curMenu == MENU_PLAY_NEXT_LEVEL && x == menuChoices[curMenu]))
                tempY -= 16;

            string item = Helptext.menuInt[curMenu + 1][x - 1];
            string str = (curSel[curMenu] == x) ? "~" + item : item;
            Fonthand.JE_dString(Video.VGAScreen, 166, tempY, str, (uint)Sprites.SMALL_FONT_SHAPES);
        }
    }

    // 星圖導航狀態
    public static readonly ushort[] planetX = { 200, 150, 240, 300, 270, 280, 320, 260, 220, 150, 160, 210, 80, 240, 220, 180, 310, 330, 150, 240, 200 };
    public static readonly ushort[] planetY = { 40, 90, 90, 80, 170, 30, 50, 130, 120, 150, 220, 200, 80, 50, 160, 10, 55, 55, 90, 90, 40 };
#pragma warning disable CS0649 // 由 JE_itemScreen 星圖邏輯指派
    public static byte planetAni, planetAniWait, currentDotNum, currentDotWait;
    public static float navX, navY, newNavX, newNavY;
    public static int tempNavX, tempNavY;
    public static readonly byte[] planetDots = new byte[5];
    public static readonly int[,] planetDotX = new int[5, 10];
    public static readonly int[,] planetDotY = new int[5, 10];
#pragma warning restore CS0649

    /// <summary>對應 game_menu.c:JE_drawLines —— 畫網格背景線。</summary>
    public static void JE_drawLines(SDL_Surface surface, bool dark)
    {
        int tempX2 = -10, tempY2 = 0;

        int tempW = 0;
        for (int x = 0; x < 20; x++)
        {
            tempW += 15;
            int tempX = tempW - tempX2;
            if (tempX > 18 && tempX < 135)
            {
                if (dark) Vga256d.JE_rectangle(surface, tempX + 1, 0, tempX + 1, 199, 32 + 3);
                else Vga256d.JE_rectangle(surface, tempX, 0, tempX, 199, 32 + 5);
            }
        }

        tempW = 0;
        for (int y = 0; y < 20; y++)
        {
            tempW += 15;
            int tempY = tempW - tempY2;
            if (tempY > 15 && tempY < 169)
            {
                if (dark) Vga256d.JE_rectangle(surface, 0, tempY + 1, 319, tempY + 1, 32 + 3);
                else Vga256d.JE_rectangle(surface, 0, tempY, 319, tempY, 32 + 5);

                int tempW2 = 0;
                for (int x = 0; x < 20; x++)
                {
                    tempW2 += 15;
                    int tempX = tempW2 - tempX2;
                    if (tempX > 18 && tempX < 135)
                        Vga256d.JE_pix3(surface, tempX, tempY, 32 + 6);
                }
            }
        }
    }

    /// <summary>對應 game_menu.c:JE_drawNavLines —— 畫星圖導航網格（隨 nav 位置捲動）。</summary>
    public static void JE_drawNavLines(bool dark)
    {
        int tempX2 = tempNavX >> 1, tempY2 = tempNavY >> 1;

        int tempW = 0;
        for (int x = 1; x <= 20; x++)
        {
            tempW += 15;
            int tempX = tempW - tempX2;
            if (tempX > 18 && tempX < 135)
            {
                if (dark) Vga256d.JE_rectangle(Video.VGAScreen, tempX + 1, 16, tempX + 1, 169, 1);
                else Vga256d.JE_rectangle(Video.VGAScreen, tempX, 16, tempX, 169, 5);
            }
        }

        tempW = 0;
        for (int y = 1; y <= 20; y++)
        {
            tempW += 15;
            int tempY = tempW - tempY2;
            if (tempY > 15 && tempY < 169)
            {
                if (dark) Vga256d.JE_rectangle(Video.VGAScreen, 19, tempY + 1, 135, tempY + 1, 1);
                else Vga256d.JE_rectangle(Video.VGAScreen, 8, tempY, 160, tempY, 5);

                int tempW2 = 0;
                for (int x = 0; x < 20; x++)
                {
                    tempW2 += 15;
                    int tempX = tempW2 - tempX2;
                    if (tempX > 18 && tempX < 135)
                        Vga256d.JE_pix3(Video.VGAScreen, tempX, tempY, 7);
                }
            }
        }
    }

    /// <summary>對應 game_menu.c:JE_weaponViewFrame —— 武器預覽單幀（建子彈/模擬/繪製 + power bar）。</summary>
    public static void JE_weaponViewFrame()
    {
        var player = Players.player;
        Vga256d.fill_rectangle_xy(Video.VGAScreen, 8, 8, 143, 182, 0);
        Backgrnd.update_and_draw_starfield(Video.VGAScreen, 1);

        player[0].mouseX = (ushort)player[0].x;
        player[0].mouseY = (ushort)player[0].y;

        for (int i = 0; i < 2; ++i)
        {
            if (Config.shotRepeat[i] > 0) { Config.shotRepeat[i]--; }
            else
            {
                int item = player[0].items.weapon[i].id;
                int item_power = player[0].items.weapon[i].power - 1;
                int item_mode = (i == Players.REAR_WEAPON) ? (int)player[0].weapon_mode - 1 : 0;
                Varz.b = Shots.player_shot_create((ushort)item, (uint)i, (ushort)player[0].x, (ushort)player[0].y, player[0].mouseX, player[0].mouseY, Episodes.weaponPort[item].op[item_mode * 11 + item_power], 1);
            }
        }

        int lsk = player[0].items.sidekick[Players.LEFT_SIDEKICK];
        if (Episodes.options[lsk].wport > 0)
        {
            if (Config.shotRepeat[Config.SHOT_LEFT_SIDEKICK] > 0) { Config.shotRepeat[Config.SHOT_LEFT_SIDEKICK]--; }
            else
            {
                int x = player[0].sidekick[Players.LEFT_SIDEKICK].x, y = player[0].sidekick[Players.LEFT_SIDEKICK].y;
                Varz.b = Shots.player_shot_create(Episodes.options[lsk].wport, (uint)Config.SHOT_LEFT_SIDEKICK, (ushort)x, (ushort)y, player[0].mouseX, player[0].mouseY, Episodes.options[lsk].wpnum, 1);
            }
        }

        int rsk = player[0].items.sidekick[Players.RIGHT_SIDEKICK];
        if (Episodes.options[rsk].tr == 2)
        {
            player[0].sidekick[Players.RIGHT_SIDEKICK].x = player[0].x;
            player[0].sidekick[Players.RIGHT_SIDEKICK].y = Math.Max(10, player[0].y - 20);
        }
        else
        {
            player[0].sidekick[Players.RIGHT_SIDEKICK].x = 72 + 15;
            player[0].sidekick[Players.RIGHT_SIDEKICK].y = 120;
        }

        if (Episodes.options[rsk].wport > 0)
        {
            if (Config.shotRepeat[Config.SHOT_RIGHT_SIDEKICK] > 0) { Config.shotRepeat[Config.SHOT_RIGHT_SIDEKICK]--; }
            else
            {
                int x = player[0].sidekick[Players.RIGHT_SIDEKICK].x, y = player[0].sidekick[Players.RIGHT_SIDEKICK].y;
                Varz.b = Shots.player_shot_create(Episodes.options[rsk].wport, (uint)Config.SHOT_RIGHT_SIDEKICK, (ushort)x, (ushort)y, player[0].mouseX, player[0].mouseY, Episodes.options[rsk].wpnum, 1);
            }
        }

        Shots.simulate_player_shots();
        Sprites.blit_sprite(Video.VGAScreenSeg, 0, 0, (uint)Sprites.OPTION_SHAPES, 12); // 升級介面

        // Power Bar
        Config.power += Config.powerAdd;
        if (Config.power > 900) Config.power = 900;

        int temp = (int)Config.power / 10;
        for (temp = 147 - temp; temp <= 146; temp++)
        {
            int temp2 = 113 + (146 - temp) / 9 + 2;
            int temp3 = (temp + 1) % 6;
            if (temp3 == 1) temp2 += 3;
            else if (temp3 != 0) temp2 += 2;

            Vga256d.JE_pix(Video.VGAScreen, 141, temp, (byte)(temp2 - 3));
            Vga256d.JE_pix(Video.VGAScreen, 142, temp, (byte)(temp2 - 3));
            Vga256d.JE_pix(Video.VGAScreen, 143, temp, (byte)(temp2 - 2));
            Vga256d.JE_pix(Video.VGAScreen, 144, temp, (byte)(temp2 - 1));
            Vga256d.fill_rectangle_xy(Video.VGAScreen, 145, temp, 149, temp, (byte)temp2);
        }

        temp = 147 - ((int)Config.power / 10);
        int tp2 = 113 + (146 - temp) / 9 + 4;
        Vga256d.JE_pix(Video.VGAScreen, 141, temp - 1, (byte)(tp2 - 1));
        Vga256d.JE_pix(Video.VGAScreen, 142, temp - 1, (byte)(tp2 - 1));
        Vga256d.JE_pix(Video.VGAScreen, 143, temp - 1, (byte)(tp2 - 1));
        Vga256d.JE_pix(Video.VGAScreen, 144, temp - 1, (byte)(tp2 - 1));
        Vga256d.fill_rectangle_xy(Video.VGAScreen, 145, temp - 1, 149, temp - 1, (byte)tp2);

        Config.lastPower = (uint)temp;
    }

    /// <summary>對應 game_menu.c:JE_weaponSimUpdate —— 武器模擬畫面更新（含升/降級成本/POWER 顯示）。</summary>
    public static void JE_weaponSimUpdate()
    {
        var player = Players.player;
        JE_weaponViewFrame();

        if ((curSel[MENU_UPGRADES] == 3 || curSel[MENU_UPGRADES] == 4) &&
            curSel[MENU_UPGRADE_SUB] < menuChoices[MENU_UPGRADE_SUB] &&
            Tyrian2.itemAvail[itemAvailMap[curSel[MENU_UPGRADES] - 2] - 1, curSel[MENU_UPGRADE_SUB] - 2] != 0)
        {
            if (leftPower)
                Fonthand.JE_outText(Video.VGAScreen, 26, 137, $"{Mainint.downgradeCost}", 1, 4);
            else
                Sprites.blit_sprite(Video.VGAScreenSeg, 24, 149, (uint)Sprites.OPTION_SHAPES, 13);

            if (rightPower)
            {
                if (!rightPowerAfford)
                {
                    Fonthand.JE_outText(Video.VGAScreen, 108, 137, $"{Mainint.upgradeCost}", 7, 4);
                    Sprites.blit_sprite(Video.VGAScreenSeg, 119, 149, (uint)Sprites.OPTION_SHAPES, 14);
                }
                else
                {
                    Fonthand.JE_outText(Video.VGAScreen, 108, 137, $"{Mainint.upgradeCost}", 1, 4);
                }
            }
            else
            {
                Sprites.blit_sprite(Video.VGAScreenSeg, 119, 149, (uint)Sprites.OPTION_SHAPES, 14);
            }

            int temp = player[0].items.weapon[curSel[MENU_UPGRADES] - 3].power;
            for (int x = 1; x <= temp; x++)
            {
                Vga256d.fill_rectangle_xy(Video.VGAScreen, 39 + x * 6, 151, 39 + x * 6 + 4, 151, 251);
                Vga256d.JE_pix(Video.VGAScreen, 39 + x * 6, 151, 252);
                Vga256d.fill_rectangle_xy(Video.VGAScreen, 39 + x * 6, 152, 39 + x * 6 + 4, 164, 250);
                Vga256d.fill_rectangle_xy(Video.VGAScreen, 39 + x * 6, 165, 39 + x * 6 + 4, 165, 249);
            }

            Fonthand.JE_outText(Video.VGAScreen, 58, 137, $"POWER: {temp}", 15, 4);
        }
        else
        {
            leftPower = false;
            rightPower = false;
            Sprites.blit_sprite(Video.VGAScreenSeg, 20, 146, (uint)Sprites.OPTION_SHAPES, 17);
        }

        JE_drawItem(1, player[0].items.ship, player[0].x - 5, player[0].y - 7);
    }

    /// <summary>對應 game_menu.c:JE_initWeaponView —— 初始化武器預覽（玩家居中、清子彈、起星空）。</summary>
    public static void JE_initWeaponView()
    {
        var player = Players.player;
        Vga256d.fill_rectangle_xy(Video.VGAScreen, 8, 8, 144, 177, 0);

        player[0].sidekick[Players.LEFT_SIDEKICK].x = 72 - 15;
        player[0].sidekick[Players.LEFT_SIDEKICK].y = 120;
        player[0].sidekick[Players.RIGHT_SIDEKICK].x = 72 + 15;
        player[0].sidekick[Players.RIGHT_SIDEKICK].y = 120;

        player[0].x = 72;
        player[0].y = 110;
        player[0].delta_x_shot_move = 0;
        player[0].delta_y_shot_move = 0;
        player[0].last_x_explosion_follow = 72;
        player[0].last_y_explosion_follow = 110;
        Config.power = 500;
        Config.lastPower = 500;

        Array.Clear(Shots.shotAvail);
        Array.Fill(Config.shotRepeat, (byte)1);
        Array.Clear(Config.shotMultiPos);

        Backgrnd.initialize_starfield();
    }

    /// <summary>對應 game_menu.c:JE_updateNavScreen —— 星圖導航畫面總繪製（網格/星球/虛線 + 平滑捲動）。</summary>
    public static void JE_updateNavScreen()
    {
        tempNavX = (int)MathF.Round(navX);
        tempNavY = (int)MathF.Round(navY);
        Vga256d.fill_rectangle_xy(Video.VGAScreen, 19, 16, 135, 169, 2);
        JE_drawNavLines(true);
        JE_drawNavLines(false);
        JE_drawDots();

        for (int x = 0; x < 11; x++)
            JE_drawPlanet(x);

        for (int x = 0; x < menuChoices[MENU_PLAY_NEXT_LEVEL] - 1; x++)
            if (Tyrian2.mapPlanet[x] > 11)
                JE_drawPlanet(Tyrian2.mapPlanet[x] - 1);

        if (Tyrian2.mapOrigin > 11)
            JE_drawPlanet(Tyrian2.mapOrigin - 1);

        Sprites.blit_sprite(Video.VGAScreenSeg, 0, 0, (uint)Sprites.OPTION_SHAPES, 28); // 導航畫面介面

        if (curSel[MENU_PLAY_NEXT_LEVEL] < menuChoices[MENU_PLAY_NEXT_LEVEL])
        {
            int oGr = Varz.PGR[Tyrian2.mapOrigin - 1] - 1;
            int origin_x_offset = Sprites.sprite((uint)Sprites.PLANET_SHAPES, (uint)oGr).width / 2;
            int origin_y_offset = Sprites.sprite((uint)Sprites.PLANET_SHAPES, (uint)oGr).height / 2;
            int destPlanet = Tyrian2.mapPlanet[curSel[MENU_PLAY_NEXT_LEVEL] - 2] - 1;
            int dGr = Varz.PGR[destPlanet] - 1;
            int dest_x_offset = Sprites.sprite((uint)Sprites.PLANET_SHAPES, (uint)dGr).width / 2;
            int dest_y_offset = Sprites.sprite((uint)Sprites.PLANET_SHAPES, (uint)dGr).height / 2;

            newNavX = (planetX[Tyrian2.mapOrigin - 1] - origin_x_offset + planetX[destPlanet] - dest_x_offset) / 2.0f;
            newNavY = (planetY[Tyrian2.mapOrigin - 1] - origin_y_offset + planetY[destPlanet] - dest_y_offset) / 2.0f;
        }

        navX += (newNavX - navX) / 2.0f;
        navY += (newNavY - navY) / 2.0f;
        if (MathF.Abs(newNavX - navX) < 1) navX = newNavX;
        if (MathF.Abs(newNavY - navY) < 1) navY = newNavY;

        Vga256d.fill_rectangle_xy(Video.VGAScreen, 314, 0, 319, 199, 230);

        if (planetAniWait > 0)
        {
            planetAniWait--;
        }
        else
        {
            planetAni++;
            if (planetAni > 14) planetAni = 0;
            planetAniWait = 3;
        }

        if (currentDotWait > 0)
        {
            currentDotWait--;
        }
        else
        {
            if (currentDotNum < planetDots[curSel[MENU_PLAY_NEXT_LEVEL] - 2])
                currentDotNum++;
            currentDotWait = 5;
        }
    }

    /// <summary>對應 game_menu.c:JE_partWay —— 兩星球間第 dist 個點的座標插值。</summary>
    public static int JE_partWay(int start, int finish, byte dots, byte dist)
        => (finish - start) / (dots + 2) * (dist + 1) + start;

    /// <summary>對應 game_menu.c:JE_computeDots —— 計算各路線的導航虛線點。</summary>
    public static void JE_computeDots()
    {
        for (int x = 0; x < Tyrian2.mapPNum; x++)
        {
            long distX = planetX[Tyrian2.mapPlanet[x] - 1] - planetX[Tyrian2.mapOrigin - 1];
            long distY = planetY[Tyrian2.mapPlanet[x] - 1] - planetY[Tyrian2.mapOrigin - 1];
            long tempX = Math.Abs(distX) + Math.Abs(distY);

            if (tempX != 0)
                planetDots[x] = (byte)(MathF.Round(MathF.Sqrt(MathF.Sqrt(distX * distX + distY * distY))) - 1);
            else
                planetDots[x] = 0;

            if (planetDots[x] > 10)
                planetDots[x] = 10;

            for (int y = 0; y < planetDots[x]; y++)
            {
                planetDotX[x, y] = JE_partWay(planetX[Tyrian2.mapOrigin - 1], planetX[Tyrian2.mapPlanet[x] - 1], planetDots[x], (byte)y);
                planetDotY[x, y] = JE_partWay(planetY[Tyrian2.mapOrigin - 1], planetY[Tyrian2.mapPlanet[x] - 1], planetDots[x], (byte)y);
            }
        }
    }

    /// <summary>對應 game_menu.c:JE_drawDots —— 畫星圖導航虛線點。</summary>
    public static void JE_drawDots()
    {
        for (int x = 0; x < Tyrian2.mapPNum; x++)
        {
            for (int y = 0; y < planetDots[x]; y++)
            {
                int tempX = planetDotX[x, y] - tempNavX + 66 - 2;
                int tempY = planetDotY[x, y] - tempNavY + 85 - 2;
                if (tempX > 0 && tempX < 140 && tempY > 0 && tempY < 168)
                    Sprites.blit_sprite(Video.VGAScreenSeg, tempX, tempY, (uint)Sprites.OPTION_SHAPES,
                        (x == curSel[MENU_PLAY_NEXT_LEVEL] - 2 && y < currentDotNum) ? 30u : 29u);
            }
        }
    }

    /// <summary>對應 game_menu.c:JE_drawPlanet —— 畫一個星球。</summary>
    public static void JE_drawPlanet(int planetNum)
    {
        int tempZ = Varz.PGR[planetNum] - 1;
        int w = Sprites.sprite((uint)Sprites.PLANET_SHAPES, (uint)tempZ).width;
        int hh = Sprites.sprite((uint)Sprites.PLANET_SHAPES, (uint)tempZ).height;
        int tempX = planetX[planetNum] + 66 - tempNavX - w / 2;
        int tempY = planetY[planetNum] + 85 - tempNavY - hh / 2;

        if (tempX > -7 && tempX + w < 170 && tempY > 0 && tempY < 160)
        {
            if (Varz.PAni[planetNum] != 0)
                tempZ += planetAni;
            Sprites.blit_sprite_dark(Video.VGAScreenSeg, tempX + 3, tempY + 3, (uint)Sprites.PLANET_SHAPES, (uint)tempZ, false);
            Sprites.blit_sprite(Video.VGAScreenSeg, tempX, tempY, (uint)Sprites.PLANET_SHAPES, (uint)tempZ);
        }
    }

    /// <summary>對應 game_menu.c:JE_scaleInPicture —— 由中心放大進場（縮放動畫，可按鍵跳過）。</summary>
    public static void JE_scaleInPicture(SDL_Surface dst, SDL_Surface src)
    {
        for (int i = 2; i < 160; i += 2)
        {
            JE_scaleBitmap(dst, src, 160 - i, 0, 160 + i - 1, 100 + (int)MathF.Round(i * 0.625f) - 1);
            Video.JE_showVGA();
            Joystick.push_joysticks_as_keyboard();
            Keyboard.handleSdlEvents();
            if (Keyboard.getInput())
                break;
        }
        long n = (long)src.pitch * src.h; // SDL_BlitSurface(src, NULL, dst, NULL)
        Buffer.MemoryCopy(src.pixels, dst.pixels, n, n);
        Video.JE_showVGA();
    }

    /// <summary>對應 game_menu.c:JE_doShipSpecs —— 顯示船艦規格屏（繪製→載回背景→放大進場→等待）。</summary>
    public static void JE_doShipSpecs()
    {
        JE_drawShipSpecs(Video.game_screen, Video.VGAScreen2);
        Picload.JE_loadPic(Video.VGAScreen2, 1, false);
        Nortsong.JE_playSampleNum((byte)Sndmast.S_SPRING);
        JE_scaleInPicture(Video.VGAScreen, Video.game_screen);
        Keyboard.waitUntilGetInput();
    }

    /// <summary>對應 game_menu.c:JE_drawShipSpecs —— 畫綠色船艦規格技術屏（船名/說明/船艦圖綠化）。</summary>
    public static void JE_drawShipSpecs(SDL_Surface screen, SDL_Surface temp_screen)
    {
        var player = Players.player;

        Video.JE_clr256(screen);
        JE_drawLines(screen, true);
        JE_drawLines(screen, false);
        Vga256d.JE_rectangle(screen, 0, 0, 319, 199, 37);
        Vga256d.JE_rectangle(screen, 1, 1, 318, 198, 35);

        string shipName;
        fixed (byte* p = Episodes.ships[player[0].items.ship].name) shipName = NameStr(p);
        Fonthand.JE_outText(screen, 10, 2, shipName, 12, 3);
        Helptext.JE_helpBox(screen, 100, 20, Helptext.shipInfo[player[0].items.ship - 1][0], 40, 9, 12, 1, Fonthand.FULL_SHADE);
        Helptext.JE_helpBox(screen, 100, 100, Helptext.shipInfo[player[0].items.ship - 1][1], 40, 9, 12, 1, Fonthand.FULL_SHADE);
        Fonthand.JE_outText(screen, Fonthand.JE_fontCenter(Helptext.miscText[4], (uint)Sprites.TINY_FONT), 190, Helptext.miscText[4], 12, 2);

        int temp_index;
        if (player[0].items.ship > 90)
            temp_index = 32;
        else if (player[0].items.ship > 0)
            temp_index = Episodes.ships[player[0].items.ship].bigshipgraphic;
        else
            temp_index = Episodes.ships[old_items[0].ship].bigshipgraphic;

        int temp_x, temp_y;
        switch (temp_index)
        {
            case 32: temp_x = 35; temp_y = 33; break;
            case 28: temp_x = 31; temp_y = 36; break;
            case 33: temp_x = 31; temp_y = 35; break;
            default: temp_x = 35; temp_y = 33; break;
        }
        temp_x -= 30;

        Video.JE_clr256(temp_screen);
        Sprites.blit_sprite(temp_screen, temp_x, temp_y, (uint)Sprites.OPTION_SHAPES, (uint)(temp_index - 1)); // 船艦圖

        // 綠化：邊緣偵測（與鄰點低 4 位比較）→ 強制綠調 0xc0
        byte* dst = screen.pixels;
        byte* src = temp_screen.pixels;
        int pitch = screen.pitch, h = screen.h;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < pitch; x++)
            {
                int avg = 0;
                if (y > 0) avg += *(src - pitch) & 0x0f;
                if (y < h - 1) avg += *(src + pitch) & 0x0f;
                if (x > 0) avg += *(src - 1) & 0x0f;
                if (x < pitch - 1) avg += *(src + 1) & 0x0f;
                avg /= 4;

                if ((*src & 0x0f) > avg)
                    *dst = (byte)((*src & 0x0f) | 0xc0);

                src++;
                dst++;
            }
        }
    }

    /// <summary>對應 game_menu.c:JE_scaleBitmap —— 將 src 縮放寫入 dst 的 (x1,y1)-(x2,y2) 矩形（最近鄰）。</summary>
    public static void JE_scaleBitmap(SDL_Surface dst, SDL_Surface src, int x1, int y1, int x2, int y2)
    {
        int w = x2 - x1 + 1, h = y2 - y1 + 1;
        float base_skip_w = src.pitch / (float)w;
        float base_skip_h = src.h / (float)h;

        byte* dstp = dst.pixels + y1 * dst.pitch + x1;
        float cumulative_skip_h = 0;

        for (int i = 0; i < h; i++)
        {
            byte* src_w = src.pixels + src.w * (uint)cumulative_skip_h;
            byte* srcp = src_w;
            cumulative_skip_h += base_skip_h;
            float cumulative_skip_w = 0;

            for (int j = 0; j < w; j++)
            {
                *dstp = *srcp;
                dstp++;
                cumulative_skip_w += base_skip_w;
                srcp = src_w + (uint)cumulative_skip_w;
            }

            dstp += dst.pitch - w;
        }
    }

    private static string NameStr(byte* p)
    {
        int n = 0;
        while (n < 30 && p[n] != 0) n++;
        var c = new char[n];
        for (int i = 0; i < n; ++i) c[i] = (char)p[i];
        return new string(c);
    }

    /// <summary>對應 game_menu.c:JE_genItemMenu —— 建立升級子選單的物品清單。</summary>
    public static void JE_genItemMenu(byte itemNum)
    {
        int mapIdx = itemAvailMap[itemNum - 2] - 1;
        menuChoices[MENU_UPGRADE_SUB] = (byte)(Tyrian2.itemAvailMax[mapIdx] + 2);

        int t3 = 2;
        byte t2 = playeritem_map(ref Players.player[0].items, itemNum - 2);

        Helptext.menuInt[5][0] = Helptext.menuInt[2][itemNum - 1];

        int tw;
        for (tw = 0; tw < Tyrian2.itemAvailMax[mapIdx]; tw++)
        {
            byte tmp = Tyrian2.itemAvail[mapIdx, tw];
            string name = "";
            switch (itemNum)
            {
                case 2: fixed (byte* p = Episodes.ships[tmp].name) name = NameStr(p); break;
                case 3:
                case 4: fixed (byte* p = Episodes.weaponPort[tmp].name) name = NameStr(p); break;
                case 5: fixed (byte* p = Episodes.shields[tmp].name) name = NameStr(p); break;
                case 6: fixed (byte* p = Episodes.powerSys[tmp].name) name = NameStr(p); break;
                case 7:
                case 8: fixed (byte* p = Episodes.options[tmp].name) name = NameStr(p); break;
            }
            if (tmp == t2)
                t3 = tw + 2;
            Helptext.menuInt[5][tw] = name;
        }

        Helptext.menuInt[5][tw] = Helptext.miscText[13];
        curSel[MENU_UPGRADE_SUB] = (byte)t3;
    }

    /// <summary>對應 game_menu.c:playeritem_map —— 玩家裝備第 i 項欄位的 ref（船/前後武器/護盾/動力/僚機×2）。</summary>
    public static ref byte playeritem_map(ref PlayerItems items, int i)
    {
        switch (i)
        {
            case 0: return ref items.ship;
            case 1: return ref items.weapon[Players.FRONT_WEAPON].id;
            case 2: return ref items.weapon[Players.REAR_WEAPON].id;
            case 3: return ref items.shield;
            case 4: return ref items.generator;
            case 5: return ref items.sidekick[0];
            case 6: return ref items.sidekick[1];
            default: throw new ArgumentOutOfRangeException(nameof(i));
        }
    }

    /// <summary>對應 game_menu.c:JE_cashLeft —— 購買後剩餘現金（含武器升級累計）。</summary>
    public static long JE_cashLeft()
    {
        long tempL = Players.player[0].cash;
        ushort itemNum = playeritem_map(ref Players.player[0].items, curSel[MENU_UPGRADES] - 2);
        tempL -= Mainint.JE_getCost((byte)curSel[MENU_UPGRADES], itemNum);

        if (curSel[MENU_UPGRADES] == 3 || curSel[MENU_UPGRADES] == 4)
        {
            uint tw = 0;
            for (uint i = 1; i < Players.player[0].items.weapon[curSel[MENU_UPGRADES] - 3].power; ++i)
            {
                tw += (uint)(Episodes.weaponPort[itemNum].cost * i);
                tempL -= tw;
            }
        }
        return tempL;
    }

    /// <summary>對應 game_menu.c:JE_drawScore —— 升級子選單顯示剩餘現金。</summary>
    public static void JE_drawScore()
    {
        if (curMenu == MENU_UPGRADE_SUB)
            Fonthand.JE_textShade(Video.VGAScreen, 65, 173, $"{JE_cashLeft()}", 1, 6, Fonthand.DARKEN);
    }

    /// <summary>對應 game_menu.c:JE_drawItem —— 畫一個物品圖示（武器/動力/選項/護盾/船艦）。</summary>
    public static void JE_drawItem(byte itemType, ushort itemNum, int x, int y)
    {
        if (itemNum == 0)
            return;

        ushort tempW = 0;
        switch (itemType)
        {
            case 2:
            case 3: tempW = Episodes.weaponPort[itemNum].itemgraphic; break;
            case 5: tempW = Episodes.powerSys[itemNum].itemgraphic; break;
            case 6:
            case 7: tempW = Episodes.options[itemNum].itemgraphic; break;
            case 4: tempW = Episodes.shields[itemNum].itemgraphic; break;
        }

        if (itemType == 1)
        {
            if (itemNum > 90)
            {
                Varz.shipGrPtr = Sprites.spriteSheet9;
                Varz.shipGr = Varz.JE_SGr((ushort)(itemNum - 90), ref Varz.shipGrPtr);
                Sprites.blit_sprite2x2(Video.VGAScreen, x, y, Varz.shipGrPtr, Varz.shipGr);
            }
            else
            {
                Sprites.blit_sprite2x2(Video.VGAScreen, x, y, Sprites.spriteSheet9, Episodes.ships[itemNum].shipgraphic);
            }
        }
        else if (tempW > 0)
        {
            Sprites.blit_sprite2x2(Video.VGAScreen, x, y, Sprites.shopSpriteSheet, tempW);
        }
    }
}
