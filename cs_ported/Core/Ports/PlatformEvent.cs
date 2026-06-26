namespace AprCSTyrian.Core.Ports;

/// <summary>中性平台事件類型（對應所需的 SDL_EventType 子集）。</summary>
public enum PlatformEventType
{
    Other = 0,
    Quit,
    KeyDown,
    KeyUp,
    TextInput,
    MouseMotion,
    MouseButtonDown,
    MouseButtonUp,
    WindowFocusGained,
    WindowFocusLost,
    WindowResized,
}

/// <summary>
/// 中性平台事件：App 的 SDL adapter 把 SDL_Event 轉成此結構交給 Core，
/// 讓 Core 的 keyboard.c 能處理輸入而不直接依賴 SDL。欄位值（scancode/sym/mod）
/// 沿用 SDL 數值（見 <see cref="SdlKeys"/>）。
/// </summary>
public struct PlatformEvent
{
    public PlatformEventType Type;

    // 鍵盤
    public int Scancode;   // SDL_Scancode
    public int Sym;        // SDL_Keycode
    public int Mod;        // SDL_Keymod
    public bool Repeat;

    // 文字輸入（已解碼的 Unicode 字串；Core 再對應到 CP437）
    public string? Text;

    // 滑鼠（視窗座標；Core 透過 IVideoBackend.MapWindowToScreen 轉成遊戲座標）
    public int X, Y, Xrel, Yrel;
    public int Button;
}
