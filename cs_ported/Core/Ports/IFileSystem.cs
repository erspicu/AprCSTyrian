namespace AprCSTyrian.Core.Ports;

/// <summary>
/// 檔案存取埠 (Port)。涵蓋遊戲資料檔（唯讀）與設定/存檔（讀寫）。
/// 路徑使用相對於各自根目錄的相對路徑，由 Adapter 對應到實際位置。
/// </summary>
public interface IFileSystem
{
    /// <summary>遊戲資料檔是否存在（如 *.SHP、TYRIAN.PIC）。</summary>
    bool DataFileExists(string relativePath);

    /// <summary>開啟唯讀資料檔串流。找不到時擲出。</summary>
    Stream OpenData(string relativePath);

    /// <summary>開啟（或建立）使用者資料檔以供寫入（設定/存檔）。</summary>
    Stream OpenUserWrite(string relativePath);

    /// <summary>開啟使用者資料檔以供讀取；不存在時回傳 null。</summary>
    Stream? OpenUserRead(string relativePath);
}
