using System.Buffers.Binary;

namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/file.c + file.h —— 檔案存取與二進位讀寫 helper。
/// C 的 <c>FILE*</c> 對應 .NET <see cref="Stream"/>。資料為 little-endian；
/// 在 LE 平台上原始的位元組交換為 no-op，故各 typed 讀寫直接讀原始位元組。
/// （命名沿用原始 C：dir_fopen / fread_u16_die …，以利對照。）
/// </summary>
internal static unsafe class CFile
{
    public static string? custom_data_dir = null;
    private static string? _dataDir = null;  // data_dir() 的 static 快取

    /// <summary>finds the Tyrian data directory（對應 file.c:data_dir）。</summary>
    public static string data_dir()
    {
        if (_dataDir != null)
            return _dataDir;

        string?[] dirs =
        {
            custom_data_dir,
            Globals.ConfiguredDataDir,
            "data",
            ".",
        };

        foreach (string? d in dirs)
        {
            if (d == null)
                continue;

            Stream? f = dir_fopen(d, "tyrian1.lvl", "rb");
            if (f != null)
            {
                fclose(f);
                _dataDir = d;
                break;
            }
        }

        _dataDir ??= ""; // data not found
        return _dataDir;
    }

    // prepend directory and fopen
    public static Stream? dir_fopen(string dir, string file, string mode)
    {
        string path = dir + "/" + file;
        return fopen(path, mode);
    }

    // warn when dir_fopen fails
    public static Stream? dir_fopen_warn(string dir, string file, string mode)
    {
        Stream? f = dir_fopen(dir, file, mode);
        if (f == null)
            Console.Error.WriteLine($"warning: failed to open '{file}'");
        return f;
    }

    // die when dir_fopen fails
    public static Stream dir_fopen_die(string dir, string file, string mode)
    {
        Stream? f = dir_fopen(dir, file, mode);
        if (f == null)
        {
            Console.Error.WriteLine($"error: failed to open '{file}'");
            Console.Error.WriteLine(
                $"error: One or more of the required Tyrian {Opentyr.TYRIAN_VERSION} data files could not be found.\n" +
                "       Please read the README file.");
            Varz.JE_tyrianHalt(1);
        }
        return f!;
    }

    // check if file can be opened for reading
    public static bool dir_file_exists(string dir, string file)
    {
        Stream? f = dir_fopen(dir, file, "rb");
        if (f != null)
            fclose(f);
        return f != null;
    }

    /// <summary>對應 C 的 fopen，回傳 null 表失敗。mode: "rb"/"wb"/"r+b"/"w+b" 等。</summary>
    public static Stream? fopen(string path, string mode)
    {
        try
        {
            bool read = mode.Contains('r');
            bool write = mode.Contains('w');
            bool plus = mode.Contains('+');

            FileMode fm;
            FileAccess fa;
            if (read && !write && !plus)        { fm = FileMode.Open;   fa = FileAccess.Read; }
            else if (read && plus)              { fm = FileMode.Open;   fa = FileAccess.ReadWrite; }
            else if (write && plus)             { fm = FileMode.Create; fa = FileAccess.ReadWrite; }
            else /* write */                    { fm = FileMode.Create; fa = FileAccess.Write; }

            return new FileStream(path, fm, fa);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>對應 fclose。</summary>
    public static void fclose(Stream f) => f.Dispose();

    /// <summary>returns end-of-file position（對應 file.c:ftell_eof）。</summary>
    public static long ftell_eof(Stream f) => f.Length;

    // === 原始指標式核心 ===

    public static void fread_die(void* buffer, nuint size, nuint count, Stream stream)
    {
        int total = checked((int)(size * count));
        var span = new Span<byte>(buffer, total);
        int got = ReadAll(stream, span);
        if (got != total)
            ReadError();
    }

    public static void fwrite_die(void* buffer, nuint size, nuint count, Stream stream)
    {
        int total = checked((int)(size * count));
        stream.Write(new ReadOnlySpan<byte>(buffer, total));
    }

    private static int ReadAll(Stream s, Span<byte> dst)
    {
        int off = 0;
        while (off < dst.Length)
        {
            int n = s.Read(dst.Slice(off));
            if (n == 0) break;
            off += n;
        }
        return off;
    }

    private static void ReadError()
    {
        Console.Error.WriteLine("error: An unexpected problem occurred while reading from a file.");
        throw new TyrianHaltException(1);
    }

    // === typed 指標式 fread（LE：直接讀原始位元組）===

    // 8-bit fread（對應 file.h:fread_u8）—— 不 die，回傳實際讀取的位元組數（供 EOF/feof 判斷）。
    public static nuint fread_u8(byte* buffer, nuint count, Stream s)
    {
        int total = checked((int)count);
        var span = new Span<byte>(buffer, total);
        return (nuint)ReadAll(s, span);
    }

    public static void fread_u8_die(byte* buffer, nuint count, Stream s) => fread_die(buffer, 1, count, s);
    public static void fread_s8_die(sbyte* buffer, nuint count, Stream s) => fread_die(buffer, 1, count, s);
    public static void fread_u16_die(ushort* buffer, nuint count, Stream s) => fread_die(buffer, 2, count, s);
    public static void fread_s16_die(short* buffer, nuint count, Stream s) => fread_die(buffer, 2, count, s);
    public static void fread_u32_die(uint* buffer, nuint count, Stream s) => fread_die(buffer, 4, count, s);
    public static void fread_s32_die(int* buffer, nuint count, Stream s) => fread_die(buffer, 4, count, s);

    public static void fwrite_u8_die(byte* buffer, nuint count, Stream s) => fwrite_die(buffer, 1, count, s);
    public static void fwrite_s8_die(sbyte* buffer, nuint count, Stream s) => fwrite_die(buffer, 1, count, s);

    // === 純量便利讀寫（非原始 API，但簡化 `fread_x(&v,1,f)` 樣式的移植）===

    public static byte read_u8(Stream s)
    {
        int b = s.ReadByte();
        if (b < 0) ReadError();
        return (byte)b;
    }
    public static sbyte read_s8(Stream s) => (sbyte)read_u8(s);

    public static ushort read_u16(Stream s) { Span<byte> b = stackalloc byte[2]; ReadExact(s, b); return BinaryPrimitives.ReadUInt16LittleEndian(b); }
    public static short read_s16(Stream s) { Span<byte> b = stackalloc byte[2]; ReadExact(s, b); return BinaryPrimitives.ReadInt16LittleEndian(b); }
    public static uint read_u32(Stream s) { Span<byte> b = stackalloc byte[4]; ReadExact(s, b); return BinaryPrimitives.ReadUInt32LittleEndian(b); }
    public static int read_s32(Stream s) { Span<byte> b = stackalloc byte[4]; ReadExact(s, b); return BinaryPrimitives.ReadInt32LittleEndian(b); }
    public static bool read_bool(Stream s) => read_u8(s) != 0;

    public static void write_u8(Stream s, byte v) => s.WriteByte(v);
    public static void write_s8(Stream s, sbyte v) => s.WriteByte((byte)v);
    public static void write_u16(Stream s, ushort v) { Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteUInt16LittleEndian(b, v); s.Write(b); }
    public static void write_s16(Stream s, short v) { Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteInt16LittleEndian(b, v); s.Write(b); }
    public static void write_u32(Stream s, uint v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(b, v); s.Write(b); }
    public static void write_s32(Stream s, int v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteInt32LittleEndian(b, v); s.Write(b); }
    public static void write_bool(Stream s, bool v) => s.WriteByte((byte)(v ? 1 : 0));

    private static void ReadExact(Stream s, Span<byte> b)
    {
        int off = 0;
        while (off < b.Length)
        {
            int n = s.Read(b.Slice(off));
            if (n == 0) { ReadError(); return; }
            off += n;
        }
    }
}
