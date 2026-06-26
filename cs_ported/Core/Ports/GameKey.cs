namespace AprCSTyrian.Core.Ports;

/// <summary>
/// 遊戲邏輯使用的抽象按鍵，與實體鍵碼解耦。
/// Adapter 負責把實際輸入 (SDL scancode 等) 對應到這些值。
/// </summary>
public enum GameKey
{
    Up,
    Down,
    Left,
    Right,
    Fire,        // 主武器（space）
    Change,      // 切換後方武器模式（enter）
    SideLeft,    // 左僚機（ctrl）
    SideRight,   // 右僚機（alt）
    Pause,
    Enter,
    Escape,
    Space,
    FullscreenToggle, // alt-enter
}
