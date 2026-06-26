namespace AprCSTyrian.Core;

/// <summary>
/// SDL2 的 scancode / keycode / keymod 常數（數值與 SDL2 一致），讓 Core 的輸入處理與
/// 遊戲程式碼能沿用 SDL_SCANCODE_* / KMOD_* / KEY_COMBO 而不直接連結 SDL。
/// </summary>
internal static class SdlKeys
{
    public const int SDL_NUM_SCANCODES = 512;

    // 字母
    public const int SDL_SCANCODE_A = 4, SDL_SCANCODE_B = 5, SDL_SCANCODE_C = 6, SDL_SCANCODE_D = 7,
        SDL_SCANCODE_E = 8, SDL_SCANCODE_F = 9, SDL_SCANCODE_G = 10, SDL_SCANCODE_H = 11, SDL_SCANCODE_I = 12,
        SDL_SCANCODE_J = 13, SDL_SCANCODE_K = 14, SDL_SCANCODE_L = 15, SDL_SCANCODE_M = 16, SDL_SCANCODE_N = 17,
        SDL_SCANCODE_O = 18, SDL_SCANCODE_P = 19, SDL_SCANCODE_Q = 20, SDL_SCANCODE_R = 21, SDL_SCANCODE_S = 22,
        SDL_SCANCODE_T = 23, SDL_SCANCODE_U = 24, SDL_SCANCODE_V = 25, SDL_SCANCODE_W = 26, SDL_SCANCODE_X = 27,
        SDL_SCANCODE_Y = 28, SDL_SCANCODE_Z = 29;

    // 數字列
    public const int SDL_SCANCODE_1 = 30, SDL_SCANCODE_2 = 31, SDL_SCANCODE_3 = 32, SDL_SCANCODE_4 = 33,
        SDL_SCANCODE_5 = 34, SDL_SCANCODE_6 = 35, SDL_SCANCODE_7 = 36, SDL_SCANCODE_8 = 37, SDL_SCANCODE_9 = 38,
        SDL_SCANCODE_0 = 39;

    public const int SDL_SCANCODE_RETURN = 40, SDL_SCANCODE_ESCAPE = 41, SDL_SCANCODE_BACKSPACE = 42,
        SDL_SCANCODE_TAB = 43, SDL_SCANCODE_SPACE = 44, SDL_SCANCODE_MINUS = 45, SDL_SCANCODE_EQUALS = 46,
        SDL_SCANCODE_LEFTBRACKET = 47, SDL_SCANCODE_RIGHTBRACKET = 48, SDL_SCANCODE_BACKSLASH = 49,
        SDL_SCANCODE_SEMICOLON = 51, SDL_SCANCODE_APOSTROPHE = 52, SDL_SCANCODE_GRAVE = 53,
        SDL_SCANCODE_COMMA = 54, SDL_SCANCODE_PERIOD = 55, SDL_SCANCODE_SLASH = 56, SDL_SCANCODE_CAPSLOCK = 57;

    // 功能鍵
    public const int SDL_SCANCODE_F1 = 58, SDL_SCANCODE_F2 = 59, SDL_SCANCODE_F3 = 60, SDL_SCANCODE_F4 = 61,
        SDL_SCANCODE_F5 = 62, SDL_SCANCODE_F6 = 63, SDL_SCANCODE_F7 = 64, SDL_SCANCODE_F8 = 65, SDL_SCANCODE_F9 = 66,
        SDL_SCANCODE_F10 = 67, SDL_SCANCODE_F11 = 68, SDL_SCANCODE_F12 = 69;

    public const int SDL_SCANCODE_PAUSE = 72, SDL_SCANCODE_INSERT = 73, SDL_SCANCODE_HOME = 74,
        SDL_SCANCODE_PAGEUP = 75, SDL_SCANCODE_DELETE = 76, SDL_SCANCODE_END = 77, SDL_SCANCODE_PAGEDOWN = 78,
        SDL_SCANCODE_RIGHT = 79, SDL_SCANCODE_LEFT = 80, SDL_SCANCODE_DOWN = 81, SDL_SCANCODE_UP = 82;

    // 鍵盤右下數字鍵盤
    public const int SDL_SCANCODE_KP_DIVIDE = 84, SDL_SCANCODE_KP_MULTIPLY = 85, SDL_SCANCODE_KP_MINUS = 86,
        SDL_SCANCODE_KP_PLUS = 87, SDL_SCANCODE_KP_ENTER = 88, SDL_SCANCODE_KP_1 = 89, SDL_SCANCODE_KP_2 = 90,
        SDL_SCANCODE_KP_3 = 91, SDL_SCANCODE_KP_4 = 92, SDL_SCANCODE_KP_5 = 93, SDL_SCANCODE_KP_6 = 94,
        SDL_SCANCODE_KP_7 = 95, SDL_SCANCODE_KP_8 = 96, SDL_SCANCODE_KP_9 = 97, SDL_SCANCODE_KP_0 = 98,
        SDL_SCANCODE_KP_PERIOD = 99;

    // 修飾鍵
    public const int SDL_SCANCODE_LCTRL = 224, SDL_SCANCODE_LSHIFT = 225, SDL_SCANCODE_LALT = 226,
        SDL_SCANCODE_LGUI = 227, SDL_SCANCODE_RCTRL = 228, SDL_SCANCODE_RSHIFT = 229, SDL_SCANCODE_RALT = 230,
        SDL_SCANCODE_RGUI = 231;

    // Keymod
    public const int KMOD_NONE = 0x0000;
    public const int KMOD_LSHIFT = 0x0001, KMOD_RSHIFT = 0x0002;
    public const int KMOD_LCTRL = 0x0040, KMOD_RCTRL = 0x0080;
    public const int KMOD_LALT = 0x0100, KMOD_RALT = 0x0200;
    public const int KMOD_LGUI = 0x0400, KMOD_RGUI = 0x0800;
    public const int KMOD_NUM = 0x1000, KMOD_CAPS = 0x2000, KMOD_MODE = 0x4000;
    public const int KMOD_SHIFT = KMOD_LSHIFT | KMOD_RSHIFT;
    public const int KMOD_CTRL = KMOD_LCTRL | KMOD_RCTRL;
    public const int KMOD_ALT = KMOD_LALT | KMOD_RALT;
    public const int KMOD_GUI = KMOD_LGUI | KMOD_RGUI;

    // Keycode（僅 lordKeySyms 需要：小寫字母 = ASCII）
    public const int SDLK_l = 'l', SDLK_o = 'o', SDLK_r = 'r', SDLK_d = 'd';

    // 滑鼠按鍵
    public const int SDL_BUTTON_LEFT = 1, SDL_BUTTON_MIDDLE = 2, SDL_BUTTON_RIGHT = 3;
    public static int SDL_BUTTON(int x) => 1 << (x - 1);

    /// <summary>對應 keyboard.h:KEY_COMBO 巨集。</summary>
    public static uint KEY_COMBO(int mod, int scancode)
    {
        uint m = 0;
        if ((mod & KMOD_SHIFT) != 0) m |= KMOD_SHIFT;
        if ((mod & KMOD_CTRL) != 0) m |= KMOD_CTRL;
        if ((mod & KMOD_ALT) != 0) m |= KMOD_ALT;
        if ((mod & KMOD_GUI) != 0) m |= KMOD_GUI;
        return (uint)scancode | (m << 16);
    }
}
