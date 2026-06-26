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
