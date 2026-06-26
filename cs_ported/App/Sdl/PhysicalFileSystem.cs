using AprCSTyrian.Core.Ports;

namespace AprCSTyrian.App.Sdl;

/// <summary>
/// <see cref="IFileSystem"/> 的實作：資料檔與使用者檔分別對應到兩個實體根目錄。
/// （與 SDL 無關，但屬於平台層職責，故放在 App。）
/// </summary>
internal sealed class PhysicalFileSystem : IFileSystem
{
    private readonly string _dataRoot;
    private readonly string _userRoot;

    public PhysicalFileSystem(string dataRoot, string userRoot)
    {
        _dataRoot = dataRoot;
        _userRoot = userRoot;
        Directory.CreateDirectory(_userRoot);
    }

    public bool DataFileExists(string relativePath) =>
        File.Exists(Path.Combine(_dataRoot, relativePath));

    public Stream OpenData(string relativePath) =>
        File.OpenRead(Path.Combine(_dataRoot, relativePath));

    public Stream OpenUserWrite(string relativePath)
    {
        string full = Path.Combine(_userRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        return File.Create(full);
    }

    public Stream? OpenUserRead(string relativePath)
    {
        string full = Path.Combine(_userRoot, relativePath);
        return File.Exists(full) ? File.OpenRead(full) : null;
    }
}
