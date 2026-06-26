namespace AprCSTyrian.Core;

/// <summary>
/// varz.c 的船艦資訊函式（JE_getShipInfo/JE_SGr），先前因依賴 episodes/editship 而延後。
/// shipGrPtr/shipGr2ptr 原為 Sprite2_array*；C# 改用 Sprite2_array 值複製（共用 data 指標）。
/// </summary>
internal static unsafe partial class Varz
{
    public static ushort shipGr, shipGr2;
    public static Sprite2_array shipGrPtr, shipGr2ptr;

    public static void JE_getShipInfo()
    {
        var player = Players.player;

        shipGrPtr = Sprites.spriteSheet9;
        shipGr2ptr = Sprites.spriteSheet9;

        Config.powerAdd = Episodes.powerSys[player[0].items.generator].power;

        bool extraShip = player[0].items.ship > 90;
        if (extraShip)
        {
            int bas = (player[0].items.ship - 91) * 15;
            shipGr = JE_SGr((ushort)(player[0].items.ship - 90), ref shipGrPtr);
            player[0].armor = Editship.extraShips[bas + 7];
        }
        else
        {
            shipGr = Episodes.ships[player[0].items.ship].shipgraphic;
            player[0].armor = Episodes.ships[player[0].items.ship].dmg;
        }

        bool extraShip2 = player[1].items.ship > 90;
        if (extraShip2)
        {
            int bas2 = (player[1].items.ship - 91) * 15;
            shipGr2 = JE_SGr((ushort)(player[1].items.ship - 90), ref shipGr2ptr);
            player[1].armor = Editship.extraShips[bas2 + 7];
        }
        else
        {
            shipGr2 = 0;
            player[1].armor = 10;
        }

        for (uint i = 0; i < 2; ++i)
        {
            player[i].initial_armor = player[i].armor;

            uint t = ((i == 0 && extraShip) || (i == 1 && extraShip2)) ? 2u : Episodes.ships[player[i].items.ship].ani;

            if (t == 0)
            {
                player[i].shot_hit_area_x = 12;
                player[i].shot_hit_area_y = 10;
            }
            else
            {
                player[i].shot_hit_area_x = 11;
                player[i].shot_hit_area_y = 14;
            }
        }
    }

    public static ushort JE_SGr(ushort ship, ref Sprite2_array ptr)
    {
        ushort[] GR = { 233, 157, 195, 271, 81, 0, 119, 5, 43, 81, 119, 157, 195, 233, 271 };

        ushort tempW = Editship.extraShips[(ship - 1) * 15];
        if (tempW > 7)
            ptr = Editship.extraShapes;

        return GR[tempW - 1];
    }
}
