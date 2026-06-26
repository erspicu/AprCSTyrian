namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/lvllib.c —— 關卡檔分析：取得關卡數與各關資料偏移。
/// </summary>
internal static unsafe class Lvllib
{
    public static readonly int[] lvlPos = new int[43]; // JE_LvlPosType [1..42+1]
    public static readonly byte[] levelFile = new byte[13];
    public static ushort lvlNum;

    public static void JE_analyzeLevel()
    {
        Stream f = CFile.dir_fopen_die(CFile.data_dir(), CStr(levelFile), "rb");

        lvlNum = CFile.read_u16(f);

        fixed (int* p = lvlPos)
            CFile.fread_s32_die(p, lvlNum, f);

        lvlPos[lvlNum] = (int)CFile.ftell_eof(f);

        CFile.fclose(f);
    }

    private static string CStr(byte[] s)
    {
        int n = 0;
        while (n < s.Length && s[n] != 0) n++;
        var c = new char[n];
        for (int i = 0; i < n; ++i) c[i] = (char)s[i];
        return new string(c);
    }
}
