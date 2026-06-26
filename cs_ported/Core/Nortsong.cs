namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/nortsong.c —— 目前僅移植**畫面計時**部分（對應原 PIT frameCount 機制），
/// 以 <see cref="Globals.Clock"/> 取代 SDL_GetTicks/SDL_Delay。
/// 音效載入/音量 (loadSndFile/JE_playSampleNum/JE_changeVolume) 待 Phase D 音訊一併移植。
/// </summary>
internal static unsafe class Nortsong
{
    public static ushort frameCountMax = 0;

    // 音效樣本（轉成輸出取樣率/格式後的 16-bit mono；非託管以供混音器指標推進）。
    public static readonly short*[] soundSamples = new short*[Sndmast.SOUND_COUNT];
    public static readonly nuint[] soundSampleCount = new nuint[Sndmast.SOUND_COUNT];

#pragma warning disable CS0649 // 由遊戲邏輯（設定/選單）指派
    public static ushort tyrMusicVolume, fxVolume;
    public static ushort tempVolume;
#pragma warning restore CS0649
    public const ushort fxPlayVol = 4;

    // x86 PIT 頻率為 (315/88/3) MHz；每 `speed` 週期觸發一次中斷以遞減 frameCount。
    private static ushort frameSpeed = 0x4300;

    // Fixed point UQ6.10 milliseconds.
    private static ushort framePeriod = (ushort)(((ulong)0x4300 << 10) * 1000 * 88 * 3 / 315000000);

    // Fixed point UQ22.10 milliseconds.
    private static uint frameCountEnd = 0;
    private static uint frameCount2End = 0;

    public static void setFrameSpeed(ushort speed)
    {
        frameSpeed = speed;
        framePeriod = (ushort)(((ulong)speed << 10) * 1000 * 88 * 3 / 315000000);

        uint now = Globals.Clock.Ticks << 10;
        frameCountEnd = now;
    }

    public static void setFrameCount(ushort frameCount)
    {
        // Keep the partial timer period that has already elapsed.
        uint now = Globals.Clock.Ticks << 10;
        int diff = (int)(now - frameCountEnd);
        if (diff >= framePeriod)
            frameCountEnd = now - (uint)diff % framePeriod;
        else if (-diff >= framePeriod)
            frameCountEnd = now + (uint)(-diff) % framePeriod;

        frameCountEnd += (uint)(frameCount * framePeriod);
    }

    public static void setFrameCount2(ushort frameCount2)
    {
        uint now = Globals.Clock.Ticks << 10;
        int diff = (int)(now - frameCount2End);
        if (diff >= framePeriod)
            frameCount2End = now - (uint)diff % framePeriod;
        else if (-diff >= framePeriod)
            frameCount2End = now + (uint)(-diff) % framePeriod;

        frameCount2End += (uint)(frameCount2 * framePeriod);
    }

    public static uint getFrameCountTicks()
    {
        const uint half = 1 << 9;
        uint now = Globals.Clock.Ticks << 10;
        int diff = (int)(frameCountEnd - now);
        return diff >= 0 ? ((uint)diff + half) >> 10 : 0;
    }

    public static uint getFrameCount2Ticks()
    {
        const uint half = 1 << 9;
        uint now = Globals.Clock.Ticks << 10;
        int diff = (int)(frameCount2End - now);
        return diff >= 0 ? ((uint)diff + half) >> 10 : 0;
    }

    public static void delayUntilElapsed()
    {
        const uint half = 1 << 9;
        uint now = Globals.Clock.Ticks << 10;
        int diff = (int)(frameCountEnd - now);
        if (diff >= 0)
            Globals.Clock.Delay(((uint)diff + half) >> 10);
    }

    /// <summary>
    /// 載入 tyrian.snd / voices.snd 音效，並轉成輸出取樣率的 16-bit mono（對應 nortsong.c:loadSndFile）。
    /// 原始用 SDL_ConvertAudio（S8 11025→S16SYS audioSampleRate）；此處以最近鄰升頻 + S8&lt;&lt;8 取代（不依賴 SDL）。
    /// </summary>
    public static void loadSndFile(bool xmas)
    {
        // 先把 SFX 與語音的原始 8-bit 位元組讀進暫存。
        var raw = new byte[Sndmast.SOUND_COUNT][];

        Stream f = CFile.dir_fopen_die(CFile.data_dir(), "tyrian.snd", "rb");
        ushort sfxCount = CFile.read_u16(f);
        if (sfxCount != Sndmast.SFX_COUNT) DieRead();

        uint[] sfxPositions = new uint[Sndmast.SFX_COUNT + 1];
        for (int i = 0; i < sfxCount; ++i) sfxPositions[i] = CFile.read_u32(f);
        f.Seek(0, SeekOrigin.End);
        sfxPositions[sfxCount] = (uint)f.Position;

        for (int i = 0; i < sfxCount; ++i)
        {
            int n = (int)(sfxPositions[i + 1] - sfxPositions[i]);
            if (n > ushort.MaxValue) DieRead();
            raw[i] = new byte[n];
            f.Seek(sfxPositions[i], SeekOrigin.Begin);
            ReadExactBytes(f, raw[i]);
        }
        CFile.fclose(f);

        f = CFile.dir_fopen_die(CFile.data_dir(), xmas ? "voicesc.snd" : "voices.snd", "rb");
        ushort voiceCount = CFile.read_u16(f);
        if (voiceCount != Sndmast.VOICE_COUNT) DieRead();

        uint[] voicePositions = new uint[Sndmast.VOICE_COUNT + 1];
        for (int i = 0; i < voiceCount; ++i) voicePositions[i] = CFile.read_u32(f);
        f.Seek(0, SeekOrigin.End);
        voicePositions[voiceCount] = (uint)f.Position;

        for (int vi = 0; vi < voiceCount; ++vi)
        {
            int i = Sndmast.SFX_COUNT + vi;
            int n = (int)(voicePositions[vi + 1] - voicePositions[vi]);
            n = n >= 100 ? n - 100 : 0; // Voice sounds have some bad data at the end.
            if (n > ushort.MaxValue) DieRead();
            raw[i] = new byte[n];
            f.Seek(voicePositions[vi], SeekOrigin.Begin);
            ReadExactBytes(f, raw[i]);
        }
        CFile.fclose(f);

        // 轉換到輸出取樣率（最近鄰）：S8 11025 → S16 audioSampleRate。
        int outRate = Loudness.audioSampleRate > 0 ? Loudness.audioSampleRate : 44100;
        for (int i = 0; i < Sndmast.SOUND_COUNT; ++i)
        {
            byte[] src = raw[i];
            int inLen = src.Length;
            int outLen = (int)((long)inLen * outRate / 11025);

            CMem.free(soundSamples[i]);
            short* dst = (short*)CMem.malloc((nuint)outLen * sizeof(short));
            for (int j = 0; j < outLen; ++j)
            {
                int srcIdx = (int)((long)j * 11025 / outRate);
                if (srcIdx >= inLen) srcIdx = inLen - 1;
                dst[j] = srcIdx >= 0 ? (short)((sbyte)src[srcIdx] << 8) : (short)0;
            }
            soundSamples[i] = dst;
            soundSampleCount[i] = (nuint)outLen;
        }
    }

    private static void ReadExactBytes(Stream s, byte[] buf)
    {
        int off = 0;
        while (off < buf.Length)
        {
            int r = s.Read(buf, off, buf.Length - off);
            if (r == 0) { DieRead(); return; }
            off += r;
        }
    }

    private static void DieRead()
    {
        Console.Error.WriteLine("error: Unexpected data was read from a file.");
        throw new TyrianHaltException(1);
    }

    /// <summary>播放音效（對應 nortsong.c:JE_playSampleNum）。</summary>
    public static void JE_playSampleNum(byte samplenum)
    {
        Loudness.multiSamplePlay(soundSamples[samplenum - 1], soundSampleCount[samplenum - 1], 0, (byte)fxPlayVol);
    }

    /// <summary>對應 nortsong.c:JE_changeVolume。</summary>
    public static void JE_changeVolume(ref ushort music, int music_delta, ref ushort sample, int sample_delta)
    {
        int music_temp = music + music_delta;
        int sample_temp = sample + sample_delta;

        if (music_delta != 0)
        {
            if (music_temp > 255) { music_temp = 255; JE_playSampleNum((byte)Sndmast.S_CLINK); }
            else if (music_temp < 0) { music_temp = 0; JE_playSampleNum((byte)Sndmast.S_CLINK); }
        }
        if (sample_delta != 0)
        {
            if (sample_temp > 255) { sample_temp = 255; JE_playSampleNum((byte)Sndmast.S_CLINK); }
            else if (sample_temp < 0) { sample_temp = 0; JE_playSampleNum((byte)Sndmast.S_CLINK); }
        }

        music = (ushort)music_temp;
        sample = (ushort)sample_temp;

        Loudness.set_volume((byte)music, (byte)sample);
    }
}
