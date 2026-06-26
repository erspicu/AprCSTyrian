namespace AprCSTyrian.Core;

/// <summary>
/// Core 自有的最小 SDL 相容型別（與真正 SDL 同名同欄位，但**不依賴 SDL2**），
/// 讓繪圖相關 .c 的 <c>SDL_Surface*</c>/<c>SDL_Color</c>/<c>SDL_Rect</c> 能逐行對照移植。
/// 8-bit indexed surface，最終由 video 模組橋接到 <see cref="Ports.IVideoBackend"/>。
/// </summary>
internal struct SDL_Color
{
    public byte r, g, b, unused;

    public SDL_Color(byte r, byte g, byte b) { this.r = r; this.g = g; this.b = b; unused = 0; }
}

internal struct SDL_Rect
{
    public int x, y, w, h;

    public SDL_Rect(int x, int y, int w, int h) { this.x = x; this.y = y; this.w = w; this.h = h; }
}

/// <summary>對應 SDL_Surface（僅保留 8-bit indexed 所需欄位）。pixels 為非託管記憶體。</summary>
internal sealed unsafe class SDL_Surface
{
    public byte* pixels;
    public int pitch;
    public int w;
    public int h;
    private bool _ownsPixels;

    /// <summary>建立 w×h 的 8-bit indexed surface（pixels 清零，pitch=w）。</summary>
    public static SDL_Surface Create(int w, int h)
    {
        var s = new SDL_Surface { w = w, h = h, pitch = w, _ownsPixels = true };
        s.pixels = (byte*)CMem.calloc((nuint)(w * h), sizeof(byte));
        return s;
    }

    /// <summary>釋放像素記憶體（對應 SDL_FreeSurface）。</summary>
    public void Free()
    {
        if (_ownsPixels && pixels != null)
        {
            CMem.free(pixels);
            pixels = null;
        }
    }
}

/// <summary>對應少數用到的 SDL 函式（8-bit 版）。</summary>
internal static unsafe class Sdl
{
    /// <summary>SDL_FillRect 的 8-bit 實作：以 color 填滿（裁切後）矩形。rect=null 表整面。</summary>
    public static void SDL_FillRect(SDL_Surface dst, SDL_Rect* rect, byte color)
    {
        int x0, y0, x1, y1;
        if (rect == null)
        {
            x0 = 0; y0 = 0; x1 = dst.w; y1 = dst.h;
        }
        else
        {
            x0 = rect->x; y0 = rect->y;
            x1 = rect->x + rect->w; y1 = rect->y + rect->h;
        }

        // 裁切到 surface 範圍
        if (x0 < 0) x0 = 0;
        if (y0 < 0) y0 = 0;
        if (x1 > dst.w) x1 = dst.w;
        if (y1 > dst.h) y1 = dst.h;
        if (x0 >= x1 || y0 >= y1) return;

        int width = x1 - x0;
        for (int y = y0; y < y1; ++y)
        {
            byte* row = dst.pixels + y * dst.pitch + x0;
            new Span<byte>(row, width).Fill(color);
        }
    }
}
