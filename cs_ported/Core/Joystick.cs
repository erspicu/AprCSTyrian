using AprCSTyrian.Core.Ports;

namespace AprCSTyrian.Core;

/// <summary>
/// 對應 sources/src/joystick.c —— 忠實移植。
/// SDL 原生搖桿呼叫經由 <see cref="Globals.Joystick"/> (<see cref="IJoystickBackend"/>) 抽離；
/// Core 不直接引用 SDL。指派的 load/save (config_file.c INI) 未移植，詳見 load/save 方法。
/// </summary>
// 對應 joystick.h 的列舉。
internal enum Joystick_assignment_types { NONE, AXIS, BUTTON, HAT }

// 對應 joystick.h 的 Joystick_assignment 結構。
internal struct Joystick_assignment
{
    public Joystick_assignment_types type;
    public int num;

    // 若為 hat：x_axis 為真表 X 軸，否則 Y 軸。
    public bool x_axis;

    // 若為 hat 或 axis：negative_axis 為真表負向，否則正向。
    public bool negative_axis;
}

// 對應 joystick.h 的 Joystick 結構（handle 改為 index + open 旗標）。
internal sealed class JoystickType
{
    public int index;
    public bool open;

    public readonly Joystick_assignment[][] assignment = MakeAssign(); // [10][2] 0-3:方向 4-9:動作

    public bool analog;
    public int sensitivity, threshold;

    public int x, y;
    public readonly int[] analog_direction = new int[4];
    public readonly bool[] direction = new bool[4];
    public readonly bool[] direction_pressed = new bool[4]; // up, right, down, left

    public bool confirm, cancel;
    public readonly bool[] action = new bool[6];
    public readonly bool[] action_pressed = new bool[6]; // fire, mode swap, left fire, right fire, menu, pause

    public uint joystick_delay;
    public bool input_pressed;

    private static Joystick_assignment[][] MakeAssign()
    {
        var a = new Joystick_assignment[10][];
        for (int i = 0; i < 10; i++) a[i] = new Joystick_assignment[2];
        return a;
    }
}

internal static class Joystick
{
    // 對應 SDL_HAT_* 位元遮罩。
    private const int SDL_HAT_CENTERED = 0;
    private const int SDL_HAT_UP = 1;
    private const int SDL_HAT_RIGHT = 2;
    private const int SDL_HAT_DOWN = 4;
    private const int SDL_HAT_LEFT = 8;

    public static int joystick_repeat_delay = 300; // 毫秒，按鍵重複延遲
    public static bool joydown = false;            // 任一搖桿鈕按下，poll_joysticks() 更新
    public static bool ignore_joystick = false;

    public static int joysticks = 0;
    public static JoystickType[] joystick = Array.Empty<JoystickType>();

    public static int joystick_config = 0;

    private const int joystick_analog_max = 32767;

    // 消除低於門檻的軸移動
    public static int joystick_axis_threshold(int j, int value)
    {
        bool negative = value < 0;
        if (negative)
            value = -value;

        if (value <= joystick[j].threshold * 1000)
            return 0;

        value -= joystick[j].threshold * 1000;

        return negative ? -value : value;
    }

    // 把軸值轉成 Tyrian 可用值（依靈敏度）
    public static int joystick_axis_reduce(int j, int value)
    {
        value = joystick_axis_threshold(j, value);

        if (value == 0)
            return 0;

        return value / (3000 - 200 * joystick[j].sensitivity);
    }

    // 把類比軸轉成角度；軸置中（無角度）時回 false
    public static bool joystick_analog_angle(int j, ref float angle)
    {
        float x = joystick_axis_threshold(j, joystick[j].x), y = joystick_axis_threshold(j, joystick[j].y);

        if (x != 0)
        {
            angle += MathF.Atan(-y / x);
            angle += (x < 0) ? -(MathF.PI / 2) : (MathF.PI / 2);
            return true;
        }
        else if (y != 0)
        {
            angle += y < 0 ? MathF.PI : 0;
            return true;
        }

        return false;
    }

    /* 回傳 0..joystick_analog_max，表示某個指派的鈕已按下、
     * 或某個指派的軸/帽已朝指派方向移動。 */
    private static int check_assigned(int j, Joystick_assignment[] assignment)
    {
        int result = 0;

        for (int i = 0; i < 2; i++)
        {
            int temp = 0;

            switch (assignment[i].type)
            {
            case Joystick_assignment_types.NONE:
                continue;

            case Joystick_assignment_types.AXIS:
                temp = Globals.Joystick.GetAxis(j, assignment[i].num);

                if (assignment[i].negative_axis)
                    temp = -temp;
                break;

            case Joystick_assignment_types.BUTTON:
                temp = Globals.Joystick.GetButton(j, assignment[i].num) ? joystick_analog_max : 0;
                break;

            case Joystick_assignment_types.HAT:
                temp = Globals.Joystick.GetHat(j, assignment[i].num);

                if (assignment[i].x_axis)
                    temp &= SDL_HAT_LEFT | SDL_HAT_RIGHT;
                else
                    temp &= SDL_HAT_UP | SDL_HAT_DOWN;

                if (assignment[i].negative_axis)
                    temp &= SDL_HAT_LEFT | SDL_HAT_UP;
                else
                    temp &= SDL_HAT_RIGHT | SDL_HAT_DOWN;

                temp = temp != 0 ? joystick_analog_max : 0;
                break;
            }

            if (temp > result)
                result = temp;
        }

        return result;
    }

    // 更新單一搖桿狀態
    public static void poll_joystick(int j)
    {
        if (!joystick[j].open)
            return;

        Globals.Joystick.Update();

        // 自上次 poll 以來是否有方向/動作被按下
        joystick[j].input_pressed = false;

        // 方向/動作是否已按住夠久以模擬重複按鍵
        bool repeat = joystick[j].joystick_delay < Globals.Clock.Ticks;

        // 更新方向狀態
        for (int d = 0; d < joystick[j].direction.Length; d++)
        {
            bool old = joystick[j].direction[d];

            joystick[j].analog_direction[d] = check_assigned(j, joystick[j].assignment[d]);
            joystick[j].direction[d] = joystick[j].analog_direction[d] > (joystick_analog_max / 2);
            joydown |= joystick[j].direction[d];

            joystick[j].direction_pressed[d] = joystick[j].direction[d] && (!old || repeat);
            joystick[j].input_pressed |= joystick[j].direction_pressed[d];
        }

        joystick[j].x = -joystick[j].analog_direction[3] + joystick[j].analog_direction[1];
        joystick[j].y = -joystick[j].analog_direction[0] + joystick[j].analog_direction[2];

        // 更新動作狀態
        for (int d = 0; d < joystick[j].action.Length; d++)
        {
            bool old = joystick[j].action[d];

            joystick[j].action[d] = check_assigned(j, joystick[j].assignment[d + joystick[j].direction.Length]) > (joystick_analog_max / 2);
            joydown |= joystick[j].action[d];

            joystick[j].action_pressed[d] = joystick[j].action[d] && (!old || repeat);
            joystick[j].input_pressed |= joystick[j].action_pressed[d];
        }

        joystick[j].confirm = joystick[j].action[0] || joystick[j].action[4];
        joystick[j].cancel = joystick[j].action[1] || joystick[j].action[5];

        // 若有新輸入，重設按鍵重複延遲
        if (joystick[j].input_pressed)
            joystick[j].joystick_delay = Globals.Clock.Ticks + (uint)joystick_repeat_delay;
    }

    // 更新所有搖桿狀態
    public static void poll_joysticks()
    {
        joydown = false;

        for (int j = 0; j < joysticks; j++)
            poll_joystick(j);
    }

    // 送出指定鍵的 KEYDOWN 與 KEYUP 事件
    public static void push_key(int key)
    {
        Globals.Joystick.PushKey(key);
    }

    // 偷懶地把搖桿假裝成鍵盤（選單用）
    public static void push_joysticks_as_keyboard()
    {
        const int confirm = SdlKeys.SDL_SCANCODE_RETURN, cancel = SdlKeys.SDL_SCANCODE_ESCAPE;
        int[] direction = { SdlKeys.SDL_SCANCODE_UP, SdlKeys.SDL_SCANCODE_RIGHT, SdlKeys.SDL_SCANCODE_DOWN, SdlKeys.SDL_SCANCODE_LEFT };

        poll_joysticks();

        for (int j = 0; j < joysticks; j++)
        {
            if (!joystick[j].input_pressed)
                continue;

            if (joystick[j].confirm)
                push_key(confirm);
            if (joystick[j].cancel)
                push_key(cancel);

            for (int d = 0; d < joystick[j].direction_pressed.Length; d++)
            {
                if (joystick[j].direction_pressed[d])
                    push_key(direction[d]);
            }
        }
    }

    // 初始化搖桿系統並載入指派
    public static void init_joysticks()
    {
        if (ignore_joystick)
            return;

        joysticks = Globals.Joystick.NumJoysticks;
        joystick = new JoystickType[joysticks];

        for (int j = 0; j < joysticks; j++)
        {
            joystick[j] = new JoystickType { index = j, open = true };

            // 對應原版開啟搖桿後讀名稱/軸鈕帽數的偵測訊息（此處不輸出 stdout）。
            if (!load_joystick_assignments(j))
                reset_joystick_assignments(j);
        }
    }

    // 反初始化搖桿系統並儲存指派
    public static void deinit_joysticks()
    {
        if (ignore_joystick)
            return;

        for (int j = 0; j < joysticks; j++)
        {
            if (joystick[j].open)
                save_joystick_assignments(j);
        }
        // handle 的關閉/子系統 Quit 由 App 的 SdlJoystick.Dispose 負責。
    }

    public static void reset_joystick_assignments(int j)
    {
        // 預設：前 2 軸、第 1 帽、前 6 鈕
        for (int a = 0; a < joystick[j].assignment.Length; a++)
        {
            // 清空指派
            for (int i = 0; i < joystick[j].assignment[a].Length; i++)
                joystick[j].assignment[a][i].type = Joystick_assignment_types.NONE;

            if (a < 4)
            {
                if (Globals.Joystick.NumAxes(j) >= 2)
                {
                    joystick[j].assignment[a][0].type = Joystick_assignment_types.AXIS;
                    joystick[j].assignment[a][0].num = (a + 1) % 2;
                    joystick[j].assignment[a][0].negative_axis = (a == 0 || a == 3);
                }

                if (Globals.Joystick.NumHats(j) >= 1)
                {
                    joystick[j].assignment[a][1].type = Joystick_assignment_types.HAT;
                    joystick[j].assignment[a][1].num = 0;
                    joystick[j].assignment[a][1].x_axis = (a == 1 || a == 3);
                    joystick[j].assignment[a][1].negative_axis = (a == 0 || a == 3);
                }
            }
            else
            {
                if (a - 4 < Globals.Joystick.NumButtons(j))
                {
                    joystick[j].assignment[a][0].type = Joystick_assignment_types.BUTTON;
                    joystick[j].assignment[a][0].num = a - 4;
                }
            }
        }

        joystick[j].analog = false;
        joystick[j].sensitivity = 5;
        joystick[j].threshold = 5;
    }

    /* 已知限制：搖桿指派的持久化 (load/save_joystick_assignments) 依賴 config_file.c 的 INI 讀寫，
     * 該模組未移植。因此 load 一律回 false（觸發 reset 預設指派），save 為 no-op。
     * 影響：指派不會跨次啟動保存，但每次以合理預設（前 2 軸 + 第 1 帽 + 前 6 鈕）開局，仍可遊玩。 */
    public static bool load_joystick_assignments(int j) => false;

    public static bool save_joystick_assignments(int j) => true;

    private static readonly string[] assignment_names =
    {
        "up",
        "right",
        "down",
        "left",
        "fire",
        "change fire",
        "left sidekick",
        "right sidekick",
        "menu",
        "pause",
    };

    // 以逗號分隔列出指派的搖桿功能
    public static void joystick_assignments_to_string(ref string buffer, Joystick_assignment[] assignments)
    {
        buffer = "";

        bool comma = false;
        for (int i = 0; i < assignments.Length; i++)
        {
            if (assignments[i].type == Joystick_assignment_types.NONE)
                continue;

            buffer += (comma ? ", " : "") + assignment_to_code(in assignments[i]);

            comma = true;
        }
    }

    /* 給一個搖桿指派的簡短 (<=6 字元) 識別碼 */
    private static string assignment_to_code(in Joystick_assignment assignment)
    {
        switch (assignment.type)
        {
        case Joystick_assignment_types.AXIS:
            return $"AX {assignment.num + 1}{(assignment.negative_axis ? '-' : '+')}";

        case Joystick_assignment_types.BUTTON:
            return $"BTN {assignment.num + 1}";

        case Joystick_assignment_types.HAT:
            return $"H {assignment.num + 1}{(assignment.x_axis ? 'X' : 'Y')}{(assignment.negative_axis ? '-' : '+')}";

        case Joystick_assignment_types.NONE:
        default:
            return "";
        }
    }

    // 擷取搖桿輸入以設定指派；偵測到非搖桿輸入則回 false
    public static bool detect_joystick_assignment(int j, out Joystick_assignment assignment)
    {
        assignment = default;

        // 取得初始狀態以比對是否有變動
        int axes = Globals.Joystick.NumAxes(j);
        int[] axis = new int[axes];
        for (int i = 0; i < axes; i++)
            axis[i] = Globals.Joystick.GetAxis(j, i);

        int buttons = Globals.Joystick.NumButtons(j);
        bool[] button = new bool[buttons];
        for (int i = 0; i < buttons; i++)
            button[i] = Globals.Joystick.GetButton(j, i);

        int hats = Globals.Joystick.NumHats(j);
        int[] hat = new int[hats];
        for (int i = 0; i < hats; i++)
            hat[i] = Globals.Joystick.GetHat(j, i);

        bool detected = false;

        while (true)
        {
            Nortsong.setFrameCount(1);

            // NETWORK_KEEP_ALIVE(): network.c 未移植，略過。

            Nortsong.delayUntilElapsed();

            Keyboard.handleSdlEvents();

            for (int i = 0; i < axes; ++i)
            {
                int temp = Globals.Joystick.GetAxis(j, i);

                if (Math.Abs(temp - axis[i]) > joystick_analog_max * 2 / 3)
                {
                    assignment.type = Joystick_assignment_types.AXIS;
                    assignment.num = i;
                    assignment.negative_axis = temp < axis[i];
                    detected = true;
                    break;
                }
            }

            for (int i = 0; i < buttons; ++i)
            {
                bool new_button = Globals.Joystick.GetButton(j, i);
                bool changed = button[i] ^ new_button;

                if (!changed)
                    continue;

                if (!new_button) // 鈕被放開
                {
                    button[i] = new_button;
                }
                else             // 鈕被按下
                {
                    assignment.type = Joystick_assignment_types.BUTTON;
                    assignment.num = i;
                    detected = true;
                    break;
                }
            }

            for (int i = 0; i < hats; ++i)
            {
                int new_hat = Globals.Joystick.GetHat(j, i);
                int changed = hat[i] ^ new_hat;

                if (changed == 0)
                    continue;

                if ((new_hat & changed) == SDL_HAT_CENTERED) // 帽被置中
                {
                    hat[i] = new_hat;
                }
                else
                {
                    assignment.type = Joystick_assignment_types.HAT;
                    assignment.num = i;
                    assignment.x_axis = (changed & (SDL_HAT_LEFT | SDL_HAT_RIGHT)) != 0;
                    assignment.negative_axis = (changed & (SDL_HAT_LEFT | SDL_HAT_UP)) != 0;
                    detected = true;
                }
            }

            if (detected || Keyboard.hasInput(InputFlags.INPUT_NO_MOTION))
                break;
        }

        return detected;
    }

    // 比較搖桿指派的相關部分是否相等
    public static bool joystick_assignment_cmp(ref Joystick_assignment a, ref Joystick_assignment b)
    {
        if (a.type == b.type)
        {
            switch (a.type)
            {
            case Joystick_assignment_types.NONE:
                return true;
            case Joystick_assignment_types.AXIS:
                return (a.num == b.num) &&
                       (a.negative_axis == b.negative_axis);
            case Joystick_assignment_types.BUTTON:
                return (a.num == b.num);
            case Joystick_assignment_types.HAT:
                return (a.num == b.num) &&
                       (a.x_axis == b.x_axis) &&
                       (a.negative_axis == b.negative_axis);
            }
        }

        return false;
    }
}
