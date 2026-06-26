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

    /// <summary>
    /// 開啟裝置並以 <paramref name="source"/> 作為樣本來源。開啟後**保持暫停**，
    /// 待呼叫端完成初始化後再以 <see cref="SetPaused"/>(false) 開始。
    /// </summary>
    void Start(IAudioSource source);

    /// <summary>暫停/恢復播放。</summary>
    void SetPaused(bool paused);

    /// <summary>鎖定（暫停）音訊 callback，期間可安全修改共享狀態（對應 SDL_LockAudioDevice）。</summary>
    void Lock();

    /// <summary>解除鎖定（對應 SDL_UnlockAudioDevice）。</summary>
    void Unlock();
}
