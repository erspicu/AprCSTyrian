using AprCSTyrian.Core.Ports;

namespace AprCSTyrian.Core;

/// <summary>
/// 遊戲核心進入點（移植骨架）。目前驅動已移植的 video/palette/vga256d 管線：
/// init_video → 載入/建立調色盤 → 以繪圖原語作畫到 VGAScreen → JE_showVGA。
/// 偵測不到 Tyrian 資料檔時退回合成調色盤，確保視窗仍可顯示。
/// 之後將以 opentyr.c 的 main/titleScreen/JE_main 取代本暫時迴圈。
/// </summary>
public sealed class TyrianGame
{
    private readonly IGamePlatform _platform;
    private readonly string _dataDir;
    private readonly string _userDir;

    public TyrianGame(IGamePlatform platform, string dataDir, string userDir)
    {
        _platform = platform;
        _dataDir = dataDir;
        _userDir = userDir;
    }

    /// <summary>執行主迴圈，直到使用者要求離開。</summary>
    public unsafe void Run()
    {
        Globals.Init(_platform, _dataDir, _userDir);

        Video.init_video();
        try
        {
            bool dataFound = CFile.data_dir().Length != 0;
            if (dataFound)
            {
                // 載入真正的 Tyrian 調色盤並套用第一組。
                Palette.JE_loadPals();
                Palette.set_palette(Palette.palettes[0], 0, 255);
            }
            else
            {
                // 無資料檔：以合成調色盤示意，仍走 set_palette → 上傳管線。
                BuildSyntheticPalette();
                Palette.set_palette(Palette.colors, 0, 255);
            }

            var input = _platform.Input;
            uint frame = 0;
            while (true)
            {
                input.Poll();
                if (input.QuitRequested || input.IsKeyDown(GameKey.Escape))
                    break;

                RenderTestPattern(frame);
                Video.JE_showVGA();

                frame++;
                Nortsong.setFrameCount(1);
                Nortsong.delayUntilElapsed();
            }
        }
        finally
        {
            Video.deinit_video();
        }
    }

    /// <summary>填入一條 HSV 色環到 Palette.colors（資料檔缺席時的示意調色盤）。</summary>
    private static void BuildSyntheticPalette()
    {
        Palette.colors[0] = new SDL_Color(0, 0, 0);
        for (int i = 1; i < 256; i++)
        {
            double h = (i - 1) / 255.0 * 360.0;
            (byte r, byte g, byte b) = HsvToRgb(h, 1.0, 1.0);
            Palette.colors[i] = new SDL_Color(r, g, b);
        }
    }

    /// <summary>用已移植的繪圖原語在 VGAScreen 上畫動態圖樣，驗證整條管線。</summary>
    private static unsafe void RenderTestPattern(uint frame)
    {
        var screen = Video.VGAScreen;
        int t = (int)frame;
        byte* px = screen.pixels;
        for (int y = 0; y < Video.vga_height; y++)
        {
            int rowBase = y * screen.pitch;
            for (int x = 0; x < Video.vga_width; x++)
            {
                int v = (x + y + t) ^ ((x - y) >> 1);
                px[rowBase + x] = (byte)(1 + (v & 0xFF) % 255);
            }
        }

        // 疊一個會移動的方框，順便驗證 vga256d 原語。
        int bx = 20 + (t % 240);
        Vga256d.JE_rectangle(screen, bx, 40, bx + 60, 160, 255);
        Vga256d.fill_rectangle_wh(screen, bx + 5, 45, 50, 20, 200);
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
