using AprCSTyrian.Core.Ports;

namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/loudness.c —— 音訊混音器。原始的 SDL audioCallback 在此實作為
/// <see cref="IAudioSource"/>，由 App 的 SdlAudio adapter 在音訊執行緒呼叫；
/// 共享狀態以 <see cref="IAudioBackend.Lock"/>/Unlock 保護。
/// 音樂(OPL/lds) 目前為靜音 stub；音效(SFX) 混音完整可用。
/// </summary>
internal static unsafe class Loudness
{
    private const int OUTPUT_QUALITY = 4; // 44.1 kHz

    public static int audioSampleRate = 0;

    private static bool music_stopped = true;
    public static uint song_playing = 0;
    public static bool playing = false; // loudness.h: extern JE_boolean playing（歌曲是否正在播放）

    public static bool audio_disabled = false, music_disabled = false, samples_disabled = false;

    private static byte musicVolume = 255;
    private static byte sampleVolume = 255;

    private const float volumeRange = 30.0f; // dB

    // Fixed point Q20.12
    private static readonly int[] volumeFactorTable = new int[256];
    private static int TO_FIXED(float x) => (int)(x * (1 << 12));
    private static int FIXED_TO_INT(int x) => x >> 12;

    private const int ldsUpdate2Rate = 139; // 69.5 * 2
    private static int samplesPerLdsUpdate;
    private static int samplesPerLdsUpdateFrac;
    private static int samplesUntilLdsUpdate = 0;
    private static int samplesUntilLdsUpdateFrac = 0;

    private static Stream? music_file = null;
    private static uint[] song_offset = Array.Empty<uint>();
    private static ushort song_count = 0;

    private const int CHANNEL_COUNT = 8;
    private static readonly short*[] channelSamples = new short*[CHANNEL_COUNT];
    private static readonly nuint[] channelSampleCount = new nuint[CHANNEL_COUNT];
    private static readonly byte[] channelVolume = new byte[CHANNEL_COUNT];
    private const int CHANNEL_VOLUME_LEVELS = 8;

    private sealed class Source : IAudioSource
    {
        public void GenerateSamples(Span<short> buffer) => audioCallback(buffer);
    }
    private static readonly Source _source = new();

    public static bool init_audio()
    {
        if (audio_disabled)
            return false;

        // 裝置由 adapter 以 mono S16 44100 開啟（保持暫停）。
        Globals.Audio.Start(_source);

        audioSampleRate = Globals.Audio.SampleRate;

        samplesPerLdsUpdate = 2 * (audioSampleRate / ldsUpdate2Rate);
        samplesPerLdsUpdateFrac = 2 * (audioSampleRate % ldsUpdate2Rate);

        volumeFactorTable[0] = 0;
        for (int i = 1; i < 256; ++i)
            volumeFactorTable[i] = TO_FIXED(MathF.Pow(10, (255 - i) * (-volumeRange / (20.0f * 255))));

        Opl.opl_init();

        Globals.Audio.SetPaused(false); // unpause

        return true;
    }

    private static void audioCallback(Span<short> stream)
    {
        int samplesCount = stream.Length;
        if (samplesCount == 0)
            return;

        fixed (short* samples = stream)
        {
            if (!music_disabled && !music_stopped)
            {
                short* remaining = samples;
                int remainingCount = samplesCount;
                while (remainingCount > 0)
                {
                    if (samplesUntilLdsUpdate == 0)
                    {
                        Lds.lds_update();

                        samplesUntilLdsUpdate += samplesPerLdsUpdate;
                        samplesUntilLdsUpdateFrac += samplesPerLdsUpdateFrac;
                        if (samplesUntilLdsUpdateFrac >= ldsUpdate2Rate)
                        {
                            samplesUntilLdsUpdate += 1;
                            samplesUntilLdsUpdateFrac -= ldsUpdate2Rate;
                        }
                    }

                    int count = Math.Min(samplesUntilLdsUpdate, remainingCount);

                    Opl.opl_update(remaining, count);

                    remaining += count;
                    remainingCount -= count;
                    samplesUntilLdsUpdate -= count;
                }
            }
            else
            {
                for (int i = 0; i < samplesCount; ++i)
                    samples[i] = 0;
            }

            int musicVolumeFactor = volumeFactorTable[musicVolume];
            musicVolumeFactor *= 2; // OPL emulator is too quiet

            if (samples_disabled && !music_disabled)
            {
                short* remaining = samples;
                int remainingCount = samplesCount;
                while (remainingCount > 0)
                {
                    int sample = *remaining * musicVolumeFactor;
                    sample = FIXED_TO_INT(sample);
                    *remaining = (short)Math.Min(Math.Max(short.MinValue, sample), short.MaxValue);
                    remaining += 1;
                    remainingCount -= 1;
                }
            }
            else if (!samples_disabled)
            {
                int sampleVolumeFactor = volumeFactorTable[sampleVolume];
                Span<int> sampleVolumeFactors = stackalloc int[CHANNEL_VOLUME_LEVELS];
                for (int i = 0; i < CHANNEL_VOLUME_LEVELS; ++i)
                    sampleVolumeFactors[i] = sampleVolumeFactor * (i + 1) / CHANNEL_VOLUME_LEVELS;

                short* remaining = samples;
                int remainingCount = samplesCount;
                while (remainingCount > 0)
                {
                    int sample = *remaining * musicVolumeFactor;

                    for (int i = 0; i < CHANNEL_COUNT; ++i)
                    {
                        if (channelSampleCount[i] > 0)
                        {
                            sample += *channelSamples[i] * sampleVolumeFactors[channelVolume[i]];
                            channelSamples[i] += 1;
                            channelSampleCount[i] -= 1;
                        }
                    }

                    sample = FIXED_TO_INT(sample);
                    *remaining = (short)Math.Min(Math.Max(short.MinValue, sample), short.MaxValue);
                    remaining += 1;
                    remainingCount -= 1;
                }
            }
        }
    }

    public static void deinit_audio()
    {
        if (audio_disabled)
            return;

        Globals.Audio.SetPaused(true);

        for (int i = 0; i < CHANNEL_COUNT; ++i)
            channelSampleCount[i] = 0;

        Lds.lds_free();
    }

    public static void load_music()
    {
        if (music_file == null)
        {
            music_file = CFile.dir_fopen_die(CFile.data_dir(), "music.mus", "rb");

            song_count = CFile.read_u16(music_file);

            song_offset = new uint[song_count + 1];
            for (int i = 0; i < song_count; ++i)
                song_offset[i] = CFile.read_u32(music_file);

            song_offset[song_count] = (uint)CFile.ftell_eof(music_file);
        }
    }

    private static void load_song(uint song_num)
    {
        if (song_num < song_count)
        {
            uint song_size = song_offset[song_num + 1] - song_offset[song_num];
            Lds.lds_load(music_file!, song_offset[song_num], song_size);
        }
        else
        {
            Console.Error.WriteLine($"warning: failed to load song {song_num + 1}");
        }
    }

    public static void play_song(uint song_num)
    {
        if (audio_disabled)
            return;

        if (song_num != song_playing)
        {
            Globals.Audio.Lock();
            music_stopped = true;
            Globals.Audio.Unlock();

            load_song(song_num);
            song_playing = song_num;
        }

        Globals.Audio.Lock();
        music_stopped = false;
        Globals.Audio.Unlock();
    }

    public static void restart_song()
    {
        if (audio_disabled) return;
        Globals.Audio.Lock();
        Lds.lds_rewind();
        music_stopped = false;
        Globals.Audio.Unlock();
    }

    public static void stop_song()
    {
        if (audio_disabled) return;
        Globals.Audio.Lock();
        music_stopped = true;
        Globals.Audio.Unlock();
    }

    public static void fade_song()
    {
        if (audio_disabled) return;
        Globals.Audio.Lock();
        Lds.lds_fade(1);
        Globals.Audio.Unlock();
    }

    public static void set_volume(byte musicVolume_, byte sampleVolume_)
    {
        if (audio_disabled) return;
        Globals.Audio.Lock();
        musicVolume = musicVolume_;
        sampleVolume = sampleVolume_;
        Globals.Audio.Unlock();
    }

    public static void multiSamplePlay(short* samples, nuint sampleCount, byte chan, byte vol)
    {
        System.Diagnostics.Debug.Assert(chan < CHANNEL_COUNT);
        System.Diagnostics.Debug.Assert(vol < CHANNEL_VOLUME_LEVELS);

        if (audio_disabled || samples_disabled)
            return;

        Globals.Audio.Lock();
        channelSamples[chan] = samples;
        channelSampleCount[chan] = sampleCount;
        channelVolume[chan] = vol;
        Globals.Audio.Unlock();
    }
}
