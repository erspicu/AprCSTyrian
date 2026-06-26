namespace AprCSTyrian.Core;

/// <summary>對應 arg_parse.h:Options（選項定義）。</summary>
internal struct Options
{
    public int value;
    public char short_opt;
    public string? long_opt;
    public bool has_arg;
    public Options(int value, char short_opt, string? long_opt, bool has_arg)
    { this.value = value; this.short_opt = short_opt; this.long_opt = long_opt; this.has_arg = has_arg; }
}

/// <summary>對應 arg_parse.h:Option（解析結果）。</summary>
internal struct Option
{
    public int value;
    public string? arg;
    public int argn;
}

/// <summary>
/// 移植 sources/src/arg_parse.c —— getopt_long() 的重實作。
/// argv 為 string[]（argv[0]=程式名）；指標改為 substring；保留 C 的靜態解析狀態（一次性使用）。
/// </summary>
internal static class ArgParse
{
    public const int NOT_OPTION = 0;
    public const int INVALID_OPTION = -1;
    public const int AMBIGUOUS_OPTION = -2;
    public const int OPTION_MISSING_ARG = -3;

    private static int argn = 1;
    private static bool no_more_options = false;
    private static int first_nonopt = 1;
    private static int shortOffset = 1;

    private static bool IsTerminator(in Options o) => o.short_opt == 0 && o.long_opt == null;

    public static Option parse_args(int argc, string[] argv, Options[] options)
    {
        Option option = new() { value = NOT_OPTION, arg = null, argn = 0 };
        option.argn = first_nonopt;

        while (argn < argc)
        {
            int arg_len = argv[argn].Length;

            if (!no_more_options && argv[argn][0] == '-' && arg_len > 1)
            {
                option.argn = argn;

                if (argv[argn][1] == '-') // "--"
                {
                    if (arg_len == 2)
                    {
                        ++argn;
                        no_more_options = true;
                    }
                    else
                    {
                        argn = parse_long_opt(argc, argv, options, ref option);
                    }
                }
                else
                {
                    argn = parse_short_opt(argc, argv, options, ref option);
                }

                permute(argv, ref first_nonopt, ref option.argn, argn);

                if (no_more_options)
                    ++option.argn;
                break;
            }
            else
            {
                ++argn;
            }
        }

        return option;
    }

    private static void permute(string[] argv, ref int first_nonopt, ref int first_opt, int after_opt)
    {
        int nonopts = first_opt - first_nonopt;

        for (int i = first_opt; i < after_opt; ++i)
        {
            for (int j = i; j > first_nonopt; --j)
            {
                (argv[j - 1], argv[j]) = (argv[j], argv[j - 1]);
            }
            ++first_nonopt;
        }

        first_opt -= nonopts;
    }

    private static int parse_short_opt(int argc, string[] argv, Options[] options, ref Option option)
    {
        int an = option.argn;
        string arg = argv[an];
        int arg_len = arg.Length;

        bool arg_attached = (shortOffset + 1 < arg_len);
        bool last_in_argv = (an == argc - 1);

        option.value = INVALID_OPTION;

        for (int oi = 0; !IsTerminator(options[oi]); ++oi)
        {
            if (options[oi].short_opt != 0 && options[oi].short_opt == arg[shortOffset])
            {
                option.value = options[oi].value;

                if (options[oi].has_arg)
                {
                    if (arg_attached)
                    {
                        option.arg = arg.Substring(shortOffset + 1);
                        shortOffset = arg_len;
                    }
                    else if (!last_in_argv)
                    {
                        option.arg = argv[++an];
                        shortOffset = arg_len;
                    }
                    else
                    {
                        option.value = OPTION_MISSING_ARG;
                        break;
                    }
                }
                break;
            }
        }

        switch (option.value)
        {
            case INVALID_OPTION:
                Console.Error.WriteLine($"{argv[0]}: invalid option -- '{argv[option.argn][shortOffset]}'");
                break;
            case OPTION_MISSING_ARG:
                Console.Error.WriteLine($"{argv[0]}: option requires an argument -- '{argv[option.argn][shortOffset]}'");
                break;
        }

        if (++shortOffset >= arg_len)
        {
            ++an;
            shortOffset = 1;
        }

        return an;
    }

    private static int parse_long_opt(int argc, string[] argv, Options[] options, ref Option option)
    {
        int an = option.argn;
        string full = argv[an];
        string arg = full.Substring(2); // ignore "--"
        int arg_len = arg.Length;
        int eq = arg.IndexOf('=');
        int arg_opt_len = eq < 0 ? arg_len : eq;

        bool arg_attached = (arg_opt_len < arg_len);
        bool last_in_argv = (an == argc - 1);

        option.value = INVALID_OPTION;

        for (int oi = 0; !IsTerminator(options[oi]); ++oi)
        {
            string? lo = options[oi].long_opt;
            if (lo != null && arg_opt_len <= lo.Length && string.CompareOrdinal(lo, 0, arg, 0, arg_opt_len) == 0)
            {
                if (option.value != INVALID_OPTION)
                {
                    option.value = AMBIGUOUS_OPTION;
                    break;
                }

                option.value = options[oi].value;

                if (options[oi].has_arg)
                {
                    if (arg_attached)
                        option.arg = arg.Substring(arg_opt_len + 1);
                    else if (!last_in_argv)
                        option.arg = argv[++an];
                    else
                        option.value = OPTION_MISSING_ARG;
                }

                if (arg_opt_len == lo.Length) // exact match
                    break;
            }
        }

        switch (option.value)
        {
            case INVALID_OPTION:
                Console.Error.WriteLine($"{argv[0]}: unrecognized option '{argv[option.argn]}'");
                break;
            case AMBIGUOUS_OPTION:
                Console.Error.WriteLine($"{argv[0]}: option '{argv[option.argn]}' is ambiguous");
                break;
            case OPTION_MISSING_ARG:
                Console.Error.WriteLine($"{argv[0]}: option '{argv[option.argn]}' requires an argument");
                break;
        }

        ++an;
        return an;
    }
}
