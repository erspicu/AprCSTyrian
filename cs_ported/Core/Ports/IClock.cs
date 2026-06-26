namespace AprCSTyrian.Core.Ports;

/// <summary>
/// 時間埠 (Port)。對應原始碼的 SDL_GetTicks / SDL_Delay 用法。
/// </summary>
public interface IClock
{
    /// <summary>自啟動以來的毫秒數（單調遞增）。</summary>
    uint Ticks { get; }

    /// <summary>暫停目前執行緒約指定毫秒數。</summary>
    void Delay(uint milliseconds);
}
