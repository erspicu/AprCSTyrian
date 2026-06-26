using System.Runtime.InteropServices;
using AprCSTyrian.Core.Ports;
using SDL2;

namespace AprCSTyrian.App.Sdl;

/// <summary>
/// <see cref="IAudioBackend"/> 的 SDL2 實作。以 callback 模式向 <see cref="IAudioSource"/>
/// 索取已混好的 16-bit PCM。裝置在 <see cref="Start"/> 時才開啟（沒用到音訊就不碰裝置）。
/// </summary>
internal sealed class SdlAudio : IAudioBackend
{
    private uint _device;
    private IAudioSource? _source;
    // callback 委派必須持有參考，避免被 GC 回收造成原生端野指標。
    private SDL.SDL_AudioCallback? _callback;

    public int SampleRate { get; private set; }
    public int Channels { get; private set; }

    public SdlAudio(int sampleRate = 44100, int channels = 2)
    {
        SampleRate = sampleRate;
        Channels = channels;
    }

    public void Start(IAudioSource source)
    {
        _source = source;
        if (SDL.SDL_WasInit(SDL.SDL_INIT_AUDIO) == 0)
            SDL.SDL_InitSubSystem(SDL.SDL_INIT_AUDIO);

        _callback = AudioCallback;
        var desired = new SDL.SDL_AudioSpec
        {
            freq = SampleRate,
            format = SDL.AUDIO_S16SYS,
            channels = (byte)Channels,
            samples = 1024,
            callback = _callback,
        };

        _device = SDL.SDL_OpenAudioDevice(null!, 0, ref desired, out SDL.SDL_AudioSpec obtained, 0);
        if (_device == 0)
            throw new InvalidOperationException($"SDL_OpenAudioDevice 失敗: {SDL.SDL_GetError()}");

        SampleRate = obtained.freq;
        Channels = obtained.channels;
        SDL.SDL_PauseAudioDevice(_device, 0); // 開始播放
    }

    public void SetPaused(bool paused)
    {
        if (_device != 0)
            SDL.SDL_PauseAudioDevice(_device, paused ? 1 : 0);
    }

    private void AudioCallback(IntPtr userdata, IntPtr stream, int len)
    {
        int sampleCount = len / sizeof(short);
        unsafe
        {
            var buffer = new Span<short>((void*)stream, sampleCount);
            if (_source is not null)
                _source.GenerateSamples(buffer);
            else
                buffer.Clear();
        }
    }

    public void Dispose()
    {
        if (_device != 0)
        {
            SDL.SDL_CloseAudioDevice(_device);
            _device = 0;
        }
    }
}
