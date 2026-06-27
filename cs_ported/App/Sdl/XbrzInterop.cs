namespace AprNes;

/// <summary>
/// libXBRz.cs（向量化濾鏡，源自 AprNes 專案）唯一的外部相依：非託管記憶體配置。
/// 此處以 Marshal.AllocHGlobal 提供（xBRZ 的查表/緩衝為長生命週期、一次性配置）。
/// </summary>
internal static class NesCore
{
    public static unsafe void* AllocUnmanaged(int bytes)
        => (void*)System.Runtime.InteropServices.Marshal.AllocHGlobal(bytes);
}
