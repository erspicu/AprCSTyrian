using System.Text;
using AprCSTyrian.Core.Ports;
using SDL2;

namespace AprCSTyrian.App.Sdl;

/// <summary>
/// <see cref="IInputBackend"/> 的 SDL2 實作：把 SDL_Event 轉成中性 <see cref="PlatformEvent"/>，
/// 交由 Core 的 keyboard.c 處理。Core 不直接依賴 SDL。
/// </summary>
internal sealed unsafe class SdlInput : IInputBackend
{
    public bool PollEvent(out PlatformEvent e)
    {
        e = default;
        if (SDL.SDL_PollEvent(out SDL.SDL_Event se) == 0)
            return false;

        switch (se.type)
        {
            case SDL.SDL_EventType.SDL_QUIT:
                e.Type = PlatformEventType.Quit;
                break;

            case SDL.SDL_EventType.SDL_KEYDOWN:
                e.Type = PlatformEventType.KeyDown;
                e.Scancode = (int)se.key.keysym.scancode;
                e.Sym = (int)se.key.keysym.sym;
                e.Mod = (int)se.key.keysym.mod;
                e.Repeat = se.key.repeat != 0;
                break;

            case SDL.SDL_EventType.SDL_KEYUP:
                e.Type = PlatformEventType.KeyUp;
                e.Scancode = (int)se.key.keysym.scancode;
                e.Sym = (int)se.key.keysym.sym;
                e.Mod = (int)se.key.keysym.mod;
                break;

            case SDL.SDL_EventType.SDL_TEXTINPUT:
                e.Type = PlatformEventType.TextInput;
                e.Text = DecodeText(se.text.text);
                break;

            case SDL.SDL_EventType.SDL_MOUSEMOTION:
                e.Type = PlatformEventType.MouseMotion;
                e.X = se.motion.x; e.Y = se.motion.y;
                e.Xrel = se.motion.xrel; e.Yrel = se.motion.yrel;
                break;

            case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                e.Type = PlatformEventType.MouseButtonDown;
                e.X = se.button.x; e.Y = se.button.y; e.Button = se.button.button;
                break;

            case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                e.Type = PlatformEventType.MouseButtonUp;
                e.X = se.button.x; e.Y = se.button.y; e.Button = se.button.button;
                break;

            case SDL.SDL_EventType.SDL_WINDOWEVENT:
                e.Type = se.window.windowEvent switch
                {
                    SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED => PlatformEventType.WindowFocusGained,
                    SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST => PlatformEventType.WindowFocusLost,
                    SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED => PlatformEventType.WindowResized,
                    _ => PlatformEventType.Other,
                };
                break;

            default:
                e.Type = PlatformEventType.Other;
                break;
        }
        return true;
    }

    private static string DecodeText(byte* text)
    {
        int len = 0;
        while (len < 32 && text[len] != 0) len++;
        return Encoding.UTF8.GetString(text, len);
    }

    public void SetRelativeMouseMode(bool enable) =>
        SDL.SDL_SetRelativeMouseMode(enable ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE);

    public void ShowCursor(bool show) =>
        SDL.SDL_ShowCursor(show ? SDL.SDL_ENABLE : SDL.SDL_DISABLE);
}
