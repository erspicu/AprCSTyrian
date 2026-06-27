using System.IO;
using System.IO.Compression;

namespace AprCSTyrian.App.Sdl;

/// <summary>最小 RGBA PNG 編碼器（截圖用）。輸入為 ARGB8888（0xAARRGGBB）緩衝。</summary>
internal static unsafe class PngWriter
{
    private static readonly uint[] Crc = BuildCrc();

    private static uint[] BuildCrc()
    {
        var t = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            t[n] = c;
        }
        return t;
    }

    private static uint Crc32(byte[] b, int off, int len)
    {
        uint c = 0xFFFFFFFFu;
        for (int i = 0; i < len; i++) c = Crc[(c ^ b[off + i]) & 0xff] ^ (c >> 8);
        return c ^ 0xFFFFFFFFu;
    }

    private static uint Adler32(byte[] d)
    {
        uint a = 1, b = 0;
        foreach (byte x in d) { a = (a + x) % 65521; b = (b + a) % 65521; }
        return (b << 16) | a;
    }

    private static void Be32(Stream s, uint v)
    { s.WriteByte((byte)(v >> 24)); s.WriteByte((byte)(v >> 16)); s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v); }

    private static void Chunk(Stream s, string type, byte[] data)
    {
        Be32(s, (uint)data.Length);
        byte[] td = System.Text.Encoding.ASCII.GetBytes(type);
        var crcBuf = new byte[4 + data.Length];
        System.Array.Copy(td, crcBuf, 4);
        System.Array.Copy(data, 0, crcBuf, 4, data.Length);
        s.Write(td, 0, 4);
        s.Write(data, 0, data.Length);
        Be32(s, Crc32(crcBuf, 0, crcBuf.Length));
    }

    /// <summary>把 ARGB8888 緩衝（w*h，0xAARRGGBB）以不透明 RGBA 寫成 PNG 檔。</summary>
    public static void WriteArgb(string path, uint* argb, int w, int h)
    {
        // 列前置 filter byte 0；ARGB→RGBA（A 固定 255）。
        var raw = new byte[h * (w * 4 + 1)];
        for (int y = 0; y < h; y++)
        {
            int o = y * (w * 4 + 1);
            raw[o++] = 0;
            uint* row = argb + y * w;
            for (int x = 0; x < w; x++)
            {
                uint p = row[x];
                raw[o++] = (byte)(p >> 16); // R
                raw[o++] = (byte)(p >> 8);  // G
                raw[o++] = (byte)p;         // B
                raw[o++] = 255;             // A
            }
        }

        byte[] deflated;
        using (var ms = new MemoryStream())
        {
            using (var ds = new DeflateStream(ms, CompressionLevel.Optimal, true)) ds.Write(raw, 0, raw.Length);
            deflated = ms.ToArray();
        }
        var idat = new byte[2 + deflated.Length + 4];
        idat[0] = 0x78; idat[1] = 0x9C;
        System.Array.Copy(deflated, 0, idat, 2, deflated.Length);
        uint ad = Adler32(raw);
        idat[^4] = (byte)(ad >> 24); idat[^3] = (byte)(ad >> 16); idat[^2] = (byte)(ad >> 8); idat[^1] = (byte)ad;

        using var f = File.Create(path);
        f.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0, 8);
        var ihdr = new byte[13];
        ihdr[0] = (byte)(w >> 24); ihdr[1] = (byte)(w >> 16); ihdr[2] = (byte)(w >> 8); ihdr[3] = (byte)w;
        ihdr[4] = (byte)(h >> 24); ihdr[5] = (byte)(h >> 16); ihdr[6] = (byte)(h >> 8); ihdr[7] = (byte)h;
        ihdr[8] = 8; ihdr[9] = 6; // RGBA
        Chunk(f, "IHDR", ihdr);
        Chunk(f, "IDAT", idat);
        Chunk(f, "IEND", System.Array.Empty<byte>());
    }
}
