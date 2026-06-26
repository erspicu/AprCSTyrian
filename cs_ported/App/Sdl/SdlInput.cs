using AprCSTyrian.Core.Ports;
using SDL2;

namespace AprCSTyrian.App.Sdl;

/// <summary>
/// <see cref="IInputBackend"/> 的 SDL2 實作：每幀抽取事件佇列，
/// 維護鍵盤的「按住」與「本幀剛按下」狀態，並把 SDL scancode 對應到 <see cref="GameKey"/>。
/// </summary>
internal sealed class SdlInput : IInputBackend
{
    // 每個 GameKey 對應一組可能的實體 scancode。
    private static readonly Dictionary<GameKey, SDL.SDL_Scancode[]> KeyMap = new()
    {
        [GameKey.Up]      = [SDL.SDL_Scancode.SDL_SCANCODE_UP],
        [GameKey.Down]    = [SDL.SDL_Scancode.SDL_SCANCODE_DOWN],
        [GameKey.Left]    = [SDL.SDL_Scancode.SDL_SCANCODE_LEFT],
        [GameKey.Right]   = [SDL.SDL_Scancode.SDL_SCANCODE_RIGHT],
        [GameKey.Fire]    = [SDL.SDL_Scancode.SDL_SCANCODE_SPACE],
        [GameKey.Change]  = [SDL.SDL_Scancode.SDL_SCANCODE_RETURN],
        [GameKey.SideLeft]= [SDL.SDL_Scancode.SDL_SCANCODE_LCTRL, SDL.SDL_Scancode.SDL_SCANCODE_RCTRL],
        [GameKey.SideRight]=[SDL.SDL_Scancode.SDL_SCANCODE_LALT, SDL.SDL_Scancode.SDL_SCANCODE_RALT],
        [GameKey.Pause]   = [SDL.SDL_Scancode.SDL_SCANCODE_P],
        [GameKey.Enter]   = [SDL.SDL_Scancode.SDL_SCANCODE_RETURN],
        [GameKey.Escape]  = [SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE],
        [GameKey.Space]   = [SDL.SDL_Scancode.SDL_SCANCODE_SPACE],
        [GameKey.FullscreenToggle] = [SDL.SDL_Scancode.SDL_SCANCODE_RETURN], // 搭配 alt 判定
    };

    private readonly HashSet<SDL.SDL_Scancode> _down = new();
    private readonly HashSet<SDL.SDL_Scancode> _pressedThisFrame = new();

    public bool QuitRequested { get; private set; }

    public void Poll()
    {
        _pressedThisFrame.Clear();

        while (SDL.SDL_PollEvent(out SDL.SDL_Event e) != 0)
        {
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_QUIT:
                    QuitRequested = true;
                    break;

                case SDL.SDL_EventType.SDL_KEYDOWN:
                    if (e.key.repeat == 0)
                    {
                        var sc = e.key.keysym.scancode;
                        if (_down.Add(sc))
                            _pressedThisFrame.Add(sc);
                    }
                    break;

                case SDL.SDL_EventType.SDL_KEYUP:
                    _down.Remove(e.key.keysym.scancode);
                    break;
            }
        }
    }

    public bool IsKeyDown(GameKey key)
    {
        foreach (var sc in KeyMap[key])
            if (_down.Contains(sc)) return true;
        return false;
    }

    public bool WasKeyPressed(GameKey key)
    {
        foreach (var sc in KeyMap[key])
            if (_pressedThisFrame.Contains(sc)) return true;
        return false;
    }
}
