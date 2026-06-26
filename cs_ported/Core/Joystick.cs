namespace AprCSTyrian.Core;

/// <summary>
/// 對應 sources/src/joystick.c —— **目前為 stub**（搖桿支援待後續移植）。
/// keyboard.c 的 wait* 迴圈會呼叫 push_joysticks_as_keyboard。
/// </summary>
internal static class Joystick
{
    public static bool ignore_joystick;

    public static void init_joysticks() { }
    public static void deinit_joysticks() { }
    public static void push_joysticks_as_keyboard() { }
}
