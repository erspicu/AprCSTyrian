namespace AprCSTyrian.Core;

/// <summary>
/// 移植 sources/src/mtrand.c — Mersenne Twister。忠實沿用原始指標式實作
/// （state 向量以非託管記憶體配置，p0/p1/pm 為 <c>uint*</c>），確保亂數序列與原版一致。
/// 原始 <c>unsigned long</c> 在 0xffffffff 遮罩下為 32-bit 語意，故以 <c>uint</c> 對應。
/// </summary>
internal static unsafe class MtRand
{
    private const int N = 624;
    private const int M = 397;
    private const uint MATRIX_A = 0x9908b0dfu;   // constant vector a
    private const uint UPPER_MASK = 0x80000000u; // most significant w-r bits
    private const uint LOWER_MASK = 0x7fffffffu; // least significant r bits

    public const uint MT_RAND_MAX = 0xffffffffu;

    private static uint* x;              // the array for the state vector [N]
    private static uint* p0, p1, pm;

    private static void EnsureState()
    {
        if (x == null)
            x = (uint*)CMem.calloc(N, sizeof(uint));
    }

    public static void mt_srand(uint s)
    {
        EnsureState();

        x[0] = s & 0xffffffffu;
        for (int i = 1; i < N; ++i)
        {
            x[i] = unchecked(1812433253u * (x[i - 1] ^ (x[i - 1] >> 30)) + (uint)i)
                 & 0xffffffffu;          // for >32 bit machines
        }
        p0 = x;
        p1 = x + 1;
        pm = x + M;
    }

    /// <summary>generates a random number on the interval [0,0xffffffff]</summary>
    public static uint mt_rand()
    {
        EnsureState();

        if (p0 == null)
        {
            // Default seed
            mt_srand(5489u);
        }

        // Twisted feedback
        uint y = *p0 = unchecked(*pm++ ^ (((*p0 & UPPER_MASK) | (*p1 & LOWER_MASK)) >> 1)
                              ^ ((uint)(~(*p1 & 1) + 1) & MATRIX_A));
        p0 = p1++;
        if (pm == x + N) pm = x;
        if (p1 == x + N) p1 = x;

        // Temper
        y ^= y >> 11;
        y ^= (y << 7) & 0x9d2c5680u;
        y ^= (y << 15) & 0xefc60000u;
        y ^= y >> 18;
        return y;
    }

    /// <summary>generates a random number on the interval [0,1].</summary>
    public static float mt_rand_1() => (float)mt_rand() / (float)MT_RAND_MAX;

    /// <summary>generates a random number on the interval [0,1).</summary>
    public static float mt_rand_lt1() => (float)mt_rand() / ((float)MT_RAND_MAX + 1.0f);

    /// <summary>釋放 state 向量（程式結束時呼叫）。</summary>
    public static void Shutdown()
    {
        if (x != null)
        {
            CMem.free(x);
            x = null;
            p0 = p1 = pm = null;
        }
    }
}
