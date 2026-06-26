namespace AprCSTyrian.Core.Ports;

/// <summary>
/// 畫面輸出埠 (Port)。Core 以 320×200 indexed (8-bit 調色盤) 緩衝作畫，
/// 由 Adapter 負責放大、轉 RGB 並呈現到實際視窗。
/// 視窗的建立/標題/縮放等屬 Adapter 職責，Core 不涉入。
/// </summary>
public interface IVideoBackend : IDisposable
{
    /// <summary>邏輯畫面寬度（像素）。</summary>
    int Width { get; }

    /// <summary>邏輯畫面高度（像素）。</summary>
    int Height { get; }

    /// <summary>設定 256 色調色盤（不足 256 筆時其餘維持原值）。</summary>
    void SetPalette(ReadOnlySpan<Color> palette);

    /// <summary>
    /// 呈現一張 indexed 影格。<paramref name="indexedPixels"/> 長度須為 Width*Height，
    /// 每個 byte 為調色盤索引。
    /// </summary>
    void Present(ReadOnlySpan<byte> indexedPixels);
}
