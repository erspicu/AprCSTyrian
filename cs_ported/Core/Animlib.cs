namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/animlib.c —— .ANM 動畫播放（Run/Skip/Dump 解碼）。
/// 用於關卡載入的 ]A 命令（playAnim("tyrend.anm",...)）。
/// </summary>
internal static unsafe class Animlib
{
    private struct FileHeader
    {
        public ushort pageCount;
        public uint recordCount;
    }

    private struct PageDescriptor
    {
        public ushort firstRecord;
        public ushort recordCount;
        public ushort recordsSize;
    }

    private static int ReadBytes(Stream f, byte* buf, int n)
    {
        var span = new Span<byte>(buf, n);
        int total = 0;
        while (total < n)
        {
            int r = f.Read(span.Slice(total));
            if (r <= 0) break;
            total += r;
        }
        return total;
    }

    private static bool readFileHeader(out FileHeader fileHeader, Stream f)
    {
        byte* data = stackalloc byte[256];
        int size = ReadBytes(f, data, 256);

        MemReader reader = new() { data = data, size = (nuint)size, error = false };

        MemIO.memReaderSkip(ref reader, 6);
        fileHeader.pageCount = MemIO.memReadU16LE(ref reader);
        fileHeader.recordCount = MemIO.memReadU32LE(ref reader);
        MemIO.memReaderSkip(ref reader, 244);

        return !reader.error;
    }

    private static bool readPalette(SDL_Color[] palette, Stream f)
    {
        byte* data = stackalloc byte[4 * 256];
        int size = ReadBytes(f, data, 4 * 256);

        MemReader reader = new() { data = data, size = (nuint)size, error = false };

        for (int i = 0; i < 256; ++i)
        {
            byte b = MemIO.memReadU8(ref reader);
            byte g = MemIO.memReadU8(ref reader);
            byte r = MemIO.memReadU8(ref reader);
            MemIO.memReadU8(ref reader);
            palette[i] = new SDL_Color(r, g, b);
        }

        return !reader.error;
    }

    private static bool readPageDescriptors(PageDescriptor[] pageDescriptors, Stream f)
    {
        byte* data = stackalloc byte[6 * 256];
        int size = ReadBytes(f, data, 6 * 256);

        MemReader reader = new() { data = data, size = (nuint)size, error = false };

        for (int i = 0; i < 256; ++i)
        {
            pageDescriptors[i].firstRecord = MemIO.memReadU16LE(ref reader);
            pageDescriptors[i].recordCount = MemIO.memReadU16LE(ref reader);
            pageDescriptors[i].recordsSize = MemIO.memReadU16LE(ref reader);
        }

        return !reader.error;
    }

    private static void decodeRunSkipDump(ref MemWriter writer, ref MemReader reader)
    {
        while (!reader.error)
        {
            byte opCode = MemIO.memReadU8(ref reader);

            if (opCode == 0)  // 00: Short run
            {
                byte size = MemIO.memReadU8(ref reader);
                byte value = MemIO.memReadU8(ref reader);
                MemIO.memWriteFill(ref writer, value, size);
            }
            else if (opCode > 0x80)  // 81..FF: Short skip
            {
                byte size = (byte)(opCode - 0x80);
                MemIO.memWriterSkip(ref writer, size);
            }
            else if (opCode < 0x80)  // 01..7F: Short dump
            {
                byte size = opCode;
                MemIO.memWriteRead(ref writer, ref reader, size);
            }
            else  // 80: Long op
            {
                ushort longOp = MemIO.memReadU16LE(ref reader);

                if (longOp == 0)  // 0000: Stop
                    return;
                else if (longOp >= 0xC000)  // Long run
                {
                    ushort size = (ushort)(longOp - 0xC000);
                    byte value = MemIO.memReadU8(ref reader);
                    MemIO.memWriteFill(ref writer, value, size);
                }
                else if (longOp < 0x8000)  // Long skip
                {
                    ushort size = longOp;
                    MemIO.memWriterSkip(ref writer, size);
                }
                else  // 8000..BFFF: Long dump
                {
                    ushort size = (ushort)(longOp - 0x8000);
                    MemIO.memWriteRead(ref writer, ref reader, size);
                }
            }
        }
    }

    public static void playAnim(string filename, byte startingFrame, byte speed)
    {
        Video.JE_clr256(Video.VGAScreen);
        Video.JE_showVGA();

        Stream? f = CFile.dir_fopen(CFile.data_dir(), filename, "rb");
        if (f == null)
            return;

        try
        {
            bool success = readFileHeader(out FileHeader fileHeader, f);

            var palette = new SDL_Color[256];
            success = success && readPalette(palette, f);

            var pageDescriptors = new PageDescriptor[256];
            success = success && readPageDescriptors(pageDescriptors, f);

            if (!success)
                return;

            palette[0] = new SDL_Color(0, 0, 0);
            Palette.set_palette(palette, 0, 255);

            ushort firstRecord = 0;
            ushort recordCount = 0;

            const int dataSize = 1 << 16;
            byte* data = (byte*)CMem.malloc(dataSize);

            MemReader recordSizesReader = default;
            MemReader recordsReader = default;

            const int imageSize = 320 * 200;
            byte* image = (byte*)CMem.calloc(imageSize, 1);

            for (uint record = startingFrame; record < fileHeader.recordCount - 1; ++record)
            {
                Nortsong.setFrameCount(speed);

                if (record < firstRecord || record - firstRecord >= recordCount)
                {
                    // 找到包含此 record 的 page 並載入。
                    for (int i = 0; i < fileHeader.pageCount; ++i)
                    {
                        ref PageDescriptor pd = ref pageDescriptors[i];
                        firstRecord = pd.firstRecord;
                        recordCount = pd.recordCount;
                        ushort recordsSize = pd.recordsSize;

                        if (record >= firstRecord && record - firstRecord < recordCount)
                        {
                            f.Seek(0xB00 + ((long)i << 16), SeekOrigin.Begin);

                            int pageSize = 8 + 2 * recordCount + recordsSize;
                            int toRead = Math.Min(pageSize, dataSize);
                            int size = ReadBytes(f, data, toRead);

                            MemReader pageReader = new() { data = data, size = (nuint)size, error = size != pageSize };

                            MemIO.memReaderSkip(ref pageReader, 8);
                            recordSizesReader = MemIO.memReaderSplit(ref pageReader, (nuint)(2 * recordCount));
                            recordsReader = pageReader;
                            break;
                        }
                    }
                }

                while (record >= firstRecord && recordCount > 0)
                {
                    firstRecord += 1;
                    recordCount -= 1;

                    ushort recordSize = MemIO.memReadU16LE(ref recordSizesReader);
                    if (recordSizesReader.error)
                        continue;

                    MemReader recordReader = MemIO.memReaderSplit(ref recordsReader, recordSize);

                    if (record > firstRecord)
                        continue;

                    // Record header.
                    MemIO.memReadU8(ref recordReader);     // Bitmap ID ('B')
                    MemIO.memReadU8(ref recordReader);     // Flags
                    MemIO.memReadU16LE(ref recordReader);  // Body Type

                    MemWriter imageWriter = new() { data = image, size = imageSize, error = false };
                    decodeRunSkipDump(ref imageWriter, ref recordReader);

                    var screen = Video.VGAScreen;
                    for (int y = 0; y < 200; ++y)
                        Buffer.MemoryCopy(image + y * 320, screen.pixels + y * screen.pitch, 320, 320);

                    Video.JE_showVGA();
                }

                if (Keyboard.waitUntilGetInputOrElapsed())
                    break;
            }

            CMem.free(image);
            CMem.free(data);
        }
        finally
        {
            f.Dispose();
        }
    }
}
