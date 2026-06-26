namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/editship.c —— 額外（自訂編輯器）船艦圖載入與解密。
/// 通常 newsh$.shp 不存在，extraAvail 維持 false（no-op）。
/// </summary>
internal static unsafe class Editship
{
    private const int SAS = 154 - 4; // sizeof(JE_ShipsType) - 4

    private static readonly byte[] extraCryptKey = { 58, 23, 16, 192, 254, 82, 113, 147, 62, 99 };

    public static bool extraAvail;
    public static readonly byte[] extraShips = new byte[154]; // JE_ShipsType
    public static Sprite2_array extraShapes;
    public static ushort extraShapeSize;

    public static void JE_decryptShips()
    {
        byte[] s2 = new byte[154];
        bool correct = true;

        for (int x = SAS - 1; x >= 0; x--)
        {
            s2[x] = (byte)(extraShips[x] ^ extraCryptKey[(x + 1) % 10]);
            if (x > 0)
                s2[x] ^= extraShips[x - 1];
        }

        byte y;
        y = 0;
        for (int x = 0; x < SAS; x++) y += s2[x];
        if (extraShips[SAS + 0] != y) correct = false;
        y = 0;
        for (int x = 0; x < SAS; x++) y -= s2[x];
        if (extraShips[SAS + 1] != y) correct = false;
        y = 1;
        for (int x = 0; x < SAS; x++) y = (byte)(y * s2[x] + 1);
        if (extraShips[SAS + 2] != y) correct = false;
        y = 0;
        for (int x = 0; x < SAS; x++) y ^= s2[x];
        if (extraShips[SAS + 3] != y) correct = false;

        if (!correct)
            throw new TyrianHaltException(255);

        Array.Copy(s2, extraShips, extraShips.Length);
    }

    public static void JE_loadExtraShapes()
    {
        Stream? f = CFile.dir_fopen(Config.get_user_directory(), "newsh$.shp", "rb");
        if (f != null)
        {
            extraAvail = true;
            extraShapeSize = (ushort)(CFile.ftell_eof(f) - extraShips.Length);
            extraShapes.size = extraShapeSize;
            extraShapes.data = (byte*)CMem.malloc(extraShapeSize);
            CFile.fread_die(extraShapes.data, extraShapeSize, 1, f);
            fixed (byte* p = extraShips) CFile.fread_die(p, (nuint)extraShips.Length, 1, f);
            JE_decryptShips();
            CFile.fclose(f);
        }
    }
}
