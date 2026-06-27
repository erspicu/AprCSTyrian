using AprCSTyrian.Core.Ports;

namespace AprCSTyrian.Core;

/// <summary>
/// C 風格全域：持有平台 (SDL) Ports 參考與資料/使用者目錄路徑，
/// 供移植自各 .c 的 static 模組直接存取（對應 C 的全域變數/extern）。
/// 由 App 組合根於啟動時 <see cref="Init"/>。
/// </summary>
internal static class Globals
{
    public static IVideoBackend Video = null!;
    public static IAudioBackend Audio = null!;
    public static IInputBackend Input = null!;
    public static IClock Clock = null!;
    public static IFileSystem Files = null!;
    public static IJoystickBackend Joystick = null!;

    /// <summary>設定指定的資料根目錄（對應 file.c 的候選目錄之一）。</summary>
    public static string ConfiguredDataDir = "data";

    /// <summary>使用者可寫目錄（設定/存檔）。</summary>
    public static string UserDir = ".";

    public static void Init(IGamePlatform p, string dataDir, string userDir)
    {
        Video = p.Video;
        Audio = p.Audio;
        Input = p.Input;
        Clock = p.Clock;
        Files = p.Files;
        Joystick = p.Joystick;
        ConfiguredDataDir = dataDir;
        UserDir = userDir;
    }
}
