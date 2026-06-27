using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/config_file.c + config_file.h —— OpenTyrian 的 INI 設定程式庫。
///
/// 以慣用 C# 表示原版的 C 結構：
/// <list type="bullet">
///   <item><see cref="ConfigFile"/> = sections 串列（對應 C 的 <c>Config</c>；C# 既有的 static
///         <c>Config</c>（config.c）名稱衝突，故文件型別改名 <c>ConfigFile</c>）。</item>
///   <item><see cref="ConfigSection"/> = type/name + options 串列。</item>
///   <item><see cref="ConfigOption"/> = key + 單值（item）或值串列（list）。</item>
/// </list>
/// C 的 ConfigString SSO union 直接用 <see cref="string"/>（字元視為 0..255 位元組）。
///
/// 檔案格式逐位元組對齊原版（config_write / write_field / config_parse / read_field）。
/// </summary>
internal sealed class ConfigFile
{
    /// <summary>對應 C <c>Config::sections</c>。</summary>
    public readonly List<ConfigSection> sections = new();

    /// <summary>對應 config_init —— 清空為空設定。</summary>
    public void Init()
    {
        sections.Clear();
    }

    /* ----- config section accessors/manipulators -- by type, name ----- */

    /// <summary>對應 config_add_section（name 可為 null）。</summary>
    public ConfigSection AddSection(string type, string? name)
    {
        var section = new ConfigSection(type, name);
        sections.Add(section);
        return section;
    }

    /// <summary>對應 config_find_sections —— 依 type 列舉。</summary>
    public IEnumerable<ConfigSection> FindSections(string type)
    {
        foreach (var s in sections)
            if (s.type == type)
                yield return s;
    }

    /// <summary>對應 config_find_section —— 依 type+name 尋找（name 為 null 比對 null）。</summary>
    public ConfigSection? FindSection(string type, string? name)
    {
        foreach (var section in sections)
        {
            if (section.type == type)
            {
                // (section_name == NULL || name == NULL) ? section_name == name : strcmp(...) == 0
                if ((section.name == null || name == null)
                        ? section.name == name
                        : section.name == name)
                    return section;
            }
        }
        return null;
    }

    /// <summary>對應 config_find_or_add_section。</summary>
    public ConfigSection FindOrAddSection(string type, string? name)
    {
        return FindSection(type, name) ?? AddSection(type, name);
    }

    /* ----- config parser ----- */

    /// <summary>對應 config_parse —— 從位元組串流解析（含 config_init 清空）。</summary>
    public static bool Parse(ConfigFile config, Stream file)
    {
        config.Init();

        // 讀入整個檔案為可變字元緩衝（位元組→字元，保留 0..255），尾端補 '\0'。
        byte[] raw;
        using (var ms = new MemoryStream())
        {
            file.CopyTo(ms);
            raw = ms.ToArray();
        }
        int len = raw.Length;
        char[] buffer = new char[len + 1];
        for (int k = 0; k < len; ++k)
            buffer[k] = (char)raw[k];
        buffer[len] = '\0';

        ConfigSection? section = null;
        ConfigOption? option = null;

        int line = 0;
        while (line < len)
        {
            int i = line;

            Directive directive = MatchDirective(buffer, ref i);

            switch (directive)
            {
            case Directive.Invalid:
                break;

            case Directive.Section:
                {
                    if (!ParseField(buffer, ref i, out int typeStart, out int typeLen))
                        break;

                    bool hasName = ParseField(buffer, ref i, out int nameStart, out int nameLen);

                    section = config.AddSection(
                        new string(buffer, typeStart, typeLen),
                        hasName ? new string(buffer, nameStart, nameLen) : null);
                    option = null;
                }
                break;

            case Directive.Item:
            case Directive.List:
                {
                    if (section == null)
                        break;

                    if (!ParseField(buffer, ref i, out int keyStart, out int keyLen))
                        break;

                    if (!ParseField(buffer, ref i, out int valueStart, out int valueLen))
                        break;

                    string key = new string(buffer, keyStart, keyLen);
                    string value = new string(buffer, valueStart, valueLen);

                    if (directive == Directive.Item)
                    {
                        option = section.SetOption(key, value);
                    }
                    else
                    {
                        if (option == null || option.key != key)
                            option = section.GetOrSetOption(key, null);
                        option = option.AddValue(value);
                    }
                }
                break;
            }

            // 前進到下一行（消費單一 '\n' 或 '\r'，與原版相同）。
            int j = line;
            while (j < len && buffer[j] != '\n' && buffer[j] != '\r')
                ++j;
            if (j < len)
                ++j;
            line = j;
        }

        return true;
    }

    private enum Directive { Invalid = 0, Section, Item, List }

    private static bool IsWhitespace(char c) => c == '\t' || c == ' ';
    private static bool IsEnd(char c) => c == '\0' || c == '\n' || c == '\r';
    private static bool IsWhitespaceOrEnd(char c) => IsWhitespace(c) || IsEnd(c);

    private static Directive MatchDirective(char[] buffer, ref int index)
    {
        int i = index;

        while (IsWhitespace(buffer[i]))
            ++i;

        Directive directive;

        if (StrncmpKeyword(buffer, i, "section"))
        {
            directive = Directive.Section;
            i += 7;
        }
        else if (StrncmpKeyword(buffer, i, "item"))
        {
            directive = Directive.Item;
            i += 4;
        }
        else if (StrncmpKeyword(buffer, i, "list"))
        {
            directive = Directive.List;
            i += 4;
        }
        else
        {
            return Directive.Invalid;
        }

        if (!IsWhitespaceOrEnd(buffer[i]))
            return Directive.Invalid;

        index = i;
        return directive;
    }

    private static bool StrncmpKeyword(char[] buffer, int i, string kw)
    {
        // 對應 strncmp(kw, &buffer[i], len) == 0；buffer 必有 '\0' 終止。
        for (int k = 0; k < kw.Length; ++k)
            if (buffer[i + k] != kw[k])
                return false;
        return true;
    }

    private static bool MatchNonquoteField(char[] buffer, ref int index, out int length)
    {
        int i = index;

        for (; ; ++i)
        {
            char c = buffer[i];

            if (IsWhitespaceOrEnd(c))
            {
                break;
            }
            else if (c <= ' ' || c > '~' || c == '#' || c == '\'' || c == '"')
            {
                length = 0;
                return false;
            }
        }

        length = i - index;
        index = i;

        return length > 0;
    }

    private static bool ParseQuoteField(char[] buffer, ref int index, out int length)
    {
        int i = index;
        int o = index;

        char quote = buffer[i];

        for (; ; )
        {
            char c = buffer[++i];

            if (c == quote)
            {
                ++i;
                break;
            }
            else if (c == '\\')
            {
                c = buffer[++i];
                if (c == quote)
                {
                    buffer[o++] = quote;
                }
                else
                {
                    switch (c)
                    {
                    case 't':
                        buffer[o++] = '\t';
                        break;
                    case 'n':
                        buffer[o++] = '\n';
                        break;
                    case 'r':
                        buffer[o++] = '\r';
                        break;
                    case '\\':
                        buffer[o++] = '\\';
                        break;
                    case 'x':
                        {
                            // 解析兩個十六進位數字
                            c = buffer[++i];
                            int m = (c >= '0' && c <= '9') ? '0' :
                                    (c >= 'a' && c <= 'f') ? 'a' - 10 :
                                    (c >= 'A' && c <= 'F') ? 'A' - 10 : 0;
                            if (m == 0)
                            {
                                length = 0;
                                return false;
                            }
                            int h = c - m;
                            c = buffer[++i];
                            m = (c >= '0' && c <= '9') ? '0' :
                                (c >= 'a' && c <= 'f') ? 'a' - 10 :
                                (c >= 'A' && c <= 'F') ? 'A' - 10 : 0;
                            if (m == 0)
                            {
                                length = 0;
                                return false;
                            }
                            buffer[o++] = (char)(((h << 4) | (c - m)) & 0xff);
                        }
                        break;
                    default:
                        length = 0;
                        return false;
                    }
                }
            }
            else if (c >= ' ' && c <= '~')
            {
                buffer[o++] = c;
            }
            else
            {
                length = 0;
                return false;
            }
        }

        length = o - index;
        index = i;

        return true;
    }

    private static bool ParseField(char[] buffer, ref int index, out int start, out int length)
    {
        int i = index;

        while (IsWhitespace(buffer[i]))
            ++i;

        start = i;

        if (buffer[i] == '"' || buffer[i] == '\'')
        {
            if (!ParseQuoteField(buffer, ref i, out length))
                return false;
        }
        else
        {
            if (!MatchNonquoteField(buffer, ref i, out length))
                return false;
        }

        if (!IsWhitespaceOrEnd(buffer[i]))
            return false;

        index = i;
        return true;
    }

    /* ----- config writer ----- */

    /// <summary>對應 config_write —— 寫出到位元組串流（逐位元組對齊 write_field）。</summary>
    public static void Write(ConfigFile config, Stream file)
    {
        var sb = new StringBuilder();

        foreach (var section in config.sections)
        {
            sb.Append("section ");
            WriteField(sb, section.type);
            if (section.name != null)
            {
                sb.Append(' ');
                WriteField(sb, section.name);
            }
            sb.Append('\n');

            foreach (var option in section.options)
            {
                if (option.ValuesCount == 0 && option.ItemValue != null)
                {
                    sb.Append("\titem ");
                    WriteField(sb, option.key);
                    sb.Append(' ');
                    WriteField(sb, option.ItemValue);
                    sb.Append('\n');
                }
                else
                {
                    // list 選項：對每個值各寫一行；空 list 不寫任何行。
                    if (option.ListValues != null)
                    {
                        foreach (var value in option.ListValues)
                        {
                            sb.Append("\tlist ");
                            WriteField(sb, option.key);
                            sb.Append(' ');
                            WriteField(sb, value);
                            sb.Append('\n');
                        }
                    }
                }
            }

            sb.Append('\n');
        }

        // 所有輸出位元組皆 ASCII（write_field 將非可列印者轉義成 \xNN），以 Latin-1 寫出。
        string text = sb.ToString();
        byte[] bytes = new byte[text.Length];
        for (int k = 0; k < text.Length; ++k)
            bytes[k] = (byte)text[k];
        file.Write(bytes, 0, bytes.Length);
    }

    private static void WriteField(StringBuilder sb, string field)
    {
        sb.Append('\'');

        foreach (char ch in field)
        {
            char c = ch;
            switch (c)
            {
                case '\t':
                    sb.Append('\\'); sb.Append('t');
                    break;
                case '\n':
                    sb.Append('\\'); sb.Append('n');
                    break;
                case '\r':
                    sb.Append('\\'); sb.Append('r');
                    break;
                case '\'':
                case '\\':
                    sb.Append('\\'); sb.Append(c);
                    break;
                default:
                    if (c >= ' ' && c <= '~')
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        int v = c & 0xff;
                        sb.Append('\\');
                        sb.Append('x');
                        int n = (v >> 4) & 0x0f;
                        sb.Append((char)((n < 10 ? '0' : ('a' - 10)) + n));
                        n = v & 0x0f;
                        sb.Append((char)((n < 10 ? '0' : ('a' - 10)) + n));
                    }
                    break;
            }
        }

        sb.Append('\'');
    }
}

/// <summary>對應 config_file.h 的 ConfigSection。</summary>
internal sealed class ConfigSection
{
    /// <summary>段的型別（對應 C <c>type</c>）。</summary>
    public string type;

    /// <summary>段的選擇性名稱（對應 C <c>name</c>，可為 null）。</summary>
    public string? name;

    /// <summary>段中的選項（對應 C <c>options</c>）。</summary>
    public readonly List<ConfigOption> options = new();

    public ConfigSection(string type, string? name)
    {
        this.type = type;
        this.name = name;
    }

    /* ----- by key ----- */

    private ConfigOption? GetOptionInternal(string key)
    {
        foreach (var option in options)
            if (option.key == key)
                return option;
        return null;
    }

    /// <summary>對應 config_get_option。</summary>
    public ConfigOption? GetOption(string key) => GetOptionInternal(key);

    private ConfigOption Append(string key, string? value)
    {
        var option = new ConfigOption(key, value);
        options.Add(option);
        return option;
    }

    /// <summary>對應 config_set_option（value 為 null 表設空 list / 刪除 item）。</summary>
    public ConfigOption SetOption(string key, string? value)
    {
        var option = GetOptionInternal(key);
        if (option != null)
            return option.SetValue(value);
        return Append(key, value);
    }

    /// <summary>對應 config_get_or_set_option。</summary>
    public ConfigOption GetOrSetOption(string key, string? value)
    {
        return GetOptionInternal(key) ?? Append(key, value);
    }

    /// <summary>對應 config_set_string_option。</summary>
    public void SetStringOption(string key, string? value) => SetOption(key, value);

    /// <summary>對應 config_get_string_option。</summary>
    public bool GetStringOption(string key, out string value)
    {
        var option = GetOptionInternal(key);
        if (option != null)
        {
            string? v = option.GetValue();
            if (v != null)
            {
                value = v;
                return true;
            }
        }
        value = string.Empty;
        return false;
    }

    /// <summary>對應 config_get_or_set_string_option。</summary>
    public string? GetOrSetStringOption(string key, string? value)
    {
        if (!GetStringOption(key, out string existing))
        {
            SetStringOption(key, value);
            return value;
        }
        return existing;
    }

    private static readonly string[][] bool_values =
    {
        new[] { "0", "1" },
        new[] { "no", "yes" },
        new[] { "off", "on" },
        new[] { "false", "true" },
    };

    /// <summary>對應 config_set_bool_option。</summary>
    public void SetBoolOption(string key, bool value, ConfigBoolStyle style)
    {
        SetOption(key, bool_values[(int)style][value ? 1 : 0]);
    }

    /// <summary>對應 config_get_bool_option。</summary>
    public bool GetBoolOption(string key, out bool value)
    {
        if (GetStringOption(key, out string s))
        {
            for (int i = 0; i < bool_values.Length; ++i)
            {
                for (int j = 0; j < bool_values[i].Length; ++j)
                {
                    if (s == bool_values[i][j])
                    {
                        value = j != 0;
                        return true;
                    }
                }
            }
        }
        value = false;
        return false;
    }

    /// <summary>對應 config_get_or_set_bool_option。</summary>
    public bool GetOrSetBoolOption(string key, bool value, ConfigBoolStyle style)
    {
        if (!GetBoolOption(key, out bool existing))
        {
            SetBoolOption(key, value, style);
            return value;
        }
        return existing;
    }

    /// <summary>對應 config_set_int_option（"%i" → 十進位）。</summary>
    public void SetIntOption(string key, int value)
    {
        SetOption(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>對應 config_get_int_option（"%i" 全字串消費）。</summary>
    public bool GetIntOption(string key, out int value)
    {
        if (GetStringOption(key, out string s) && TryParseCInt(s, out value))
            return true;
        value = 0;
        return false;
    }

    /// <summary>對應 config_get_or_set_int_option。</summary>
    public int GetOrSetIntOption(string key, int value)
    {
        if (!GetIntOption(key, out int existing))
        {
            SetIntOption(key, value);
            return value;
        }
        return existing;
    }

    // 模擬 C 的 sscanf("%i%n") + 要求整個字串被消費（允許前導空白）。
    private static bool TryParseCInt(string s, out int value)
    {
        value = 0;
        int i = 0, n = s.Length;

        while (i < n && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' ||
                         s[i] == '\r' || s[i] == '\v' || s[i] == '\f'))
            ++i;

        bool neg = false;
        if (i < n && (s[i] == '+' || s[i] == '-'))
        {
            neg = s[i] == '-';
            ++i;
        }

        int b = 10;
        if (i < n && s[i] == '0')
        {
            if (i + 1 < n && (s[i + 1] == 'x' || s[i + 1] == 'X'))
            {
                b = 16;
                i += 2;
            }
            else
            {
                b = 8;
            }
        }

        long acc = 0;
        int digits = 0;
        for (; i < n; ++i)
        {
            char c = s[i];
            int d;
            if (c >= '0' && c <= '9') d = c - '0';
            else if (c >= 'a' && c <= 'f') d = c - 'a' + 10;
            else if (c >= 'A' && c <= 'F') d = c - 'A' + 10;
            else break;
            if (d >= b) break;
            acc = acc * b + d;
            ++digits;
        }

        if (digits == 0)
            return false;
        if (i != n) // 必須消費整個字串
            return false;

        value = (int)(neg ? -acc : acc);
        return true;
    }
}

/// <summary>
/// 對應 config_file.h 的 ConfigOption —— 一個 item（單值）或 list（值串列）。
///
/// C 的 union 標籤語意：
/// <list type="bullet">
///   <item>values_count == 0 且 value != null → item（單值）。</item>
///   <item>values_count == 0 且 value == null → 空 list 選項。</item>
///   <item>values_count &gt; 0 → list（非空值串列）。</item>
/// </list>
/// </summary>
internal sealed class ConfigOption
{
    /// <summary>選項鍵（對應 C <c>key</c>）。</summary>
    public string key;

    // item 時持有單值（null 表空 list 選項）；list 時為 null。
    private string? _value;
    // list 時持有值串列（count > 0）；item / 空 list 時為 null。
    private List<string>? _values;

    public ConfigOption(string key, string? value)
    {
        this.key = key;
        _value = value;
        _values = null;
    }

    /// <summary>對應 C <c>values_count</c>。</summary>
    public int ValuesCount => _values?.Count ?? 0;

    /// <summary>item 值（供 writer 使用；null 表空 list）。</summary>
    public string? ItemValue => _values == null ? _value : null;

    /// <summary>list 值串列（供 writer 使用；null 表非 list）。</summary>
    public IReadOnlyList<string>? ListValues => _values;

    /// <summary>對應 config_set_value（value 可為 null）。</summary>
    public ConfigOption SetValue(string? value)
    {
        _value = value;
        _values = null;
        return this;
    }

    /// <summary>對應 config_add_value（value 非 null）。</summary>
    public ConfigOption AddValue(string value)
    {
        // 將 'item' 轉為 'list'
        if (_values == null && _value != null)
        {
            _values = new List<string> { _value, value };
            _value = null;
        }
        else
        {
            _values ??= new List<string>();
            _values.Add(value);
        }
        return this;
    }

    /// <summary>對應 config_get_value（list 或無值回 null）。</summary>
    public string? GetValue()
    {
        if (_values != null) // values_count != 0
            return null;
        return _value;
    }

    /// <summary>對應 config_is_value_list。</summary>
    public bool IsValueList() => _values != null || _value == null;

    /// <summary>對應 config_get_value_count。</summary>
    public int ValueCount() => _values != null ? _values.Count : (_value == null ? 0 : 1);

    /// <summary>
    /// 對應 foreach_option_value / foreach_option_i_value 的值列舉：
    /// item → 單值；空 list → 無；list → 各值。
    /// </summary>
    public IReadOnlyList<string> GetValues()
    {
        if (_values != null)
            return _values;
        if (_value != null)
            return new[] { _value };
        return System.Array.Empty<string>();
    }
}

/// <summary>對應 config_file.h 的 ConfigBoolStyle。</summary>
internal enum ConfigBoolStyle
{
    ZERO_ONE = 0,
    NO_YES = 1,
    OFF_ON = 2,
    FALSE_TRUE = 3,
}
