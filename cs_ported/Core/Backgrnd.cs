namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/backgrnd.c —— 遊戲內三層捲動背景繪製。
/// C 原版以 byte** 指標走訪 megaData.mainmap；此處 mainmap 為 nint[]，故改用「flat 索引」走訪（行為等價）。
/// mapYPos/mapY2Pos/mapY3Pos（C 的 byte**）對應為 mapYPosIdx/… 的 flat 索引（指向各自 megaData.mainmap）。
/// </summary>
internal static unsafe partial class Backgrnd
{
#pragma warning disable CS0649 // 由遊戲主迴圈/JE_loadMap 指派
    public static ushort mapX, mapY, mapX2, mapX3, mapY2, mapY3;
    public static ushort backPos, backPos2, backPos3;
    public static ushort backMove, backMove2, backMove3;
    public static ushort mapXPos, oldMapXOfs, mapXOfs, mapX2Ofs, mapX2Pos, mapX3Pos, oldMapX3Ofs, mapX3Ofs, tempMapXOfs;
    public static int mapXbpPos, mapX2bpPos, mapX3bpPos;          // C: intptr_t（tile 欄位偏移）
    public static int mapYPosIdx, mapY2PosIdx, mapY3PosIdx;       // C: byte**（改為 mainmap flat 索引）
    public static int BKwrap1Idx, BKwrap1toIdx;                   // C: BKwrap1/BKwrap1to（megaData1.mainmap flat 索引）
    public static int BKwrap2Idx, BKwrap2toIdx;                   // C: BKwrap2/BKwrap2to（megaData2.mainmap flat 索引）
    public static int BKwrap3Idx, BKwrap3toIdx;                   // C: BKwrap3/BKwrap3to（megaData3.mainmap flat 索引）
    public static ushort tempBackMove;                            // 當前敵人層的背景捲動量
    public static byte map1YDelay, map1YDelayMax, map2YDelay, map2YDelayMax;
    public static bool anySmoothies;
    public static ushort neat;                                    // tyrian2.c: superWild 用全域累加（JE_darkenBackground 的明暗等級）
    public static readonly byte[] smoothie_data = new byte[9];
    public static int starfield_speed;
#pragma warning restore CS0649

    // 遊戲內星空（對應 backgrnd.c initialize_starfield / update_and_draw_starfield）
    private const int MAX_STARS = 100;
    private const int STARFIELD_HUE = 0x90;
    private struct StarfieldStar { public int position; public int speed; public byte color; }
    private static readonly StarfieldStar[] starfield_stars = new StarfieldStar[MAX_STARS];

    public static void initialize_starfield()
    {
        int pitch = Video.VGAScreen.pitch;
        for (int i = MAX_STARS - 1; i >= 0; --i)
        {
            starfield_stars[i].position = (int)(MtRand.mt_rand() % 320 + MtRand.mt_rand() % 200 * (uint)pitch);
            starfield_stars[i].speed = (int)(MtRand.mt_rand() % 3 + 2);
            starfield_stars[i].color = (byte)(MtRand.mt_rand() % 16 + STARFIELD_HUE);
        }
    }

    public static void update_and_draw_starfield(SDL_Surface surface, int move_speed)
    {
        byte* p = surface.pixels;
        int pitch = surface.pitch;
        for (int i = MAX_STARS - 1; i >= 0; --i)
        {
            ref StarfieldStar star = ref starfield_stars[i];
            star.position += (star.speed + move_speed) * pitch;

            if (star.position < 177 * pitch)
            {
                if (p[star.position] == 0)
                    p[star.position] = star.color;

                if (star.color - 4 >= STARFIELD_HUE)
                {
                    if (p[star.position + 1] == 0) p[star.position + 1] = (byte)(star.color - 4);
                    if (star.position > 0 && p[star.position - 1] == 0) p[star.position - 1] = (byte)(star.color - 4);
                    if (p[star.position + pitch] == 0) p[star.position + pitch] = (byte)(star.color - 4);
                    if (star.position >= pitch && p[star.position - pitch] == 0) p[star.position - pitch] = (byte)(star.color - 4);
                }
            }
        }
    }

    public static void JE_darkenBackground(ushort neat) /* wild detail level */
    {
        var screen = Video.VGAScreen;
        byte* s = screen.pixels + 24;
        int pitch = screen.pitch;

        for (int y = 184; y != 0; y--)
        {
            for (int x = 264; x != 0; x--)
            {
                int upper = (y == 184) ? 0 : *(s - (pitch - 1));
                *s = (byte)(((((*s & 0x0f) << 4) - (*s & 0x0f) + ((((x - neat - y) >> 2) + *(s - 2) + upper) & 0x0f)) >> 4) | (*s & 0xf0));
                s++;
            }
            s += pitch - 264;
        }
    }

    /// <summary>對應 blit_background_row。map = megaData.mainmap，從 mapIdx 起算 12 個 tile。</summary>
    private static void blit_background_row(SDL_Surface surface, int x, int y, JE_MegaData md, int mapIdx)
    {
        byte* pixels = surface.pixels + (y * surface.pitch) + x;
        byte* pixels_ll = surface.pixels;
        byte* pixels_ul = surface.pixels + (surface.h * surface.pitch);

        for (int ty = 0; ty < 28; ty++)
        {
            if ((pixels + (12 * 24)) < pixels_ll)
            {
                pixels += surface.pitch;
                continue;
            }

            for (int tile = 0; tile < 12; tile++)
            {
                int idx = mapIdx + tile;
                byte* data = (idx >= 0 && idx < md.mainmap.Length) ? (byte*)md.mainmap[idx] : null;

                if (data == null)
                {
                    pixels += 24;
                    continue;
                }

                data += ty * 24;

                for (int tx = 24; tx != 0; tx--)
                {
                    if (pixels >= pixels_ul)
                        return;
                    if (pixels >= pixels_ll && *data != 0)
                        *pixels = *data;
                    pixels++;
                    data++;
                }
            }

            pixels += surface.pitch - 12 * 24;
        }
    }

    private static void blit_background_row_blend(SDL_Surface surface, int x, int y, JE_MegaData md, int mapIdx)
    {
        byte* pixels = surface.pixels + (y * surface.pitch) + x;
        byte* pixels_ll = surface.pixels;
        byte* pixels_ul = surface.pixels + (surface.h * surface.pitch);

        for (int ty = 0; ty < 28; ty++)
        {
            if ((pixels + (12 * 24)) < pixels_ll)
            {
                pixels += surface.pitch;
                continue;
            }

            for (int tile = 0; tile < 12; tile++)
            {
                int idx = mapIdx + tile;
                byte* data = (idx >= 0 && idx < md.mainmap.Length) ? (byte*)md.mainmap[idx] : null;

                if (data == null)
                {
                    pixels += 24;
                    continue;
                }

                data += ty * 24;

                for (int tx = 24; tx != 0; tx--)
                {
                    if (pixels >= pixels_ul)
                        return;
                    if (pixels >= pixels_ll && *data != 0)
                        *pixels = (byte)((*data & 0xf0) | (((*pixels & 0x0f) + (*data & 0x0f)) / 2));
                    pixels++;
                    data++;
                }
            }

            pixels += surface.pitch - 12 * 24;
        }
    }

    public static void draw_background_1(SDL_Surface surface)
    {
        Sdl.SDL_FillRect(surface, null, 0);

        int mapIdx = mapYPosIdx + mapXbpPos - 12;
        for (int i = -1; i < 7; i++)
        {
            blit_background_row(surface, mapXPos, (i * 28) + backPos, Varz.megaData1, mapIdx);
            mapIdx += 14;
        }
    }

    public static void draw_background_2(SDL_Surface surface)
    {
        if (map2YDelayMax > 1 && backMove2 < 2)
            backMove2 = (ushort)((map2YDelay == 1) ? 1 : 0);

        if (Config.background2 != false)
        {
            // water effect：bg1/bg2 同步 x 座標
            int x = Config.smoothies[1] ? mapXPos : mapX2Pos;
            int mapIdx = mapY2PosIdx + (Config.smoothies[1] ? mapXbpPos : mapX2bpPos) - 12;

            for (int i = -1; i < 7; i++)
            {
                blit_background_row(surface, x, (i * 28) + backPos2, Varz.megaData2, mapIdx);
                mapIdx += 14;
            }
        }

        if (--map2YDelay == 0)
        {
            map2YDelay = map2YDelayMax;
            backPos2 += backMove2;
            if (backPos2 > 27)
            {
                backPos2 -= 28;
                mapY2--;
                mapY2PosIdx -= 14;
            }
        }
    }

    public static void draw_background_2_blend(SDL_Surface surface)
    {
        if (map2YDelayMax > 1 && backMove2 < 2)
            backMove2 = (ushort)((map2YDelay == 1) ? 1 : 0);

        int mapIdx = mapY2PosIdx + mapX2bpPos - 12;
        for (int i = -1; i < 7; i++)
        {
            blit_background_row_blend(surface, mapX2Pos, (i * 28) + backPos2, Varz.megaData2, mapIdx);
            mapIdx += 14;
        }

        if (--map2YDelay == 0)
        {
            map2YDelay = map2YDelayMax;
            backPos2 += backMove2;
            if (backPos2 > 27)
            {
                backPos2 -= 28;
                mapY2--;
                mapY2PosIdx -= 14;
            }
        }
    }

    public static void draw_background_3(SDL_Surface surface)
    {
        backPos3 += backMove3;
        if (backPos3 > 27)
        {
            backPos3 -= 28;
            mapY3--;
            mapY3PosIdx -= 15;
        }

        int mapIdx = mapY3PosIdx + mapX3bpPos - 12;
        for (int i = -1; i < 7; i++)
        {
            blit_background_row(surface, mapX3Pos, (i * 28) + backPos3, Varz.megaData3, mapIdx);
            mapIdx += 15;
        }
    }
}
