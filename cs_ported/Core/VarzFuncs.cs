namespace AprCSTyrian.Core;

/// <summary>
/// varz.c 中**自足**的函式移植（爆炸、超級像素、HUD bars、port 設定）。
/// 重度依賴 shots/weapons/audio 的函式（JE_specialComplete/JE_doSpecialShot/JE_getShipInfo/
/// JE_drawShield/Armor 等）待相關模組移植後補入。
/// </summary>
internal static unsafe partial class Varz
{
    private struct ExplosionData { public ushort sprite; public byte ttl; }

    private static readonly ExplosionData[] explosion_data = BuildExplosionData();

    private static ExplosionData[] BuildExplosionData()
    {
        (ushort, byte)[] d =
        {
            (144,7),(120,12),(190,12),(209,12),(152,12),(171,12),(133,7),(1,12),(20,12),(39,12),
            (58,12),(110,3),(76,7),(91,3),(227,3),(230,3),(233,3),(252,3),(246,3),(249,3),
            (265,3),(268,3),(271,3),(236,3),(239,3),(242,3),(261,3),(274,3),(277,3),(280,3),
            (299,3),(284,3),(287,3),(290,3),(293,3),(165,8),(184,8),(203,8),(222,8),(168,8),
            (187,8),(206,8),(225,10),(169,10),(188,10),(207,20),(226,14),(170,14),(189,14),(208,14),
            (246,14),(227,14),(265,14),
        };
        var a = new ExplosionData[d.Length];
        for (int i = 0; i < d.Length; ++i) { a[i].sprite = d[i].Item1; a[i].ttl = d[i].Item2; }
        return a;
    }

    public static void JE_setupExplosion(int x, int y, int deltaY, int type, bool fixedPosition, bool followPlayer)
    {
        if (y > -16 && y < 190)
        {
            for (int i = 0; i < VarzConst.MAX_EXPLOSIONS; i++)
            {
                if (explosions[i].ttl == 0)
                {
                    explosions[i].x = (short)x;
                    explosions[i].y = (short)y;
                    if (type == 6)
                    {
                        explosions[i].y += 12;
                        explosions[i].x += 2;
                    }
                    else if (type == 98)
                    {
                        type = 6;
                    }
                    explosions[i].sprite = explosion_data[type].sprite;
                    explosions[i].ttl = explosion_data[type].ttl;
                    explosions[i].followPlayer = followPlayer;
                    explosions[i].fixedPosition = fixedPosition;
                    explosions[i].deltaY = (short)deltaY;
                    break;
                }
            }
        }
    }

    public static void JE_setupExplosionLarge(bool enemyGround, byte exploNum, int x, int y)
    {
        if (y >= 0)
        {
            if (enemyGround)
            {
                JE_setupExplosion(x - 6, y - 14, 0, 2, false, false);
                JE_setupExplosion(x + 6, y - 14, 0, 4, false, false);
                JE_setupExplosion(x - 6, y, 0, 3, false, false);
                JE_setupExplosion(x + 6, y, 0, 5, false, false);
            }
            else
            {
                JE_setupExplosion(x - 6, y - 14, 0, 7, false, false);
                JE_setupExplosion(x + 6, y - 14, 0, 9, false, false);
                JE_setupExplosion(x - 6, y, 0, 8, false, false);
                JE_setupExplosion(x + 6, y, 0, 10, false, false);
            }

            bool big;
            if (exploNum > 10)
            {
                exploNum -= 10;
                big = true;
            }
            else
            {
                big = false;
            }

            if (exploNum != 0)
            {
                for (int i = 0; i < VarzConst.MAX_REPEATING_EXPLOSIONS; i++)
                {
                    if (rep_explosions[i].ttl == 0)
                    {
                        rep_explosions[i].ttl = exploNum;
                        rep_explosions[i].delay = 2;
                        rep_explosions[i].x = (uint)x;
                        rep_explosions[i].y = (uint)y;
                        rep_explosions[i].big = big;
                        break;
                    }
                }
            }
        }
    }

    public static void JE_doSP(ushort x, ushort y, ushort num, byte explowidth, byte color) // superpixels
    {
        for (temp = 0; temp < num; temp++)
        {
            float tempr = (float)(MtRand.mt_rand_lt1() * (2 * Opentyr.M_PI));
            int tempy = (int)MathF.Round(MathF.Cos(tempr) * MtRand.mt_rand_1() * explowidth, MidpointRounding.AwayFromZero);
            int tempx = (int)MathF.Round(MathF.Sin(tempr) * MtRand.mt_rand_1() * explowidth, MidpointRounding.AwayFromZero);

            if (++last_superpixel >= VarzConst.MAX_SUPERPIXELS)
                last_superpixel = 0;
            superpixels[last_superpixel].x = (uint)(tempx + x);
            superpixels[last_superpixel].y = (uint)(tempy + y);
            superpixels[last_superpixel].delta_x = tempx;
            superpixels[last_superpixel].delta_y = tempy + 1;
            superpixels[last_superpixel].color = color;
            superpixels[last_superpixel].z = 15;
        }
    }

    public static void JE_drawSP()
    {
        var screen = Video.VGAScreen;
        for (int i = VarzConst.MAX_SUPERPIXELS - 1; i >= 0; i--)
        {
            ref superpixel_type sp = ref superpixels[i];
            if (sp.z != 0)
            {
                sp.x = (uint)(sp.x + sp.delta_x);
                sp.y = (uint)(sp.y + sp.delta_y);

                if (sp.x < (uint)screen.w && sp.y < (uint)screen.h)
                {
                    byte* s = screen.pixels + sp.y * (uint)screen.pitch + sp.x;

                    *s = (byte)((((*s & 0x0f) + (int)sp.z) >> 1) + sp.color);
                    if (sp.x > 0)
                        *(s - 1) = (byte)((((*(s - 1) & 0x0f) + ((int)sp.z >> 1)) >> 1) + sp.color);
                    if (sp.x < (uint)screen.w - 1u)
                        *(s + 1) = (byte)((((*(s + 1) & 0x0f) + ((int)sp.z >> 1)) >> 1) + sp.color);
                    if (sp.y > 0)
                        *(s - screen.pitch) = (byte)((((*(s - screen.pitch) & 0x0f) + ((int)sp.z >> 1)) >> 1) + sp.color);
                    if (sp.y < (uint)screen.h - 1u)
                        *(s + screen.pitch) = (byte)((((*(s + screen.pitch) & 0x0f) + ((int)sp.z >> 1)) >> 1) + sp.color);
                }

                sp.z--;
            }
        }
    }

    public static ushort JE_portConfigs()
    {
        uint player_index = Config.twoPlayerMode ? 1u : 0u;
        return tempW = Episodes.weaponPort[Players.player[player_index].items.weapon[Players.REAR_WEAPON].id].opnum;
    }

    public static void JE_wipeShieldArmorBars()
    {
        var seg = Video.VGAScreenSeg;
        if (!Config.twoPlayerMode || Config.galagaMode)
            Vga256d.fill_rectangle_xy(seg, 270, 137, 278, 194 - (int)Players.player[0].shield * 2, 0);
        else
        {
            Vga256d.fill_rectangle_xy(seg, 270, 60 - 44, 278, 60, 0);
            Vga256d.fill_rectangle_xy(seg, 270, 194 - 44, 278, 194, 0);
        }
        if (!Config.twoPlayerMode || Config.galagaMode)
            Vga256d.fill_rectangle_xy(seg, 307, 137, 315, 194 - (int)Players.player[0].armor * 2, 0);
        else
        {
            Vga256d.fill_rectangle_xy(seg, 307, 60 - 44, 315, 60, 0);
            Vga256d.fill_rectangle_xy(seg, 307, 194 - 44, 315, 194, 0);
        }
    }

    public static void JE_drawOptions()
    {
        SDL_Surface temp_surface = Video.VGAScreen;
        Video.VGAScreen = Video.VGAScreenSeg;

        Player this_player = Players.player[Config.twoPlayerMode ? 1 : 0];

        for (int i = 0; i < this_player.sidekick.Length; ++i)
        {
            JE_OptionType this_option = Episodes.options[this_player.items.sidekick[i]];

            this_player.sidekick[i].ammo =
            this_player.sidekick[i].ammo_max = this_option.ammo;

            this_player.sidekick[i].ammo_refill_ticks =
            this_player.sidekick[i].ammo_refill_ticks_max = (uint)((105 - this_player.sidekick[i].ammo) * 4);

            this_player.sidekick[i].style = this_option.tr;

            this_player.sidekick[i].animation_enabled = (this_option.option == 1);
            this_player.sidekick[i].animation_frame = 0;

            this_player.sidekick[i].charge = 0;
            this_player.sidekick[i].charge_ticks = 20;

            // draw initial sidekick HUD
            int y = hud_sidekick_y[Config.twoPlayerMode ? 1 : 0, i];

            Vga256d.fill_rectangle_xy(Video.VGAScreenSeg, 284, y, 284 + 28, y + 15, 0);
            if (this_option.icongr > 0)
                Sprites.blit_sprite(Video.VGAScreenSeg, 284, y, Sprites.OPTION_SHAPES, (uint)(this_option.icongr - 1)); // sidekick HUD icon
            Vga256d.draw_segmented_gauge(Video.VGAScreenSeg, 284, y + 13, 112, 2, 2,
                (uint)Opentyr.MAX(1, this_player.sidekick[i].ammo_max / 10), (uint)this_player.sidekick[i].ammo);
        }

        Video.VGAScreen = temp_surface;

        JE_drawOptionLevel();
    }

    public static void JE_drawOptionLevel()
    {
        if (Config.twoPlayerMode)
        {
            for (temp = 1; temp <= 3; temp++)
            {
                Vga256d.fill_rectangle_xy(Video.VGAScreenSeg, 268, 127 + (temp - 1) * 6, 269, 127 + 3 + (temp - 1) * 6,
                    (byte)(193 + ((Players.player[1].items.sidekick_level - 100) == temp ? 1 : 0) * 11));
            }
        }
    }
}
