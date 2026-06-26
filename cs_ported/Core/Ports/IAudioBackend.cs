namespace AprCSTyrian.Core.Ports;

/// <summary>
/// 音訊樣本來源。混音（OPL FM 模擬、取樣音效疊加）邏輯屬於 Core，
/// Adapter 只負責把產生的 PCM 推送到音效裝置。
/// </summary>
public interface IAudioSource
{
    /// <summary>
    /// 填滿輸出緩衝。<paramref name="buffer"/> 為交錯 (interleaved) 的 16-bit 帶符號樣本，
    /// 長度 = 幀數 × 聲道數。實作須填滿整個緩衝（無資料時填靜音 0）。
    /// </summary>
    void GenerateSamples(Span<short> buffer);
}

/// <summary>
/// 音訊輸出埠 (Port)。
/// </summary>
public interface IAudioBackend : IDisposable
{
    /// <summary>實際取樣率 (Hz)。</summary>
    int SampleRate { get; }

    /// <summary>聲道數 (1=mono, 2=stereo)。</summary>
    int Channels { get; }

    /// <summary>開始播放，並以 <paramref name="source"/> 作為樣本來源。</summary>
    void Start(IAudioSource source);

    /// <summary>暫停/恢復播放。</summary>
    void SetPaused(bool paused);
}
