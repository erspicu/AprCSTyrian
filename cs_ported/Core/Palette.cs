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
