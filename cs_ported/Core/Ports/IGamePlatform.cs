namespace AprCSTyrian.Core.Ports;

/// <summary>
/// 平台聚合埠：把所有多媒體/系統埠集中傳給 Core。
/// App 端的組合根 (Composition Root) 建立具體 (SDL) 實作後注入。
/// </summary>
public interface IGamePlatform
{
    IVideoBackend Video { get; }
    IAudioBackend Audio { get; }
    IInputBackend Input { get; }
    IClock Clock { get; }
    IFileSystem Files { get; }
}
