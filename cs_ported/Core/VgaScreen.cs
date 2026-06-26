namespace AprCSTyrian.Core;

/// <summary>
/// VGA 模式常數。Tyrian 以 320×200 256 色 (mode 13h) 作畫。
/// </summary>
public static class VgaScreen
{
    public const int Width = 320;
    public const int Height = 200;
    public const int PixelCount = Width * Height;
}
