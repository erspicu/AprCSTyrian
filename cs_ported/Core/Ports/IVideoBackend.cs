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

    /// <summary>把視窗座標轉成 320×200 遊戲座標（對應 video.c:mapWindowPointToScreen）。</summary>
    void MapWindowToScreen(ref int x, ref int y);

    /// <summary>切換全螢幕（對應 video.c:toggle_fullscreen）。</summary>
    void ToggleFullscreen();

    /// <summary>第一段放大濾鏡名稱（1x / NN2x / Scale2x / Scale3x / xBRZ2x / xBRZ3x）。供寫回 opentyrian.cfg。</summary>
    string FirstFilter { get; }

    /// <summary>第二段放大濾鏡名稱（none / NN2x / Scale2x / Scale3x / xBRZ2x / xBRZ3x）。供寫回 opentyrian.cfg。</summary>
    string SecondFilter { get; }

    /// <summary>
    /// 設定兩段式放大濾鏡管線（第一段 → 第二段）。未知名稱當作無作用（1x/none）。
    /// 切換時負責安全釋放/重建中介緩衝與 texture/視窗，不可 crash。
    /// </summary>
    void SetFilters(string first, string second);

    /// <summary>把目前畫面存成 PNG 截圖（存到執行檔旁 screenshot/ 目錄，檔名為時間戳）。</summary>
    void SaveScreenshot();
}
