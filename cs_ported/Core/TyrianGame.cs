using AprCSTyrian.Core.Ports;

namespace AprCSTyrian.Core;

/// <summary>
/// 遊戲核心進入點。目前為移植骨架：透過 Ports 顯示一個動態測試畫面，
/// 用以驗證 Core ↔ Adapter (SDL) 的影格/調色盤/輸入管線是否打通。
/// 之後將逐步把 sources/ 的 C 模組（opentyr/mainint/tyrian2…）移植進來。
/// </summary>
public sealed class TyrianGame
{
    private readonly IGamePlatform _platform;
    private readonly byte[] _framebuffer = new byte[VgaScreen.PixelCount];
    private readonly Color[] _palette = new Color[256];

    public TyrianGame(IGamePlatform platform)
    {
        _platform = platform;
    }

    /// <summary>執行主迴圈，直到使用者要求離開。</summary>
    public void Run()
    {
        BuildTestPalette();
        _platform.Video.SetPalette(_palette);

        var input = _platform.Input;
        var clock = _platform.Clock;

        uint frame = 0;
        while (true)
        {
            input.Poll();
            if (input.QuitRequested || input.IsKeyDown(GameKey.Escape))
                break;

            RenderTestPattern(frame);
            _platform.Video.Present(_framebuffer);

            frame++;
            clock.Delay(16); // ~60 FPS（暫以固定延遲，之後改為原版的計時節奏）
        }
    }

    /// <summary>建立一條 HSV 色環調色盤，方便肉眼確認調色盤管線正確。</summary>
    private void BuildTestPalette()
    {
        _palette[0] = new Color(0, 0, 0);
        for (int i = 1; i < 256; i++)
        {
            double h = (i - 1) / 255.0 * 360.0;
            (byte r, byte g, byte b) = HsvToRgb(h, 1.0, 1.0);
            _palette[i] = new Color(r, g, b);
        }
    }

    /// <summary>畫一個會隨 frame 捲動的同心/斜紋圖樣，證明每幀更新有效。</summary>
    private void RenderTestPattern(uint frame)
    {
        int t = (int)frame;
        for (int y = 0; y < VgaScreen.Height; y++)
        {
            int rowBase = y * VgaScreen.Width;
            for (int x = 0; x < VgaScreen.Width; x++)
            {
                int v = (x + y + t) ^ ((x - y) >> 1);
                _framebuffer[rowBase + x] = (byte)(1 + (v & 0xFF) % 255);
            }
        }
    }

    private static (byte, byte, byte) HsvToRgb(double h, double s, double v)
    {
        double c = v * s;
        double hp = h / 60.0;
        double xx = c * (1 - Math.Abs(hp % 2 - 1));
        double r = 0, g = 0, b = 0;
        switch ((int)hp)
        {
            case 0: r = c; g = xx; break;
            case 1: r = xx; g = c; break;
            case 2: g = c; b = xx; break;
            case 3: g = xx; b = c; break;
            case 4: r = xx; b = c; break;
            default: r = c; b = xx; break;
        }
        double m = v - c;
        return ((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
    }
}
