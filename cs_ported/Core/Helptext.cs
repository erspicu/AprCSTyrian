namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/helptext.c —— 從 tyrian.hdt 載入加密 Pascal 字串（選單/說明/船艦資訊文字），
/// 及說明框繪製 JE_helpBox/JE_HBox。文字表以 string[] 儲存（字元即 CP437 byte，供字型繪製）。
/// </summary>
internal static class Helptext
{
    public const int MENU_MAX = 14;
    public const int DESTRUCT_MODES = 5;

    public static readonly byte[,] menuHelp = /* [14][11] */
    {
        {  1, 34,  2,  3,  4,  5,  0, 0, 0, 0, 0 },
        {  6,  7,  8,  9, 10, 11, 11, 12, 0, 0, 0 },
        { 13, 14, 15, 15, 16, 17, 12, 0, 0, 0, 0 },
        {  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        {  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        {  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        {  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        {  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        {  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        {  4, 30, 30,  3,  5,  0, 0, 0, 0, 0, 0 },
        {  0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 16, 17, 15, 15, 12,  0, 0, 0, 0, 0, 0 },
        { 31, 31, 31, 31, 32, 12,  0, 0, 0, 0, 0 },
        {  4, 34,  3,  5,  0, 0, 0, 0, 0, 0, 0 },
    };

    public static readonly string[] helpTxt = new string[39];
    public static readonly string[] pName = new string[21];
    public static readonly string[] miscText = new string[68];
    public static readonly string[] miscTextB = new string[5];
    public static readonly string[] keyName = new string[8];
    public static readonly string[] menuText = new string[7];
    public static readonly string[] outputs = new string[9];
    public static readonly string[] topicName = new string[6];
    public static readonly string[] mainMenuHelp = new string[34];
    public static readonly string[] inGameText = new string[6];
    public static readonly string[] detailLevel = new string[6];
    public static readonly string[] gameSpeedText = new string[5];
    public static readonly string[] inputDevices = new string[3];
    public static readonly string[] networkText = new string[4];
    public static readonly string[] difficultyNameB = new string[11];
    public static readonly string[] joyButtonNames = new string[5];
    public static readonly string[] superShips = new string[11];
    public static readonly string[] specialName = new string[9];
    public static readonly string[] destructHelp = new string[25];
    public static readonly string[] weaponNames = new string[17];
    public static readonly string[] destructModeName = new string[DESTRUCT_MODES];
    public static readonly string[][] shipInfo = NewJagged(13, 2);
    public static readonly string[][] menuInt = NewJagged(MENU_MAX + 1, 11);

    // 各表的字串緩衝大小（對應 C 的 sizeof）
    private const int SZ_helpTxt = 231, SZ_pName = 16, SZ_miscText = 42, SZ_miscTextB = 11, SZ_menuText = 21,
        SZ_outputs = 31, SZ_topicName = 21, SZ_mainMenuHelp = 66, SZ_inGameText = 21, SZ_detailLevel = 13,
        SZ_gameSpeedText = 13, SZ_inputDevices = 13, SZ_networkText = 22, SZ_difficultyNameB = 21,
        SZ_joyButtonNames = 21, SZ_superShips = 26, SZ_specialName = 10, SZ_destructHelp = 22, SZ_weaponNames = 17,
        SZ_destructModeName = 13, SZ_shipInfo = 256, SZ_menuInt = 18,
        SZ_episode_name = 31, SZ_difficulty_name = 21, SZ_gameplay_name = 26;

    private static string[][] NewJagged(int a, int b)
    {
        var x = new string[a][];
        for (int i = 0; i < a; ++i) x[i] = new string[b];
        return x;
    }

    private static readonly byte[] crypt_key = { 204, 129, 63, 255, 71, 19, 25, 62, 1, 99 };

    private static void decrypt_string(byte[] s, int len)
    {
        if (len == 0) return;
        for (int i = len - 1; ; --i)
        {
            s[i] ^= crypt_key[i % crypt_key.Length];
            if (i == 0) break;
            s[i] ^= s[i - 1];
        }
    }

    private static string read_encrypted_pascal_string(Stream f, int size)
    {
        byte len = CFile.read_u8(f);
        byte[] buffer = new byte[255];
        for (int i = 0; i < len; ++i) buffer[i] = CFile.read_u8(f);

        if (size == 0) return "";

        decrypt_string(buffer, len);

        int outLen = Math.Min(len, size - 1);
        var chars = new char[outLen];
        for (int i = 0; i < outLen; ++i) chars[i] = (char)buffer[i];
        return new string(chars);
    }

    private static void skip_pascal_string(Stream f)
    {
        byte len = CFile.read_u8(f);
        for (int i = 0; i < len; ++i) CFile.read_u8(f);
    }

    public static void JE_loadHelpText()
    {
        int[] menuInt_entries = { -1, 7, 9, 8, -1, -1, 11, -1, -1, -1, 6, 4, 6, 7, 5 };

        Stream f = CFile.dir_fopen_die(CFile.data_dir(), "tyrian.hdt", "rb");
        Episodes.episode1DataLoc = CFile.read_s32(f);

        ReadTable(f, helpTxt, SZ_helpTxt);
        ReadTable(f, pName, SZ_pName);
        ReadTable(f, miscText, SZ_miscText);
        ReadTable(f, miscTextB, SZ_miscTextB);
        ReadMenuInt(f, 6, menuInt_entries[6], SZ_menuInt);
        ReadTable(f, menuText, SZ_menuText);
        ReadTable(f, outputs, SZ_outputs);
        ReadTable(f, topicName, SZ_topicName);
        ReadTable(f, mainMenuHelp, SZ_mainMenuHelp);
        ReadMenuInt(f, 1, menuInt_entries[1], SZ_menuInt);
        ReadMenuInt(f, 2, menuInt_entries[2], SZ_menuInt);
        ReadMenuInt(f, 3, menuInt_entries[3], SZ_menuInt);
        ReadTable(f, inGameText, SZ_inGameText);
        ReadTable(f, detailLevel, SZ_detailLevel);
        ReadTable(f, gameSpeedText, SZ_gameSpeedText);
        ReadTable(f, Menus.episode_name, SZ_episode_name);
        ReadTable(f, Menus.difficulty_name, SZ_difficulty_name);
        ReadTable(f, Menus.gameplay_name, SZ_gameplay_name);
        ReadMenuInt(f, 10, menuInt_entries[10], SZ_menuInt);
        ReadTable(f, inputDevices, SZ_inputDevices);
        ReadTable(f, networkText, SZ_networkText);
        ReadMenuInt(f, 11, menuInt_entries[11], SZ_menuInt);
        ReadTable(f, difficultyNameB, SZ_difficultyNameB);
        ReadMenuInt(f, 12, menuInt_entries[12], SZ_menuInt);
        ReadMenuInt(f, 13, menuInt_entries[13], SZ_menuInt);
        ReadTable(f, joyButtonNames, SZ_joyButtonNames);
        ReadTable(f, superShips, SZ_superShips);
        ReadTable(f, specialName, SZ_specialName);
        ReadTable(f, destructHelp, SZ_destructHelp);
        ReadTable(f, weaponNames, SZ_weaponNames);
        ReadTable(f, destructModeName, SZ_destructModeName);

        // Ship Info（每項兩段）
        skip_pascal_string(f);
        for (int i = 0; i < shipInfo.Length; ++i)
        {
            shipInfo[i][0] = read_encrypted_pascal_string(f, SZ_shipInfo);
            shipInfo[i][1] = read_encrypted_pascal_string(f, SZ_shipInfo);
        }
        skip_pascal_string(f);

        // Menu 14（末段無尾 skip）
        skip_pascal_string(f);
        for (int i = 0; i < menuInt_entries[14]; ++i)
            menuInt[14][i] = read_encrypted_pascal_string(f, SZ_menuInt);

        CFile.fclose(f);
    }

    private static void ReadTable(Stream f, string[] table, int size)
    {
        skip_pascal_string(f);
        for (int i = 0; i < table.Length; ++i)
            table[i] = read_encrypted_pascal_string(f, size);
        skip_pascal_string(f);
    }

    private static void ReadMenuInt(Stream f, int idx, int count, int size)
    {
        skip_pascal_string(f);
        for (int i = 0; i < count; ++i)
            menuInt[idx][i] = read_encrypted_pascal_string(f, size);
        skip_pascal_string(f);
    }

    public static void JE_helpBox(SDL_Surface screen, int x, int y, string message, byte boxWidth, byte verticalHeight, byte color, byte brightness, byte shadeType)
    {
        if (message.Length == 0) return;

        int pos = 1, endpos = 0;
        bool endstring = false;

        do
        {
            int startpos = endpos + 1;
            do
            {
                endpos = pos;
                do
                {
                    pos++;
                    if (pos == message.Length)
                    {
                        endstring = true;
                        if ((uint)(pos - startpos) < boxWidth)
                            endpos = pos + 1;
                    }
                } while (!(message[pos - 1] == ' ' || endstring));
            } while (!((uint)(pos - startpos) > boxWidth || endstring));

            int subLen = Math.Min(endpos - startpos, message.Length - (startpos - 1));
            if (subLen < 0) subLen = 0;
            string substring = message.Substring(startpos - 1, subLen);
            Fonthand.JE_textShade(screen, x, y, substring, color, brightness, shadeType);

            y += verticalHeight;
        } while (!endstring);

        if (endpos != pos + 1 && endpos <= message.Length)
            Fonthand.JE_textShade(screen, x, y, message.Substring(Math.Min(endpos, message.Length)), color, brightness, shadeType);
    }

    public static void JE_HBox(SDL_Surface screen, int x, int y, byte messageNum, byte boxWidth, byte verticalHeight, byte color, byte brightness)
    {
        JE_helpBox(screen, x, y, helpTxt[messageNum - 1], boxWidth, verticalHeight, color, brightness, Fonthand.FULL_SHADE);
    }
}
