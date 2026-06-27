namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/varz.c —— 遊戲全域變數與少數工具函式。
/// 目前僅放入早期被依賴的 <see cref="JE_tyrianHalt"/>；其餘全域將隨各模組移植陸續補入。
/// </summary>
internal static partial class Varz
{
    // === 遊戲核心資料層全域（對應 varz.h 的 struct 陣列；其餘 scalar 全域與 const 表隨函式移植補入）===
    public static readonly JE_SingleEnemyType[] enemy = new JE_SingleEnemyType[100];     // JE_MultiEnemyType
    public static readonly byte[] enemyAvail = new byte[100];                            // JE_EnemyAvailType
    public static readonly boss_bar_t[] boss_bar = new boss_bar_t[2];                     // boss 血條
    public static readonly bool[] enemyShotAvail = new bool[VarzConst.ENEMY_SHOT_MAX];
    public static readonly EnemyShotType[] enemyShot = new EnemyShotType[VarzConst.ENEMY_SHOT_MAX];
    public static readonly Explosion[] explosions = new Explosion[VarzConst.MAX_EXPLOSIONS];
    public static readonly rep_explosion_type[] rep_explosions = new rep_explosion_type[VarzConst.MAX_REPEATING_EXPLOSIONS];
    public static readonly superpixel_type[] superpixels = new superpixel_type[VarzConst.MAX_SUPERPIXELS];

    /// <summary>結束遊戲（對應 varz.c:JE_tyrianHalt）。以例外解開呼叫堆疊，由組合根清理資源。</summary>
    public static void JE_tyrianHalt(byte code)
    {
        // 原始會在此釋放 audio/video/shape tables/sound samples。
        // 這些模組尚未全部移植；已移植者於此釋放，其餘待補。
        // 對應 varz.c:JE_tyrianHalt 順序：deinit_joysticks（將指派寫入 opentyrian_config）
        // 須在 saveConfiguration（→save_opentyrian_config 寫檔）之前。
        Joystick.deinit_joysticks();

        MtRand.Shutdown();

        if (code != 9)
        {
            Config.saveConfiguration();  // 內含 save_opentyrian_config（寫出 opentyrian.cfg）
            Config.saveSaves();
        }

        throw new TyrianHaltException(code);
    }
}
