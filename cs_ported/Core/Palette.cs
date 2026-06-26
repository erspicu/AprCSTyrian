using AprCSTyrian.Core.Ports;

namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/palette.c —— 調色盤載入、設定與淡入淡出。
/// 原 set_* 透過 SDL_MapRGB 計算 rgb_palette；此處改以自有 MapRGB，並在調色盤變更後
/// 將目前調色盤上傳到 <see cref="Globals.Video"/>。yuv_palette 僅供 hqNx 縮放器，暫不移植。
/// </summary>
internal static class Palette
{
    public const int PALETTE_COUNT = 23;

    // 23 組調色盤，每組 256 色（對應 Palette palettes[PALETTE_COUNT]）。
    public static readonly SDL_Color[][] palettes = NewPaletteArray(PALETTE_COUNT);
    public static int palette_count;

    private static readonly SDL_Color[] palette = new SDL_Color[256]; // 目前生效調色盤
    public static readonly uint[] rgb_palette = new uint[256];

    public static readonly SDL_Color[] colors = new SDL_Color[256];   // 共用暫存調色盤

    /// <summary>
    /// 對應 vga_palette.c:vga_palette（256 色 DOS VGA 預設調色盤，僅 jukebox 用）。
    /// 值為 8-bit（0..252），逐項照抄 C 原值，未做 6-bit&lt;&lt;2 轉換。
    /// </summary>
    public static readonly SDL_Color[] vga_palette =
    {
        new(  0,   0,   0), new(  0,   0, 168), new(  0, 168,   0), new(  0, 168, 168),
        new(168,   0,   0), new(168,   0, 168), new(168,  84,   0), new(168, 168, 168),
        new( 84,  84,  84), new( 84,  84, 252), new( 84, 252,  84), new( 84, 252, 252),
        new(252,  84,  84), new(252,  84, 252), new(252, 252,  84), new(252, 252, 252),
        new(  0,   0,   0), new( 20,  20,  20), new( 32,  32,  32), new( 44,  44,  44),
        new( 56,  56,  56), new( 68,  68,  68), new( 80,  80,  80), new( 96,  96,  96),
        new(112, 112, 112), new(128, 128, 128), new(144, 144, 144), new(160, 160, 160),
        new(180, 180, 180), new(200, 200, 200), new(224, 224, 224), new(252, 252, 252),
        new(  0,   0, 252), new( 64,   0, 252), new(124,   0, 252), new(188,   0, 252),
        new(252,   0, 252), new(252,   0, 188), new(252,   0, 124), new(252,   0,  64),
        new(252,   0,   0), new(252,  64,   0), new(252, 124,   0), new(252, 188,   0),
        new(252, 252,   0), new(188, 252,   0), new(124, 252,   0), new( 64, 252,   0),
        new(  0, 252,   0), new(  0, 252,  64), new(  0, 252, 124), new(  0, 252, 188),
        new(  0, 252, 252), new(  0, 188, 252), new(  0, 124, 252), new(  0,  64, 252),
        new(124, 124, 252), new(156, 124, 252), new(188, 124, 252), new(220, 124, 252),
        new(252, 124, 252), new(252, 124, 220), new(252, 124, 188), new(252, 124, 156),
        new(252, 124, 124), new(252, 156, 124), new(252, 188, 124), new(252, 220, 124),
        new(252, 252, 124), new(220, 252, 124), new(188, 252, 124), new(156, 252, 124),
        new(124, 252, 124), new(124, 252, 156), new(124, 252, 188), new(124, 252, 220),
        new(124, 252, 252), new(124, 220, 252), new(124, 188, 252), new(124, 156, 252),
        new(180, 180, 252), new(196, 180, 252), new(216, 180, 252), new(232, 180, 252),
        new(252, 180, 252), new(252, 180, 232), new(252, 180, 216), new(252, 180, 196),
        new(252, 180, 180), new(252, 196, 180), new(252, 216, 180), new(252, 232, 180),
        new(252, 252, 180), new(232, 252, 180), new(216, 252, 180), new(196, 252, 180),
        new(180, 252, 180), new(180, 252, 196), new(180, 252, 216), new(180, 252, 232),
        new(180, 252, 252), new(180, 232, 252), new(180, 216, 252), new(180, 196, 252),
        new(  0,   0, 112), new( 28,   0, 112), new( 56,   0, 112), new( 84,   0, 112),
        new(112,   0, 112), new(112,   0,  84), new(112,   0,  56), new(112,   0,  28),
        new(112,   0,   0), new(112,  28,   0), new(112,  56,   0), new(112,  84,   0),
        new(112, 112,   0), new( 84, 112,   0), new( 56, 112,   0), new( 28, 112,   0),
        new(  0, 112,   0), new(  0, 112,  28), new(  0, 112,  56), new(  0, 112,  84),
        new(  0, 112, 112), new(  0,  84, 112), new(  0,  56, 112), new(  0,  28, 112),
        new( 56,  56, 112), new( 68,  56, 112), new( 84,  56, 112), new( 96,  56, 112),
        new(112,  56, 112), new(112,  56,  96), new(112,  56,  84), new(112,  56,  68),
        new(112,  56,  56), new(112,  68,  56), new(112,  84,  56), new(112,  96,  56),
        new(112, 112,  56), new( 96, 112,  56), new( 84, 112,  56), new( 68, 112,  56),
        new( 56, 112,  56), new( 56, 112,  68), new( 56, 112,  84), new( 56, 112,  96),
        new( 56, 112, 112), new( 56,  96, 112), new( 56,  84, 112), new( 56,  68, 112),
        new( 80,  80, 112), new( 88,  80, 112), new( 96,  80, 112), new(104,  80, 112),
        new(112,  80, 112), new(112,  80, 104), new(112,  80,  96), new(112,  80,  88),
        new(112,  80,  80), new(112,  88,  80), new(112,  96,  80), new(112, 104,  80),
        new(112, 112,  80), new(104, 112,  80), new( 96, 112,  80), new( 88, 112,  80),
        new( 80, 112,  80), new( 80, 112,  88), new( 80, 112,  96), new( 80, 112, 104),
        new( 80, 112, 112), new( 80, 104, 112), new( 80,  96, 112), new( 80,  88, 112),
        new(  0,   0,  64), new( 16,   0,  64), new( 32,   0,  64), new( 48,   0,  64),
        new( 64,   0,  64), new( 64,   0,  48), new( 64,   0,  32), new( 64,   0,  16),
        new( 64,   0,   0), new( 64,  16,   0), new( 64,  32,   0), new( 64,  48,   0),
        new( 64,  64,   0), new( 48,  64,   0), new( 32,  64,   0), new( 16,  64,   0),
        new(  0,  64,   0), new(  0,  64,  16), new(  0,  64,  32), new(  0,  64,  48),
        new(  0,  64,  64), new(  0,  48,  64), new(  0,  32,  64), new(  0,  16,  64),
        new( 32,  32,  64), new( 40,  32,  64), new( 48,  32,  64), new( 56,  32,  64),
        new( 64,  32,  64), new( 64,  32,  56), new( 64,  32,  48), new( 64,  32,  40),
        new( 64,  32,  32), new( 64,  40,  32), new( 64,  48,  32), new( 64,  56,  32),
        new( 64,  64,  32), new( 56,  64,  32), new( 48,  64,  32), new( 40,  64,  32),
        new( 32,  64,  32), new( 32,  64,  40), new( 32,  64,  48), new( 32,  64,  56),
        new( 32,  64,  64), new( 32,  56,  64), new( 32,  48,  64), new( 32,  40,  64),
        new( 44,  44,  64), new( 48,  44,  64), new( 52,  44,  64), new( 60,  44,  64),
        new( 64,  44,  64), new( 64,  44,  60), new( 64,  44,  52), new( 64,  44,  48),
        new( 64,  44,  44), new( 64,  48,  44), new( 64,  52,  44), new( 64,  60,  44),
        new( 64,  64,  44), new( 60,  64,  44), new( 52,  64,  44), new( 48,  64,  44),
        new( 44,  64,  44), new( 44,  64,  48), new( 44,  64,  52), new( 44,  64,  60),
        new( 44,  64,  64), new( 44,  60,  64), new( 44,  52,  64), new( 44,  48,  64),
        new(  0,   0,   0), new(  0,   0,   0), new(  0,   0,   0), new(  0,   0,   0),
        new(  0,   0,   0), new(  0,   0,   0), new(  0,   0,   0), new(  0,   0,   0),
    };

    // 上傳到平台用的 RGB 緩衝（避免每次 new）。
    private static readonly Color[] _rt = new Color[256];

    private static SDL_Color[][] NewPaletteArray(int n)
    {
        var a = new SDL_Color[n][];
        for (int i = 0; i < n; ++i)
            a[i] = new SDL_Color[256];
        return a;
    }

    public static void JE_loadPals()
    {
        Stream f = CFile.dir_fopen_die(CFile.data_dir(), "palette.dat", "rb");

        palette_count = (int)(CFile.ftell_eof(f) / (256 * 3));
        System.Diagnostics.Debug.Assert(palette_count == PALETTE_COUNT);

        for (int p = 0; p < palette_count; ++p)
        {
            for (int i = 0; i < 256; ++i)
            {
                // VGA 硬體調色盤每分量僅 6-bit，需放大到 8-bit。用原值的高 2 bit 補低位，
                // 使 63 對應到 255。
                byte r = CFile.read_u8(f);
                byte g = CFile.read_u8(f);
                byte b = CFile.read_u8(f);
                palettes[p][i].r = (byte)((r << 2) | (r >> 4));
                palettes[p][i].g = (byte)((g << 2) | (g >> 4));
                palettes[p][i].b = (byte)((b << 2) | (b >> 4));
            }
        }

        CFile.fclose(f);
    }

    private static uint MapRGB(byte r, byte g, byte b) =>
        0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;

    private static void UploadPalette()
    {
        for (int i = 0; i < 256; ++i)
            _rt[i] = new Color(palette[i].r, palette[i].g, palette[i].b);
        Globals.Video.SetPalette(_rt);
    }

    public static void set_palette(SDL_Color[] colors, uint first_color, uint last_color)
    {
        for (uint i = first_color; i <= last_color; ++i)
        {
            palette[i] = colors[i];
            rgb_palette[i] = MapRGB(palette[i].r, palette[i].g, palette[i].b);
        }
        UploadPalette();
    }

    public static void set_colors(SDL_Color color, uint first_color, uint last_color)
    {
        for (uint i = first_color; i <= last_color; ++i)
        {
            palette[i] = color;
            rgb_palette[i] = MapRGB(palette[i].r, palette[i].g, palette[i].b);
        }
        UploadPalette();
    }

    public static void init_step_fade_palette(int[,] diff, SDL_Color[] colors, uint first_color, uint last_color)
    {
        for (uint i = first_color; i <= last_color; i++)
        {
            diff[i, 0] = colors[i].r - palette[i].r;
            diff[i, 1] = colors[i].g - palette[i].g;
            diff[i, 2] = colors[i].b - palette[i].b;
        }
    }

    public static void init_step_fade_solid(int[,] diff, SDL_Color color, uint first_color, uint last_color)
    {
        for (uint i = first_color; i <= last_color; i++)
        {
            diff[i, 0] = color.r - palette[i].r;
            diff[i, 1] = color.g - palette[i].g;
            diff[i, 2] = color.b - palette[i].b;
        }
    }

    public static void step_fade_palette(int[,] diff, int steps, uint first_color, uint last_color)
    {
        System.Diagnostics.Debug.Assert(steps > 0);

        for (uint i = first_color; i <= last_color; i++)
        {
            int d0 = diff[i, 0] / steps, d1 = diff[i, 1] / steps, d2 = diff[i, 2] / steps;

            diff[i, 0] -= d0;
            diff[i, 1] -= d1;
            diff[i, 2] -= d2;

            palette[i].r = (byte)(palette[i].r + d0);
            palette[i].g = (byte)(palette[i].g + d1);
            palette[i].b = (byte)(palette[i].b + d2);

            rgb_palette[i] = MapRGB(palette[i].r, palette[i].g, palette[i].b);
        }
        UploadPalette();
    }

    private static readonly int[,] _fadeDiff = new int[256, 3];

    public static void fade_palette(SDL_Color[] colors, int steps, uint first_color, uint last_color)
    {
        System.Diagnostics.Debug.Assert(steps > 0);

        init_step_fade_palette(_fadeDiff, colors, first_color, last_color);

        for (; steps > 0; steps--)
        {
            Nortsong.setFrameCount(1);
            step_fade_palette(_fadeDiff, steps, first_color, last_color);
            Video.JE_showVGA();
            Keyboard.waitUntilElapsed();
        }

        // Discard input during fade.
        Keyboard.keyboardClearInput();
        Mouse.mouseClearInput();
    }

    public static void fade_solid(SDL_Color color, int steps, uint first_color, uint last_color)
    {
        System.Diagnostics.Debug.Assert(steps > 0);

        init_step_fade_solid(_fadeDiff, color, first_color, last_color);

        for (; steps > 0; steps--)
        {
            Nortsong.setFrameCount(1);
            step_fade_palette(_fadeDiff, steps, first_color, last_color);
            Video.JE_showVGA();
            Keyboard.waitUntilElapsed();
        }

        Keyboard.keyboardClearInput();
        Mouse.mouseClearInput();
    }

    public static void fade_black(int steps)
    {
        SDL_Color black = new(0, 0, 0);
        fade_solid(black, steps, 0, 255);
    }

    public static void fade_white(int steps)
    {
        SDL_Color white = new(255, 255, 255);
        fade_solid(white, steps, 0, 255);
    }
}
