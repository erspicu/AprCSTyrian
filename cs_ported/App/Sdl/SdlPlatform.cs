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
    private readonly SdlJoystick _joystick;

    public IVideoBackend Video => _video;
    public IAudioBackend Audio => _audio;
    public IInputBackend Input => _input;
    public IClock Clock => _clock;
    public IFileSystem Files => _files;
    public IJoystickBackend Joystick => _joystick;

    public SdlPlatform(string windowTitle, int scale, string dataRoot, string userRoot)
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_EVENTS) != 0)
            throw new InvalidOperationException($"SDL_Init 失敗: {SDL.SDL_GetError()}");

        _video = new SdlVideo(windowTitle, VgaScreen.Width, VgaScreen.Height, scale);
        // Tyrian/Loudness 以 44.1kHz 單聲道 S16 輸出（OPL/混音為 mono）。
        _audio = new SdlAudio(11025 * 4, 1);
        _input = new SdlInput();
        _clock = new SdlClock();
        _files = new PhysicalFileSystem(dataRoot, userRoot);
        _joystick = new SdlJoystick();
    }

    public void Dispose()
    {
        _joystick.Dispose();
        _audio.Dispose();
        _video.Dispose();
        SDL.SDL_Quit();
    }
}
