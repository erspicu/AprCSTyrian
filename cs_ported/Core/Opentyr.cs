namespace AprCSTyrian.Core;

/// <summary>
/// 對應 sources/src/opentyr.h 的全域常數/巨集與版本字串。
/// （進入點 main/setupMenu 之後於 Phase F 移植。）
/// </summary>
internal static class Opentyr
{
    public const string TYRIAN_VERSION = "2.1";

    public static readonly string opentyrian_str = "OpenTyrian";
    public static readonly string opentyrian_version = "AprCSTyrian";

    // 巨集對應（COUNTOF/MIN/MAX）。C# 已有 Math.Min/Max，但保留同名以利對照。
    public static uint COUNTOF<T>(T[] a) => (uint)a.Length;

    public static int MIN(int a, int b) => a < b ? a : b;
    public static int MAX(int a, int b) => a > b ? a : b;
    public static float MIN(float a, float b) => a < b ? a : b;
    public static float MAX(float a, float b) => a > b ? a : b;

    public const double M_PI = 3.14159265358979323846;
    public const double M_PI_2 = 1.57079632679489661923;
    public const double M_PI_4 = 0.78539816339744830962;
}
