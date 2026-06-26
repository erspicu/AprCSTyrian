namespace AprCSTyrian.Core.Ports;

/// <summary>
/// 8-bit RGB 調色盤項目。原始 VGA 為 6-bit (0..63)，Adapter 負責在需要時擴展到 0..255。
/// Core 內部一律使用此 8-bit 表示。
/// </summary>
public readonly record struct Color(byte R, byte G, byte B)
{
    /// <summary>由 VGA 6-bit (0..63) 分量建立 8-bit 顏色。</summary>
    public static Color FromVga(byte r6, byte g6, byte b6) =>
        new((byte)(r6 * 255 / 63), (byte)(g6 * 255 / 63), (byte)(b6 * 255 / 63));
}
