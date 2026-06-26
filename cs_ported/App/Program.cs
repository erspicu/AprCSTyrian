using AprCSTyrian.App.Sdl;
using AprCSTyrian.Core;

namespace AprCSTyrian.App;

/// <summary>
/// 進入點 / 組合根 (Composition Root)。
/// 唯一知道「Core 跑在 SDL 上」的地方：建立 SDL 平台並注入 <see cref="TyrianGame"/>。
/// 換平台 = 換掉這裡注入的 IGamePlatform 實作，Core 不需更動。
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // 資料/使用者目錄：可由參數覆寫，預設放在執行檔旁。
        string baseDir = AppContext.BaseDirectory;
        string dataRoot = args.Length > 0 ? args[0] : Path.Combine(baseDir, "data");
        string userRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AprCSTyrian");

        try
        {
            using var platform = new SdlPlatform(
                windowTitle: "AprCSTyrian",
                scale: 3,
                dataRoot: dataRoot,
                userRoot: userRoot);

            var game = new TyrianGame(platform, dataRoot, userRoot);
            game.Run();
            return 0;
        }
        catch (TyrianHaltException halt)
        {
            // 正常結束流程（對應 C 的 exit(code)）。
            return halt.Code;
        }
        catch (Exception ex)
        {
            // WinExe 無 console，致命錯誤寫入記錄檔方便診斷。
            try
            {
                string logPath = Path.Combine(baseDir, "crash.log");
                File.WriteAllText(logPath, ex.ToString());
            }
            catch { /* ignore */ }
            return 1;
        }
    }
}
