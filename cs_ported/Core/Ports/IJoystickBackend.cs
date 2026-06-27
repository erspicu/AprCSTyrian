namespace AprCSTyrian.Core.Ports;

/// <summary>
/// 搖桿輸入埠 (Port)。對應 joystick.c 用到的 SDL2 原生搖桿呼叫。
/// Core 只透過此介面操作搖桿（以 joy 索引定位），平台細節 (SDL_Joystick*) 由 Adapter 封裝。
/// 鐵則：Core 不直接引用 SDL，搖桿原生呼叫只進 App 的 Adapter。
/// </summary>
public interface IJoystickBackend
{
    /// <summary>已開啟的搖桿數（對應 SDL_NumJoysticks）。</summary>
    int NumJoysticks { get; }

    /// <summary>更新所有搖桿狀態（SDL_JoystickUpdate）。</summary>
    void Update();

    /// <summary>讀取軸值，範圍 -32768..32767（SDL_JoystickGetAxis）。</summary>
    int GetAxis(int joy, int axis);

    /// <summary>按鍵是否按下（SDL_JoystickGetButton）。</summary>
    bool GetButton(int joy, int button);

    /// <summary>讀取方向帽 (hat) 的 SDL_HAT_* 位元遮罩（SDL_JoystickGetHat）。</summary>
    int GetHat(int joy, int hat);

    /// <summary>軸數量（SDL_JoystickNumAxes）。</summary>
    int NumAxes(int joy);

    /// <summary>按鍵數量（SDL_JoystickNumButtons）。</summary>
    int NumButtons(int joy);

    /// <summary>方向帽數量（SDL_JoystickNumHats）。</summary>
    int NumHats(int joy);

    /// <summary>搖桿名稱（SDL_JoystickName）。</summary>
    string GetName(int joy);

    /// <summary>送出 KEYDOWN+KEYUP 事件假裝鍵盤輸入（選單用，對應 joystick.c push_key）。</summary>
    void PushKey(int scancode);
}
