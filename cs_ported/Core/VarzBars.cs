namespace AprCSTyrian.Core;

/// <summary>varz.c 的護盾/裝甲 bar 繪製（先前因依賴 nortvars JE_dBar3 而延後）。</summary>
internal static unsafe partial class Varz
{
    public static void JE_drawShield()
    {
        var screen = Video.VGAScreen;
        var player = Players.player;

        if (Config.twoPlayerMode && !Config.galagaMode)
        {
            for (int i = 0; i < 2; ++i)
                Nortvars.JE_dBar3(screen, 270, 60 + 134 * i, (int)MathF.Round(player[i].shield * 0.8f), 144);
        }
        else
        {
            Nortvars.JE_dBar3(screen, 270, 194, (int)player[0].shield, 144);
            if (player[0].shield != player[0].shield_max)
            {
                int y = 193 - (int)(player[0].shield_max * 2);
                Vga256d.JE_rectangle(screen, 270, y, 278, y, 68); /* <MXD> SEGa000 */
            }
        }
    }

    public static void JE_drawArmor()
    {
        var screen = Video.VGAScreen;
        var player = Players.player;

        for (int i = 0; i < 2; ++i)
            if (player[i].armor > 28)
                player[i].armor = 28;

        if (Config.twoPlayerMode && !Config.galagaMode)
        {
            for (int i = 0; i < 2; ++i)
                Nortvars.JE_dBar3(screen, 307, 60 + 134 * i, (int)MathF.Round(player[i].armor * 0.8f), 224);
        }
        else
        {
            Nortvars.JE_dBar3(screen, 307, 194, (int)player[0].armor, 224);
        }
    }
}
