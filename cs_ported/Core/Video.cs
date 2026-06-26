namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/video.c —— 視窗/renderer/縮放由 App 的 SdlVideo adapter 負責，
/// 故 Core 端只保留 8-bit 遊戲 surfaces 與最終 flip。
/// VGAScreen 等皆為 320×200×8，與視窗大小無關；JE_showVGA 將其送往 <see cref="Globals.Video"/>。
/// </summary>
internal static unsafe class Video
{
    public const int vga_width = 320;
    public const int vga_height = 200;

    public static SDL_Surface VGAScreen = null!, VGAScreenSeg = null!;
    public static SDL_Surface VGAScreen2 = null!;
    public static SDL_Surface game_screen = null!;

    public static int fullscreen_display = -1; // -1 表示視窗模式

    public static void init_video()
    {
        // 建立遊戲繪製用的 software surfaces（皆 320×200×8）。視窗已由平台層建立。
        VGAScreen = VGAScreenSeg = SDL_Surface.Create(vga_width, vga_height);
        VGAScreen2 = SDL_Surface.Create(vga_width, vga_height);
        game_screen = SDL_Surface.Create(vga_width, vga_height);

        JE_clr256(VGAScreen);
    }

    public static void deinit_video()
    {
        VGAScreenSeg.Free();
        VGAScreen2.Free();
        game_screen.Free();
    }

    public static void JE_clr256(SDL_Surface screen)
    {
        Sdl.SDL_FillRect(screen, null, 0);
    }

    public static void JE_showVGA()
    {
        // 把 VGAScreen 的 indexed 影格送到平台呈現（放大/轉 RGB 由 adapter 處理）。
        Globals.Video.Present(new ReadOnlySpan<byte>(VGAScreen.pixels, vga_width * vga_height));
    }
}
