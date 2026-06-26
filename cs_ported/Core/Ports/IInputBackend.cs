namespace AprCSTyrian.Core.Ports;

/// <summary>
/// 輸入埠 (Port)。Core 每幀呼叫 <see cref="Poll"/> 後查詢狀態。
/// </summary>
public interface IInputBackend
{
    /// <summary>抽取/處理本幀累積的輸入事件，更新內部狀態。</summary>
    void Poll();

    /// <summary>使用者要求關閉（視窗 X、Alt+F4 等）。</summary>
    bool QuitRequested { get; }

    /// <summary>指定按鍵目前是否按住。</summary>
    bool IsKeyDown(GameKey key);

    /// <summary>指定按鍵是否在本幀剛被按下（邊緣觸發）。</summary>
    bool WasKeyPressed(GameKey key);
}
