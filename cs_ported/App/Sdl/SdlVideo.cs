using AprCSTyrian.Core.Ports;
using ScalexFilter;
using SDL2;

namespace AprCSTyrian.App.Sdl;

/// <summary>
/// <see cref="IVideoBackend"/> 的 SDL2 實作：開一個視窗 + renderer + 一張 streaming texture，
/// 把 Core 的 320×200 indexed 影格轉成 ARGB，經一條「放大濾鏡管線」後呈現。
/// 管線可串接多段（Nearest2x / Scale2x / Scale3x / xBRZ2x），由 opentyrian.cfg 的
/// video[scaler] 設定（透過 <see cref="SetScaler"/>）。視窗放大到最終解析度（超過桌面則等比縮）。
/// </summary>
internal sealed class SdlVideo : IVideoBackend
{
    private enum Step { Nearest2, Scale2x, Scale3x, Xbrz2 }

    private readonly IntPtr _window;
    private readonly IntPtr _renderer;
    private IntPtr _texture;

    private readonly uint[] _palette = new uint[256];   // 0xAARRGGBB
    private readonly uint[] _rgbBuffer;                  // Width*Height ARGB（原始）

    // 預設管線：Nearest 2x → Scale3x = 6x（與 Scale3x+xBRZ2x 互相比較用）。
    private Step[] _pipeline = { Step.Nearest2, Step.Scale3x };
    private string _scalerName = "Nearest2x+Scale3x";
    private uint[] _bufA = System.Array.Empty<uint>();   // ping-pong 中介緩衝（最終尺寸）
    private uint[] _bufB = System.Array.Empty<uint>();
    private int _finalW, _finalH;                        // 最終 texture / 邏輯尺寸
    private int _xbrzInW, _xbrzInH;                      // xBRZ 輸入尺寸（initTable 用）
    private bool _xbrzInited;

    public int Width { get; }
    public int Height { get; }
    public string ScalerName => _scalerName;

    private static int StepFactor(Step s) => s == Step.Scale3x ? 3 : 2;

    public SdlVideo(string title, int width, int height, int scale)
    {
        Width = width;
        Height = height;
        _rgbBuffer = new uint[width * height];

        // 像素藝術：最近鄰拉伸（非整數倍才會用到）。
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

        ApplyPipeline(_pipeline, _scalerName);
    }

    /// <summary>依放大管線重建中介緩衝 + texture + 邏輯尺寸 + 視窗大小。</summary>
    private void ApplyPipeline(Step[] pipeline, string name)
    {
        _pipeline = pipeline;
        _scalerName = name;

        int w = Width, h = Height;
        foreach (var s in pipeline)
        {
            if (s == Step.Xbrz2) { _xbrzInW = w; _xbrzInH = h; }
            w *= StepFactor(s);
            h *= StepFactor(s);
        }
        _finalW = w;
        _finalH = h;

        _bufA = new uint[_finalW * _finalH];
        _bufB = new uint[_finalW * _finalH];

        if (System.Array.IndexOf(pipeline, Step.Xbrz2) >= 0 && !_xbrzInited)
        {
            // xBRZ 查表/緩衝以「輸入尺寸」一次性配置（initTable 內有 once 守衛）。
            XBRz_speed.HS_XBRz.initTable(_xbrzInW, _xbrzInH);
            _xbrzInited = true;
        }

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

        // 視窗 = 最終解析度（顯示完整細節），但至少維持 3x（960×600），且不超過桌面可用區域。
        int tW = Math.Max(Width * 3, _finalW), tH = Math.Max(Height * 3, _finalH);
        if (SDL.SDL_GetDisplayUsableBounds(0, out SDL.SDL_Rect ub) == 0 && ub.w > 0 && ub.h > 0)
        {
            double k = Math.Min(1.0, Math.Min((double)ub.w / tW, (double)ub.h / tH));
            tW = (int)(tW * k);
            tH = (int)(tH * k);
        }
        SDL.SDL_SetWindowSize(_window, tW, tH);
        SDL.SDL_SetWindowPosition(_window, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED);
    }

    public void SetScaler(string name)
    {
        Step[] p; string canon;
        switch (name?.Trim().ToLowerInvariant())
        {
            case "none": case "nearest": case "no scaling": p = System.Array.Empty<Step>(); canon = "None"; break;
            case "scale2x": p = new[] { Step.Scale2x }; canon = "Scale2x"; break;
            case "scale3x": p = new[] { Step.Scale3x }; canon = "Scale3x"; break;
            case "scale3x+xbrz2x": case "xbrz6x": p = new[] { Step.Scale3x, Step.Xbrz2 }; canon = "Scale3x+xBRZ2x"; break;
            case "nearest2x+scale3x": case "6x": p = new[] { Step.Nearest2, Step.Scale3x }; canon = "Nearest2x+Scale3x"; break;
            default: return; // 未知：維持現值
        }
        ApplyPipeline(p, canon);
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

    /// <summary>最近鄰 2x：每像素複製成 2×2。</summary>
    private static unsafe void Nearest2x(uint* src, int w, int h, uint* dst)
    {
        int dw = w * 2;
        for (int y = 0; y < h; y++)
        {
            uint* srow = src + y * w;
            uint* d0 = dst + (y * 2) * dw;
            uint* d1 = d0 + dw;
            for (int x = 0; x < w; x++)
            {
                uint p = srow[x];
                int dx = x * 2;
                d0[dx] = p; d0[dx + 1] = p;
                d1[dx] = p; d1[dx + 1] = p;
            }
        }
    }

    public void Present(ReadOnlySpan<byte> indexedPixels)
    {
        int count = Width * Height;
        for (int i = 0; i < count; i++)
            _rgbBuffer[i] = _palette[indexedPixels[i]];

        unsafe
        {
            fixed (uint* pSrc = _rgbBuffer)
            fixed (uint* pA = _bufA)
            fixed (uint* pB = _bufB)
            {
                uint* cur = pSrc;
                int cw = Width, ch = Height;
                foreach (var s in _pipeline)
                {
                    uint* outp = (cur == pA) ? pB : pA; // src→pA，之後 A/B 交替
                    switch (s)
                    {
                        case Step.Nearest2: Nearest2x(cur, cw, ch, outp); break;
                        case Step.Scale2x: ScalexTool.toScale2x_dx(cur, cw, ch, outp); break;
                        case Step.Scale3x: ScalexTool.toScale3x_dx(cur, cw, ch, outp); break;
                        case Step.Xbrz2: XBRz_speed.HS_XBRz.ScaleImage(cur, outp, 2); break;
                    }
                    cur = outp;
                    cw *= StepFactor(s);
                    ch *= StepFactor(s);
                }
                // cur 現為最終結果（None 時 = 原始 320×200），pitch = cw。
                SDL.SDL_UpdateTexture(_texture, IntPtr.Zero, (IntPtr)cur, cw * sizeof(uint));
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
