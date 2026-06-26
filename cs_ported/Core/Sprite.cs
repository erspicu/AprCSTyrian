namespace AprCSTyrian.Core;

/// <summary>對應 sprite.h 的 Sprite（單一壓縮 sprite）。data 為非託管記憶體。</summary>
internal unsafe struct Sprite
{
    public ushort width, height;
    public ushort size;
    public byte* data;
}

/// <summary>對應 sprite.h 的 Sprite_array（一個 shape table）。</summary>
internal sealed class Sprite_array
{
    public uint count;
    public Sprite[] sprite = new Sprite[Sprites.SPRITES_PER_TABLE_MAX];
}

/// <summary>對應 sprite.h 的 Sprite2_array（newsh*.shp 等連續壓縮 sprite sheet）。</summary>
internal unsafe struct Sprite2_array
{
    public nuint size;
    public byte* data;
}

/// <summary>
/// 移植 sources/src/sprite.c —— shape table 載入與各種 blit（透明/混色/變色/變暗）。
/// 沿用原始指標式逐位元組 RLE 解碼。LE 平台上 SDL_SwapLE16 為 identity。
/// </summary>
internal static unsafe class Sprites
{
    public const int FONT_SHAPES = 0;
    public const int SMALL_FONT_SHAPES = 1;
    public const int TINY_FONT = 2;
    public const int PLANET_SHAPES = 3;
    public const int FACE_SHAPES = 4;
    public const int OPTION_SHAPES = 5; // Also contains help shapes
    public const int WEAPON_SHAPES = 6;
    public const int EXTRA_SHAPES = 7;  // Used for Ending pics

    public const int SPRITE_TABLES_MAX = 8;
    public const int SPRITES_PER_TABLE_MAX = 151;

    public static readonly Sprite_array[] sprite_table = NewTables();

    public static Sprite2_array shopSpriteSheet = default;
    public static Sprite2_array explosionSpriteSheet = default;
    public static Sprite2_array[] enemySpriteSheets = new Sprite2_array[4];
    public static byte[] enemySpriteSheetIds = new byte[4];
    public static Sprite2_array destructSpriteSheet = default;
    public static Sprite2_array spriteSheet8;
    public static Sprite2_array spriteSheet9;
    public static Sprite2_array spriteSheet10;
    public static Sprite2_array spriteSheet11;
    public static Sprite2_array spriteSheet12;

    private static Sprite_array[] NewTables()
    {
        var a = new Sprite_array[SPRITE_TABLES_MAX];
        for (int i = 0; i < a.Length; ++i)
            a[i] = new Sprite_array();
        return a;
    }

    // ref 取代 C 的 Sprite* 回傳
    public static ref Sprite sprite(uint table, uint index) => ref sprite_table[table].sprite[index];

    public static bool sprite_exists(uint table, uint index) => sprite(table, index).data != null;
    public static ushort get_sprite_width(uint table, uint index) => sprite_exists(table, index) ? sprite(table, index).width : (ushort)0;
    public static ushort get_sprite_height(uint table, uint index) => sprite_exists(table, index) ? sprite(table, index).height : (ushort)0;

    private static ushort SwapLE16(ushort v) => v; // LE 平台 identity

    public static void load_sprites_file(uint table, string filename)
    {
        free_sprites(table);
        Stream f = CFile.dir_fopen_die(CFile.data_dir(), filename, "rb");
        load_sprites(table, f);
        CFile.fclose(f);
    }

    public static void load_sprites(uint table, Stream f)
    {
        free_sprites(table);

        ushort temp = CFile.read_u16(f);
        sprite_table[table].count = temp;
        System.Diagnostics.Debug.Assert(sprite_table[table].count <= SPRITES_PER_TABLE_MAX);

        for (uint i = 0; i < sprite_table[table].count; ++i)
        {
            ref Sprite cur_sprite = ref sprite(table, i);

            bool populated = CFile.read_bool(f);
            if (!populated) // sprite is empty
                continue;

            cur_sprite.width = CFile.read_u16(f);
            cur_sprite.height = CFile.read_u16(f);
            cur_sprite.size = CFile.read_u16(f);

            cur_sprite.data = (byte*)CMem.malloc(cur_sprite.size);
            CFile.fread_u8_die(cur_sprite.data, cur_sprite.size, f);
        }
    }

    public static void free_sprites(uint table)
    {
        for (uint i = 0; i < sprite_table[table].count; ++i)
        {
            ref Sprite cur_sprite = ref sprite(table, i);
            cur_sprite.width = 0;
            cur_sprite.height = 0;
            cur_sprite.size = 0;
            CMem.free(cur_sprite.data);
            cur_sprite.data = null;
        }
        sprite_table[table].count = 0;
    }

    // does not clip on left or right edges of surface
    public static void blit_sprite(SDL_Surface surface, int x, int y, uint table, uint index)
    {
        if (index >= sprite_table[table].count || !sprite_exists(table, index))
            return;

        ref Sprite cur_sprite = ref sprite(table, index);

        byte* data = cur_sprite.data;
        byte* data_ul = data + cur_sprite.size;

        uint width = cur_sprite.width;
        uint x_offset = 0;

        byte* pixels = surface.pixels + (y * surface.pitch) + x;
        byte* pixels_ll = surface.pixels;
        byte* pixels_ul = surface.pixels + (surface.h * surface.pitch);

        for (; data < data_ul; ++data)
        {
            switch (*data)
            {
            case 255: data++; pixels += *data; x_offset += *data; break;
            case 254: pixels += width - x_offset; x_offset = width; break;
            case 253: pixels++; x_offset++; break;
            default:
                if (pixels >= pixels_ul) return;
                if (pixels >= pixels_ll) *pixels = *data;
                pixels++; x_offset++;
                break;
            }
            if (x_offset >= width) { pixels += surface.pitch - x_offset; x_offset = 0; }
        }
    }

    public static void blit_sprite_blend(SDL_Surface surface, int x, int y, uint table, uint index)
    {
        if (index >= sprite_table[table].count || !sprite_exists(table, index))
            return;

        ref Sprite cur_sprite = ref sprite(table, index);
        byte* data = cur_sprite.data;
        byte* data_ul = data + cur_sprite.size;
        uint width = cur_sprite.width;
        uint x_offset = 0;
        byte* pixels = surface.pixels + (y * surface.pitch) + x;
        byte* pixels_ll = surface.pixels;
        byte* pixels_ul = surface.pixels + (surface.h * surface.pitch);

        for (; data < data_ul; ++data)
        {
            switch (*data)
            {
            case 255: data++; pixels += *data; x_offset += *data; break;
            case 254: pixels += width - x_offset; x_offset = width; break;
            case 253: pixels++; x_offset++; break;
            default:
                if (pixels >= pixels_ul) return;
                if (pixels >= pixels_ll)
                    *pixels = (byte)((*data & 0xf0) | (((*pixels & 0x0f) + (*data & 0x0f)) / 2));
                pixels++; x_offset++;
                break;
            }
            if (x_offset >= width) { pixels += surface.pitch - x_offset; x_offset = 0; }
        }
    }

    // unsafe: 不檢查 value 是否溢位到 hue
    public static void blit_sprite_hv_unsafe(SDL_Surface surface, int x, int y, uint table, uint index, byte hue, sbyte value)
    {
        if (index >= sprite_table[table].count || !sprite_exists(table, index))
            return;

        hue <<= 4;

        ref Sprite cur_sprite = ref sprite(table, index);
        byte* data = cur_sprite.data;
        byte* data_ul = data + cur_sprite.size;
        uint width = cur_sprite.width;
        uint x_offset = 0;
        byte* pixels = surface.pixels + (y * surface.pitch) + x;
        byte* pixels_ll = surface.pixels;
        byte* pixels_ul = surface.pixels + (surface.h * surface.pitch);

        for (; data < data_ul; ++data)
        {
            switch (*data)
            {
            case 255: data++; pixels += *data; x_offset += *data; break;
            case 254: pixels += width - x_offset; x_offset = width; break;
            case 253: pixels++; x_offset++; break;
            default:
                if (pixels >= pixels_ul) return;
                if (pixels >= pixels_ll) *pixels = (byte)(hue | ((*data & 0x0f) + value));
                pixels++; x_offset++;
                break;
            }
            if (x_offset >= width) { pixels += surface.pitch - x_offset; x_offset = 0; }
        }
    }

    public static void blit_sprite_hv(SDL_Surface surface, int x, int y, uint table, uint index, byte hue, sbyte value)
    {
        if (index >= sprite_table[table].count || !sprite_exists(table, index))
            return;

        hue <<= 4;

        ref Sprite cur_sprite = ref sprite(table, index);
        byte* data = cur_sprite.data;
        byte* data_ul = data + cur_sprite.size;
        uint width = cur_sprite.width;
        uint x_offset = 0;
        byte* pixels = surface.pixels + (y * surface.pitch) + x;
        byte* pixels_ll = surface.pixels;
        byte* pixels_ul = surface.pixels + (surface.h * surface.pitch);

        for (; data < data_ul; ++data)
        {
            switch (*data)
            {
            case 255: data++; pixels += *data; x_offset += *data; break;
            case 254: pixels += width - x_offset; x_offset = width; break;
            case 253: pixels++; x_offset++; break;
            default:
                if (pixels >= pixels_ul) return;
                if (pixels >= pixels_ll)
                {
                    byte temp_value = (byte)((*data & 0x0f) + value);
                    if (temp_value > 0xf)
                        temp_value = (byte)((temp_value >= 0x1f) ? 0x0 : 0xf);
                    *pixels = (byte)(hue | temp_value);
                }
                pixels++; x_offset++;
                break;
            }
            if (x_offset >= width) { pixels += surface.pitch - x_offset; x_offset = 0; }
        }
    }

    public static void blit_sprite_hv_blend(SDL_Surface surface, int x, int y, uint table, uint index, byte hue, sbyte value)
    {
        if (index >= sprite_table[table].count || !sprite_exists(table, index))
            return;

        hue <<= 4;

        ref Sprite cur_sprite = ref sprite(table, index);
        byte* data = cur_sprite.data;
        byte* data_ul = data + cur_sprite.size;
        uint width = cur_sprite.width;
        uint x_offset = 0;
        byte* pixels = surface.pixels + (y * surface.pitch) + x;
        byte* pixels_ll = surface.pixels;
        byte* pixels_ul = surface.pixels + (surface.h * surface.pitch);

        for (; data < data_ul; ++data)
        {
            switch (*data)
            {
            case 255: data++; pixels += *data; x_offset += *data; break;
            case 254: pixels += width - x_offset; x_offset = width; break;
            case 253: pixels++; x_offset++; break;
            default:
                if (pixels >= pixels_ul) return;
                if (pixels >= pixels_ll)
                {
                    byte temp_value = (byte)((*data & 0x0f) + value);
                    if (temp_value > 0xf)
                        temp_value = (byte)((temp_value >= 0x1f) ? 0x0 : 0xf);
                    *pixels = (byte)(hue | (((*pixels & 0x0f) + temp_value) / 2));
                }
                pixels++; x_offset++;
                break;
            }
            if (x_offset >= width) { pixels += surface.pitch - x_offset; x_offset = 0; }
        }
    }

    public static void blit_sprite_dark(SDL_Surface surface, int x, int y, uint table, uint index, bool black)
    {
        if (index >= sprite_table[table].count || !sprite_exists(table, index))
            return;

        ref Sprite cur_sprite = ref sprite(table, index);
        byte* data = cur_sprite.data;
        byte* data_ul = data + cur_sprite.size;
        uint width = cur_sprite.width;
        uint x_offset = 0;
        byte* pixels = surface.pixels + (y * surface.pitch) + x;
        byte* pixels_ll = surface.pixels;
        byte* pixels_ul = surface.pixels + (surface.h * surface.pitch);

        for (; data < data_ul; ++data)
        {
            switch (*data)
            {
            case 255: data++; pixels += *data; x_offset += *data; break;
            case 254: pixels += width - x_offset; x_offset = width; break;
            case 253: pixels++; x_offset++; break;
            default:
                if (pixels >= pixels_ul) return;
                if (pixels >= pixels_ll)
                    *pixels = black ? (byte)0x00 : (byte)((*pixels & 0xf0) | ((*pixels & 0x0f) / 2));
                pixels++; x_offset++;
                break;
            }
            if (x_offset >= width) { pixels += surface.pitch - x_offset; x_offset = 0; }
        }
    }

    public static void JE_loadCompShapes(ref Sprite2_array sprite2s, char s)
    {
        free_sprite2s(ref sprite2s);

        string buffer = $"newsh{char.ToLowerInvariant(s)}.shp";
        Stream f = CFile.dir_fopen_die(CFile.data_dir(), buffer, "rb");

        sprite2s.size = (nuint)CFile.ftell_eof(f);
        JE_loadCompShapesB(ref sprite2s, f);

        CFile.fclose(f);
    }

    public static void JE_loadCompShapesB(ref Sprite2_array sprite2s, Stream f)
    {
        System.Diagnostics.Debug.Assert(sprite2s.data == null);
        sprite2s.data = (byte*)CMem.malloc(sprite2s.size);
        CFile.fread_u8_die(sprite2s.data, sprite2s.size, f);
    }

    public static void free_sprite2s(ref Sprite2_array sprite2s)
    {
        CMem.free(sprite2s.data);
        sprite2s.data = null;
        sprite2s.size = 0;
    }

    public static void blit_sprite2(SDL_Surface surface, int x, int y, Sprite2_array sprite2s, uint index)
    {
        byte* pixels = surface.pixels + (y * surface.pitch) + x;
        byte* pixels_ll = surface.pixels;
        byte* pixels_ul = surface.pixels + (surface.h * surface.pitch);

        byte* data = sprite2s.data + SwapLE16(((ushort*)sprite2s.data)[index - 1]);

        for (; *data != 0x0f; ++data)
        {
            pixels += *data & 0x0f;
            uint count = (uint)((*data & 0xf0) >> 4);

            if (count == 0)
            {
                pixels += Video.VGAScreen.pitch - 12;
            }
            else
            {
                while (count-- != 0)
                {
                    ++data;
                    if (pixels >= pixels_ul) return;
                    if (pixels >= pixels_ll) *pixels = *data;
                    ++pixels;
                }
            }
        }
    }

    public static void blit_sprite2_clip(SDL_Surface surface, int x, int y, Sprite2_array sprite2s, uint index)
    {
        byte* data = sprite2s.data + SwapLE16(((ushort*)sprite2s.data)[index - 1]);

        for (; *data != 0x0f; ++data)
        {
            if (y >= surface.h) return;

            byte skip_count = (byte)(*data & 0x0f);
            byte fill_count = (byte)((*data >> 4) & 0x0f);

            x += skip_count;

            if (fill_count == 0)
            {
                y += 1; x -= 12;
            }
            else if (y >= 0)
            {
                byte* pixel_row = surface.pixels + (y * surface.pitch);
                do
                {
                    ++data;
                    if (x >= 0 && x < surface.pitch) pixel_row[x] = *data;
                    x += 1;
                } while (--fill_count != 0);
            }
            else
            {
                data += fill_count; x += fill_count;
            }
        }
    }

    public static void blit_sprite2_blend(SDL_Surface surface, int x, int y, Sprite2_array sprite2s, uint index)
    {
        byte* pixels = surface.pixels + (y * surface.pitch) + x;
        byte* pixels_ll = surface.pixels;
        byte* pixels_ul = surface.pixels + (surface.h * surface.pitch);
        byte* data = sprite2s.data + SwapLE16(((ushort*)sprite2s.data)[index - 1]);

        for (; *data != 0x0f; ++data)
        {
            pixels += *data & 0x0f;
            uint count = (uint)((*data & 0xf0) >> 4);
            if (count == 0) { pixels += Video.VGAScreen.pitch - 12; }
            else
            {
                while (count-- != 0)
                {
                    ++data;
                    if (pixels >= pixels_ul) return;
                    if (pixels >= pixels_ll)
                        *pixels = (byte)((((*data & 0x0f) + (*pixels & 0x0f)) / 2) | (*data & 0xf0));
                    ++pixels;
                }
            }
        }
    }

    public static void blit_sprite2_darken(SDL_Surface surface, int x, int y, Sprite2_array sprite2s, uint index)
    {
        byte* pixels = surface.pixels + (y * surface.pitch) + x;
        byte* pixels_ll = surface.pixels;
        byte* pixels_ul = surface.pixels + (surface.h * surface.pitch);
        byte* data = sprite2s.data + SwapLE16(((ushort*)sprite2s.data)[index - 1]);

        for (; *data != 0x0f; ++data)
        {
            pixels += *data & 0x0f;
            uint count = (uint)((*data & 0xf0) >> 4);
            if (count == 0) { pixels += Video.VGAScreen.pitch - 12; }
            else
            {
                while (count-- != 0)
                {
                    ++data;
                    if (pixels >= pixels_ul) return;
                    if (pixels >= pixels_ll)
                        *pixels = (byte)(((*pixels & 0x0f) / 2) + (*pixels & 0xf0));
                    ++pixels;
                }
            }
        }
    }

    public static void blit_sprite2_filter(SDL_Surface surface, int x, int y, Sprite2_array sprite2s, uint index, byte filter)
    {
        byte* pixels = surface.pixels + (y * surface.pitch) + x;
        byte* pixels_ll = surface.pixels;
        byte* pixels_ul = surface.pixels + (surface.h * surface.pitch);
        byte* data = sprite2s.data + SwapLE16(((ushort*)sprite2s.data)[index - 1]);

        for (; *data != 0x0f; ++data)
        {
            pixels += *data & 0x0f;
            uint count = (uint)((*data & 0xf0) >> 4);
            if (count == 0) { pixels += Video.VGAScreen.pitch - 12; }
            else
            {
                while (count-- != 0)
                {
                    ++data;
                    if (pixels >= pixels_ul) return;
                    if (pixels >= pixels_ll) *pixels = (byte)(filter | (*data & 0x0f));
                    ++pixels;
                }
            }
        }
    }

    public static void blit_sprite2_filter_clip(SDL_Surface surface, int x, int y, Sprite2_array sprite2s, uint index, byte filter)
    {
        byte* data = sprite2s.data + SwapLE16(((ushort*)sprite2s.data)[index - 1]);

        for (; *data != 0x0f; ++data)
        {
            if (y >= surface.h) return;

            byte skip_count = (byte)(*data & 0x0f);
            byte fill_count = (byte)((*data >> 4) & 0x0f);

            x += skip_count;

            if (fill_count == 0) { y += 1; x -= 12; }
            else if (y >= 0)
            {
                byte* pixel_row = surface.pixels + (y * surface.pitch);
                do
                {
                    ++data;
                    if (x >= 0 && x < surface.pitch) pixel_row[x] = (byte)(filter | (*data & 0x0f));
                    x += 1;
                } while (--fill_count != 0);
            }
            else
            {
                data += fill_count; x += fill_count;
            }
        }
    }

    public static void blit_sprite2x2(SDL_Surface s, int x, int y, Sprite2_array a, uint index)
    {
        blit_sprite2(s, x, y, a, index);
        blit_sprite2(s, x + 12, y, a, index + 1);
        blit_sprite2(s, x, y + 14, a, index + 19);
        blit_sprite2(s, x + 12, y + 14, a, index + 20);
    }

    public static void blit_sprite2x2_clip(SDL_Surface s, int x, int y, Sprite2_array a, uint index)
    {
        blit_sprite2_clip(s, x, y, a, index);
        blit_sprite2_clip(s, x + 12, y, a, index + 1);
        blit_sprite2_clip(s, x, y + 14, a, index + 19);
        blit_sprite2_clip(s, x + 12, y + 14, a, index + 20);
    }

    public static void blit_sprite2x2_blend(SDL_Surface s, int x, int y, Sprite2_array a, uint index)
    {
        blit_sprite2_blend(s, x, y, a, index);
        blit_sprite2_blend(s, x + 12, y, a, index + 1);
        blit_sprite2_blend(s, x, y + 14, a, index + 19);
        blit_sprite2_blend(s, x + 12, y + 14, a, index + 20);
    }

    public static void blit_sprite2x2_darken(SDL_Surface s, int x, int y, Sprite2_array a, uint index)
    {
        blit_sprite2_darken(s, x, y, a, index);
        blit_sprite2_darken(s, x + 12, y, a, index + 1);
        blit_sprite2_darken(s, x, y + 14, a, index + 19);
        blit_sprite2_darken(s, x + 12, y + 14, a, index + 20);
    }

    public static void blit_sprite2x2_filter(SDL_Surface s, int x, int y, Sprite2_array a, uint index, byte filter)
    {
        blit_sprite2_filter(s, x, y, a, index, filter);
        blit_sprite2_filter(s, x + 12, y, a, index + 1, filter);
        blit_sprite2_filter(s, x, y + 14, a, index + 19, filter);
        blit_sprite2_filter(s, x + 12, y + 14, a, index + 20, filter);
    }

    public static void blit_sprite2x2_filter_clip(SDL_Surface s, int x, int y, Sprite2_array a, uint index, byte filter)
    {
        blit_sprite2_filter_clip(s, x, y, a, index, filter);
        blit_sprite2_filter_clip(s, x + 12, y, a, index + 1, filter);
        blit_sprite2_filter_clip(s, x, y + 14, a, index + 19, filter);
        blit_sprite2_filter_clip(s, x + 12, y + 14, a, index + 20, filter);
    }

    public static void JE_loadMainShapeTables(string shpfile)
    {
        const int SHP_NUM = 12;

        Stream f = CFile.dir_fopen_die(CFile.data_dir(), shpfile, "rb");

        ushort shpNumb = CFile.read_u16(f);
        int[] shpPos = new int[SHP_NUM + 1]; // +1 for storing file length
        System.Diagnostics.Debug.Assert(shpNumb + 1 == shpPos.Length);

        fixed (int* pp = shpPos)
            CFile.fread_s32_die(pp, shpNumb, f);

        f.Seek(0, SeekOrigin.End);
        for (uint i = shpNumb; i < shpPos.Length; ++i)
            shpPos[i] = (int)f.Position;

        int j;
        // fonts, interface, option sprites
        for (j = 0; j < 7; j++)
        {
            f.Seek(shpPos[j], SeekOrigin.Begin);
            load_sprites((uint)j, f);
        }

        // player shot sprites
        spriteSheet8.size = (nuint)(shpPos[j + 1] - shpPos[j]);
        JE_loadCompShapesB(ref spriteSheet8, f); j++;
        // player ship sprites
        spriteSheet9.size = (nuint)(shpPos[j + 1] - shpPos[j]);
        JE_loadCompShapesB(ref spriteSheet9, f); j++;
        // power-up sprites
        spriteSheet10.size = (nuint)(shpPos[j + 1] - shpPos[j]);
        JE_loadCompShapesB(ref spriteSheet10, f); j++;
        // coins, datacubes, etc sprites
        spriteSheet11.size = (nuint)(shpPos[j + 1] - shpPos[j]);
        JE_loadCompShapesB(ref spriteSheet11, f); j++;
        // more player shot sprites
        spriteSheet12.size = (nuint)(shpPos[j + 1] - shpPos[j]);
        JE_loadCompShapesB(ref spriteSheet12, f);

        CFile.fclose(f);
    }

    public static void free_main_shape_tables()
    {
        for (uint i = 0; i < sprite_table.Length; ++i)
            free_sprites(i);

        free_sprite2s(ref spriteSheet8);
        free_sprite2s(ref spriteSheet9);
        free_sprite2s(ref spriteSheet10);
        free_sprite2s(ref spriteSheet11);
        free_sprite2s(ref spriteSheet12);
    }
}
