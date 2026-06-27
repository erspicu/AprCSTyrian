using AprCSTyrian.Core.Ports;
using SDL2;

namespace AprCSTyrian.App.Sdl;

/// <summary>
/// <see cref="IJoystickBackend"/> 的 SDL2 實作：初始化搖桿子系統，開啟所有偵測到的搖桿，
/// 以 joy 索引對應已開啟的 handle 呼叫對應 SDL2 函式。對應 joystick.c 的 SDL 原生呼叫側。
/// </summary>
internal sealed class SdlJoystick : IJoystickBackend
{
    private readonly IntPtr[] _handles;

    public int NumJoysticks { get; }

    public SdlJoystick()
    {
        if (SDL.SDL_InitSubSystem(SDL.SDL_INIT_JOYSTICK) != 0)
        {
            // 初始化失敗：當作沒有搖桿（對應 joystick.c init_joysticks 的 ignore 路徑）。
            _handles = Array.Empty<IntPtr>();
            NumJoysticks = 0;
            return;
        }

        // 我們自己 poll，不要 SDL 把搖桿事件塞進事件佇列。
        SDL.SDL_JoystickEventState(SDL.SDL_IGNORE);

        int n = SDL.SDL_NumJoysticks();
        _handles = new IntPtr[n];
        for (int j = 0; j < n; j++)
            _handles[j] = SDL.SDL_JoystickOpen(j);

        NumJoysticks = n;
    }

    public void Update() => SDL.SDL_JoystickUpdate();

    public int GetAxis(int joy, int axis) => SDL.SDL_JoystickGetAxis(_handles[joy], axis);

    public bool GetButton(int joy, int button) => SDL.SDL_JoystickGetButton(_handles[joy], button) == 1;

    public int GetHat(int joy, int hat) => SDL.SDL_JoystickGetHat(_handles[joy], hat);

    public int NumAxes(int joy) => SDL.SDL_JoystickNumAxes(_handles[joy]);

    public int NumButtons(int joy) => SDL.SDL_JoystickNumButtons(_handles[joy]);

    public int NumHats(int joy) => SDL.SDL_JoystickNumHats(_handles[joy]);

    public string GetName(int joy) => SDL.SDL_JoystickName(_handles[joy]) ?? string.Empty;

    // 對應 joystick.c:218 push_key —— 送出 KEYDOWN 接著 KEYUP。
    public void PushKey(int scancode)
    {
        var e = new SDL.SDL_Event();
        e.key.keysym.scancode = (SDL.SDL_Scancode)scancode;
        e.key.state = SDL.SDL_RELEASED;

        e.type = SDL.SDL_EventType.SDL_KEYDOWN;
        SDL.SDL_PushEvent(ref e);

        e.type = SDL.SDL_EventType.SDL_KEYUP;
        SDL.SDL_PushEvent(ref e);
    }

    public void Dispose()
    {
        foreach (var h in _handles)
            if (h != IntPtr.Zero)
                SDL.SDL_JoystickClose(h);

        SDL.SDL_QuitSubSystem(SDL.SDL_INIT_JOYSTICK);
    }
}
