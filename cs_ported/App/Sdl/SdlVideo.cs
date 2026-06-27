using AprCSTyrian.Core.Ports;
using ScalexFilter;
using SDL2;

namespace AprCSTyrian.App.Sdl;

/// <summary>
/// <see cref="IVideoBackend"/> 的 SDL2 實作：開一個視窗 + renderer + 一張 streaming texture，
/// 把 Core 的 320×200 indexed 影格轉成 ARGB，經選定的放大濾鏡後呈現。
/// 濾鏡可由 opentyrian.cfg 的 video[scaler] 設定（透過 <see cref="SetScaler"/>）。
/// 支援兩段式：基礎放大（None/Scale2x/Scale3x）後可再串接 xBRZ 2x（Scale3x+xBRZ2x = 6x）。
/// </summary>
internal sealed class SdlVideo : IVideoBackend
{
    private readonly IntPtr _window;
    private readonly IntPtr _renderer;
    private IntPtr _texture;

    private readonly uint[] _palette = new uint[256];   // 0xAARRGGBB
    private readonly uint[] _rgbBuffer;                  // Width*Height ARGB（原始）

    private int _filterScale = 3;                        // 基礎放大：1=None, 2=Scale2x, 3=Scale3x
    private bool _xbrz2x = true;                          // 在基礎放大之上再套 xBRZ 2x（預設 Scale3x→6x）
    private string _scalerName = "Scale3x+xBRZ2x";
    private uint[] _scaledBuffer = System.Array.Empty<uint>();  // 基礎放大輸出（= xBRZ 輸入）
    private uint[] _xbrzBuffer = System.Array.Empty<uint>();    // xBRZ 2x 輸出（最終上傳 texture）
    private int _scaledW, _scaledH;                      // 基礎放大尺寸
    private int _finalW, _finalH;                        // 最終 texture / 邏輯尺寸
    private bool _xbrzInited;

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

        ApplyScaler(_filterScale, _xbrz2x); // 預設 Scale3x + xBRZ2x = 6x
    }

    /// <summary>依基礎放大倍率 + xBRZ 後處理重建中介緩衝 + texture + 邏輯尺寸 + 視窗大小。</summary>
    private void ApplyScaler(int filterScale, bool xbrz2x)
    {
        _filterScale = filterScale;
        _xbrz2x = xbrz2x;
        _scaledW = Width * filterScale;
        _scaledH = Height * filterScale;
        _scaledBuffer = new uint[_scaledW * _scaledH];

        int mul = xbrz2x ? 2 : 1;
        _finalW = _scaledW * mul;
        _finalH = _scaledH * mul;

        if (xbrz2x)
        {
            _xbrzBuffer = new uint[_finalW * _finalH];
            // xBRZ 查表/緩衝以「輸入尺寸」一次性配置（initTable 內部有 once 守衛）。
            // 僅此一種 xBRZ 模式（輸入恆為 Scale3x 輸出），故尺寸固定一致。
            if (!_xbrzInited)
            {
                XBRz_speed.HS_XBRz.initTable(_scaledW, _scaledH);
                _xbrzInited = true;
            }
        }

        // 邏輯尺寸 = 最終解析度；renderer 拉伸到視窗。
        SDL.SDL_RenderSetLogicalSize(_renderer, _finalW, _finalH);

        if (_texture != IntPtr.Zero)
            SDL.SDL_DestroyTexture(_texture);
        _texture = SDL.SDL_CreateTexture(
            _renderer,
            SDL.SDL_PIXELFORMAT_ARGB8888,
            (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
            _finalW, _finalH);
        if (_texture == IntPtr.Zero)
            throw new InvalidOperationException($"SDL_CreateTexture 失敗: {SDL.SDL_GetError()}");

        // 視窗放大到最終解析度（顯示 xBRZ 6x 細節）；但不超過桌面可用區域，
        // 避免在 1080p 等螢幕上視窗比螢幕高、標題列跑出畫面外。超出時等比例縮小，
        // 由 renderer 將最終邏輯尺寸縮進視窗。
        int winW = _finalW, winH = _finalH;
        if (SDL.SDL_GetDisplayUsableBounds(0, out SDL.SDL_Rect ub) == 0 && ub.w > 0 && ub.h > 0)
        {
            double k = Math.Min(1.0, Math.Min((double)ub.w / winW, (double)ub.h / winH));
            winW = (int)(winW * k);
            winH = (int)(winH * k);
        }
        SDL.SDL_SetWindowSize(_window, winW, winH);
        SDL.SDL_SetWindowPosition(_window, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED);
    }

    public void SetScaler(string name)
    {
        int fs; bool xbrz;
        switch (name?.Trim().ToLowerInvariant())
        {
            case "none": case "nearest": case "no scaling": fs = 1; xbrz = false; break;
            case "scale2x": fs = 2; xbrz = false; break;
            case "scale3x": fs = 3; xbrz = false; break;
            case "scale3x+xbrz2x": case "xbrz6x": case "6x": fs = 3; xbrz = true; break;
            default: return; // 未知：維持現值
        }

        _scalerName = xbrz ? "Scale3x+xBRZ2x"
                    : fs == 1 ? "None" : fs == 2 ? "Scale2x" : "Scale3x";
        if (fs != _filterScale || xbrz != _xbrz2x)
            ApplyScaler(fs, xbrz);
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
                    default: // None：直接複製 320×200，由 renderer 拉伸
                        Buffer.MemoryCopy(src, dst, (long)count * sizeof(uint), (long)count * sizeof(uint));
                        break;
                }

                if (_xbrz2x)
                {
                    // 第二段：把基礎放大結果(_scaledBuffer)再經 xBRZ 2x → _xbrzBuffer（最終 6x）。
                    fixed (uint* fin = _xbrzBuffer)
                    {
                        XBRz_speed.HS_XBRz.ScaleImage(dst, fin, 2);
                        SDL.SDL_UpdateTexture(_texture, IntPtr.Zero, (IntPtr)fin, _finalW * sizeof(uint));
                    }
                }
                else
                {
                    SDL.SDL_UpdateTexture(_texture, IntPtr.Zero, (IntPtr)dst, _finalW * sizeof(uint));
                }
            }
        }

        SDL.SDL_RenderClear(_renderer);
        SDL.SDL_RenderCopy(_renderer, _texture, IntPtr.Zero, IntPtr.Zero);
        SDL.SDL_RenderPresent(_renderer);
    }

    public void MapWindowToScreen(ref int x, ref int y)
    {
        // 邏輯尺寸為最終放大倍率，換回遊戲 320×200 座標（_finalW/Width = 總倍率）。
        SDL.SDL_RenderWindowToLogical(_renderer, x, y, out float lx, out float ly);
        x = (int)lx / (_finalW / Width);
        y = (int)ly / (_finalH / Height);
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
