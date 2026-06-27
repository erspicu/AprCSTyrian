using AprCSTyrian.Core.Ports;
using ScalexFilter;
using SDL2;

namespace AprCSTyrian.App.Sdl;

/// <summary>
/// <see cref="IVideoBackend"/> 的 SDL2 實作：視窗 + renderer + streaming texture，
/// 把 Core 的 320×200 indexed 影格轉成 ARGB，經兩段式放大管線（First → Second）後呈現。
/// 每段可為 1x/none、NN2x、Scale2x、Scale3x、xBRZ2x、xBRZ3x（由 opentyrian.cfg / Graphics 選單設定）。
/// 切換濾鏡時安全釋放/重建緩衝、texture、視窗（xBRZ 緩衝由 initTable grow-only 管理）。
/// </summary>
internal sealed class SdlVideo : IVideoBackend
{
    private enum Step { Nearest2, Scale2x, Scale3x, Xbrz2, Xbrz3 }

    private readonly IntPtr _window;
    private readonly IntPtr _renderer;
    private IntPtr _texture;

    private readonly uint[] _palette = new uint[256];   // 0xAARRGGBB
    private readonly uint[] _rgbBuffer;                  // Width*Height ARGB（原始）

    // 預設：1x + none = 原始 320×200（無濾鏡）。
    private string _first = "1x";
    private string _second = "none";
    private Step[] _pipeline = System.Array.Empty<Step>();
    private uint[] _bufA = System.Array.Empty<uint>();   // ping-pong 中介緩衝（最終尺寸）
    private uint[] _bufB = System.Array.Empty<uint>();
    private int _finalW, _finalH;                        // 最終 texture / 邏輯尺寸

    private volatile bool _shotPending;

    public int Width { get; }
    public int Height { get; }
    public string FirstFilter => _first;
    public string SecondFilter => _second;

    private static int StepFactor(Step s) => (s == Step.Scale3x || s == Step.Xbrz3) ? 3 : 2;

    /// <summary>濾鏡名稱 → Step；1x/none/未知 → null（無作用）。</summary>
    private static Step? ParseFilter(string? name)
    {
        switch (name?.Trim().ToLowerInvariant())
        {
            case "nn2x": case "nearest2x": case "nearest": return Step.Nearest2;
            case "scale2x": return Step.Scale2x;
            case "scale3x": case "scale3": return Step.Scale3x;
            case "xbrz2x": return Step.Xbrz2;
            case "xbrz3x": return Step.Xbrz3;
            default: return null; // 1x / none / 未知
        }
    }

    private static string CanonFirst(string? name) => ParseFilter(name) switch
    {
        Step.Nearest2 => "NN2x", Step.Scale2x => "Scale2x", Step.Scale3x => "Scale3x",
        Step.Xbrz2 => "xBRZ2x", Step.Xbrz3 => "xBRZ3x", _ => "1x",
    };
    private static string CanonSecond(string? name) => ParseFilter(name) switch
    {
        Step.Nearest2 => "NN2x", Step.Scale2x => "Scale2x", Step.Scale3x => "Scale3x",
        Step.Xbrz2 => "xBRZ2x", Step.Xbrz3 => "xBRZ3x", _ => "none",
    };

    public SdlVideo(string title, int width, int height, int scale)
    {
        Width = width;
        Height = height;
        _rgbBuffer = new uint[width * height];

        SDL.SDL_SetHint(SDL.SDL_HINT_RENDER_SCALE_QUALITY, "0"); // 像素藝術：最近鄰

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

        ApplyPipeline();
    }

    /// <summary>依 _first/_second 重建管線 + 中介緩衝 + texture + 邏輯尺寸 + 視窗大小。</summary>
    private void ApplyPipeline()
    {
        var steps = new System.Collections.Generic.List<Step>(2);
        var f1 = ParseFilter(_first); if (f1.HasValue) steps.Add(f1.Value);
        var f2 = ParseFilter(_second); if (f2.HasValue) steps.Add(f2.Value);
        _pipeline = steps.ToArray();

        // 計算最終尺寸；並對 xBRZ 段「先把緩衝配置/擴充到該輸入尺寸」（grow-only，安全）。
        int w = Width, h = Height;
        foreach (var s in _pipeline)
        {
            if (s == Step.Xbrz2 || s == Step.Xbrz3)
                XBRz_speed.HS_XBRz.initTable(w, h); // 確保容量；切換時不會用到過小/已釋放緩衝
            w *= StepFactor(s);
            h *= StepFactor(s);
        }
        _finalW = w;
        _finalH = h;

        _bufA = new uint[_finalW * _finalH];
        _bufB = new uint[_finalW * _finalH];

        SDL.SDL_RenderSetLogicalSize(_renderer, _finalW, _finalH);

        if (_texture != IntPtr.Zero)
        {
            SDL.SDL_DestroyTexture(_texture);
            _texture = IntPtr.Zero;
        }
        _texture = SDL.SDL_CreateTexture(
            _renderer, SDL.SDL_PIXELFORMAT_ARGB8888,
            (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
            _finalW, _finalH);
        if (_texture == IntPtr.Zero)
            throw new InvalidOperationException($"SDL_CreateTexture 失敗: {SDL.SDL_GetError()}");

        // 視窗 = 最終解析度（至少維持 3x=960×600 以免太小），不超過桌面可用區域。
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

    public void SetFilters(string first, string second)
    {
        _first = CanonFirst(first);
        _second = CanonSecond(second);
        ApplyPipeline();
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

    public void SaveScreenshot() => _shotPending = true;

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
                d0[dx] = p; d0[dx + 1] = p; d1[dx] = p; d1[dx + 1] = p;
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
                        case Step.Xbrz2:
                            XBRz_speed.HS_XBRz.initTable(cw, ch);          // 設定本次輸入尺寸（安全）
                            XBRz_speed.HS_XBRz.ScaleImage(cur, outp, 2);
                            break;
                        case Step.Xbrz3:
                            XBRz_speed.HS_XBRz.initTable(cw, ch);
                            XBRz_speed.HS_XBRz.ScaleImage(cur, outp, 3);
                            break;
                    }
                    cur = outp;
                    cw *= StepFactor(s);
                    ch *= StepFactor(s);
                }

                // cur 為最終結果（無濾鏡時 = 原始 320×200）。
                if (_shotPending)
                {
                    _shotPending = false;
                    TrySaveScreenshot(cur, cw, ch);
                }

                SDL.SDL_UpdateTexture(_texture, IntPtr.Zero, (IntPtr)cur, cw * sizeof(uint));
            }
        }

        SDL.SDL_RenderClear(_renderer);
        SDL.SDL_RenderCopy(_renderer, _texture, IntPtr.Zero, IntPtr.Zero);
        SDL.SDL_RenderPresent(_renderer);
    }

    private static unsafe void TrySaveScreenshot(uint* buf, int w, int h)
    {
        try
        {
            string dir = System.IO.Path.Combine(AppContext.BaseDirectory, "screenshot");
            System.IO.Directory.CreateDirectory(dir);
            string name = DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png";
            PngWriter.WriteArgb(System.IO.Path.Combine(dir, name), buf, w, h);
        }
        catch { /* 截圖失敗不影響遊戲 */ }
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
