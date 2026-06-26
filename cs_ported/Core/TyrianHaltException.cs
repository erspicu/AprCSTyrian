namespace AprCSTyrian.Core;

/// <summary>
/// 對應原始 C 的 exit()/JE_tyrianHalt 結束流程。以例外取代 exit()，
/// 讓進入點 (組合根) 能優雅釋放 SDL/非託管資源後再結束。
/// </summary>
public sealed class TyrianHaltException : Exception
{
    public int Code { get; }

    public TyrianHaltException(int code) : base($"JE_tyrianHalt({code})")
    {
        Code = code;
    }
}
