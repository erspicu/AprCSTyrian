using AprCSTyrian.Core.Ports;
using ScalexFilter;
using SDL2;

namespace AprCSTyrian.App.Sdl;

/// <summary>
/// <see cref="IVideoBackend"/> 的 SDL2 實作：開一個固定尺寸視窗 + renderer + 一張 streaming texture，
/// 把 Core 的 320×200 indexed 影格轉成 ARGB，經選定的放大濾鏡（None/Scale2x/Scale3x）後呈現。
/// 濾鏡可由 opentyrian.cfg 的 video[scaler] 設定（透過 <see cref="SetScaler"/>）。
/// 視窗尺寸固定，濾鏡只改變中介解析度，再由 renderer 以最近鄰拉伸填滿視窗
/// （Scale3x 時邏輯=視窗，1:1 無拉伸）。
/// </summary>
internal sealed class SdlVideo : IVideoBackend
{
    private readonly IntPtr _window;
    private readonly IntPtr _renderer;
    private IntPtr _texture;

    private readonly uint[] _palette = new uint[256];   // 0xAARRGGBB
    private readonly uint[] _rgbBuffer;                  // Width*Height ARGB（原始）

    private int _filterScale = 3;                        // 1=None, 2=Scale2x, 3=Scale3x
    private string _scalerName = "Scale3x";
    private uint[] _scaledBuffer = System.Array.Empty<uint>();
    private int _scaledW, _scaledH;

    public int Width { get; }
    public int Height { get; }
    public string ScalerName => _scalerName;

    public SdlVideo(string title, int width, int height, int scale)
    {
        Width = width;
        Height = height;
        _rgbBuffer = new uint[width * height];

        // 像素藝術：最近鄰拉伸（非整數倍時才會用到）。
        SDL.SDL_SetHint(SDL.SDL_HINT_RENDER_SCALE_QUALITY, "0");

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

        ApplyScaler(_filterScale); // 預設 Scale3x
    }

    /// <summary>依 _filterScale 重建中介緩衝 + texture + 邏輯尺寸。</summary>
    private void ApplyScaler(int filterScale)
    {
        _filterScale = filterScale;
        _scaledW = Width * filterScale;
        _scaledH = Height * filterScale;
        _scaledBuffer = new uint[_scaledW * _scaledH];

        // 邏輯尺寸 = 中介解析度；renderer 拉伸到固定視窗（Scale3x 時=視窗即 1:1）。
        SDL.SDL_RenderSetLogicalSize(_renderer, _scaledW, _scaledH);

        if (_texture != IntPtr.Zero)
            SDL.SDL_DestroyTexture(_texture);
        _texture = SDL.SDL_CreateTexture(
            _renderer,
            SDL.SDL_PIXELFORMAT_ARGB8888,
            (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
            _scaledW, _scaledH);
        if (_texture == IntPtr.Zero)
            throw new InvalidOperationException($"SDL_CreateTexture 失敗: {SDL.SDL_GetError()}");
    }

    public void SetScaler(string name)
    {
        int fs = name?.Trim().ToLowerInvariant() switch
        {
            "none" or "nearest" or "no scaling" => 1,
            "scale2x" => 2,
            "scale3x" => 3,
            _ => 0, // 未知：維持現值
        };
        if (fs == 0)
            return;

        _scalerName = fs == 1 ? "None" : fs == 2 ? "Scale2x" : "Scale3x";
        if (fs != _filterScale)
            ApplyScaler(fs);
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
            fixed (uint* src = _rgbBuffer)
            fixed (uint* dst = _scaledBuffer)
            {
                switch (_filterScale)
                {
                    case 2: ScalexTool.toScale2x_dx(src, Width, Height, dst); break;
                    case 3: ScalexTool.toScale3x_dx(src, Width, Height, dst); break;
                    default: // None：直接複製 320×200，由 renderer 最近鄰拉伸
                        Buffer.MemoryCopy(src, dst, (long)count * sizeof(uint), (long)count * sizeof(uint));
                        break;
                }
                SDL.SDL_UpdateTexture(_texture, IntPtr.Zero, (IntPtr)dst, _scaledW * sizeof(uint));
            }
        }

        SDL.SDL_RenderClear(_renderer);
        SDL.SDL_RenderCopy(_renderer, _texture, IntPtr.Zero, IntPtr.Zero);
        SDL.SDL_RenderPresent(_renderer);
    }

    public void MapWindowToScreen(ref int x, ref int y)
    {
        // 邏輯尺寸為 _filterScale 倍，換回遊戲 320×200 座標。
        SDL.SDL_RenderWindowToLogical(_renderer, x, y, out float lx, out float ly);
        x = (int)lx / _filterScale;
        y = (int)ly / _filterScale;
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
