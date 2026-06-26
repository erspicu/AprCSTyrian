namespace AprCSTyrian.Core;

/// <summary>
/// 對應 sources/src/lds_play.c（Loudness .lds 音樂格式播放器，驅動 OPL 暫存器）。
/// **目前為 stub** —— 完整 772 行播放器待下一輪與 OPL 一併移植。
/// </summary>
internal static class Lds
{
#pragma warning disable CS0649 // 由完整 lds 播放器移植後指派
    public static bool playing, songlooped;
#pragma warning restore CS0649

    public static int lds_update() => 0;
    public static bool lds_load(Stream f, uint music_offset, uint music_size) => true;
    public static void lds_free() { }
    public static void lds_rewind() { }
    public static void lds_fade(byte speed) { }
}
