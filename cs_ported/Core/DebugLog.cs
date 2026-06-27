namespace AprCSTyrian.Core;

/// <summary>
/// 暫時性除錯 log（追 in-game 選單 / 死亡 / 設定寫入 等 bug 用）。
/// 寫到 使用者目錄/buglog.txt（append，每次啟動加一段 SESSION 標記）。
/// 穩定後把 <see cref="Enabled"/> 設 false 或整檔移除。
/// </summary>
internal static class DebugLog
{
    /// <summary>除錯期 true；穩定後設 false。</summary>
    internal const bool Enabled = true;

    private static System.IO.StreamWriter? _w;
    private static bool _init;

    private static void Ensure()
    {
        if (_init) return;
        _init = true;
        if (!Enabled) return;
        try
        {
            string dir = Config.get_user_directory();
            System.IO.Directory.CreateDirectory(dir);
            _w = new System.IO.StreamWriter(System.IO.Path.Combine(dir, "buglog.txt"), append: true) { AutoFlush = true };
            _w.WriteLine();
            _w.WriteLine($"===== SESSION START {System.DateTime.Now:yyyy-MM-dd HH:mm:ss} =====");
        }
        catch { _w = null; }
    }

    internal static void Log(string msg)
    {
        if (!Enabled) return;
        Ensure();
        _w?.WriteLine($"[{System.DateTime.Now:HH:mm:ss.fff}] {msg}");
    }
}
