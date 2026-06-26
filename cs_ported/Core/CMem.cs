using System.Runtime.InteropServices;

namespace AprCSTyrian.Core;

/// <summary>
/// C 風格記憶體配置包裝（對應 malloc/calloc/realloc/free），底層用 <see cref="NativeMemory"/>。
/// 用於忠實移植 C 的非託管陣列/指標。DEBUG 下追蹤配置，可在結束時驗證無 leak。
/// </summary>
internal static unsafe class CMem
{
#if DEBUG
    private static readonly object _lock = new();
    private static readonly Dictionary<nint, nuint> _allocs = new();

    private static void Track(void* p, nuint size)
    {
        if (p == null) return;
        lock (_lock) _allocs[(nint)p] = size;
    }

    private static void Untrack(void* p)
    {
        if (p == null) return;
        lock (_lock) _allocs.Remove((nint)p);
    }
#else
    private static void Track(void* p, nuint size) { }
    private static void Untrack(void* p) { }
#endif

    public static void* malloc(nuint size)
    {
        void* p = NativeMemory.Alloc(size);
        Track(p, size);
        return p;
    }

    public static void* calloc(nuint num, nuint size)
    {
        void* p = NativeMemory.AllocZeroed(num, size);
        Track(p, num * size);
        return p;
    }

    public static void* realloc(void* ptr, nuint newSize)
    {
        Untrack(ptr);
        void* p = NativeMemory.Realloc(ptr, newSize);
        Track(p, newSize);
        return p;
    }

    public static void free(void* ptr)
    {
        if (ptr == null) return;
        Untrack(ptr);
        NativeMemory.Free(ptr);
    }

    /// <summary>結束時呼叫；DEBUG 下若仍有未釋放配置則擲出，協助揪出 memory leak。</summary>
    public static void AssertNoLeaks()
    {
#if DEBUG
        lock (_lock)
        {
            if (_allocs.Count != 0)
            {
                nuint total = 0;
                foreach (var kv in _allocs) total += kv.Value;
                throw new InvalidOperationException(
                    $"CMem 偵測到 {_allocs.Count} 筆未釋放配置（共 {total} bytes）。");
            }
        }
#endif
    }
}
