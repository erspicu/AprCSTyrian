namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/fonthand.c —— CP437→font sprite 對照表與各式文字輸出。
/// </summary>
internal static class Fonthand
{
    public const int PART_SHADE = 0;
    public const int FULL_SHADE = 1;
    public const int DARKEN = 2;
    public const int TRICK = 3;
    public const int NO_SHADE = 255;

    // Mapping from CP437 to font sprite index.
    public static readonly sbyte[] fontMap = /* [33..168] */
    {
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, 26, 33, 60, 61, 62, -1, 32, 64, 65, 63, 84, 29, 83, 28, 80, //  !"#$%&'()*+,-./
        79, 70, 71, 72, 73, 74, 75, 76, 77, 78, 31, 30, -1, 85, -1, 27, // 0123456789:;<=>?
        -1,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, // @ABCDEFGHIJKLMNO
        15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 68, 82, 69, -1, -1, // PQRSTUVWXYZ[\]^_
        -1, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, // `abcdefghijklmno
        49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 66, 81, 67, -1, -1, // pqrstuvwxyz{|}~
        86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99,100,101,
       102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,
       118,119,120,121,122,123,124,125,126, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
    };

    // 這些是跨模組遊戲全域，將由尚未移植的遊戲邏輯指派；暫時僅用預設值。
#pragma warning disable CS0649
    public static byte textGlowFont;
    public static byte textGlowBrightness = 6;

    public static bool levelWarningDisplay;
    public static byte levelWarningLines;
    public static readonly byte[][] levelWarningText = NewWarnText(); // [10][61]
    public static bool warningRed;

    public static byte warningSoundDelay;
    public static ushort armorShipDelay;
    public static byte warningCol;
    public static sbyte warningColChange;
#pragma warning restore CS0649

    private static byte[][] NewWarnText()
    {
        var a = new byte[10][];
        for (int i = 0; i < 10; ++i) a[i] = new byte[61];
        return a;
    }

    public static void JE_dString(SDL_Surface screen, int x, int y, string s, uint font)
    {
        const int defaultBrightness = -3;
        int bright = 0;

        for (int i = 0; i < s.Length; ++i)
        {
            char ch = s[i];
            int sprite_id = fontMap[(byte)ch];

            switch (ch)
            {
            case ' ':
                x += 6;
                break;
            case '~':
                bright = (bright == 0) ? 2 : 0;
                break;
            default:
                if (sprite_id != -1)
                {
                    Sprites.blit_sprite_dark(screen, x + 2, y + 2, font, (uint)sprite_id, false);
                    Sprites.blit_sprite_hv_unsafe(screen, x, y, font, (uint)sprite_id, 0xf, (sbyte)(defaultBrightness + bright));
                    x += Sprites.sprite(font, (uint)sprite_id).width + 1;
                }
                break;
            }
        }
    }

    public static int JE_fontCenter(string s, uint font) => 160 - (JE_textWidth(s, font) / 2);

    public static int JE_textWidth(string s, uint font)
    {
        int x = 0;
        for (int i = 0; i < s.Length; ++i)
        {
            char ch = s[i];
            int sprite_id = fontMap[(byte)ch];
            if (ch == ' ')
                x += 6;
            else if (sprite_id != -1)
                x += Sprites.sprite(font, (uint)sprite_id).width + 1;
        }
        return x;
    }

    public static void JE_textShade(SDL_Surface screen, int x, int y, string s, uint colorbank, int brightness, uint shadetype)
    {
        switch (shadetype)
        {
        case PART_SHADE:
            JE_outText(screen, x + 1, y + 1, s, 0, -1);
            JE_outText(screen, x, y, s, colorbank, brightness);
            break;
        case FULL_SHADE:
            JE_outText(screen, x - 1, y, s, 0, -1);
            JE_outText(screen, x + 1, y, s, 0, -1);
            JE_outText(screen, x, y - 1, s, 0, -1);
            JE_outText(screen, x, y + 1, s, 0, -1);
            JE_outText(screen, x, y, s, colorbank, brightness);
            break;
        case DARKEN:
            JE_outTextAndDarken(screen, x + 1, y + 1, s, colorbank, brightness < 0 ? 0u : (uint)brightness, Sprites.TINY_FONT);
            break;
        case TRICK:
            JE_outTextModify(screen, x, y, s, colorbank, brightness < 0 ? 0u : (uint)brightness, Sprites.TINY_FONT);
            break;
        }
    }

    public static void JE_outText(SDL_Surface screen, int x, int y, string s, uint colorbank, int brightness)
    {
        int bright = 0;

        for (int i = 0; i < s.Length; ++i)
        {
            char ch = s[i];
            int sprite_id = fontMap[(byte)ch];

            switch (ch)
            {
            case ' ':
                x += 6;
                break;
            case '~':
                bright = (bright == 0) ? 4 : 0;
                break;
            default:
                if (sprite_id != -1 && Sprites.sprite_exists(Sprites.TINY_FONT, (uint)sprite_id))
                {
                    if (brightness >= 0)
                        Sprites.blit_sprite_hv_unsafe(screen, x, y, Sprites.TINY_FONT, (uint)sprite_id, (byte)colorbank, (sbyte)(brightness + bright));
                    else
                        Sprites.blit_sprite_dark(screen, x, y, Sprites.TINY_FONT, (uint)sprite_id, true);
                    x += Sprites.sprite(Sprites.TINY_FONT, (uint)sprite_id).width + 1;
                }
                break;
            }
        }
    }

    public static void JE_outTextModify(SDL_Surface screen, int x, int y, string s, uint filter, uint brightness, uint font)
    {
        for (int i = 0; i < s.Length; ++i)
        {
            char ch = s[i];
            int sprite_id = fontMap[(byte)ch];
            if (ch == ' ')
                x += 6;
            else if (sprite_id != -1)
            {
                Sprites.blit_sprite_hv_blend(screen, x, y, font, (uint)sprite_id, (byte)filter, (sbyte)brightness);
                x += Sprites.sprite(font, (uint)sprite_id).width + 1;
            }
        }
    }

    public static void JE_outTextAdjust(SDL_Surface screen, int x, int y, string s, uint filter, int brightness, uint font, bool shadow)
    {
        int bright = 0;

        for (int i = 0; i < s.Length; ++i)
        {
            char ch = s[i];
            int sprite_id = fontMap[(byte)ch];

            switch (ch)
            {
            case ' ':
                x += 6;
                break;
            case '~':
                bright = (bright == 0) ? 4 : 0;
                break;
            default:
                if (sprite_id != -1 && Sprites.sprite_exists(Sprites.TINY_FONT, (uint)sprite_id))
                {
                    if (shadow)
                        Sprites.blit_sprite_dark(screen, x + 2, y + 2, font, (uint)sprite_id, false);
                    Sprites.blit_sprite_hv(screen, x, y, font, (uint)sprite_id, (byte)filter, (sbyte)(brightness + bright));
                    x += Sprites.sprite(font, (uint)sprite_id).width + 1;
                }
                break;
            }
        }
    }

    public static void JE_outTextAndDarken(SDL_Surface screen, int x, int y, string s, uint colorbank, uint brightness, uint font)
    {
        int bright = 0;

        for (int i = 0; i < s.Length; ++i)
        {
            char ch = s[i];
            int sprite_id = fontMap[(byte)ch];

            switch (ch)
            {
            case ' ':
                x += 6;
                break;
            case '~':
                bright = (bright == 0) ? 4 : 0;
                break;
            default:
                if (sprite_id != -1 && Sprites.sprite_exists(Sprites.TINY_FONT, (uint)sprite_id))
                {
                    Sprites.blit_sprite_dark(screen, x + 1, y + 1, font, (uint)sprite_id, false);
                    Sprites.blit_sprite_hv_unsafe(screen, x, y, font, (uint)sprite_id, (byte)colorbank, (sbyte)(brightness + bright));
                    x += Sprites.sprite(font, (uint)sprite_id).width + 1;
                }
                break;
            }
        }
    }

    public static void JE_updateWarning(SDL_Surface screen)
    {
        if (Nortsong.getFrameCount2Ticks() == 0)
        {
            // Update Color Bars
            warningCol = (byte)(warningCol + warningColChange);
            if (warningCol > 14 * 16 + 10 || warningCol < 14 * 16 + 4)
                warningColChange = (sbyte)-warningColChange;

            Vga256d.fill_rectangle_xy(screen, 0, 0, 319, 5, warningCol);
            Vga256d.fill_rectangle_xy(screen, 0, 194, 319, 199, warningCol);
            Video.JE_showVGA();

            Nortsong.setFrameCount2(6);

            if (warningSoundDelay > 0)
            {
                warningSoundDelay--;
            }
            else
            {
                warningSoundDelay = 14;
                Nortsong.JE_playSampleNum(Sndmast.S_WARNING);
            }
        }
    }

    public static void JE_outTextGlow(SDL_Surface screen, int x, int y, string s)
    {
        byte c = 15;
        if (warningRed)
            c = 7;

        JE_outTextAdjust(screen, x - 1, y, s, 0, -12, textGlowFont, false);
        JE_outTextAdjust(screen, x, y - 1, s, 0, -12, textGlowFont, false);
        JE_outTextAdjust(screen, x + 1, y, s, 0, -12, textGlowFont, false);
        JE_outTextAdjust(screen, x, y + 1, s, 0, -12, textGlowFont, false);

        if (Nortsong.frameCountMax > 0)
        {
            for (int z = 1; z <= 12; z++)
            {
                Nortsong.setFrameCount(Nortsong.frameCountMax);
                JE_outTextAdjust(screen, x, y, s, c, z - 10, textGlowFont, false);
                Video.JE_showVGA();
                if (Keyboard.waitUntilGetInputOrElapsed())
                    Nortsong.frameCountMax = 0;
            }
        }

        for (int z = (Nortsong.frameCountMax == 0) ? 6 : 12; z >= textGlowBrightness; z--)
        {
            Nortsong.setFrameCount(Nortsong.frameCountMax);
            JE_outTextAdjust(screen, x, y, s, c, z - 10, textGlowFont, false);
            Video.JE_showVGA();
            if (Keyboard.waitUntilGetInputOrElapsed())
                Nortsong.frameCountMax = 0;
        }

        textGlowBrightness = 6;
    }
}
