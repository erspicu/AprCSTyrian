namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/memreader.c 的 MemReader（指向記憶體緩衝的游標 + 越界 error 旗標）。
/// </summary>
internal unsafe struct MemReader
{
    public byte* data;
    public nuint size;
    public bool error;  // 任一操作失敗後即停止後續操作
}

/// <summary>
/// 移植 sources/src/memwriter.c 的 MemWriter。
/// </summary>
internal unsafe struct MemWriter
{
    public byte* data;
    public nuint size;
    public bool error;
}

/// <summary>
/// 移植 memreader.c / memwriter.c 的 LE 記憶體讀寫函式（沿用原始函式名）。
/// </summary>
internal static unsafe class MemIO
{
    // ===== MemReader =====

    public static MemReader memReaderSplit(ref MemReader reader, nuint size)
    {
        reader.error |= reader.size < size;
        if (reader.error)
            return new MemReader { data = null, size = 0, error = true };

        byte* data = reader.data;
        reader.data += size;
        reader.size -= size;
        return new MemReader { data = data, size = size, error = false };
    }

    public static void memReaderSkip(ref MemReader reader, nuint size)
    {
        reader.error |= reader.size < size;
        if (reader.error)
            return;
        reader.data += size;
        reader.size -= size;
    }

    public static byte memReadU8(ref MemReader reader)
    {
        reader.error |= reader.size < 1;
        if (reader.error)
            return 0;
        byte value = reader.data[0];
        reader.data += 1;
        reader.size -= 1;
        return value;
    }

    public static void memReadU8Array(ref MemReader reader, byte* values, nuint count)
    {
        reader.error |= reader.size < count;
        if (reader.error)
        {
            for (; count > 0; --count) { *values = 0; values += 1; }
            return;
        }
        for (; count > 0; --count)
        {
            *values = reader.data[0];
            values += 1;
            reader.data += 1;
            reader.size -= 1;
        }
    }

    private static ushort loadU16LE(byte* data) =>
        (ushort)(data[0] | (data[1] << 8));

    public static ushort memReadU16LE(ref MemReader reader)
    {
        reader.error |= reader.size < 2;
        if (reader.error)
            return 0;
        ushort value = loadU16LE(reader.data);
        reader.data += 2;
        reader.size -= 2;
        return value;
    }

    public static void memReadU16LEArray(ref MemReader reader, ushort* values, nuint count)
    {
        reader.error |= reader.size / 2 < count;
        if (reader.error)
        {
            for (; count > 0; --count) { *values = 0; values += 1; }
            return;
        }
        for (; count > 0; --count)
        {
            *values = loadU16LE(reader.data);
            values += 1;
            reader.data += 2;
            reader.size -= 2;
        }
    }

    private static uint loadU32LE(byte* data) =>
        (uint)(data[0] | (data[1] << 8) | (data[2] << 16) | (data[3] << 24));

    public static uint memReadU32LE(ref MemReader reader)
    {
        reader.error |= reader.size < 4;
        if (reader.error)
            return 0;
        uint value = loadU32LE(reader.data);
        reader.data += 4;
        reader.size -= 4;
        return value;
    }

    public static void memReadU32LEArray(ref MemReader reader, uint* values, nuint count)
    {
        reader.error |= reader.size / 4 < count;
        if (reader.error)
        {
            for (; count > 0; --count) { *values = 0; values += 1; }
            return;
        }
        for (; count > 0; --count)
        {
            *values = loadU32LE(reader.data);
            values += 1;
            reader.data += 4;
            reader.size -= 4;
        }
    }

    public static bool memReadBool(ref MemReader reader) => memReadU8(ref reader) != 0;
    public static byte memReadChar(ref MemReader reader) => memReadU8(ref reader);
    public static void memReadCharArray(ref MemReader reader, byte* values, nuint count) => memReadU8Array(ref reader, values, count);
    public static sbyte memReadS8(ref MemReader reader) => (sbyte)memReadU8(ref reader);
    public static void memReadS8Array(ref MemReader reader, sbyte* values, nuint count) => memReadU8Array(ref reader, (byte*)values, count);
    public static short memReadS16LE(ref MemReader reader) => (short)memReadU16LE(ref reader);
    public static void memReadS16LEArray(ref MemReader reader, short* values, nuint count) => memReadU16LEArray(ref reader, (ushort*)values, count);
    public static int memReadS32LE(ref MemReader reader) => (int)memReadU32LE(ref reader);
    public static void memReadS32LEArray(ref MemReader reader, int* values, nuint count) => memReadU32LEArray(ref reader, (uint*)values, count);

    // ===== MemWriter =====

    public static MemWriter memWriterSplit(ref MemWriter writer, nuint size)
    {
        writer.error |= writer.size < size;
        if (writer.error)
            return new MemWriter { data = null, size = 0, error = true };

        byte* data = writer.data;
        writer.data += size;
        writer.size -= size;
        return new MemWriter { data = data, size = size, error = false };
    }

    public static void memWriterSkip(ref MemWriter writer, nuint size)
    {
        writer.error |= writer.size < size;
        if (writer.error)
            return;
        writer.data += size;
        writer.size -= size;
    }

    public static void memWriteFill(ref MemWriter writer, byte value, nuint size)
    {
        writer.error |= writer.size < size;
        if (writer.error)
            return;
        new Span<byte>(writer.data, checked((int)size)).Fill(value);
        writer.data += size;
        writer.size -= size;
    }

    public static void memWriteRead(ref MemWriter writer, ref MemReader reader, nuint size)
    {
        writer.error |= writer.size < size;
        reader.error |= reader.size < size;
        writer.error |= reader.error;
        reader.error = writer.error;

        if (writer.error)
            return;

        Buffer.MemoryCopy(reader.data, writer.data, writer.size, size);
        writer.data += size;
        writer.size -= size;
        reader.data += size;
        reader.size -= size;
    }

    public static void memWriteU8(ref MemWriter writer, byte value)
    {
        writer.error |= writer.size < 1;
        if (writer.error)
            return;
        writer.data[0] = value;
        writer.data += 1;
        writer.size -= 1;
    }

    public static void memWriteU8Array(ref MemWriter writer, byte* values, nuint count)
    {
        writer.error |= writer.size < count;
        if (writer.error)
            return;
        for (; count > 0; --count)
        {
            writer.data[0] = *values;
            values += 1;
            writer.data += 1;
            writer.size -= 1;
        }
    }

    private static void storeU16LE(byte* data, ushort value)
    {
        data[0] = (byte)value;
        data[1] = (byte)(value >> 8);
    }

    public static void memWriteU16LE(ref MemWriter writer, ushort value)
    {
        writer.error |= writer.size < 2;
        if (writer.error)
            return;
        storeU16LE(writer.data, value);
        writer.data += 2;
        writer.size -= 2;
    }

    public static void memWriteU16LEArray(ref MemWriter writer, ushort* values, nuint count)
    {
        writer.error |= writer.size / 2 < count;
        if (writer.error)
            return;
        for (; count > 0; --count)
        {
            storeU16LE(writer.data, *values);
            values += 1;
            writer.data += 2;
            writer.size -= 2;
        }
    }

    private static void storeU32LE(byte* data, uint value)
    {
        data[0] = (byte)value;
        data[1] = (byte)(value >> 8);
        data[2] = (byte)(value >> 16);
        data[3] = (byte)(value >> 24);
    }

    public static void memWriteU32LE(ref MemWriter writer, uint value)
    {
        writer.error |= writer.size < 4;
        if (writer.error)
            return;
        storeU32LE(writer.data, value);
        writer.data += 4;
        writer.size -= 4;
    }

    public static void memWriteU32LEArray(ref MemWriter writer, uint* values, nuint count)
    {
        writer.error |= writer.size / 4 < count;
        if (writer.error)
            return;
        for (; count > 0; --count)
        {
            storeU32LE(writer.data, *values);
            values += 1;
            writer.data += 4;
            writer.size -= 4;
        }
    }

    public static void memWriteBool(ref MemWriter writer, bool value) => memWriteU8(ref writer, (byte)(value ? 1 : 0));
    public static void memWriteChar(ref MemWriter writer, byte value) => memWriteU8(ref writer, value);
    public static void memWriteCharArray(ref MemWriter writer, byte* values, nuint count) => memWriteU8Array(ref writer, values, count);
    public static void memWriteS8(ref MemWriter writer, sbyte value) => memWriteU8(ref writer, (byte)value);
    public static void memWriteS8Array(ref MemWriter writer, sbyte* values, nuint count) => memWriteU8Array(ref writer, (byte*)values, count);
    public static void memWriteS16LE(ref MemWriter writer, short value) => memWriteU16LE(ref writer, (ushort)value);
    public static void memWriteS16LEArray(ref MemWriter writer, short* values, nuint count) => memWriteU16LEArray(ref writer, (ushort*)values, count);
    public static void memWriteS32LE(ref MemWriter writer, int value) => memWriteU32LE(ref writer, (uint)value);
    public static void memWriteS32LEArray(ref MemWriter writer, int* values, nuint count) => memWriteU32LEArray(ref writer, (uint*)values, count);
}
