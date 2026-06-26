using AprCSTyrian.Core.Ports;

namespace AprCSTyrian.Core;

/// <summary>對應 keyboard.h:KeyboardInput。</summary>
internal struct KeyboardInput
{
    public int sym;       // SDL_Keycode（文字輸入時為 -1）
    public int scancode;  // SDL_Scancode（文字輸入時為 -1）
    public int mod;       // SDL_Keymod
    public byte ch;       // CP437 字元（文字輸入用）
}

/// <summary>對應 keyboard.h:MouseInput。</summary>
internal struct MouseInput
{
    public int x, y;
    public byte button;
}

/// <summary>對應 keyboard.h:InputFlags。</summary>
internal enum InputFlags
{
    INPUT_ANY = 0,
    INPUT_NO_MOTION = 1,
}

/// <summary>
/// 移植 sources/src/keyboard.c —— 透過中性事件 (<see cref="IInputBackend.PollEvent"/>) 處理輸入，
/// 維護 keysactive / 鍵盤與滑鼠事件佇列 / 焦點等狀態。保持 Core 不直接依賴 SDL。
/// </summary>
internal static class Keyboard
{
    private const int SDL_POLL_INTERVAL = 10;

#pragma warning disable CS0649 // 由遊戲邏輯指派
    public static bool ESCPressed;
#pragma warning restore CS0649
    public static bool windowHasFocus;

    public static readonly bool[] keysactive = new bool[SdlKeys.SDL_NUM_SCANCODES];

    public static readonly int[] lordKeySyms = { SdlKeys.SDLK_l, SdlKeys.SDLK_o, SdlKeys.SDLK_r, SdlKeys.SDLK_d };
    public static readonly bool[] lordKeySymsDown = new bool[4];

    private static readonly KeyboardInput[] keyboardInputs = new KeyboardInput[32];
    private static int keyboardInputsFront, keyboardInputsBack, keyboardInputsCount;

    public static int mouseX, mouseY;
    public static byte mouseButtonsDown;

    private static readonly MouseInput[] mouseInputs = new MouseInput[4];
    private static int mouseInputsFront, mouseInputsBack, mouseInputsCount;
    private static bool mouseHasMotionInput;

    private static bool mouseRelativeEnabled;
    private static int mouseWindowXRelative, mouseWindowYRelative;

    // Mapping from CP437 to UCS for 0x80 to 0xA8.
    private static readonly ushort[] ucsMap =
    {
        0x00C7,0x00FC,0x00E9,0x00E2,0x00E4,0x00E0,0x00E5,0x00E7,
        0x00EA,0x00EB,0x00E8,0x00EF,0x00EE,0x00EC,0x00C4,0x00C5,
        0x00C9,0x00E6,0x00C6,0x00F4,0x00F6,0x00F2,0x00FB,0x00F9,
        0x00FF,0x00D6,0x00DC,0x00A2,0x00A3,0x00A5,0x20A7,0x0192,
        0x00E1,0x00ED,0x00F3,0x00FA,0x00F1,0x00D1,0x00AA,0x00BA,
        0x00BF,
    };

    public static void init_keyboard()
    {
        Globals.Input.ShowCursor(false);
    }

    /// <summary>移植對照重播：把一個 KeyDown 事件注入鍵盤佇列（等同原版 SDL_KEYDOWN 的 push）。</summary>
    public static void InjectQueueInput(int sym, int scancode, int mod)
    {
        if (keyboardInputsCount < keyboardInputs.Length)
        {
            keyboardInputs[keyboardInputsBack] = new KeyboardInput { sym = sym, scancode = scancode, mod = mod, ch = 0 };
            keyboardInputsBack = keyboardInputsBack == keyboardInputs.Length - 1 ? 0 : keyboardInputsBack + 1;
            keyboardInputsCount += 1;
        }
    }

    public static bool keyboardHasInput() => keyboardInputsCount > 0;

    public static bool keyboardGetInput(out KeyboardInput out_input)
    {
        if (keyboardInputsCount > 0)
        {
            out_input = keyboardInputs[keyboardInputsFront];
            keyboardInputsFront = keyboardInputsFront == keyboardInputs.Length - 1 ? 0 : keyboardInputsFront + 1;
            keyboardInputsCount -= 1;
            return true;
        }
        out_input = default;
        return false;
    }

    public static void keyboardClearInput()
    {
        keyboardInputsFront = keyboardInputsBack = keyboardInputsCount = 0;
    }

    public static bool mouseHasInput(InputFlags flags) =>
        mouseInputsCount > 0 || (((int)flags & (int)InputFlags.INPUT_NO_MOTION) == 0 && mouseHasMotionInput);

    public static bool mouseGetInput(InputFlags flags, out MouseInput out_input)
    {
        if (mouseInputsCount > 0)
        {
            out_input = mouseInputs[mouseInputsFront];
            mouseInputsFront = mouseInputsFront == mouseInputs.Length - 1 ? 0 : mouseInputsFront + 1;
            mouseInputsCount -= 1;
            return true;
        }

        if (((int)flags & (int)InputFlags.INPUT_NO_MOTION) == 0 && mouseHasMotionInput)
        {
            out_input = new MouseInput { x = mouseX, y = mouseY, button = 0 };
            mouseHasMotionInput = false;
            return true;
        }

        out_input = default;
        return false;
    }

    public static void mouseClearInput()
    {
        mouseInputsFront = mouseInputsBack = mouseInputsCount = 0;
        mouseHasMotionInput = false;
    }

    public static void mouseSetRelative(bool enable)
    {
        Globals.Input.SetRelativeMouseMode(enable && windowHasFocus);
        mouseRelativeEnabled = enable;
        mouseWindowXRelative = 0;
        mouseWindowYRelative = 0;
    }

    public static void mouseGetRelativePosition(out int out_x, out int out_y)
    {
        out_x = mouseWindowXRelative;
        out_y = mouseWindowYRelative;
        mouseWindowXRelative = 0;
        mouseWindowYRelative = 0;
    }

    public static void handleSdlEvents()
    {
        while (Globals.Input.PollEvent(out PlatformEvent ev))
        {
            switch (ev.Type)
            {
            case PlatformEventType.WindowFocusLost:
                windowHasFocus = false;
                mouseSetRelative(mouseRelativeEnabled);
                break;
            case PlatformEventType.WindowFocusGained:
                windowHasFocus = true;
                mouseSetRelative(mouseRelativeEnabled);
                break;
            case PlatformEventType.WindowResized:
                Video.video_on_win_resize();
                break;

            case PlatformEventType.KeyDown:
                if ((ev.Mod & SdlKeys.KMOD_ALT) != 0 && ev.Scancode == SdlKeys.SDL_SCANCODE_RETURN)
                {
                    Video.toggle_fullscreen();
                    break;
                }

                if (!ev.Repeat && ev.Scancode >= 0 && ev.Scancode < keysactive.Length)
                    keysactive[ev.Scancode] = true;

                for (int i = 0; i < lordKeySyms.Length; ++i)
                    lordKeySymsDown[i] |= ev.Sym == lordKeySyms[i];

                if (keyboardInputsCount < keyboardInputs.Length)
                {
                    keyboardInputs[keyboardInputsBack] = new KeyboardInput
                    {
                        sym = ev.Sym,
                        scancode = ev.Scancode,
                        mod = ev.Mod,
                        ch = 0,
                    };
                    keyboardInputsBack = keyboardInputsBack == keyboardInputs.Length - 1 ? 0 : keyboardInputsBack + 1;
                    keyboardInputsCount += 1;
                }

                KeyLog.NoteKeyDown(ev.Sym, ev.Scancode, (int)ev.Mod); // 移植對照：擷取佇列事件

                Mouse.mouseInactive = true;
                break;

            case PlatformEventType.KeyUp:
                if (ev.Scancode >= 0 && ev.Scancode < keysactive.Length)
                    keysactive[ev.Scancode] = false;
                for (int i = 0; i < lordKeySyms.Length; ++i)
                    lordKeySymsDown[i] &= ev.Sym != lordKeySyms[i];
                break;

            case PlatformEventType.TextInput:
                HandleTextInput(ev.Text);
                break;

            case PlatformEventType.MouseMotion:
                mouseX = ev.X; mouseY = ev.Y;
                Video.mapWindowPointToScreen(ref mouseX, ref mouseY);
                mouseHasMotionInput = true;

                if (mouseRelativeEnabled && windowHasFocus)
                {
                    mouseWindowXRelative += ev.Xrel;
                    mouseWindowYRelative += ev.Yrel;
                }

                Globals.Input.ShowCursor(mouseX < 0 || mouseX >= Video.vga_width ||
                                         mouseY < 0 || mouseY >= Video.vga_height);

                if (ev.Xrel != 0 || ev.Yrel != 0)
                    Mouse.mouseInactive = false;
                break;

            case PlatformEventType.MouseButtonDown:
            {
                int bx = ev.X, by = ev.Y;
                Video.mapWindowPointToScreen(ref bx, ref by);
                if (mouseInputsCount < mouseInputs.Length)
                {
                    mouseInputs[mouseInputsBack] = new MouseInput { button = (byte)ev.Button, x = bx, y = by };
                    mouseInputsBack = mouseInputsBack == mouseInputs.Length - 1 ? 0 : mouseInputsBack + 1;
                    mouseInputsCount += 1;
                }
                mouseButtonsDown |= (byte)SdlKeys.SDL_BUTTON(ev.Button);
                Mouse.mouseInactive = false;
                break;
            }

            case PlatformEventType.MouseButtonUp:
                mouseButtonsDown &= (byte)~SdlKeys.SDL_BUTTON(ev.Button);
                break;

            case PlatformEventType.Quit:
                throw new TyrianHaltException(0); // 對應 exit(0)
            }
        }

        KeyLog.InjectInput(); // 移植對照：重播模式下用原版 log 覆寫 keysactive
    }

    private static void HandleTextInput(string? text)
    {
        if (text == null) return;
        foreach (char cp in text)
        {
            byte ch = 0;
            if (cp < 0x80)
            {
                ch = (byte)cp;
            }
            else
            {
                for (int j = 0; j < ucsMap.Length; ++j)
                {
                    if (cp == ucsMap[j]) { ch = (byte)(0x80 + j); break; }
                }
                if (ch == 0) continue;
            }

            if (keyboardInputsCount < keyboardInputs.Length)
            {
                keyboardInputs[keyboardInputsBack] = new KeyboardInput { sym = -1, scancode = -1, mod = SdlKeys.KMOD_NONE, ch = ch };
                keyboardInputsBack = keyboardInputsBack == keyboardInputs.Length - 1 ? 0 : keyboardInputsBack + 1;
                keyboardInputsCount += 1;
            }
        }
    }

    public static bool hasInput(InputFlags flags) => keyboardHasInput() || mouseHasInput(flags);

    public static bool getInput() => keyboardGetInput(out _) || mouseGetInput(InputFlags.INPUT_NO_MOTION, out _);

    public static void waitUntilHasInput(InputFlags flags)
    {
        while (true)
        {
            Joystick.push_joysticks_as_keyboard();
            handleSdlEvents();
            if (hasInput(flags)) return;
            Globals.Clock.Delay(SDL_POLL_INTERVAL);
        }
    }

    public static void waitUntilGetInput()
    {
        while (true)
        {
            Joystick.push_joysticks_as_keyboard();
            handleSdlEvents();
            if (getInput()) return;
            Globals.Clock.Delay(SDL_POLL_INTERVAL);
        }
    }

    public static void waitUntilElapsed()
    {
        while (true)
        {
            Joystick.push_joysticks_as_keyboard();
            handleSdlEvents();
            uint delay = Nortsong.getFrameCountTicks();
            if (delay == 0) return;
            Globals.Clock.Delay(Math.Min(delay, SDL_POLL_INTERVAL));
        }
    }

    public static bool waitUntilHasInputOrElapsed()
    {
        while (true)
        {
            Joystick.push_joysticks_as_keyboard();
            handleSdlEvents();
            if (hasInput(InputFlags.INPUT_NO_MOTION)) return true;
            uint delay = Nortsong.getFrameCountTicks();
            if (delay == 0) return false;
            Globals.Clock.Delay(Math.Min(delay, SDL_POLL_INTERVAL));
        }
    }

    public static bool waitUntilGetInputOrElapsed()
    {
        while (true)
        {
            Joystick.push_joysticks_as_keyboard();
            handleSdlEvents();
            if (getInput()) return true;
            uint delay = Nortsong.getFrameCountTicks();
            if (delay == 0) return false;
            Globals.Clock.Delay(Math.Min(delay, SDL_POLL_INTERVAL));
        }
    }
}
