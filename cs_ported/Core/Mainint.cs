namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/mainint.c —— 主流程（標題畫面/遊戲流程）。**逐步移植中**：
/// 先放入可獨立切割的函式（JE_initPlayerData 等），titleScreen/JE_main 等大型流程待後續。
/// </summary>
internal static unsafe partial class Mainint
{
    private static void StrCpy(byte[] dst, string src)
    {
        int n = Math.Min(src.Length, dst.Length - 1);
        for (int i = 0; i < n; ++i) dst[i] = (byte)src[i];
        dst[n] = 0;
    }

    public static void JE_initPlayerData()
    {
        var player = Players.player;

        // New Game Items/Data
        player[0].items.ship = 1;                              // USP Talon
        player[0].items.weapon[Players.FRONT_WEAPON].id = 1;   // Pulse Cannon
        player[0].items.weapon[Players.REAR_WEAPON].id = 0;    // None
        player[0].items.shield = 4;                            // Gencore High Energy Shield
        player[0].items.generator = 2;                         // Advanced MR-12
        for (int i = 0; i < 2; ++i)
            player[0].items.sidekick[i] = 0;                   // None
        player[0].items.special = 0;                           // None

        player[0].last_items = player[0].items;

        player[1].items = player[0].items;
        player[1].items.weapon[Players.REAR_WEAPON].id = 15;   // Vulcan Cannon
        player[1].items.sidekick_level = 101;                  // 101, 102, 103
        player[1].items.sidekick_series = 0;                   // None

        Config.gameHasRepeated = false;
        Config.onePlayerAction = false;
        Config.superArcadeMode = VarzConst.SA_NONE;
        Config.superTyrian = false;
        Config.twoPlayerMode = false;

        Config.secretHint = (byte)((MtRand.mt_rand() % 3) + 1);

        for (int p = 0; p < 2; ++p)
        {
            for (int i = 0; i < 2; ++i)
                player[p].items.weapon[i].power = 1;

            player[p].weapon_mode = 1;
            player[p].armor = Episodes.ships[player[p].items.ship].dmg;

            player[p].is_dragonwing = (p == 1);
            // TODO: 原為 player[p].lives = &player[p].items.weapon[p].power（C 指標別名 hack）；
            //       class 欄位無法穩定取址，待生命值系統移植時改為等價存取。
        }

        Config.mainLevel = Episodes.FIRST_LEVEL;
        Config.saveLevel = Episodes.FIRST_LEVEL;

        StrCpy(Config.lastLevelName, Helptext.miscText[19]);
    }
}
