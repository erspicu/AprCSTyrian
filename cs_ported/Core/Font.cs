namespace AprCSTyrian.Core;

internal enum Font
{
    FONT_LARGE = 0,
    FONT_NORMAL = 1,
    FONT_SMALL = 2,
}

internal enum FontAlignment
{
    ALIGN_LEFT,
    ALIGN_CENTER,
    ALIGN_RIGHT,
}

/// <summary>
/// 移植 sources/src/font.c —— 文字繪製（含陰影/對齊/混色/變暗）。
/// '~' 不繪製，而是切換高亮（value += 4）。
/// </summary>
internal static class FontDraw
{
    public static void drawFontHvShadow(SDL_Surface surface, int x, int y, string text, Font font, byte hue, sbyte value, bool black, int shadowDist)
    {
        drawFontDark(surface, x + shadowDist, y + shadowDist, text, font, black);
        drawFontHv(surface, x, y, text, font, hue, value);
    }

    public static void drawFontHvShadowAligned(SDL_Surface surface, int x, int y, string text, Font font, FontAlignment alignment, byte hue, sbyte value, bool black, int shadowDist)
    {
        x = AlignX(x, text, font, alignment);
        drawFontHvShadow(surface, x, y, text, font, hue, value, black, shadowDist);
    }

    public static void drawFontHvFullShadow(SDL_Surface surface, int x, int y, string text, Font font, byte hue, sbyte value, bool black, int shadowDist)
    {
        drawFontDark(surface, x, y - shadowDist, text, font, black);
        drawFontDark(surface, x + shadowDist, y, text, font, black);
        drawFontDark(surface, x, y + shadowDist, text, font, black);
        drawFontDark(surface, x - shadowDist, y, text, font, black);
        drawFontHv(surface, x, y, text, font, hue, value);
    }

    public static void drawFontHvFullShadowAligned(SDL_Surface surface, int x, int y, string text, Font font, FontAlignment alignment, byte hue, sbyte value, bool black, int shadowDist)
    {
        x = AlignX(x, text, font, alignment);
        drawFontHvFullShadow(surface, x, y, text, font, hue, value, black, shadowDist);
    }

    public static void drawFontHv(SDL_Surface surface, int x, int y, string text, Font font, byte hue, sbyte value)
    {
        bool highlight = false;

        for (int i = 0; i < text.Length; ++i)
        {
            char ch = text[i];
            int sprite_id = Fonthand.fontMap[(byte)ch];

            switch (ch)
            {
            case ' ':
                x += 6;
                break;
            case '~':
                highlight = !highlight;
                value = (sbyte)(value + (highlight ? 4 : -4));
                break;
            default:
                if (sprite_id != -1 && Sprites.sprite_exists((uint)font, (uint)sprite_id))
                {
                    Sprites.blit_sprite_hv(surface, x, y, (uint)font, (uint)sprite_id, hue, value);
                    x += Sprites.sprite((uint)font, (uint)sprite_id).width + 1;
                }
                break;
            }
        }
    }

    public static void drawFontHvAligned(SDL_Surface surface, int x, int y, string text, Font font, FontAlignment alignment, byte hue, sbyte value)
    {
        x = AlignX(x, text, font, alignment);
        drawFontHv(surface, x, y, text, font, hue, value);
    }

    public static void drawFontHvBlend(SDL_Surface surface, int x, int y, string text, Font font, byte hue, sbyte value)
    {
        for (int i = 0; i < text.Length; ++i)
        {
            char ch = text[i];
            int sprite_id = Fonthand.fontMap[(byte)ch];

            switch (ch)
            {
            case ' ':
                x += 6;
                break;
            case '~':
                break;
            default:
                if (sprite_id != -1 && Sprites.sprite_exists((uint)font, (uint)sprite_id))
                {
                    Sprites.blit_sprite_hv_blend(surface, x, y, (uint)font, (uint)sprite_id, hue, value);
                    x += Sprites.sprite((uint)font, (uint)sprite_id).width + 1;
                }
                break;
            }
        }
    }

    public static void drawFontHvBlendAligned(SDL_Surface surface, int x, int y, string text, Font font, FontAlignment alignment, byte hue, sbyte value)
    {
        x = AlignX(x, text, font, alignment);
        drawFontHvBlend(surface, x, y, text, font, hue, value);
    }

    public static void drawFontDark(SDL_Surface surface, int x, int y, string text, Font font, bool black)
    {
        for (int i = 0; i < text.Length; ++i)
        {
            char ch = text[i];
            int sprite_id = Fonthand.fontMap[(byte)ch];

            switch (ch)
            {
            case ' ':
                x += 6;
                break;
            case '~':
                break;
            default:
                if (sprite_id != -1 && Sprites.sprite_exists((uint)font, (uint)sprite_id))
                {
                    Sprites.blit_sprite_dark(surface, x, y, (uint)font, (uint)sprite_id, black);
                    x += Sprites.sprite((uint)font, (uint)sprite_id).width + 1;
                }
                break;
            }
        }
    }

    public static void drawFontDarkAligned(SDL_Surface surface, int x, int y, string text, Font font, FontAlignment alignment, bool black)
    {
        x = AlignX(x, text, font, alignment);
        drawFontDark(surface, x, y, text, font, black);
    }

    private static int AlignX(int x, string text, Font font, FontAlignment alignment) => alignment switch
    {
        FontAlignment.ALIGN_CENTER => x - Fonthand.JE_textWidth(text, (uint)font) / 2,
        FontAlignment.ALIGN_RIGHT => x - Fonthand.JE_textWidth(text, (uint)font),
        _ => x,
    };
}
