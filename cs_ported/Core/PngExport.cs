using System.IO;
using System.IO.Compression;

namespace AprCSTyrian.Core;

/// <summary>
/// 素材匯出工具（非遊戲邏輯）：把 Tyrian 各種圖形格式套用調色盤、保留透明度，匯出成 RGBA PNG。
/// 由環境變數 EXPORT_PNG=&lt;輸出目錄&gt; 觸發（在主 shape table 載入後執行一次）。
/// 透明來源：sprite RLE 的 skip opcode（253/254/255）與 sprite2 的 skip nibble 對應的像素 → alpha=0。
/// </summary>
internal static unsafe class PngExport
{
    // ---- 最小 RGBA PNG 編碼器 ----
    private static readonly uint[] crcTable = BuildCrcTable();

    private static uint[] BuildCrcTable()
    {
        var t = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            t[n] = c;
        }
        return t;
    }

    private static uint Crc32(byte[] buf, int offset, int len)
    {
        uint c = 0xFFFFFFFFu;
        for (int i = 0; i < len; i++)
            c = crcTable[(c ^ buf[offset + i]) & 0xff] ^ (c >> 8);
        return c ^ 0xFFFFFFFFu;
    }

    private static uint Adler32(byte[] data)
    {
        uint a = 1, b = 0;
        foreach (byte x in data) { a = (a + x) % 65521; b = (b + a) % 65521; }
        return (b << 16) | a;
    }

    private static void WriteBE32(Stream s, uint v)
    {
        s.WriteByte((byte)(v >> 24)); s.WriteByte((byte)(v >> 16));
        s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v);
    }

    private static void WriteChunk(Stream s, string type, byte[] data)
    {
        WriteBE32(s, (uint)data.Length);
        byte[] td = System.Text.Encoding.ASCII.GetBytes(type);
        var crcBuf = new byte[4 + data.Length];
        System.Array.Copy(td, crcBuf, 4);
        System.Array.Copy(data, 0, crcBuf, 4, data.Length);
        s.Write(td, 0, 4);
        s.Write(data, 0, data.Length);
        WriteBE32(s, Crc32(crcBuf, 0, crcBuf.Length));
    }

    /// <summary>把 RGBA 像素(w*h*4, 由左上往右下)寫成 PNG 檔。</summary>
    private static void WritePng(string path, byte[] rgba, int w, int h)
    {
        // 每列前置 filter byte 0
        var raw = new byte[h * (w * 4 + 1)];
        for (int y = 0; y < h; y++)
        {
            raw[y * (w * 4 + 1)] = 0;
            System.Array.Copy(rgba, y * w * 4, raw, y * (w * 4 + 1) + 1, w * 4);
        }
        byte[] deflated;
        using (var ms = new MemoryStream())
        {
            using (var ds = new DeflateStream(ms, CompressionLevel.Optimal, true))
                ds.Write(raw, 0, raw.Length);
            deflated = ms.ToArray();
        }
        // zlib 包裝: 0x78 0x9C + deflate + adler32
        var idat = new byte[2 + deflated.Length + 4];
        idat[0] = 0x78; idat[1] = 0x9C;
        System.Array.Copy(deflated, 0, idat, 2, deflated.Length);
        uint adler = Adler32(raw);
        idat[^4] = (byte)(adler >> 24); idat[^3] = (byte)(adler >> 16);
        idat[^2] = (byte)(adler >> 8); idat[^1] = (byte)adler;

        using var f = File.Create(path);
        f.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0, 8);
        var ihdr = new byte[13];
        ihdr[0] = (byte)(w >> 24); ihdr[1] = (byte)(w >> 16); ihdr[2] = (byte)(w >> 8); ihdr[3] = (byte)w;
        ihdr[4] = (byte)(h >> 24); ihdr[5] = (byte)(h >> 16); ihdr[6] = (byte)(h >> 8); ihdr[7] = (byte)h;
        ihdr[8] = 8;   // bit depth
        ihdr[9] = 6;   // color type RGBA
        WriteChunk(f, "IHDR", ihdr);
        WriteChunk(f, "IDAT", idat);
        WriteChunk(f, "IEND", System.Array.Empty<byte>());
    }

    // ---- 解碼器(套調色盤 + 透明) ----

    /// <summary>解碼 sprite_table 的單一 Sprite（RLE，skip opcode = 透明）。對應 blit_sprite。</summary>
    private static byte[] DecodeSprite(in Sprite spr, SDL_Color[] pal)
    {
        int w = spr.width, h = spr.height;
        var rgba = new byte[w * h * 4]; // 全 0 = 透明
        if (spr.data == null || w == 0 || h == 0) return rgba;
        byte* data = spr.data, end = data + spr.size;
        int x_offset = 0, pos = 0;
        for (; data < end; ++data)
        {
            switch (*data)
            {
                case 255: data++; pos += *data; x_offset += *data; break;
                case 254: pos += w - x_offset; x_offset = w; break;
                case 253: pos++; x_offset++; break;
                default:
                    if (pos >= 0 && pos < w * h) PutPixel(rgba, pos, pal[*data]);
                    pos++; x_offset++;
                    break;
            }
            if (x_offset >= w) { pos += w - x_offset; x_offset = 0; }
        }
        return rgba;
    }

    /// <summary>sprite2 sheet 內的 sprite 數量 = 偏移表第一項 / 2。</summary>
    private static int Sprite2Count(byte* sheet) => sheet == null ? 0 : ((ushort*)sheet)[0] / 2;

    /// <summary>解碼 sprite2 sheet 的單一 sprite（固定 12px 寬，skip nibble = 透明）。對應 blit_sprite2。</summary>
    private static byte[] DecodeSprite2(byte* sheet, uint index, SDL_Color[] pal, out int w, out int h)
    {
        w = 12; const int maxH = 240;
        var tmp = new byte[w * maxH * 4];
        byte* data = sheet + ((ushort*)sheet)[index - 1];
        int pos = 0, maxPos = 0;
        for (; *data != 0x0f; ++data)
        {
            pos += *data & 0x0f;
            int count = (*data & 0xf0) >> 4;
            if (count == 0)
            {
                pos += w - 12; // w=12 → 0；skip nibble 已換行
            }
            else
            {
                while (count-- != 0)
                {
                    ++data;
                    if (pos >= 0 && pos < w * maxH) PutPixel(tmp, pos, pal[*data]);
                    pos++;
                }
            }
            if (pos > maxPos) maxPos = pos;
        }
        h = (maxPos + w - 1) / w;
        if (h < 1) h = 1;
        var rgba = new byte[w * h * 4];
        System.Array.Copy(tmp, rgba, System.Math.Min(rgba.Length, tmp.Length));
        return rgba;
    }

    /// <summary>解碼 tyrian.pic 的整頁 320x200 圖（不透明）。對應 JE_loadPic 的 RLE。</summary>
    private static byte[] DecodePic(byte PCXnumber, SDL_Color[] pal)
    {
        const int W = 320, H = 200;
        var idx = new byte[W * H];
        Stream f = CFile.dir_fopen_die(CFile.data_dir(), "tyrian.pic", "rb");
        // 確保 pcxpos 已填(JE_loadPic first 會做；此處自行讀)
        f.Seek(0, SeekOrigin.Begin);
        _ = CFile.read_u16(f);
        fixed (int* pp = Pcxmast.pcxpos)
            CFile.fread_s32_die(pp, Pcxmast.PCX_NUM, f);
        Pcxmast.pcxpos[Pcxmast.PCX_NUM] = (int)CFile.ftell_eof(f);

        int pi = PCXnumber - 1;
        uint size = (uint)(Pcxmast.pcxpos[pi + 1] - Pcxmast.pcxpos[pi]);
        byte* buffer = (byte*)CMem.malloc(size);
        f.Seek(Pcxmast.pcxpos[pi], SeekOrigin.Begin);
        CFile.fread_u8_die(buffer, size, f);
        CFile.fclose(f);

        byte* p = buffer;
        int s = 0;
        for (int i = 0; i < W * H;)
        {
            if ((*p & 0xc0) == 0xc0)
            {
                int run = *p & 0x3f;
                i += run;
                for (int k = 0; k < run; k++) idx[s + k] = *(p + 1);
                s += run; p += 2;
            }
            else { idx[s++] = *p++; i++; }
        }
        CMem.free(buffer);

        var rgba = new byte[W * H * 4];
        for (int i = 0; i < W * H; i++) PutPixel(rgba, i, pal[idx[i]], 255);
        return rgba;
    }

    /// <summary>解碼背景地形 tile（24x28，index 0 = 透明）。對應 blit_background_row。</summary>
    private static byte[] DecodeTile(byte[] tile, SDL_Color[] pal)
    {
        var rgba = new byte[24 * 28 * 4];
        for (int i = 0; i < 24 * 28; i++)
            if (tile[i] != 0) PutPixel(rgba, i, pal[tile[i]], 255);
        return rgba;
    }

    /// <summary>讀 shapes&lt;c&gt;.dat（最多 600 個 24x28 tile，blank 跳過）→ 匯出 PNG。</summary>
    private static int ExportTiles(char c, string dir, SDL_Color[] pal)
    {
        string fn = $"shapes{char.ToLowerInvariant(c)}.dat";
        if (!CFile.dir_file_exists(CFile.data_dir(), fn)) return 0;
        Stream f = CFile.dir_fopen_die(CFile.data_dir(), fn, "rb");
        Directory.CreateDirectory(dir);
        var buf = new byte[24 * 28];
        int cnt = 0;
        for (int z = 0; z < 600; z++)
        {
            int b = f.ReadByte();
            if (b < 0) break;          // EOF
            if (b != 0) continue;      // blank tile
            int got = 0, r;
            while (got < buf.Length && (r = f.Read(buf, got, buf.Length - got)) > 0) got += r;
            if (got < buf.Length) break;
            WritePng(Path.Combine(dir, $"{z:D3}.png"), DecodeTile(buf, pal), 24, 28);
            cnt++;
        }
        CFile.fclose(f);
        return cnt;
    }

    private static void PutPixel(byte[] rgba, int pos, SDL_Color c, byte a = 255)
    {
        int o = pos * 4;
        rgba[o] = c.r; rgba[o + 1] = c.g; rgba[o + 2] = c.b; rgba[o + 3] = a;
    }

    private static SDL_Color[] BuildPal(int p) => Palette.palettes[p];

    // ---- 匯出主流程 ----
    public static void ExportAll(string outRoot)
    {
        var pal0 = BuildPal(0); // 主遊戲調色盤(sprite/sheet 用)
        int total = 0;

        // 1) sprite_table 0..7（字型/介面/option/weapon/extra）
        Sprites.load_sprites_file((uint)Sprites.EXTRA_SHAPES, "estsc.shp");
        string[] tableNames = { "00_font", "01_smallfont", "02_tinyfont", "03_planet", "04_face", "05_option", "06_weapon", "07_extra" };
        for (uint t = 0; t < tableNames.Length; t++)
        {
            string dir = Path.Combine(outRoot, "sprites", tableNames[t]);
            Directory.CreateDirectory(dir);
            for (uint i = 0; i < Sprites.sprite_table[t].count; i++)
            {
                if (!Sprites.sprite_exists(t, i)) continue;
                ref Sprite spr = ref Sprites.sprite(t, i);
                var rgba = DecodeSprite(spr, pal0);
                WritePng(Path.Combine(dir, $"{i:D3}.png"), rgba, spr.width, spr.height);
                total++;
            }
        }

        // 2) 內建 sprite2 sheets 8..12（玩家彈/船/道具/錢幣/彈2）
        var sheets8 = new (Sprite2_array sheet, string name)[]
        {
            (Sprites.spriteSheet8, "08_player_shots"), (Sprites.spriteSheet9, "09_player_ships"),
            (Sprites.spriteSheet10, "10_powerups"), (Sprites.spriteSheet11, "11_coins_cubes"),
            (Sprites.spriteSheet12, "12_player_shots2"),
        };
        foreach (var (sheet, name) in sheets8)
            total += ExportSheet(sheet.data, Path.Combine(outRoot, "sheets", name), pal0);

        // 3) newsh*.shp 外部 sheets（敵人/船艦/介面等）
        char[] shapeChars = "0123456789abcdefghijklmnopqrstuv#^~".ToCharArray();
        foreach (char c in shapeChars)
        {
            string fn = $"newsh{char.ToLowerInvariant(c)}.shp";
            if (!CFile.dir_file_exists(CFile.data_dir(), fn)) continue;
            Sprite2_array sh = default;
            Sprites.JE_loadCompShapes(ref sh, c);
            total += ExportSheet(sh.data, Path.Combine(outRoot, "sheets_newsh", $"newsh_{c}"), pal0);
            Sprites.free_sprite2s(ref sh);
        }

        // 4) tyrian.pic 整頁圖(各自的調色盤)
        string picDir = Path.Combine(outRoot, "pics");
        Directory.CreateDirectory(picDir);
        for (byte n = 1; n <= Pcxmast.PCX_NUM; n++)
        {
            var pal = BuildPal(Pcxmast.pcxpal[n - 1]);
            var rgba = DecodePic(n, pal);
            WritePng(Path.Combine(picDir, $"pic_{n:D2}.png"), rgba, 320, 200);
            total++;
        }

        // 5) shapes*.dat 背景地形 tile（24x28，index 0 = 透明）
        foreach (char c in new[] { ')', 'w', 'x', 'y', 'z' })
        {
            string sub = c == ')' ? "shapes_paren" : $"shapes_{c}";
            total += ExportTiles(c, Path.Combine(outRoot, "tiles", sub), pal0);
        }

        System.Console.Error.WriteLine($"[PngExport] 完成，共 {total} 張 PNG → {outRoot}");
    }

    private static int ExportSheet(byte* data, string dir, SDL_Color[] pal)
    {
        if (data == null) return 0;
        Directory.CreateDirectory(dir);
        int n = Sprite2Count(data), cnt = 0;
        for (uint i = 1; i <= n; i++)
        {
            var rgba = DecodeSprite2(data, i, pal, out int w, out int h);
            WritePng(Path.Combine(dir, $"{i:D3}.png"), rgba, w, h);
            cnt++;
        }
        return cnt;
    }
}
