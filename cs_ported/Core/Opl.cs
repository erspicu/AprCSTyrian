namespace AprCSTyrian.Core;

/// <summary>
/// 對應 sources/src/opl.c（DOSBox OPL2/OPL3 FM 模擬器）。
/// **目前為靜音 stub** —— 完整 1620 行 DSP 模擬待下一輪移植。
/// 在那之前音樂為靜音；音效(SFX) 不經過 OPL，故仍可正常發聲。
/// </summary>
internal static unsafe class Opl
{
    public static void opl_init() { /* adlib_init(audioSampleRate) — full port pending */ }

    /// <summary>對應 adlib_getsample：產生 num 個 mono 樣本到 buf。stub 輸出靜音。</summary>
    public static void opl_update(short* buf, int num)
    {
        for (int i = 0; i < num; ++i)
            buf[i] = 0;
    }

    public static void opl_write(int reg, byte val) { /* adlib_write — full port pending */ }
}
