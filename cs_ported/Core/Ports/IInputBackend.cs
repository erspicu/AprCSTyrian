namespace AprCSTyrian.Core.Ports;

/// <summary>
/// 輸入埠 (Port)：薄薄的 SDL 事件來源。Core 的 keyboard.c 透過 <see cref="PollEvent"/>
/// 抽取中性事件（對應 SDL_PollEvent），並自行維護按鍵/滑鼠狀態。
/// </summary>
public interface IInputBackend
{
    /// <summary>抽取下一個待處理事件（對應 SDL_PollEvent）。無事件時回傳 false。</summary>
    bool PollEvent(out PlatformEvent e);

    /// <summary>啟用/停用相對滑鼠模式（對應 SDL_SetRelativeMouseMode）。</summary>
    void SetRelativeMouseMode(bool enable);

    /// <summary>顯示/隱藏系統滑鼠游標（對應 SDL_ShowCursor）。</summary>
    void ShowCursor(bool show);
}
