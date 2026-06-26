namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/game_menu.c —— 商店 (JE_itemScreen) / 設定選單。**逐步移植中**：
/// 先放入自足的繪製 helper（JE_drawItem 等）；完整選單狀態機與 JE_itemScreen 主迴圈待後續。
/// </summary>
internal static unsafe partial class GameMenu
{
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
