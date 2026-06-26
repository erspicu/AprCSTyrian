using AprCSTyrian.Core;
using AprCSTyrian.Core.Ports;
using SDL2;

namespace AprCSTyrian.App.Sdl;

/// <summary>
/// SDL 平台聚合：負責 SDL 全域生命週期 (SDL_Init/SDL_Quit) 並組裝各 Adapter。
/// </summary>
internal sealed class SdlPlatform : IGamePlatform, IDisposable
{
    private readonly SdlVideo _video;
    private readonly SdlAudio _audio;
    private readonly SdlInput _input;
    private readonly SdlClock _clock;
    private readonly PhysicalFileSystem _files;

    public IVideoBackend Video => _video;
    public IAudioBackend Audio => _audio;
    public IInputBackend Input => _input;
    public IClock Clock => _clock;
    public IFileSystem Files => _files;

    public SdlPlatform(string windowTitle, int scale, string dataRoot, string userRoot)
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_EVENTS) != 0)
            throw new InvalidOperationException($"SDL_Init 失敗: {SDL.SDL_GetError()}");

        _video = new SdlVideo(windowTitle, VgaScreen.Width, VgaScreen.Height, scale);
        _audio = new SdlAudio();
        _input = new SdlInput();
        _clock = new SdlClock();
        _files = new PhysicalFileSystem(dataRoot, userRoot);
    }

    public void Dispose()
    {
        _audio.Dispose();
        _video.Dispose();
        SDL.SDL_Quit();
    }
}
