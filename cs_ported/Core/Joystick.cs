namespace AprCSTyrian.Core;

/// <summary>
/// 對應 sources/src/joystick.c —— **目前為 stub**（搖桿支援待後續移植）。
/// keyboard.c 的 wait* 迴圈會呼叫 push_joysticks_as_keyboard。
/// </summary>
// 對應 joystick.h 的型別（stub 階段：joysticks==0，下列僅供商店/設定選單忠實移植編譯用）。
internal enum Joystick_assignment_types { NONE, KEYBOARD, JOYSTICK_AXIS, JOYSTICK_BUTTON, JOYSTICK_HAT }

internal struct Joystick_assignment
{
    public Joystick_assignment_types type;
}

internal sealed class JoystickType
{
    public bool analog;
    public int sensitivity, threshold;
    public readonly int[] analog_direction = new int[4];
    public readonly bool[] direction = new bool[4];
    public readonly bool[] direction_pressed = new bool[4];
    public readonly Joystick_assignment[][] assignment = MakeAssign(); // [10][2]
    private static Joystick_assignment[][] MakeAssign()
    {
        var a = new Joystick_assignment[10][];
        for (int i = 0; i < 10; i++) a[i] = new Joystick_assignment[2];
        return a;
    }
}

internal static class Joystick
{
    public static bool ignore_joystick;

    // stub 狀態：無搖桿
    public static int joysticks = 0;
    public static int joystick_config = 0;
    public static bool joydown = false;
    public static readonly JoystickType[] joystick = MakeJoysticks();
    private static JoystickType[] MakeJoysticks()
    {
        var a = new JoystickType[8];
        for (int i = 0; i < a.Length; i++) a[i] = new JoystickType();
        return a;
    }

    public static void init_joysticks() { }
    public static void deinit_joysticks() { }
    public static void push_joysticks_as_keyboard() { }
    public static void poll_joysticks() { }
    public static void reset_joystick_assignments(int j) { }
    public static bool detect_joystick_assignment(int j, out Joystick_assignment a) { a = default; return false; }
    public static bool joystick_assignment_cmp(ref Joystick_assignment a, ref Joystick_assignment b) => false;
    public static void joystick_assignments_to_string(ref string buffer, Joystick_assignment[] assignments) { buffer = ""; }
}
