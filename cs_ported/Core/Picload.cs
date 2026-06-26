namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/picload.c —— 從 tyrian.pic 載入 RLE 壓縮的整頁圖到指定 surface。
/// </summary>
internal static unsafe class Picload
{
    private static bool first = true;

    public static void JE_loadPic(SDL_Surface screen, byte PCXnumber, bool storepal)
    {
        PCXnumber--;

        Stream f = CFile.dir_fopen_die(CFile.data_dir(), "tyrian.pic", "rb");

        if (first)
        {
            first = false;

            ushort temp = CFile.read_u16(f);
            _ = temp;

            fixed (int* pp = Pcxmast.pcxpos)
                CFile.fread_s32_die(pp, Pcxmast.PCX_NUM, f);
            Pcxmast.pcxpos[Pcxmast.PCX_NUM] = (int)CFile.ftell_eof(f);
        }

        uint size = (uint)(Pcxmast.pcxpos[PCXnumber + 1] - Pcxmast.pcxpos[PCXnumber]);
        byte* buffer = (byte*)CMem.malloc(size);

        f.Seek(Pcxmast.pcxpos[PCXnumber], SeekOrigin.Begin);
        CFile.fread_u8_die(buffer, size, f);
        CFile.fclose(f);

        byte* p = buffer;
        byte* s = screen.pixels; /* screen pointer, 8-bit specific */

        for (int i = 0; i < 320 * 200; )
        {
            if ((*p & 0xc0) == 0xc0)
            {
                i += (*p & 0x3f);
                new Span<byte>(s, *p & 0x3f).Fill(*(p + 1));
                s += (*p & 0x3f); p += 2;
            }
            else
            {
                i++;
                *s = *p;
                s++; p++;
            }
            if (i != 0 && (i % 320 == 0))
            {
                s += screen.pitch - 320;
            }
        }

        CMem.free(buffer);

        // memcpy(colors, palettes[pcxpal[PCXnumber]], sizeof(colors));
        Array.Copy(Palette.palettes[Pcxmast.pcxpal[PCXnumber]], Palette.colors, 256);

        if (storepal)
            Palette.set_palette(Palette.colors, 0, 255);
    }
}
