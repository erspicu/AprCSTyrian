namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/pcxload.c —— RLE PCX 載入（僅用於 tshp2.pcx，整頁 320×200 → VGAScreen）。
/// </summary>
internal static unsafe class Pcxload
{
    public static void JE_loadPCX(string file) // this is only meant to load tshp2.pcx
    {
        byte* s = Video.VGAScreen.pixels; /* 8-bit specific */

        Stream f = CFile.dir_fopen_die(CFile.data_dir(), file, "rb");

        f.Seek(-769, SeekOrigin.End);

        byte temp = CFile.read_u8(f);
        if (temp == 12)
        {
            for (int i = 0; i < 256; i++)
            {
                Palette.colors[i].r = CFile.read_u8(f);
                Palette.colors[i].g = CFile.read_u8(f);
                Palette.colors[i].b = CFile.read_u8(f);
            }
        }

        f.Seek(128, SeekOrigin.Begin);

        for (int i = 0; i < 320 * 200; )
        {
            byte p = CFile.read_u8(f);
            if ((p & 0xc0) == 0xc0)
            {
                i += (p & 0x3f);
                temp = CFile.read_u8(f);
                new Span<byte>(s, p & 0x3f).Fill(temp);
                s += (p & 0x3f);
            }
            else
            {
                i++;
                *s = p;
                s++;
            }
            if (i != 0 && (i % 320 == 0))
            {
                s += Video.VGAScreen.pitch - 320;
            }
        }

        CFile.fclose(f);
    }
}
