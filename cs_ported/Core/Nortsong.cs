namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/nortsong.c —— 目前僅移植**畫面計時**部分（對應原 PIT frameCount 機制），
/// 以 <see cref="Globals.Clock"/> 取代 SDL_GetTicks/SDL_Delay。
/// 音效載入/音量 (loadSndFile/JE_playSampleNum/JE_changeVolume) 待 Phase D 音訊一併移植。
/// </summary>
internal static class Nortsong
{
    public static ushort frameCountMax = 0;

    // x86 PIT 頻率為 (315/88/3) MHz；每 `speed` 週期觸發一次中斷以遞減 frameCount。
    private static ushort frameSpeed = 0x4300;

    // Fixed point UQ6.10 milliseconds.
    private static ushort framePeriod = (ushort)(((ulong)0x4300 << 10) * 1000 * 88 * 3 / 315000000);

    // Fixed point UQ22.10 milliseconds.
    private static uint frameCountEnd = 0;
    private static uint frameCount2End = 0;

    public static void setFrameSpeed(ushort speed)
    {
        frameSpeed = speed;
        framePeriod = (ushort)(((ulong)speed << 10) * 1000 * 88 * 3 / 315000000);

        uint now = Globals.Clock.Ticks << 10;
        frameCountEnd = now;
    }

    public static void setFrameCount(ushort frameCount)
    {
        // Keep the partial timer period that has already elapsed.
        uint now = Globals.Clock.Ticks << 10;
        int diff = (int)(now - frameCountEnd);
        if (diff >= framePeriod)
            frameCountEnd = now - (uint)diff % framePeriod;
        else if (-diff >= framePeriod)
            frameCountEnd = now + (uint)(-diff) % framePeriod;

        frameCountEnd += (uint)(frameCount * framePeriod);
    }

    public static void setFrameCount2(ushort frameCount2)
    {
        uint now = Globals.Clock.Ticks << 10;
        int diff = (int)(now - frameCount2End);
        if (diff >= framePeriod)
            frameCount2End = now - (uint)diff % framePeriod;
        else if (-diff >= framePeriod)
            frameCount2End = now + (uint)(-diff) % framePeriod;

        frameCount2End += (uint)(frameCount2 * framePeriod);
    }

    public static uint getFrameCountTicks()
    {
        const uint half = 1 << 9;
        uint now = Globals.Clock.Ticks << 10;
        int diff = (int)(frameCountEnd - now);
        return diff >= 0 ? ((uint)diff + half) >> 10 : 0;
    }

    public static uint getFrameCount2Ticks()
    {
        const uint half = 1 << 9;
        uint now = Globals.Clock.Ticks << 10;
        int diff = (int)(frameCount2End - now);
        return diff >= 0 ? ((uint)diff + half) >> 10 : 0;
    }

    public static void delayUntilElapsed()
    {
        const uint half = 1 << 9;
        uint now = Globals.Clock.Ticks << 10;
        int diff = (int)(frameCountEnd - now);
        if (diff >= 0)
            Globals.Clock.Delay(((uint)diff + half) >> 10);
    }

    /// <summary>播放音效（暫行 no-op，音訊待 Phase D；對應 nortsong.c:JE_playSampleNum）。</summary>
    public static void JE_playSampleNum(byte samplenum) { /* TODO: Phase D 音訊 */ }
}
