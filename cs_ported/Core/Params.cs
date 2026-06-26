namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/params.c —— 命令列參數處理。網路相關選項在本版（不含網路）視為不支援。
/// </summary>
internal static class Params
{
    public static bool richMode = false, constantPlay = false, constantDie = false;

    private static readonly string[] pars =
        { "LOOT", "RECORD", "NOJOY", "CONSTANT", "DEATH", "NOSOUND", "NOXMAS", "YESXMAS" };

    public static void JE_paramCheck(int argc, string[] argv)
    {
        Options[] options =
        {
            new('h', 'h', "help", false),
            new('s', 's', "no-sound", false),
            new('j', 'j', "no-joystick", false),
            new('x', 'x', "no-xmas", false),
            new('t', 't', "data", true),
            new('n', 'n', "net", true),
            new(256, (char)0, "net-player-name", true),
            new(257, (char)0, "net-player-number", true),
            new('p', 'p', "net-port", true),
            new('d', 'd', "net-delay", true),
            new('X', 'X', "xmas", false),
            new('c', 'c', "constant", false),
            new('k', 'k', "death", false),
            new('r', 'r', "record", false),
            new('l', 'l', "loot", false),
            new(0, (char)0, null, false),
        };

        Option option;

        for (; ; )
        {
            option = ArgParse.parse_args(argc, argv, options);

            if (option.value == ArgParse.NOT_OPTION)
                break;

            switch (option.value)
            {
            case ArgParse.INVALID_OPTION:
            case ArgParse.AMBIGUOUS_OPTION:
            case ArgParse.OPTION_MISSING_ARG:
                Console.Error.WriteLine($"Try `{argv[0]} --help' for more information.");
                throw new TyrianHaltException(1);

            case 'h':
                Console.WriteLine(
                    $"Usage: {argv[0]} [OPTION...]\n\n" +
                    "Options:\n" +
                    "  -h, --help                   Show help about options\n\n" +
                    "  -s, --no-sound               Disable audio\n" +
                    "  -j, --no-joystick            Disable joystick/gamepad input\n" +
                    "  -x, --no-xmas                Disable Christmas mode\n\n" +
                    "  -t, --data=DIR               Set Tyrian data directory\n");
                throw new TyrianHaltException(0);

            case 's':
                Loudness.audio_disabled = true;
                break;
            case 'j':
                Joystick.ignore_joystick = true;
                break;
            case 'x':
                Xmas.xmas = false;
                break;
            case 't':
                CFile.custom_data_dir = option.arg;
                break;

            case 'n':
            case 'p':
            case 'd':
            case 256:
            case 257:
                Console.Error.WriteLine($"{argv[0]}: networking is not supported in this build");
                break;

            case 'X':
                Xmas.xmas = true;
                break;
            case 'c':
                constantPlay = true;
                break;
            case 'k':
                constantDie = true;
                break;
            case 'r':
                Varz.record_demo = true;
                break;
            case 'l':
                richMode = true;
                break;
            }
        }

        // legacy parameter support
        for (int i = option.argn; i < argc; ++i)
        {
            string up = argv[i].ToUpperInvariant();
            for (int j = 0; j < pars.Length; ++j)
            {
                if (up == pars[j])
                {
                    switch (j)
                    {
                        case 0: richMode = true; break;
                        case 1: Varz.record_demo = true; break;
                        case 2: Joystick.ignore_joystick = true; break;
                        case 3: constantPlay = true; break;
                        case 4: constantDie = true; break;
                        case 5: Loudness.audio_disabled = true; break;
                        case 6: Xmas.xmas = false; break;
                        case 7: Xmas.xmas = true; break;
                    }
                }
            }
        }
    }
}
