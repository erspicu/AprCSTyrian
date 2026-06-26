using AprCSTyrian.Core.Ports;
using SDL2;

namespace AprCSTyrian.App.Sdl;

/// <summary>
/// <see cref="IVideoBackend"/> 的 SDL2 實作：開一個視窗 + 硬體加速 renderer +
/// 一張 streaming texture，把 Core 的 indexed 影格轉成 ARGB 後放大呈現。
/// </summary>
internal sealed class SdlVideo : IVideoBackend
{
    private readonly IntPtr _window;
    private readonly IntPtr _renderer;
    private readonly IntPtr _texture;

    private readonly uint[] _palette = new uint[256];   // 0xAARRGGBB
    private readonly uint[] _rgbBuffer;                  // Width*Height ARGB

    public int Width { get; }
    public int Height { get; }

    public SdlVideo(string title, int width, int height, int scale)
    {
        Width = width;
        Height = height;
        _rgbBuffer = new uint[width * height];

        _window = SDL.SDL_CreateWindow(
            title,
            SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
            width * scale, height * scale,
            SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
        if (_window == IntPtr.Zero)
            throw new InvalidOperationException($"SDL_CreateWindow 失敗: {SDL.SDL_GetError()}");

        _renderer = SDL.SDL_CreateRenderer(
            _window, -1,
            SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
            SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
        if (_renderer == IntPtr.Zero)
            throw new InvalidOperationException($"SDL_CreateRenderer 失敗: {SDL.SDL_GetError()}");

        // 以邏輯尺寸讓 SDL 自動等比例放大；整數縮放避免模糊。
        SDL.SDL_RenderSetLogicalSize(_renderer, width, height);
        SDL.SDL_RenderSetIntegerScale(_renderer, SDL.SDL_bool.SDL_TRUE);

        _texture = SDL.SDL_CreateTexture(
            _renderer,
            SDL.SDL_PIXELFORMAT_ARGB8888,
            (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
            width, height);
        if (_texture == IntPtr.Zero)
            throw new InvalidOperationException($"SDL_CreateTexture 失敗: {SDL.SDL_GetError()}");
    }

    public void SetPalette(ReadOnlySpan<Color> palette)
    {
        int n = Math.Min(palette.Length, 256);
        for (int i = 0; i < n; i++)
        {
            Color c = palette[i];
            _palette[i] = 0xFF000000u | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;
        }
    }

    public void Present(ReadOnlySpan<byte> indexedPixels)
    {
        int count = Width * Height;
        for (int i = 0; i < count; i++)
            _rgbBuffer[i] = _palette[indexedPixels[i]];

        unsafe
        {
            fixed (uint* p = _rgbBuffer)
            {
                SDL.SDL_UpdateTexture(_texture, IntPtr.Zero, (IntPtr)p, Width * sizeof(uint));
            }
        }

        SDL.SDL_RenderClear(_renderer);
        SDL.SDL_RenderCopy(_renderer, _texture, IntPtr.Zero, IntPtr.Zero);
        SDL.SDL_RenderPresent(_renderer);
    }

    public void MapWindowToScreen(ref int x, ref int y)
    {
        SDL.SDL_RenderWindowToLogical(_renderer, x, y, out float lx, out float ly);
        x = (int)lx;
        y = (int)ly;
    }

    private bool _fullscreen;
    public void ToggleFullscreen()
    {
        _fullscreen = !_fullscreen;
        SDL.SDL_SetWindowFullscreen(_window,
            _fullscreen ? (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP : 0);
    }

    public void Dispose()
    {
        if (_texture != IntPtr.Zero) SDL.SDL_DestroyTexture(_texture);
        if (_renderer != IntPtr.Zero) SDL.SDL_DestroyRenderer(_renderer);
        if (_window != IntPtr.Zero) SDL.SDL_DestroyWindow(_window);
    }
}
