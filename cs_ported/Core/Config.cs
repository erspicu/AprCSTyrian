// 設定全域由 config.c（待移植）指派；此為最小占位。
#pragma warning disable CS0649

namespace AprCSTyrian.Core;

/// <summary>
/// 對應 sources/src/config.c/config.h 的全域設定 —— **目前為最小占位**，
/// 僅放入其他模組已需要的少數全域；完整 config（設定/存檔序列化）待 Phase 後段移植。
/// </summary>
internal static partial class Config
{
    public static bool twoPlayerMode;
    public static bool galagaMode;

    // config.c 全域（shots/武器邏輯所需；完整 config 移植時整併）
    public static uint power, lastPower, powerAdd;
    public static readonly byte[] shotRepeat = new byte[11];
    public static readonly byte[] shotMultiPos = new byte[11];
    public static bool background2;
}
